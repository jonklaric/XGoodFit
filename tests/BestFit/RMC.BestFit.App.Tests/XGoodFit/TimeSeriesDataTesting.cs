using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.VisualTree;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlot;
using RMC.BestFit.UI;
using RMC_BestFit;
using WpfCompat;
using static RMC.BestFit.App.Tests.XGoodFit.SharedControlsTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// Sets the thread's culture for a test, and restores it. The examples were saved in en-US, and RMC-BestFit
    /// reads their dates in the current culture (as the file-compatibility suite notes), so the tests that open
    /// them run in en-US, as the native screenshots' machine did.
    /// </summary>
    internal sealed class CultureScope : IDisposable
    {
        private readonly CultureInfo _culture = CultureInfo.CurrentCulture;
        private readonly CultureInfo _uiCulture = CultureInfo.CurrentUICulture;

        public CultureScope(string name)
        {
            CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(name);
            CultureInfo.CurrentUICulture = CultureInfo.GetCultureInfo(name);
        }

        public void Dispose()
        {
            CultureInfo.CurrentCulture = _culture;
            CultureInfo.CurrentUICulture = _uiCulture;
        }
    }

    /// <summary>
    /// XGoodFit addition: the Time Series Data examples (sample-data/RMC-BestFit/examples/1-time-series-data), the
    /// elements their native screenshots show, and the Time Series Data views shown on them as the application
    /// shows them.
    /// </summary>
    internal static class TimeSeriesDataTesting
    {
        public const string Usgs = "1-time-series-data/1-usgs-download/usgs-download-example.bestfit";
        public const string Ghcn = "1-time-series-data/2-ghcn-download/ghcn-download-example.bestfit";
        public const string Chmn = "1-time-series-data/3-chmn-download/chmn-download-example.bestfit";
        public const string Abom = "1-time-series-data/4-abom-download/abom-download-example.bestfit";
        public const string Dss = "1-time-series-data/5-hec-dss-import/hec-dss-import-example.bestfit";
        public const string Manual = "1-time-series-data/6-manual-entry/manual-entry-example.bestfit";

        /// <summary>The examples, in the chapter's order.</summary>
        public static readonly string[] Examples = { Usgs, Ghcn, Chmn, Abom, Dss, Manual };

        /// <summary>
        /// The native plot screenshots (screenshots/&lt;name&gt;-native.png beside each example): the example, the element,
        /// the view's tab (its header) and the alternative shown with the element, if any.
        /// </summary>
        public static readonly (string Example, string Name, string Element, string Tab, string Alternative)[] NativePlots =
        {
            (Usgs, "usgs-daily-discharge-ts-plot", "USGS - 01134500 - Daily Discharge", "Time Series", null),
            (Usgs, "usgs-daily-discharge-seasonality", "USGS - 01134500 - Daily Discharge", "Seasonality Plot", null),
            (Usgs, "usgs-instantaneous-discharge-ts-plot", "USGS - 01646500 - Instantaneous Discharge", "Time Series", null),
            (Usgs, "usgs-measured-discharge-ts-plot", "USGS - 01570500 - Measured Discharge", "Time Series", null),
            (Usgs, "usgs-peak-discharge-ts-plot", "USGS - 01614000 - Peak Discharge", "Time Series", null),
            (Ghcn, "ghcn-precipitation-ts-plot", "GHCN - USC00040741 - Daily Precipitation", "Time Series", null),
            (Ghcn, "ghcn-precipitation-seasonality-plot", "GHCN - USC00040741 - Daily Precipitation", "Seasonality Plot", null),
            (Ghcn, "ghcn-snow-ts-plot", "GHCN - USC00046685 - Daily Snow", "Time Series", null),
            (Chmn, "chmn-daily-discharge-ts-plot", "CHMN - 08MG005 - Daily Discharge", "Time Series", null),
            (Chmn, "chmn-daily-discharge-seasonality", "CHMN - 08MG005 - Daily Discharge", "Seasonality Plot", null),
            (Chmn, "chmn-instantaneous-discharge-ts-plot", "CHMN - 08MG005 - Instantaneous Discharge", "Time Series", null),
            (Chmn, "chmn-peak-discharge-ts-plot", "CHMN - 08MG005 - Peak Discharge", "Time Series", null),
            (Abom, "abom-daily-discharge-ts-plot", "ABOM - 410730 - Daily Discharge", "Time Series", null),
            (Abom, "abom-instantaneous-stage-ts-plot", "ABOM - 409202 - Instantaneous Stage", "Time Series", null),
            (Abom, "abom-precipitation-ts-plot", "ABOM - 410730 - Daily Precipitation", "Time Series", null),
            (Dss, "hec-dss-inflow-ts-plot", "Grapevine Dam - Inflow", "Time Series", null),
            (Dss, "hec-dss-inflow-outflow-comparison", "Grapevine Dam - Inflow", "Time Series", "Grapevine Dam - Outflow"),
            (Manual, "manual-entry-airline-ts-plot", "Airline Passengers", "Time Series", null),
            (Manual, "manual-entry-airline-acf", "Airline Passengers", "ACF Plot", null),
            (Manual, "manual-entry-co2-ts-plot", "Mauna Loa CO2", "Time Series", null),
            (Manual, "manual-entry-nile-ts-plot", "Nile River Flows", "Time Series", null),
        };

        /// <summary>The native properties panel screenshots: the example, the screenshot's name and the element.</summary>
        public static readonly (string Example, string Name, string Element)[] NativeProperties =
        {
            (Usgs, "usgs-properties-panel", "USGS - 01134500 - Daily Discharge"),
            (Ghcn, "ghcn-properties-panel", "GHCN - USC00040741 - Daily Precipitation"),
            (Chmn, "chmn-properties-panel", "CHMN - 08MG005 - Daily Discharge"),
            (Abom, "abom-properties-panel", "ABOM - 410730 - Daily Precipitation"),
            (Dss, "hec-dss-properties-panel", "Grapevine Dam - Inflow"),
            (Manual, "manual-entry-properties", "Airline Passengers"),
        };

        /// <summary>A native screenshot's file beside the example (name with its -native or -current suffix, and extension).</summary>
        public static string NativeScreenshot(string example, string file) =>
            example.Substring(0, example.LastIndexOf('/')) + "/screenshots/" + file;

        /// <summary>
        /// Shows the document view on an element in a window, as MainProjectNode makes it, with a tab selected.
        /// </summary>
        public static (TimeSeriesControl View, Window Window) ShowView(TimeSeriesElement element, string tab = "Time Series", double width = 1400, double height = 720)
        {
            var view = new TimeSeriesControl { Element = element };
            Window window = Show(view, width, height);
            SelectTab(view, tab);
            return (view, window);
        }

        /// <summary>Selects one of the view's tabs by its header, as a user clicks it.</summary>
        public static void SelectTab(TimeSeriesControl view, string header)
        {
            var tabs = view.FindControl<TabControl>("MainTabControl");
            tabs.SelectedItem = tabs.Items.OfType<TabItem>().Single(t => (string)t.Header == header);
            Settle();
        }

        /// <summary>The visible texts of a control's text blocks and text boxes, in the visual tree's order.</summary>
        public static List<string> VisibleTexts(Control root)
        {
            var texts = new List<string>();
            foreach (var visual in root.GetVisualDescendants())
            {
                if (visual is not Control control || !IsShown(control, root)) continue;
                if (control is TextBox box)
                    texts.Add(box.Text ?? "");
                else if (control is TextBlock block && block.FindAncestorOfType<TextBox>() == null && !string.IsNullOrEmpty(block.Text))
                    texts.Add(block.Text);
            }
            return texts;
        }

        private static bool IsShown(Control control, Control root)
        {
            for (Visual visual = control; visual != null && visual != root; visual = visual.GetVisualParent())
            {
                if (!visual.IsVisible || visual.Opacity == 0) return false;
            }
            return true;
        }

        /// <summary>A date as WPF's binding culture (en-US) shows it.</summary>
        public static string UsDate(DateTime date) => date.ToString(FormattingCulture.Current);

        /// <summary>The plot's series' titles, in order.</summary>
        public static string Titles(OxyPlot.Wpf.Plot plot) => string.Join(" | ", plot.Series.Select(s => s.Title));

        /// <summary>An OxyPlot data point's text, for comparisons.</summary>
        public static string Text(DataPoint point) => point.X.ToString("R", CultureInfo.InvariantCulture) + "," + point.Y.ToString("R", CultureInfo.InvariantCulture);

        /// <summary>Fails with the details when a value isn't within a relative tolerance of the expected one.</summary>
        public static void AssertClose(double expected, double actual, string what, double tolerance = 1e-9)
        {
            double scale = Math.Max(1, Math.Abs(expected));
            Assert.IsTrue(Math.Abs(expected - actual) <= tolerance * scale, $"{what}: expected {expected}, was {actual}");
        }
    }
}
