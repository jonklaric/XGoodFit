using System;
using System.Collections.Generic;
using System.Linq;

namespace Reproducibility;

/// <summary>How far two runs' values are apart, from the closest to the farthest.</summary>
public enum ValueClass
{
    /// <summary>Bit for bit the same (NaN equals NaN).</summary>
    Identical,

    /// <summary>At most <see cref="Classes.LastBit"/> apart, relatively: the last bits of a double.</summary>
    LastBit,

    /// <summary>At most <see cref="Classes.Small"/> apart: rounding, carried through an optimizer or a long sum.</summary>
    Small,

    /// <summary>Farther apart, in a value an optimizer located (<see cref="ValueRole.Optimizer"/>), within <see cref="Classes.OptimizerBound"/>.</summary>
    Optimizer,

    /// <summary>Farther apart, in a Monte Carlo value (<see cref="ValueRole.MonteCarlo"/>), within <see cref="Classes.MonteCarloBound"/>.</summary>
    MonteCarlo,

    /// <summary>
    /// Unexplained against the saved results, but RMC-BestFit's own element layer differs from them the same way: what
    /// RMC-BestFit computes has changed since the example was saved (or the machine that saved it rounded differently).
    /// Only the report assigns it, when it has RMC-BestFit's own rerun (tools/BestFitFileCompat.Wpf rerun).
    /// </summary>
    SinceSaved,

    /// <summary>Anything else: a deterministic value farther than <see cref="Classes.Small"/> apart, a value one run has and the other hasn't, or a Monte Carlo value beyond the bound.</summary>
    Unexplained,
}

/// <summary>The classification of differences (docs/reproducibility.md).</summary>
public static class Classes
{
    /// <summary>The largest relative difference that is the last bits of a double (a double has about 2.2e-16).</summary>
    public const double LastBit = 1e-12;

    /// <summary>The largest relative difference that is rounding carried through a computation.</summary>
    public const double Small = 1e-6;

    /// <summary>
    /// The largest relative difference accepted in a value an optimizer located: ten times the square root of the
    /// optimizers' 1e-8 tolerance, which is how far a flat objective leaves the point undetermined.
    /// </summary>
    public const double OptimizerBound = 1e-3;

    /// <summary>
    /// The largest relative difference accepted as Monte Carlo error. Two samples of the same posterior or bootstrap
    /// distribution differ by Monte Carlo error, a few percent at most for the analyses' sample sizes in limits and
    /// quantiles, more in diagnostics such as an effective sample size; this bound only catches a value that is no
    /// longer the same quantity.
    /// </summary>
    public const double MonteCarloBound = 0.5;

    /// <summary>
    /// Values whose magnitudes are both below this are compared as equal-scale zeros: a value that is zero up to
    /// rounding (1e-17 against -3e-17) is a last-bit difference, not a relative difference of 400%.
    /// </summary>
    public const double ZeroFloor = 1e-13;

    /// <summary>
    /// The relative difference of two values, <c>|a - b| / max(|a|, |b|, scale)</c>: 0 if identical, infinity if only one
    /// is a number. <paramref name="scale"/> is the magnitude a value that can be near zero is measured against
    /// (<see cref="ResultValue.Scale"/>), at least <see cref="ZeroFloor"/>.
    /// </summary>
    public static double Relative(double? a, double? b, double? scale = null)
    {
        if (a is null || b is null) return a is null && b is null ? 0 : double.PositiveInfinity;
        double x = a.Value, y = b.Value;
        if (x.Equals(y)) return 0;
        if (!double.IsFinite(x) || !double.IsFinite(y)) return double.PositiveInfinity;
        double floor = scale is double s && double.IsFinite(s) ? Math.Max(Math.Abs(s), ZeroFloor) : ZeroFloor;
        return Math.Abs(x - y) / Math.Max(Math.Max(Math.Abs(x), Math.Abs(y)), floor);
    }

    /// <summary>Classifies one value's difference.</summary>
    public static ValueClass Classify(double? a, double? b, ValueRole role, double? scale = null)
    {
        if (a is null && b is null) return ValueClass.Identical;
        if (a is not null && b is not null && a.Value.Equals(b.Value)) return ValueClass.Identical;
        double relative = Relative(a, b, scale);
        if (relative <= LastBit) return ValueClass.LastBit;
        if (relative <= Small) return ValueClass.Small;
        if (role == ValueRole.Optimizer && relative <= OptimizerBound) return ValueClass.Optimizer;
        if (role == ValueRole.MonteCarlo && relative <= MonteCarloBound) return ValueClass.MonteCarlo;
        return ValueClass.Unexplained;
    }

    /// <summary>The class's name in the report.</summary>
    public static string Label(this ValueClass value) => value switch
    {
        ValueClass.Identical => "identical",
        ValueClass.LastBit => "last-bit",
        ValueClass.Small => "small",
        ValueClass.Optimizer => "optimizer",
        ValueClass.MonteCarlo => "Monte Carlo",
        ValueClass.SinceSaved => "since saved",
        _ => "unexplained",
    };
}

