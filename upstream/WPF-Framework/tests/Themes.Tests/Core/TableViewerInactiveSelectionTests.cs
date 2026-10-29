using System.IO;
using System.Xml.Linq;
using Xunit;

namespace Themes.Tests.Core;

/// <summary>
/// Regression tests for TableViewer active and inactive selection styling.
/// </summary>
public class TableViewerInactiveSelectionTests
{
    private static readonly XNamespace PresentationNamespace = "http://schemas.microsoft.com/winfx/2006/xaml/presentation";
    private static readonly XNamespace XamlNamespace = "http://schemas.microsoft.com/winfx/2006/xaml";

    /// <summary>
    /// Verifies TableViewer exposes inactive selection brushes as dependency properties.
    /// </summary>
    [Fact]
    public void TableViewer_ExposesInactiveSelectionDependencyProperties()
    {
        string source = File.ReadAllText(ResolveRepoPath("src/DatabaseControls/TableViewer.xaml.cs"));

        Assert.Contains("InactiveSelectedColorProperty", source);
        Assert.Contains("nameof(InactiveSelectedColor)", source);
        Assert.Contains("public Brush InactiveSelectedColor", source);
        Assert.Contains("InactiveSelectedForegroundColorProperty", source);
        Assert.Contains("nameof(InactiveSelectedForegroundColor)", source);
        Assert.Contains("public Brush InactiveSelectedForegroundColor", source);
        Assert.Contains("OnVisualPropertyChanged", source);
    }

