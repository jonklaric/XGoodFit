using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using GenericControls;
using Microsoft.Win32;
using OxyPlot.Wpf;

namespace OxyPlotControls
{
    /// <summary>
    /// A dialog window for exporting OxyPlot charts.
    /// Provides a live plot preview, export dimensions, report-theme selection,
    /// and delegates file name, type, overwrite, and folder selection to the
    /// native Windows Save As dialog.
    /// </summary>
    public partial class SavePlotImageDialog : MetroDialogWindow
    {
        private const string SaveFileFilter = "PNG Image (*.png)|*.png|PDF Document (*.pdf)|*.pdf|SVG Vector Image (*.svg)|*.svg";
        private const int PngFilterIndex = 1;
        private const int PdfFilterIndex = 2;
        private const int SvgFilterIndex = 3;
        private const int DefaultFileNameMaxLength = 200;

        private readonly Plot _sourcePlot;
        private readonly DispatcherTimer _resizeDebounceTimer;
        private bool _isInitialized;
        private bool _useReportTheme = true;
        private static string _lastUsedFolderPath = string.Empty;
        private double _desiredPreviewWidth;
        private double _desiredPreviewHeight;

        // Layout constants for dialog sizing calculations.
        // These estimate the non-preview space so the preview can scale to the selected export size.
        private const double ControlsPanelHeight = 110;
        private const double DialogChromeHeight = 50;
        private const double DialogHorizontalPadding = 36;
        private const double MinPreviewWidth = 280;
        private const double MinPreviewHeight = 180;
        private const double ScreenUsageFraction = 0.95;

        /// <summary>
        /// Initializes a new instance of the <see cref="SavePlotImageDialog"/> class.
        /// </summary>
        /// <param name="thePlot">The OxyPlot Plot control to save as an image.</param>
        public SavePlotImageDialog(Plot thePlot)
        {
            InitializeComponent();

            _sourcePlot = thePlot;

            SetThemeAwareIcon();

            _resizeDebounceTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(300)
            };
            _resizeDebounceTimer.Tick += ResizeDebounceTimer_Tick;

            // Compute correct initial size from the default combo selection before the window is shown,
            // so WindowStartupLocation="CenterOwner" centers at the right size with no flash/shift.
            ParseDimensionsFromComboBox();
            if (TryGetExportDimensions(out int initWidth, out int initHeight))
            {
                var (w, h, pw, ph) = ComputeDialogSize(initWidth, initHeight);
                Width = w;
                Height = h;
                _desiredPreviewWidth = pw;
                _desiredPreviewHeight = ph;
            }

            ContentRendered += SavePlotImageDialog_ContentRendered;
            Closing += SavePlotImageDialog_Closing;
        }

        /// <summary>
        /// Handles the ContentRendered event to initialize the dialog and render the first preview.
        /// </summary>
        private void SavePlotImageDialog_ContentRendered(object? sender, EventArgs e)
        {
            WidthTextBox.ToolTip = "Current Plot Width is " + ((int)_sourcePlot.ActualWidth).ToString() + " px";
            HeightTextBox.ToolTip = "Current Plot Height is " + ((int)_sourcePlot.ActualHeight).ToString() + " px";

            _isInitialized = true;

            // Defer first preview render until layout is complete.
            Dispatcher.BeginInvoke(DispatcherPriority.Loaded, new Action(UpdatePreview));
        }

        /// <summary>
        /// Handles the Closing event to activate the owner window.
        /// </summary>
        private void SavePlotImageDialog_Closing(object? sender, CancelEventArgs e)
        {
            // Stop the resize-debounce timer so a pending Tick doesn't fire after the
            // visual tree has been torn down and UpdatePreview() accesses disposed elements.
            _resizeDebounceTimer.Stop();
            _resizeDebounceTimer.Tick -= ResizeDebounceTimer_Tick;

            if (Owner != null) Owner.Activate();
        }

