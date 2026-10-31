namespace Reproducibility;

/// <summary>
/// Differences the check found and explained as upstream issues (IssuesToRaise/), by analysis and platform. Their
/// unexplained values are classed <see cref="ValueClass.Known"/>, so the weekly run reports them, with their issue,
/// without failing. Remove an entry when its issue is fixed.
/// </summary>
public static class KnownIssues
{
    /// <summary>The known cases: the example (its file name, without .bestfit), the analysis, the platform, and the issue.</summary>
    public static readonly IReadOnlyList<(string Example, string Analysis, string Platform, string Issue)> All = new[]
    {
        // Bulletin 17C's penalized GMM reaches another fixed point on macOS x64 (Apple's maths library on Intel): skew
        // 0.0018 instead of -0.1797, the 1e-6 AEP flow 35% higher, still reported as Success (first seen in the workflow's
        // run 41183231534, 2026-10-31).
        ("blakely-mountain-dam-b17c-metric", "Systematic + Historical + QPen", "osx-x64",
            "IssuesToRaise/RMC-BestFit-b17c-penalized-gmm-stops-at-tolerance-or-evaluation-limit.md"),
    };

    /// <summary>The issue that explains an analysis's unexplained values on a platform, or null.</summary>
    public static string? For(string example, string analysis, string platform) =>
        All.FirstOrDefault(k => k.Platform == platform && k.Analysis == analysis &&
            string.Equals(Path.GetFileNameWithoutExtension(example), k.Example, StringComparison.Ordinal)).Issue;
}
