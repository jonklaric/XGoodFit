using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Reproducibility;

/// <summary>One run of the tool on one machine: where it ran, and each analysis it found.</summary>
/// <param name="Format">The file format's version.</param>
/// <param name="Environment">The machine, OS and runtime.</param>
/// <param name="Examples">The examples folder, as given.</param>
/// <param name="Only">The <c>--only</c> filters, if any.</param>
/// <param name="Seconds">The run's wall-clock time.</param>
/// <param name="Analyses">Every analysis of the examples run, rerun or not.</param>
public sealed record RunFile(int Format, EnvironmentInfo Environment, string Examples, string[] Only, double Seconds, List<AnalysisRecord> Analyses)
{
    /// <summary>The current format.</summary>
    public const int CurrentFormat = 1;

    /// <summary>How run files are written: indented, and NaN and infinities as strings (JSON has no literal for them).</summary>
    public static readonly JsonSerializerOptions Json = new()
    {
        WriteIndented = true,
        NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.KebabCaseLower) },
    };

    /// <summary>Reads a run file.</summary>
    public static RunFile Read(string path) =>
        JsonSerializer.Deserialize<RunFile>(File.ReadAllText(path), Json) ?? throw new InvalidDataException($"{path}: not a run file");

    /// <summary>Writes the run file, with "\n" line ends on every OS.</summary>
    public void Write(string path)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        File.WriteAllText(path, JsonSerializer.Serialize(this, Json).ReplaceLineEndings("\n") + "\n");
    }
}

/// <summary>Where a run happened.</summary>
/// <param name="Application">
/// Whose element layer reopened and estimated the examples: <c>XGoodFit</c>, or <c>RMC-BestFit &lt;version&gt;</c> for
/// upstream's own WPF one (tools/BestFitFileCompat.Wpf rerun, Windows only).
/// </param>
/// <param name="Platform">The runtime identifier, e.g. win-x64.</param>
/// <param name="OsDescription">The OS's own description.</param>
/// <param name="Architecture">The process architecture.</param>
/// <param name="Framework">The .NET runtime's description.</param>
/// <param name="ProcessorCount">The logical processors .NET sees.</param>
/// <param name="Machine">The machine's name, or a CI runner's.</param>
/// <param name="Started">When the run started (UTC, ISO 8601).</param>
/// <param name="Commit">The repository's commit, when known (CI's GITHUB_SHA).</param>
public sealed record EnvironmentInfo(string Application, string Platform, string OsDescription, string Architecture, string Framework,
    int ProcessorCount, string Machine, string Started, string? Commit)
{
    /// <summary>What the report groups runs by: the platform, and the application if it isn't XGoodFit.</summary>
    [JsonIgnore]
    public string Label => Application is null or "XGoodFit" ? Platform : $"{Platform} {Application}";

    /// <summary>This process's environment.</summary>
    public static EnvironmentInfo Current(string application) => new(
        application,
        RuntimeInformation.RuntimeIdentifier,
        RuntimeInformation.OSDescription,
        RuntimeInformation.ProcessArchitecture.ToString(),
        RuntimeInformation.FrameworkDescription,
        System.Environment.ProcessorCount,
        System.Environment.GetEnvironmentVariable("RUNNER_NAME") ?? System.Environment.MachineName,
        DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ"),
        System.Environment.GetEnvironmentVariable("GITHUB_SHA"));
}

/// <summary>What happened to an analysis in a run.</summary>
public enum AnalysisStatus
{
    /// <summary>Estimated again; <see cref="AnalysisRecord.Values"/> has the saved and the new results.</summary>
    Rerun,

    /// <summary>The project holds it unestimated, so there is nothing to compare: listed, not run.</summary>
    NotEstimated,

    /// <summary>Not run, for the reason given (too long, or not runnable headlessly).</summary>
    Skipped,

    /// <summary>Its rerun threw, or ended unestimated.</summary>
    Failed,
}

/// <summary>What a value is, which decides how far it may move between runs (<see cref="Classes"/>).</summary>
public enum ValueRole
{
    /// <summary>
    /// Computed deterministically from the data and settings (a fit, its parameters and quantiles, goodness of fit):
    /// it may differ only in rounding.
    /// </summary>
    Estimate,

    /// <summary>
    /// Located by an iterative optimizer that stops once its objective agrees to a tolerance (Bulletin 17C's GMM fit:
    /// BFGS, 1e-8, at most 2,000 evaluations), and what is computed from that point (the fit's curve, AIC, RMSE, ERL):
    /// on a flat objective the point is pinned only to about the square root of the tolerance, so a rounding difference
    /// can move it that far.
    /// </summary>
    Optimizer,

    /// <summary>
    /// Comes from random sampling with the saved seed (an MCMC sample and everything taken from it, a bootstrap's
    /// limits and counters, a Monte Carlo integration): a rounding difference can change which proposals are accepted
    /// or which replicates' optimizers take another path, and so move it by Monte Carlo error.
    /// </summary>
    MonteCarlo,
}

/// <summary>One analysis of an example project.</summary>
/// <param name="Example">The example's path under the examples folder, with '/' separators.</param>
/// <param name="Collection">The project's collection the analysis is in.</param>
/// <param name="Name">The analysis's name.</param>
/// <param name="Kind">The element's type, e.g. B17CAnalysis.</param>
/// <param name="Method">How it estimates: the settings that decide which values are Monte Carlo, and the seed.</param>
/// <param name="Status">What happened to it.</param>
/// <param name="Reason">Why it wasn't rerun, or how it failed.</param>
/// <param name="Seconds">How long its rerun took.</param>
/// <param name="Values">Its results, as saved and as rerun, by name.</param>
public sealed record AnalysisRecord(string Example, string Collection, string Name, string Kind, string Method, AnalysisStatus Status,
    string? Reason, double? Seconds, List<ResultValue>? Values)
{
    /// <summary>The analysis's key across runs.</summary>
    [JsonIgnore]
    public string Key => Example + " | " + Name;
}

/// <summary>One result of an analysis, as the project saved it and as the rerun computed it.</summary>
/// <param name="Name">What it is, e.g. <c>point/quantile AEP 0.01</c> or <c>mcmc/MAP/µ</c>.</param>
/// <param name="Role">Whether it is deterministic or Monte Carlo.</param>
/// <param name="Saved">The value the project holds (RMC-BestFit 2.0.x's, on Windows), or null if it doesn't hold one.</param>
/// <param name="Rerun">The value the rerun computed, or null if it computed none.</param>
/// <param name="Scale">
/// For a value that can be near zero, the magnitude its differences are measured against (<see cref="Classes.Relative"/>):
/// a posterior summary's posterior standard deviation, a count's number of replicates. The larger of the two sides'.
/// </param>
public sealed record ResultValue(string Name, ValueRole Role, double? Saved, double? Rerun, double? Scale = null);

/// <summary>One result as one run read it.</summary>
/// <param name="Name">What it is.</param>
/// <param name="Role">Whether it is deterministic or Monte Carlo.</param>
/// <param name="Number">The value, or null if there is none.</param>
/// <param name="Scale">The magnitude its differences are measured against, if it can be near zero (<see cref="ResultValue.Scale"/>).</param>
public sealed record Value(string Name, ValueRole Role, double? Number, double? Scale = null);
