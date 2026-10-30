using System.Collections;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using DatabaseManager;
using ExpressionParser;
using ExpressionParser.Parser;
using GenericControls;
using WpfCompat;

namespace DatabaseControls.Tests;

/// <summary>
/// The windows the TableViewer opens: find, the field calculator and its select-by-attribute mode. The field
/// calculator's results are checked against upstream's ExpressionParser, evaluated row by row.
/// </summary>
[TestClass]
public sealed class DialogTests
{
    // The values upstream's ExpressionParser gives the expression for each row of the view.
    private static object[] Evaluate(DataTableView view, string expression)
    {
        var variables = new Dictionary<string, ResultType>();
        for (int i = 0; i < view.ColumnNames.Length; i++)
        {
            variables[view.ColumnNames[i]] = Parser.TypeToResultType(view.ColumnTypes[i]);
        }
        IParserNode tree = Parser.Parse(expression, true, variables);
        var results = new object[view.NumberOfRows];
        for (int row = 0; row < results.Length; row++)
        {
            foreach (VariableNode node in tree.GetVariableNodes())
            {
                node.SetValue(view.GetColumn(node.VariableName)[row]);
            }
            results[row] = tree.Evaluate().Result;
        }
        return results;
    }

    private static MenuItem ColumnMenuItem(Window window, TableViewer viewer, int column, string header)
    {
        window.RightClick(viewer.ColumnHeadersGrid.Children.OfType<Border>().ElementAt(column).Centre());
        var menu = window.GetVisualDescendants().OfType<ContextMenu>().Single(m => m.IsOpen);
        menu.Close();
        return menu.Items.Cast<MenuItem>().Single(i => (string)i.Header! == header);
    }

    [TestMethod]
    public void Find_MovesToTheNextMatch_WrapsAround_AndSaysWhenThereIsNone()
    {
        HeadlessSession.Run(() =>
        {
            Captures.UsePopupOverlay();
            var (viewer, window) = TestSupport.ShowViewer(TestSupport.InMemory(TestSupport.SampleTable(100)));
            var results = new List<(int Row, double Scroll)>();
            string[] notFound = [];
            string title = "";

            using (TestSupport.WhenOpened<FindAndReplace>(find =>
            {
                title = find.Title ?? "";
                find.FindText.Text = "Item 4";
                find.FindNextButton.RaiseClick();
                results.Add((viewer.ActiveCellRowIndex, viewer.VerticalScrollbar.Value));
                find.FindNextButton.RaiseClick();
                results.Add((viewer.ActiveCellRowIndex, viewer.VerticalScrollbar.Value));
                // Whole text only.
                find.MatchWordCheckbox.IsChecked = true;
                find.FindNextButton.RaiseClick();
                results.Add((viewer.ActiveCellRowIndex, viewer.VerticalScrollbar.Value));
                // Backwards from row 4 wraps to the end.
                find.MatchWordCheckbox.IsChecked = false;
                find.FindText.Text = "Item 9";
                find.FindPreviousButton.RaiseClick();
                results.Add((viewer.ActiveCellRowIndex, viewer.VerticalScrollbar.Value));
                // Case.
                find.MatchCaseCheckbox.IsChecked = true;
                find.FindText.Text = "item 9";
                using (TestSupport.WhenOpened<MessageBoxWindow>(box =>
                {
                    notFound = box.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
                    box.ButtonWithContent("OK").RaiseClick();
                }))
                {
                    find.FindNextButton.RaiseClick();
                }
                find.Close();
            }))
            {
                ColumnMenuItem(window, viewer, 0, "Find...").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }

            Assert.AreEqual("Find In: Name", title);
            Assert.AreEqual(4, results[0].Row, "the first match after the active cell");
            Assert.AreEqual(40, results[1].Row, "\"Item 40\" contains the text");
            Assert.AreEqual(40, results[1].Scroll, "the table scrolls to the match");
            Assert.AreEqual(4, results[2].Row, "Match Entire Text skips 41 to 49, wrapping round to 4");
            Assert.AreEqual(99, results[3].Row, "Find Previous wraps to the end");
            CollectionAssert.Contains(notFound, "Search for: 'item 9' was not found.");
            window.Close();
        });
    }

