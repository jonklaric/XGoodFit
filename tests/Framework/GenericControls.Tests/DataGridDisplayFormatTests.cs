using System.Globalization;
using Avalonia.Controls;
using Avalonia.Data;
using static GenericControls.Tests.GridTesting;

namespace GenericControls.Tests;

/// <summary>
/// XGoodFit addition: a column whose binding gets its StringFormat after the column has it, as upstream's
/// code-behinds set the formats they read from the user's settings, shows its values formatted, as WPF's does
/// (DataGridDisplayBinding).
/// </summary>
[TestClass]
public sealed class DataGridDisplayFormatTests
{
    [TestMethod]
    public void AFormatSetAfterTheColumnHasItsBinding_FormatsTheValues()
    {
        HeadlessSession.Run(() =>
        {
            var (window, grid) = ShowGrid(Rows(2), g =>
            {
                g.AutoGenerateColumns = false;
                g.IsReadOnly = true;
                var later = new DataGridTextColumn { Header = "Later", Binding = new ReflectionBinding(nameof(GridRow.Value)) { ConverterCulture = CultureInfo.InvariantCulture } };
                var first = new DataGridTextColumn { Header = "First", Binding = new ReflectionBinding(nameof(GridRow.Value)) { ConverterCulture = CultureInfo.InvariantCulture, StringFormat = "N3" } };
                g.Columns.Add(later);
                g.Columns.Add(first);
                // ProDataGrid gave the first binding its converter; the format comes after, before the first render.
                ((ReflectionBinding)later.Binding).StringFormat = "E4";
            });
            try
            {
                Assert.AreEqual("1.0500E+001", (Cell(grid, 0, 0).Content as TextBlock)?.Text);
                Assert.AreEqual("2.1000E+001", (Cell(grid, 1, 0).Content as TextBlock)?.Text);
                Assert.AreEqual("10.500", (Cell(grid, 0, 1).Content as TextBlock)?.Text, "a format the binding had already");
            }
            finally
            {
                window.Close();
            }
        });
    }
}
