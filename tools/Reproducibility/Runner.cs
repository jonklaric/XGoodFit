using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using Numerics.Utilities;
using RMC.BestFit.UI;

namespace Reproducibility;

/// <summary>
/// The run step: opens each example project with the port, reads each estimated analysis's saved results, estimates it
/// again with its saved settings and seed, and reads the new results.
/// </summary>
/// <remarks>
/// tools/BestFitFileCompat.Wpf compiles this file too, to do the same with upstream's own WPF RMC.BestFit.UI: the element
/// layer's API is the same in both, and only the UI thread differs (<see cref="IUiThread"/>).
/// </remarks>
/// <remarks>
/// A project is opened as the file-compatibility suite opens it (its ProjectRoundTrip): a copy in a new temporary folder,
/// under its own file name, on a <see cref="BestFitProject"/> instance of its own, in en-US, the culture the examples
/// were saved in. Every analysis's saved results are read first, right after opening, because estimating an analysis
/// clears the results of those built on it. Then the analyses are estimated again in project order, as the
/// application's Estimate button does (<see cref="IAnalysisElement.RunAsync"/>, or <see cref="FittingAnalysis.RunAsync"/>),
/// on the application's UI thread (the port's: a headless Avalonia session's). An analysis built on others (a composite's components, a bivariate
/// analysis's marginals) is estimated after them, with their new results, as a user would.
/// </remarks>
public static class Runner
{
    /// <summary>The application's name in <see cref="EnvironmentInfo.Application"/>: XGoodFit's.</summary>
    public const string XGoodFit = "XGoodFit";

    /// <summary>The culture the examples were saved in.</summary>
    private const string CultureName = "en-US";

    /// <summary>Lists the analyses of the examples.</summary>
    public static int List(string examples, string[] only, IUiThread ui)
    {
        UseCulture();
        string workRoot = Directory.CreateTempSubdirectory("xgoodfit-repro-").FullName;
        try
        {
            foreach (var (name, path) in FindExamples(examples))
            {
                ui.Run(() =>
                {
                    WithProject(path, workRoot, project =>
                    {
                        foreach (var (collection, element) in Analyses(project).Where(a => Matches(name, a.Element.Name, only)))
                        {
                            Extracted saved = Extract.From(element);
                            Console.WriteLine($"{name} | {collection} | {element.Name} | {element.GetType().Name} | {saved.Method} | " +
                                (IsEstimated(element) ? $"estimated, {saved.Values.Count} values" : "not estimated"));
                        }
                    });
                    return 0;
                });
            }
        }
        finally
        {
            DeleteFolder(workRoot);
        }
        return 0;
    }

    /// <summary>Estimates the examples' analyses again and writes the run file.</summary>
    /// <param name="examples">The examples folder.</param>
    /// <param name="output">The run file to write.</param>
    /// <param name="only">The <c>--only</c> filters.</param>
    /// <param name="limit">How long an analysis's rerun may take before it is cancelled.</param>
    /// <param name="ui">The application's UI thread, where projects are opened and estimated.</param>
    /// <param name="application">The application whose element layer does it (<see cref="EnvironmentInfo.Application"/>).</param>
    public static int Run(string examples, string output, string[] only, TimeSpan limit, IUiThread ui, string application)
    {
        UseCulture();
        var clock = Stopwatch.StartNew();
        EnvironmentInfo environment = EnvironmentInfo.Current(application);
        var records = new List<AnalysisRecord>();
        string workRoot = Directory.CreateTempSubdirectory("xgoodfit-repro-").FullName;
        Console.WriteLine($"# {environment.Application} | {environment.Platform} | {environment.OsDescription} | {environment.Framework} | " +
            $"{environment.ProcessorCount} processors");
        try
        {
            foreach (var (name, path) in FindExamples(examples))
            {
                records.AddRange(ui.Await(() => RunExample(name, path, workRoot, only, limit)));
                // Written after each example, so a run stopped by a time limit keeps what it did.
                new RunFile(RunFile.CurrentFormat, environment, examples, only, clock.Elapsed.TotalSeconds, records).Write(output);
            }
        }
        finally
        {
            DeleteFolder(workRoot);
        }

        new RunFile(RunFile.CurrentFormat, environment, examples, only, clock.Elapsed.TotalSeconds, records).Write(output);
        int failed = records.Count(r => r.Status == AnalysisStatus.Failed);
        Console.WriteLine($"# {records.Count(r => r.Status == AnalysisStatus.Rerun)} rerun, {records.Count(r => r.Status == AnalysisStatus.NotEstimated)} not estimated, " +
            $"{records.Count(r => r.Status == AnalysisStatus.Skipped)} skipped, {failed} failed, in {clock.Elapsed:h\\:mm\\:ss}; written to {output}");
        // Failures are the report's to judge (they make it unexplained): the run itself succeeds.
        return 0;
    }