    [TestMethod]
    public void FieldCalculator_AddsAColumn_WithTheParsersValues()
    {
        HeadlessSession.Run(() =>
        {
            const string expression = "[Value] * 2 + [Count]";
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            object[] expected = Evaluate(view, expression);
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            string[] fields = [];
            int previewRows = 0;
            object? previewHeader = null;
            bool? createNew = null;

            using (TestSupport.WhenOpened<FieldCalculator>(fc =>
            {
                TestSupport.Settle(); // the viewer restores the last expression once the window has rendered
                createNew = fc.CreateNewFieldRadioButton.IsChecked;
                fields = fc.ExistingFieldsCombobox.Items.Cast<string>().ToArray();
                fc.NewFieldName.Text = "Doubled";
                fc.ExpressionCalculator.SetExpressionText(expression);
                TestSupport.Settle();
                previewRows = ((IEnumerable)fc.PreviewGrid.ItemsSource).Cast<object>().Count();
                previewHeader = fc.PreviewExpander.Header;
                Assert.IsTrue(fc.ExecuteButton.IsEnabled);
                fc.ExecuteButton.RaiseClick();
            }))
            {
                viewer.OpenFC.RaiseClick();
            }

            Assert.AreEqual(true, createNew, "with no column given, it creates one");
            CollectionAssert.AreEqual(new[] { "Name", "Value", "Count" }, fields);
            Assert.AreEqual(5, previewRows, "the preview shows the first five rows");
            Assert.AreEqual("Preview — first 5 of 20 rows", previewHeader);
            CollectionAssert.AreEqual(new[] { "Name", "Value", "Count", "Doubled" }, view.ColumnNames);
            Assert.AreEqual(typeof(double), view.ColumnTypes[3]);
            CollectionAssert.AreEqual(expected, view.GetColumn(3));
            CollectionAssert.AreEqual(new[] { "Name", "Value", "Count", "Doubled" }, viewer.HeaderTexts());
            Assert.AreEqual("4", viewer.CellText(1, 3));
            Assert.IsTrue(viewer.Undo.IsEnabled);

            // The calculator opens again with the last expression.
            string reopened = "";
            bool parsed = false;
            using (TestSupport.WhenOpened<FieldCalculator>(fc =>
            {
                TestSupport.Settle();
                reopened = fc.ExpressionCalculator.GetExpressionText();
                parsed = fc.ExecuteButton.IsEnabled && fc.PreviewExpander.IsVisible;
                fc.Close();
            }))
            {
                viewer.OpenFC.RaiseClick();
            }
            Assert.AreEqual(expression, reopened);
            Assert.IsTrue(parsed, "the restored expression is parsed, as after WPF's ContentRendered");
            window.Close();
        });
    }

    [TestMethod]
    public void FieldCalculator_ForAColumn_UpdatesTheSelectedRowsOnly()
    {
        HeadlessSession.Run(() =>
        {
            Captures.UsePopupOverlay();
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            var headers = viewer.RowHeadersGrid.Children.OfType<Border>().ToArray();
            window.Click(headers[2].Centre());
            window.Click(headers[4].Centre(), Avalonia.Input.RawInputModifiers.Shift);
            bool fieldsShown = true;
            bool? selectedOnly = null;

            using (TestSupport.WhenOpened<FieldCalculator>(fc =>
            {
                fieldsShown = fc.FieldSelectionGrid.IsVisible;
                selectedOnly = fc.UseSelectedRange.IsChecked;
                fc.ExpressionCalculator.SetExpressionText("[Count] + 100");
                TestSupport.Settle();
                fc.ExecuteButton.RaiseClick();
            }))
            {
                ColumnMenuItem(window, viewer, 2, "Field Calculator...").RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            }

            Assert.IsFalse(fieldsShown, "the column is given, so the field choice hides");
            Assert.AreEqual(true, selectedOnly, "with rows selected, it applies to them");
            CollectionAssert.AreEqual(new object[] { 0, 1, 102, 103, 100, 1 }, view.GetColumn(2).Take(6).ToArray());
            Assert.AreEqual("103", viewer.CellText(3, 2));
            window.Close();
        });
    }

