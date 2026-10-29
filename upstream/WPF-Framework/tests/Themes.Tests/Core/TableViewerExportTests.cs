using System.IO;
using Xunit;

namespace Themes.Tests.Core;

/// <summary>
/// Regression tests for TableViewer export dialog defaults.
/// </summary>
public class TableViewerExportTests
{
    /// <summary>
    /// Verifies the Export Table dialog defaults to comma-delimited output.
    /// </summary>
    [Fact]
    public void ExportTable_DefaultsToCsv()
    {
        string source = File.ReadAllText(ResolveRepoPath("src/DatabaseControls/TableViewer.xaml.cs"));
        string exportTable = ExtractMethod(source, "private void ExportTable()");

        Assert.Contains("string filters = \"comma delimited(*.csv) |*.csv|", exportTable);
        Assert.Contains("new SaveFileDialog { Filter = filters, FilterIndex = 1 }", exportTable);
        Assert.DoesNotContain("FilterIndex = 3", exportTable);
    }

    /// <summary>
    /// Extracts a method body from a C# source file.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="signature">The method signature prefix.</param>
    /// <returns>The method body, including the signature and braces.</returns>
    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, signature + " should exist.");

        int bodyStart = source.IndexOf('{', start);
        Assert.True(bodyStart >= start, signature + " should have a body.");

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException(signature + " body was not closed.");
    }

    /// <summary>
    /// Resolves a repository-relative path from the test output directory.
    /// </summary>
    /// <param name="repoRelativePath">The repository-relative path.</param>
    /// <returns>The absolute path.</returns>
    private static string ResolveRepoPath(string repoRelativePath)
    {
        string current = AppContext.BaseDirectory;
        while (!string.IsNullOrEmpty(current))
        {
            if (File.Exists(Path.Combine(current, "WPF-Framework.sln")))
            {
                return Path.Combine(current, repoRelativePath.Replace('/', Path.DirectorySeparatorChar));
            }

            string? parent = Path.GetDirectoryName(current);
            if (parent == current)
            {
                break;
            }

            current = parent!;
        }

        throw new InvalidOperationException(
            $"Could not locate repository root (looking for WPF-Framework.sln) starting from {AppContext.BaseDirectory}");
    }
}
