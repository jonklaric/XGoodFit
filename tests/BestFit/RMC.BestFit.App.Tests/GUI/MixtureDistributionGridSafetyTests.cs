using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Threading;
using Avalonia.VisualTree;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Numerics.Distributions;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC.BestFit.UI;
using RMC_BestFit;
using System;
using System.Collections.ObjectModel;
using System.Linq;

namespace RMC.BestFit.App.Tests.GUI
{
    /// <summary>
    /// Exercises the actual mixture properties grid, realized cells, and deferred row edits.
    /// </summary>
    /// <remarks>
    /// No estimation runs. Tests use default unestimated analyses and the production resource graph.
    /// Application resources and WPF controls require serialized STA execution.
    /// <para>
    /// XGoodFit port of upstream's GUI/MixtureDistributionGridSafetyTests.cs, run against the Avalonia panel:
    /// the panel is shown in a window of upstream's size, since Avalonia applies a control's template (and the grid
    /// realizes its cells) only in a tree with a root, and rendering is the headless session's. FindName is
    /// FindControl; the grid's ItemsSource is ProDataGrid's (not an ItemsControl's), and a binding on it is found
    /// with BindingOperations.GetBindingExpressionBase (WPF's IsDataBound); selected cells are ProDataGrid's
    /// DataGridCellInfo of the row's item and the column; and the combo box's item is selected, as a click does.
    /// </para>
    /// </remarks>
    [TestClass]
    [DoNotParallelize]
    public class MixtureDistributionGridSafetyTests
    {
        /// <summary>
        /// Releases resources created on this test's STA before another test uses its own STA.
        /// </summary>
        [TestCleanup]
        public void Cleanup()
        {
            WpfTestHost.ReleaseResources();
        }

        /// <summary>
        /// Keeps the column-header sort menu off: the grid rows are identity references that cannot be compared,
        /// and a sorted view would no longer follow the component order.
        /// </summary>
        [STATestMethod]
        public void DistributionGrid_DoesNotOfferHeaderSorting()
        {
            using var fixture = new GridFixture();
            Assert.IsFalse(fixture.Grid.ShowSortContextMenu,
                "Sorting compares MixtureDistributionRow items, which throws and terminates the application.");
        }

        /// <summary>
        /// Deletes the second equal-valued default row and renders the result without corrupting WPF's generator.
        /// </summary>
        [STATestMethod]
        public void DeleteSecondDefaultRow_PreservesFirstRowAndRendersThroughUndoRedo()
        {
            using var fixture = new GridFixture();
            object firstRow = fixture.Grid.Items[0];
            fixture.SelectRows(1);
            fixture.Grid.DeleteRows();
            fixture.Render();

            fixture.AssertCounts(1);
            Assert.AreSame(firstRow, fixture.Grid.Items[0], "Deleting the second row must retain the first row identity.");
            Assert.IsTrue(IsItemsSourceBound(fixture.Grid));
            Assert.AreEqual(1, fixture.Element.UndoManager.UndoStack.Count);

            fixture.Element.UndoManager.Undo();
            fixture.Render();
            fixture.AssertCounts(2);
            Assert.IsFalse(fixture.Element.UndoManager.CanUndo, "One row deletion is one undo action.");
            fixture.Element.UndoManager.Redo();
            fixture.Render();
            fixture.AssertCounts(1);
        }

