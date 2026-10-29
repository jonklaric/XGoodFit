using global::NumericControls;
using global::NumericControls.Distributions.Univariate;
using Numerics.Distributions;
using Xunit;

namespace NumericControls.Tests.Distributions;

/// <summary>
/// Guards NumericControls against distribution types that require external component configuration.
/// </summary>
public class DistributionFactoryCompatibilityTests
{
    /// <summary>
    /// Verifies that both selector defaults initialize without throwing and contain exact constructible types.
    /// </summary>
    [StaFact]
    public void DefaultSelectorDistributions_AreDirectlyConstructible()
    {
        var expectedTypes = Enum.GetValues<UnivariateDistributionType>()
            .Where(type => type != UnivariateDistributionType.Empirical &&
                           type != UnivariateDistributionType.KernelDensity &&
                           type != UnivariateDistributionType.UserDefined)
            .Where(type => UnivariateDistributionFactory.TryCreateDistribution(type, out _))
            .ToArray();

        var selectorTypes = Selector.DefaultDistributions.Select(distribution => distribution.Type).ToArray();
        var controlTypes = DistributionSelectorControl.DefaultDistributions
            .Select(distribution => distribution.Type)
            .ToArray();

        Assert.Equal(expectedTypes, selectorTypes);
        Assert.Equal(expectedTypes, controlTypes);
    }

    /// <summary>
    /// Verifies that unsupported types are absent from uncertain-curve defaults.
    /// </summary>
    [StaFact]
    public void DefaultUncertainDistributionOptions_ExcludeUnsupportedTypes()
    {
        var selectorOptions = UncertainOrderedDataSelectorControl.DefaultDistributionOptions;
        var editorOptions = UncertainOrderedDataTableEditor.DefaultDistributionOptions;

        Assert.All(selectorOptions, AssertDirectlyConstructible);
        Assert.All(editorOptions, AssertDirectlyConstructible);
        Assert.DoesNotContain(UnivariateDistributionType.UserDefined, selectorOptions);
        Assert.DoesNotContain(UnivariateDistributionType.UserDefined, editorOptions);
    }

    /// <summary>
    /// Asserts that a distribution type can be constructed without external components.
    /// </summary>
    /// <param name="type">Distribution type to validate.</param>
    private static void AssertDirectlyConstructible(UnivariateDistributionType type)
    {
        Assert.True(UnivariateDistributionFactory.TryCreateDistribution(type, out var distribution));
        Assert.NotNull(distribution);
        Assert.Equal(type, distribution.Type);
    }
}
