#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace BestFitFileCompat;

/// <summary>How two descriptions of the same project differ at one place.</summary>
public enum DifferenceKind
{
    /// <summary>Upstream itself wrote different values when it saved the same file twice; not compared.</summary>
    Volatile,

    /// <summary>The same text with different line ends: XElement.ToString() writes Environment.NewLine.</summary>
    LineEnds,

    /// <summary>The same date with a narrow no-break space before AM or PM, as ICU's en-US formats have.</summary>
    NarrowNoBreakSpace,

    /// <summary>The same path with the platform's directory separator.</summary>
    PathSeparator,

    /// <summary>The same content compressed to different bytes (the runtime's zlib).</summary>
    Compression,

    /// <summary>In-memory state that differs only in numbers, as a different maths library may round computed statistics.</summary>
    Numbers,

    /// <summary>Anything else: the port wrote something upstream doesn't.</summary>
    Mismatch,
}

/// <summary>One difference between an expected and an actual description.</summary>
public sealed record Difference(DifferenceKind Kind, string Table, string Column, long RowId, string Expected, string Actual, string Detail)
{
    /// <summary>Where the difference is, for messages.</summary>
    public string Location => Table.Length == 0 ? "(file)" : Column.Length == 0 ? Table : $"{Table}.{Column} (rowid {RowId.ToString(CultureInfo.InvariantCulture)})";

    /// <inheritdoc/>
    public override string ToString() =>
        $"{Kind}: {Location}: expected {Expected}, actual {Actual}" + (Detail.Length > 0 ? $" ({Detail})" : "");
}

/// <summary>
/// Compares a description of what upstream saved with one of what the port saved, cell by cell.
/// </summary>
/// <remarks>
/// XGoodFit addition (docs/migration-plan.md, Phase 6), shared with tools/BestFitFileCompat.Wpf. Text and
/// numbers compare exactly: equal tokens mean equal text, or equal bits for a REAL. A difference that
/// disappears in <see cref="FileDescription.Canonical">canonical form</see> is reported as the platform
/// difference that explains it; whether that's acceptable is the caller's decision (on Windows it isn't).
/// </remarks>
public static class DescriptionComparison
{
    /// <summary>Every difference between two descriptions of the same project.</summary>
    public static List<Difference> Compare(FileDescription expected, FileDescription actual)
    {
        var differences = new List<Difference>();
        void File(string what, string e, string a) => differences.Add(new Difference(DifferenceKind.Mismatch, "", "", 0, e, a, what));

        foreach (string key in expected.Pragmas.Select(p => p.Key).Union(actual.Pragmas.Select(p => p.Key)))
        {
            string e = expected.Pragmas.FirstOrDefault(p => p.Key == key).Value ?? "(none)";
            string a = actual.Pragmas.FirstOrDefault(p => p.Key == key).Value ?? "(none)";
            if (e != a) File("PRAGMA " + key, e, a);
        }

        string expectedObjects = string.Join("; ", expected.Objects.Select(o => string.Join(' ', o)));
        string actualObjects = string.Join("; ", actual.Objects.Select(o => string.Join(' ', o)));
        if (expectedObjects != actualObjects) File("indexes, views and triggers", expectedObjects, actualObjects);

        var actualTables = actual.Tables.ToDictionary(t => t.Name, StringComparer.Ordinal);
        foreach (TableDescription table in expected.Tables)
        {
            if (!actualTables.Remove(table.Name, out TableDescription? other))
            {
                differences.Add(new Difference(DifferenceKind.Mismatch, table.Name, "", 0, $"{table.Rows.Count} rows", "no table", "table missing"));
                continue;
            }
            CompareTable(table, other, differences);
        }
        foreach (TableDescription extra in actualTables.Values)
            differences.Add(new Difference(DifferenceKind.Mismatch, extra.Name, "", 0, "no table", $"{extra.Rows.Count} rows", "extra table"));
        return differences;
    }