    [TestMethod]
    public void FieldCalculator_Insert_AndDoubleClick_PutTheColumnInTheExpression()
    {
        HeadlessSession.Run(() =>
        {
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var fc = new FieldCalculator(view, null, null);
            fc.Show();
            TestSupport.Settle();

            fc.VariablesListBox.SelectedIndex = 1;
            TestSupport.Settle();
            Assert.IsTrue(fc.InsertVariableButton.IsEnabled);
            fc.InsertVariableButton.RaiseClick();
            Assert.AreEqual("[Value]", fc.ExpressionCalculator.GetExpressionText());

            var item = (ListBoxItem)fc.VariablesListBox.Items[2]!;
            fc.DoubleClick(item.Centre());
            Assert.AreEqual("[Value][Count]", fc.ExpressionCalculator.GetExpressionText().Replace(" ", ""), "a double click inserts, as WPF's MouseDoubleClick on the list");
            fc.Close();
        });
    }

    [TestMethod]
    public void FieldCalculator_RefusesAnExistingName_AndAnExpressionWithErrors()
    {
        HeadlessSession.Run(() =>
        {
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var fc = new FieldCalculator(view, null, null);
            fc.Show();
            TestSupport.Settle();

            fc.ExpressionCalculator.SetExpressionText("[Value] * (2");
            TestSupport.Settle();
            Assert.IsFalse(fc.ExecuteButton.IsEnabled, "an expression with errors can't run");

            fc.ExpressionCalculator.SetExpressionText("[Value] * 2");
            TestSupport.Settle();
            fc.NewFieldName.Text = "Value";
            string[] message = [];
            using (TestSupport.WhenOpened<MessageBoxWindow>(box =>
            {
                message = box.GetVisualDescendants().OfType<TextBlock>().Select(t => t.Text ?? "").ToArray();
                box.ButtonWithContent("OK").RaiseClick();
            }))
            {
                fc.ExecuteButton.RaiseClick();
            }
            CollectionAssert.Contains(message, "The field name, Value, already exists. Please define a new field name, or choose the option \"Update Existing Field\".");
            Assert.IsTrue(fc.IsVisible, "the calculator stays open");
            Assert.AreEqual(3, view.ColumnNames.Length);
            fc.Close();
        });
    }

    [TestMethod]
    public void SelectByAttribute_SelectsTheRowsWhereTheExpressionIsTrue()
    {
        HeadlessSession.Run(() =>
        {
            var view = TestSupport.InMemory(TestSupport.SampleTable(20));
            var (viewer, window) = TestSupport.ShowViewer(view, configure: v => v.Editable = true);
            List<int>? reported = null;
            viewer.SelectedRowIndicesChanged += rows => reported = rows.ToList();
            string title = "";
            bool fieldsShown = true;
            string[] previewHeaders = [];

            using (TestSupport.WhenOpened<FieldCalculator>(fc =>
            {
                TestSupport.Settle(); // the viewer restores the last expression once the window has rendered
                title = fc.Title ?? "";
                fieldsShown = fc.FieldSelectionGrid.IsVisible;
                // A number isn't a selection.
                fc.ExpressionCalculator.SetExpressionText("[Value] * 2");
                TestSupport.Settle();
                Assert.IsFalse(fc.ExecuteButton.IsEnabled);
                Assert.IsTrue(fc.resultMessageText.IsVisible);
                StringAssert.StartsWith(fc.resultMessageText.Text, "Expression must return a true/false result for selection.");

                fc.ExpressionCalculator.SetExpressionText("[Value] > 20");
                TestSupport.Settle();
                previewHeaders = fc.PreviewGrid.Columns.Select(c => (string)c.Header!).ToArray();
                fc.ExecuteButton.RaiseClick();
            }))
            {
                viewer.SelectByAttribute.RaiseClick();
            }

            Assert.AreEqual("Select By Attribute", title);
            Assert.IsFalse(fieldsShown);
            CollectionAssert.AreEqual(new[] { "Row", "Selected" }, previewHeaders);
            int[] expected = Enumerable.Range(0, 20).Where(i => i * 1.5 > 20).ToArray();
            CollectionAssert.AreEqual(expected, viewer.GetSelectedRows);
            CollectionAssert.AreEqual(expected, reported);
            Assert.IsTrue(viewer.ShowSelected.IsEnabled);
            window.Close();
        });
    }
}
