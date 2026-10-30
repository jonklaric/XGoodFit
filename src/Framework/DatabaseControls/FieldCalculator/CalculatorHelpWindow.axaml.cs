using System;
using System.IO;
using System.Reflection;
using System.Text;
using Avalonia.Controls;
using TheArtOfDev.HtmlRenderer.Avalonia;
using TheArtOfDev.HtmlRenderer.Core.Entities;

namespace DatabaseControls
{
    /// <summary>
    /// A window that displays help documentation for the calculator functionality.
    /// Loads embedded HTML help resources for FieldCalculator or SelectByAttribute.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: the browser is HtmlRenderer's HtmlPanel (see CalculatorHelpWindow.axaml).
    /// Upstream extracts the page and its pictures to a temporary folder and navigates to the page; here
    /// the panel shows the extracted page's text and loads its pictures from that folder, as a browser
    /// resolves them against the page's address. ContentRendered is Opened.
    /// </remarks>
    public partial class CalculatorHelpWindow : GenericControls.MetroWindow
    {
        private readonly string _helpResourcePrefix;

        // XGoodFit port: the folder of the page shown, which its pictures' relative addresses start from.
        private string? _documentFolder;

        /// <summary>
        /// Initializes a new instance of the <see cref="CalculatorHelpWindow"/> class.
        /// </summary>
        /// <param name="helpType">The type of help to display: "FieldCalculator" or "SelectByAttribute".</param>
        public CalculatorHelpWindow(string helpType = "FieldCalculator")
        {
            InitializeComponent();
            _helpResourcePrefix = helpType == "SelectByAttribute"
                ? "DatabaseControls.Resources.Help.SelectByAttribute."
                : "DatabaseControls.Resources.Help.FieldCalculator.";
            Title = helpType == "SelectByAttribute" ? "Select By Attribute Help" : "Field Calculator Help";
            Opened += CalculatorHelpWindow_ContentRendered; // XGoodFit port: WPF's ContentRendered
            Closing += CalculatorHelpWindow_Closing;
            HelpBrowser.ImageLoad += HelpBrowser_ImageLoad;
        }

        /// <summary>
        /// Disposes the embedded help browser when the help window closes.
        /// </summary>
        /// <param name="sender">The help window that raised the event.</param>
        /// <param name="e">The closing event data.</param>
        private void CalculatorHelpWindow_Closing(object? sender, WindowClosingEventArgs e)
        {
            // XGoodFit port: the panel's container holds the page and its pictures, as WPF's browser did.
            HelpBrowser?.Container.Dispose();
        }

        /// <summary>
        /// Loads embedded help content after the window has rendered.
        /// </summary>
        /// <param name="sender">The help window that raised the event.</param>
        /// <param name="e">The event data.</param>
        private void CalculatorHelpWindow_ContentRendered(object? sender, EventArgs e)
        {
            LoadHelpContent();
        }

        /// <summary>
        /// Loads the selected embedded HTML help file and its image resources.
        /// </summary>
        private void LoadHelpContent()
        {
            var assembly = Assembly.GetExecutingAssembly();
            string htmlResourceName = _helpResourcePrefix +
                (_helpResourcePrefix.Contains("SelectByAttribute") ? "SelectByAttribute.html" : "FieldCalculator.html");

            using var stream = assembly.GetManifestResourceStream(htmlResourceName);
            if (stream == null)
            {
                HelpBrowser.Text = "<html><body><p>Help content not available.</p></body></html>";
                return;
            }

            // Extract HTML and any referenced images to a temp directory
            string tempDir = Path.Combine(Path.GetTempPath(), "DatabaseControls_Help");
            Directory.CreateDirectory(tempDir);

            // Extract all resources with this prefix
            foreach (string name in assembly.GetManifestResourceNames())
            {
                if (!name.StartsWith(_helpResourcePrefix))
                    continue;

                string fileName = name.Substring(_helpResourcePrefix.Length);
                string filePath = Path.Combine(tempDir, fileName);

                using var resourceStream = assembly.GetManifestResourceStream(name);
                if (resourceStream != null)
                {
                    using var fileStream = File.Create(filePath);
                    resourceStream.CopyTo(fileStream);
                }
            }

            string htmlPath = Path.Combine(tempDir, htmlResourceName.Substring(_helpResourcePrefix.Length));
            if (File.Exists(htmlPath))
            {
                // XGoodFit port: WPF's Navigate(new Uri(htmlPath)); the pages declare ISO-8859-1.
                _documentFolder = Path.GetDirectoryName(htmlPath);
                HelpBrowser.Text = File.ReadAllText(htmlPath, Encoding.Latin1);
            }
            else
            {
                HelpBrowser.Text = "<html><body><p>Help content not available.</p></body></html>";
            }
        }

        /// <summary>
        /// XGoodFit addition: loads a picture of the page from the page's folder when its address is relative,
        /// as WPF's browser did for the page it navigated to.
        /// </summary>
        /// <param name="sender">The help browser.</param>
        /// <param name="e">The picture to load.</param>
        private void HelpBrowser_ImageLoad(object? sender, HtmlRendererRoutedEventArgs<HtmlImageLoadEventArgs> e)
        {
            if (_documentFolder is null || string.IsNullOrEmpty(e.Event.Src) || Uri.TryCreate(e.Event.Src, UriKind.Absolute, out _))
                return;
            e.Event.Callback(Path.Combine(_documentFolder, e.Event.Src));
        }
    }
}