        /// <summary>
        /// Verifies equal distribution values remain distinct selectable and editable rows.
        /// </summary>
        [STATestMethod]
        public void SecondDuplicateRow_SelectsAndEditsOnlySecondComponent()
        {
            using var fixture = new GridFixture();
            object firstRow = fixture.Grid.Items[0];
            object secondRow = fixture.Grid.Items[1];
            fixture.SelectRows(1);
            CollectionAssert.AreEqual(new[] { 1 }, fixture.Grid.GetRowsWithSelectedCells().ToArray());

            var combo = FindDescendant<ComboBox>(fixture.Grid.GetCell(1, 0));
            Assert.IsNotNull(combo);
            // XGoodFit port: the item, as a click selects it (see the remarks).
            combo.SelectedItem = combo.Items.Cast<UnivariateDistributionItem>().Single(i => i.Value == UnivariateDistributionType.Gumbel);
            fixture.Render();

            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Normal, UnivariateDistributionType.Gumbel },
                fixture.Element.Distributions.ToArray());
            Assert.AreSame(firstRow, fixture.Grid.Items[0]);
            Assert.AreSame(secondRow, fixture.Grid.Items[1], "Changing a distribution type retains its row identity.");
            fixture.Element.UndoManager.Undo();
            fixture.Render();
            Assert.AreEqual(UnivariateDistributionType.Normal, fixture.Element.Distributions[1]);
            fixture.Element.UndoManager.Redo();
            fixture.Render();
            Assert.AreEqual(UnivariateDistributionType.Gumbel, fixture.Element.Distributions[1]);
        }

        /// <summary>
        /// Verifies toolbar operations retain the existing one-to-three component bounds.
        /// </summary>
        [STATestMethod]
        public void AddAndDeleteRows_EnforceExistingComponentBounds()
        {
            using var fixture = new GridFixture();
            fixture.Grid.AddRows(20);
            fixture.Render();
            fixture.AssertCounts(3);
            fixture.SelectRows(0, 1, 2);
            fixture.Grid.DeleteRows();
            fixture.Render();
            fixture.AssertCounts(3);
            fixture.SelectRows(1, 2);
            fixture.Grid.DeleteRows();
            fixture.Render();
            fixture.AssertCounts(1);
            fixture.SelectRows(0);
            fixture.Grid.DeleteRows();
            fixture.Render();
            fixture.AssertCounts(1);
        }

        /// <summary>
        /// Verifies a queued delete cannot operate on a different analysis after an element switch.
        /// </summary>
        [STATestMethod]
        public void DeferredDelete_AfterElementSwitch_LeavesBothAnalysesUnchanged()
        {
            using var fixture = new GridFixture();
            var replacement = new MixtureAnalysis("Replacement", fixture.Collection);
            fixture.SelectRows(1);
            fixture.Grid.DeleteRows();
            fixture.Control.Element = replacement;
            fixture.Render();

            Assert.AreEqual(2, fixture.Element.Distributions.Count);
            Assert.AreEqual(2, replacement.Distributions.Count);
            Assert.AreEqual(2, fixture.Grid.Items.Count);
        }

        /// <summary>
        /// Verifies replacing the public collection invalidates a delete already queued for its previous rows.
        /// </summary>
        [STATestMethod]
        public void DeferredDelete_AfterCollectionReplacement_LeavesBothCollectionsUnchanged()
        {
            using var fixture = new GridFixture();
            var previous = fixture.Element.Distributions;
            var replacement = new ObservableCollection<UnivariateDistributionType>
            {
                UnivariateDistributionType.Gumbel, UnivariateDistributionType.Normal
            };
            fixture.SelectRows(1);
            fixture.Grid.DeleteRows();
            fixture.Element.Distributions = replacement;
            fixture.Render();

            Assert.AreEqual(2, previous.Count);
            CollectionAssert.AreEqual(new[] { UnivariateDistributionType.Gumbel, UnivariateDistributionType.Normal },
                replacement.ToArray());
            fixture.AssertCounts(2);
        }

        /// <summary>
        /// Verifies a queued deletion follows its captured row identity after intervening collection edits.
        /// </summary>
        [STATestMethod]
        public void DeferredDelete_AfterEarlierRowRemoval_DoesNotDeleteTheRemainingRow()
        {
            using var fixture = new GridFixture();
            fixture.Grid.AddRows(1);
            fixture.Render();
            object retainedRow = fixture.Grid.Items[2];
            fixture.SelectRows(1);
            fixture.Grid.DeleteRows();
            fixture.Element.Distributions.RemoveAt(0);
            fixture.Render();

            fixture.AssertCounts(1);
            Assert.AreSame(retainedRow, fixture.Grid.Items[0]);
        }

        /// <summary>
        /// Verifies replacing or resetting the public enum collection updates the persistent grid source.
        /// </summary>
        [STATestMethod]
        public void CollectionReplacementAndReset_RefreshRowsAndDetachPreviousCollection()
        {
            using var fixture = new GridFixture();
            var previous = fixture.Element.Distributions;
            var replacement = new ObservableCollection<UnivariateDistributionType>
            {
                UnivariateDistributionType.Gumbel, UnivariateDistributionType.Normal
            };
            fixture.Element.Distributions = replacement;
            fixture.Render();
            object source = fixture.Grid.ItemsSource;
            previous.Add(UnivariateDistributionType.Normal);
            fixture.Render();
            fixture.AssertCounts(2);

            replacement.Clear();
            replacement.Add(UnivariateDistributionType.Normal);
            fixture.Render();
            fixture.AssertCounts(1);
            Assert.AreSame(source, fixture.Grid.ItemsSource);
            Assert.IsTrue(IsItemsSourceBound(fixture.Grid));
        }

        /// <summary>
        /// XGoodFit port: whether the grid's items come from a binding (WPF's BindingOperations.IsDataBound).
        /// </summary>
        /// <param name="grid">The grid.</param>
        /// <returns>True if its ItemsSource is bound.</returns>
        private static bool IsItemsSourceBound(CopyPasteDataGrid grid) =>
            BindingOperations.GetBindingExpressionBase(grid, DataGrid.ItemsSourceProperty) != null;

        /// <summary>
        /// Finds the first realized descendant of the requested type.
        /// </summary>
        /// <typeparam name="T">The descendant type.</typeparam>
        /// <param name="root">The visual subtree root.</param>
        /// <returns>The matching descendant, or null if none is realized.</returns>
        /// <remarks>XGoodFit port: Avalonia's visual tree (WPF's VisualTreeHelper).</remarks>
        private static T FindDescendant<T>(Visual root) where T : Visual
        {
            if (root == null) return null;
            if (root is T match) return match;
            return root.GetVisualDescendants().OfType<T>().FirstOrDefault();
        }

        /// <summary>
        /// Owns an unestimated mixture and its actual, realized properties grid for one test.
        /// </summary>
        private sealed class GridFixture : IDisposable
        {
            /// <summary>XGoodFit port: the window that shows the panel (see the class's remarks).</summary>
            private readonly Window _window;

            /// <summary>
            /// Creates the production control without adding an element to persistent project storage.
            /// </summary>
            internal GridFixture()
            {
                WpfTestHost.EnsureResources();
                Collection = new UnivariateAnalysisCollection(BestFitProject.GetInstance());
                Element = new MixtureAnalysis("GridProbe", Collection);
                Control = new MixtureAnalysisPropertiesControl { Element = Element };
                Grid = Control.FindControl<CopyPasteDataGrid>("DistributionDataGrid");
                _window = new Window { Width = 850, Height = 1100, Content = Control };
                _window.Show();
                Render();
                Element.UndoManager.Clear();
            }

            /// <summary>Gets the transient parent collection.</summary>
            internal UnivariateAnalysisCollection Collection { get; }

            /// <summary>Gets the unestimated analysis.</summary>
            internal MixtureAnalysis Element { get; }

            /// <summary>Gets the production properties control.</summary>
            internal MixtureAnalysisPropertiesControl Control { get; }

            /// <summary>Gets the actual toolbar-compatible grid.</summary>
            internal CopyPasteDataGrid Grid { get; }

            /// <summary>
            /// Realizes cells and executes deferred row edits and layout callbacks.
            /// </summary>
            /// <remarks>XGoodFit port: the headless session's layout and rendering (WPF's Measure, Arrange and UpdateLayout).</remarks>
            internal void Render()
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                WpfTestHost.DrainDispatcher();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
                WpfTestHost.DrainDispatcher();
            }

            /// <summary>
            /// Selects actual realized cells so framework row lookup participates in the test.
            /// </summary>
            /// <param name="indices">The physical row indices to select.</param>
            internal void SelectRows(params int[] indices)
            {
                // XGoodFit port: ProDataGrid's cell information names the row's item and the column.
                DataGridColumn column = Grid.Columns[0];
                Grid.SelectedCells = indices
                    .Select(index =>
                    {
                        Assert.IsNotNull(Grid.GetCell(index, 0), "row " + index + "'s cell is realized");
                        return new DataGridCellInfo(Grid.Items[index], column, index, 0);
                    })
                    .ToList();
            }

            /// <summary>
            /// Checks that the grid, public collection, and model agree about the component count.
            /// </summary>
            /// <param name="expected">The expected number of components.</param>
            internal void AssertCounts(int expected)
            {
                Assert.AreEqual(expected, Grid.Items.Count);
                Assert.AreEqual(expected, Element.Distributions.Count);
                Assert.AreEqual(expected, Element.MixtureDistribution.Mixture.Distributions.Length);
            }

            /// <summary>
            /// Detaches element-scoped subscriptions and drains binding work before the test exits.
            /// </summary>
            public void Dispose()
            {
                Control.Element = null;
                WpfTestHost.DrainDispatcher();
                // XGoodFit port: the panel's window closes too.
                _window.Close();
                WpfTestHost.DrainDispatcher();
            }
        }
    }
}
