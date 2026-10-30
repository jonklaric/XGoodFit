using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using Avalonia.Styling;
using Avalonia.Threading;
using WpfCompat;
using WpfCompat.Input;

namespace GenericControls
{
    /// <summary>
    /// A toolbar control providing standard DataGrid operations such as add, insert, delete, copy, and paste.
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
    /// <para>
    /// XGoodFit port: the button and separator styles are ControlThemes (the buttons' and separators'
    /// <c>Theme</c>), and the clipboard is WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class DataGridToolbar : UserControl
    {

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="DataGridToolbar"/> class.
        /// </summary>
        public DataGridToolbar()
        {

            // This call is required by the designer.
            this.InitializeComponent();

            // Add any initialization after the InitializeComponent() call.
            var defaultButtonStyle = DefaultStackPanelButtonStyle();
            this.AddRowsButton.Theme = defaultButtonStyle;
            this.InsertRowsButton.Theme = defaultButtonStyle;
            this.DeleteRowsButton.Theme = defaultButtonStyle;
            this.SelectAllButton.Theme = defaultButtonStyle;
            this.CopyButton.Theme = defaultButtonStyle;
            this.CopyWithHeadersButton.Theme = defaultButtonStyle;
            this.PasteButton.Theme = defaultButtonStyle;
            //
            var defaultSeparatorStyle = DefaultStackPanelSeparatorStyle();
            this.EditSelectSeparator.Theme = defaultSeparatorStyle;
            this.CustomOptionsSeparator.Theme = defaultSeparatorStyle;
            // 
            this.InsertRowsButton.IsEnabled = false;
            this.DeleteRowsButton.IsEnabled = false;
            this.CopyButton.IsEnabled = false;
            this.CopyWithHeadersButton.IsEnabled = false;
            this.PasteButton.IsEnabled = false;
            // 
            _customButtons.CollectionChanged += (sender, e) =>
                {
                    if (e.OldItems is not null)
                    {
                        foreach (Button item in e.OldItems)
                            this.ToolbarStackPanel.Children.Remove(item);
                    }
                    if (e.NewItems is not null)
                    {
                        foreach (Button item in e.NewItems)
                        {
                            item.Theme = DefaultStackPanelButtonStyle();
                            this.ToolbarStackPanel.Children.Insert(3 + _customButtons.Count, item);
                        }
                    }
                    // 
                    if (_customButtons.Count == 0)
                    {
                        this.CustomOptionsSeparator.Visibility = Visibility.Collapsed;
                    }
                    else
                    {
                        this.CustomOptionsSeparator.Visibility = Visibility.Visible;
                    }
                };
        }



        #endregion

        #region Members

        /// <summary>
        /// Dependency property for the data grid.
        /// </summary>
        public static readonly StyledProperty<CopyPasteDataGrid> DataGridProperty = AvaloniaProperty.Register<DataGridToolbar, CopyPasteDataGrid>(nameof(DataGrid), null);

        static DataGridToolbar()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            DataGridProperty.Changed.AddClassHandler<DataGridToolbar>(DataGridChangedCallback);
            StackPanelButtonStyleProperty.Changed.AddClassHandler<DataGridToolbar>(StackPanelButtonStylePropertyCallback);
            StackPanelSeparatorStyleProperty.Changed.AddClassHandler<DataGridToolbar>(StackPanelSeperatorStylePropertyCallback);
        }

