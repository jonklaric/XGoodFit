#nullable enable
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace BestFitFileCompat;

/// <summary>
/// A description of a saved <c>.bestfit</c> project file (a SQLite database), table by table and cell
/// by cell, precise enough to tell whether two saves wrote the same thing.
/// </summary>
/// <remarks>
/// XGoodFit addition (docs/migration-plan.md, Phase 6). This file is compiled both into the port's
/// file-compatibility tests and into tools/BestFitFileCompat.Wpf, which describes what upstream's WPF
/// RMC-BestFit saves; the descriptions it writes are the tests' golden data (Golden/README.md).
/// <para>
/// Each cell is a token that keeps its SQLite storage class:
/// <list type="bullet">
/// <item><c>n</c>: NULL.</item>
/// <item><c>i:42</c>: an INTEGER.</item>
/// <item><c>r:0.1</c>: a REAL, as .NET's shortest round-trip text, so equal text means equal bits.</item>
/// <item><c>t:text</c>: TEXT of up to <see cref="InlineTextLength"/> characters, escaped (<see cref="Escape"/>).</item>
/// <item><c>t#bytes:sha</c>: longer TEXT, as its UTF-8 length and a SHA-256 prefix. When the text has
/// Windows line ends or a narrow no-break space, <c>/sha:flags</c> follows: the hash of its
/// <see cref="Canonical">canonical form</see>, which tells platform differences from real ones, and
/// which of them it has (<see cref="CanonicalFlags"/>).</item>
/// <item><c>p:text</c>: TEXT that named the folder the file was saved in, with that folder as <c>{dir}</c>.
/// Upstream stores the project's own path (Project.FullFileName), which is a temporary copy here.</item>
/// <item><c>b#bytes:sha</c>: a BLOB. When it inflates (Numerics' <c>Tools.Compress</c> is raw Deflate),
/// <c>&gt;z#bytes:sha</c> describes what it holds, with a canonical hash as for text.</item>
/// </list>
/// A token that starts with <c>~</c> is volatile: upstream wrote different values when it saved the same
/// file twice, so it isn't compared.
/// </para>
/// <para>
/// Besides the file, a description records the in-memory state: a hash per element after the project
/// was opened (<see cref="States"/>), and what differed after the saved file was opened again
/// (<see cref="ReopenDifferences"/>).
/// </para>
/// </remarks>
public sealed class FileDescription
{
    /// <summary>The format's first line.</summary>
    public const string Header = "bestfit-file-description\t1";

    /// <summary>The longest text that is written out in full rather than hashed.</summary>
    public const int InlineTextLength = 80;

    /// <summary>The project's path relative to the examples folder, with '/' separators.</summary>
    public string Name { get; set; } = "";

    /// <summary>Database settings that describe its content: encoding, journal mode, versions.</summary>
    public List<KeyValuePair<string, string>> Pragmas { get; } = new();

    /// <summary>The schema objects other than tables (indexes, views, triggers): type, name, table, SQL hash.</summary>
    public List<string[]> Objects { get; } = new();

    /// <summary>The tables, by name (ordinal order).</summary>
    public List<TableDescription> Tables { get; } = new();

    /// <summary>
    /// The in-memory state that differed after the saved file was opened again
    /// (<see cref="StateSnapshot.Differences"/>): property paths, ideally none.
    /// </summary>
    public List<string> ReopenDifferences { get; } = new();

    /// <summary>
    /// The in-memory state the project opened to, a hash per element (<see cref="StateSnapshot.GroupHashes"/>):
    /// the group, the hash of its values, and the hash with numbers left out.
    /// </summary>
    public List<string[]> States { get; } = new();

    /// <summary>A fingerprint of the whole file: its pragmas, schema and every table's fingerprint.</summary>
    public string Fingerprint
    {
        get
        {
            var text = new StringBuilder();
            foreach (KeyValuePair<string, string> pragma in Pragmas) text.Append(pragma.Key).Append('=').Append(pragma.Value).Append('\n');
            foreach (string[] item in Objects) text.Append(string.Join('\t', item)).Append('\n');
            foreach (TableDescription table in Tables) text.Append(table.Name).Append('\t').Append(table.Fingerprint).Append('\n');
            return Sha(text.ToString());
        }
    }

    /// <summary>The number of cells in all tables.</summary>
    public int CellCount => Tables.Sum(t => t.Rows.Count * t.Columns.Count);

