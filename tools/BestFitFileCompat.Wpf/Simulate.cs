using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;

namespace BestFitFileCompat;

/// <summary>
/// Makes, on Windows, the files the port saves on another OS, so that <see cref="OpenSaved"/> can be tried
/// on them without that OS: each file the port saved on Windows, rewritten cell by cell as that OS's run of
/// the suite described its own save (artifacts/filecompat/&lt;rid&gt;/ of a CI run).
/// </summary>
/// <remarks>
/// Only the platform differences can be rewritten, and only they are expected: XML with "\n" line ends,
/// dates with ICU's narrow no-break space before AM or PM, and a FullFileName in a Unix folder
/// (<c>/tmp/xgoodfit-filecompat-simulated/…</c>). Compressed cells are compressed again as
/// Numerics' <c>Tools.Compress</c> does (raw Deflate, optimal), so their bytes may still differ from that
/// OS's. Afterwards each file is described again and compared with the target's description: anything
/// left apart from compressed bytes means the simulation failed. The files the port changed before saving
/// (<c>changed/</c>) aren't simulated: their content depends on what was computed where.
/// </remarks>
internal static class Simulate
{
    private static readonly string NarrowNoBreakSpace = ((char)0x202F).ToString();

    /// <summary>Runs the command; returns 0 when every file now matches its target description.</summary>
    public static int Run(string windowsFolder, string targetFolder, string outputFolder, string? only)
    {
        int failed = 0, simulated = 0;
        foreach ((string name, string path) in Examples.Find(Path.GetFullPath(windowsFolder))
            .Where(file => !file.Name.StartsWith(OpenSaved.ChangedFolder, StringComparison.Ordinal))
            .Where(file => only == null || file.Name.Contains(only, StringComparison.OrdinalIgnoreCase)))
        {
            string targetPath = Examples.DescriptionPath(targetFolder, name);
            if (!File.Exists(targetPath))
            {
                Console.WriteLine($"{name}: no target description, skipped");
                continue;
            }
            FileDescription target = Examples.Read(targetPath);
            string output = Path.GetFullPath(Path.Combine(outputFolder, name));
            ProjectRoundTrip.Keep(path, output);

            string? stored = OpenSaved.StoredFullFileName(output);
            string? savedIn = stored?.Substring(0, Math.Max(0, stored.LastIndexOfAny(new[] { '/', '\\' })));
            string unixFolder = "/tmp/xgoodfit-filecompat-simulated/" + Guid.NewGuid().ToString("N").Substring(0, 12);
            FileDescription source = FileDescription.Describe(output, name, savedIn);

            var problems = new List<string>();
            int rewritten = 0;
            string connectionString = new SQLiteConnectionStringBuilder { DataSource = output, Pooling = false }.ConnectionString;
            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.Open();
                foreach (Difference difference in DescriptionComparison.Compare(source, target))
                {
                    if (difference.Kind == DifferenceKind.Compression) continue;
                    object? value = difference.Kind is DifferenceKind.LineEnds or DifferenceKind.NarrowNoBreakSpace or DifferenceKind.PathSeparator
                        ? Rewrite(connection, difference, unixFolder)
                        : null;
                    if (value == null)
                    {
                        problems.Add($"can't simulate {difference}");
                        continue;
                    }
                    using var update = new SQLiteCommand($"UPDATE {Quote(difference.Table)} SET {Quote(difference.Column)} = @value WHERE rowid = @rowid", connection);
                    update.Parameters.AddWithValue("@value", value);
                    update.Parameters.AddWithValue("@rowid", difference.RowId);
                    update.ExecuteNonQuery();
                    rewritten++;
                }
            }
            SQLiteConnection.ClearAllPools();

            List<Difference> left = DescriptionComparison.Compare(target, FileDescription.Describe(output, name, unixFolder));
            problems.AddRange(left.Where(d => d.Kind != DifferenceKind.Compression).Select(d => "still differs: " + d));
            int compression = left.Count(d => d.Kind == DifferenceKind.Compression);
            simulated++;
            if (problems.Count > 0) failed++;
            Console.WriteLine($"{name}: {rewritten} cell(s) rewritten; " +
                (problems.Count == 0 ? "matches the target description" : $"{problems.Count} problem(s)") +
                (compression > 0 ? $" ({compression} compressed cell(s) hold the same content in other bytes)" : ""));
            foreach (string problem in problems.Take(10)) Console.WriteLine("  " + problem);
        }
        Console.WriteLine($"# {simulated} file(s) simulated in {Path.GetFullPath(outputFolder)}, {failed} with problems");
        return failed == 0 && simulated > 0 ? 0 : 1;
    }

    /// <summary>The cell's value as the target OS wrote it, or null if this can't make it.</summary>
    private static object? Rewrite(SQLiteConnection connection, Difference difference, string unixFolder)
    {
        string token = difference.Actual;
        if (token.StartsWith("t:", StringComparison.Ordinal))
            return FileDescription.Unescape(token.Substring(2));
        if (token.StartsWith("p:", StringComparison.Ordinal))
            return FileDescription.Unescape(token.Substring(2)).Replace("{dir}", unixFolder, StringComparison.Ordinal);

        using var select = new SQLiteCommand($"SELECT {Quote(difference.Column)} FROM {Quote(difference.Table)} WHERE rowid = @rowid", connection);
        select.Parameters.AddWithValue("@rowid", difference.RowId);
        object? current = select.ExecuteScalar();
        if (token.StartsWith("t#", StringComparison.Ordinal) && current is string text)
        {
            string wanted = HashOf(token.Substring(2));
            return Candidates(text).FirstOrDefault(candidate => FileDescription.Sha(candidate) == wanted);
        }
        if (token.StartsWith("b#", StringComparison.Ordinal) && current is byte[] bytes && token.Contains(">z#", StringComparison.Ordinal))
        {
            string wanted = HashOf(token.Substring(token.IndexOf(">z#", StringComparison.Ordinal) + 3));
            string inflated = Encoding.UTF8.GetString(Inflate(bytes));
            string? content = Candidates(inflated).FirstOrDefault(candidate => FileDescription.Sha(candidate) == wanted);
            return content == null ? null : Compress(Encoding.UTF8.GetBytes(content));
        }
        return null;
    }

    // The text with "\n" line ends, with narrow no-break spaces before AM and PM, or both.
    private static IEnumerable<string> Candidates(string text)
    {
        string lineEnds = text.Replace("\r\n", "\n", StringComparison.Ordinal);
        yield return lineEnds;
        yield return WithNarrowNoBreakSpaces(text);
        yield return WithNarrowNoBreakSpaces(lineEnds);
    }

    private static string WithNarrowNoBreakSpaces(string text) =>
        System.Text.RegularExpressions.Regex.Replace(text, "(?<=[0-9]) (?=[AP]M)", NarrowNoBreakSpace);

    // "len:sha[/canonical:flags]" -> sha
    private static string HashOf(string lengthAndHashes) => lengthAndHashes.Split('/')[0].Split(':')[1];

    private static byte[] Inflate(byte[] bytes)
    {
        using var input = new MemoryStream(bytes);
        using var output = new MemoryStream();
        using (var inflater = new DeflateStream(input, CompressionMode.Decompress))
            inflater.CopyTo(output);
        return output.ToArray();
    }

    // As Numerics' Tools.Compress (upstream/Numerics/Numerics/Utilities/Tools.cs), which RMC-BestFit uses.
    private static byte[] Compress(byte[] data)
    {
        var output = new MemoryStream();
        using (var deflater = new DeflateStream(output, CompressionLevel.Optimal))
            deflater.Write(data, 0, data.Length);
        return output.ToArray();
    }

    private static string Quote(string identifier) => "\"" + identifier.Replace("\"", "\"\"", StringComparison.Ordinal) + "\"";
}
