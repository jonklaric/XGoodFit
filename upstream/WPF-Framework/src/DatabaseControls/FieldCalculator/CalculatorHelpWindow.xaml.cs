using System;
using System.IO;
using System.Reflection;

namespace DatabaseControls
{
    /// <summary>
    /// A window that displays help documentation for the calculator functionality.
    /// Loads embedded HTML help resources for FieldCalculator or SelectByAttribute.
    /// </summary>
    public partial class CalculatorHelpWindow : GenericControls.MetroWindow
    {
        private readonly string _helpResourcePrefix;

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
            ContentRendered += CalculatorHelpWindow_ContentRendered;
            Closing += CalculatorHelpWindow_Closing;
        }

        /// <summary>
        /// Disposes the embedded help browser when the help window closes.
        /// </summary>
        /// <param name="sender">The help window that raised the event.</param>
        /// <param name="e">The closing event data.</param>
        private void CalculatorHelpWindow_Closing(object? sender, System.ComponentModel.CancelEventArgs e)
        {
            HelpBrowser?.Dispose();
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
                HelpBrowser.NavigateToString("<html><body><p>Help content not available.</p></body></html>");
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
                HelpBrowser.Navigate(new Uri(htmlPath));
            }
            else
            {
                HelpBrowser.NavigateToString("<html><body><p>Help content not available.</p></body></html>");
            }
        }
    }
}
