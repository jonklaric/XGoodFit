using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Runtime.Versioning;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Shapes;
using GenericControls;
using Themes;

#nullable enable

namespace GenericControls.Demo
{
    /// <summary>
    /// Main window for demonstrating the GenericControls library functionality.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This demo application showcases all custom controls available in the GenericControls library,
    /// including text inputs, numeric controls, color pickers, property editors, file/folder selectors,
    /// and data grids. The application supports runtime theme switching between Light, Blue, and Dark themes.
    /// </para>
    /// <para>
    /// The window serves as its own ViewModel by implementing <see cref="INotifyPropertyChanged"/>,
    /// with the <c>DataContext</c> set to itself. All controls bind directly to properties
    /// defined in this class.
    /// </para>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    [SupportedOSPlatform("windows")]
    public partial class MainWindow : MetroWindow, INotifyPropertyChanged
    {
        #region Private Fields

        private string _nameProperty = "Sample Name";
        private string _textProperty = "Sample Text";
        private string _descriptionProperty = "This is a sample description that demonstrates the resizable text property control.";
        private string _fontProperty = "Segoe UI";
        private FontWeight _fontWeightProperty = FontWeights.Normal;
        private bool _booleanProperty = true;
        private double _numericProperty = 25.0;
        private double _numericAutoProperty = double.NaN;
        private double _opacityProperty = 0.8;
        private double _lineWidthProperty = 2.0;
        private DoubleCollection _lineStyleProperty = new DoubleCollection();
        private SolidColorBrush _colorProperty = new SolidColorBrush(Colors.SteelBlue);
        private SolidColorBrush _newColorProperty = new SolidColorBrush(Colors.Orange);
        private HorizontalAlignment _horizontalAlignmentProperty = HorizontalAlignment.Center;
        private VerticalAlignment _verticalAlignmentProperty = VerticalAlignment.Center;
        private Point _pointProperty = new Point(100, 200);
        private Point3D _point3DProperty = new Point3D(10, 20, 30);
        private DateTime _dateTimeProperty = DateTime.Now;
        private CalendarWeekRule _calendarWeekRuleProperty = CalendarWeekRule.FirstDay;
        private Thickness _thicknessProperty = new Thickness(5);
        private string _filePathProperty = "";
        private string _folderPathProperty = "";
        private string _directoryPathProperty = "";
        private string _dialogResultText = "Click a button above to see the dialog result.";
        private IList<string> _stringListProperty = new List<string>();
        private ObservableCollection<ColorItem> _colorItems = new ObservableCollection<ColorItem>();
        private int _colorCounter = 1;

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="TestGridLength"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TestGridLengthProperty =
            DependencyProperty.Register(
                nameof(TestGridLength),
                typeof(GridLength),
                typeof(MainWindow),
                new FrameworkPropertyMetadata(new GridLength(100d)));

        /// <summary>
        /// Gets or sets the test grid length value used for demonstrating the GridLengthControl.
        /// </summary>
        public GridLength TestGridLength
        {
            get => (GridLength)GetValue(TestGridLengthProperty);
            set => SetValue(TestGridLengthProperty, value);
        }

        #endregion

        #region Bindable Properties

        /// <summary>
        /// Gets or sets the name property value for NameTextBox and NameTextPropertyControl demonstrations.
        /// </summary>
        public new string NameProperty
        {
            get => _nameProperty;
            set => SetProperty(ref _nameProperty, value);
        }

        /// <summary>
        /// Gets or sets the text property value for TextPropertyControl demonstrations.
        /// </summary>
        public string TextProperty
        {
            get => _textProperty;
            set => SetProperty(ref _textProperty, value);
        }

        /// <summary>
        /// Gets or sets the description text for ResizeableTextPropertyControl demonstrations.
        /// </summary>
        public string DescriptionProperty
        {
            get => _descriptionProperty;
            set => SetProperty(ref _descriptionProperty, value);
        }