    private static void CompareTable(TableDescription expected, TableDescription actual, List<Difference> differences)
    {
        string expectedColumns = string.Join(", ", expected.Columns.Select(c => $"{c.Name} {c.Type}"));
        string actualColumns = string.Join(", ", actual.Columns.Select(c => $"{c.Name} {c.Type}"));
        if (expectedColumns != actualColumns)
        {
            differences.Add(new Difference(DifferenceKind.Mismatch, expected.Name, "", 0, expectedColumns, actualColumns, "columns differ"));
            return;
        }
        if (expected.SqlHash != actual.SqlHash)
            differences.Add(new Difference(DifferenceKind.Mismatch, expected.Name, "", 0, expected.SqlHash, actual.SqlHash, "CREATE TABLE statement differs"));
        if (expected.Rows.Count != actual.Rows.Count)
            differences.Add(new Difference(DifferenceKind.Mismatch, expected.Name, "", 0, $"{expected.Rows.Count} rows", $"{actual.Rows.Count} rows", "row count differs"));

        for (int r = 0; r < Math.Min(expected.Rows.Count, actual.Rows.Count); r++)
        {
            RowDescription e = expected.Rows[r], a = actual.Rows[r];
            if (e.RowId != a.RowId)
                differences.Add(new Difference(DifferenceKind.Mismatch, expected.Name, "rowid", e.RowId, e.RowId.ToString(CultureInfo.InvariantCulture), a.RowId.ToString(CultureInfo.InvariantCulture), $"row {r}"));
            for (int c = 0; c < expected.Columns.Count; c++)
            {
                Difference? difference = CompareCell(e.Cells[c], a.Cells[c]) is (DifferenceKind kind, string detail)
                    ? new Difference(kind, expected.Name, expected.Columns[c].Name, e.RowId, e.Cells[c], a.Cells[c], detail)
                    : null;
                if (difference != null) differences.Add(difference);
            }
        }
    }

    /// <summary>How two cell tokens differ, or null if they're the same.</summary>
    public static (DifferenceKind Kind, string Detail)? CompareCell(string expected, string actual)
    {
        if (expected.StartsWith('~'))
            return expected.Substring(1) == actual ? null : (DifferenceKind.Volatile, "upstream writes this cell differently each time");
        if (expected == actual) return null;

        Token e = Token.Parse(expected), a = Token.Parse(actual);
        if (e.Kind != a.Kind)
            return (DifferenceKind.Mismatch, "different storage class");

        if (e.Kind == 'r')
            return (DifferenceKind.Mismatch, RealDetail(e.Value, a.Value));

        if (e.Kind is 't' or 'p')
        {
            if (e.Canonical == a.Canonical && e.Exact != a.Exact)
                return (PlatformKind(e.Flags + a.Flags), "same text in canonical form");
            return (DifferenceKind.Mismatch, e.Kind == 't' && e.Value != null && a.Value != null ? "" : $"{e.Length} vs {a.Length} UTF-8 bytes");
        }

        if (e.Kind == 'b')
        {
            if (e.Inflated != null && e.Inflated == a.Inflated)
                return (DifferenceKind.Compression, "same content, different compressed bytes");
            if (e.InflatedCanonical != null && e.InflatedCanonical == a.InflatedCanonical)
                return (PlatformKind(e.Flags + a.Flags), "same compressed content in canonical form");
            return (DifferenceKind.Mismatch, $"{e.Length} vs {a.Length} bytes" + (e.Inflated != null ? $", inflated {e.InflatedLength} vs {a.InflatedLength}" : ""));
        }
        return (DifferenceKind.Mismatch, "");
    }

    private static DifferenceKind PlatformKind(string flags) =>
        flags.Contains('r') ? DifferenceKind.LineEnds
        : flags.Contains('s') ? DifferenceKind.NarrowNoBreakSpace
        : flags.Contains('b') ? DifferenceKind.PathSeparator
        : DifferenceKind.Mismatch;

    private static string RealDetail(string? expected, string? actual)
    {
        if (!double.TryParse(expected, NumberStyles.Float, CultureInfo.InvariantCulture, out double e) ||
            !double.TryParse(actual, NumberStyles.Float, CultureInfo.InvariantCulture, out double a))
            return "";
        long ulps = Math.Abs(BitConverter.DoubleToInt64Bits(e) - BitConverter.DoubleToInt64Bits(a));
        return $"{ulps.ToString(CultureInfo.InvariantCulture)} ulp apart";
    }

    /// <summary>
    /// The groups (the project, its collections and elements) whose in-memory state after opening differs
    /// (<see cref="FileDescription.States"/>): <see cref="DifferenceKind.Mismatch"/> when more than numbers
    /// differ, <see cref="DifferenceKind.Numbers"/> when only numbers do, and <see cref="DifferenceKind.Volatile"/>
    /// for groups upstream itself opened differently from run to run.
    /// </summary>
    public static List<(string Group, DifferenceKind Kind)> CompareStates(FileDescription expected, FileDescription actual) =>
        CompareStates(expected.States, actual.States);