        /// <summary>
        /// Creates and sets a theme-aware vector icon for the dialog window.
        /// Uses the EnvironmentWindowText color so the icon adapts to light/dark themes.
        /// </summary>
        private void SetThemeAwareIcon()
        {
            try
            {
                var iconBrush = FindResource("EnvironmentWindowText") as SolidColorBrush ?? Brushes.Black;

                // Camera/save icon: simple camera shape matching the toolbar CameraIcon.
                var geometry = Geometry.Parse(
                    "F0 M 5.40625 1.828125 C 5.2435365 1.828125 5.1126068 1.9997109 5.03125 2.140625 " +
                    "C 4.7248392 2.671344 4.09375 3.734375 4.09375 3.734375 L 2.4375 3.734375 " +
                    "C 1.5394159 3.734375 0 5.1192214 0 6.265625 L 0 12.015625 " +
                    "C 0 13.322471 1.2946973 14.734375 2.40625 14.734375 L 7.96875 14.734375 " +
                    "L 13.53125 14.734375 C 14.642803 14.734375 16 13.322471 16 12.015625 " +
                    "L 16 6.265625 C 16 5.1192214 14.398084 3.734375 13.5 3.734375 " +
                    "L 11.84375 3.734375 C 11.84375 3.734375 11.212661 2.671344 10.90625 2.140625 " +
                    "C 10.82489 1.9997109 10.693963 1.828125 10.53125 1.828125 " +
                    "L 7.96875 1.828125 L 5.40625 1.828125 z " +
                    "M 8 4.8203125 C 10.421762 4.8203125 12.400391 6.7989415 12.400391 9.2207031 " +
                    "C 12.400391 11.642465 10.421762 13.619141 8 13.619141 " +
                    "C 5.5782383 13.619141 3.5996094 11.642465 3.5996094 9.2207031 " +
                    "C 3.5996094 6.7989415 5.5782383 4.8203125 8 4.8203125 z " +
                    "M 8 6.2207031 C 6.3348545 6.2207031 5 7.5555576 5 9.2207031 " +
                    "C 5 10.885849 6.3348545 12.220703 8 12.220703 " +
                    "C 9.6651455 12.220703 11 10.885849 11 9.2207031 " +
                    "C 11 7.5555576 9.6651455 6.2207031 8 6.2207031 z");

                var drawing = new GeometryDrawing(iconBrush, null, geometry);
                var drawingImage = new DrawingImage(drawing);
                drawingImage.Freeze();
                Icon = drawingImage;
            }
            catch
            {
                // Fall back gracefully - no icon if resource resolution fails.
            }
        }

