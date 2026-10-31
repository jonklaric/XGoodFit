using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Avalonia.Controls;
using Avalonia.VisualTree;
using FrameworkUI.ProjectExplorer;
using GenericControls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;
using static RMC.BestFit.App.Tests.XGoodFit.WalkthroughTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the Input Data walkthroughs' common steps (examples/2-input-data/*/*.md), through the main window:
    /// the data frame's grid, statistics and plots as a user reads them, the walkthroughs' figures, and new inputs.
    /// </summary>
    internal static class WalkthroughInputDataTesting
    {
        /// <summary>The Input Data view's left-hand tabs.</summary>
        internal static TabControl MainTabs(InputDataControl view) => view.FindControl<TabControl>("MainTabControl");

        /// <summary>The Data Frame tab's tabs: Exact, Uncertain, Interval and Threshold Data.</summary>
        internal static TabControl DataTabs(InputDataControl view) => view.FindControl<TabControl>("DataTabControl");

        /// <summary>The Exact Data grid, with the Data Frame and Exact Data tabs shown.</summary>
        internal static ValidationDataGrid ExactData(InputDataControl view)
        {
            Select(Tab(MainTabs(view), "Data Frame"));
            Select(Tab(DataTabs(view), "Exact Data"));
            return view.FindControl<ValidationDataGrid>("ExactDataGrid");
        }

        /// <summary>The rows the Threshold Data grid shows.</summary>
        internal static int ThresholdRows(InputDataControl view)
        {
            Select(Tab(MainTabs(view), "Data Frame"));
            Select(Tab(DataTabs(view), "Threshold Data"));
            int rows = view.FindControl<ValidationDataGrid>("ThresholdDataGrid").Items.Count;
            Select(Tab(DataTabs(view), "Exact Data"));
            return rows;
        }

        /// <summary>The Summary Statistics tab's measures of the exact data, as it shows them, by name.</summary>
        internal static Dictionary<string, string> SummaryStatistics(InputDataControl view)
        {
            Select(Tab(MainTabs(view), "Summary Statistics"));
            Dictionary<string, string> shown = Statistics(view.FindControl<CopyPasteDataGrid>("SummaryStatisticsDataGrid"));
            Select(Tab(MainTabs(view), "Data Frame"));
            return shown;
        }

        /// <summary>
        /// Shows a plot as a user picks it: the Data Frame's Chronology Plot or Frequency Plot, or the POT Diagnostics' Mean
        /// Residual Life, Modified Scale or Shape (the radio buttons below the plots).
        /// </summary>
        internal static OxyPlot.Wpf.Plot ShowPlot(InputDataControl view, string choice)
        {
            (string tab, string radio) = choice switch
            {
                "Chronology Plot" => ("Data Frame", "ChronologyPlotRadioButton"),
                "Frequency Plot" => ("Data Frame", "FrequencyPlotRadioButton"),
                "Mean Residual Life" => ("POT Diagnostics", "MRLPlotRadioButton"),
                "Modified Scale" => ("POT Diagnostics", "ModifiedScalePlotRadioButton"),
                "Shape" => ("POT Diagnostics", "ShapePlotRadioButton"),
                _ => throw new ArgumentException(choice),
            };
            Select(Tab(MainTabs(view), tab));
            var button = view.FindControl<RadioButton>(radio);
            Assert.AreEqual(choice, button.Content as string, "the selector's label");
            Assert.IsTrue(button.IsEffectivelyVisible, choice + " is offered below the plot");
            button.IsChecked = true;
            BestFitShell.Frame();
            return view.GetCurrentPlot();
        }

        /// <summary>The (index, value) points a plot's series shows, by its title.</summary>
        internal static List<(double X, double Y)> Series(OxyPlot.Wpf.Plot plot, string title) =>
            Points(plot.Series.Single(s => s.Title == title).InternalSeries, "ActualPoints");

        /// <summary>
        /// Each figure of a walkthrough (images/*.plotspec.json.gz beside the example, of an Input Data element) is what the
        /// view of its element shows with the figure's plot chosen, series by series and point by point.
        /// </summary>
        /// <returns>The figures compared, by file name.</returns>
        internal static List<string> AssertFigures(WalkthroughSession session, string folder, string prefix)
        {
            var compared = new List<string>();
            string images = Path.Combine(SharedControlsTesting.ExamplesFolder, folder, "images");
            foreach (string file in Directory.GetFiles(images, prefix + "*.plotspec.json.gz").OrderBy(f => f, StringComparer.Ordinal))
            {
                JsonElement figure = Data(folder + "images/" + Path.GetFileName(file));
                string choice = figure.GetProperty("plotId").GetString() switch
                {
                    "input_data.chronology" => "Chronology Plot",
                    "input_data.frequency" => "Frequency Plot",
                    "input_data.mean_residual_life" => "Mean Residual Life",
                    "input_data.modified_scale" => "Modified Scale",
                    "input_data.shape" => "Shape",
                    string other => throw new AssertFailedException(file + ": a figure of " + other),
                };
                var view = session.OpenDocument<InputDataControl>(figure.GetProperty("source").GetProperty("id").GetString());
                try
                {
                    AssertFigureData(ShowPlot(view, choice), figure, Path.GetFileName(file));
                    compared.Add(Path.GetFileName(file));
                }
                finally
                {
                    session.CloseDocuments();
                }
            }
            return compared;
        }

        /// <summary>
        /// A native export of a time series' plot beside an Input Data example (screenshots/*-native.json.gz) is what the
        /// series' Time Series tab shows.
        /// </summary>
        internal static void AssertNativeTimeSeries(WalkthroughSession session, string folder, string capture)
        {
            JsonElement native = Data(folder + "screenshots/" + capture);
            var view = session.OpenDocument<TimeSeriesControl>(native.GetProperty("element").GetString());
            try
            {
                WalkthroughTimeSeriesTesting.Show(view, "Time Series");
                Assert.AreSame(view.TimeSeriesPlot, view.GetCurrentPlot());
                NativeCaptures.AssertPlotMatches(view.TimeSeriesPlot, native, 1E-12);
            }
            finally
            {
                session.CloseDocuments();
            }
        }

        /// <summary>
        /// Adds an input as the walkthroughs say: right-click Input Data in the explorer, choose New Input Data..., and enter
        /// a name. Its document opens, with its properties beside it.
        /// </summary>
        internal static InputData NewInputData(WalkthroughSession session, string name)
        {
            ElementNodeCollection collection = session.Node.ChildNodes.OfType<ElementNodeCollection>().Single(c => c.ElementCollection is InputDataCollection);
            int count = collection.ElementCollection.Count;
            session.Window.RightClick(collection.NodeHeader);
            Assert.IsTrue(collection.ContextMenu!.IsOpen, "a right click opens the collection's menu");
            MenuItem item = collection.ContextMenu.Items.OfType<MenuItem>().Single(m => m.Header as string == "New Input Data...");
            NameDialog dialog = null;
            using (session.Expect<NameDialog>())
            using (BestFitShell.WhenOpened<NameDialog>(opened =>
            {
                dialog = opened;
                opened.Text = name;
                BestFitShell.ClickButton(opened, "OK");
            }))
            {
                BestFitShell.Click(item);
            }
            collection.ContextMenu.Close();
            BestFitShell.Frame();
            Assert.IsNotNull(dialog, "New Input Data... asks for a name");
            Assert.AreEqual(count + 1, collection.ElementCollection.Count, "an input is added");
            var added = (InputData)collection.ElementCollection[count];
            Assert.AreEqual(name, added.Name);
            session.OpenDocument<InputDataControl>(added);
            Assert.AreSame(added, ((InputDataPropertiesControl)session.Properties).Element, "its properties show");
            return added;
        }

        /// <summary>Picks the item of a properties row's combo box that shows <paramref name="text"/>, as a user does.</summary>
        internal static void Choose(ContentPropertyControl row, string text)
        {
            object content = row.InnerContent;
            var box = content as ComboBox ?? (ComboBox)((Border)content).Child;
            object item = box.Items.Cast<object>().SingleOrDefault(i => ItemText(i) == text);
            Assert.IsNotNull(item, row.Title + " offers " + text + ": " + string.Join(", ", box.Items.Cast<object>().Select(ItemText)));
            box.SelectedItem = item;
            BestFitShell.Frame();
        }

        private static string ItemText(object item) =>
            item.GetType().GetProperty("DisplayName")?.GetValue(item) as string
            ?? item.GetType().GetProperty("Name")?.GetValue(item) as string
            ?? item.ToString();

        /// <summary>Whether a properties row's check box (the Multiple Grubbs-Beck Test's) is ticked.</summary>
        internal static bool IsTicked(BooleanPropertyControl row) => row.GetVisualDescendants().OfType<CheckBox>().Single().IsChecked == true;

        /// <summary>Presses a properties panel's Process (or Download) button and waits for it to finish.</summary>
        internal static void Process(InputDataPropertiesControl properties)
        {
            var button = properties.FindControl<Button>("ProcessDataButton");
            Click(button);
            BestFitShell.WaitUntil(() => button.IsEnabled && WpfCompat.Input.Mouse.OverrideCursor == null, TimeSpan.FromMinutes(2), "the processing");
            BestFitShell.Frame();
        }
    }
}