    /// <summary>
    /// Describes a project file. <paramref name="savedIn"/> is the folder it was saved in, whose name
    /// is replaced with <c>{dir}</c> wherever a text cell contains it. <paramref name="store"/>, if given,
    /// receives the content of every hashed cell with its hash: the text, or a blob's inflated content.
    /// </summary>
    public static FileDescription Describe(string file, string name, string? savedIn, Action<string, byte[]>? store = null)
    {
        var description = new FileDescription { Name = name };
        string connectionString = new SQLiteConnectionStringBuilder { DataSource = file, ReadOnly = true, Pooling = false }.ConnectionString;
        using var connection = new SQLiteConnection(connectionString);
        connection.Open();

        foreach (string pragma in new[] { "encoding", "journal_mode", "user_version", "application_id", "auto_vacuum" })
        {
            using var command = new SQLiteCommand("PRAGMA " + pragma, connection);
            description.Pragmas.Add(new KeyValuePair<string, string>(pragma, Convert.ToString(command.ExecuteScalar(), CultureInfo.InvariantCulture) ?? ""));
        }

        var tables = new List<(string Name, string Sql)>();
        using (var command = new SQLiteCommand("SELECT type, name, tbl_name, sql FROM sqlite_master ORDER BY type, name", connection))
        using (SQLiteDataReader reader = command.ExecuteReader())
        {
            while (reader.Read())
            {
                string type = reader.GetString(0), objectName = reader.GetString(1), table = reader.GetString(2);
                string sql = reader.IsDBNull(3) ? "" : reader.GetString(3);
                if (type == "table")
                    tables.Add((objectName, sql));
                else
                    description.Objects.Add(new[] { type, objectName, table, Sha(sql) });
            }
        }
        tables.Sort((a, b) => string.CompareOrdinal(a.Name, b.Name));

        foreach ((string tableName, string sql) in tables)
        {
            var table = new TableDescription { Name = tableName, SqlHash = Sha(sql) };
            using (var command = new SQLiteCommand($"PRAGMA table_info({QuoteIdentifier(tableName)})", connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                    table.Columns.Add(new ColumnDescription(reader.GetString(1), reader.IsDBNull(2) ? "" : reader.GetString(2)));
            }

            // typeof() keeps the storage class; the unary plus drops the declared type, so the reader
            // returns the stored value as it is (long, double, string or byte[]).
            string select = "SELECT rowid" + string.Concat(table.Columns.Select(c => $", typeof({QuoteIdentifier(c.Name)}), +{QuoteIdentifier(c.Name)}")) +
                $" FROM {QuoteIdentifier(tableName)} ORDER BY rowid";
            using (var command = new SQLiteCommand(select, connection))
            using (SQLiteDataReader reader = command.ExecuteReader())
            {
                while (reader.Read())
                {
                    var cells = new string[table.Columns.Count];
                    for (int i = 0; i < cells.Length; i++)
                        cells[i] = CellToken(reader.GetString(1 + (2 * i)), reader.GetValue(2 + (2 * i)), savedIn, store);
                    table.Rows.Add(new RowDescription(reader.GetInt64(0), cells));
                }
            }
            description.Tables.Add(table);
        }
        return description;
    }

    /// <summary>The token for one stored value (see the class remarks).</summary>
    public static string CellToken(string storageClass, object value, string? savedIn, Action<string, byte[]>? store = null)
    {
        switch (storageClass)
        {
            case "null":
                return "n";
            case "integer":
                return "i:" + Convert.ToInt64(value, CultureInfo.InvariantCulture).ToString(CultureInfo.InvariantCulture);
            case "real":
                return "r:" + Convert.ToDouble(value, CultureInfo.InvariantCulture).ToString("R", CultureInfo.InvariantCulture);
            case "text":
            {
                string text = (string)value;
                if (!string.IsNullOrEmpty(savedIn) && text.Contains(savedIn, StringComparison.Ordinal))
                    return TextToken("p", text.Replace(savedIn, "{dir}", StringComparison.Ordinal), store);
                return TextToken("t", text, store);
            }
            case "blob":
            {
                byte[] bytes = (byte[])value;
                string sha = Sha(bytes);
                string token = $"b#{bytes.Length}:{sha}";
                byte[]? inflated = TryInflate(bytes);
                if (inflated == null)
                {
                    store?.Invoke(sha, bytes);
                    return token;
                }
                string inflatedSha = Sha(inflated);
                store?.Invoke(inflatedSha, inflated);
                token += $">z#{inflated.Length}:{inflatedSha}";
                string? canonical = CanonicalHash(inflated);
                if (canonical != null) token += "/" + canonical;
                return token;
            }
            default:
                throw new InvalidDataException("Unknown SQLite storage class: " + storageClass);
        }
    }

    private static string TextToken(string kind, string text, Action<string, byte[]>? store)
    {
        if (text.Length <= InlineTextLength)
            return kind + ":" + Escape(text);
        byte[] bytes = Encoding.UTF8.GetBytes(text);
        string sha = Sha(bytes);
        store?.Invoke(sha, bytes);
        string token = $"{kind}#{bytes.Length}:{sha}";
        string canonical = Canonical(text, kind == "p");
        if (canonical != text) token += $"/{Sha(canonical)}:{CanonicalFlags(text, kind == "p")}";
        return token;
    }

    /// <summary>
    /// The form of a text in which known platform differences disappear: Windows line ends become
    /// "\n" (XElement.ToString() writes Environment.NewLine), the narrow no-break space ICU puts before
    /// AM and PM becomes a space, and, in paths, '\' becomes '/'.
    /// </summary>
    public static string Canonical(string text, bool isPath = false)
    {
        string canonical = text.Replace("\r\n", "\n", StringComparison.Ordinal).Replace('\u202F', ' ');
        return isPath ? canonical.Replace('\\', '/') : canonical;
    }

    /// <summary>
    /// Which of the canonical form's changes apply to a text: <c>r</c> for Windows line ends, <c>s</c>
    /// for a narrow no-break space, <c>b</c> for a backslash in a path.
    /// </summary>
    public static string CanonicalFlags(string text, bool isPath = false) =>
        (text.Contains("\r\n", StringComparison.Ordinal) ? "r" : "") +
        (text.Contains('\u202F') ? "s" : "") +
        (isPath && text.Contains('\\') ? "b" : "");

    /// <summary>The hash and flags of an inflated blob's canonical text, if it is UTF-8 text that differs from it.</summary>
    private static string? CanonicalHash(byte[] content)
    {
        string text;
        try
        {
            text = new UTF8Encoding(false, true).GetString(content);
        }
        catch (DecoderFallbackException)
        {
            return null;
        }
        string canonical = Canonical(text);
        return canonical == text ? null : $"{Sha(canonical)}:{CanonicalFlags(text)}";
    }

    /// <summary>Inflates raw Deflate data, or returns null if the bytes aren't a complete Deflate stream.</summary>
    private static byte[]? TryInflate(byte[] bytes)
    {
        if (bytes.Length == 0) return null;
        try
        {
            using var input = new MemoryStream(bytes);
            using var output = new MemoryStream();
            using (var inflater = new DeflateStream(input, CompressionMode.Decompress, leaveOpen: true))
                inflater.CopyTo(output);
            // Only a stream that ends exactly where the data does counts; anything else is just bytes.
            return input.Position == bytes.Length && output.Length > 0 ? output.ToArray() : null;
        }
        catch (InvalidDataException)
        {
            return null;
        }
    }

    /// <summary>Escapes a text so that it fits on one line of ASCII: '\', tab, CR, LF and anything outside printable ASCII.</summary>
    public static string Escape(string text)
    {
        var escaped = new StringBuilder(text.Length);
        foreach (char c in text)
        {
            switch (c)
            {
                case '\\': escaped.Append(@"\\"); break;
                case '\t': escaped.Append(@"\t"); break;
                case '\r': escaped.Append(@"\r"); break;
                case '\n': escaped.Append(@"\n"); break;
                default:
                    if (c < ' ' || c > '~') escaped.Append(@"\u").Append(((int)c).ToString("X4", CultureInfo.InvariantCulture));
                    else escaped.Append(c);
                    break;
            }
        }
        return escaped.ToString();
    }

    /// <summary>Reverses <see cref="Escape"/>.</summary>
    public static string Unescape(string text)
    {
        var unescaped = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c != '\\' || i + 1 == text.Length)
            {
                unescaped.Append(c);
                continue;
            }
            char next = text[++i];
            switch (next)
            {
                case 't': unescaped.Append('\t'); break;
                case 'r': unescaped.Append('\r'); break;
                case 'n': unescaped.Append('\n'); break;
                case 'u':
                    unescaped.Append((char)int.Parse(text.AsSpan(i + 1, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                    i += 4;
                    break;
                default: unescaped.Append(next); break;
            }
        }
        return unescaped.ToString();
    }

    /// <summary>The first 16 hexadecimal digits of the SHA-256 of a text's UTF-8 bytes.</summary>
    public static string Sha(string text) => Sha(Encoding.UTF8.GetBytes(text));

    /// <summary>The first 16 hexadecimal digits of the SHA-256 of some bytes.</summary>
    public static string Sha(byte[] bytes) => Convert.ToHexString(SHA256.HashData(bytes), 0, 8).ToLowerInvariant();

    private static string QuoteIdentifier(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";

    /// <summary>Writes the description in its text form (see the class remarks).</summary>
    public void Write(TextWriter writer)
    {
        writer.Write(Header + "\n");
        writer.Write($"file\t{Escape(Name)}\tsha={Fingerprint}\ttables={Tables.Count}\tcells={CellCount}\n");
        foreach (KeyValuePair<string, string> pragma in Pragmas)
            writer.Write($"pragma\t{pragma.Key}\t{Escape(pragma.Value)}\n");
        foreach (string[] item in Objects)
            writer.Write("object\t" + string.Join('\t', item.Select(Escape)) + "\n");
        foreach (TableDescription table in Tables)
        {
            writer.Write($"table\t{Escape(table.Name)}\tsha={table.Fingerprint}\tsql={table.SqlHash}\trows={table.Rows.Count}\n");
            for (int i = 0; i < table.Columns.Count; i++)
                writer.Write($"column\t{i}\t{Escape(table.Columns[i].Name)}\t{Escape(table.Columns[i].Type)}\n");
            foreach (RowDescription row in table.Rows)
                writer.Write($"row\t{row.RowId.ToString(CultureInfo.InvariantCulture)}\t{string.Join('\t', row.Cells)}\n");
        }
        foreach (string path in ReopenDifferences)
            writer.Write($"reopen\t{Escape(path)}\n");
        foreach (string[] state in States)
            writer.Write($"state\t{Escape(state[0])}\t{state[1]}\t{state[2]}\n");
        writer.Write("end\n");
    }

    /// <summary>The description in its text form.</summary>
    public override string ToString()
    {
        using var writer = new StringWriter(CultureInfo.InvariantCulture);
        Write(writer);
        return writer.ToString();
    }

    /// <summary>Reads a description written by <see cref="Write"/>.</summary>
    public static FileDescription Parse(string text)
    {
        string[] lines = text.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n');
        if (lines.Length == 0 || lines[0] != Header)
            throw new InvalidDataException("Not a file description: " + (lines.Length > 0 ? lines[0] : ""));
        var description = new FileDescription();
        TableDescription? table = null;
        foreach (string line in lines.Skip(1))
        {
            if (line.Length == 0) continue;
            string[] fields = line.Split('\t');
            switch (fields[0])
            {
                case "file":
                    description.Name = Unescape(fields[1]);
                    break;
                case "pragma":
                    description.Pragmas.Add(new KeyValuePair<string, string>(fields[1], Unescape(fields[2])));
                    break;
                case "object":
                    description.Objects.Add(fields.Skip(1).Select(Unescape).ToArray());
                    break;
                case "table":
                    table = new TableDescription { Name = Unescape(fields[1]), SqlHash = Field(fields, "sql") };
                    description.Tables.Add(table);
                    break;
                case "column":
                    table!.Columns.Add(new ColumnDescription(Unescape(fields[2]), Unescape(fields[3])));
                    break;
                case "row":
                    table!.Rows.Add(new RowDescription(long.Parse(fields[1], CultureInfo.InvariantCulture), fields.Skip(2).ToArray()));
                    break;
                case "reopen":
                    description.ReopenDifferences.Add(Unescape(fields[1]));
                    break;
                case "state":
                    description.States.Add(new[] { Unescape(fields[1]), fields[2], fields[3] });
                    break;
                case "end":
                    return description;
                default:
                    throw new InvalidDataException("Unknown line in a file description: " + line);
            }
        }
        throw new InvalidDataException("The file description has no end line (truncated?).");
    }

    private static string Field(string[] fields, string key) =>
        fields.Skip(1).Select(f => f.Split('=', 2)).Where(p => p.Length == 2 && p[0] == key).Select(p => p[1]).FirstOrDefault() ?? "";
}

/// <summary>A table: its columns and rows (in rowid order).</summary>
public sealed class TableDescription
{
    /// <summary>The table's name.</summary>
    public string Name { get; set; } = "";

    /// <summary>The hash of the table's CREATE statement.</summary>
    public string SqlHash { get; set; } = "";

    /// <summary>The columns, in order.</summary>
    public List<ColumnDescription> Columns { get; } = new();

    /// <summary>The rows, in rowid order.</summary>
    public List<RowDescription> Rows { get; } = new();

    /// <summary>
    /// A fingerprint of the table: its columns and cells. Volatile cells count as "~", so the
    /// fingerprint of upstream's output stays the same from run to run.
    /// </summary>
    public string Fingerprint
    {
        get
        {
            var text = new StringBuilder();
            text.Append(SqlHash).Append('\n');
            foreach (ColumnDescription column in Columns) text.Append(column.Name).Append(':').Append(column.Type).Append('\n');
            foreach (RowDescription row in Rows)
            {
                text.Append(row.RowId.ToString(CultureInfo.InvariantCulture));
                foreach (string cell in row.Cells) text.Append('\t').Append(cell.StartsWith('~') ? "~" : cell);
                text.Append('\n');
            }
            return FileDescription.Sha(text.ToString());
        }
    }
}

/// <summary>A column's name and declared type.</summary>
public sealed record ColumnDescription(string Name, string Type);

/// <summary>A row's rowid and its cells' tokens, in column order.</summary>
public sealed record RowDescription(long RowId, string[] Cells);