        /// <summary>
        /// Gets or sets the font family name for FontSelectorControl demonstrations.
        /// </summary>
        public string FontProperty
        {
            get => _fontProperty;
            set => SetProperty(ref _fontProperty, value);
        }

        /// <summary>
        /// Gets or sets the font weight for FontWeightSelectorControl demonstrations.
        /// </summary>
        public new FontWeight FontWeightProperty
        {
            get => _fontWeightProperty;
            set => SetProperty(ref _fontWeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the boolean value for BooleanPropertyControl demonstrations.
        /// </summary>
        public bool BooleanProperty
        {
            get => _booleanProperty;
            set => SetProperty(ref _booleanProperty, value);
        }

        /// <summary>
        /// Gets or sets the numeric value for NumericPropertyControl demonstrations.
        /// </summary>
        public double NumericProperty
        {
            get => _numericProperty;
            set => SetProperty(ref _numericProperty, value);
        }

        /// <summary>
        /// Gets or sets the numeric value with auto/default support for NumericAutoPropertyControl demonstrations.
        /// </summary>
        public double NumericAutoProperty
        {
            get => _numericAutoProperty;
            set => SetProperty(ref _numericAutoProperty, value);
        }

        /// <summary>
        /// Gets or sets the opacity value (0-1) for NumericSliderPropertyControl demonstrations.
        /// </summary>
        public new double OpacityProperty
        {
            get => _opacityProperty;
            set => SetProperty(ref _opacityProperty, value);
        }

        /// <summary>
        /// Gets or sets the line width for LineWidthSelectorControl demonstrations.
        /// </summary>
        public double LineWidthProperty
        {
            get => _lineWidthProperty;
            set => SetProperty(ref _lineWidthProperty, value);
        }

        /// <summary>
        /// Gets or sets the line style (dash pattern) as a <see cref="DoubleCollection"/>
        /// for LineStyleSelectorControl demonstrations.
        /// </summary>
        public DoubleCollection LineStyleProperty
        {
            get => _lineStyleProperty;
            set => SetProperty(ref _lineStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the color as a <see cref="SolidColorBrush"/>
        /// for ColorPicker and ColorPropertyControl demonstrations.
        /// </summary>
        public SolidColorBrush ColorProperty
        {
            get => _colorProperty;
            set => SetProperty(ref _colorProperty, value);
        }

        /// <summary>
        /// Gets or sets the new color to add to the color collection.
        /// </summary>
        public SolidColorBrush NewColorProperty
        {
            get => _newColorProperty;
            set => SetProperty(ref _newColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the horizontal alignment for HorizontalAlignmentControl demonstrations.
        /// </summary>
        public new HorizontalAlignment HorizontalAlignmentProperty
        {
            get => _horizontalAlignmentProperty;
            set => SetProperty(ref _horizontalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the vertical alignment for VerticalAlignmentControl demonstrations.
        /// </summary>
        public new VerticalAlignment VerticalAlignmentProperty
        {
            get => _verticalAlignmentProperty;
            set => SetProperty(ref _verticalAlignmentProperty, value);
        }

        /// <summary>
        /// Gets or sets the 2D point for PointPropertyControl demonstrations.
        /// </summary>
        public Point PointProperty
        {
            get => _pointProperty;
            set => SetProperty(ref _pointProperty, value);
        }

        /// <summary>
        /// Gets or sets the 3D point for Point3DPropertyControl demonstrations.
        /// </summary>
        public Point3D Point3DProperty
        {
            get => _point3DProperty;
            set => SetProperty(ref _point3DProperty, value);
        }

        /// <summary>
        /// Gets or sets the date/time value for DateTimePropertyControl and DateAndTimeTextBoxControl demonstrations.
        /// </summary>
        public DateTime DateTimeProperty
        {
            get => _dateTimeProperty;
            set => SetProperty(ref _dateTimeProperty, value);
        }

        /// <summary>
        /// Gets or sets the calendar week rule for CalendarWeekRulePropertyControl demonstrations.
        /// </summary>
        public CalendarWeekRule CalendarWeekRuleProperty
        {
            get => _calendarWeekRuleProperty;
            set => SetProperty(ref _calendarWeekRuleProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness value for ThicknessControl demonstrations.
        /// </summary>
        public Thickness ThicknessProperty
        {
            get => _thicknessProperty;
            set => SetProperty(ref _thicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the file path for FileSelectorControl demonstrations.
        /// </summary>
        public string FilePathProperty
        {
            get => _filePathProperty;
            set => SetProperty(ref _filePathProperty, value);
        }

        /// <summary>
        /// Gets or sets the folder path for FolderSelectorControl demonstrations.
        /// </summary>
        public string FolderPathProperty
        {
            get => _folderPathProperty;
            set => SetProperty(ref _folderPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the directory path for DirectorySelectorControl demonstrations.
        /// </summary>
        public string DirectoryPathProperty
        {
            get => _directoryPathProperty;
            set => SetProperty(ref _directoryPathProperty, value);
        }

        /// <summary>
        /// Gets or sets the text displaying dialog results from button demonstrations.
        /// </summary>
        public string DialogResultText
        {
            get => _dialogResultText;
            set => SetProperty(ref _dialogResultText, value);
        }

        /// <summary>
        /// Gets or sets the list of strings for StringListPropertyControl demonstrations.
        /// </summary>
        public IList<string> StringListProperty
        {
            get => _stringListProperty;
            set => SetProperty(ref _stringListProperty, value);
        }

        /// <summary>
        /// Gets the observable collection of color items for the Color Collection tab.
        /// </summary>
        public ObservableCollection<ColorItem> ColorItems => _colorItems;

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed. This is automatically provided by the compiler.</param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property value and raises <see cref="PropertyChanged"/> if the value changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value.</param>
        /// <param name="propertyName">The name of the property. This is automatically provided by the compiler.</param>
        /// <returns><c>true</c> if the value changed; otherwise, <c>false</c>.</returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        /// <remarks>
        /// Sets up the data context, initializes sample data for demonstration controls,
        /// and configures default property values.
        /// </remarks>
        public MainWindow()
        {
            InitializeComponent();
            DataContext = this;
            InitializeSampleData();
        }

        /// <summary>
        /// Initializes all sample data for the demonstration controls.
        /// </summary>
        private void InitializeSampleData()
        {
            // Initialize line style
            LineStyleProperty = LineStyleSelectorControl.LineStyleOptions[0];

            // Initialize colors
            ColorProperty = new SolidColorBrush(Color.FromArgb(200, 70, 130, 180)); // Steel Blue
            NewColorProperty = new SolidColorBrush(Colors.Orange);

            // Initialize string list
            StringListProperty = new List<string>
            {
                "First item",
                "Second item",
                "Third item"
            };

            // Initialize color collection
            _colorItems = new ObservableCollection<ColorItem>
            {
                new ColorItem { Name = "Primary", ColorBrush = new SolidColorBrush(Colors.SteelBlue) },
                new ColorItem { Name = "Secondary", ColorBrush = new SolidColorBrush(Colors.DarkSlateGray) },
                new ColorItem { Name = "Accent", ColorBrush = new SolidColorBrush(Colors.OrangeRed) },
                new ColorItem { Name = "Success", ColorBrush = new SolidColorBrush(Colors.ForestGreen) },
                new ColorItem { Name = "Warning", ColorBrush = new SolidColorBrush(Colors.Gold) }
            };
            ColorItemsControl.ItemsSource = _colorItems;

            // Initialize DataGrid sample data. Each grid gets its own ObservableCollection
            // so that Add/Insert/Delete actions in the main grid don't silently mutate the
            // popup grid (and vice versa) through CopyPasteDataGrid's IList operations.
            CPDataGrid.ItemsSource = CreateSampleDataItems();
            CPDataGrid2.ItemsSource = CreateSampleDataItems();
        }

        /// <summary>
        /// Creates sample rows for the copy-paste data-grid demos.
        /// </summary>
        /// <returns>A new sample data collection.</returns>
        private static ObservableCollection<SampleDataItem> CreateSampleDataItems()
        {
            return new ObservableCollection<SampleDataItem>
            {
                new SampleDataItem { Name = "Item 1", Value = 100, Category = "Alpha" },
                new SampleDataItem { Name = "Item 2", Value = 250, Category = "Beta" },
                new SampleDataItem { Name = "Item 3", Value = 175, Category = "Alpha" },
                new SampleDataItem { Name = "Item 4", Value = 320, Category = "Gamma" },
                new SampleDataItem { Name = "Item 5", Value = 95, Category = "Beta" }
            };
        }

        #endregion

        #region Theme Handling

        /// <summary>
        /// Handles theme radio button selection changes.
        /// </summary>
        /// <param name="sender">The radio button that was checked.</param>
        /// <param name="e">Event arguments.</param>
        private void ThemeRadio_Checked(object sender, RoutedEventArgs e)
        {
            if (sender is RadioButton radioButton)
            {
                Theme theme = Theme.Light;

                if (radioButton == LightThemeRadio)
                    theme = Theme.Light;
                else if (radioButton == BlueThemeRadio)
                    theme = Theme.Blue;
                else if (radioButton == DarkThemeRadio)
                    theme = Theme.Dark;

                ThemeService.Instance.SetTheme(theme);
            }
        }

        #endregion

        #region DataGrid Event Handlers

        /// <summary>
        /// Handles the AutoGeneratedColumns event to inspect property names bound to each column.
        /// </summary>
        /// <param name="sender">The DataGrid that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void CPDataGrid_AutoGeneratedColumns(object sender, EventArgs e)
        {
            foreach (var column in CPDataGrid.Columns)
            {
                if (column is DataGridTextColumn textColumn && textColumn.Binding is Binding binding)
                {
                    Debug.Print($"Column generated for property: {binding.Path.Path}");
                }
            }
        }

        /// <summary>
        /// When the mouse enters the DataGrid, set the popup to StaysOpen=true
        /// to allow the DataGrid to keep capture of the mouse and give it focus.
        /// </summary>
        /// <param name="sender">The DataGrid that raised the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void CPDataGrid2_MouseEnter(object sender, MouseEventArgs e)
        {
            DataGridPopup.StaysOpen = true;
            CPDataGrid2.Focus();
        }

        /// <summary>
        /// When the mouse leaves the DataGrid, set the popup to StaysOpen=false
        /// and give it focus so that clicking outside the popup will close it.
        /// </summary>
        /// <param name="sender">The DataGrid that raised the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void CPDataGrid2_MouseLeave(object sender, MouseEventArgs e)
        {
            DataGridPopup.StaysOpen = false;
            DataGridPopup.Focus();
        }

        /// <summary>
        /// When the context menu closes, the focus gets out of sync in the popup
        /// and needs to be reset by toggling the popup open state.
        /// </summary>
        /// <param name="sender">The DataGrid that raised the event.</param>
        /// <param name="e">Context menu event arguments.</param>
        private void CPDataGrid2_ContextMenuClosing(object sender, ContextMenuEventArgs e)
        {
            DataGridPopup.IsOpen = false;
            DataGridPopup.IsOpen = true;
        }

        #endregion

        #region Dialog Button Handlers

        /// <summary>
        /// Shows the NameDialog and displays the result.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowNameDialog_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new NameDialog
            {
                Owner = this,
                Title = "Enter Name",
                Text = "Default Name"
            };

            if (dialog.ShowDialog() == true)
            {
                DialogResultText = $"Name Dialog Result: \"{dialog.Text}\"";
            }
            else
            {
                DialogResultText = "Name Dialog was cancelled.";
            }
        }

        /// <summary>
        /// Shows the FolderBrowser dialog and displays the result.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowFolderBrowser_Click(object sender, RoutedEventArgs e)
        {
            string? folder = GeneralMethods.FolderBrowserDialog(this);

            if (!string.IsNullOrEmpty(folder))
            {
                DialogResultText = $"Folder Browser Result: \"{folder}\"";
                FolderPathProperty = folder;
            }
            else
            {
                DialogResultText = "Folder Browser was cancelled or no folder selected.";
            }
        }

        /// <summary>
        /// Shows the File Open dialog and displays the result.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void ShowFileDialog_Click(object sender, RoutedEventArgs e)
        {
            string file = GeneralMethods.FileOpenDialog("All Files (*.*)|*.*|Text Files (*.txt)|*.txt");

            if (!string.IsNullOrEmpty(file))
            {
                DialogResultText = $"File Dialog Result: \"{file}\"";
                FilePathProperty = file;
            }
            else
            {
                DialogResultText = "File Dialog was cancelled or no file selected.";
            }
        }

        #endregion

        #region Color Collection Handlers

        /// <summary>
        /// Adds a new color to the color collection using the NewColorProperty value.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void AddColor_Click(object sender, RoutedEventArgs e)
        {
            if (NewColorProperty != null)
            {
                _colorItems.Add(new ColorItem
                {
                    Name = $"Color {++_colorCounter}",
                    ColorBrush = new SolidColorBrush(NewColorProperty.Color)
                });
            }
        }

        /// <summary>
        /// Removes a color from the color collection.
        /// </summary>
        /// <param name="sender">The button that raised the event.</param>
        /// <param name="e">Event arguments.</param>
        private void RemoveColor_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button button && button.Tag is ColorItem colorItem)
            {
                _colorItems.Remove(colorItem);
            }
        }

        /// <summary>
        /// Displays a color picker popup bound to the clicked color rectangle's data context.
        /// </summary>
        /// <param name="sender">The rectangle that was clicked.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void ColorRectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (sender is Rectangle rectangle && rectangle.DataContext is ColorItem colorItem)
            {
                var popup = new System.Windows.Controls.Primitives.Popup
                {
                    IsOpen = false,
                    StaysOpen = false,
                    AllowsTransparency = true,
                    Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom,
                    PlacementTarget = rectangle
                };
                popup.Closed += PopupClosed;

                var colorPicker = new ColorPicker
                {
                    Width = 175,
                    Height = 250,
                    Background = Brushes.White,
                    BorderBrush = Brushes.Gray,
                    BorderThickness = new Thickness(1),
                    Padding = new Thickness(2)
                };

                colorPicker.SetResourceReference(ColorPicker.BorderBrushProperty, "Button.Static.Border");
                colorPicker.SetResourceReference(ColorPicker.BackgroundProperty, "EnvironmentWindowBackground");

                BindingOperations.SetBinding(
                    colorPicker,
                    ColorPicker.ColorProperty,
                    new Binding(nameof(ColorItem.ColorBrush))
                    {
                        Source = colorItem,
                        UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                        Mode = BindingMode.TwoWay
                    });

                popup.Child = colorPicker;
                popup.IsOpen = true;
            }
        }

        /// <summary>
        /// Cleans up the color picker binding when the popup is closed.
        /// </summary>
        /// <param name="sender">The popup that was closed.</param>
        /// <param name="e">Event arguments.</param>
        private void PopupClosed(object? sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.Primitives.Popup popup && popup.Child is ColorPicker colorPicker)
            {
                BindingOperations.ClearBinding(colorPicker, ColorPicker.ColorProperty);
            }
        }

        #endregion
    }

    /// <summary>
    /// Represents a sample data item for DataGrid demonstrations.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class SampleDataItem
    {
        /// <summary>
        /// Gets or sets the name of the item.
        /// </summary>
        public string Name { get; set; } = "";

        /// <summary>
        /// Gets or sets the numeric value of the item.
        /// </summary>
        public double Value { get; set; }

        /// <summary>
        /// Gets or sets the category of the item.
        /// </summary>
        public string Category { get; set; } = "";
    }
}