    private static async Task<List<AnalysisRecord>> RunExample(string name, string path, string workRoot, string[] only, TimeSpan limit)
    {
        var records = new List<AnalysisRecord>();
        string copy = CopyToWorkFolder(path, workRoot);
        BestFitProject project = Open(copy);
        try
        {
            var analyses = Analyses(project).Where(a => Matches(name, a.Element.Name, only)).ToList();
            if (analyses.Count == 0) return records;

            // Every saved result first: a later rerun can change what an earlier-read analysis holds (a component's
            // rerun clears its composite's results, say).
            var saved = analyses.Select(a => (a.Collection, a.Element, Estimated: IsEstimated(a.Element), Saved: Extract.From(a.Element))).ToList();
            // Analyses built on others after them, as a user estimates them (BatchAnalysisRunner orders them the same way):
            // a composite takes its components' samples, a bivariate analysis its marginals' fits, and a coincident
            // frequency analysis the bivariate analysis's sample.
            foreach (var (collection, element, estimated, before) in saved.OrderBy(a => Stage(a.Element)))
            {
                string kind = element.GetType().Name;
                if (!estimated)
                {
                    records.Add(new AnalysisRecord(name, collection, element.Name, kind, before.Method, AnalysisStatus.NotEstimated, null, null, null));
                    Console.WriteLine($"{name} | {element.Name}: not estimated in the project");
                    continue;
                }
                if (Skips.Reason(name, element.Name) is string skip)
                {
                    records.Add(new AnalysisRecord(name, collection, element.Name, kind, before.Method, AnalysisStatus.Skipped, skip, null,
                        Values(before, null)));
                    Console.WriteLine($"{name} | {element.Name}: skipped, {skip}");
                    continue;
                }

                var clock = Stopwatch.StartNew();
                string? problem = await Estimate(element, limit);
                double seconds = clock.Elapsed.TotalSeconds;
                if (problem == Timeout)
                {
                    string reason = $"longer than {limit.TotalMinutes:0.#} minutes on this machine (cancelled)";
                    records.Add(new AnalysisRecord(name, collection, element.Name, kind, before.Method, AnalysisStatus.Skipped, reason, seconds, Values(before, null)));
                    Console.WriteLine($"{name} | {element.Name}: skipped, {reason}");
                    continue;
                }
                if (problem == null && !IsEstimated(element)) problem = "the rerun ended unestimated";
                Extracted after = Extract.From(element);
                records.Add(new AnalysisRecord(name, collection, element.Name, kind, before.Method,
                    problem == null ? AnalysisStatus.Rerun : AnalysisStatus.Failed, problem, seconds, Values(before, problem == null ? after : null)));
                Console.WriteLine($"{name} | {element.Name}: {(problem ?? "rerun")} in {seconds.ToString("0.0", CultureInfo.InvariantCulture)} s");
            }
        }
        finally
        {
            Close(project);
            DeleteFolder(Path.GetDirectoryName(copy)!);
        }
        return records;
    }

    private const string Timeout = "timeout";

    // Runs an analysis's Estimate, cancelling it after the limit; returns null, Timeout, or what went wrong.
    private static async Task<string?> Estimate(IElement element, TimeSpan limit)
    {
        Task run;
        try
        {
            run = element switch
            {
                IAnalysisElement analysis => analysis.RunAsync(new SafeProgressReporter()),
                FittingAnalysis fit => fit.RunAsync(new SafeProgressReporter()),
                _ => throw new InvalidOperationException($"{element.GetType().Name} can't be estimated"),
            };
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }

        if (await Task.WhenAny(run, Task.Delay(limit)) != run)
        {
            if (element is IAnalysisElement analysis) analysis.CancelAnalysis();
            else if (element is FittingAnalysis fit) fit.CancelAnalysis();
            try
            {
                await run;
            }
            catch (Exception)
            {
                // A cancelled run may throw; it's listed as skipped either way.
            }
            return Timeout;
        }

        try
        {
            await run;
            return null;
        }
        catch (Exception ex)
        {
            return $"{ex.GetType().Name}: {ex.Message}";
        }
    }

    private static List<ResultValue> Values(Extracted saved, Extracted? rerun)
    {
        var values = new List<ResultValue>();
        var rerunByName = new Dictionary<string, Value>(StringComparer.Ordinal);
        foreach (Value value in rerun?.Values ?? new()) rerunByName.TryAdd(value.Name, value);
        var savedNames = new HashSet<string>(StringComparer.Ordinal);
        foreach (Value value in saved.Values)
        {
            if (!savedNames.Add(value.Name)) continue;
            Value? after = rerunByName.GetValueOrDefault(value.Name);
            values.Add(new ResultValue(value.Name, value.Role, value.Number, after?.Number, Larger(value.Scale, after?.Scale)));
        }
        foreach (Value value in rerun?.Values ?? new())
            if (savedNames.Add(value.Name)) values.Add(new ResultValue(value.Name, value.Role, null, value.Number, value.Scale));
        return values;

        static double? Larger(double? a, double? b) => a is null ? b : b is null ? a : Math.Max(Math.Abs(a.Value), Math.Abs(b.Value));
    }