    /// <summary>
    /// The groups whose in-memory state after the saved file was opened again differs
    /// (<see cref="FileDescription.StatesAfterReopening"/>), as <see cref="CompareStates(FileDescription, FileDescription)"/>.
    /// </summary>
    public static List<(string Group, DifferenceKind Kind)> CompareReopenedStates(FileDescription expected, FileDescription actual) =>
        CompareStates(expected.StatesAfterReopening(), actual.StatesAfterReopening());

    /// <summary>The groups whose hashes differ between two lists of group states (group, hash, hash without numbers).</summary>
    public static List<(string Group, DifferenceKind Kind)> CompareStates(IEnumerable<string[]> expectedStates, IEnumerable<string[]> actualStates)
    {
        var differences = new List<(string, DifferenceKind)>();
        var actualByGroup = actualStates.ToDictionary(s => s[0], StringComparer.Ordinal);
        foreach (string[] state in expectedStates)
        {
            if (!actualByGroup.Remove(state[0], out string[]? other))
                differences.Add((state[0], DifferenceKind.Mismatch));
            else if (state[1] == "~" && state[2] == "~")
                differences.Add((state[0], DifferenceKind.Volatile));
            else if (state[1] != other[1])
                differences.Add((state[0], state[2] == other[2] ? DifferenceKind.Numbers : state[2] == "~" ? DifferenceKind.Volatile : DifferenceKind.Mismatch));
        }
        differences.AddRange(actualByGroup.Keys.Select(group => (group, DifferenceKind.Mismatch)));
        return differences;
    }

    /// <summary>
    /// Marks as volatile (<c>~</c>) the cells of <paramref name="description"/> that another save of the
    /// same file, <paramref name="other"/>, wrote differently. Returns the cells newly marked.
    /// </summary>
    /// <exception cref="InvalidOperationException">The two saves differ in more than cell values.</exception>
    public static List<Difference> MarkVolatile(FileDescription description, FileDescription other)
    {
        var marked = new List<Difference>();
        if (description.Tables.Count != other.Tables.Count)
            throw new InvalidOperationException($"{description.Name}: two saves wrote different tables.");
        for (int t = 0; t < description.Tables.Count; t++)
        {
            TableDescription table = description.Tables[t], otherTable = other.Tables[t];
            if (table.Name != otherTable.Name || table.Rows.Count != otherTable.Rows.Count || table.Columns.Count != otherTable.Columns.Count)
                throw new InvalidOperationException($"{description.Name}: two saves wrote table {table.Name} differently.");
            for (int r = 0; r < table.Rows.Count; r++)
            {
                string[] cells = table.Rows[r].Cells, otherCells = otherTable.Rows[r].Cells;
                for (int c = 0; c < cells.Length; c++)
                {
                    string value = cells[c].TrimStart('~');
                    if (value == otherCells[c] || cells[c].StartsWith('~')) continue;
                    marked.Add(new Difference(DifferenceKind.Volatile, table.Name, table.Columns[c].Name, table.Rows[r].RowId, cells[c], otherCells[c], ""));
                    cells[c] = "~" + cells[c];
                }
            }
        }

        // The same for the in-memory state each run opened the file to.
        var otherStates = other.States.ToDictionary(s => s[0], StringComparer.Ordinal);
        foreach (string[] state in description.States)
        {
            if (!otherStates.TryGetValue(state[0], out string[]? otherState))
                throw new InvalidOperationException($"{description.Name}: two opens gave different elements ({state[0]}).");
            for (int i = 1; i <= 2; i++)
            {
                if (state[i] == "~" || state[i] == otherState[i]) continue;
                marked.Add(new Difference(DifferenceKind.Volatile, "(state)", state[0], 0, state[i], otherState[i], i == 1 ? "values" : "values without numbers"));
                state[i] = "~";
            }
        }

        // And for the state the saved file opened to.
        var reopened = description.StatesAfterReopening().ToDictionary(s => s[0], StringComparer.Ordinal);
        var otherReopened = other.StatesAfterReopening().ToDictionary(s => s[0], StringComparer.Ordinal);
        foreach (string group in reopened.Keys.Union(otherReopened.Keys).OrderBy(g => g, StringComparer.Ordinal))
        {
            string[] state = reopened.GetValueOrDefault(group) ?? new[] { group, "-", "-" };
            string[] otherState = otherReopened.GetValueOrDefault(group) ?? new[] { group, "-", "-" };
            if (Enumerable.Range(1, 2).All(i => state[i] == "~" || state[i] == otherState[i])) continue;
            string[] marking = description.ReopenedStates.FirstOrDefault(s => s[0] == group) ?? AddReopened(description, state);
            for (int i = 1; i <= 2; i++)
            {
                if (marking[i] == "~" || marking[i] == otherState[i]) continue;
                marked.Add(new Difference(DifferenceKind.Volatile, "(reopened state)", group, 0, marking[i], otherState[i], i == 1 ? "values" : "values without numbers"));
                marking[i] = "~";
            }
        }
        return marked;
    }

