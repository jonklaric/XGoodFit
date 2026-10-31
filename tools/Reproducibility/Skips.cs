using System.Linq;

namespace Reproducibility;

/// <summary>
/// The estimated analyses the run doesn't estimate again, with the reason, which the report lists. Their saved results
/// are still read. Any other analysis taking longer than the run's <c>--limit</c> is cancelled and listed the same way.
/// </summary>
public static class Skips
{
    // (example path, analysis name, reason)
    private static readonly (string Example, string Analysis, string Reason)[] All =
    {
    };

    /// <summary>Why an analysis is skipped, or null to estimate it again.</summary>
    public static string? Reason(string example, string analysis) =>
        All.FirstOrDefault(s => s.Example == example && s.Analysis == analysis).Reason;
}