/// <summary>One value's difference between the two sides of a comparison.</summary>
/// <param name="Name">The value's name.</param>
/// <param name="Left">Its left side.</param>
/// <param name="Right">Its right side.</param>
/// <param name="Relative">Their relative difference (<see cref="Classes.Relative"/>).</param>
/// <param name="Class">Its class.</param>
public sealed record Difference(string Name, double? Left, double? Right, double Relative, ValueClass Class);

/// <summary>How one analysis's values compare between two sides (saved and rerun, or two OSes' reruns).</summary>
/// <param name="Class">The worst class among its values.</param>
/// <param name="Worst">The value that decides the class: the farthest apart among the values of that class, or null if all are identical.</param>
/// <param name="Largest">The farthest apart of all its values, or null if all are identical.</param>
/// <param name="LargestEstimate">The farthest apart of its deterministic values, or null if all are identical (leaving out those since saved).</param>
/// <param name="LargestOptimizer">The farthest apart of the values an optimizer located, or null if all are identical (leaving out those since saved).</param>
/// <param name="Counts">How many values fall in each class.</param>
/// <param name="Unexplained">The values classed unexplained.</param>
public sealed record Comparison(ValueClass Class, Difference? Worst, Difference? Largest, Difference? LargestEstimate,
    Difference? LargestOptimizer, IReadOnlyDictionary<ValueClass, int> Counts, IReadOnlyList<Difference> Unexplained)
{
    /// <summary>The largest relative difference among its values.</summary>
    public double MaxRelative => Largest?.Relative ?? 0;

    /// <summary>The largest relative difference among its deterministic values (not an optimizer's or Monte Carlo).</summary>
    public double MaxEstimateRelative => LargestEstimate?.Relative ?? 0;

    /// <summary>Compares two sides of the same analysis's values, matched by name.</summary>
    /// <param name="left">One side's values.</param>
    /// <param name="right">The other side's values.</param>
    /// <param name="explain">For a value classed unexplained, the class that explains it, if any (<see cref="ValueClass.SinceSaved"/>).</param>
    public static Comparison Of(IReadOnlyList<Value> left, IReadOnlyList<Value> right, Func<Difference, ValueClass?>? explain = null)
    {
        var rightByName = new Dictionary<string, Value>(StringComparer.Ordinal);
        foreach (Value value in right) rightByName[value.Name] = value;
        var leftNames = new HashSet<string>(left.Select(v => v.Name), StringComparer.Ordinal);

        var counts = Enum.GetValues<ValueClass>().ToDictionary(c => c, _ => 0);
        var differences = new List<(Difference Difference, ValueRole Role)>();
        foreach (Value a in left)
            differences.Add(Compare(a.Name, a.Role, a.Number, rightByName.GetValueOrDefault(a.Name)?.Number,
                Max(a.Scale, rightByName.GetValueOrDefault(a.Name)?.Scale)));
        foreach (Value b in right)
            if (!leftNames.Contains(b.Name)) differences.Add(Compare(b.Name, b.Role, null, b.Number, b.Scale));
        if (explain != null)
            for (int i = 0; i < differences.Count; i++)
                if (differences[i].Difference.Class == ValueClass.Unexplained && explain(differences[i].Difference) is ValueClass explained)
                    differences[i] = (differences[i].Difference with { Class = explained }, differences[i].Role);
        foreach (var (difference, _) in differences) counts[difference.Class]++;

        ValueClass worst = differences.Select(d => d.Difference.Class).DefaultIfEmpty(ValueClass.Identical).Max();
        Difference? Farthest(IEnumerable<Difference> candidates) =>
            candidates.Where(d => d.Class != ValueClass.Identical).OrderByDescending(d => d.Relative).FirstOrDefault();
        return new Comparison(worst,
            Farthest(differences.Select(d => d.Difference).Where(d => d.Class == worst)),
            Farthest(differences.Select(d => d.Difference)),
            Farthest(differences.Where(d => d.Role == ValueRole.Estimate && d.Difference.Class != ValueClass.SinceSaved).Select(d => d.Difference)),
            Farthest(differences.Where(d => d.Role == ValueRole.Optimizer && d.Difference.Class != ValueClass.SinceSaved).Select(d => d.Difference)),
            counts,
            differences.Select(d => d.Difference).Where(d => d.Class == ValueClass.Unexplained).OrderByDescending(d => d.Relative).ToList());
    }

    private static (Difference, ValueRole) Compare(string name, ValueRole role, double? a, double? b, double? scale) =>
        (new Difference(name, a, b, Classes.Relative(a, b, scale), Classes.Classify(a, b, role, scale)), role);

    private static double? Max(double? a, double? b) => a is null ? b : b is null ? a : Math.Max(Math.Abs(a.Value), Math.Abs(b.Value));
}