    // The order analyses are estimated in: those that use other analyses' results after them (a stable sort keeps the
    // project's order within each stage).
    private static int Stage(IElement element) => element switch
    {
        CompositeAnalysis => 1,
        BivariateAnalysis => 2,
        CoincidentFrequencyAnalysis => 3,
        _ => 0,
    };

    /// <summary>Whether an analysis is estimated.</summary>
    private static bool IsEstimated(IElement element) => element switch
    {
        IAnalysisElement analysis => analysis.IsEstimated,
        FittingAnalysis fit => fit.IsEstimated,
        _ => false,
    };

    /// <summary>The project's analyses, collection by collection, in order, with the name of the collection each is in.</summary>
    private static IEnumerable<(string Collection, IElement Element)> Analyses(BestFitProject project) =>
        (project.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(c => AnalysesOf(c));

    private static IEnumerable<(string, IElement)> AnalysesOf(IElementCollection collection) =>
        collection.Where(e => e is IAnalysisElement or FittingAnalysis).Select(e => (collection.Name, (IElement)e))
            .Concat((collection.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(AnalysesOf));

    private static bool Matches(string example, string analysis, string[] only) =>
        only.Length == 0 || only.Any(o => $"{example} | {analysis}".Contains(o, StringComparison.OrdinalIgnoreCase));

    /// <summary>The example projects under a folder, by name (their path under it, with '/'), in ordinal order.</summary>
    private static IReadOnlyList<(string Name, string Path)> FindExamples(string folder)
    {
        if (!Directory.Exists(folder))
            throw new ArgumentException($"No examples folder {folder} (scripts/fetch-sample-data.sh RMC-BestFit fetches them).");
        return Directory.GetFiles(folder, "*.bestfit", SearchOption.AllDirectories)
            .Select(path => (Name: Path.GetRelativePath(folder, path).Replace('\\', '/'), Path: path))
            .OrderBy(e => e.Name, StringComparer.Ordinal)
            .ToList();
    }

    private static void UseCulture()
    {
        var culture = new CultureInfo(CultureName);
        CultureInfo.DefaultThreadCurrentCulture = culture;
        CultureInfo.DefaultThreadCurrentUICulture = culture;
        CultureInfo.CurrentCulture = culture;
        CultureInfo.CurrentUICulture = culture;
    }

    private static void WithProject(string path, string workRoot, Action<BestFitProject> action)
    {
        string copy = CopyToWorkFolder(path, workRoot);
        BestFitProject project = Open(copy);
        try
        {
            action(project);
        }
        finally
        {
            Close(project);
            DeleteFolder(Path.GetDirectoryName(copy)!);
        }
    }

    // A copy under the example's own file name: the project's name is its file name.
    private static string CopyToWorkFolder(string example, string workRoot)
    {
        string folder = Path.Combine(workRoot, Guid.NewGuid().ToString("N")[..12]);
        Directory.CreateDirectory(folder);
        string copy = Path.Combine(folder, Path.GetFileName(example));
        File.Copy(example, copy);
        return copy;
    }

    private static BestFitProject Open(string file)
    {
        var project = (BestFitProject)Activator.CreateInstance(typeof(BestFitProject), nonPublic: true)!;
        project.FullFileName = file;
        project.Open();
        return project;
    }

    // Closes the project, releases SQLite's pooled connections to it, and drops the messages its elements posted (the
    // messenger holds every element that posted one).
    private static void Close(BestFitProject project)
    {
        project.Close();
        SQLiteConnection.ClearAllPools();
        Messenger.GetInstance().Clear();
    }

    private static void DeleteFolder(string folder)
    {
        if (!Directory.Exists(folder)) return;
        SQLiteConnection.ClearAllPools();
        for (int attempt = 0; ; attempt++)
        {
            try
            {
                Directory.Delete(folder, recursive: true);
                return;
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException && attempt < 20)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                Thread.Sleep(100);
            }
        }
    }
}

/// <summary>An application's UI thread, which owns its projects' objects (the port's plots are Avalonia objects, upstream's WPF ones).</summary>
public interface IUiThread
{
    /// <summary>Runs a function on the UI thread and waits for its result.</summary>
    T Run<T>(Func<T> function);

    /// <summary>Runs an asynchronous function on the UI thread, as the application awaits an analysis, and waits for its result.</summary>
    T Await<T>(Func<Task<T>> function);
}