    /// <summary>
    /// Verifies TableViewerStyle maps active and inactive selection colors to shared DataGrid theme resources.
    /// </summary>
    [Fact]
    public void TableViewerStyle_UsesSharedActiveAndInactiveSelectionResources()
    {
        XElement style = GetTableViewerStyle();

        Assert.Contains(
            style.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "SelectedColor", "{DynamicResource DataGrid.Row.Selection.Background}"));
        Assert.Contains(
            style.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "SelectedForegroundColor", "{DynamicResource DataGrid.Row.Selection.Foreground}"));
        Assert.Contains(
            style.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "InactiveSelectedColor", "{DynamicResource DataGrid.Row.Selection.Inactive.Background}"));
        Assert.Contains(
            style.Elements(PresentationNamespace + "Setter"),
            setter => HasSetter(setter, "InactiveSelectedForegroundColor", "{DynamicResource DataGrid.Row.Selection.Inactive.Foreground}"));
    }

    /// <summary>
    /// Verifies selected cells switch to inactive brushes when table selection is inactive.
    /// </summary>
    [Fact]
    public void SelectCell_UsesInactiveBrushesWhenTableSelectionIsInactive()
    {
        string source = File.ReadAllText(ResolveRepoPath("src/DatabaseControls/TableViewer.xaml.cs"));
        string selectCell = ExtractMethod(source, "private void SelectCell");
        string isTableSelectionActive = ExtractMethod(source, "private bool IsTableSelectionActive");

        Assert.Contains("bool selectionActive = IsTableSelectionActive();", selectCell);
        Assert.Contains("cell.Background = selectionActive ? SelectedColor : InactiveSelectedColor;", selectCell);
        Assert.Contains("cell.Foreground = selectionActive ? SelectedForegroundColor : InactiveSelectedForegroundColor;", selectCell);
        Assert.Contains("IsKeyboardFocusWithin", isTableSelectionActive);
        Assert.Contains("GridPanel.IsKeyboardFocusWithin", isTableSelectionActive);
        Assert.Contains("_cellEditTextBox.IsKeyboardFocusWithin", isTableSelectionActive);
        Assert.Contains("_mouseSelectionMode != SelectionMode.None", isTableSelectionActive);
    }

    /// <summary>
    /// Verifies the focused active-cell highlight is skipped when table selection is inactive.
    /// </summary>
    [Fact]
    public void SetActiveCell_DoesNotForceActiveBrushesWhenSelectionIsInactive()
    {
        string source = File.ReadAllText(ResolveRepoPath("src/DatabaseControls/TableViewer.xaml.cs"));
        string setActiveCell = ExtractMethod(source, "private void SetActiveCell");

        Assert.Contains("if (!IsTableSelectionActive()) return;", setActiveCell);
        Assert.Contains("cell.Background = ActiveCellBackground;", setActiveCell);
        Assert.Contains("cell.Foreground = ActiveCellForeground;", setActiveCell);
    }

    /// <summary>
    /// Verifies focus changes repaint visible selections without clearing selection state.
    /// </summary>
    [Fact]
    public void FocusHandlersRefreshSelectionVisualsWithoutClearingSelectionState()
    {
        string source = File.ReadAllText(ResolveRepoPath("src/DatabaseControls/TableViewer.xaml.cs"));
        string refreshSelectionVisuals = ExtractMethod(source, "private void RefreshSelectionVisuals");
        string focusHandler = ExtractMethod(source, "private void TableSelectionFocusChanged");

        Assert.Contains("GridPanel.GotKeyboardFocus += TableSelectionFocusChanged;", source);
        Assert.Contains("GridPanel.LostKeyboardFocus += TableSelectionFocusChanged;", source);
        Assert.Contains("IsKeyboardFocusWithinChanged += TableSelectionFocusWithinChanged;", source);
        Assert.Contains("RefreshSelectionVisuals();", focusHandler);
        Assert.Contains("RefreshSelectionVisuals();", ExtractMethod(source, "private void TableSelectionFocusWithinChanged"));
        Assert.Contains("DeSelectAllCells();", refreshSelectionVisuals);
        Assert.Contains("SetSelectedCells();", refreshSelectionVisuals);
        Assert.DoesNotContain("_selectedDataRowIndices.Clear()", refreshSelectionVisuals);
        Assert.DoesNotContain("_selectedCellIndices.Clear()", refreshSelectionVisuals);
        Assert.DoesNotContain("_selectedColumnIndices.Clear()", refreshSelectionVisuals);
        Assert.Contains("GridPanel.Focus();", ExtractMethod(source, "private void GridPanel_MouseLeftButtonDown"));
        Assert.Contains("GridPanel.Focus();", ExtractMethod(source, "private void RowsGrid_MouseLeftButtonDown"));
        Assert.Contains("GridPanel.Focus();", ExtractMethod(source, "private void ColumnsGrid_MouseLeftButtonDown"));
        Assert.Contains("GridPanel.Focus();", ExtractMethod(source, "private void SelectAllLeftMouseDown"));
    }

    /// <summary>
    /// Finds the TableViewerStyle in the DatabaseControls theme resource dictionary.
    /// </summary>
    /// <returns>The TableViewer style element.</returns>
    private static XElement GetTableViewerStyle()
    {
        XDocument document = XDocument.Load(ResolveRepoPath("src/DatabaseControls/Themes/DatabaseControlsTheme.xaml"));
        XElement? style = document
            .Descendants(PresentationNamespace + "Style")
            .FirstOrDefault(element => (string?)element.Attribute(XamlNamespace + "Key") == "TableViewerStyle");

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
    /// Extracts a method body from a C# source file.
    /// </summary>
    /// <param name="source">The source text.</param>
    /// <param name="signature">The method signature prefix.</param>
    /// <returns>The method body, including the signature and braces.</returns>
    private static string ExtractMethod(string source, string signature)
    {
        int start = source.IndexOf(signature, StringComparison.Ordinal);
        Assert.True(start >= 0, signature + " should exist.");

        int bodyStart = source.IndexOf('{', start);
        Assert.True(bodyStart >= start, signature + " should have a body.");

        int depth = 0;
        for (int i = bodyStart; i < source.Length; i++)
        {
            if (source[i] == '{')
            {
                depth++;
            }
            else if (source[i] == '}')
            {
                depth--;
                if (depth == 0)
                {
                    return source.Substring(start, i - start + 1);
                }
            }
        }

        throw new InvalidOperationException(signature + " body was not closed.");
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
