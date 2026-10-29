using System.IO;
using Xunit;

namespace GenericControls.Tests;

/// <summary>
/// Structural regression guard for the XAML <c>ConverterCulture</c> binding pattern.
/// </summary>
/// <remarks>
/// Phase 3 added <c>xmlns:glob="clr-namespace:System.Globalization;assembly=mscorlib"</c>
/// and <c>ConverterCulture={x:Static glob:CultureInfo.CurrentCulture}</c> to numeric
/// bindings in shared framework controls so they parse user input under the user's
/// locale even in host apps that don't call <c>FrameworkElement.LanguageProperty.OverrideMetadata</c>.
///
/// These tests scan the XAML files at test time and verify the patterns are present.
/// They catch accidental removal during XAML refactors / merges. They run by reading
/// files from the source tree (not from the compiled BAML), so the source is what's
/// guarded.
///
/// A runtime test of binding behavior would be more thorough but is omitted intentionally:
/// once any test in the assembly triggers <c>FrameworkElement.LanguageProperty.OverrideMetadata</c>,
/// the override cannot be cleanly reset, leading to false-pass results that hide regressions.
/// </remarks>
public class XamlBindingCultureTests
{
    private const string GlobNamespace = "xmlns:glob=\"clr-namespace:System.Globalization;assembly=mscorlib\"";
    private const string ConverterCulturePattern = "ConverterCulture={x:Static glob:CultureInfo.CurrentCulture}";

    public static TheoryData<string> XamlFilesWithCultureBinding => new()
    {
        "src/GenericControls/General Controls/NumericTextBox2.xaml",
        "src/GenericControls/Properties Controls/NumericPropertyControl.xaml",
        "src/GenericControls/Properties Controls/NumericAutoPropertyControl.xaml",
        "src/GenericControls/Properties Controls/NumericSliderPropertyControl.xaml",
        "src/GenericControls/Properties Controls/NumericPropertySelectorControl.xaml",
        "src/FrameworkUI.Demo/UI/Hazard Controls/HazardControl.xaml",
        "src/GenericControls.Demo/MainWindow.xaml",
        "src/NumericControls/Distributions/Univariate/Distribution Selector/Selector.xaml",
        "src/NumericControls/Distributions/Univariate/Distribution Selector/DistributionSelectorControl.xaml",
    };

    [Theory]
    [MemberData(nameof(XamlFilesWithCultureBinding))]
    public void XamlFile_DeclaresGlobalizationNamespace(string repoRelativePath)
    {
        string fullPath = ResolveRepoPath(repoRelativePath);
        Assert.True(File.Exists(fullPath), $"XAML file not found at {fullPath}");

        string content = File.ReadAllText(fullPath);
        Assert.Contains(GlobNamespace, content);
    }

    [Theory]
    [MemberData(nameof(XamlFilesWithCultureBinding))]
    public void XamlFile_HasConverterCultureBindingPattern(string repoRelativePath)
    {
        string fullPath = ResolveRepoPath(repoRelativePath);
        Assert.True(File.Exists(fullPath), $"XAML file not found at {fullPath}");

        string content = File.ReadAllText(fullPath);
        Assert.Contains(ConverterCulturePattern, content);
    }

    /// <summary>
    /// Walks up from the test assembly's bin directory to find the repository root
    /// (identified by the presence of <c>WPF-Framework.sln</c>), then resolves the
    /// repo-relative path. This works regardless of build configuration depth.
    /// </summary>
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