        /// <summary>
        /// Callback method invoked when the DataGrid property changes.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data containing old and new values.</param>
        private static void DataGridChangedCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(DataGridToolbar))
                return;
            DataGridToolbar thisControl = (DataGridToolbar)d;
            // 
            if (e.OldValue is not null)
            {
                CopyPasteDataGrid oldGrid = e.OldValue as CopyPasteDataGrid;
                if (oldGrid is not null)
                {
                    oldGrid.PropertyChanged -= thisControl.DataGridPropertyChanged;
                    oldGrid.SelectedCellsChanged -= thisControl.DataGrid_SelectedCellsChanged;
                }
            }

            if (e.NewValue == null)
                return;
            CopyPasteDataGrid newGrid = e.NewValue as CopyPasteDataGrid;
            if (newGrid == null)
                return;
            // Can probably set the visibility of these buttons by binding to the datagrid property and using the booleantovisibility converter. Need to check.
            newGrid.PropertyChanged += thisControl.DataGridPropertyChanged;

            if (newGrid.CanUserAddInsertDeleteRows)
            {
                thisControl.AddRowsButton.Visibility = Visibility.Visible;
                thisControl.InsertRowsButton.Visibility = Visibility.Visible;
                thisControl.DeleteRowsButton.Visibility = Visibility.Visible;
            }
            else
            {
                thisControl.AddRowsButton.Visibility = Visibility.Collapsed;
                thisControl.InsertRowsButton.Visibility = Visibility.Collapsed;
                thisControl.DeleteRowsButton.Visibility = Visibility.Collapsed;
            }
            newGrid.SelectedCellsChanged += thisControl.DataGrid_SelectedCellsChanged;
        }

        /// <summary>
        /// Handles the SelectedCellsChanged event on the associated DataGrid,
        /// enabling or disabling toolbar buttons based on the current selection.
        /// </summary>
        private void DataGrid_SelectedCellsChanged(object sender, DataGridSelectedCellsChangedEventArgs e)
        {
            var grid = DataGrid;
            if (grid == null) return;

            bool wasAllSelected = _allCellsSelected;
            _allCellsSelected = grid.SelectedCells.Count > 0
                && grid.Items.Count > 0
                && grid.Columns.Count > 0
                && grid.SelectedCells.Count >= grid.Items.Count * grid.Columns.Count;
            if (_allCellsSelected != wasAllSelected)
                UpdateSelectAllButton();

            if (grid.SelectedCells.Count <= 0)
            {
                InsertRowsButton.IsEnabled = false;
                DeleteRowsButton.IsEnabled = false;
                CopyButton.IsEnabled = false;
                CopyWithHeadersButton.IsEnabled = false;
                PasteButton.IsEnabled = false;
            }
            else
            {
                InsertRowsButton.IsEnabled = true;
                DeleteRowsButton.IsEnabled = true;
                CopyButton.IsEnabled = true;
                CopyWithHeadersButton.IsEnabled = true;
                if (grid.IsReadOnly)
                {
                    PasteButton.IsEnabled = false;
                }
                else
                {
                    // Defer clipboard check so it doesn't block the UI thread
                    // during selection changes, hover transitions, etc.
                    Dispatcher.UIThread.Post(() =>
                    {
                        try
                        {
                            PasteButton.IsEnabled = Clipboard.ContainsText();
                        }
                        catch (Exception ex)
                        {
                            Debug.WriteLine(ex);
                            PasteButton.IsEnabled = false;
                        }
                    }, DispatcherPriority.Background);
                }
            }
        }

        /// <summary>
        /// Handles changes to data grid properties and updates toolbar visibility accordingly.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing the property name that changed.</param>
        private void DataGridPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if ((e.PropertyName ?? "") == nameof(CopyPasteDataGrid.CanUserAddInsertDeleteRows))
            {
                if (DataGrid.CanUserAddInsertDeleteRows)
                {
                    this.AddRowsButton.Visibility = Visibility.Visible;
                    this.InsertRowsButton.Visibility = Visibility.Visible;
                    this.DeleteRowsButton.Visibility = Visibility.Visible;
                }
                else
                {
                    this.AddRowsButton.Visibility = Visibility.Collapsed;
                    this.InsertRowsButton.Visibility = Visibility.Collapsed;
                    this.DeleteRowsButton.Visibility = Visibility.Collapsed;
                }
            }
        }

        /// <summary>
        /// Gets or sets the data grid associated with this toolbar.
        /// </summary>
        [Category("Miscellaneous")]
        [Description("Get and set the data grid for the control.")]
        [Browsable(true)]
        public CopyPasteDataGrid DataGrid
        {
            get
            {
                return this.GetValue(DataGridProperty);
            }
            set
            {
                this.SetValue(DataGridProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the toolbar orientation.
        /// </summary>
        public static readonly StyledProperty<Orientation> ToolOrientationProperty = AvaloniaProperty.Register<DataGridToolbar, Orientation>(nameof(ToolOrientation), Orientation.Horizontal);

        /// <summary>
        /// Gets or sets the orientation of the toolbar buttons.
        /// </summary>
        [Category("Miscellaneous")]
        [Description("Get and set the tool buttons orientation.")]
        [Browsable(true)]
        public Orientation ToolOrientation
        {
            get
            {
                return this.GetValue(ToolOrientationProperty);
            }
            set
            {
                this.SetValue(ToolOrientationProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the control background color.
        /// </summary>
        public static readonly StyledProperty<ISolidColorBrush> BackgroundColorProperty = AvaloniaProperty.Register<DataGridToolbar, ISolidColorBrush>(nameof(BackgroundColor), new ImmutableSolidColorBrush(Colors.Transparent));

        /// <summary>
        /// Gets or sets the control background color.
        /// </summary>
        [Category("Brush")]
        [Description("Gets and sets the background color brush of the control.")]
        [Browsable(true)]
        public ISolidColorBrush BackgroundColor
        {
            get
            {
                return this.GetValue(BackgroundColorProperty);
            }
            set
            {
                this.SetValue(BackgroundColorProperty, value);
            }
        }


        /// <summary>
        /// Dependency property for the stack panel button style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> StackPanelButtonStyleProperty = AvaloniaProperty.Register<DataGridToolbar, ControlTheme>(nameof(StackPanelButtonStyle), DefaultStackPanelButtonStyle());

        /// <summary>
        /// Callback method invoked when the StackPanelButtonStyle property changes.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data containing old and new values.</param>
        private static void StackPanelButtonStylePropertyCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            DataGridToolbar thisControl = (DataGridToolbar)d;
            //
            ControlTheme newStyle = e.NewValue as ControlTheme;

            if (thisControl.AddRowsButton is not null)
                thisControl.AddRowsButton.Theme = newStyle;
            if (thisControl.InsertRowsButton is not null)
                thisControl.InsertRowsButton.Theme = newStyle;
            if (thisControl.DeleteRowsButton is not null)
                thisControl.DeleteRowsButton.Theme = newStyle;
            if (thisControl.SelectAllButton is not null)
                thisControl.SelectAllButton.Theme = newStyle;
            if (thisControl.CopyButton is not null)
                thisControl.CopyButton.Theme = newStyle;
            if (thisControl.CopyWithHeadersButton is not null)
                thisControl.CopyWithHeadersButton.Theme = newStyle;
            if (thisControl.PasteButton is not null)
                thisControl.PasteButton.Theme = newStyle;
        }

        /// <summary>
        /// Returns the default style for toolbar buttons in a stack panel layout.
        /// Cached after first creation to avoid repeated resource lookups.
        /// </summary>
        /// <returns>The default <see cref="ControlTheme"/> for toolbar buttons.</returns>
        private static ControlTheme _cachedDefaultButtonStyle;
        /// <summary>
        /// Creates or returns the cached default style for compact toolbar buttons.
        /// </summary>
        /// <returns>The default stack-panel button style.</returns>
        /// <remarks>
        /// XGoodFit port: the ToolBar key is Themes' string key "ToolBar.ButtonStyleKey". Where WPF
        /// throws without the themes, this falls back to the button's own theme, and caches the style
        /// only once it found the ToolBar style. The IsEnabled trigger is a :disabled style.
        /// </remarks>
        private static ControlTheme DefaultStackPanelButtonStyle()
        {
            if (_cachedDefaultButtonStyle != null)
                return _cachedDefaultButtonStyle;

            var toolBarButtonStyle = Application.Current?.TryFindResource("ToolBar.ButtonStyleKey") as ControlTheme;
            var s = new ControlTheme(typeof(Button)) { BasedOn = toolBarButtonStyle ?? Application.Current?.TryFindResource(typeof(Button)) as ControlTheme };
            s.Setters.Add(new Setter(Layoutable.HeightProperty, 24d));
            s.Setters.Add(new Setter(Layoutable.WidthProperty, 24d));
            s.Setters.Add(new Setter(InputElement.CursorProperty, Cursors.Hand));
            var disabledShadeTrigger = new Style(x => x.Nesting().Class(":disabled"));
            disabledShadeTrigger.Setters.Add(new Setter(Visual.OpacityProperty, 0.5d));
            s.Children.Add(disabledShadeTrigger);

            if (toolBarButtonStyle != null)
                _cachedDefaultButtonStyle = s;
            return s;
        }

        /// <summary>
        /// Gets or sets the stack panel button style.
        /// </summary>
        public ControlTheme StackPanelButtonStyle
        {
            get
            {
                return this.GetValue(StackPanelButtonStyleProperty);
            }
            set
            {
                this.SetValue(StackPanelButtonStyleProperty, value);
            }
        }

        /// <summary>
        /// Dependency property for the stack panel separator style.
        /// </summary>
        public static readonly StyledProperty<ControlTheme> StackPanelSeparatorStyleProperty = AvaloniaProperty.Register<DataGridToolbar, ControlTheme>(nameof(StackPanelSeparatorStyle), DefaultStackPanelSeparatorStyle());

        /// <summary>
        /// Callback method invoked when the StackPanelSeperatorStyle property changes.
        /// </summary>
        /// <param name="d">The dependency object.</param>
        /// <param name="e">The event data containing old and new values.</param>
        private static void StackPanelSeperatorStylePropertyCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            DataGridToolbar thisControl = (DataGridToolbar)d;
            //
            ControlTheme newStyle = e.NewValue as ControlTheme;
            //
            if (thisControl.EditSelectSeparator is not null)
                thisControl.EditSelectSeparator.Theme = newStyle;
            if (thisControl.CustomOptionsSeparator is not null)
                thisControl.CustomOptionsSeparator.Theme = newStyle;
        }

        /// <summary>
        /// Returns the default style for separators in the toolbar.
        /// </summary>
        /// <returns>The default <see cref="ControlTheme"/>.</returns>
        /// <remarks>
        /// XGoodFit port: Avalonia controls have no LayoutTransform to turn the separator upright. The
        /// style is based on Themes' VerticalSeparatorStyle, the ported upright separator, or else on
        /// the separator's own theme.
        /// </remarks>
        private static ControlTheme DefaultStackPanelSeparatorStyle()
        {
            var s = new ControlTheme(typeof(Separator))
            {
                BasedOn = Application.Current?.TryFindResource("VerticalSeparatorStyle") as ControlTheme
                    ?? Application.Current?.TryFindResource(typeof(Separator)) as ControlTheme,
            };
            s.Setters.Add(new Setter(Layoutable.MarginProperty, new Thickness(2d, 1d, 2d, 1d)));
            // Vertical text

            return s;
        }

        /// <summary>
        /// Gets or sets the stack panel separator style.
        /// </summary>
        public ControlTheme StackPanelSeparatorStyle
        {
            get
            {
                return this.GetValue(StackPanelSeparatorStyleProperty);
            }
            set
            {
                this.SetValue(StackPanelSeparatorStyleProperty, value);
            }
        }

        private ObservableCollection<Button> _customButtons = new ObservableCollection<Button>();

        /// <summary>
        /// Gets the collection of custom buttons added by the user.
        /// These are displayed in the toolbar between the Edit and Clipboard groups.
        /// </summary>
        public ObservableCollection<Button> CustomButtons
        {
            get
            {
                return _customButtons;
            }
            private set
            {
                // do nothing
            }
        }
        #endregion

        #region Methods

        /// <summary>
        /// Handles the AddRowsButton click event and adds rows to the data grid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void AddRowsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;
            if (DataGrid.CanUserAddInsertDeleteRows)
            {
                var uniqueRows = DataGrid.GetRowsWithSelectedCells();
                DataGrid.AddRows(Math.Max(uniqueRows.Count, 1));
                DataGrid.Focus();
            }
        }

        /// <summary>
        /// Handles the InsertRowsButton click event and inserts rows into the data grid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void InsertRowsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;
            if (DataGrid.CanUserAddInsertDeleteRows)
                DataGrid.InsertRows();
            DataGrid.Focus();
        }

        /// <summary>
        /// Handles the DeleteRowsButton click event and deletes rows from the data grid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void DeleteRowsButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;
            if (DataGrid.CanUserAddInsertDeleteRows)
                DataGrid.DeleteRows();
            DataGrid.Focus();
        }


        private bool _allCellsSelected;

        /// <summary>
        /// Toggles selection of all cells in the associated data grid.
        /// </summary>
        /// <param name="sender">The select-all button that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;

            if (_allCellsSelected)
            {
                DataGrid.UnselectAllCells();
            }
            else
            {
                DataGrid.SelectAllCells();
            }
            DataGrid.Focus();
        }

        /// <summary>
        /// Updates the SelectAll button icon and tooltip to reflect the current selection state.
        /// </summary>
        private void UpdateSelectAllButton()
        {
            // XGoodFit port: Avalonia's FindResource; ToolTip is WpfCompat's.
            if (_allCellsSelected)
            {
                SelectAllButton.Content = this.FindResource("DeselectAllIcon");
                SelectAllButton.ToolTip = "Deselect all cells";
            }
            else
            {
                SelectAllButton.Content = this.FindResource("SelectAllIcon");
                SelectAllButton.ToolTip = "Select all cells";
            }
        }

        /// <summary>
        /// Handles the CopyButton click event and copies the selected cells to the clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CopyButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;
            DataGrid.CopySelectedCellsToClipboard(false);
            DataGrid.Focus();
        }

        /// <summary>
        /// Handles the CopyWithHeadersButton click event and copies the selected cells with headers to the clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CopyWithHeadersButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;
            DataGrid.CopySelectedCellsToClipboard(true);
            DataGrid.Focus();
        }

        /// <summary>
        /// Handles the PasteButton click event and pastes data from the clipboard into the data grid.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PasteButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataGrid == null)
                return;
            // Only paste if there is data in the clipboard.
            try
            {
                string[][] clipboardData = Clipboard.GetText().Split('\n').Select(row => row.Split('\t').Select(Clipboardcell => Clipboardcell.Length > 0 && Clipboardcell[Clipboardcell.Length - 1] == '\r' ? Clipboardcell.Substring(0, Clipboardcell.Length - 1) : Clipboardcell).ToArray()).Where(a => a.Any(b => b.Length > 0)).ToArray();
                if (clipboardData.Length > 0)
                    DataGrid.PasteClipboard();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex);
            }
            DataGrid.Focus();
        }

        #endregion

    }
}