        /// <summary>
        /// Handles the SelectionChanged event of the ImageSizeComboBox to update width and height fields
        /// and resize the dialog.
        /// </summary>
        private void ImageSizeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!_isInitialized) return;
            if (WidthTextBox == null || HeightTextBox == null) return;
            if (ImageSizeComboBox.SelectedIndex < 0) return;

            bool isCustom = ImageSizeComboBox.SelectedIndex == ImageSizeComboBox.Items.Count - 1;
            CustomDimensionsPanel.Visibility = isCustom ? Visibility.Visible : Visibility.Collapsed;
            WidthTextBox.IsEnabled = isCustom;
            HeightTextBox.IsEnabled = isCustom;

            if (isCustom)
            {
                WidthTextBox.Text = ((int)_sourcePlot.ActualWidth).ToString();
                HeightTextBox.Text = ((int)_sourcePlot.ActualHeight).ToString();
            }
            else
            {
                ParseDimensionsFromComboBox();
            }

            UpdateDialogSize();
            UpdatePreview();
        }

        /// <summary>
        /// Parses width and height values from the currently selected ComboBox preset item.
        /// </summary>
        private void ParseDimensionsFromComboBox()
        {
            if (ImageSizeComboBox.SelectedIndex < 0) return;
            if (ImageSizeComboBox.SelectedIndex == ImageSizeComboBox.Items.Count - 1) return;

            var size = ((ComboBoxItem)ImageSizeComboBox.SelectedItem).Content.ToString()!;
            var sizes = size.Split(' ');
            if (sizes.Length >= 3)
            {
                WidthTextBox.Text = sizes[0];
                HeightTextBox.Text = sizes[2];
            }
        }

        /// <summary>
        /// Handles dimension text changes with debouncing.
        /// </summary>
        private void DimensionTextChanged(object sender, TextChangedEventArgs e)
        {
            if (!_isInitialized) return;
            _resizeDebounceTimer.Stop();
            _resizeDebounceTimer.Start();
        }

        /// <summary>
        /// Handles the Checked/Unchecked events of the ReportThemeCheckBox to toggle the report theme for preview and export.
        /// </summary>
        private void ReportThemeCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            _useReportTheme = ReportThemeCheckBox.IsChecked == true;
            if (_isInitialized)
                UpdatePreview();
        }

        /// <summary>
        /// Fires after the debounce delay to update the dialog size and preview.
        /// </summary>
        private void ResizeDebounceTimer_Tick(object? sender, EventArgs e)
        {
            _resizeDebounceTimer.Stop();
            UpdateDialogSize();
            UpdatePreview();
        }

        /// <summary>
        /// Computes the optimal dialog dimensions for the given export size.
        /// Used both for initial sizing before the window is shown and subsequent resizes.
        /// </summary>
        private static (double dialogWidth, double dialogHeight, double previewWidth, double previewHeight) ComputeDialogSize(int exportWidth, int exportHeight)
        {
            var workArea = SystemParameters.WorkArea;
            double maxDialogWidth = workArea.Width * ScreenUsageFraction;
            double maxDialogHeight = workArea.Height * ScreenUsageFraction;

            double availablePreviewWidth = maxDialogWidth - DialogHorizontalPadding;
            double availablePreviewHeight = maxDialogHeight - ControlsPanelHeight - DialogChromeHeight;

            // Scale to fit and never upscale beyond the export dimensions.
            double scaleX = availablePreviewWidth / exportWidth;
            double scaleY = availablePreviewHeight / exportHeight;
            double scale = Math.Min(scaleX, scaleY);
            if (scale > 1.0) scale = 1.0;

            double previewWidth = Math.Max(exportWidth * scale, MinPreviewWidth);
            double previewHeight = Math.Max(exportHeight * scale, MinPreviewHeight);

            double dialogWidth = previewWidth + DialogHorizontalPadding;
            double dialogHeight = previewHeight + ControlsPanelHeight + DialogChromeHeight;

            dialogWidth = Math.Clamp(dialogWidth, 500, maxDialogWidth);
            dialogHeight = Math.Clamp(dialogHeight, 450, maxDialogHeight);

            return (dialogWidth, dialogHeight, previewWidth, previewHeight);
        }

        /// <summary>
        /// Resizes the dialog to reflect the selected export dimensions while maintaining aspect ratio.
        /// The dialog is capped at 95% of the screen working area.
        /// </summary>
        private void UpdateDialogSize()
        {
            if (!_isInitialized) return;
            if (!TryGetExportDimensions(out int exportWidth, out int exportHeight)) return;

            var (dialogWidth, dialogHeight, previewWidth, previewHeight) = ComputeDialogSize(exportWidth, exportHeight);
            Width = dialogWidth;
            Height = dialogHeight;
            _desiredPreviewWidth = previewWidth;
            _desiredPreviewHeight = previewHeight;

            if (Owner != null)
            {
                var workArea = SystemParameters.WorkArea;
                double newLeft = Owner.Left + (Owner.ActualWidth - Width) / 2;
                double newTop = Owner.Top + (Owner.ActualHeight - Height) / 2;

                Left = Math.Max(workArea.Left, Math.Min(newLeft, workArea.Right - Width));
                Top = Math.Max(workArea.Top, Math.Min(newTop, workArea.Bottom - Height));
            }
        }

        /// <summary>
        /// Renders the plot preview at the appropriate size within the preview container.
        /// Uses OxyPlot's PngExporter to generate a WYSIWYG bitmap preview.
        /// </summary>
        private void UpdatePreview()
        {
            if (!_isInitialized) return;
            if (_sourcePlot?.ActualModel == null) return;
            if (!TryGetExportDimensions(out int exportWidth, out int exportHeight)) return;

            if (_desiredPreviewWidth <= 0 || _desiredPreviewHeight <= 0) return;

            int previewWidth = Math.Max((int)_desiredPreviewWidth, 100);
            int previewHeight = Math.Max((int)_desiredPreviewHeight, 100);

            PreviewImage.Width = previewWidth;
            PreviewImage.Height = previewHeight;

            try
            {
                if (_useReportTheme)
                {
                    OxyPlotThemeManager.WithThemedModel(_sourcePlot.ActualModel, OxyPlotThemeManager.ReportTheme, model =>
                    {
                        var exporter = new PngExporter
                        {
                            Width = previewWidth,
                            Height = previewHeight,
                            Background = model.Background
                        };
                        PreviewImage.Source = exporter.ExportToBitmap(model);
                    });
                }
                else
                {
                    var exporter = new PngExporter
                    {
                        Width = previewWidth,
                        Height = previewHeight,
                        Background = _sourcePlot.ActualModel.Background
                    };
                    PreviewImage.Source = exporter.ExportToBitmap(_sourcePlot.ActualModel);
                }
            }
            catch (Exception)
            {
                // Preview is helpful but not required for export.
            }
        }

        /// <summary>
        /// Handles the SizeChanged event of the preview container to update the preview.
        /// </summary>
        private void PreviewContainer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!_isInitialized) return;
            UpdatePreview();
        }

        /// <summary>
        /// Tries to parse the current width and height values from the dimension controls.
        /// </summary>
        private bool TryGetExportDimensions(out int width, out int height)
        {
            width = 0;
            height = 0;

            if (!WidthTextBox.ValueIsValid || !HeightTextBox.ValueIsValid) return false;

            width = WidthTextBox.GetValueAsInteger();
            height = HeightTextBox.GetValueAsInteger();

            return width > 0 && height > 0;
        }

        /// <summary>
        /// Handles the Save As button click event to collect a file path and export the plot.
        /// </summary>
        private void SaveAsButton_Click(object sender, RoutedEventArgs e)
        {
            if (!TryValidateExportInputs(out int imageWidth, out int imageHeight)) return;

            var saveFileDialog = CreateSaveFileDialog();
            if (saveFileDialog.ShowDialog() != true) return;

            string saveFile = GetExportFilePath(saveFileDialog.FileName, saveFileDialog.FilterIndex);
            string extension = Path.GetExtension(saveFile).ToLowerInvariant();

            try
            {
                if (_useReportTheme)
                {
                    OxyPlotThemeManager.WithThemedModel(_sourcePlot.ActualModel, OxyPlotThemeManager.ReportTheme, model =>
                    {
                        ExportModel(model, saveFile, extension, imageWidth, imageHeight);
                    });
                }
                else
                {
                    ExportModel(_sourcePlot.ActualModel, saveFile, extension, imageWidth, imageHeight);
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show("Error occurred attempting to save the image file: " + ex.Message, "Cannot Save", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            _lastUsedFolderPath = Path.GetDirectoryName(saveFile) ?? string.Empty;
            Close();
        }

        /// <summary>
        /// Validates that the plot and requested export dimensions can be saved.
        /// </summary>
        /// <param name="imageWidth">The validated image width.</param>
        /// <param name="imageHeight">The validated image height.</param>
        /// <returns><c>true</c> when export inputs are valid.</returns>
        private bool TryValidateExportInputs(out int imageWidth, out int imageHeight)
        {
            imageWidth = 0;
            imageHeight = 0;

            if (_sourcePlot.ActualModel == null)
            {
                GenericControls.MessageBox.Show("The plot has no model to export.", "Cannot Save", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (!TryGetExportDimensions(out imageWidth, out imageHeight))
            {
                GenericControls.MessageBox.Show("Image dimensions are not valid. Width and height must be between 50 and 10,000 pixels.", "Invalid Dimensions", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Creates the configured save-file dialog for image export.
        /// </summary>
        /// <returns>The configured save-file dialog.</returns>
        private SaveFileDialog CreateSaveFileDialog()
        {
            var saveFileDialog = new SaveFileDialog
            {
                Title = "Save Plot Image As",
                Filter = SaveFileFilter,
                FilterIndex = PngFilterIndex,
                DefaultExt = "png",
                AddExtension = true,
                OverwritePrompt = true,
                ValidateNames = true,
                CheckPathExists = true,
                FileName = GetDefaultFileName(_sourcePlot.Model?.Title ?? _sourcePlot.ActualModel?.Title)
            };

            string initialDirectory = GetInitialDirectory();
            if (Directory.Exists(initialDirectory))
            {
                saveFileDialog.InitialDirectory = initialDirectory;
            }

            return saveFileDialog;
        }

        /// <summary>
        /// Gets the initial folder for the image save dialog.
        /// </summary>
        /// <returns>The last used folder, pictures folder, or documents folder.</returns>
        private static string GetInitialDirectory()
        {
            if (!string.IsNullOrEmpty(_lastUsedFolderPath) && Directory.Exists(_lastUsedFolderPath))
            {
                return _lastUsedFolderPath;
            }

            string picturesPath = Environment.GetFolderPath(Environment.SpecialFolder.MyPictures);
            if (Directory.Exists(picturesPath))
            {
                return picturesPath;
            }

            return Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
        }

        /// <summary>
        /// Builds a safe default image file name from the plot title.
        /// </summary>
        /// <param name="plotTitle">The optional plot title.</param>
        /// <returns>A sanitized file name without an extension.</returns>
        private static string GetDefaultFileName(string? plotTitle)
        {
            string source = string.IsNullOrWhiteSpace(plotTitle) ? "plot" : plotTitle.Trim();
            char[] invalidCharacters = Path.GetInvalidFileNameChars();
            var fileName = new char[source.Length];

            for (int i = 0; i < source.Length; i++)
            {
                fileName[i] = Array.IndexOf(invalidCharacters, source[i]) >= 0 ? '_' : source[i];
            }

            string sanitized = new string(fileName).Trim().TrimEnd('.');
            if (sanitized.Length > DefaultFileNameMaxLength)
            {
                sanitized = sanitized.Substring(0, DefaultFileNameMaxLength).Trim().TrimEnd('.');
            }

            return string.IsNullOrWhiteSpace(sanitized) ? "plot" : sanitized;
        }

        /// <summary>
        /// Ensures the selected export path has a supported extension.
        /// </summary>
        /// <param name="filePath">The path selected by the user.</param>
        /// <param name="filterIndex">The selected file-dialog filter index.</param>
        /// <returns>The export path with a supported extension.</returns>
        private static string GetExportFilePath(string filePath, int filterIndex)
        {
            string extension = Path.GetExtension(filePath).ToLowerInvariant();
            if (IsSupportedExportExtension(extension))
            {
                return filePath;
            }

            return Path.ChangeExtension(filePath, GetExportExtensionForFilterIndex(filterIndex));
        }

        /// <summary>
        /// Maps a file-dialog filter index to the export extension.
        /// </summary>
        /// <param name="filterIndex">The selected file-dialog filter index.</param>
        /// <returns>The extension for the selected filter.</returns>
        private static string GetExportExtensionForFilterIndex(int filterIndex)
        {
            return filterIndex switch
            {
                PdfFilterIndex => ".pdf",
                SvgFilterIndex => ".svg",
                _ => ".png"
            };
        }

        /// <summary>
        /// Determines whether an image export extension is supported.
        /// </summary>
        /// <param name="extension">The extension to test.</param>
        /// <returns><c>true</c> when the extension is supported.</returns>
        private static bool IsSupportedExportExtension(string extension)
        {
            return string.Equals(extension, ".png", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".pdf", StringComparison.OrdinalIgnoreCase)
                || string.Equals(extension, ".svg", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// Exports the given PlotModel to the specified file using the appropriate exporter.
        /// </summary>
        private static void ExportModel(OxyPlot.PlotModel model, string filePath, string extension, int width, int height)
        {
            switch (extension.ToLowerInvariant())
            {
                case ".png":
                    var exporter = new PngExporter { Width = width, Height = height, Background = model.Background };
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        exporter.Export(model, fs);
                    }
                    break;
                case ".svg":
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        OxyPlot.Wpf.WpfSvgExporter.Export(model, fs, width, height, true);
                    }
                    break;
                case ".pdf":
                    using (var fs = new FileStream(filePath, FileMode.Create))
                    {
                        OxyPlot.Wpf.WpfPdfExporter.Export(model, fs, width, height);
                    }
                    break;
                default:
                    throw new NotSupportedException("Unsupported plot export format: " + extension);
            }
        }

        /// <summary>
        /// Handles the Cancel button click event to close the dialog without saving.
        /// </summary>
        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}
