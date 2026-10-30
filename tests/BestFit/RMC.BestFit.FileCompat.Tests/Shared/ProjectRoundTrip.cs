#nullable enable
using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using FrameworkInterfaces;
using RMC.BestFit.UI;

namespace BestFitFileCompat;

/// <summary>
/// Opens and saves an example project the way RMC-BestFit does, on a copy, and describes the result.
/// </summary>
/// <remarks>
/// XGoodFit addition (docs/migration-plan.md, Phase 6), compiled both into the port's file-compatibility
/// tests and into tools/BestFitFileCompat.Wpf, so upstream's WPF RMC.BestFit.UI and the port run exactly
/// the same steps:
/// <list type="number">
/// <item>Copy the example into a new temporary folder, under its own file name: the project's name is its
/// file name, and upstream renames the file on save when they differ.</item>
/// <item>Open it as File &gt; Open does: <see cref="BestFitProject.Open"/> opens every element collection,
/// which opens each element from the file.</item>
/// <item>Save every element, then the project, as File &gt; Save does once the elements have changed
/// (<see cref="BestFitProject.Save"/> saves the project row and then each collection). Saving each
/// element makes upstream rewrite all of its cells, plots included, even when nothing changed.</item>
/// <item>Close it, and release SQLite's pooled connections, which closes the write-ahead log.</item>
/// </list>
/// Upstream's own end-to-end test (PointProcessProjectCompatibilityTests) opens a project the same way, on
/// an instance of its own rather than the application's singleton. Formatting follows the current
/// culture, as upstream's dates do (FrameworkInterfaces' Tools.DateToUniversalString), so callers pin
/// it with <see cref="UseCulture"/>.
/// </remarks>
public static class ProjectRoundTrip
{
    /// <summary>The culture the examples were saved in, and the suite runs in.</summary>
    public const string CultureName = "en-US";

    /// <summary>Sets the culture every thread uses: en-US unless another is named (see the class remarks).</summary>
    public static void UseCulture(string name = CultureName)
    {
        var culture = new CultureInfo(name);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    /// <summary>Copies a project into a new folder under <paramref name="workRoot"/>, keeping its file name.</summary>
    public static string CopyToWorkFolder(string example, string workRoot)
    {
        string folder = Path.Combine(workRoot, Guid.NewGuid().ToString("N").Substring(0, 12));
        Directory.CreateDirectory(folder);
        string copy = Path.Combine(folder, Path.GetFileName(example));
        File.Copy(example, copy);
        return copy;
    }

    /// <summary>Opens a project file, on an instance of its own (not the application's singleton).</summary>
    public static BestFitProject Open(string file)
    {
        var project = (BestFitProject)Activator.CreateInstance(typeof(BestFitProject), nonPublic: true)!;
        project.FullFileName = file;
        project.Open();
        return project;
    }

    /// <summary>Every element of the project, collection by collection, in order.</summary>
    public static IEnumerable<IElement> Elements(BestFitProject project) =>
        (project.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(ElementsOf);

    private static IEnumerable<IElement> ElementsOf(IElementCollection collection) =>
        collection.Concat((collection.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(ElementsOf));

    /// <summary>Saves every element, then the project.</summary>
    public static void SaveAll(BestFitProject project)
    {
        foreach (IElement element in Elements(project).ToList())
            element.Save();
        project.Save();
    }

    /// <summary>Closes the project and releases SQLite's pooled connections to its file.</summary>
    public static void Close(BestFitProject project)
    {
        project.Close();
        SQLiteConnection.ClearAllPools();
    }

    /// <summary>
    /// Opens and saves an example on a copy and describes the saved file, with a hash of each element's
    /// in-memory state after opening (<see cref="FileDescription.States"/>). Then opens the saved file again
    /// and records which parts of the in-memory state differ from the first open
    /// (<see cref="FileDescription.ReopenDifferences"/>). Run it on the thread that owns the UI objects
    /// (the STA thread for WPF, the UI thread for Avalonia).
    /// </summary>
    /// <param name="name">The example's name (see <see cref="Examples"/>).</param>
    /// <param name="example">The example project, which is copied, never opened.</param>
    /// <param name="workRoot">The folder to make the copy in.</param>
    /// <param name="store">If given, receives the content of every hashed cell (<see cref="FileDescription.Describe"/>).</param>
    /// <param name="reopenDetails">If given, receives each reopen difference with its values before and after.</param>
    /// <param name="opened">If given, receives the state the example opened to.</param>
    public static FileDescription SaveAndDescribe(string name, string example, string workRoot,
        Action<string, byte[]>? store = null, List<string>? reopenDetails = null, Action<StateSnapshot>? opened = null)
    {
        string copy = CopyToWorkFolder(example, workRoot);
        string folder = Path.GetDirectoryName(copy)!;
        try
        {
            BestFitProject project = Open(copy);
            StateSnapshot openedState = StateSnapshot.Take(project, folder);
            opened?.Invoke(openedState);
            SaveAll(project);
            Close(project);

            FileDescription description = FileDescription.Describe(copy, name, folder, store);
            description.States.AddRange(openedState.GroupHashes().Select(group => new[] { group.Key, group.Value.Full, group.Value.WithoutNumbers }));

            BestFitProject reopened = Open(copy);
            StateSnapshot reopenedState = StateSnapshot.Take(reopened, folder);
            Close(reopened);
            description.ReopenDifferences.AddRange(openedState.Differences(reopenedState));
            reopenDetails?.AddRange(description.ReopenDifferences.Select(path =>
                $"{path}: {openedState.Values.GetValueOrDefault(path, "(none)")} -> {reopenedState.Values.GetValueOrDefault(path, "(none)")}"));
            return description;
        }
        finally
        {
            DeleteWorkFolder(copy);
        }
    }

    /// <summary>Deletes a work folder made by <see cref="CopyToWorkFolder"/>, retrying while SQLite lets go of it.</summary>
    public static void DeleteWorkFolder(string copy)
    {
        string? folder = Path.GetDirectoryName(copy);
        if (folder == null || !Directory.Exists(folder)) return;
        SQLiteConnection.ClearAllPools();
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
                return;
            }
            catch (IOException) when (attempt < 20)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(100);
            }
            catch (UnauthorizedAccessException) when (attempt < 20)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(100);
            }
        }
    }
}
