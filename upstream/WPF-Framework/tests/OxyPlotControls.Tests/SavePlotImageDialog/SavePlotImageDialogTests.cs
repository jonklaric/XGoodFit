using System.IO;
using System.Reflection;
using System.Windows;
using Microsoft.Win32;
using OxyPlot;
using OxyPlot.Wpf;
using Xunit;

namespace OxyPlotControls.Tests.SavePlotImageDialog;

/// <summary>
/// Unit tests for the SavePlotImageDialog.
/// Tests construction, export options, native Save As configuration, and file export.
/// </summary>
public class SavePlotImageDialogTests
{
    /// <summary>
    /// Creates a Plot control with a simple PlotModel for testing.
    /// </summary>
    private static Plot CreateTestPlot(string? title = "Test Plot")
    {
        var plot = new Plot();
        var model = new PlotModel { Title = title };
        model.Series.Add(new OxyPlot.Series.LineSeries());
        plot.Model = model;
        return plot;
    }

    #region Construction Tests

    /// <summary>
    /// Verifies that the dialog can be constructed without throwing an exception.
    /// </summary>
    [StaFact]
    public void Constructor_WithValidPlot_DoesNotThrow()
    {
        var plot = CreateTestPlot();

        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        Assert.NotNull(dialog);
    }

    /// <summary>
    /// Verifies that the dialog title reflects export settings rather than file selection.
    /// </summary>
    [StaFact]
    public void Constructor_WithValidPlot_TitleIsSetCorrectly()
    {
        var plot = CreateTestPlot();

        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        Assert.Equal("Export Plot Image", dialog.Title);
    }

    /// <summary>
    /// Verifies that the dialog initial size is computed from the default preset (600x480) and
    /// falls within the expected bounds. The exact dialog size depends on display/DPI and CI.
    /// </summary>
    [StaFact]
    public void Constructor_WithValidPlot_HasCorrectInitialSize()
    {
        var plot = CreateTestPlot();

        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        Assert.InRange(dialog.Width, dialog.MinWidth, 800);
        Assert.InRange(dialog.Height, dialog.MinHeight, 800);
    }

    /// <summary>
    /// Verifies that the dialog minimum size constraints are set.
    /// </summary>
    [StaFact]
    public void Constructor_WithValidPlot_HasCorrectMinimumSize()
    {
        var plot = CreateTestPlot();

        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        Assert.Equal(500, dialog.MinWidth);
        Assert.Equal(450, dialog.MinHeight);
    }

    #endregion

    #region Image Size Preset Tests

    /// <summary>
    /// Verifies that the ImageSizeComboBox has all 9 preset items plus Custom.
    /// </summary>
    [StaFact]
    public void ImageSizeComboBox_HasCorrectItemCount()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var comboBox = (System.Windows.Controls.ComboBox)dialog.FindName("ImageSizeComboBox");

