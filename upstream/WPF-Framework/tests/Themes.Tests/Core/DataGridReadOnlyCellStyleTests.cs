using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Themes.Tests.Core;

/// <summary>
/// Regression tests for shared DataGrid read-only cell styles.
/// </summary>
public class DataGridReadOnlyCellStyleTests
{
    private static readonly XNamespace PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// Verifies selectable cell helper styles inherit the shared selectable base style.
    /// </summary>
    /// <param name="styleKey">The selectable cell style key.</param>
    [Theory]
    [InlineData("Left_CellStyle")]
    [InlineData("Center_CellStyle")]
    [InlineData("Center_ReadOnly_CellStyle")]
    [InlineData("Right_CellStyle")]
    [InlineData("Right_ReadOnly_CellStyle")]
    [InlineData("DataGridCellStyle")]
    public void SelectableCellStyles_InheritSelectableBaseStyle(string styleKey)
    {
        XElement style = GetStyle(styleKey);

        Assert.Equal("{StaticResource SelectableDataGridCellStyle}", (string?)style.Attribute("BasedOn"));
    }

    /// <summary>
    /// Verifies the selectable cell base style uses theme resources for active and inactive selection.
    /// </summary>
    [Fact]
    public void SelectableDataGridCellStyle_UsesThemeSelectionResources()
    {
        XElement style = GetStyle("SelectableDataGridCellStyle");

        Assert.Contains(
            style.Descendants(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "Background", "{DynamicResource DataGrid.Row.Selection.Background}"));
        Assert.Contains(
            style.Descendants(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "Foreground", "{DynamicResource DataGrid.Row.Selection.Foreground}"));
        Assert.Contains(
            style.Descendants(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "Background", "{DynamicResource DataGrid.Row.Selection.Inactive.Background}"));
        Assert.Contains(
            style.Descendants(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "Foreground", "{DynamicResource DataGrid.Row.Selection.Inactive.Foreground}"));
    }

    /// <summary>
    /// Verifies read-only cell styles remain selectable by mouse.
    /// </summary>
    /// <param name="styleKey">The read-only cell style key.</param>
    [Theory]
    [InlineData("Center_ReadOnly_CellStyle")]
    [InlineData("Right_ReadOnly_CellStyle")]
    public void ReadOnlyCellStyle_DoesNotDisableHitTesting(string styleKey)
    {
        XElement style = GetStyle(styleKey);

        Assert.DoesNotContain(
            style.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "IsHitTestVisible", "False"));
    }

    /// <summary>
    /// Verifies the disabled cell style still disables interaction.
    /// </summary>
    [Fact]
    public void DisabledCellStyle_DisablesCells()
    {
        XElement style = GetStyle("Disabled_CellStyle");

        Assert.Contains(
            style.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "IsEnabled", "False"));
    }

    /// <summary>
    /// Finds a style in the shared DataGrid resource dictionary.
    /// </summary>
    /// <param name="styleKey">The style key to find.</param>
    /// <returns>The style element.</returns>
    private static XElement GetStyle(string styleKey)
    {
        XDocument document = XDocument.Load(ResolveRepoPath("src/Themes/Resources/Controls/DataGrid.xaml"));
        XElement? style = document
            .Descendants(PresentationNamespace + "Style")
            .FirstOrDefault(element => (string?)element.Attribute(XamlNamespace + "Key") == styleKey);

        Assert.NotNull(style);
        return style!;
    }

    /// <summary>
    /// Determines whether a setter has the expected property and value.
    /// </summary>
    /// <param name="setter">The setter element.</param>
    /// <param name="property">The expected property.</param>
    /// <param name="value">The expected value.</param>
    /// <returns><c>true</c> when the setter matches.</returns>
    private static bool HasSetter(XElement setter, string property, string value)
    {
        return (string?)setter.Attribute("Property") == property
            && (string?)setter.Attribute("Value") == value;
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