    private static string[] AddReopened(FileDescription description, string[] state)
    {
        string[] copy = (string[])state.Clone();
        description.ReopenedStates.Add(copy);
        return copy;
    }

    /// <summary>A short summary of differences, grouped by kind (mismatches first) and column.</summary>
    public static string Summarize(IEnumerable<Difference> differences, int examplesPerGroup = 3)
    {
        var text = new StringBuilder();
        foreach (var group in differences.GroupBy(d => (d.Kind, d.Table, d.Column)).OrderByDescending(g => g.Key.Kind).ThenBy(g => g.Key.Table, StringComparer.Ordinal).ThenBy(g => g.Key.Column, StringComparer.Ordinal))
        {
            string where = group.Key.Table.Length == 0 ? "(file)" : group.Key.Column.Length == 0 ? group.Key.Table : $"{group.Key.Table}.{group.Key.Column}";
            text.Append(CultureInfo.InvariantCulture, $"  {group.Key.Kind}: {where}: {group.Count()} cell(s)\n");
            foreach (Difference d in group.Take(examplesPerGroup))
                text.Append(CultureInfo.InvariantCulture, $"    rowid {d.RowId}: expected {Shorten(d.Expected)}, actual {Shorten(d.Actual)}{(d.Detail.Length > 0 ? " (" + d.Detail + ")" : "")}\n");
        }
        return text.ToString();
    }

    private static string Shorten(string token) => token.Length <= 160 ? token : token.Substring(0, 157) + "...";

    /// <summary>A parsed cell token (see <see cref="FileDescription"/>).</summary>
    private sealed class Token
    {
        public char Kind;
        public string? Value;
        public string Exact = "";
        public string Canonical = "";
        public string Flags = "";
        public long Length;
        public string? Inflated;
        public string? InflatedCanonical;
        public long InflatedLength;

        public static Token Parse(string token)
        {
            var parsed = new Token { Kind = token[0] };
            if (token == "n") return parsed;
            if (token.Length > 1 && token[1] == ':')
            {
                string value = token.Substring(2);
                parsed.Value = value;
                if (parsed.Kind is 't' or 'p')
                {
                    string text = FileDescription.Unescape(value);
                    parsed.Exact = FileDescription.Sha(text);
                    parsed.Canonical = FileDescription.Sha(FileDescription.Canonical(text, parsed.Kind == 'p'));
                    parsed.Flags = FileDescription.CanonicalFlags(text, parsed.Kind == 'p');
                    parsed.Length = Encoding.UTF8.GetByteCount(text);
                }
                return parsed;
            }

            // t#len:sha[/canonical:flags], p#..., b#len:sha[>z#len:sha[/canonical:flags]]
            string[] parts = token.Substring(2).Split('>');
            (parsed.Length, parsed.Exact, string? canonical, string flags) = ParseHashed(parts[0]);
            parsed.Canonical = canonical ?? parsed.Exact;
            parsed.Flags = flags;
            if (parts.Length > 1 && parts[1].StartsWith("z#", StringComparison.Ordinal))
            {
                (parsed.InflatedLength, parsed.Inflated, string? inflatedCanonical, string inflatedFlags) = ParseHashed(parts[1].Substring(2));
                parsed.InflatedCanonical = inflatedCanonical ?? parsed.Inflated;
                parsed.Flags += inflatedFlags;
            }
            return parsed;
        }

        private static (long Length, string Sha, string? Canonical, string Flags) ParseHashed(string text)
        {
            string[] hashes = text.Split('/');
            string[] lengthAndSha = hashes[0].Split(':');
            string? canonical = null, flags = "";
            if (hashes.Length > 1)
            {
                string[] canonicalAndFlags = hashes[1].Split(':');
                canonical = canonicalAndFlags[0];
                flags = canonicalAndFlags.Length > 1 ? canonicalAndFlags[1] : "";
            }
            return (long.Parse(lengthAndSha[0], CultureInfo.InvariantCulture), lengthAndSha[1], canonical, flags);
        }
    }
}