        Assert.NotNull(comboBox);
        Assert.Equal(10, comboBox.Items.Count);
    }

    /// <summary>
    /// Verifies that the first preset is selected by default.
    /// </summary>
    [StaFact]
    public void ImageSizeComboBox_DefaultSelectionIsFirstPreset()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var comboBox = (System.Windows.Controls.ComboBox)dialog.FindName("ImageSizeComboBox");

        Assert.NotNull(comboBox);
        Assert.Equal(0, comboBox.SelectedIndex);
    }

    /// <summary>
    /// Verifies that the last item in the ComboBox is "Custom".
    /// </summary>
    [StaFact]
    public void ImageSizeComboBox_LastItemIsCustom()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var comboBox = (System.Windows.Controls.ComboBox)dialog.FindName("ImageSizeComboBox");
        var lastItem = (System.Windows.Controls.ComboBoxItem)comboBox!.Items[comboBox.Items.Count - 1];

        Assert.Equal("Custom", lastItem.Content.ToString());
    }

    /// <summary>
    /// Verifies that preset items contain expected dimension format.
    /// </summary>
    [StaFact]
    public void ImageSizeComboBox_PresetsContainExpectedFormat()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);
        var comboBox = (System.Windows.Controls.ComboBox)dialog.FindName("ImageSizeComboBox");

        var firstItem = (System.Windows.Controls.ComboBoxItem)comboBox!.Items[0];
        var fullHdItem = (System.Windows.Controls.ComboBoxItem)comboBox.Items[5];

        Assert.Equal("600 x 480 (5:4)", firstItem.Content.ToString());
        Assert.Equal("1920 x 1080 (16:9)", fullHdItem.Content.ToString());
    }

    #endregion

    #region Custom Dimensions Panel Tests

    /// <summary>
    /// Verifies that the custom dimensions panel is collapsed by default.
    /// </summary>
    [StaFact]
    public void CustomDimensionsPanel_IsCollapsedByDefault()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var panel = (System.Windows.Controls.Grid)dialog.FindName("CustomDimensionsPanel");

        Assert.NotNull(panel);
        Assert.Equal(Visibility.Collapsed, panel.Visibility);
    }

    /// <summary>
    /// Verifies that width and height textboxes are disabled by default (non-custom mode).
    /// </summary>
    [StaFact]
    public void DimensionTextBoxes_AreDisabledByDefault()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var widthBox = (GenericControls.NumericTextBox)dialog.FindName("WidthTextBox");
        var heightBox = (GenericControls.NumericTextBox)dialog.FindName("HeightTextBox");

        Assert.NotNull(widthBox);
        Assert.NotNull(heightBox);
        Assert.False(widthBox.IsEnabled);
        Assert.False(heightBox.IsEnabled);
    }

    #endregion

    #region Native Save As Tests

    /// <summary>
    /// Verifies that custom file-picking controls are no longer part of the export window.
    /// </summary>
    [StaFact]
    public void CustomFilePickerControls_AreRemoved()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        Assert.Null(dialog.FindName("FileTypeComboBox"));
        Assert.Null(dialog.FindName("FolderPathControl"));
        Assert.Null(dialog.FindName("FileNameTextBox"));
    }

    /// <summary>
    /// Verifies that the native SaveFileDialog is configured like a traditional Save As dialog.
    /// </summary>
    [StaFact]
    public void CreateSaveFileDialog_UsesExpectedConfiguration()
    {
        var plot = CreateTestPlot("Hydrograph: Test/Run");
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var saveFileDialog = InvokePrivateInstance<SaveFileDialog>(dialog, "CreateSaveFileDialog");

        Assert.Equal("Save Plot Image As", saveFileDialog.Title);
        Assert.Equal("PNG Image (*.png)|*.png|PDF Document (*.pdf)|*.pdf|SVG Vector Image (*.svg)|*.svg", saveFileDialog.Filter);
        Assert.Equal(1, saveFileDialog.FilterIndex);
        Assert.Equal("png", saveFileDialog.DefaultExt);
        Assert.True(saveFileDialog.AddExtension);
        Assert.True(saveFileDialog.OverwritePrompt);
        Assert.True(saveFileDialog.ValidateNames);
        Assert.True(saveFileDialog.CheckPathExists);
        Assert.Equal("Hydrograph_ Test_Run", saveFileDialog.FileName);
    }

    /// <summary>
    /// Verifies that blank plot titles fall back to a simple default file name.
    /// </summary>
    [StaFact]
    public void GetDefaultFileName_BlankTitle_ReturnsPlot()
    {
        var fileName = InvokePrivateStatic<string>("GetDefaultFileName", "   ");

        Assert.Equal("plot", fileName);
    }

    /// <summary>
    /// Verifies that default file names do not contain invalid Windows filename characters.
    /// </summary>
    [StaFact]
    public void GetDefaultFileName_InvalidCharacters_AreReplaced()
    {
        var fileName = InvokePrivateStatic<string>("GetDefaultFileName", "A:B/C*D?");

        Assert.DoesNotContain(fileName, c => Array.IndexOf(Path.GetInvalidFileNameChars(), c) >= 0);
        Assert.Contains("_", fileName);
    }

    /// <summary>
    /// Verifies that supported user-entered extensions are honored even when the selected filter differs.
    /// </summary>
    [StaFact]
    public void GetExportFilePath_WithSupportedExtension_HonorsExtension()
    {
        string path = Path.Combine("C:\\Temp", "chart.svg");

        var exportPath = InvokePrivateStatic<string>("GetExportFilePath", path, 1);

        Assert.Equal(path, exportPath);
    }

    /// <summary>
    /// Verifies that a missing extension uses the selected filter extension.
    /// </summary>
    [StaFact]
    public void GetExportFilePath_WithoutExtension_AddsFilterExtension()
    {
        string path = Path.Combine("C:\\Temp", "chart");

        var exportPath = InvokePrivateStatic<string>("GetExportFilePath", path, 2);

        Assert.Equal(Path.Combine("C:\\Temp", "chart.pdf"), exportPath);
    }

    /// <summary>
    /// Verifies that unsupported extensions are replaced by the selected filter extension.
    /// </summary>
    [StaFact]
    public void GetExportFilePath_WithUnsupportedExtension_ReplacesWithFilterExtension()
    {
        string path = Path.Combine("C:\\Temp", "chart.txt");

        var exportPath = InvokePrivateStatic<string>("GetExportFilePath", path, 3);

        Assert.Equal(Path.Combine("C:\\Temp", "chart.svg"), exportPath);
    }

    /// <summary>
    /// Verifies that unknown filter indexes fall back to PNG.
    /// </summary>
    [StaFact]
    public void GetExportExtensionForFilterIndex_UnknownIndex_ReturnsPng()
    {
        var extension = InvokePrivateStatic<string>("GetExportExtensionForFilterIndex", 99);

        Assert.Equal(".png", extension);
    }

    #endregion

    #region Preview Container Tests

    /// <summary>
    /// Verifies that the preview container and image control exist.
    /// </summary>
    [StaFact]
    public void PreviewContainer_Exists()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var container = (System.Windows.Controls.Grid)dialog.FindName("PreviewContainer");
        var image = (System.Windows.Controls.Image)dialog.FindName("PreviewImage");

        Assert.NotNull(container);
        Assert.NotNull(image);
    }

    /// <summary>
    /// Verifies that the preview image uses high quality bitmap scaling.
    /// </summary>
    [StaFact]
    public void PreviewImage_UsesHighQualityScaling()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var image = (System.Windows.Controls.Image)dialog.FindName("PreviewImage");

        Assert.NotNull(image);
        Assert.Equal(System.Windows.Media.BitmapScalingMode.HighQuality,
                     System.Windows.Media.RenderOptions.GetBitmapScalingMode(image));
    }

    #endregion

    #region Button Tests

    /// <summary>
    /// Verifies that the Save As button exists and is the default button.
    /// </summary>
    [StaFact]
    public void SaveAsButton_IsDefault()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var saveAsButton = (System.Windows.Controls.Button)dialog.FindName("SaveAsButton");

        Assert.NotNull(saveAsButton);
        Assert.True(saveAsButton.IsDefault);
        Assert.Equal("Save As...", saveAsButton.Content.ToString());
    }

    /// <summary>
    /// Verifies that the Cancel button exists and is the cancel button.
    /// </summary>
    [StaFact]
    public void CancelButton_IsCancel()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var cancelButton = (System.Windows.Controls.Button)dialog.FindName("CancelButton");

        Assert.NotNull(cancelButton);
        Assert.True(cancelButton.IsCancel);
        Assert.Equal("Cancel", cancelButton.Content.ToString());
    }

    /// <summary>
    /// Verifies that both buttons have consistent dimensions.
    /// </summary>
    [StaFact]
    public void Buttons_HaveConsistentDimensions()
    {
        var plot = CreateTestPlot();
        var dialog = new OxyPlotControls.SavePlotImageDialog(plot);

        var saveAsButton = (System.Windows.Controls.Button)dialog.FindName("SaveAsButton");
        var cancelButton = (System.Windows.Controls.Button)dialog.FindName("CancelButton");

        Assert.Equal(90, saveAsButton!.Width);
        Assert.Equal(24, saveAsButton.Height);
        Assert.Equal(75, cancelButton!.Width);
        Assert.Equal(24, cancelButton.Height);
    }

    #endregion

    #region Export Integration Tests

    /// <summary>
    /// Verifies that a PNG file can be exported to a temporary directory.
    /// </summary>
    [StaFact]
    public void Export_Png_CreatesFile()
    {
        AssertExportCreatesFile(".png");
    }

    /// <summary>
    /// Verifies that an SVG file can be exported to a temporary directory.
    /// </summary>
    [StaFact]
    public void Export_Svg_CreatesFile()
    {
        AssertExportCreatesFile(".svg");
    }

    /// <summary>
    /// Verifies that a PDF file can be exported to a temporary directory.
    /// </summary>
    [StaFact]
    public void Export_Pdf_CreatesFile()
    {
        AssertExportCreatesFile(".pdf");
    }

    private static void AssertExportCreatesFile(string extension)
    {
        var plot = CreateTestPlot();
        string tempDir = Path.Combine(Path.GetTempPath(), "SavePlotImageDialogTests_" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        try
        {
            string filePath = Path.Combine(tempDir, "test_export" + extension);

            InvokePrivateStatic("ExportModel", plot.Model!, filePath, extension, 600, 480);

            Assert.True(File.Exists(filePath));
            Assert.True(new FileInfo(filePath).Length > 0);
        }
        finally
        {
            Directory.Delete(tempDir, true);
        }
    }

    #endregion

    private static T InvokePrivateStatic<T>(string methodName, params object?[] parameters)
    {
        var method = typeof(OxyPlotControls.SavePlotImageDialog).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        var result = method!.Invoke(null, parameters);
        return Assert.IsType<T>(result);
    }

    private static void InvokePrivateStatic(string methodName, params object?[] parameters)
    {
        var method = typeof(OxyPlotControls.SavePlotImageDialog).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);

        method!.Invoke(null, parameters);
    }

    private static T InvokePrivateInstance<T>(object instance, string methodName, params object?[] parameters)
    {
        var method = typeof(OxyPlotControls.SavePlotImageDialog).GetMethod(methodName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);

        var result = method!.Invoke(instance, parameters);
        return Assert.IsType<T>(result);
    }
}
