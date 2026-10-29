using System.IO;
using System.Xml.Linq;
using Xunit;

namespace GenericControls.Tests.Controls;

/// <summary>
/// Regression tests for GenericControls DataGrid selection styling.
/// </summary>
public class DataGridThemeSelectionTests
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
    [InlineData("CopyPasteDataGridCellStyle")]
    [InlineData("ValidationDataGridCellStyle")]
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
    /// Verifies read-only helper styles remain hit-testable for mouse selection.
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
    /// Verifies the disabled cell helper remains disabled.
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
    /// Verifies the grid-level styles use selectable cell styles.
    /// </summary>
    /// <param name="gridStyleKey">The grid style key.</param>
    /// <param name="cellStyleKey">The expected cell style key.</param>
    [Theory]
    [InlineData("CopyPasteDataGridStyle", "CopyPasteDataGridCellStyle")]
    [InlineData("ValidationDataGridStyle", "ValidationDataGridCellStyle")]
    public void DataGridStyle_UsesSelectableCellStyle(string gridStyleKey, string cellStyleKey)
    {
        XElement gridStyle = GetStyle(gridStyleKey);
        XElement cellStyle = GetStyle(cellStyleKey);

        Assert.Contains(
            gridStyle.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "CellStyle", "{StaticResource " + cellStyleKey + "}"));
        Assert.Equal("{StaticResource SelectableDataGridCellStyle}", (string?)cellStyle.Attribute("BasedOn"));
        Assert.DoesNotContain(
            cellStyle.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "IsHitTestVisible", "False"));
    }

    /// <summary>
    /// Finds a style in the GenericControls resource dictionary.
    /// </summary>
    /// <param name="styleKey">The style key to find.</param>
    /// <returns>The style element.</returns>
    private static XElement GetStyle(string styleKey)
    {
        XDocument document = XDocument.Load(ResolveRepoPath("src/GenericControls/Themes/GenericControlsTheme.xaml"));
        XElement? style = document
            .Descendants(PresentationNamespace + "Style")
            .FirstOrDefault(element => (string?)element.Attribute(XamlNamespace + "Key") == styleKey);

        Assert.NotNull(style);
        return style!;
    }

    /// <summary>
    /// Determines whether an element has the expected property and value.
    /// </summary>
    /// <param name="element">The element to inspect.</param>
    /// <param name="property">The expected property.</param>
    /// <param name="value">The expected value.</param>
    /// <returns><c>true</c> when the element matches.</returns>
    private static bool HasSetter(XElement element, string property, string value)
    {
        return (string?)element.Attribute("Property") == property
            && (string?)element.Attribute("Value") == value;
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
