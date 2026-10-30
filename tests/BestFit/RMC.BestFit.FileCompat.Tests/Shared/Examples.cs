#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace BestFitFileCompat;

/// <summary>
/// The example projects, and where their descriptions are kept.
/// </summary>
/// <remarks>
/// XGoodFit addition (docs/migration-plan.md, Phase 6), shared with tools/BestFitFileCompat.Wpf. An
/// example's name is its path under the examples folder (sample-data/RMC-BestFit/examples), with '/'
/// separators; its description is <c>&lt;name&gt;.txt</c> under a descriptions folder.
/// </remarks>
public static class Examples
{
    /// <summary>The example projects under a folder, by name (ordinal order).</summary>
    public static IReadOnlyList<(string Name, string Path)> Find(string folder) =>
        Directory.GetFiles(folder, "*.bestfit", SearchOption.AllDirectories)
            .Select(path => (Name: System.IO.Path.GetRelativePath(folder, path).Replace('\\', '/'), Path: path))
            .OrderBy(example => example.Name, StringComparer.Ordinal)
            .ToList();

    /// <summary>Where an example's description is kept under a descriptions folder.</summary>
    public static string DescriptionPath(string folder, string name) => System.IO.Path.Combine(folder, name + ".txt");

    /// <summary>The names of the descriptions under a folder, by name (ordinal order).</summary>
    public static IReadOnlyList<string> DescribedNames(string folder) =>
        Directory.GetFiles(folder, "*.bestfit.txt", SearchOption.AllDirectories)
            .Select(path => System.IO.Path.GetRelativePath(folder, path).Replace('\\', '/'))
            .Select(name => name.Substring(0, name.Length - ".txt".Length))
            .OrderBy(name => name, StringComparer.Ordinal)
            .ToList();

    /// <summary>Writes a description, as UTF-8 with "\n" line ends on every OS.</summary>
    public static void Write(FileDescription description, string path)
    {
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, description.ToString(), new UTF8Encoding(false));
    }

    /// <summary>Reads a description.</summary>
    public static FileDescription Read(string path) => FileDescription.Parse(File.ReadAllText(path, Encoding.UTF8));

    /// <summary>A content store: writes each hashed cell's content to <c>&lt;hash&gt;.txt</c> (or <c>.bin</c>) in a folder.</summary>
    public static Action<string, byte[]> ContentStore(string folder)
    {
        Directory.CreateDirectory(folder);
        return (sha, content) =>
        {
            string extension = IsUtf8(content) ? ".txt" : ".bin";
            string path = System.IO.Path.Combine(folder, sha + extension);
            if (!File.Exists(path)) File.WriteAllBytes(path, content);
        };
    }

    private static bool IsUtf8(byte[] content)
    {
        try
        {
            new UTF8Encoding(false, true).GetString(content);
            return true;
        }
        catch (DecoderFallbackException)
        {
            return false;
        }
    }
}
