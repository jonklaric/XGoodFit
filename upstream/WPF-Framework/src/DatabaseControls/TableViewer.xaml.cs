using System;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using DatabaseManager;
using Microsoft.Win32;

namespace DatabaseControls
{
    /// <summary>
    /// A WPF control for viewing and interacting with database tables.
    /// </summary>
    public partial class TableViewer : UserControl
    {
        #region Enumerables

        /// <summary>
        /// Defines the selection states supported by the table viewer.
        /// </summary>
        private enum SelectionMode : byte
        {
            CellSelect = 0,
            RowSelect = 1,
            ColumnSelect = 2,
            None = 3,
            All = 4,
            EditSelect = 5
        }

        /// <summary>
        /// Defines the sort state applied to a table column.
        /// </summary>
        private enum SortOrder : byte
        {
            Ascending = 2,
            Descending = 1,
            None = 0
        }

        #endregion

        #region Fields

        private int _visibleRowCount;
        private bool _isLoaded = false;
        private int _activeCellDataColumnIndex = 0;
        private int _activeCellVirtualRowIndex = 0;
        private int _mouseDownVirtualRowIndex;
        private int _mouseDownColumnIndex;
        private SelectionMode _mouseSelectionMode = SelectionMode.None;
        private List<int> _selectedDataRowIndices = new List<int>();
        private List<int> _selectedColumnIndices = new List<int>();
        private SortedDictionary<int, SortedSet<int>> _selectedCellIndices = new SortedDictionary<int, SortedSet<int>>();
        private bool _selectedRowsOnly = false;
        private bool _visualRefreshPending = false;
        private string _attributeSelectorString = "";
        private int[]? _rowOffset;
        private int[]? _rowId;
        private int[]? _sortedSelectedRowOffsets;
        private SortOrder _columnSortOrder;
        private SortOrder[]? _columnsSortedOrder;
        private GridLength[]? _columnWidths;
        private readonly TextBox _cellEditTextBox;
        private string _fieldCalculatorString = "";
        private HashSet<int> _readOnlyColumns = new HashSet<int>();

        #endregion

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="DataView"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DataViewProperty = DependencyProperty.Register(
            nameof(DataView), typeof(DataTableView), typeof(TableViewer),
            new UIPropertyMetadata(null, new PropertyChangedCallback(LoadView)));

        /// <summary>
        /// Identifies the <see cref="AllCellsSelected"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AllCellsSelectedProperty = DependencyProperty.Register(
            nameof(AllCellsSelected), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(false, OnAllCellsSelectedChanged));

        /// <summary>
        /// Updates the select-all button when the all-cells selection state changes.
        /// </summary>
        /// <param name="d">The table viewer whose property changed.</param>
        /// <param name="e">The dependency-property change details.</param>
        private static void OnAllCellsSelectedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TableViewer viewer)
                viewer.UpdateSelectAllButton();
        }

        /// <summary>
        /// Identifies the <see cref="RowHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowHeightProperty = DependencyProperty.Register(
            nameof(RowHeight), typeof(double), typeof(TableViewer), new UIPropertyMetadata(23.0));

        /// <summary>
        /// Identifies the <see cref="ColumnHeaderHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderHeightProperty = DependencyProperty.Register(
            nameof(ColumnHeaderHeight), typeof(GridLength), typeof(TableViewer), new UIPropertyMetadata(GridLength.Auto));

        /// <summary>
        /// Identifies the <see cref="ColumnHeaderTextblockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderTextblockStyleProperty = DependencyProperty.Register(
            nameof(ColumnHeaderTextblockStyle), typeof(Style), typeof(TableViewer), new PropertyMetadata(GetDefaultColumnHeaderTextblockStyle()));

        /// <summary>
        /// Identifies the <see cref="ColumnHeaderBorderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnHeaderBorderStyleProperty = DependencyProperty.Register(
            nameof(ColumnHeaderBorderStyle), typeof(Style), typeof(TableViewer), new PropertyMetadata(GetDefaultColumnHeaderBorderStyle()));

        /// <summary>
        /// Identifies the <see cref="RowHeaderTextblockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowHeaderTextblockStyleProperty = DependencyProperty.Register(
            nameof(RowHeaderTextblockStyle), typeof(Style), typeof(TableViewer), new PropertyMetadata(GetDefaultRowHeaderTextblockStyle()));

        /// <summary>
        /// Identifies the <see cref="RowHeaderBorderStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowHeaderBorderStyleProperty = DependencyProperty.Register(
            nameof(RowHeaderBorderStyle), typeof(Style), typeof(TableViewer), new PropertyMetadata(GetDefaultRowHeaderBorderStyle()));

        /// <summary>
        /// Identifies the <see cref="ShowRowHeaders"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowRowHeadersProperty = DependencyProperty.Register(
            nameof(ShowRowHeaders), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="Editable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty EditableProperty = DependencyProperty.Register(
            nameof(Editable), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(false, OnEditableChanged));

        /// <summary>
        /// Updates paste availability when editability changes.
        /// </summary>
        /// <param name="d">The table viewer whose property changed.</param>
        /// <param name="e">The dependency-property change details.</param>
        private static void OnEditableChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is TableViewer tv) tv.UpdatePasteButtonState();
        }

        /// <summary>
        /// Identifies the <see cref="HasFieldCalculator"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasFieldCalculatorProperty = DependencyProperty.Register(
            nameof(HasFieldCalculator), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="HasSaveButton"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasSaveButtonProperty = DependencyProperty.Register(
            nameof(HasSaveButton), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="HasUndoRedoButtons"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasUndoRedoButtonsProperty = DependencyProperty.Register(
            nameof(HasUndoRedoButtons), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="RowSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowSelectableProperty = DependencyProperty.Register(
            nameof(RowSelectable), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CellTextblockStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellTextblockStyleProperty = DependencyProperty.Register(
            nameof(CellTextblockStyle), typeof(Style), typeof(TableViewer), new PropertyMetadata(GetDefaultCellTextblockStyle()));

        /// <summary>
        /// Identifies the <see cref="SelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedColorProperty = DependencyProperty.Register(
            nameof(SelectedColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(240, 0, 120, 215)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="InactiveSelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InactiveSelectedColorProperty = DependencyProperty.Register(
            nameof(InactiveSelectedColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromRgb(196, 213, 240)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ActiveCellForeground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActiveCellForegroundProperty = DependencyProperty.Register(
            nameof(ActiveCellForeground), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Colors.White), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ActiveCellBackground"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ActiveCellBackgroundProperty = DependencyProperty.Register(
            nameof(ActiveCellBackground), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 21, 107, 176)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DeSelectedColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeSelectedColorProperty = DependencyProperty.Register(
            nameof(DeSelectedColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(Brushes.Transparent, OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="SelectedForegroundColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SelectedForegroundColorProperty = DependencyProperty.Register(
            nameof(SelectedForegroundColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Colors.White), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="InactiveSelectedForegroundColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty InactiveSelectedForegroundColorProperty = DependencyProperty.Register(
            nameof(InactiveSelectedForegroundColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromRgb(30, 30, 30)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="DeSelectedForegroundColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty DeSelectedForegroundColorProperty = DependencyProperty.Register(
            nameof(DeSelectedForegroundColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(Brushes.Black, OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="RowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowColorProperty = DependencyProperty.Register(
            nameof(RowColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(Brushes.White, OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="AlternateRowColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AlternateRowColorProperty = DependencyProperty.Register(
            nameof(AlternateRowColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 243, 249, 247)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="RowLineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowLineColorProperty = DependencyProperty.Register(
            nameof(RowLineColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 53, 59, 122)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="RowLineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty RowLineThicknessProperty = DependencyProperty.Register(
            nameof(RowLineThickness), typeof(double), typeof(TableViewer), new UIPropertyMetadata(1.0, OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ColumnLineColor"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnLineColorProperty = DependencyProperty.Register(
            nameof(ColumnLineColor), typeof(Brush), typeof(TableViewer), new UIPropertyMetadata(new SolidColorBrush(Color.FromArgb(255, 53, 59, 122)), OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ColumnLineThickness"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnLineThicknessProperty = DependencyProperty.Register(
            nameof(ColumnLineThickness), typeof(double), typeof(TableViewer), new UIPropertyMetadata(1.0, OnVisualPropertyChanged));

        /// <summary>
        /// Identifies the <see cref="ColumnSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ColumnSelectableProperty = DependencyProperty.Register(
            nameof(ColumnSelectable), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="CellSelectable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CellSelectableProperty = DependencyProperty.Register(
            nameof(CellSelectable), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(true));

        /// <summary>
        /// Identifies the <see cref="AutoFitColumns"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty AutoFitColumnsProperty = DependencyProperty.Register(
            nameof(AutoFitColumns), typeof(bool), typeof(TableViewer), new UIPropertyMetadata(false));

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the data source for the table viewer.
        /// </summary>
        /// <value>The <see cref="DataTableView"/> containing the data to display.</value>
        public DataTableView DataView
        {
            get => (DataTableView)GetValue(DataViewProperty);
            set => SetValue(DataViewProperty, value);
        }

        /// <summary>
        /// Gets a value indicating whether only selected rows are currently being displayed.
        /// </summary>
        /// <value><c>true</c> if showing selected rows only; otherwise, <c>false</c>.</value>
        public bool ShowingSelectedRowsOnly => _selectedRowsOnly;

        /// <summary>
        /// Gets the list of currently selected row indices in the data table.
        /// </summary>
        /// <value>A list of zero-based row indices that are currently selected.</value>
        public List<int> GetSelectedRows => _selectedDataRowIndices;

        /// <summary>
        /// Gets the row index of the currently active cell.
        /// </summary>
        /// <value>The zero-based row index of the active cell in the virtual (visible) grid.</value>
        public int ActiveCellRowIndex => _activeCellVirtualRowIndex;

        /// <summary>
        /// Gets the column index of the currently active cell.
        /// </summary>
        /// <value>The zero-based column index of the active cell in the data table.</value>
        public int ActiveCellColumnIndex => _activeCellDataColumnIndex;

        /// <summary>
        /// Gets or sets a value indicating whether all cells in the table are selected.
        /// </summary>
        /// <value><c>true</c> if all cells are selected; otherwise, <c>false</c>.</value>
        public bool AllCellsSelected
        {
            get => (bool)GetValue(AllCellsSelectedProperty);
            set => SetValue(AllCellsSelectedProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of each data row in pixels.
        /// </summary>
        /// <value>The row height in pixels. Default is 23.0.</value>
        public double RowHeight
        {
            get => (double)GetValue(RowHeightProperty);
            set => SetValue(RowHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the column header row.
        /// </summary>
        /// <value>The column header height as a <see cref="GridLength"/>. Default is Auto.</value>
        public GridLength ColumnHeaderHeight
        {
            get => (GridLength)GetValue(ColumnHeaderHeightProperty);
            set => SetValue(ColumnHeaderHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to column header text blocks.
        /// </summary>
        /// <value>The <see cref="Style"/> for column header TextBlock elements.</value>
        public Style ColumnHeaderTextblockStyle
        {
            get => (Style)GetValue(ColumnHeaderTextblockStyleProperty);
            set => SetValue(ColumnHeaderTextblockStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to column header borders.
        /// </summary>
        /// <value>The <see cref="Style"/> for column header Border elements.</value>
        public Style ColumnHeaderBorderStyle
        {
            get => (Style)GetValue(ColumnHeaderBorderStyleProperty);
            set => SetValue(ColumnHeaderBorderStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to row header text blocks.
        /// </summary>
        /// <value>The <see cref="Style"/> for row header TextBlock elements.</value>
        public Style RowHeaderTextblockStyle
        {
            get => (Style)GetValue(RowHeaderTextblockStyleProperty);
            set => SetValue(RowHeaderTextblockStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to row header borders.
        /// </summary>
        /// <value>The <see cref="Style"/> for row header Border elements.</value>
        public Style RowHeaderBorderStyle
        {
            get => (Style)GetValue(RowHeaderBorderStyleProperty);
            set => SetValue(RowHeaderBorderStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether row headers are visible.
        /// </summary>
        /// <value><c>true</c> to show row headers; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ShowRowHeaders
        {
            get => (bool)GetValue(ShowRowHeadersProperty);
            set => SetValue(ShowRowHeadersProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether cells can be edited.
        /// </summary>
        /// <value><c>true</c> if the table is editable; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        public bool Editable
        {
            get => (bool)GetValue(EditableProperty);
            set => SetValue(EditableProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the field calculator button is visible.
        /// </summary>
        /// <value><c>true</c> to show the field calculator; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool HasFieldCalculator
        {
            get => (bool)GetValue(HasFieldCalculatorProperty);
            set => SetValue(HasFieldCalculatorProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the save button is visible.
        /// </summary>
        /// <value><c>true</c> to show the save button; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool HasSaveButton
        {
            get => (bool)GetValue(HasSaveButtonProperty);
            set => SetValue(HasSaveButtonProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether the undo/redo buttons are visible.
        /// </summary>
        /// <value><c>true</c> to show undo/redo buttons; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool HasUndoRedoButtons
        {
            get => (bool)GetValue(HasUndoRedoButtonsProperty);
            set => SetValue(HasUndoRedoButtonsProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether entire rows can be selected.
        /// </summary>
        /// <value><c>true</c> to allow row selection; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool RowSelectable
        {
            get => (bool)GetValue(RowSelectableProperty);
            set => SetValue(RowSelectableProperty, value);
        }

        /// <summary>
        /// Gets or sets the style applied to cell text blocks.
        /// </summary>
        /// <value>The <see cref="Style"/> for cell TextBlock elements.</value>
        public Style CellTextblockStyle
        {
            get => (Style)GetValue(CellTextblockStyleProperty);
            set => SetValue(CellTextblockStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for selected cells.
        /// </summary>
        /// <value>The <see cref="Brush"/> used to fill selected cells.</value>
        public Brush SelectedColor
        {
            get => (Brush)GetValue(SelectedColorProperty);
            set => SetValue(SelectedColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for selected cells when table selection is inactive.
        /// </summary>
        /// <value>The <see cref="Brush"/> used to fill inactive selected cells.</value>
        public Brush InactiveSelectedColor
        {
            get => (Brush)GetValue(InactiveSelectedColorProperty);
            set => SetValue(InactiveSelectedColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground color for the active cell.
        /// </summary>
        /// <value>The <see cref="Brush"/> used for the active cell's text.</value>
        public Brush ActiveCellForeground
        {
            get => (Brush)GetValue(ActiveCellForegroundProperty);
            set => SetValue(ActiveCellForegroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for the active cell.
        /// </summary>
        /// <value>The <see cref="Brush"/> used to fill the active cell.</value>
        public Brush ActiveCellBackground
        {
            get => (Brush)GetValue(ActiveCellBackgroundProperty);
            set => SetValue(ActiveCellBackgroundProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for deselected cells.
        /// </summary>
        /// <value>The <see cref="Brush"/> used to fill deselected cells.</value>
        public Brush DeSelectedColor
        {
            get => (Brush)GetValue(DeSelectedColorProperty);
            set => SetValue(DeSelectedColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground color for selected cells.
        /// </summary>
        /// <value>The <see cref="Brush"/> used for selected cell text.</value>
        public Brush SelectedForegroundColor
        {
            get => (Brush)GetValue(SelectedForegroundColorProperty);
            set => SetValue(SelectedForegroundColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground color for selected cells when table selection is inactive.
        /// </summary>
        /// <value>The <see cref="Brush"/> used for inactive selected cell text.</value>
        public Brush InactiveSelectedForegroundColor
        {
            get => (Brush)GetValue(InactiveSelectedForegroundColorProperty);
            set => SetValue(InactiveSelectedForegroundColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the foreground color for deselected cells.
        /// </summary>
        /// <value>The <see cref="Brush"/> used for deselected cell text.</value>
        public Brush DeSelectedForegroundColor
        {
            get => (Brush)GetValue(DeSelectedForegroundColorProperty);
            set => SetValue(DeSelectedForegroundColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for odd-numbered rows.
        /// </summary>
        /// <value>The <see cref="Brush"/> used to fill odd rows.</value>
        public Brush RowColor
        {
            get => (Brush)GetValue(RowColorProperty);
            set => SetValue(RowColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the background color for even-numbered rows.
        /// </summary>
        /// <value>The <see cref="Brush"/> used to fill even rows.</value>
        public Brush AlternateRowColor
        {
            get => (Brush)GetValue(AlternateRowColorProperty);
            set => SetValue(AlternateRowColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of horizontal row separator lines.
        /// </summary>
        /// <value>The <see cref="Brush"/> used for row lines.</value>
        public Brush RowLineColor
        {
            get => (Brush)GetValue(RowLineColorProperty);
            set => SetValue(RowLineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of horizontal row separator lines.
        /// </summary>
        /// <value>The line thickness in pixels. Default is 1.0.</value>
        public double RowLineThickness
        {
            get => (double)GetValue(RowLineThicknessProperty);
            set => SetValue(RowLineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets the color of vertical column separator lines.
        /// </summary>
        /// <value>The <see cref="Brush"/> used for column lines.</value>
        public Brush ColumnLineColor
        {
            get => (Brush)GetValue(ColumnLineColorProperty);
            set => SetValue(ColumnLineColorProperty, value);
        }

        /// <summary>
        /// Gets or sets the thickness of vertical column separator lines.
        /// </summary>
        /// <value>The line thickness in pixels. Default is 1.0.</value>
        public double ColumnLineThickness
        {
            get => (double)GetValue(ColumnLineThicknessProperty);
            set => SetValue(ColumnLineThicknessProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether entire columns can be selected.
        /// </summary>
        /// <value><c>true</c> to allow column selection; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool ColumnSelectable
        {
            get => (bool)GetValue(ColumnSelectableProperty);
            set => SetValue(ColumnSelectableProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether individual cells can be selected.
        /// </summary>
        /// <value><c>true</c> to allow cell selection; otherwise, <c>false</c>. Default is <c>true</c>.</value>
        public bool CellSelectable
        {
            get => (bool)GetValue(CellSelectableProperty);
            set => SetValue(CellSelectableProperty, value);
        }

        /// <summary>
        /// Gets or sets a value indicating whether column widths are automatically adjusted to fit content.
        /// </summary>
        /// <value><c>true</c> to auto-fit columns; otherwise, <c>false</c>. Default is <c>false</c>.</value>
        public bool AutoFitColumns
        {
            get => (bool)GetValue(AutoFitColumnsProperty);
            set => SetValue(AutoFitColumnsProperty, value);
        }

        #endregion

        #region Events

        /// <summary>
        /// Event handler delegate for selected row indices changed event.
        /// </summary>
        /// <param name="selectedRowIndices">The list of selected row indices.</param>
        public delegate void SelectedRowIndicesChangedEventHandler(List<int> selectedRowIndices);

        /// <summary>
        /// Event handler delegate for row right button up event.
        /// </summary>
        /// <param name="rowUpMenu">The context menu for the row.</param>
        /// <param name="dataRowIndex">The data row index.</param>
        public delegate void RowRightButtonUpEventHandler(ContextMenu rowUpMenu, int dataRowIndex);

        /// <summary>
        /// Occurs when the set of selected row indices changes.
        /// </summary>
        public event SelectedRowIndicesChangedEventHandler? SelectedRowIndicesChanged;

        /// <summary>
        /// Occurs when the active cell location changes.
        /// </summary>
        public event Action? ActiveCellLocationChanged;

        /// <summary>
        /// Occurs when the right mouse button is released on a row header.
        /// Provides the context menu and row index for custom menu handling.
        /// </summary>
        public event RowRightButtonUpEventHandler? RowRightButtonUp;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="TableViewer"/> class.
        /// </summary>
        public TableViewer()
        {
            InitializeComponent();
            _cellEditTextBox = new TextBox { Padding = new Thickness(0, 3, 0, 2), Margin = new Thickness(0, 0, 0, 1) };
            _cellEditTextBox.PreviewKeyDown += PreviewEditText;
            _cellEditTextBox.LostFocus += EditTextLostFocus;
            EditorToolbar.IsEnabled = false;
            GotFocus += (_, _) => UpdatePasteButtonState();
            IsKeyboardFocusWithinChanged += TableSelectionFocusWithinChanged;
            GridPanel.GotKeyboardFocus += TableSelectionFocusChanged;
            GridPanel.LostKeyboardFocus += TableSelectionFocusChanged;
        }

        #endregion

        #region Loading

        /// <summary>
        /// Schedules a visual refresh after a styling dependency property changes.
        /// </summary>
        /// <param name="d">The table viewer whose visual property changed.</param>
        /// <param name="e">The dependency-property change details.</param>
        private static void OnVisualPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not TableViewer tv) return;
            if (tv.DataView == null || tv.GridPanel.Children.Count == 0) return;
            // Coalesce multiple DP changes (e.g. theme switch updates many colors at once)
            // into a single visual refresh via dispatcher.
            if (!tv._visualRefreshPending)
            {
                tv._visualRefreshPending = true;
                tv.Dispatcher.InvokeAsync(() =>
                {
                    tv._visualRefreshPending = false;
                    if (tv.DataView == null || tv.GridPanel.Children.Count == 0) return;
                    tv.LoadRows();
                    // Update column grid line colors (not recreated by LoadRows)
                    int colLineCount = tv.DataView.ColumnNames.Count() + 1;
                    for (int i = 0; i < colLineCount && i < tv.GridLinesCanvas.Children.Count; i++)
                    {
                        if (tv.GridLinesCanvas.Children[i] is Line colLine)
                            colLine.Stroke = tv.ColumnLineColor;
                    }
                    // Update column header border brushes and top boundary line (not recreated by LoadRows)
                    var headerBorderBrush = (Brush)tv.FindResource("DataGrid.Header.Border");
                    foreach (UIElement child in tv.ColumnHeadersGrid.Children)
                    {
                        if (child is ColumnHeader header)
                            header.BorderBrush = headerBorderBrush;
                    }
                    // Update the initial row line (top boundary between headers and cells)
                    int topLineIndex = tv.DataView.ColumnNames.Count() + 1;
                    if (topLineIndex < tv.GridLinesCanvas.Children.Count &&
                        tv.GridLinesCanvas.Children[topLineIndex] is Line topLine)
                    {
                        topLine.Stroke = headerBorderBrush;
                    }
                    tv.DeSelectAllCells();
                    tv.SetSelectedCells();
                    tv.UpdateRowHeaders();
                }, System.Windows.Threading.DispatcherPriority.Render);
            }
        }

        /// <summary>
        /// Handles table body keyboard focus changes by repainting selected cells with active or inactive selection brushes.
        /// </summary>
        /// <param name="sender">The source of the keyboard focus change.</param>
        /// <param name="e">The keyboard focus event data.</param>
        private void TableSelectionFocusChanged(object sender, KeyboardFocusChangedEventArgs e)
        {
            RefreshSelectionVisuals();
        }

        /// <summary>
        /// Handles control-wide keyboard focus changes by repainting selected cells with active or inactive selection brushes.
        /// </summary>
        /// <param name="sender">The source of the keyboard focus change.</param>
        /// <param name="e">The focus-within property change data.</param>
        private void TableSelectionFocusWithinChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshSelectionVisuals();
        }

        /// <summary>
        /// Attaches a new <see cref="DataView"/> and rebuilds table state for it.
        /// </summary>
        /// <param name="d">The table viewer whose data view changed.</param>
        /// <param name="e">The dependency-property change details.</param>
        private static void LoadView(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d == null || d.GetType() != typeof(TableViewer)) return;
            var thisControl = (TableViewer)d;

            if (e.OldValue != null && e.OldValue is DataTableView oldView)
            {
                oldView.RowsAdded -= thisControl.TableViewRowsAdded;
                oldView.RowsDeleted -= thisControl.TableViewRowsDeleted;
                oldView.ColumnsAdded -= thisControl.TableViewColumnsAdded;
                oldView.ColumnsDeleted -= thisControl.TableViewColumnsDeleted;
            }

            if (e.NewValue == null || !(e.NewValue is DataTableView))
            {
                thisControl._selectedCellIndices = new SortedDictionary<int, SortedSet<int>>();
                thisControl._selectedColumnIndices = new List<int>();
                thisControl._selectedDataRowIndices = new List<int>();
                thisControl._activeCellDataColumnIndex = 0;
                thisControl._activeCellVirtualRowIndex = 0;
                thisControl._selectedRowsOnly = false;
                thisControl.ColumnHeadersGrid.SizeChanged -= thisControl.ColumnsGridSizeChanged;
                thisControl.GridPanel.Children.Clear();
                thisControl.RowHeadersGrid.Children.Clear();
                thisControl.RowColorGrid.Children.Clear();
                thisControl.ColumnHeadersGrid.Children.Clear();
                thisControl.GridPanel.RowDefinitions.Clear();
                thisControl.RowHeadersGrid.RowDefinitions.Clear();
                thisControl.RowColorGrid.RowDefinitions.Clear();
                thisControl.ColumnHeadersGrid.ColumnDefinitions.Clear();
                thisControl.GridPanel.ColumnDefinitions.Clear();
                thisControl.GridLinesCanvas.Children.Clear();
                thisControl.SelectionToolbar.IsEnabled = false;
                thisControl.EditorToolbar.IsEnabled = false;
            }
            else
            {
                var newView = (DataTableView)e.NewValue;
                newView.RowsAdded += thisControl.TableViewRowsAdded;
                newView.RowsDeleted += thisControl.TableViewRowsDeleted;
                newView.ColumnsAdded += thisControl.TableViewColumnsAdded;
                newView.ColumnsDeleted += thisControl.TableViewColumnsDeleted;

                if (!newView.ParentDatabase.DataBaseOpen)
                {
                    try
                    {
                        newView.ParentDatabase.Open();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Failed to open database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return;
                    }
                }

                thisControl._columnSortOrder = SortOrder.None;
                thisControl._columnsSortedOrder = new SortOrder[newView.ColumnNames.Count()];

                thisControl.Refresh();

                if (thisControl.ShowRowHeaders)
                {
                    double pixelsPerDip = VisualTreeHelper.GetDpi(thisControl).PixelsPerDip;
                    thisControl.RowHeadersColumnDefinition.Width = new GridLength(
                        (int)(new FormattedText(newView.NumberOfRows.ToString(), CultureInfo.GetCultureInfo("en-us"),
                            FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                            12, Brushes.Black, pixelsPerDip).Width) + 10);
                }

                thisControl.SelectionToolbar.IsEnabled = true;
                thisControl.EditorToolbar.IsEnabled = true;
            }
        }

        /// <summary>
        /// Marks the specified columns as read-only, preventing edits to their cells.
        /// </summary>
        /// <param name="columnIndices">Array of zero-based column indices to mark as read-only.</param>
        public void SetColumnsAsReadOnly(int[] columnIndices)
        {
            foreach (var idx in columnIndices) _readOnlyColumns.Add(idx);
        }

        /// <summary>
        /// Marks the specified columns as read-only by column name, preventing edits to their cells.
        /// </summary>
        /// <param name="columnNames">Array of column names to mark as read-only.</param>
        public void SetColumnsAsReadOnly(string[] columnNames)
        {
            foreach (var name in columnNames)
            {
                int idx = Array.IndexOf(DataView.ColumnNames, name);
                if (idx >= 0) _readOnlyColumns.Add(idx);
            }
        }

        #endregion

        #region Database Events

        /// <summary>
        /// Handles the event when columns are deleted from the data view.
        /// Updates selection indices, active cell position, and sort order accordingly.
        /// </summary>
        /// <param name="columnIndices">Array of indices of the deleted columns.</param>
        private void TableViewColumnsDeleted(int[] columnIndices)
        {
            Array.Sort(columnIndices);
            for (int i = columnIndices.Length - 1; i >= 0; i--)
            {
                // Update selected column indices
                _selectedColumnIndices.Remove(columnIndices[i]);
                for (int j = 0; j < _selectedColumnIndices.Count; j++)
                    if (_selectedColumnIndices[j] > columnIndices[i]) _selectedColumnIndices[j] -= 1;

                // Delete column from selected cells collection
                var keysToRemove = new List<int>();
                foreach (var selectedCellsByRow in _selectedCellIndices)
                {
                    selectedCellsByRow.Value.Remove(columnIndices[i]);
                    if (selectedCellsByRow.Value.Count == 0) keysToRemove.Add(selectedCellsByRow.Key);
                }
                foreach (var keyToRemove in keysToRemove)
                    _selectedCellIndices.Remove(keyToRemove);

                foreach (var selectedCellsByRow in _selectedCellIndices)
                {
                    var columnsToUpdate = new List<int>();
                    foreach (var selectedCellColumn in selectedCellsByRow.Value)
                        if (selectedCellColumn >= columnIndices[i]) columnsToUpdate.Add(selectedCellColumn);

                    foreach (var columnToUpdate in columnsToUpdate)
                        selectedCellsByRow.Value.Remove(columnToUpdate);

                    foreach (var columnToUpdate in columnsToUpdate)
                        selectedCellsByRow.Value.Add(columnToUpdate - 1);
                }

                // Update active cell
                if (_activeCellDataColumnIndex >= columnIndices[i]) _activeCellDataColumnIndex -= 1;
                if (_activeCellDataColumnIndex < 0) _activeCellDataColumnIndex = 0;

                // Update sort order
                if (_columnsSortedOrder![columnIndices[i]] != SortOrder.None) RemoveSort();

                var updated = new SortOrder[_columnsSortedOrder.Length - 1];
                for (int j = 0; j < columnIndices[i]; j++) updated[j] = _columnsSortedOrder[j];
                for (int j = columnIndices[i]; j < _columnsSortedOrder.Length - 1; j++) updated[j] = _columnsSortedOrder[j + 1];
                _columnsSortedOrder = updated;
            }
            // Update undo and redo buttons if needed
            UpdateUndoRedoButtons();
            // Refresh viewer to include new column
            NumberOfColumnsChanged();
        }

        /// <summary>
        /// Handles the event when columns are added to the data view.
        /// Updates selection indices, active cell position, and sort order accordingly.
        /// </summary>
        /// <param name="columnIndices">Array of indices where columns were added.</param>
        private void TableViewColumnsAdded(int[] columnIndices)
        {
            Array.Sort(columnIndices);
            for (int i = 0; i < columnIndices.Length; i++)
            {
                // Update selected column indices
                for (int j = 0; j < _selectedColumnIndices.Count; j++)
                    if (_selectedColumnIndices[j] >= columnIndices[i]) _selectedColumnIndices[j] += 1;

                // Adjust column in selected cells collection
                foreach (var selectedCellsByRow in _selectedCellIndices)
                {
                    var columnsToUpdate = new List<int>();
                    foreach (var selectedCellColumn in selectedCellsByRow.Value)
                        if (selectedCellColumn >= columnIndices[i]) columnsToUpdate.Add(selectedCellColumn);

                    foreach (var columnToUpdate in columnsToUpdate)
                        selectedCellsByRow.Value.Remove(columnToUpdate);

                    foreach (var columnToUpdate in columnsToUpdate)
                        selectedCellsByRow.Value.Add(columnToUpdate + 1);
                }

                // Update active cell
                if (_activeCellDataColumnIndex >= columnIndices[i]) _activeCellDataColumnIndex += 1;
                if (_activeCellDataColumnIndex > DataView.ColumnNames.Count() - 1)
                    _activeCellDataColumnIndex = DataView.ColumnNames.Count() - 1;

                // Update sort order
                var updated = new SortOrder[_columnsSortedOrder!.Length + 1];
                for (int j = 0; j < columnIndices[i]; j++) updated[j] = _columnsSortedOrder[j];
                updated[columnIndices[i]] = SortOrder.None;
                for (int j = columnIndices[i] + 1; j < updated.Length; j++) updated[j] = _columnsSortedOrder[j - 1];
                _columnsSortedOrder = updated;
            }
            UpdateUndoRedoButtons();
            NumberOfColumnsChanged();
        }

        /// <summary>
        /// Handles the event when rows are added to the data view.
        /// Updates selection indices and active cell position accordingly.
        /// </summary>
        /// <param name="rowIndices">Array of indices where rows were added.</param>
        private void TableViewRowsAdded(int[] rowIndices)
        {
            Array.Sort(rowIndices);
            for (int i = 0; i < rowIndices.Length; i++)
            {
                // Update selected row indices
                for (int j = 0; j < _selectedDataRowIndices.Count; j++)
                    if (_selectedDataRowIndices[j] >= rowIndices[i]) _selectedDataRowIndices[j] += 1;

                // Adjust row in selected cells collection
                var rowsToUpdate = new List<int>();
                foreach (var selectedCellsByRow in _selectedCellIndices)
                    if (selectedCellsByRow.Key >= rowIndices[i]) rowsToUpdate.Add(selectedCellsByRow.Key);

                rowsToUpdate.Sort();
                rowsToUpdate.Reverse();

                foreach (var rowToUpdate in rowsToUpdate)
                {
                    _selectedCellIndices.Add(rowToUpdate + 1, _selectedCellIndices[rowToUpdate]);
                    _selectedCellIndices.Remove(rowToUpdate);
                }

                // Update active cell
                if (_activeCellVirtualRowIndex >= rowIndices[i]) _activeCellVirtualRowIndex += 1;
                if (_activeCellVirtualRowIndex > DataView.NumberOfRows - 1)
                    _activeCellVirtualRowIndex = DataView.NumberOfRows - 1;
            }
            NumberOfRowsChanged();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// Handles the event when rows are deleted from the data view.
        /// Updates selection indices, cell selection, and active cell position accordingly.
        /// </summary>
        /// <param name="rowIndices">Array of indices of the deleted rows.</param>
        private void TableViewRowsDeleted(int[] rowIndices)
        {
            Array.Sort(rowIndices);
            for (int i = rowIndices.Length - 1; i >= 0; i--)
            {
                // Update selected row indices
                _selectedDataRowIndices.Remove(rowIndices[i]);
                for (int j = 0; j < _selectedDataRowIndices.Count; j++)
                    if (_selectedDataRowIndices[j] > rowIndices[i]) _selectedDataRowIndices[j] -= 1;

                // Delete row from selected cells collection
                _selectedCellIndices.Remove(rowIndices[i]);
                var rowsToUpdate = new List<int>();
                foreach (var selectedCellsByRow in _selectedCellIndices)
                    if (selectedCellsByRow.Key > rowIndices[i]) rowsToUpdate.Add(selectedCellsByRow.Key);

                rowsToUpdate.Sort();

                foreach (var rowToUpdate in rowsToUpdate)
                {
                    _selectedCellIndices.Add(rowToUpdate - 1, _selectedCellIndices[rowToUpdate]);
                    _selectedCellIndices.Remove(rowToUpdate);
                }

                // Update active cell
                if (_activeCellVirtualRowIndex >= rowIndices[i]) _activeCellVirtualRowIndex -= 1;
                if (_activeCellVirtualRowIndex < 0) _activeCellVirtualRowIndex = 0;
            }
            NumberOfRowsChanged();
            UpdateUndoRedoButtons();
        }

        #endregion

        #region Refresh Methods

        /// <summary>
        /// Performs a complete refresh of the table viewer, resetting all state and reloading data.
        /// Clears selections, recreates columns, and reloads visible rows.
        /// </summary>
        private void Refresh()
        {
            if (DataView == null) return;
            if (!DataView.ParentDatabase.DataBaseOpen)
            {
                try
                {
                    DataView.ParentDatabase.Open();
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Failed to open database: {ex.Message}", "Database Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
            }

            _selectedCellIndices = new SortedDictionary<int, SortedSet<int>>();
            _selectedColumnIndices = new List<int>();
            _selectedDataRowIndices = new List<int>();
            _activeCellDataColumnIndex = 0;
            _activeCellVirtualRowIndex = 0;
            _selectedRowsOnly = false;

            _rowId = new int[DataView.NumberOfRows];
            _rowOffset = new int[_rowId.Length];
            for (int i = 0; i < _rowId.Length; i++) _rowId[i] = i;
            _rowId.CopyTo(_rowOffset, 0);

            ColumnHeadersGrid.SizeChanged -= ColumnsGridSizeChanged;

            GridPanel.Children.Clear();
            RowHeadersGrid.Children.Clear();
            RowColorGrid.Children.Clear();
            ColumnHeadersGrid.Children.Clear();
            GridPanel.RowDefinitions.Clear();
            RowHeadersGrid.RowDefinitions.Clear();
            RowColorGrid.RowDefinitions.Clear();
            ColumnHeadersGrid.ColumnDefinitions.Clear();

            _visibleRowCount = GetMaxRows(false);
            if (_visibleRowCount < 0) _visibleRowCount = 0;
            if (_visibleRowCount > DataView.NumberOfRows) _visibleRowCount = DataView.NumberOfRows;

            VerticalScrollbar.ValueChanged -= VerticalScrollBar_ValueChanged;
            VerticalScrollbar.Maximum = DataView.NumberOfRows - _visibleRowCount;
            VerticalScrollbar.ValueChanged += VerticalScrollBar_ValueChanged;
            VerticalScrollbar.ViewportSize = _visibleRowCount;

            if (_columnsSortedOrder == null || _columnsSortedOrder.Length != DataView.ColumnNames.Count())
                Array.Resize(ref _columnsSortedOrder, DataView.ColumnNames.Count());

            GridPanel.ColumnDefinitions.Clear();
            GridLinesCanvas.Children.Clear();
            CreateColumns();

            for (int i = 0; i < _columnsSortedOrder.Length; i++)
            {
                if (_columnsSortedOrder[i] == SortOrder.Ascending)
                    ((ColumnHeader)ColumnHeadersGrid.Children[i * 2]).AddSorter(true);
                else if (_columnsSortedOrder[i] == SortOrder.Descending)
                    ((ColumnHeader)ColumnHeadersGrid.Children[i * 2]).AddSorter(false);
            }

            var lengthBinding = new Binding("ActualWidth") { ElementName = "GridPanel" };
            double rowDistanceFromTop = RowHeight * GridPanel.RowDefinitions.Count - (RowLineThickness / 2);
            var rowLine = new Line
            {
                SnapsToDevicePixels = true, X1 = 0, Y1 = rowDistanceFromTop, Y2 = rowDistanceFromTop,
                StrokeThickness = RowLineThickness, Stroke = (Brush)FindResource("DataGrid.Header.Border")
            };
            BindingOperations.SetBinding(rowLine, Line.X2Property, lengthBinding);
            GridLinesCanvas.Children.Add(rowLine);

            LoadRows();
            this.UpdateLayout();
            DeSelectAllCells();
            SetSelectedCells();
            SetActiveCell();
            RefreshColumnWidths();
            UpdateRowHeaders();
        }

        /// <summary>
        /// Refreshes the view by incrementally adjusting visible rows based on the control's size.
        /// Adds or removes rows as needed rather than rebuilding the entire grid.
        /// </summary>
        public void RefreshView()
        {
            if (!_isLoaded || DataView == null) return;

            int newNumberRows = GetMaxRows();
            int nDataRows = DataView.NumberOfRows;
            if (_selectedRowsOnly) nDataRows = _selectedDataRowIndices.Count;
            if (newNumberRows > nDataRows) newNumberRows = nDataRows;
            if (newNumberRows != _visibleRowCount && GridPanel.Children.Contains(_cellEditTextBox)) GridPanel.Focus();
            int offset = newNumberRows - _visibleRowCount;
            if (offset > (VerticalScrollbar.Maximum - Math.Floor(VerticalScrollbar.Value))) VerticalScrollbar.Value = VerticalScrollbar.Maximum;

            if (newNumberRows < _visibleRowCount)
            {
                // Remove rows incrementally
                do
                {
                    for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                        GridPanel.Children.RemoveAt(GridPanel.Children.Count - 1);
                    GridPanel.RowDefinitions.RemoveAt(GridPanel.RowDefinitions.Count - 1);
                    RowColorGrid.Children.RemoveAt(RowColorGrid.Children.Count - 1);
                    RowColorGrid.RowDefinitions.RemoveAt(RowColorGrid.RowDefinitions.Count - 1);
                    RowHeadersGrid.Children.RemoveAt(RowHeadersGrid.Children.Count - 1);
                    RowHeadersGrid.RowDefinitions.RemoveAt(RowHeadersGrid.RowDefinitions.Count - 1);
                    GridLinesCanvas.Children.RemoveAt(GridLinesCanvas.Children.Count - 1);
                    _visibleRowCount -= 1;
                } while (_visibleRowCount != newNumberRows);

                if ((int)Math.Floor(VerticalScrollbar.Value) == (int)VerticalScrollbar.Maximum)
                {
                    VerticalScrollbar.Maximum = nDataRows - _visibleRowCount;
                    VerticalScrollbar.Value = VerticalScrollbar.Maximum;
                }
                else
                {
                    VerticalScrollbar.Maximum = nDataRows - _visibleRowCount;
                }
            }
            else if (newNumberRows > _visibleRowCount)
            {
                // Add rows incrementally
                do
                {
                    AddRow();
                    if (_selectedRowsOnly)
                    {
                        for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                            SelectCell(i, _visibleRowCount);
                    }
                    _visibleRowCount += 1;
                } while (_visibleRowCount != newNumberRows);
                VerticalScrollbar.Maximum = nDataRows - newNumberRows;
                UpdateRowHeaders();
            }

            RefreshColumnWidths(false);

            if (!_selectedRowsOnly) SetSelectedCells();
            VerticalScrollbar.ViewportSize = _visibleRowCount;
        }

        /// <summary>
        /// Handles changes to the number of rows in the data view.
        /// Recalculates visible row count, scrollbar settings, row mappings, and reloads visible rows.
        /// </summary>
        private void NumberOfRowsChanged()
        {
            _visibleRowCount = GetMaxRows();
            VerticalScrollbar.ValueChanged -= VerticalScrollBar_ValueChanged;

            if (_selectedRowsOnly)
            {
                if (_visibleRowCount > _selectedDataRowIndices.Count) _visibleRowCount = _selectedDataRowIndices.Count;
                VerticalScrollbar.Maximum = _selectedDataRowIndices.Count - _visibleRowCount;
            }
            else
            {
                if (_visibleRowCount > DataView.NumberOfRows) _visibleRowCount = DataView.NumberOfRows;
                VerticalScrollbar.Maximum = DataView.NumberOfRows - _visibleRowCount;
            }

            if ((int)Math.Floor(VerticalScrollbar.Value) > (int)VerticalScrollbar.Maximum)
                VerticalScrollbar.Value = (int)VerticalScrollbar.Maximum;

            VerticalScrollbar.ValueChanged += VerticalScrollBar_ValueChanged;

            _rowId = new int[DataView.NumberOfRows];
            _rowOffset = new int[_rowId.Length];

            if (_columnSortOrder == SortOrder.None)
            {
                for (int i = 0; i < _rowId.Length; i++) _rowId[i] = i;
                _rowId.CopyTo(_rowOffset, 0);
            }
            else
            {
                for (int i = 0; i < _columnsSortedOrder!.Length; i++)
                {
                    if (_columnsSortedOrder[i] == SortOrder.Ascending) SortColumn(i, true);
                    else if (_columnsSortedOrder[i] == SortOrder.Descending) SortColumn(i, false);
                }
            }

            VerticalScrollbar.ViewportSize = _visibleRowCount;
            LoadRows();

            if (ShowRowHeaders)
            {
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                RowHeadersColumnDefinition.Width = new GridLength(
                    (int)(new FormattedText(DataView.NumberOfRows.ToString(), CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                        12, Brushes.Black, pixelsPerDip).Width) + 10);
            }

            UpdateRowHeaders();
            SetSelectedCells();
            if (DataView.NumberOfRows > 0) SetActiveCell(_activeCellVirtualRowIndex, _activeCellDataColumnIndex);
        }

        /// <summary>
        /// Handles changes to the number of columns in the data view.
        /// Clears and recreates all grid elements, restores sort indicators, and reloads visible rows.
        /// </summary>
        private void NumberOfColumnsChanged()
        {
            ColumnHeadersGrid.SizeChanged -= ColumnsGridSizeChanged;
            GridPanel.Children.Clear();
            RowHeadersGrid.Children.Clear();
            RowColorGrid.Children.Clear();
            ColumnHeadersGrid.Children.Clear();
            GridPanel.RowDefinitions.Clear();
            RowHeadersGrid.RowDefinitions.Clear();
            RowColorGrid.RowDefinitions.Clear();
            ColumnHeadersGrid.ColumnDefinitions.Clear();
            GridPanel.ColumnDefinitions.Clear();
            GridLinesCanvas.Children.Clear();
            CreateColumns();

            for (int i = 0; i < _columnsSortedOrder!.Length; i++)
            {
                if (_columnsSortedOrder[i] == SortOrder.Ascending)
                    ((ColumnHeader)ColumnHeadersGrid.Children[i * 2]).AddSorter(true);
                else if (_columnsSortedOrder[i] == SortOrder.Descending)
                    ((ColumnHeader)ColumnHeadersGrid.Children[i * 2]).AddSorter(false);
            }

            var lengthBinding = new Binding("ActualWidth") { ElementName = "GridPanel" };
            double rowDistanceFromTop = RowHeight * GridPanel.RowDefinitions.Count - (RowLineThickness / 2);
            var rowLine = new Line
            {
                SnapsToDevicePixels = true, X1 = 0, Y1 = rowDistanceFromTop, Y2 = rowDistanceFromTop,
                StrokeThickness = RowLineThickness, Stroke = (Brush)FindResource("DataGrid.Header.Border")
            };
            BindingOperations.SetBinding(rowLine, Line.X2Property, lengthBinding);
            GridLinesCanvas.Children.Add(rowLine);

            LoadRows();
            SetSelectedCells();
            RefreshColumnWidths();
            UpdateRowHeaders();
        }

        /// <summary>
        /// Calculates the maximum number of rows that can be displayed in the current viewport.
        /// </summary>
        /// <param name="updateLayout">If true, forces a layout update before calculation. Default is true.</param>
        /// <returns>The maximum number of visible rows that fit in the viewport.</returns>
        private int GetMaxRows(bool updateLayout = true)
        {
            if (updateLayout) this.UpdateLayout();
            int maxNumberRows;
            var parentWindow = Window.GetWindow(this);

            if ((parentWindow != null && parentWindow.WindowState == WindowState.Maximized) ||
                HorizontalScrollViewer.ActualHeight < HorizontalScrollViewer.ViewportHeight)
                maxNumberRows = (int)Math.Floor((HorizontalScrollViewer.ActualHeight - ColumnHeadersGrid.ActualHeight) / RowHeight) + 1;
            else
                maxNumberRows = (int)Math.Floor((HorizontalScrollViewer.ViewportHeight - ColumnHeadersGrid.ActualHeight) / RowHeight) + 1;

            if (VerticalScrollbar.Value == VerticalScrollbar.Maximum) maxNumberRows -= 1;
            return maxNumberRows < 0 ? 0 : maxNumberRows;
        }

        #endregion

        #region Grid Setup

        /// <summary>
        /// Creates all column definitions, column headers, and grid splitters for the table.
        /// Sets initial column widths based on header text length.
        /// </summary>
        private void CreateColumns()
        {
            ColumnHeadersGrid.SizeChanged += ColumnsGridSizeChanged;
            _columnWidths = new GridLength[DataView.ColumnNames.Count()];
            double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;

            for (int i = 0; i < DataView.ColumnNames.Count(); i++)
            {
                ColumnDefinition colDef;
                if (AutoFitColumns)
                {
                    colDef = new ColumnDefinition();
                    _columnWidths[i] = new GridLength(1, GridUnitType.Star);
                }
                else
                {
                    int columnWidth = (int)(new FormattedText(DataView.ColumnNames[i], CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                        12, Brushes.Black, pixelsPerDip).Width) + 12;
                    colDef = new ColumnDefinition { Width = new GridLength(columnWidth) };
                    _columnWidths[i] = new GridLength(columnWidth);
                }
                ColumnHeadersGrid.ColumnDefinitions.Add(colDef);

                var header = new ColumnHeader(DataView.ColumnNames[i], DataView.ColumnTypes[i])
                {
                    HeaderTextblockStyle = ColumnHeaderTextblockStyle,
                    HeaderBorderStyle = ColumnHeaderBorderStyle
                };
                header.MouseRightButtonUp += CreateColumnContextMenu;
                Grid.SetColumn(header, i);
                ColumnHeadersGrid.Children.Add(header);

                if (i < DataView.ColumnNames.Count() - 1)
                {
                    var resizer = new GridSplitter
                    {
                        Background = Brushes.Transparent, Width = 10,
                        Margin = new Thickness(0, 0, -5, 0),
                        VerticalAlignment = VerticalAlignment.Stretch,
                        HorizontalAlignment = HorizontalAlignment.Right
                    };
                    resizer.MouseDoubleClick += ResizeColumnSplitterDoubleClick;
                    resizer.DragDelta += ResizeColumnSplitterDragDelta;
                    resizer.DragCompleted += ResizeColumnSplitterDragComplete;
                    Grid.SetZIndex(resizer, 1);
                    Grid.SetColumn(resizer, i);
                    ColumnHeadersGrid.Children.Add(resizer);
                }

                GridPanel.ColumnDefinitions.Add(new ColumnDefinition());
            }

            if (DataView.ColumnNames.Count() == 0) return;

            var lengthBinding = new Binding(nameof(Grid.ActualHeight)) { Source = GridPanel };
            for (int i = 0; i <= DataView.ColumnNames.Count(); i++)
            {
                var gridLine = new Line
                {
                    SnapsToDevicePixels = true, X1 = 0, Y1 = 0, X2 = 0,
                    StrokeThickness = ColumnLineThickness, Stroke = ColumnLineColor
                };
                BindingOperations.SetBinding(gridLine, Line.Y2Property, lengthBinding);
                GridLinesCanvas.Children.Add(gridLine);
            }

            RefreshColumnWidths(false);
        }

        /// <summary>
        /// Clears and reloads all visible rows in the grid.
        /// Removes excess grid lines and recreates row structures for the current visible row count.
        /// </summary>
        private void LoadRows()
        {
            if (GridLinesCanvas.Children.Count > DataView.ColumnNames.Count() + 2)
                GridLinesCanvas.Children.RemoveRange(DataView.ColumnNames.Count() + 2, GridLinesCanvas.Children.Count - 2 - DataView.ColumnNames.Count());

            GridPanel.Children.Clear();
            GridPanel.RowDefinitions.Clear();
            RowHeadersGrid.Children.Clear();
            RowHeadersGrid.RowDefinitions.Clear();
            RowColorGrid.Children.Clear();
            RowColorGrid.RowDefinitions.Clear();

            for (int i = 0; i < _visibleRowCount; i++) AddRow();
        }

        /// <summary>
        /// Adds a new row to the grid with all necessary elements including cells, row header,
        /// background color, and separator line.
        /// </summary>
        private void AddRow()
        {
            if (GridPanel == null || DataView == null || VerticalScrollbar == null) return;

            // Create new row definition
            GridPanel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });

            // Create new row Cells
            var fg = DeSelectedForegroundColor;
            for (int j = 0; j < DataView.ColumnTypes.Count(); j++)
            {
                var newCell = new Cell { CellStyle = CellTextblockStyle, Foreground = fg };
                Grid.SetRow(newCell, GridPanel.RowDefinitions.Count - 1);
                Grid.SetColumn(newCell, j);
                GridPanel.Children.Add(newCell);
            }

            // Fill new row with data
            int scrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
            if ((scrollBarValue + (GridPanel.RowDefinitions.Count - 1)) < _rowId!.Length)
            {
                if (_selectedRowsOnly == false)
                {
                    FillRow(GridPanel.RowDefinitions.Count - 1, DataView.GetRow(_rowId[scrollBarValue + GridPanel.RowDefinitions.Count - 1]));
                }
                else
                {
                    if (_columnSortOrder == SortOrder.None)
                    {
                        if (_selectedDataRowIndices.Count >= scrollBarValue + GridPanel.RowDefinitions.Count)
                            FillRow(GridPanel.RowDefinitions.Count - 1, DataView.GetRow(_rowId[_selectedDataRowIndices[scrollBarValue + GridPanel.RowDefinitions.Count - 1]]));
                    }
                    else
                    {
                        if (_sortedSelectedRowOffsets!.Length >= scrollBarValue + GridPanel.RowDefinitions.Count)
                            FillRow(GridPanel.RowDefinitions.Count - 1, DataView.GetRow(_rowId[_sortedSelectedRowOffsets[scrollBarValue + GridPanel.RowDefinitions.Count - 1]]));
                    }
                }
            }

            // Create row line
            var lengthBinding = new Binding(nameof(Grid.ActualWidth)) { Source = GridPanel };
            double rowDistanceFromTop = RowHeight * (GridPanel.RowDefinitions.Count) - (RowLineThickness / 2);
            var rowLine = new Line
            {
                SnapsToDevicePixels = true,
                X1 = 0,
                Y1 = rowDistanceFromTop,
                Y2 = rowDistanceFromTop,
                StrokeThickness = RowLineThickness,
                Stroke = RowLineColor
            };
            BindingOperations.SetBinding(rowLine, Line.X2Property, lengthBinding);
            GridLinesCanvas.Children.Add(rowLine);

            // Create Row selector
            RowHeadersGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight), MaxHeight = RowHeight });
            var newRowSelector = new RowHeader
            {
                HeaderTextblockStyle = RowHeaderTextblockStyle,
                HeaderBorderStyle = RowHeaderBorderStyle,
                Height = RowHeight
            };
            Grid.SetRow(newRowSelector, RowHeadersGrid.RowDefinitions.Count - 1);
            RowHeadersGrid.Children.Add(newRowSelector);

            // Create row color
            RowColorGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(RowHeight) });
            Brush fillColor = RowColor;
            if ((RowColorGrid.RowDefinitions.Count - 1) % 2 != 0) fillColor = AlternateRowColor;
            var rect = new Rectangle
            {
                Stroke = new SolidColorBrush(Colors.Transparent),
                StrokeThickness = 0,
                Fill = fillColor
            };
            Grid.SetRow(rect, RowColorGrid.RowDefinitions.Count - 1);
            RowColorGrid.Children.Add(rect);
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Fills a visible row with data from a row array.
        /// </summary>
        /// <param name="rowIndex">The visible row index to fill.</param>
        /// <param name="row">The row data array, or null to clear the row.</param>
        private void FillRow(int rowIndex, object[]? row)
        {
            if (row == null)
            {
                for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                {
                    SetCellText(rowIndex, i, "");
                }
            }
            else
            {
                for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                {
                    if (row[i] == null)
                    {
                        SetCellText(rowIndex, i, "");
                    }
                    else
                    {
                        SetCellText(rowIndex, i, row[i].ToString() ?? "");
                    }
                }
            }
        }

        /// <summary>
        /// Updates the text content of all visible cells from the DataView.
        /// </summary>
        public void UpdateVisibleRows()
        {
            var dataRowIndices = GetDataRowIndexes(0, _visibleRowCount - 1);
            for (int i = 0; i < dataRowIndices.Count; i++)
            {
                FillRow(i, DataView.GetRow(dataRowIndices[i]));
            }
        }

        /// <summary>
        /// Updates the row header labels to display the correct row numbers.
        /// Displays 0-based row indices for backwards compatibility with VB version.
        /// Also updates row background color alternation.
        /// </summary>
        private void UpdateRowHeaders()
        {
            if (DataView == null || _rowId == null) return;
            if (_visibleRowCount == 0) return;

            int firstRowVirtualIndex = (int)Math.Floor(VerticalScrollbar.Value);
            if (VerticalScrollbar.Value == VerticalScrollbar.Maximum && VerticalScrollbar.Value != 0)
            {
                firstRowVirtualIndex = (int)VerticalScrollbar.Maximum - _visibleRowCount;
                if (firstRowVirtualIndex < 0) firstRowVirtualIndex = 0;
            }

            // Clamp against the current _rowId length. Rows may have been deleted since the
            // scroll-bar value was last clamped (e.g., during a TableViewRowsDeleted event
            // before LoadRows runs), leaving firstRowVirtualIndex referencing a now-invalid
            // slot.
            if (firstRowVirtualIndex >= _rowId.Length) firstRowVirtualIndex = _rowId.Length - 1;
            if (firstRowVirtualIndex < 0) return;

            bool alternate = _rowOffset![_rowId[firstRowVirtualIndex]] % 2 != 0;
            for (int i = 0; i < _visibleRowCount; i++)
            {
                if (i < RowColorGrid.Children.Count)
                    ((Rectangle)RowColorGrid.Children[i]).Fill = alternate ? AlternateRowColor : RowColor;
                if (i < RowHeadersGrid.Children.Count)
                    ((RowHeader)RowHeadersGrid.Children[i]).Text = GetDataRowIndex(i).ToString();
                alternate = !alternate;
            }
        }

        /// <summary>
        /// Gets a list of data row indices for a range of visible table rows.
        /// Accounts for scrolling, row selection mode, and sorting.
        /// </summary>
        /// <param name="viewRowStartIndex">The starting visible row index.</param>
        /// <param name="viewRowEndIndex">The ending visible row index.</param>
        /// <returns>A list of data row indices corresponding to the visible range.</returns>
        private List<int> GetDataRowIndexes(int viewRowStartIndex, int viewRowEndIndex)
        {
            if (viewRowEndIndex == -1) return new List<int>();
            var dataRowIndexes = new List<int>(viewRowEndIndex - viewRowStartIndex);
            int firstRowVirtualIndex = (int)Math.Floor(VerticalScrollbar.Value);

            if (_selectedRowsOnly)
            {
                if (_columnSortOrder == SortOrder.None)
                {
                    for (int i = viewRowStartIndex; i <= viewRowEndIndex; i++)
                    {
                        dataRowIndexes.Add(_rowId![_selectedDataRowIndices[firstRowVirtualIndex + i]]);
                    }
                }
                else
                {
                    for (int i = viewRowStartIndex; i <= viewRowEndIndex; i++)
                    {
                        dataRowIndexes.Add(_rowId![_sortedSelectedRowOffsets![firstRowVirtualIndex + i]]);
                    }
                }
            }
            else
            {
                for (int i = viewRowStartIndex; i <= viewRowEndIndex; i++)
                {
                    dataRowIndexes.Add(_rowId![firstRowVirtualIndex + i]);
                }
            }

            return dataRowIndexes;
        }

        /// <summary>
        /// Converts a table row index (visible position) to the actual data row index.
        /// Accounts for scrolling, row selection mode, and sorting.
        /// </summary>
        /// <param name="tableRowIndex">The visible row index in the grid.</param>
        /// <returns>The corresponding data row index, or -1 if out of bounds.</returns>
        private int GetDataRowIndex(int tableRowIndex)
        {
            int firstRowIndex = (int)Math.Floor(VerticalScrollbar.Value);
            if (_selectedRowsOnly)
            {
                if (_columnSortOrder == SortOrder.None)
                {
                    int selectedIndex = Math.Min(firstRowIndex + tableRowIndex, _selectedDataRowIndices.Count - 1);
                    return _rowId![_selectedDataRowIndices[selectedIndex]];
                }
                else
                {
                    int sortedIndex = Math.Min(firstRowIndex + tableRowIndex, _sortedSelectedRowOffsets!.Length - 1);
                    return _rowId![_sortedSelectedRowOffsets[sortedIndex]];
                }
            }
            int index = firstRowIndex + tableRowIndex;
            if (index < 0 || index >= _rowId!.Length) return Math.Max(0, _rowId!.Length - 1);
            return _rowId[index];
        }

        /// <summary>
        /// Gets the text content of a cell at the specified table position from the UI.
        /// </summary>
        /// <param name="tableRowIndex">The visible row index in the grid.</param>
        /// <param name="columnIndex">The column index.</param>
        /// <returns>The cell text.</returns>
        private string GetCellText(int tableRowIndex, int columnIndex)
        {
            return ((Cell)GridPanel.Children[tableRowIndex * DataView.ColumnNames.Count() + columnIndex]).Text;
        }

        /// <summary>
        /// Determines the table row index from a grid position (mouse coordinates).
        /// </summary>
        /// <param name="gridPosition">The position within the grid panel.</param>
        /// <returns>The row index at the specified position.</returns>
        private int GetTableRowIndex(Point gridPosition)
        {
            double runningSum = 0;
            for (int i = 0; i < GridPanel.RowDefinitions.Count; i++)
            {
                runningSum += GridPanel.RowDefinitions[i].ActualHeight;
                if (runningSum >= gridPosition.Y) return i;
            }
            return GridPanel.RowDefinitions.Count - 1;
        }

        /// <summary>
        /// Resolves a grid coordinate to the visible table column index.
        /// </summary>
        /// <param name="gridPosition">The position relative to the grid panel.</param>
        /// <returns>The column index at the specified position.</returns>
        private int GetTableColumnIndex(Point gridPosition)
        {
            double runningSum = 0;
            for (int i = 0; i <= GridPanel.ColumnDefinitions.Count - 1; i++)
            {
                runningSum += GridPanel.ColumnDefinitions[i].ActualWidth;
                if (runningSum >= gridPosition.X) return i;
            }
            return GridPanel.ColumnDefinitions.Count - 1;
        }

        #endregion

        #region Selection

        /// <summary>
        /// Applies selection highlighting to all cells that are currently selected.
        /// Handles row selection, column selection, individual cell selection, and select-all states.
        /// </summary>
        private void SetSelectedCells()
        {
            if (DataView == null) return;

            // All selected
            if (AllCellsSelected || _selectedRowsOnly)
            {
                for (int i = 0; i < _visibleRowCount; i++)
                    for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                        SelectCell(j, i);
                SetActiveCell();
                return;
            }

            // Columns
            if (_selectedColumnIndices.Count > 0)
            {
                foreach (var columnIndex in _selectedColumnIndices)
                    for (int i = 0; i < _visibleRowCount; i++)
                        SelectCell(columnIndex, i);
            }

            // Rows
            if (_selectedDataRowIndices.Count > 0)
            {
                int firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
                int lastRowDataIndex = firstRowDataIndex + _visibleRowCount - 1;

                if (_columnSortOrder == SortOrder.Ascending || _columnSortOrder == SortOrder.Descending)
                {
                    // Now set the proper row selections
                    int counter = 0;
                    for (int i = firstRowDataIndex; i <= lastRowDataIndex; i++)
                    {
                        if (_selectedDataRowIndices.BinarySearch(_rowId![i]) >= 0)
                        {
                            for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                                SelectCell(j, counter);
                        }
                        counter++;
                    }
                }
                else
                {
                    int searchIndex = _selectedDataRowIndices.BinarySearch(firstRowDataIndex);
                    if (searchIndex < 0) searchIndex = ~searchIndex; // bitwise complement
                    for (int i = searchIndex; i < _selectedDataRowIndices.Count; i++)
                    {
                        if (_selectedDataRowIndices[i] > lastRowDataIndex) break;
                        for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                            SelectCell(j, _selectedDataRowIndices[i] - firstRowDataIndex);
                    }
                }
            }

            // Cells
            if (_selectedCellIndices.Count > 0)
            {
                int firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
                for (int i = firstRowDataIndex; i <= (firstRowDataIndex + _visibleRowCount - 1); i++)
                {
                    if (_selectedCellIndices.ContainsKey(_rowId![i]))
                    {
                        int tableRowIndex = _rowOffset![_rowId[i]] - firstRowDataIndex;
                        foreach (var columnIndex in _selectedCellIndices[_rowId[i]])
                            SelectCell(columnIndex, tableRowIndex);
                    }
                }
            }

            SetActiveCell();
        }

        /// <summary>
        /// Removes selection highlighting from all visible cells.
        /// </summary>
        private void DeSelectAllCells()
        {
            var bg = DeSelectedColor;
            var fg = DeSelectedForegroundColor;
            int colCount = DataView.ColumnNames.Count();
            int rowCount = GridPanel.RowDefinitions.Count;
            var children = GridPanel.Children;
            for (int j = 0; j < rowCount; j++)
            {
                int rowOffset = j * colCount;
                for (int i = 0; i < colCount; i++)
                {
                    var cell = (Cell)children[rowOffset + i];
                    cell.Background = bg;
                    cell.Foreground = fg;
                }
            }
        }

        /// <summary>
        /// Applies selected styling to a visible cell.
        /// </summary>
        /// <param name="columnIndex">The visible column index.</param>
        /// <param name="rowIndex">The visible row index.</param>
        private void SelectCell(int columnIndex, int rowIndex)
        {
            var cell = (Cell)GridPanel.Children[rowIndex * DataView.ColumnNames.Count() + columnIndex];
            bool selectionActive = IsTableSelectionActive();
            cell.Background = selectionActive ? SelectedColor : InactiveSelectedColor;
            cell.Foreground = selectionActive ? SelectedForegroundColor : InactiveSelectedForegroundColor;
        }

        /// <summary>
        /// Applies deselected styling to a visible cell.
        /// </summary>
        /// <param name="columnIndex">The visible column index.</param>
        /// <param name="rowIndex">The visible row index.</param>
        private void DeSelectCell(int columnIndex, int rowIndex)
        {
            var cell = (Cell)GridPanel.Children[rowIndex * DataView.ColumnNames.Count() + columnIndex];
            cell.Background = DeSelectedColor;
            cell.Foreground = DeSelectedForegroundColor;
        }

        /// <summary>
        /// Applies active-cell styling when the active cell is visible.
        /// </summary>
        private void SetActiveCell()
        {
            if (!IsTableSelectionActive()) return;
            if (GridPanel.Children.Count == 0) return;
            int firstRowTableIndex = (int)Math.Floor(VerticalScrollbar.Value);
            int lastRowTableIndex = firstRowTableIndex + _visibleRowCount - 1;
            if (_activeCellVirtualRowIndex >= firstRowTableIndex && _activeCellVirtualRowIndex <= lastRowTableIndex)
            {
                int rowIndex = _activeCellVirtualRowIndex - firstRowTableIndex;
                var cell = (Cell)GridPanel.Children[rowIndex * DataView.ColumnNames.Count() + _activeCellDataColumnIndex];
                cell.Background = ActiveCellBackground;
                cell.Foreground = ActiveCellForeground;
            }
        }

        /// <summary>
        /// Gets a value indicating whether the table selection should use active selection colors.
        /// </summary>
        /// <returns><c>true</c> when the table body or in-place editor contains keyboard focus; otherwise, <c>false</c>.</returns>
        private bool IsTableSelectionActive()
        {
            return IsKeyboardFocusWithin
                || GridPanel.IsKeyboardFocusWithin
                || _cellEditTextBox.IsKeyboardFocusWithin
                || _mouseSelectionMode != SelectionMode.None;
        }

        /// <summary>
        /// Repaints visible selection cells without changing the selected row, column, or cell collections.
        /// </summary>
        private void RefreshSelectionVisuals()
        {
            if (DataView == null || GridPanel.Children.Count == 0) return;

            DeSelectAllCells();
            SetSelectedCells();
        }

        /// <summary>
        /// Sets the active cell to the specified data row and column, optionally scrolling to bring the row into view.
        /// </summary>
        /// <param name="newDataRowIndex">The zero-based data row index of the cell to activate.</param>
        /// <param name="newDataColumnIndex">The zero-based data column index of the cell to activate.</param>
        /// <param name="scrollToRow">If true, scrolls the view to make the active cell's row visible.</param>
        public void SetActiveCell(int newDataRowIndex, int newDataColumnIndex, bool scrollToRow = false)
        {
            _activeCellDataColumnIndex = newDataColumnIndex;
            _activeCellVirtualRowIndex = _rowOffset![newDataRowIndex];
            if (scrollToRow) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
            DeSelectAllCells();
            SetSelectedCells();
            ActiveCellLocationChanged?.Invoke();
        }

        /// <summary>
        /// Resets focus from the cell edit TextBox to the GridPanel if currently editing.
        /// </summary>
        public void SetEditingCell()
        {
            if (!_cellEditTextBox.IsFocused) return;
            GridPanel.Focus();
        }

        /// <summary>
        /// Programmatically updates the selected row indices and refreshes the selection display.
        /// </summary>
        /// <param name="newSelectedRowIndices">Array of row indices to select, or null to clear selection.</param>
        public void UpdateSelectedRowIndices(int[]? newSelectedRowIndices)
        {
            if (newSelectedRowIndices == null)
            {
                _selectedDataRowIndices.Clear();
            }
            else
            {
                _selectedDataRowIndices = newSelectedRowIndices.ToList();
                _selectedDataRowIndices.Sort();
            }

            DeSelectAllCells();
            if (!_selectedRowsOnly)
            {
                SetSelectedCells();
            }
            else
            {
                ShowAll_Checked(null, null);
                SetSelectedCells();
                ShowSelected_Checked(null, null);
            }

            UpdateSelectionButtonStates();
        }

        #endregion

        #region Resize Logic

        /// <summary>
        /// Handles completion of a column splitter drag operation. Currently unused but required for event binding.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ResizeColumnSplitterDragComplete(object sender, DragCompletedEventArgs e) { }

        /// <summary>
        /// Handles column splitter drag delta to resize columns during drag.
        /// </summary>
        /// <param name="sender">The source of the event (the GridSplitter).</param>
        /// <param name="e">The event data containing the drag delta.</param>
        private void ResizeColumnSplitterDragDelta(object sender, DragDeltaEventArgs e)
        {
            var splitter = (GridSplitter)sender;
            int columnIndex = Grid.GetColumn(splitter);
            double newColumnWidth = ColumnHeadersGrid.ColumnDefinitions[columnIndex].ActualWidth + e.HorizontalChange;
            ResizeColumnWidth(columnIndex, (int)newColumnWidth);
        }

        /// <summary>
        /// Handles double-click on column splitter to auto-fit the column width to content.
        /// </summary>
        /// <param name="sender">The source of the event (the GridSplitter).</param>
        /// <param name="e">The event data.</param>
        private void ResizeColumnSplitterDoubleClick(object sender, MouseButtonEventArgs e)
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var splitter = (GridSplitter)sender;
                int columnIndex = Grid.GetColumn(splitter);
                ResizeColumnWidth(columnIndex);
                Mouse.OverrideCursor = null;
            }
            catch { Mouse.OverrideCursor = null; }
        }

        /// <summary>
        /// Resizes a column to the specified width, or auto-fits to content if no width is specified.
        /// </summary>
        /// <param name="columnIndex">The zero-based index of the column to resize.</param>
        /// <param name="columnWidth">The desired column width in pixels. If -1 or less than 6, the column auto-fits to its content.</param>
        public void ResizeColumnWidth(int columnIndex, int columnWidth = -1)
        {
            if (columnWidth > 5)
            {
                _columnWidths![columnIndex] = new GridLength(columnWidth);
            }
            else
            {
                var data = DataView.GetColumn(DataView.ColumnNames[columnIndex]);
                double pixelsPerDip = VisualTreeHelper.GetDpi(this).PixelsPerDip;
                int max = (int)(new FormattedText(DataView.ColumnNames[columnIndex], CultureInfo.GetCultureInfo("en-us"),
                    FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                    12, Brushes.Black, pixelsPerDip).Width) + 12;

                for (int i = 0; i < data.Length; i++)
                {
                    string stringData = data[i]?.ToString() ?? "";
                    int formattedWidth = (int)(new FormattedText(stringData, CultureInfo.GetCultureInfo("en-us"),
                        FlowDirection.LeftToRight, new Typeface(new FontFamily("Segoe UI"), FontStyles.Normal, FontWeights.Bold, FontStretches.Normal),
                        12, Brushes.Black, pixelsPerDip).Width) + 6;
                    if (formattedWidth > max) max = formattedWidth;
                }
                if (max < 6) max = 6;
                _columnWidths![columnIndex] = new GridLength(max);
            }
            RefreshColumnWidths();
        }

        /// <summary>
        /// Handles size changes in the columns grid by refreshing all column widths.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ColumnsGridSizeChanged(object sender, SizeChangedEventArgs e) => RefreshColumnWidths();

        /// <summary>
        /// Recalculates and applies column widths to all columns in the grid.
        /// </summary>
        /// <param name="setActive">If <c>true</c>, resets the active cell styling after refresh. Default is <c>true</c>.</param>
        public void RefreshColumnWidths(bool setActive = true)
        {
            if (_columnWidths == null || DataView == null) return;
            double runningWidth = 0;
            int sumStar = 0;
            var columnWidths = new double[DataView.ColumnNames.Count()];

            for (int i = 0; i < DataView.ColumnNames.Count(); i++)
            {
                if (_columnWidths[i].IsAuto || _columnWidths[i].IsAbsolute)
                {
                    columnWidths[i] = _columnWidths[i].Value;
                    runningWidth += _columnWidths[i].Value;
                }
                else if (_columnWidths[i].IsStar)
                    sumStar += (int)_columnWidths[i].Value;
            }

            double remainingSpace = HorizontalScrollViewer.ActualWidth - ColumnLineThickness - runningWidth;
            for (int i = 0; i < DataView.ColumnNames.Count(); i++)
            {
                if (_columnWidths[i].IsStar)
                {
                    if (sumStar == 0) sumStar = (int)_columnWidths[i].Value;
                    double starredWidth = remainingSpace * _columnWidths[i].Value / sumStar;
                    if (starredWidth < 10) starredWidth = 10;
                    columnWidths[i] = Math.Round(starredWidth);
                }
            }

            runningWidth = 0;
            for (int i = 0; i < DataView.ColumnNames.Count(); i++)
            {
                double w = Math.Round(columnWidths[i]);
                ColumnHeadersGrid.ColumnDefinitions[i].Width = new GridLength(w);
                GridPanel.ColumnDefinitions[i].Width = new GridLength(w);
                runningWidth += w;
                ((Line)GridLinesCanvas.Children[i + 1]).X1 = runningWidth;
                ((Line)GridLinesCanvas.Children[i + 1]).X2 = runningWidth;
            }

            if (setActive) SetActiveCell();
        }

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the Loaded event of the TableViewer control.
        /// Triggers an initial view refresh when the control is first loaded.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void TableViewer_Loaded(object sender, RoutedEventArgs e)
        {
            bool wasFalse = _isLoaded;
            _isLoaded = true;
            if (wasFalse == false) RefreshView();

        }

        /// <summary>
        /// Releases data-view subscriptions when the table viewer unloads.
        /// </summary>
        /// <param name="sender">The table viewer that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void TableViewer_Unloaded(object sender, RoutedEventArgs e)
        {
            _isLoaded = false;
            if (DataView != null)
            {
                DataView.RowsAdded -= TableViewRowsAdded;
                DataView.RowsDeleted -= TableViewRowsDeleted;
                DataView.ColumnsAdded -= TableViewColumnsAdded;
                DataView.ColumnsDeleted -= TableViewColumnsDeleted;
            }
        }

        /// <summary>
        /// Handles vertical scrollbar value changes to update visible content.
        /// Uses incremental scrolling optimization: copies cell text and only loads changed rows.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing old and new values.</param>
        private void VerticalScrollBar_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            // Convert old and new values to integers to match row indices
            int oldValue = (int)Math.Floor(e.OldValue);
            int newValue = (int)Math.Floor(e.NewValue);
            if (oldValue == newValue) return;

            int counter = 0;

            if (newValue >= VerticalScrollbar.Maximum && VerticalScrollbar.Maximum != 0) // The last row has been reached
            {
                // Only rebuild if the visible row count doesn't already include the extra row
                int expectedRows = (int)Math.Floor(VerticalScrollbar.Maximum) + 1;
                if (_visibleRowCount < expectedRows || _visibleRowCount > expectedRows)
                {
                    NumberOfRowsChanged();
                }
                if (_visibleRowCount > 0) FillRow(_visibleRowCount - 1, DataView.GetRow(GetDataRowIndex(_visibleRowCount - 1)));
            }
            else if (oldValue >= VerticalScrollbar.Maximum && VerticalScrollbar.Maximum != 0) // The last row has been vacated
            {
                // Add a row and refresh all cells
                _visibleRowCount += 1;
                VerticalScrollbar.ViewportSize = _visibleRowCount;
                AddRow();
                UpdateVisibleRows();
            }
            else if (oldValue > newValue) // Going Up (scrolling towards beginning)
            {
                int offset = oldValue - newValue;
                // Shift existing cell text down
                for (int i = _visibleRowCount - 1; i >= offset; i--)
                {
                    for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                    {
                        SetCellText(i, j, GetCellText(i - offset, j));
                    }
                }
                // Fill new rows at top
                if (offset >= _visibleRowCount) offset = _visibleRowCount;
                foreach (int dataRowIndex in GetDataRowIndexes(0, offset - 1))
                {
                    FillRow(counter, DataView.GetRow(dataRowIndex));
                    counter++;
                }
            }
            else if (oldValue < newValue) // Going Down (scrolling towards end)
            {
                int offset = newValue - oldValue;
                // Shift existing cell text up
                for (int i = 0; i <= _visibleRowCount - offset - 1; i++)
                {
                    for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                    {
                        SetCellText(i, j, GetCellText(i + offset, j));
                    }
                }
                // Fill new rows at bottom
                if (_visibleRowCount - offset - 1 < 0)
                {
                    offset = 0;
                }
                else
                {
                    offset = _visibleRowCount - offset;
                }
                counter = offset;
                foreach (int dataRowIndex in GetDataRowIndexes(offset, _visibleRowCount - 1))
                {
                    FillRow(counter, DataView.GetRow(dataRowIndex));
                    counter++;
                }
            }

            // Turn off cell editing
            if (GridPanel.Children.Contains(_cellEditTextBox)) GridPanel.Focus();

            // Update selection
            if (!_selectedRowsOnly)
            {
                DeSelectAllCells();
                SetSelectedCells();
            }
            else
            {
                SetSelectedCells();
            }

            UpdateRowHeaders();
        }

        /// <summary>
        /// Handles changes to the vertical scrollbar's enabled state.
        /// Shows or hides the scrollbar based on whether it is enabled.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void VerticalScrollbar_IsEnabledChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            if (VerticalScrollbar.IsEnabled == false)
                VerticalScrollbar.Visibility = Visibility.Collapsed;
            else
                VerticalScrollbar.Visibility = Visibility.Visible;
        }

        /// <summary>
        /// Handles size changes of the horizontal scroll viewer by refreshing the view.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void HorizontalScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e) => RefreshView();

        /// <summary>
        /// Handles mouse wheel scrolling on the grid panel.
        /// Uses proportional scrolling based on wheel delta (e.Delta/10).
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data containing wheel delta.</param>
        private void TestGridPanel_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            VerticalScrollbar.Value -= e.Delta / 10; // if e.Delta = 30 then table will go up 3 rows
        }

        /// <summary>
        /// Handles keyboard navigation, selection, editing, and clipboard shortcuts.
        /// </summary>
        /// <param name="sender">The grid panel that raised the event.</param>
        /// <param name="e">The key event data.</param>
        private void Grid_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            int firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
            int lastRowDataIndex = firstRowDataIndex + _visibleRowCount - 1;
            int rowIndex = _activeCellVirtualRowIndex - firstRowDataIndex;
            if (e.Key == Key.PageDown)
            {
                if (lastRowDataIndex == DataView.NumberOfRows - 1) return;
                if (_cellEditTextBox.IsFocused) GridPanel.Focus();
                VerticalScrollbar.Value += _visibleRowCount;
                int maxRows = DataView.NumberOfRows;
                if (_selectedRowsOnly == true) maxRows = _selectedDataRowIndices.Count;
                if ((_activeCellVirtualRowIndex + _visibleRowCount) < maxRows - _visibleRowCount)
                {
                    SetActiveCell(_activeCellVirtualRowIndex + _visibleRowCount, _activeCellDataColumnIndex);
                }
                else
                {
                    SetActiveCell((int)Math.Floor(VerticalScrollbar.Value) + rowIndex, _activeCellDataColumnIndex);
                }
            }
            if (e.Key == Key.PageUp)
            {
                if (firstRowDataIndex == 0) return;
                if (_cellEditTextBox.IsFocused) GridPanel.Focus();
                VerticalScrollbar.Value -= _visibleRowCount;
                if ((firstRowDataIndex - _visibleRowCount) >= 0)
                {
                    SetActiveCell(_activeCellVirtualRowIndex - _visibleRowCount, _activeCellDataColumnIndex);
                }
                else
                {
                    SetActiveCell((int)Math.Floor(VerticalScrollbar.Value) + rowIndex, _activeCellDataColumnIndex);
                }
            }
            if (_cellEditTextBox.IsFocused) return;
            if (e.Key == Key.Left)
            {
                if (_activeCellDataColumnIndex > 0) SetActiveCell(_activeCellVirtualRowIndex, _activeCellDataColumnIndex - 1);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Down)
            {
                if (_selectedRowsOnly == true)
                {
                    if (_activeCellVirtualRowIndex < _selectedDataRowIndices.Count - 1) SetActiveCell(_activeCellVirtualRowIndex + 1, _activeCellDataColumnIndex);
                }
                else
                {
                    if (_activeCellVirtualRowIndex < DataView.NumberOfRows - 1) SetActiveCell(_activeCellVirtualRowIndex + 1, _activeCellDataColumnIndex);
                }
                if (_activeCellVirtualRowIndex < firstRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
                if (_activeCellVirtualRowIndex > lastRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Right)
            {
                if (_activeCellDataColumnIndex < DataView.ColumnNames.Count() - 1) SetActiveCell(_activeCellVirtualRowIndex, _activeCellDataColumnIndex + 1);
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Up)
            {
                if (_activeCellVirtualRowIndex > 0) SetActiveCell(_activeCellVirtualRowIndex - 1, _activeCellDataColumnIndex);
                if (_activeCellVirtualRowIndex < firstRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
                if (_activeCellVirtualRowIndex > lastRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;
                e.Handled = true;
                return;
            }
        }

        /// <summary>
        /// Handles the KeyDown event for Tab, Enter, copy/paste, and cell editing.
        /// </summary>
        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (_cellEditTextBox.IsFocused) return;
            if (e.Key == Key.LeftCtrl || e.Key == Key.RightCtrl) return;
            if (e.Key == Key.LeftShift || e.Key == Key.RightShift) return;

            int firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
            int lastRowDataIndex = firstRowDataIndex + _visibleRowCount - 1;

            if (e.Key == Key.Tab)
            {
                if (_activeCellDataColumnIndex < DataView.ColumnNames.Count() - 1)
                    SetActiveCell(_activeCellVirtualRowIndex, _activeCellDataColumnIndex + 1);
                if (_activeCellVirtualRowIndex < firstRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
                if (_activeCellVirtualRowIndex > lastRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Enter)
            {
                int maxRows = _selectedRowsOnly ? _selectedDataRowIndices.Count : DataView.NumberOfRows;
                if (_activeCellVirtualRowIndex < maxRows - 1)
                    SetActiveCell(_activeCellVirtualRowIndex + 1, _activeCellDataColumnIndex);
                if (_activeCellVirtualRowIndex < firstRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
                if (_activeCellVirtualRowIndex > lastRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;
                e.Handled = true;
                return;
            }

            // Filter out non-character keys
            if ((e.Key < Key.NumPad0 || e.Key > Key.Divide) && (e.Key < Key.D0 || e.Key > Key.D9) &&
                (e.Key < Key.A || e.Key > Key.Z) && (e.Key < Key.Oem1 || e.Key > Key.Oem3) &&
                (e.Key < Key.OemOpenBrackets || e.Key > Key.OemQuotes) && e.Key != Key.Space)
            {
                e.Handled = true;
                return;
            }

            if (e.Key == Key.Escape)
            {
                Clipboard.Clear();
                return;
            }

            // Copy/Paste Logic
            if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl))
            {
                if (e.Key == Key.V && Editable && !_selectedRowsOnly)
                {
                    if (Clipboard.ContainsText())
                    {
                        if (_activeCellVirtualRowIndex < firstRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
                        if (_activeCellVirtualRowIndex > lastRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;
                        Paste();
                    }
                }
                else if (e.Key == Key.C)
                {
                    Copy();
                }
                else if (e.Key == Key.A)
                {
                    AllCellsSelected = true;
                    _selectedDataRowIndices.Clear();
                    _selectedCellIndices.Clear();
                    _selectedColumnIndices.Clear();
                    DeSelectAllCells();
                    SetSelectedCells();
                }
                else if (e.Key == Key.Z && Undo.IsEnabled && Undo.Visibility == Visibility.Visible)
                {
                    UndoLastEdit();
                }
                else if (e.Key == Key.Y && Redo.IsEnabled && Redo.Visibility == Visibility.Visible)
                {
                    RedoLastEdit();
                }
                return;
            }

            // Enter cell edit logic
            if (Editable && !_readOnlyColumns.Contains(_activeCellDataColumnIndex))
            {
                if (_activeCellVirtualRowIndex < firstRowDataIndex)
                {
                    VerticalScrollbar.Value = _activeCellVirtualRowIndex;
                    firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
                }
                if (_activeCellVirtualRowIndex > lastRowDataIndex)
                {
                    VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;
                    firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
                }
                if (_activeCellVirtualRowIndex < 0) return;

                int rowIndex = _activeCellVirtualRowIndex - firstRowDataIndex;
                _mouseSelectionMode = SelectionMode.EditSelect;
                string initialText = GetCellText(rowIndex, _activeCellDataColumnIndex);
                _cellEditTextBox.Tag = new Tuple<string, Point>(initialText, new Point(_activeCellDataColumnIndex, GetDataRowIndex(rowIndex)));
                Grid.SetColumn(_cellEditTextBox, _activeCellDataColumnIndex);
                Grid.SetRow(_cellEditTextBox, rowIndex);
                if (GridPanel.Children.Contains(_cellEditTextBox)) GridPanel.Children.Remove(_cellEditTextBox);
                GridPanel.Children.Add(_cellEditTextBox);
                _cellEditTextBox.SelectAll();
                PreviewEditText(null, e);
                _cellEditTextBox.Focus();
            }
        }

        /// <summary>
        /// Handles special keys (Enter, Tab, Escape) during cell editing.
        /// </summary>
        private void PreviewEditText(object? sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter || e.Key == Key.Tab)
            {
                GridPanel.Focus();
            }
            else if (e.Key == Key.Escape)
            {
                _cellEditTextBox.Text = GetCellText(Grid.GetRow(_cellEditTextBox), Grid.GetColumn(_cellEditTextBox));
                GridPanel.Focus();
            }
        }

        /// <summary>
        /// Handles the LostFocus event for the cell edit TextBox, saving changes to the data.
        /// </summary>
        private void EditTextLostFocus(object sender, RoutedEventArgs e)
        {
            var editBox = (TextBox)sender;
            int firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
            int lastRowDataIndex = firstRowDataIndex + _visibleRowCount - 1;
            if (_activeCellVirtualRowIndex < firstRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex;
            if (_activeCellVirtualRowIndex > lastRowDataIndex) VerticalScrollbar.Value = _activeCellVirtualRowIndex - _visibleRowCount + 1;

            int columnIndex, rowIndex;
            if (editBox.Tag is not Tuple<string, Point> tagTuple)
            {
                columnIndex = Grid.GetColumn(editBox);
                rowIndex = _activeCellVirtualRowIndex - firstRowDataIndex;
                rowIndex = GetDataRowIndex(rowIndex);
            }
            else
            {
                columnIndex = (int)tagTuple.Item2.X;
                rowIndex = (int)tagTuple.Item2.Y;
            }

            GridPanel.Children.Remove(editBox);
            string newText = editBox.Text;
            var originalTag = editBox.Tag as Tuple<string, Point>;
            if (originalTag != null && originalTag.Item1 != newText)
            {
                try
                {
                    DataView.EditCell(rowIndex, columnIndex, newText);
                    if (_selectedRowsOnly)
                    {
                        int selectedRowOffset;
                        if (_columnSortOrder == SortOrder.None)
                            selectedRowOffset = _rowOffset![rowIndex];
                        else
                            selectedRowOffset = Array.IndexOf(_sortedSelectedRowOffsets!, _rowOffset![rowIndex]);
                        SetCellText(selectedRowOffset - firstRowDataIndex, columnIndex, newText);
                    }
                    else
                    {
                        SetCellText(_rowOffset![rowIndex] - firstRowDataIndex, columnIndex, newText);
                    }
                    UpdateUndoRedoButtons();
                    SetActiveCell();
                    _mouseSelectionMode = SelectionMode.None;
                }
                catch (Exception ex)
                {
                    GenericControls.MessageBox.Show(ex.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        /// <summary>
        /// Handles the MouseLeftButtonDown event on the row headers grid to start row selection.
        /// </summary>
        private void RowsGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GridPanel.Focus();
            if (_selectedRowsOnly)
            {
                _mouseSelectionMode = SelectionMode.None;
                return;
            }

            AllCellsSelected = false;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) &&
                !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                if (_selectedDataRowIndices.Count > 0)
                {
                    _selectedDataRowIndices.Clear();
                    SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
                }
                _selectedCellIndices.Clear();
                _selectedColumnIndices.Clear();
                DeSelectAllCells();
            }

            Point gridPosition = e.GetPosition(GridPanel);
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                _mouseDownVirtualRowIndex = GetTableRowIndex(gridPosition) + (int)Math.Floor(VerticalScrollbar.Value);
                _activeCellVirtualRowIndex = _mouseDownVirtualRowIndex;
                double widthSums = 0;
                int counter = 0;
                while (widthSums <= HorizontalScrollViewer.HorizontalOffset && counter < ColumnHeadersGrid.ColumnDefinitions.Count)
                {
                    widthSums += ColumnHeadersGrid.ColumnDefinitions[counter].ActualWidth;
                    counter++;
                }
                _activeCellDataColumnIndex = Math.Max(0, counter - 1);
            }

            _mouseSelectionMode = RowSelectable ? SelectionMode.RowSelect : SelectionMode.None;
            ((UIElement)sender).CaptureMouse();
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event on the row headers grid to complete row selection.
        /// </summary>
        private void RowsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AllCellsSelected = false;
            int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
            Point gridPosition = e.GetPosition(GridPanel);
            int mouseUpVirtualRowIndex = GetTableRowIndex(gridPosition) + verticalScrollBarValue;
            int mouseUpColumnIndex = GetTableColumnIndex(gridPosition);

            if (_mouseSelectionMode == SelectionMode.RowSelect)
            {
                int rowStep = mouseUpVirtualRowIndex < _mouseDownVirtualRowIndex ? -1 : 1;
                for (int i = _mouseDownVirtualRowIndex; rowStep > 0 ? i <= mouseUpVirtualRowIndex : i >= mouseUpVirtualRowIndex; i += rowStep)
                    _selectedDataRowIndices.Add(_rowId![i]);
                _selectedDataRowIndices = _selectedDataRowIndices.Distinct().ToList();
                _selectedDataRowIndices.Sort();
                SetSelectedCells();
                SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
            }
            else if (_mouseSelectionMode == SelectionMode.CellSelect)
            {
                if (_mouseDownVirtualRowIndex == mouseUpVirtualRowIndex && _mouseDownColumnIndex == mouseUpColumnIndex)
                {
                    int dataRowIndex = _rowId![mouseUpVirtualRowIndex];
                    if (!_selectedCellIndices.ContainsKey(dataRowIndex))
                        _selectedCellIndices.Add(dataRowIndex, new SortedSet<int>());
                    _selectedCellIndices[dataRowIndex].Add(mouseUpColumnIndex);
                    SelectCell(mouseUpColumnIndex, mouseUpVirtualRowIndex - verticalScrollBarValue);
                }
                else
                {
                    int rowStep = mouseUpVirtualRowIndex >= _mouseDownVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseUpColumnIndex >= _mouseDownColumnIndex ? 1 : -1;

                    for (int i = _mouseDownVirtualRowIndex; rowStep > 0 ? i <= mouseUpVirtualRowIndex : i >= mouseUpVirtualRowIndex; i += rowStep)
                    {
                        int dataRowIndex = _rowId![i];
                        if (!_selectedCellIndices.ContainsKey(dataRowIndex))
                            _selectedCellIndices.Add(dataRowIndex, new SortedSet<int>());
                        for (int j = _mouseDownColumnIndex; columnStep > 0 ? j <= mouseUpColumnIndex : j >= mouseUpColumnIndex; j += columnStep)
                            _selectedCellIndices[dataRowIndex].Add(j);
                    }
                    SetSelectedCells();
                }
            }

            UpdateSelectionButtonStates();
            _mouseSelectionMode = SelectionMode.None;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        /// <summary>
        /// Handles the MouseMove event on the row headers grid for drag selection of rows.
        /// </summary>
        private void RowsGrid_MouseMove(object sender, MouseEventArgs e)
        {
            Point gridPosition = e.GetPosition(GridPanel);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_mouseSelectionMode == SelectionMode.None || _mouseSelectionMode == SelectionMode.EditSelect) return;

                AllCellsSelected = false;
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                    Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    DeSelectAllCells();
                    SetSelectedCells();
                }
                else
                {
                    DeSelectAllCells();
                }

                int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
                int mouseMoveDataRowIndex = GetTableRowIndex(gridPosition) + verticalScrollBarValue;

                if (_mouseSelectionMode == SelectionMode.RowSelect)
                {
                    int rowStep = mouseMoveDataRowIndex < _mouseDownVirtualRowIndex ? -1 : 1;
                    int startRow, endRow;

                    if (_mouseDownVirtualRowIndex < verticalScrollBarValue)
                    {
                        startRow = 0;
                        endRow = mouseMoveDataRowIndex - verticalScrollBarValue;
                    }
                    else if (_mouseDownVirtualRowIndex > verticalScrollBarValue + _visibleRowCount - 1)
                    {
                        startRow = _visibleRowCount - 1;
                        endRow = mouseMoveDataRowIndex - verticalScrollBarValue;
                    }
                    else
                    {
                        startRow = _mouseDownVirtualRowIndex - verticalScrollBarValue;
                        endRow = mouseMoveDataRowIndex - verticalScrollBarValue;
                    }

                    for (int i = startRow; rowStep > 0 ? i <= endRow : i >= endRow; i += rowStep)
                        for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                            SelectCell(j, i);
                }
                else if (_mouseSelectionMode == SelectionMode.CellSelect)
                {
                    int mouseMoveColumnIndex = GetTableColumnIndex(gridPosition);
                    int rowStep = mouseMoveDataRowIndex >= _mouseDownVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseMoveColumnIndex >= _mouseDownColumnIndex ? 1 : -1;

                    for (int i = _mouseDownColumnIndex; columnStep > 0 ? i <= mouseMoveColumnIndex : i >= mouseMoveColumnIndex; i += columnStep)
                        for (int j = _mouseDownVirtualRowIndex - verticalScrollBarValue;
                             rowStep > 0 ? j <= mouseMoveDataRowIndex - verticalScrollBarValue : j >= mouseMoveDataRowIndex - verticalScrollBarValue;
                             j += rowStep)
                            SelectCell(i, j);
                }
                SetActiveCell();
            }
        }

        /// <summary>
        /// Handles the MouseRightButtonUp event on the row headers grid for context menu.
        /// </summary>
        private void RowsGrid_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            GridPanel.Focus();
            int dataRowIndex = GetDataRowIndex(GetTableRowIndex(e.GetPosition(GridPanel)));

            if (_selectedDataRowIndices.BinarySearch(dataRowIndex) < 0)
            {
                _selectedDataRowIndices.Clear();
                _selectedDataRowIndices.Add(dataRowIndex);
                DeSelectAllCells();
                SetSelectedCells();
                SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
            }

            var rowMenu = new ContextMenu();
            if (Editable)
            {
                string headerText = _selectedDataRowIndices.Count == 1 ? "Delete Row" : "Delete Rows";
                var rowMenuItem = new MenuItem
                {
                    Header = headerText,
                    IsEnabled = Editable,
                    Icon = FindResource("DeleteRowIcon")
                };
                rowMenuItem.Click += DeleteRows;
                rowMenu.Items.Add(rowMenuItem);
            }

            rowMenu.IsOpen = true;
            RowRightButtonUp?.Invoke(rowMenu, dataRowIndex);
        }

        /// <summary>
        /// Forwards mouse wheel events from the row headers grid to the main grid panel handler.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void RowsGrid_MouseWheel(object sender, MouseWheelEventArgs e) => TestGridPanel_MouseWheel(sender, e);

        /// <summary>
        /// Handles the MouseLeftButtonDown event on the column headers grid to start column selection.
        /// </summary>
        private void ColumnsGrid_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GridPanel.Focus();
            AllCellsSelected = false;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) &&
                !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift) && !_selectedRowsOnly)
            {
                if (_selectedDataRowIndices.Count > 0)
                {
                    _selectedDataRowIndices.Clear();
                    SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
                }
                _selectedCellIndices.Clear();
                _selectedColumnIndices.Clear();
                DeSelectAllCells();
            }

            Point gridPosition = e.GetPosition(GridPanel);
            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                _mouseDownColumnIndex = GetTableColumnIndex(gridPosition);
                _activeCellVirtualRowIndex = (int)Math.Floor(VerticalScrollbar.Value);
                _activeCellDataColumnIndex = _mouseDownColumnIndex;
            }

            if (_selectedRowsOnly)
                _mouseSelectionMode = SelectionMode.None;
            else if (ColumnSelectable)
                _mouseSelectionMode = SelectionMode.ColumnSelect;
            else
                _mouseSelectionMode = SelectionMode.None;

            // Double-click to sort
            if (e.ClickCount == 2)
            {
                _mouseSelectionMode = SelectionMode.None;
                _mouseDownColumnIndex = GetTableColumnIndex(gridPosition);
                if (!_selectedRowsOnly) _selectedColumnIndices.Add(_mouseDownColumnIndex);
                switch (_columnSortOrder)
                {
                    case SortOrder.Ascending:
                        SortColumnDescending();
                        break;
                    case SortOrder.Descending:
                        SortColumnAscending();
                        break;
                    case SortOrder.None:
                        SortColumnDescending();
                        break;
                }
                return;
            }
            ((UIElement)sender).CaptureMouse();
        }

        /// <summary>
        /// Handles the MouseMove event on the column headers grid for drag selection of columns.
        /// </summary>
        private void ColumnsGrid_MouseMove(object sender, MouseEventArgs e)
        {
            Point gridPosition = e.GetPosition(GridPanel);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_mouseSelectionMode == SelectionMode.None) return;

                AllCellsSelected = false;
                if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                    Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                {
                    DeSelectAllCells();
                    SetSelectedCells();
                }
                else
                {
                    DeSelectAllCells();
                }

                if (_mouseSelectionMode == SelectionMode.ColumnSelect)
                {
                    int columnIndex = GetTableColumnIndex(gridPosition);
                    int columnStep = columnIndex < _mouseDownColumnIndex ? -1 : 1;
                    for (int i = _mouseDownColumnIndex; columnStep > 0 ? i <= columnIndex : i >= columnIndex; i += columnStep)
                        for (int j = 0; j < _visibleRowCount; j++)
                            SelectCell(i, j);
                }
                else if (_mouseSelectionMode == SelectionMode.CellSelect)
                {
                    int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
                    int mouseMoveDataRowIndex = verticalScrollBarValue;
                    int mouseMoveColumnIndex = GetTableColumnIndex(gridPosition);
                    int rowStep = mouseMoveDataRowIndex >= _mouseDownVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseMoveColumnIndex >= _mouseDownColumnIndex ? 1 : -1;

                    for (int i = _mouseDownColumnIndex; columnStep > 0 ? i <= mouseMoveColumnIndex : i >= mouseMoveColumnIndex; i += columnStep)
                        for (int j = _mouseDownVirtualRowIndex - verticalScrollBarValue; rowStep > 0 ? j <= mouseMoveDataRowIndex - verticalScrollBarValue : j >= mouseMoveDataRowIndex - verticalScrollBarValue; j += rowStep)
                            SelectCell(i, j);
                }
                SetActiveCell();
            }
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event on the column headers grid to complete column selection.
        /// </summary>
        private void ColumnsGrid_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AllCellsSelected = false;
            Point gridPosition = e.GetPosition(GridPanel);
            int mouseUpColumnIndex = GetTableColumnIndex(gridPosition);

            if (_mouseSelectionMode == SelectionMode.ColumnSelect)
            {
                int columnStep = mouseUpColumnIndex < _mouseDownColumnIndex ? -1 : 1;
                for (int i = _mouseDownColumnIndex; columnStep > 0 ? i <= mouseUpColumnIndex : i >= mouseUpColumnIndex; i += columnStep)
                    _selectedColumnIndices.Add(i);
                _selectedColumnIndices = _selectedColumnIndices.Distinct().ToList();
                _selectedColumnIndices.Sort();
                SetSelectedCells();
            }
            else if (_mouseSelectionMode == SelectionMode.CellSelect)
            {
                int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
                int mouseUpDataRowIndex = verticalScrollBarValue;

                if (_mouseDownVirtualRowIndex == mouseUpDataRowIndex && _mouseDownColumnIndex == mouseUpColumnIndex)
                {
                    // Single cell selection — accumulate (match VB behavior)
                    int dataRowKey = _rowId![mouseUpDataRowIndex];
                    if (!_selectedCellIndices.ContainsKey(dataRowKey))
                        _selectedCellIndices.Add(dataRowKey, new SortedSet<int>());
                    _selectedCellIndices[dataRowKey].Add(mouseUpColumnIndex);
                    SelectCell(mouseUpColumnIndex, mouseUpDataRowIndex - verticalScrollBarValue);
                }
                else
                {
                    // Range of cells — accumulate (match VB behavior)
                    int rowStep = mouseUpDataRowIndex >= _mouseDownVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseUpColumnIndex >= _mouseDownColumnIndex ? 1 : -1;

                    for (int i = _mouseDownVirtualRowIndex; rowStep > 0 ? i <= mouseUpDataRowIndex : i >= mouseUpDataRowIndex; i += rowStep)
                    {
                        int dataRowKey = _rowId![i];
                        if (!_selectedCellIndices.ContainsKey(dataRowKey))
                            _selectedCellIndices.Add(dataRowKey, new SortedSet<int>());
                        for (int j = _mouseDownColumnIndex; columnStep > 0 ? j <= mouseUpColumnIndex : j >= mouseUpColumnIndex; j += columnStep)
                            _selectedCellIndices[dataRowKey].Add(j);
                    }
                    SetSelectedCells();
                }
            }

            UpdateSelectionButtonStates();
            _mouseSelectionMode = SelectionMode.None;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        /// <summary>
        /// Handles the MouseLeftButtonDown event on the select-all button. Currently unused.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SelectAllLeftMouseDown(object sender, MouseButtonEventArgs e)
        {
            GridPanel.Focus();
            _mouseSelectionMode = SelectionMode.All;
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event on the select-all button to toggle all cells selection.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SelectAllLeftMouseUp(object sender, MouseButtonEventArgs e)
        {
            if (_mouseSelectionMode == SelectionMode.All)
            {
                AllCellsSelected = !AllCellsSelected;
                _selectedDataRowIndices.Clear();
                _selectedCellIndices.Clear();
                _selectedColumnIndices.Clear();
            }
            else if (_mouseSelectionMode == SelectionMode.CellSelect)
            {
                AllCellsSelected = false;
                int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
                int mouseUpDataRowIndex = verticalScrollBarValue;
                int mouseUpColumnIndex = 0;

                if (_mouseDownVirtualRowIndex == mouseUpDataRowIndex && _mouseDownColumnIndex == mouseUpColumnIndex)
                {
                    // Single cell selection
                    if (!_selectedCellIndices.ContainsKey(_rowId![mouseUpDataRowIndex]))
                        _selectedCellIndices.Add(_rowId[mouseUpDataRowIndex], new SortedSet<int>());
                    _selectedCellIndices[_rowId[mouseUpDataRowIndex]].Add(mouseUpColumnIndex);
                }
                else
                {
                    // Multi-cell selection
                    int rowStep = mouseUpDataRowIndex >= _mouseDownVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseUpColumnIndex >= _mouseDownColumnIndex ? 1 : -1;

                    for (int i = _mouseDownVirtualRowIndex; ; i += rowStep)
                    {
                        if (!_selectedCellIndices.ContainsKey(_rowId![i]))
                            _selectedCellIndices.Add(_rowId[i], new SortedSet<int>());

                        for (int j = _mouseDownColumnIndex; ; j += columnStep)
                        {
                            _selectedCellIndices[_rowId[i]].Add(j);
                            if (j == mouseUpColumnIndex) break;
                        }
                        if (i == mouseUpDataRowIndex) break;
                    }
                }
            }
            else
            {
                AllCellsSelected = false;
            }

            DeSelectAllCells();
            SetSelectedCells();
            _mouseSelectionMode = SelectionMode.None;
        }

        /// <summary>
        /// Handles the SelectAll button click to toggle selection of all cells.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void SelectAllButton_Click(object sender, RoutedEventArgs e)
        {
            AllCellsSelected = !AllCellsSelected;
            _selectedDataRowIndices.Clear();
            _selectedCellIndices.Clear();
            _selectedColumnIndices.Clear();
            DeSelectAllCells();
            SetSelectedCells();
        }

        /// <summary>
        /// Updates the SelectAll button icon and tooltip to reflect the current selection state.
        /// </summary>
        private void UpdateSelectAllButton()
        {
            if (AllCellsSelected)
            {
                SelectAllButton.Content = FindResource("DeselectAllIcon");
                SelectAllButton.ToolTip = "Deselect all cells";
            }
            else
            {
                SelectAllButton.Content = FindResource("SelectAllIcon");
                SelectAllButton.ToolTip = "Select all cells";
            }
        }

        /// <summary>
        /// Handles the Copy button click to copy selected cells to clipboard.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CopyButton_Click(object sender, RoutedEventArgs e) => CaptureSelectionToClipboard(false);

        /// <summary>
        /// Handles the Copy With Headers button click to copy selected cells with column headers.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void CopyWithHeadersButton_Click(object sender, RoutedEventArgs e) => CaptureSelectionToClipboard(true);

        /// <summary>
        /// Handles the Paste button click to paste clipboard content to selected cells.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void PasteButton_Click(object sender, RoutedEventArgs e) => Paste();

        /// <summary>
        /// Handles the Export Table button click to export table data.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ExportTableButton_Click(object sender, RoutedEventArgs e) => ExportTable();

        /// <summary>
        /// Handles the ShowAll button click to show all rows instead of selected rows only.
        /// </summary>
        /// <param name="sender">The source of the event, or null if called programmatically.</param>
        /// <param name="e">The event arguments, or null if called programmatically.</param>
        private void ShowAll_Checked(object? sender, RoutedEventArgs? e)
        {

            _selectedRowsOnly = false;
            VerticalScrollbar.Maximum = DataView.NumberOfRows - GetMaxRows(false);
            if (_selectedDataRowIndices.Count > 0)
            {
                if (_selectedDataRowIndices[0] > VerticalScrollbar.Maximum)
                {
                    VerticalScrollbar.Value = VerticalScrollbar.Maximum - 1;
                }
                else
                {
                    VerticalScrollbar.Value = _selectedDataRowIndices[0];
                }
            }
            _visibleRowCount = GetMaxRows(false);
            if (_visibleRowCount > DataView.NumberOfRows) _visibleRowCount = DataView.NumberOfRows;
            LoadRows();
            SetSelectedCells();
            UpdateRowHeaders();
            ShowAll.IsEnabled = false;
            GridPanel.Focus();
        }

        /// <summary>
        /// Handles the ShowSelected button click to show only selected rows.
        /// </summary>
        /// <param name="sender">The source of the event, or null if called programmatically.</param>
        /// <param name="e">The event arguments, or null if called programmatically.</param>
        public void ShowSelected_Checked(object? sender, RoutedEventArgs? e)
        {
            if (_selectedDataRowIndices.Count > 0)
            {
                if (_columnSortOrder != SortOrder.None)
                {
                    _sortedSelectedRowOffsets = new int[_selectedDataRowIndices.Count];
                    for (int i = 0; i < _selectedDataRowIndices.Count; i++)
                        _sortedSelectedRowOffsets[i] = _rowOffset![_selectedDataRowIndices[i]];
                    Array.Sort(_sortedSelectedRowOffsets);
                    if (_columnSortOrder == SortOrder.Descending)
                        Array.Reverse(_sortedSelectedRowOffsets);
                }

                _selectedRowsOnly = true;
                ShowAll.IsEnabled = true;
                if (_visibleRowCount > _selectedDataRowIndices.Count) _visibleRowCount = _selectedDataRowIndices.Count;
                VerticalScrollbar.Maximum = _selectedDataRowIndices.Count - _visibleRowCount;
                VerticalScrollbar.Value = 0;
                LoadRows();
                _activeCellDataColumnIndex = 0;
                _activeCellVirtualRowIndex = 0;
                SetSelectedCells();
                UpdateRowHeaders();
            }
            GridPanel.Focus();
        }

        /// <summary>
        /// Clears all row, column, and cell selections.
        /// </summary>
        /// <param name="sender">The button or menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void DeSelectAll_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedDataRowIndices.Count > 0)
            {
                _selectedDataRowIndices.Clear();
                SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
            }
            DeSelectAllCells();
            SetSelectedCells();
            ShowSelected.IsEnabled = false;
            DeSelectAll.IsEnabled = false;
            if (_selectedRowsOnly == true)
            {
                _selectedRowsOnly = false;
                _visibleRowCount = GetMaxRows(false);
                if (_visibleRowCount > DataView.NumberOfRows) _visibleRowCount = DataView.NumberOfRows;
                VerticalScrollbar.Maximum = DataView.NumberOfRows - _visibleRowCount;
                LoadRows();
                SetSelectedCells();
                ShowAll.IsEnabled = false;
            }
            GridPanel.Focus();
        }

        /// <summary>
        /// Opens the field calculator in attribute-selection mode and applies matching rows.
        /// </summary>
        /// <param name="sender">The button or menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void SelectByAttribute_Click(object sender, RoutedEventArgs e)
        {
            var attributeSelector = new FieldCalculator(DataView, _selectedDataRowIndices, null, null, true);
            attributeSelector.ContentRendered += SelectorRendered;

            if (attributeSelector.ShowDialog() == true)
            {
                AllCellsSelected = false;
                _selectedDataRowIndices.Clear();
                _selectedCellIndices.Clear();
                _selectedColumnIndices.Clear();
                DeSelectAllCells();
                if (_selectedRowsOnly == false)
                {
                    _selectedDataRowIndices = attributeSelector.GetSelectedRows;
                }
                else
                {
                    ShowAll_Checked(null, null);
                    _selectedDataRowIndices = attributeSelector.GetSelectedRows;
                    ShowSelected_Checked(null, null);
                }
                SetSelectedCells();
                if (_selectedDataRowIndices.Count > 0 && _selectedRowsOnly == false)
                {
                    ShowSelected.IsEnabled = true;
                    DeSelectAll.IsEnabled = true;
                }
                else
                {
                    ShowSelected.IsEnabled = false;
                    DeSelectAll.IsEnabled = false;
                }
                SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
                _attributeSelectorString = attributeSelector.ExpressionCalculator.GetExpressionText();
                attributeSelector.ContentRendered -= SelectorRendered;
            }
            GridPanel.Focus();
        }

        /// <summary>
        /// Restores the last attribute-selector expression after the dialog renders.
        /// </summary>
        /// <param name="sender">The rendered field calculator window.</param>
        /// <param name="e">The render event data.</param>
        private void SelectorRendered(object? sender, EventArgs e)
        {
            if (sender is FieldCalculator fc)
                fc.ExpressionCalculator.SetExpressionText(_attributeSelectorString);
        }

        /// <summary>
        /// Restores the last field-calculator expression after the dialog renders.
        /// </summary>
        /// <param name="sender">The rendered field calculator window.</param>
        /// <param name="e">The render event data.</param>
        private void CalculatorRendered(object? sender, EventArgs e)
        {
            if (sender is FieldCalculator fc)
                fc.ExpressionCalculator.SetExpressionText(_fieldCalculatorString);
        }

        /// <summary>
        /// Handles the MouseLeftButtonDown event on the GridPanel for cell selection and editing.
        /// Manages click-based cell selection, shift+click range selection, and double-click editing.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void GridPanel_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            GridPanel.Focus();
            Point gridPosition = e.GetPosition(GridPanel);

            if (_selectedRowsOnly)
            {
                // Re-select the previously active cell
                int firstRowDataIndex = (int)Math.Floor(VerticalScrollbar.Value);
                int lastRowDataIndex = firstRowDataIndex + _visibleRowCount - 1;
                if (_activeCellVirtualRowIndex >= firstRowDataIndex && _activeCellVirtualRowIndex <= lastRowDataIndex)
                    SelectCell(_activeCellDataColumnIndex, _activeCellVirtualRowIndex - firstRowDataIndex);

                _mouseSelectionMode = SelectionMode.None;
                _mouseDownColumnIndex = GetTableColumnIndex(gridPosition);
                _mouseDownVirtualRowIndex = (int)Math.Floor(VerticalScrollbar.Value) + GetTableRowIndex(gridPosition);
                _activeCellDataColumnIndex = _mouseDownColumnIndex;
                _activeCellVirtualRowIndex = _mouseDownVirtualRowIndex;
                ActiveCellLocationChanged?.Invoke();
                SetActiveCell();

                if (e.ClickCount == 2 && Editable && !_readOnlyColumns.Contains(_mouseDownColumnIndex))
                {
                    _mouseSelectionMode = SelectionMode.EditSelect;
                    int rowIndex = GetTableRowIndex(gridPosition);
                    string initialText = GetCellText(rowIndex, _mouseDownColumnIndex);
                    _cellEditTextBox.Tag = new Tuple<string, Point>(initialText, new Point(_mouseDownColumnIndex, GetDataRowIndex(rowIndex)));
                    Grid.SetColumn(_cellEditTextBox, _mouseDownColumnIndex);
                    Grid.SetRow(_cellEditTextBox, rowIndex);
                    _cellEditTextBox.Text = initialText;
                    if (GridPanel.Children.Contains(_cellEditTextBox)) GridPanel.Children.Remove(_cellEditTextBox);
                    GridPanel.Children.Add(_cellEditTextBox);
                    _cellEditTextBox.SelectAll();
                    return;
                }
                ((UIElement)sender).CaptureMouse();
                return;
            }

            AllCellsSelected = false;
            if (!Keyboard.IsKeyDown(Key.LeftCtrl) && !Keyboard.IsKeyDown(Key.RightCtrl) &&
                !Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                if (_selectedDataRowIndices.Count > 0)
                {
                    _selectedDataRowIndices.Clear();
                    SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
                }
                _selectedCellIndices.Clear();
                _selectedColumnIndices.Clear();
                DeSelectAllCells();
            }

            if (!Keyboard.IsKeyDown(Key.LeftShift) && !Keyboard.IsKeyDown(Key.RightShift))
            {
                _mouseDownVirtualRowIndex = (int)Math.Floor(VerticalScrollbar.Value) + GetTableRowIndex(gridPosition);
                _mouseDownColumnIndex = GetTableColumnIndex(gridPosition);
                _activeCellDataColumnIndex = _mouseDownColumnIndex;
                _activeCellVirtualRowIndex = _mouseDownVirtualRowIndex;
                ActiveCellLocationChanged?.Invoke();
            }

            if (CellSelectable)
                _mouseSelectionMode = SelectionMode.CellSelect;
            else if (RowSelectable)
                _mouseSelectionMode = SelectionMode.RowSelect;
            else if (ColumnSelectable)
                _mouseSelectionMode = SelectionMode.ColumnSelect;
            else
                _mouseSelectionMode = SelectionMode.None;

            if (e.ClickCount == 2 && Editable && !_readOnlyColumns.Contains(_mouseDownColumnIndex))
            {
                _mouseSelectionMode = SelectionMode.EditSelect;
                int rowIndex = GetTableRowIndex(gridPosition);
                _mouseDownVirtualRowIndex = (int)Math.Floor(VerticalScrollbar.Value) + rowIndex;
                string initialText = GetCellText(rowIndex, _mouseDownColumnIndex);
                _cellEditTextBox.Tag = new Tuple<string, Point>(initialText, new Point(_mouseDownColumnIndex, GetDataRowIndex(rowIndex)));
                Grid.SetColumn(_cellEditTextBox, _mouseDownColumnIndex);
                Grid.SetRow(_cellEditTextBox, rowIndex);
                _cellEditTextBox.Text = initialText;
                if (GridPanel.Children.Contains(_cellEditTextBox)) GridPanel.Children.Remove(_cellEditTextBox);
                GridPanel.Children.Add(_cellEditTextBox);
                _cellEditTextBox.SelectAll();
                return;
            }
            ((UIElement)sender).CaptureMouse();
        }

        /// <summary>
        /// Handles the MouseMove event on the GridPanel for drag selection.
        /// </summary>
        private void GridPanel_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                if (_mouseSelectionMode != SelectionMode.None && _mouseSelectionMode != SelectionMode.EditSelect)
                {
                    AllCellsSelected = false;
                    if (Keyboard.IsKeyDown(Key.LeftCtrl) || Keyboard.IsKeyDown(Key.RightCtrl) ||
                        Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift))
                    {
                        DeSelectAllCells();
                        SetSelectedCells();
                    }
                    else
                    {
                        DeSelectAllCells();
                    }
                }

                int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
                Point gridPosition = e.GetPosition(GridPanel);

                if (_mouseSelectionMode == SelectionMode.CellSelect)
                {
                    int tableRowIndex = GetTableRowIndex(gridPosition);
                    int mouseMoveDataRowIndex = tableRowIndex + verticalScrollBarValue;
                    int mouseMoveColumnIndex = GetTableColumnIndex(gridPosition);
                    int rowStep = mouseMoveDataRowIndex >= _activeCellVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseMoveColumnIndex >= _activeCellDataColumnIndex ? 1 : -1;
                    int startValue = _activeCellVirtualRowIndex - verticalScrollBarValue;
                    if (startValue < 0) startValue = 0;
                    if (startValue > _visibleRowCount - 1) startValue = _visibleRowCount - 1;

                    for (int i = _activeCellDataColumnIndex; columnStep > 0 ? i <= mouseMoveColumnIndex : i >= mouseMoveColumnIndex; i += columnStep)
                        for (int j = startValue; rowStep > 0 ? j <= (mouseMoveDataRowIndex - verticalScrollBarValue) : j >= (mouseMoveDataRowIndex - verticalScrollBarValue); j += rowStep)
                            SelectCell(i, j);
                }
                else if (_mouseSelectionMode == SelectionMode.RowSelect)
                {
                    int mouseMoveDataRowIndex = GetTableRowIndex(gridPosition) + verticalScrollBarValue;
                    int rowStep = mouseMoveDataRowIndex < _mouseDownVirtualRowIndex ? -1 : 1;
                    int startRow, endRow;

                    if (_mouseDownVirtualRowIndex < verticalScrollBarValue)
                    {
                        startRow = 0;
                        endRow = mouseMoveDataRowIndex - verticalScrollBarValue;
                    }
                    else if (_mouseDownVirtualRowIndex > verticalScrollBarValue + _visibleRowCount - 1)
                    {
                        startRow = _visibleRowCount - 1;
                        endRow = mouseMoveDataRowIndex - verticalScrollBarValue;
                    }
                    else
                    {
                        startRow = _mouseDownVirtualRowIndex - verticalScrollBarValue;
                        endRow = mouseMoveDataRowIndex - verticalScrollBarValue;
                    }

                    for (int i = startRow; rowStep > 0 ? i <= endRow : i >= endRow; i += rowStep)
                        for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                            SelectCell(j, i);
                }
                else if (_mouseSelectionMode == SelectionMode.ColumnSelect)
                {
                    int columnIndex = GetTableColumnIndex(gridPosition);
                    int columnStep = columnIndex < _mouseDownColumnIndex ? -1 : 1;
                    for (int i = _mouseDownColumnIndex; columnStep > 0 ? i <= columnIndex : i >= columnIndex; i += columnStep)
                        for (int j = 0; j < _visibleRowCount; j++)
                            SelectCell(i, j);
                }
                SetActiveCell();
            }
        }

        /// <summary>
        /// Handles the MouseLeftButtonUp event on the GridPanel to complete selection.
        /// </summary>
        private void GridPanel_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            AllCellsSelected = false;
            Point gridPosition = e.GetPosition(GridPanel);
            int verticalScrollBarValue = (int)Math.Floor(VerticalScrollbar.Value);
            int mouseUpDataRowIndex = GetTableRowIndex(gridPosition) + verticalScrollBarValue;
            int mouseUpColumnIndex = GetTableColumnIndex(gridPosition);

            if (_mouseSelectionMode == SelectionMode.CellSelect)
            {
                if (_activeCellVirtualRowIndex == mouseUpDataRowIndex && _activeCellDataColumnIndex == mouseUpColumnIndex)
                {
                    if (_selectedCellIndices.ContainsKey(_rowId![mouseUpDataRowIndex]))
                        _selectedCellIndices[_rowId[mouseUpDataRowIndex]].Add(mouseUpColumnIndex);
                    else
                        _selectedCellIndices.Add(_rowId[mouseUpDataRowIndex], new SortedSet<int> { mouseUpColumnIndex });
                }
                else
                {
                    int rowStep = mouseUpDataRowIndex >= _activeCellVirtualRowIndex ? 1 : -1;
                    int columnStep = mouseUpColumnIndex >= _activeCellDataColumnIndex ? 1 : -1;

                    for (int i = _activeCellVirtualRowIndex; rowStep > 0 ? i <= mouseUpDataRowIndex : i >= mouseUpDataRowIndex; i += rowStep)
                    {
                        if (!_selectedCellIndices.ContainsKey(_rowId![i]))
                            _selectedCellIndices.Add(_rowId[i], new SortedSet<int>());
                        for (int j = _activeCellDataColumnIndex; columnStep > 0 ? j <= mouseUpColumnIndex : j >= mouseUpColumnIndex; j += columnStep)
                            _selectedCellIndices[_rowId[i]].Add(j);
                    }
                }
                SetSelectedCells();
            }
            else if (_mouseSelectionMode == SelectionMode.RowSelect)
            {
                int rowStep = mouseUpDataRowIndex < _mouseDownVirtualRowIndex ? -1 : 1;
                for (int i = _mouseDownVirtualRowIndex; rowStep > 0 ? i <= mouseUpDataRowIndex : i >= mouseUpDataRowIndex; i += rowStep)
                    _selectedDataRowIndices.Add(_rowId![i]);
                _selectedDataRowIndices = _selectedDataRowIndices.Distinct().ToList();
                _selectedDataRowIndices.Sort();
                _mouseDownColumnIndex = _activeCellDataColumnIndex;
                _mouseDownVirtualRowIndex = _activeCellVirtualRowIndex;
                SetSelectedCells();
                SelectedRowIndicesChanged?.Invoke(_selectedDataRowIndices);
            }
            else if (_mouseSelectionMode == SelectionMode.ColumnSelect)
            {
                int columnStep = mouseUpColumnIndex < _mouseDownColumnIndex ? -1 : 1;
                for (int i = _mouseDownColumnIndex; columnStep > 0 ? i <= mouseUpColumnIndex : i >= mouseUpColumnIndex; i += columnStep)
                    _selectedColumnIndices.Add(i);
                _selectedColumnIndices = _selectedColumnIndices.Distinct().ToList();
                _selectedColumnIndices.Sort();
                SetSelectedCells();
            }
            else if (_mouseSelectionMode == SelectionMode.EditSelect)
            {
                _cellEditTextBox.Focus();
            }

            if (_mouseSelectionMode != SelectionMode.EditSelect)
            {
                UpdateSelectionButtonStates();
            }

            _mouseSelectionMode = SelectionMode.None;
            ((UIElement)sender).ReleaseMouseCapture();
        }

        /// <summary>
        /// Builds and opens the column context menu for the clicked column.
        /// </summary>
        /// <param name="sender">The grid panel that raised the event.</param>
        /// <param name="e">The mouse-button event data.</param>
        private void CreateColumnContextMenu(object sender, MouseButtonEventArgs e)
        {
            GridPanel.Focus();
            if (DataView.NumberOfRows == 0) return;

            Point gridPosition = e.GetPosition(GridPanel);
            _mouseDownColumnIndex = GetTableColumnIndex(gridPosition);
            //
            if (_selectedColumnIndices.BinarySearch(_mouseDownColumnIndex) < 0)
            {
                // Select the column but preserve row selection for the field calculator's "apply to selected records" feature.
                _selectedCellIndices.Clear();
                _selectedColumnIndices.Clear();
                _selectedColumnIndices.Add(_mouseDownColumnIndex);
                DeSelectAllCells();
                SetSelectedCells();
            }
            //
            var columnMenu = new ContextMenu();
            var columnMenuItem = new MenuItem { Header = "Sort Ascending", Icon = FindResource("SortAscFilterIcon") };
            columnMenuItem.Click += SortColumnAscending;
            if (_columnsSortedOrder![_mouseDownColumnIndex] == SortOrder.Ascending) columnMenuItem.IsEnabled = false;
            columnMenu.Items.Add(columnMenuItem);
            //
            columnMenuItem = new MenuItem { Header = "Sort Descending", Icon = FindResource("SortDescFilterIcon") };
            columnMenuItem.Click += SortColumnDescending;
            if (_columnsSortedOrder[_mouseDownColumnIndex] == SortOrder.Descending) columnMenuItem.IsEnabled = false;
            columnMenu.Items.Add(columnMenuItem);
            //
            columnMenuItem = new MenuItem { Header = "Clear Sort", Icon = FindResource("ClearFilterIcon") };
            columnMenuItem.Click += RemoveSort;
            if (_columnsSortedOrder[_mouseDownColumnIndex] == SortOrder.None) columnMenuItem.IsEnabled = false;
            columnMenu.Items.Add(columnMenuItem);
            //
            columnMenuItem = new MenuItem { Header = "Summary Statistics...", Icon = FindResource("StatisticsIcon") };
            columnMenuItem.Click += CalcColumnStatistics;
            if (DataView.ColumnTypes[_mouseDownColumnIndex] == typeof(object)) columnMenuItem.IsEnabled = false;
            columnMenu.Items.Add(columnMenuItem);
            //
            columnMenuItem = new MenuItem { Header = "Find...", Icon = FindResource("FindIcon") };
            columnMenuItem.Click += SearchText;
            if (_selectedRowsOnly == true) columnMenuItem.IsEnabled = false;
            if (DataView.NumberOfRows == 0) columnMenuItem.IsEnabled = false;
            columnMenu.Items.Add(columnMenuItem);

            if (Editable == true && _readOnlyColumns.Contains(_mouseDownColumnIndex) == false)
            {
                columnMenuItem = new MenuItem { Header = "Field Calculator...", Icon = FindResource("CalculatorIcon") };
                columnMenuItem.Click += OpenFcForSpecificColumn;
                columnMenu.Items.Add(columnMenuItem);
            }

            if (Editable == true && _readOnlyColumns.Contains(_mouseDownColumnIndex) == false)
            {
                columnMenuItem = new MenuItem { Header = "Delete Columns", Icon = FindResource("DeleteColumnIcon") };
                if (_selectedColumnIndices.Count == 1) columnMenuItem.Header = "Delete Column";
                columnMenuItem.Click += DeleteColumn;
                columnMenu.Items.Add(columnMenuItem);
            }

            columnMenu.IsOpen = true;

        }

        #endregion

        #region Sorting

        /// <summary>
        /// Handles the menu command that clears sorting.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void RemoveSort(object sender, RoutedEventArgs e) => RemoveSort();
        /// <summary>
        /// Clears all sort state and restores natural row order.
        /// </summary>
        private void RemoveSort()
        {
            _columnSortOrder = SortOrder.None;
            for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                _columnsSortedOrder![i] = SortOrder.None;

            for (int i = 0; i < _rowId!.Length; i++)
                _rowId[i] = i;
            _rowId.CopyTo(_rowOffset!, 0);

            UpdateVisibleRows();
            if (!_selectedRowsOnly) { DeSelectAllCells(); SetSelectedCells(); }
            ((ColumnHeader)ColumnHeadersGrid.Children[_mouseDownColumnIndex * 2]).RemoveSorter();
            UpdateRowHeaders();
        }

        /// <summary>
        /// Handles the menu command that sorts the current column descending.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void SortColumnDescending(object sender, RoutedEventArgs e) => SortColumnDescending();
        /// <summary>
        /// Sorts the current context-menu column in descending order.
        /// </summary>
        private void SortColumnDescending()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                SortColumn(_mouseDownColumnIndex, false);
                _columnSortOrder = SortOrder.Descending;
                for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                    _columnsSortedOrder![i] = SortOrder.None;
                _columnsSortedOrder![_mouseDownColumnIndex] = SortOrder.Descending;

                if (_selectedRowsOnly == false)
                {
                    DeSelectAllCells();
                    SetSelectedCells();
                }
                else
                {
                    _sortedSelectedRowOffsets = new int[_selectedDataRowIndices.Count];
                    for (int i = 0; i < _selectedDataRowIndices.Count; i++)
                        _sortedSelectedRowOffsets[i] = _rowOffset![_selectedDataRowIndices[i]];
                    Array.Sort(_sortedSelectedRowOffsets);
                    Array.Reverse(_sortedSelectedRowOffsets);
                }

                UpdateVisibleRows();
                for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                    ((ColumnHeader)ColumnHeadersGrid.Children[i * 2]).RemoveSorter();
                ((ColumnHeader)ColumnHeadersGrid.Children[_mouseDownColumnIndex * 2]).AddSorter(false);
                UpdateRowHeaders();
                Mouse.OverrideCursor = null;
            }
            catch { Mouse.OverrideCursor = null; }
        }

        /// <summary>
        /// Handles the menu command that sorts the current column ascending.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void SortColumnAscending(object sender, RoutedEventArgs e) => SortColumnAscending();
        /// <summary>
        /// Sorts the current context-menu column in ascending order.
        /// </summary>
        private void SortColumnAscending()
        {
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                SortColumn(_mouseDownColumnIndex, true);
                _columnSortOrder = SortOrder.Ascending;
                for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                    _columnsSortedOrder![i] = SortOrder.None;
                _columnsSortedOrder![_mouseDownColumnIndex] = SortOrder.Ascending;

                if (_selectedRowsOnly == false)
                {
                    DeSelectAllCells();
                    SetSelectedCells();
                }
                else
                {
                    _sortedSelectedRowOffsets = new int[_selectedDataRowIndices.Count];
                    for (int i = 0; i < _selectedDataRowIndices.Count; i++)
                        _sortedSelectedRowOffsets[i] = _rowOffset![_selectedDataRowIndices[i]];
                    Array.Sort(_sortedSelectedRowOffsets);
                }

                UpdateVisibleRows();
                for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                    ((ColumnHeader)ColumnHeadersGrid.Children[i * 2]).RemoveSorter();
                ((ColumnHeader)ColumnHeadersGrid.Children[_mouseDownColumnIndex * 2]).AddSorter(true);
                UpdateRowHeaders();
                Mouse.OverrideCursor = null;
            }
            catch { Mouse.OverrideCursor = null; }
        }

        /// <summary>
        /// Sorts row-index mappings for a column using the column data type.
        /// </summary>
        /// <param name="columnIndex">The column index to sort.</param>
        /// <param name="ascending"><c>true</c> for ascending order; otherwise descending.</param>
        private void SortColumn(int columnIndex, bool ascending)
        {
            object[] columnData = DataView.GetColumn(columnIndex);
            //
            List<int> idx;
            switch (DataView.ColumnTypes[columnIndex])
            {
                case Type t when t == typeof(byte):
                    var sortedByte = columnData.ToList().Select((x, i) => new KeyValuePair<byte, int>(Convert.IsDBNull(x) ? byte.MinValue : Convert.ToByte(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedByte.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(short):
                    var sortedShort = columnData.ToList().Select((x, i) => new KeyValuePair<short, int>(Convert.IsDBNull(x) ? short.MinValue : Convert.ToInt16(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedShort.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(ushort):
                    var sortedUShort = columnData.ToList().Select((x, i) => new KeyValuePair<ushort, int>(Convert.IsDBNull(x) ? ushort.MinValue : Convert.ToUInt16(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedUShort.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(int):
                    var sortedInt = columnData.ToList().Select((x, i) => new KeyValuePair<int, int>(Convert.IsDBNull(x) ? int.MinValue : Convert.ToInt32(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedInt.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(uint):
                    var sortedUInt = columnData.ToList().Select((x, i) => new KeyValuePair<uint, int>(Convert.IsDBNull(x) ? uint.MinValue : Convert.ToUInt32(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedUInt.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(long):
                    var sortedLong = columnData.ToList().Select((x, i) => new KeyValuePair<long, int>(Convert.IsDBNull(x) ? long.MinValue : Convert.ToInt64(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedLong.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(ulong):
                    var sortedULong = columnData.ToList().Select((x, i) => new KeyValuePair<ulong, int>(Convert.IsDBNull(x) ? ulong.MinValue : Convert.ToUInt64(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedULong.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(float):
                    var sortedSingle = columnData.ToList().Select((x, i) => new KeyValuePair<float, int>(Convert.IsDBNull(x) ? float.NaN : Convert.ToSingle(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedSingle.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(double):
                    var sortedDouble = columnData.ToList().Select((x, i) => new KeyValuePair<double, int>(Convert.IsDBNull(x) ? double.NaN : Convert.ToDouble(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedDouble.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(string):
                    var sortedString = columnData.ToList().Select((x, i) => new KeyValuePair<string, int>(x.ToString()!, i)).OrderBy(x => x.Key).ToList();
                    idx = sortedString.Select(x => x.Value).ToList();
                    break;
                case Type t when t == typeof(bool):
                    var sortedBool = columnData.ToList().Select((x, i) => new KeyValuePair<bool, int>(Convert.IsDBNull(x) ? false : Convert.ToBoolean(x), i)).OrderBy(x => x.Key).ToList();
                    idx = sortedBool.Select(x => x.Value).ToList();
                    break;
                default:
                    var sortedDefault = columnData.ToList().Select((x, i) => new KeyValuePair<string, int>(x.ToString()!, i)).OrderBy(x => x.Key).ToList();
                    idx = sortedDefault.Select(x => x.Value).ToList();
                    break;
            }
            //
            if (ascending == false) idx.Reverse();
            for (int i = 0; i <= idx.Count - 1; i++)
            {
                _rowOffset![idx[i]] = i;
            }
            idx.CopyTo(_rowId!);
        }

        #endregion

        #region Column Context Menu Actions

        /// <summary>
        /// Opens the statistics window for the current context-menu column.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void CalcColumnStatistics(object sender, RoutedEventArgs e)
        {
            var columnstats = new ColumnStatsWindow(this, _mouseDownColumnIndex) { Owner = Window.GetWindow(this) };
            columnstats.Show();
        }

        /// <summary>
        /// Opens find and replace for the current context-menu column.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void SearchText(object sender, RoutedEventArgs e)
        {
            if (DataView.NumberOfRows > 0)
            {
                var findWindow = new FindAndReplace(this, _mouseDownColumnIndex, _activeCellVirtualRowIndex);
                findWindow.ShowDialog();
            }
        }

        /// <summary>
        /// Opens the field calculator targeting the current context-menu column.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void OpenFcForSpecificColumn(object sender, RoutedEventArgs e)
        {
            var f = new FieldCalculator(DataView, _selectedDataRowIndices, _readOnlyColumns, DataView.ColumnNames[_mouseDownColumnIndex]);
            if (f.ShowDialog() == true)
            {
                UpdateVisibleRows();
                UpdateUndoRedoButtons();
            }
        }

        /// <summary>
        /// Deletes selected editable columns from the data view.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void DeleteColumn(object sender, RoutedEventArgs e)
        {
            var columnIndices = _selectedColumnIndices.ToList();
            for (int i = columnIndices.Count - 1; i >= 0; i--)
            {
                if (_readOnlyColumns.Contains(columnIndices[i])) columnIndices.RemoveAt(i);
            }
            //
            DataView.DeleteColumns(columnIndices.ToArray());
        }

        #endregion

        #region Clipboard
        /// <summary>
        /// Builds and opens the grid clipboard context menu.
        /// </summary>
        /// <param name="sender">The grid panel that raised the event.</param>
        /// <param name="e">The mouse-button event data.</param>
        private void GridPanel_RightMouseUp(object sender, MouseButtonEventArgs e)
        {
            bool enableCopy = false;
            if (AllCellsSelected == true)
            {
                enableCopy = true;
            }
            else if (_selectedCellIndices.Count > 0)
            {
                enableCopy = true;
            }
            else if (_selectedColumnIndices.Count > 0)
            {
                enableCopy = true;
            }
            else if (_selectedDataRowIndices.Count > 0)
            {
                enableCopy = true;
            }
            //
            var gridMenu = new ContextMenu();
            var gridMenuItem = new MenuItem { IsEnabled = enableCopy, Header = "Copy", Icon = FindResource("CopyIcon") };
            gridMenuItem.Click += Copy;
            gridMenu.Items.Add(gridMenuItem);
            //
            gridMenuItem = new MenuItem { IsEnabled = enableCopy, Header = "Copy w/ Headers", Icon = FindResource("CopyWithHeadersIcon") };
            gridMenuItem.Click += CopyWithHeaders;
            gridMenu.Items.Add(gridMenuItem);
            //
            if (Editable == true)
            {
                gridMenuItem = new MenuItem { Header = "Paste", Icon = FindResource("PasteIcon") };
                gridMenuItem.Click += Paste;
                if (Clipboard.ContainsText() == false || _selectedRowsOnly == true) gridMenuItem.IsEnabled = false;
                gridMenu.Items.Add(gridMenuItem);
            }
            gridMenu.IsOpen = true;
        }
        /// <summary>
        /// Handles the menu command that copies the current selection without headers.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void Copy(object sender, RoutedEventArgs e) => Copy();
        /// <summary>
        /// Copies the current selection without headers.
        /// </summary>
        private void Copy()
        {
            try
            {
                CaptureSelectionToClipboard(false);
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message);
            }
        }
        /// <summary>
        /// Copies the current selection with column headers.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void CopyWithHeaders(object sender, RoutedEventArgs e)
        {
            try
            {
                CaptureSelectionToClipboard(true);
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Writes the current uniform selection to the clipboard.
        /// </summary>
        /// <param name="includeHeaders">Whether to include column headers in the copied text.</param>
        private void CaptureSelectionToClipboard(bool includeHeaders)
        {
            if (!IsSelectionUniform())
                throw new Exception("Selection must be uniform to copy.");

            Clipboard.Clear();

            if (AllCellsSelected)
            {
                CopyAllToClipboard(includeHeaders);
            }
            else if (_selectedDataRowIndices.Count > 0)
            {
                CopySelectedRowsToClipboard(includeHeaders);
            }
            else if (_selectedColumnIndices.Count > 0)
            {
                CopySelectedColumnsToClipboard(includeHeaders);
            }
            else if (_selectedCellIndices.Count > 0)
            {
                CopySelectedCellsToClipboard(includeHeaders);
            }
            UpdatePasteButtonState();
        }

        /// <summary>
        /// Copies all table data to clipboard using GetRow pattern.
        /// </summary>
        private void CopyAllToClipboard(bool includeHeaders)
        {
            if (DataView.ColumnNames.Count() * DataView.NumberOfRows > 50000)
            {
                var msgString = $"Operation will copy {DataView.NumberOfRows * DataView.ColumnNames.Count()} cell values to the clipboard.  Are you sure you want to copy that much data to the clipboard?";
                if (GenericControls.MessageBox.Show(msgString, "Large Amount of Data To Clipboard", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
            }

            var boardText = new System.Text.StringBuilder();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (includeHeaders)
                {
                    boardText.Append(DataView.ColumnNames[0]);
                    for (int i = 1; i < DataView.ColumnNames.Count(); i++)
                    {
                        boardText.Append('\t' + DataView.ColumnNames[i]);
                    }
                    boardText.Append('\n');
                }

                object[] readerRow;
                for (int i = 0; i < DataView.NumberOfRows; i++)
                {
                    readerRow = DataView.GetRow(_rowId![i]);
                    boardText.Append(readerRow[0]?.ToString() ?? "");
                    for (int j = 1; j < DataView.ColumnNames.Count(); j++)
                    {
                        boardText.Append('\t' + (readerRow[j]?.ToString() ?? ""));
                    }
                    if (i < DataView.NumberOfRows - 1) boardText.Append('\n');
                }
                Mouse.OverrideCursor = null;
            }
            catch (Exception)
            {
                Mouse.OverrideCursor = null;
                throw new Exception("Error copying data to clipboard.");
            }

            Clipboard.SetText(boardText.ToString());
        }

        /// <summary>
        /// Copies selected columns to clipboard using GetColumn pattern.
        /// </summary>
        private void CopySelectedColumnsToClipboard(bool includeHeaders)
        {
            if (_selectedColumnIndices.Count * DataView.NumberOfRows > 50000)
            {
                var msgString = $"Operation will copy {_selectedColumnIndices.Count * DataView.NumberOfRows} cell values to the clipboard.  Are you sure you want to copy that much data to the clipboard?";
                if (GenericControls.MessageBox.Show(msgString, "Large Amount of Data To Clipboard", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
            }

            var boardText = new System.Text.StringBuilder();

            try
            {
                var columnData = new List<object[]>();
                Mouse.OverrideCursor = Cursors.Wait;

                for (int i = 0; i < _selectedColumnIndices.Count; i++)
                {
                    if (includeHeaders) boardText.Append(DataView.ColumnNames[_selectedColumnIndices[i]] + '\t');
                    columnData.Add(DataView.GetColumn(DataView.ColumnNames[_selectedColumnIndices[i]]));
                }
                if (includeHeaders && boardText.Length > 0) boardText[boardText.Length - 1] = '\n';

                for (int i = 0; i < DataView.NumberOfRows; i++)
                {
                    boardText.Append(columnData[0][_rowId![i]].ToString());
                    for (int j = 1; j < columnData.Count; j++)
                    {
                        boardText.Append('\t' + columnData[j][_rowId![i]].ToString());
                    }
                    boardText.Append('\n');
                }
                boardText.Remove(boardText.Length - 1, 1);
                Mouse.OverrideCursor = null;
            }
            catch (Exception)
            {
                Mouse.OverrideCursor = null;
                throw new Exception("Error copying data to clipboard.");
            }

            Clipboard.SetText(boardText.ToString());
        }

        /// <summary>
        /// Copies selected rows to clipboard using GetRow pattern.
        /// </summary>
        private void CopySelectedRowsToClipboard(bool includeHeaders)
        {
            if (_selectedDataRowIndices.Count * DataView.ColumnNames.Count() > 50000)
            {
                var msgString = $"Operation will copy {_selectedDataRowIndices.Count * DataView.ColumnNames.Count()} cell values to the clipboard.  Are you sure you want to copy that much data to the clipboard?";
                if (GenericControls.MessageBox.Show(msgString, "Large Amount of Data To Clipboard", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
            }

            var boardText = new System.Text.StringBuilder();
            object[]? readerRow;
            try
            {
                Mouse.OverrideCursor = Cursors.Wait;

                if (includeHeaders)
                {
                    boardText.Append(DataView.ColumnNames[0]);
                    for (int i = 1; i < DataView.ColumnNames.Count(); i++)
                    {
                        boardText.Append('\t' + DataView.ColumnNames[i]);
                    }
                    boardText.Append('\n');
                }

                if (_columnSortOrder != SortOrder.None)
                {
                    foreach (var r in GetSelectedRowVirtualRowIndices())
                    {
                        readerRow = DataView.GetRow(r.Value);
                        boardText.Append(readerRow[0]?.ToString() ?? "");
                        for (int j = 1; j < DataView.ColumnNames.Count(); j++)
                        {
                            boardText.Append('\t' + (readerRow[j]?.ToString() ?? ""));
                        }
                        boardText.Append('\n');
                    }
                    boardText.Remove(boardText.Length - 1, 1);
                }
                else
                {
                    for (int i = 0; i < _selectedDataRowIndices.Count; i++)
                    {
                        readerRow = DataView.GetRow(_selectedDataRowIndices[i]);
                        boardText.Append(readerRow[0]?.ToString() ?? "");
                        for (int j = 1; j < DataView.ColumnNames.Count(); j++)
                        {
                            boardText.Append('\t' + (readerRow[j]?.ToString() ?? ""));
                        }
                        boardText.Append('\n');
                    }
                    boardText.Remove(boardText.Length - 1, 1);
                }
                Mouse.OverrideCursor = null;
            }
            catch (Exception)
            {
                Mouse.OverrideCursor = null;
                throw new Exception("Error copying data to clipboard.");
            }

            Clipboard.SetText(boardText.ToString());
        }

        /// <summary>
        /// Copies selected cells to clipboard using GetRow pattern with visual ordering.
        /// </summary>
        private void CopySelectedCellsToClipboard(bool includeHeaders)
        {
            if (_selectedCellIndices.Count > 50000)
            {
                var msgString = $"Operation will copy {_selectedCellIndices.Count} cell values to the clipboard.  It can take a long time to copy this much data, are you sure you want to copy that much data to the clipboard?";
                if (GenericControls.MessageBox.Show(msgString, "Large Amount of Data To Clipboard", MessageBoxButton.YesNo) != MessageBoxResult.Yes)
                    return;
            }

            var boardText = new System.Text.StringBuilder();

            try
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var keysInVisualOrder = new SortedDictionary<int, int>();
                foreach (int dataRowIndex in _selectedCellIndices.Keys)
                {
                    keysInVisualOrder.TryAdd(_rowOffset![dataRowIndex], dataRowIndex);
                }

                if (includeHeaders)
                {
                    foreach (int column in _selectedCellIndices[_selectedCellIndices.Keys.First()])
                    {
                        boardText.Append(DataView.ColumnNames[column] + '\t');
                    }
                    boardText[boardText.Length - 1] = '\n';
                }

                object[] row;
                SortedSet<int> rowCellEdits;
                foreach (int dataRowIndex in keysInVisualOrder.Values)
                {
                    rowCellEdits = _selectedCellIndices[dataRowIndex];
                    row = DataView.GetRow(dataRowIndex);

                    foreach (int column in rowCellEdits)
                    {
                        boardText.Append((row[column]?.ToString() ?? "") + '\t');
                    }
                    boardText[boardText.Length - 1] = '\n';
                }
                boardText.Remove(boardText.Length - 1, 1);
                Mouse.OverrideCursor = null;
            }
            catch (Exception)
            {
                Mouse.OverrideCursor = null;
                throw new Exception("Error copying data to clipboard.");
            }

            Clipboard.SetText(boardText.ToString());
        }

        /// <summary>
        /// Checks if the current selection is uniform (not mixing different selection types).
        /// </summary>
        private bool IsSelectionUniform()
        {
            if (_selectedColumnIndices.Count > 0 && _selectedDataRowIndices.Count > 0) return false;

            // All Cells Selected
            if (AllCellsSelected) return true;

            if (_selectedDataRowIndices.Count > 0)
            {
                // Check for non-uniform cell selection
                if (_selectedCellIndices.Count > 0)
                {
                    foreach (var rowSelectedCells in _selectedCellIndices)
                    {
                        if (_selectedDataRowIndices.BinarySearch(rowSelectedCells.Key) < 0) return false;
                    }
                }
            }
            else if (_selectedColumnIndices.Count > 0)
            {
                // Check for non-uniform cell selection
                if (_selectedCellIndices.Count > 0)
                {
                    foreach (var rowSelectedCells in _selectedCellIndices)
                    {
                        foreach (int column in rowSelectedCells.Value)
                        {
                            if (_selectedColumnIndices.BinarySearch(column) < 0) return false;
                        }
                    }
                }
            }
            else if (_selectedCellIndices.Count > 0)
            {
                // Check that cell-only selections form a uniform rectangle
                int columnCount = _selectedCellIndices.First().Value.Count;
                int[] columnIndices = _selectedCellIndices.First().Value.ToArray();
                foreach (var rowSelectedCells in _selectedCellIndices)
                {
                    if (columnCount != rowSelectedCells.Value.Count) return false;
                    for (int i = 0; i < columnIndices.Length; i++)
                    {
                        if (!rowSelectedCells.Value.Contains(columnIndices[i])) return false;
                    }
                }
            }
            return true;
        }

        /// <summary>
        /// Gets selected row indices sorted by virtual row position for proper visual ordering.
        /// </summary>
        private SortedDictionary<int, int> GetSelectedRowVirtualRowIndices()
        {
            var sortedRowsIndices = new SortedDictionary<int, int>();
            for (int i = 0; i < _selectedDataRowIndices.Count; i++)
            {
                sortedRowsIndices.Add(_rowOffset![_selectedDataRowIndices[i]], _selectedDataRowIndices[i]);
            }
            if (_columnSortOrder == SortOrder.Descending)
                return new SortedDictionary<int, int>(sortedRowsIndices, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            return sortedRowsIndices;
        }

        /// <summary>
        /// Handles the paste command from a menu item or button.
        /// </summary>
        /// <param name="sender">The command source.</param>
        /// <param name="e">The routed event data.</param>
        private void Paste(object sender, RoutedEventArgs e)
        {
            Paste();
        }
        /// <summary>
        /// Pastes clipboard text into the current editable table selection.
        /// </summary>
        private void Paste()
        {
            try
            {
                PasteClipboard();
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message);
            }
        }

        /// <summary>
        /// Parses clipboard text into string array of arrays (rows and columns).
        /// </summary>
        private string[][] GetClipboardData()
        {
            string clipText = Clipboard.GetText();
            // Trim the trailing \r\n (or \n) that Windows clipboards append to copied cell
            // ranges. Without this trim, Split('\n') produces an empty trailing element that
            // the paste path later writes as a spurious blank row at the end of the selection.
            clipText = clipText.TrimEnd('\r', '\n');
            string[] clipTextLineSplit = clipText.Split('\n');
            string[][] result = new string[clipTextLineSplit.Length][];
            var clipboardRows = clipTextLineSplit.Select(r => r.Split('\t'));
            int counter = 0;

            foreach (var row in clipboardRows)
            {
                string[] rowData = new string[row.Length];
                for (int i = 0; i < row.Length; i++)
                {
                    if (row[i].Length > 0 && row[i][row[i].Length - 1] == '\r')
                    {
                        rowData[i] = row[i].Substring(0, row[i].Length - 1);
                    }
                    else
                    {
                        rowData[i] = row[i];
                    }
                }
                result[counter] = rowData;
                counter++;
            }

            return result;
        }

        /// <summary>
        /// Validates that the current selection is continuous (no gaps) for paste operation.
        /// </summary>
        private bool IsSelectionContinuous()
        {
            if (_selectedColumnIndices.Count > 0 && _selectedDataRowIndices.Count > 0) return false;

            if (AllCellsSelected)
            {
                return true;
            }
            else if (_selectedColumnIndices.Count > 0)
            {
                // Check columns are continuous
                for (int i = 1; i < _selectedColumnIndices.Count; i++)
                {
                    if (_selectedColumnIndices[i] - _selectedColumnIndices[i - 1] != 1) return false;
                }
                // Check cell selection matches column selection
                if (_selectedCellIndices.Count > 0)
                {
                    foreach (var selectedCellsByRow in _selectedCellIndices)
                    {
                        foreach (int columnIndex in selectedCellsByRow.Value)
                        {
                            if (_selectedColumnIndices.BinarySearch(columnIndex) < 0) return false;
                        }
                    }
                }
            }
            else if (_selectedDataRowIndices.Count > 0)
            {
                // Check rows are continuous (in visual order)
                var virtualSelectedRowIndices = GetSelectedRowVirtualRowIndices();
                int rowIndex = virtualSelectedRowIndices.First().Key;
                foreach (var r in virtualSelectedRowIndices)
                {
                    if (r.Key - rowIndex > 1) return false;
                    rowIndex = r.Key;
                }
                // Check cell selection matches row selection
                if (_selectedCellIndices.Count > 0)
                {
                    foreach (var selectedCellsByRow in _selectedCellIndices)
                    {
                        if (_selectedDataRowIndices.BinarySearch(selectedCellsByRow.Key) < 0) return false;
                    }
                }
            }
            else if (_selectedCellIndices.Count > 0)
            {
                if (_selectedCellIndices.Count == 1) return true;

                // Check columns are continuous across all rows
                int[] columnIndices = _selectedCellIndices.First().Value.ToArray();
                int tempRowIndex = _selectedCellIndices.First().Key;
                var virtualRowsSorted = new List<int>(_selectedCellIndices.Count);

                foreach (var selectedCellsByRow in _selectedCellIndices)
                {
                    virtualRowsSorted.Add(_rowOffset![selectedCellsByRow.Key]);
                    if (selectedCellsByRow.Key == tempRowIndex) continue;

                    for (int i = 0; i < columnIndices.Length; i++)
                    {
                        if (!selectedCellsByRow.Value.Contains(columnIndices[i])) return false;
                    }
                    tempRowIndex = selectedCellsByRow.Key;
                }

                // Check rows are continuous
                virtualRowsSorted.Sort();
                for (int i = 1; i < virtualRowsSorted.Count; i++)
                {
                    if (virtualRowsSorted[i] - virtualRowsSorted[i - 1] != 1) return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Main paste logic. Pastes clipboard data based on current selection type.
        /// </summary>
        private void PasteClipboard()
        {
            if (!IsSelectionContinuous())
                throw new Exception("Selection must be continuous to paste.");

            string[][] clipboardData = GetClipboardData();
            var rowIndices = new List<int>();
            var columnIndices = new List<int>();
            var editValues = new List<object>();

            if (AllCellsSelected)
            {
                // Paste into all cells
                for (int i = 0; i < DataView.NumberOfRows; i++)
                {
                    if (i == clipboardData.Length) break;
                    for (int j = 0; j < DataView.ColumnNames.Count(); j++)
                    {
                        if (j == clipboardData[i].Length) break;
                        if (_readOnlyColumns.Contains(j)) continue;
                        rowIndices.Add(_rowId![i]);
                        columnIndices.Add(j);
                        editValues.Add(clipboardData[i][j]);
                    }
                }
            }
            else if (_selectedColumnIndices.Count > 0)
            {
                // Paste into selected columns
                for (int i = 0; i < DataView.NumberOfRows; i++)
                {
                    if (i == clipboardData.Length) break;
                    for (int j = 0; j < _selectedColumnIndices.Count; j++)
                    {
                        if (j == clipboardData[i].Length) break;
                        if (_readOnlyColumns.Contains(_selectedColumnIndices[j])) continue;
                        rowIndices.Add(_rowId![i]);
                        columnIndices.Add(_selectedColumnIndices[j]);
                        editValues.Add(clipboardData[i][j]);
                    }
                }
            }
            else if (_selectedDataRowIndices.Count > 0)
            {
                // Paste into selected rows
                int rowIndexCounter = 0;
                foreach (var r in GetSelectedRowVirtualRowIndices())
                {
                    if (rowIndexCounter == clipboardData.Length) break;
                    for (int i = 0; i < DataView.ColumnNames.Count(); i++)
                    {
                        if (i == clipboardData[rowIndexCounter].Length) break;
                        if (_readOnlyColumns.Contains(i)) continue;
                        rowIndices.Add(r.Value);
                        columnIndices.Add(i);
                        editValues.Add(clipboardData[rowIndexCounter][i]);
                    }
                    rowIndexCounter++;
                }
            }
            else if (_selectedCellIndices.Count > 0)
            {
                int dataBaseRowIndex = _selectedCellIndices.First().Key;
                int columnIndex = _selectedCellIndices.First().Value.First();

                if (_selectedCellIndices.Count == 1 && _selectedCellIndices.First().Value.Count == 1)
                {
                    // Single cell selected - paste from that cell onwards
                    for (int i = 0; i < clipboardData.Length; i++)
                    {
                        if (_rowOffset![dataBaseRowIndex] + i >= DataView.NumberOfRows) break;
                        for (int j = 0; j < clipboardData[i].Length; j++)
                        {
                            if (columnIndex + j >= DataView.ColumnNames.Count()) break;
                            if (_readOnlyColumns.Contains(columnIndex + j)) continue;
                            rowIndices.Add(_rowId![_rowOffset[dataBaseRowIndex] + i]);
                            columnIndices.Add(columnIndex + j);
                            editValues.Add(clipboardData[i][j]);
                        }
                    }
                }
                else
                {
                    // Multiple cells selected - paste into selection
                    int rowIndexCounter = 0;
                    foreach (var r in GetSelectedCellVirtualRowIndices())
                    {
                        if (rowIndexCounter == clipboardData.Length) break;
                        int columnIndexCounter = 0;
                        foreach (int column in _selectedCellIndices[r.Value])
                        {
                            if (columnIndexCounter == clipboardData[rowIndexCounter].Length) break;
                            if (_readOnlyColumns.Contains(column)) continue;
                            rowIndices.Add(r.Value);
                            columnIndices.Add(column);
                            editValues.Add(clipboardData[rowIndexCounter][columnIndexCounter]);
                            columnIndexCounter++;
                        }
                        rowIndexCounter++;
                    }
                }
            }
            else
            {
                throw new Exception("No Cells are currently selected to paste into.");
            }

            DataView.EditCells(rowIndices.ToArray(), columnIndices.ToArray(), editValues.ToArray());
            UpdateVisibleRows();
            UpdateUndoRedoButtons();
        }

        /// <summary>
        /// Gets selected cell indices sorted by virtual row position for proper visual ordering.
        /// </summary>
        private SortedDictionary<int, int> GetSelectedCellVirtualRowIndices()
        {
            var sortedRowsIndices = new SortedDictionary<int, int>();
            foreach (var selectedCell in _selectedCellIndices)
            {
                sortedRowsIndices.Add(_rowOffset![selectedCell.Key], selectedCell.Key);
            }
            if (_columnSortOrder == SortOrder.Descending)
                return new SortedDictionary<int, int>(sortedRowsIndices, Comparer<int>.Create((a, b) => b.CompareTo(a)));
            return sortedRowsIndices;
        }

        /// <summary>
        /// Opens a save dialog and exports the table data to the selected file format.
        /// Supports CSV, DBF, Excel (XLSX), and SQLite formats.
        /// </summary>
        private void ExportTable()
        {
            if (DataView == null) return;
            string filters = "comma delimited(*.csv) |*.csv|database(*.dbf) |*.dbf|Excel(*.xlsx) |*.xlsx|Sqlite(*.sqlite) |*.sqlite";
            try
            {
                var saveFileBrowser = new SaveFileDialog { Filter = filters, FilterIndex = 1 };
                if (saveFileBrowser.ShowDialog() == true)
                {
                    switch (System.IO.Path.GetExtension(saveFileBrowser.FileName))
                    {
                        case ".csv":
                            DataView.ExportToCsv(saveFileBrowser.FileName);
                            break;
                        case ".dbf":
                            DataView.ExportToDbf(saveFileBrowser.FileName);
                            break;
                        case ".xlsx":
                            DataView.ExportToXlsx(saveFileBrowser.FileName);
                            break;
                        case ".sqlite":
                            DataView.ExportToSqlite(saveFileBrowser.FileName, DataView.TableName);
                            break;
                        default:
                            throw new Exception("selected file format extension '" + System.IO.Path.GetExtension(saveFileBrowser.FileName) + "' is not supported for export.");
                    }
                }
            }
            catch (Exception ex)
            {
                GenericControls.MessageBox.Show(ex.Message);
            }
        }

        #endregion

        #region Undo/Redo

        /// <summary>
        /// Updates undo, redo, and save button enabled states.
        /// </summary>
        private void UpdateUndoRedoButtons()
        {
            bool canUndo = DataView.CanUndo();
            Undo.IsEnabled = canUndo;
            SaveButton.IsEnabled = canUndo;
            Redo.IsEnabled = DataView.CanRedo();
        }

        /// <summary>
        /// Handles the Undo button click event.
        /// </summary>
        private void Undo_Click(object sender, RoutedEventArgs e) => UndoLastEdit();

        /// <summary>
        /// Handles the Redo button click event.
        /// </summary>
        private void Redo_Click(object sender, RoutedEventArgs e) => RedoLastEdit();

        /// <summary>
        /// Prompts for confirmation and applies pending edits to the data view.
        /// </summary>
        /// <param name="sender">The save command source.</param>
        /// <param name="e">The routed event data.</param>
        private void Save_Click(object sender, RoutedEventArgs e)
        {
            if (GenericControls.MessageBox.Show("Are you sure you want to save edits?", "Apply Edits", MessageBoxButton.OKCancel) == MessageBoxResult.OK)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                try
                {
                    DataView.ApplyEdits();
                    UpdateUndoRedoButtons();
                    Mouse.OverrideCursor = null;
                }
                catch (Exception ex)
                {
                    GenericControls.MessageBox.Show(ex.Message, "Error Saving Edits", MessageBoxButton.OK, MessageBoxImage.Exclamation);
                    Mouse.OverrideCursor = null;
                }
            }
        }

        /// <summary>
        /// Handles the OpenFC (Field Calculator) button click event.
        /// </summary>
        private void OpenFC_Click(object sender, RoutedEventArgs e)
        {
            var fc = new FieldCalculator(DataView, _selectedDataRowIndices, _readOnlyColumns);
            fc.ContentRendered += CalculatorRendered;

            if (fc.ShowDialog() == true)
            {
                _fieldCalculatorString = fc.ExpressionCalculator.GetExpressionText();
                UpdateVisibleRows();
                UpdateUndoRedoButtons();
                fc.ContentRendered -= CalculatorRendered;
            }
        }

        /// <summary>
        /// Reapplies the last undone edit and refreshes the visible table.
        /// </summary>
        private void RedoLastEdit()
        {
            DataView.RedoEdit();
            UpdateVisibleRows();
            UpdateUndoRedoButtons();
        }
        /// <summary>
        /// Reverts the last edit and refreshes the visible table.
        /// </summary>
        private void UndoLastEdit()
        {
            DataView.UndoEdit();
            UpdateVisibleRows();
            UpdateUndoRedoButtons();
        }

        #endregion

        #region Helper Methods

        /// <summary>
        /// Updates the displayed text for a visible cell.
        /// </summary>
        /// <param name="rowIndex">The visible row index.</param>
        /// <param name="columnIndex">The visible column index.</param>
        /// <param name="newText">The text to display.</param>
        private void SetCellText(int rowIndex, int columnIndex, string newText)
        {
            ((Cell)GridPanel.Children[rowIndex * DataView.ColumnNames.Count() + columnIndex]).Text = newText;
        }

        /// <summary>
        /// Updates the selection button states based on current selection.
        /// </summary>
        private void UpdateSelectionButtonStates()
        {
            if (_selectedDataRowIndices.Count > 0 && !_selectedRowsOnly)
            {
                ShowSelected.IsEnabled = true;
                DeSelectAll.IsEnabled = true;
            }
            else if (_selectedDataRowIndices.Count <= 0 && _selectedRowsOnly)
            {
                ShowAll_Checked(null, null);
            }
            else if (_selectedRowsOnly)
            {
                // Do nothing - keep current state
            }
            else
            {
                ShowSelected.IsEnabled = false;
                DeSelectAll.IsEnabled = false;
            }
        }

        /// <summary>
        /// Enables paste controls when editing is allowed and the clipboard has text.
        /// </summary>
        private void UpdatePasteButtonState()
        {
            if (!Editable)
            {
                PasteButton.IsEnabled = false;
                return;
            }
            Dispatcher.BeginInvoke(System.Windows.Threading.DispatcherPriority.Background, () =>
            {
                try
                {
                    PasteButton.IsEnabled = Clipboard.ContainsText();
                }
                catch
                {
                    PasteButton.IsEnabled = false;
                }
            });
        }

        /// <summary>
        /// Deletes the currently selected rows from the data view.
        /// </summary>
        /// <param name="sender">The menu item that raised the event.</param>
        /// <param name="e">The routed event data.</param>
        private void DeleteRows(object sender, RoutedEventArgs e)
        {
            DataView.DeleteRows(_selectedDataRowIndices.ToArray());
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Renders a table column header with optional sort indicators.
        /// </summary>
        private class ColumnHeader : Border
        {
            public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
                nameof(Text), typeof(string), typeof(ColumnHeader), new PropertyMetadata(""));

            public string Text
            {
                get => (string)GetValue(TextProperty);
                set => SetValue(TextProperty, value);
            }

            public static readonly DependencyProperty HeaderTextblockStyleProperty = DependencyProperty.Register(
                nameof(HeaderTextblockStyle), typeof(Style), typeof(ColumnHeader), new PropertyMetadata(GetDefaultColumnHeaderTextblockStyle()));

            public Style HeaderTextblockStyle
            {
                get => (Style)GetValue(HeaderTextblockStyleProperty);
                set => SetValue(HeaderTextblockStyleProperty, value);
            }

            public static readonly DependencyProperty HeaderBorderStyleProperty = DependencyProperty.Register(
                nameof(HeaderBorderStyle), typeof(Style), typeof(ColumnHeader), new PropertyMetadata(GetDefaultColumnHeaderBorderStyle()));

            public Style HeaderBorderStyle
            {
                get => (Style)GetValue(HeaderBorderStyleProperty);
                set => SetValue(HeaderBorderStyleProperty, value);
            }

            private readonly Viewbox _sortDownViewBox = new Viewbox { Width = 9, Visibility = Visibility.Collapsed, Margin = new Thickness(2, 0, 2, 0) };
            private readonly Viewbox _sortUpViewBox = new Viewbox { Width = 9, Visibility = Visibility.Collapsed, Margin = new Thickness(2, 0, 2, 0) };

            /// <summary>
            /// Initializes a column header for the specified data column.
            /// </summary>
            /// <param name="columnName">The displayed column name.</param>
            /// <param name="columnType">The column data type shown in the tooltip.</param>
            public ColumnHeader(string columnName, Type columnType)
            {
                var g = new Grid();
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto });
                g.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

                var downArrow = new Polygon { Points = new PointCollection { new Point(0, 0), new Point(8, 0), new Point(4, 6) } };
                downArrow.SetResourceReference(Shape.FillProperty, "DataGrid.Header.Foreground");
                _sortDownViewBox.Child = downArrow;
                g.Children.Add(_sortDownViewBox);

                var upArrow = new Polygon { Points = new PointCollection { new Point(4, 0), new Point(8, 6), new Point(0, 6) } };
                upArrow.SetResourceReference(Shape.FillProperty, "DataGrid.Header.Foreground");
                _sortUpViewBox.Child = upArrow;
                g.Children.Add(_sortUpViewBox);

                var tBlock = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center,
                    Background = Brushes.Transparent,
                    FontWeight = FontWeights.Bold,
                    TextTrimming = TextTrimming.CharacterEllipsis
                };
                tBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Text)) { Source = this });
                tBlock.SetBinding(TextBlock.StyleProperty, new Binding(nameof(HeaderTextblockStyle)) { Source = this });
                Grid.SetColumn(tBlock, 1);
                g.Children.Add(tBlock);

                SetBinding(Border.StyleProperty, new Binding(nameof(HeaderBorderStyle)) { Source = this });
                Child = g;
                Text = columnName;

                string typeName = columnType switch
                {
                    Type t when t == typeof(byte) => "Byte",
                    Type t when t == typeof(short) || t == typeof(ushort) => "Short Integer",
                    Type t when t == typeof(int) || t == typeof(uint) => "Integer",
                    Type t when t == typeof(long) || t == typeof(ulong) => "Long Integer",
                    Type t when t == typeof(float) => "Single",
                    Type t when t == typeof(double) => "Double",
                    Type t when t == typeof(string) => "String",
                    Type t when t == typeof(bool) => "Boolean",
                    _ => "Unknown"
                };
                ToolTip = $"{columnName}\nType: {typeName}";
            }

            /// <summary>
            /// Hides all sort indicator visuals.
            /// </summary>
            public void RemoveSorter()
            {
                _sortDownViewBox.Visibility = Visibility.Collapsed;
                _sortUpViewBox.Visibility = Visibility.Collapsed;
            }

            /// <summary>
            /// Shows the sort indicator for the current sort direction.
            /// </summary>
            /// <param name="ascending"><c>true</c> to show ascending; otherwise descending.</param>
            public void AddSorter(bool ascending)
            {
                _sortDownViewBox.Visibility = ascending ? Visibility.Collapsed : Visibility.Visible;
                _sortUpViewBox.Visibility = ascending ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        /// <summary>
        /// Renders a table cell with bindable text and styling.
        /// </summary>
        private class Cell : Border
        {
            public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
                nameof(Text), typeof(string), typeof(Cell), new PropertyMetadata(""));

            public string Text
            {
                get => (string)GetValue(TextProperty);
                set => SetValue(TextProperty, value);
            }

            public static readonly DependencyProperty CellStyleProperty = DependencyProperty.Register(
                nameof(CellStyle), typeof(Style), typeof(Cell), new PropertyMetadata(GetDefaultCellTextblockStyle()));

            public Style CellStyle
            {
                get => (Style)GetValue(CellStyleProperty);
                set => SetValue(CellStyleProperty, value);
            }

            public static readonly DependencyProperty ForegroundProperty = DependencyProperty.Register(
                nameof(Foreground), typeof(Brush), typeof(Cell), new PropertyMetadata(Brushes.Black));

            public Brush Foreground
            {
                get => (Brush)GetValue(ForegroundProperty);
                set => SetValue(ForegroundProperty, value);
            }

            /// <summary>
            /// Initializes a table cell visual.
            /// </summary>
            public Cell()
            {
                HorizontalAlignment = HorizontalAlignment.Stretch;
                VerticalAlignment = VerticalAlignment.Stretch;
                Background = Brushes.Transparent;
                IsHitTestVisible = false;

                var tBlock = new TextBlock();
                tBlock.SetBinding(TextBlock.StyleProperty, new Binding(nameof(CellStyle)) { Source = this });
                tBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Text)) { Source = this });
                tBlock.SetBinding(TextBlock.ForegroundProperty, new Binding(nameof(Foreground)) { Source = this });
                Child = tBlock;
            }
        }

        /// <summary>
        /// Renders a table row header with bindable text and styling.
        /// </summary>
        private class RowHeader : Border
        {
            public static readonly DependencyProperty TextProperty = DependencyProperty.Register(
                nameof(Text), typeof(string), typeof(RowHeader), new PropertyMetadata(""));

            public string Text
            {
                get => (string)GetValue(TextProperty);
                set => SetValue(TextProperty, value);
            }

            public static readonly DependencyProperty HeaderTextblockStyleProperty = DependencyProperty.Register(
                nameof(HeaderTextblockStyle), typeof(Style), typeof(RowHeader), new PropertyMetadata(GetDefaultRowHeaderTextblockStyle()));

            public Style HeaderTextblockStyle
            {
                get => (Style)GetValue(HeaderTextblockStyleProperty);
                set => SetValue(HeaderTextblockStyleProperty, value);
            }

            public static readonly DependencyProperty HeaderBorderStyleProperty = DependencyProperty.Register(
                nameof(HeaderBorderStyle), typeof(Style), typeof(RowHeader), new PropertyMetadata(GetDefaultRowHeaderBorderStyle()));

            public Style HeaderBorderStyle
            {
                get => (Style)GetValue(HeaderBorderStyleProperty);
                set => SetValue(HeaderBorderStyleProperty, value);
            }

            /// <summary>
            /// Initializes a row header visual.
            /// </summary>
            public RowHeader()
            {
                var tBlock = new TextBlock
                {
                    HorizontalAlignment = HorizontalAlignment.Left,
                    VerticalAlignment = VerticalAlignment.Center,
                    Margin = new Thickness(2, 0, 0, 0),
                    Background = Brushes.Transparent
                };
                tBlock.SetBinding(TextBlock.TextProperty, new Binding(nameof(Text)) { Source = this });
                tBlock.SetBinding(TextBlock.StyleProperty, new Binding(nameof(HeaderTextblockStyle)) { Source = this });
                SetBinding(Border.StyleProperty, new Binding(nameof(HeaderBorderStyle)) { Source = this });
                Child = tBlock;
            }
        }

        #endregion

        #region Default Styles

        /// <summary>
        /// Creates the default text style used by table cells.
        /// </summary>
        /// <returns>The default cell text style.</returns>
        private static Style GetDefaultCellTextblockStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            s.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Left));
            s.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            s.Setters.Add(new Setter(TextBlock.BackgroundProperty, Brushes.Transparent));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            s.Setters.Add(new Setter(TextBlock.IsHitTestVisibleProperty, false));
            s.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 2, 0)));
            return s;
        }

        /// <summary>
        /// Creates the default text style used by column headers.
        /// </summary>
        /// <returns>The default column-header text style.</returns>
        private static Style GetDefaultColumnHeaderTextblockStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextTrimmingProperty, TextTrimming.CharacterEllipsis));
            s.Setters.Add(new Setter(TextBlock.BackgroundProperty, Brushes.Transparent));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            s.Setters.Add(new Setter(TextBlock.FontWeightProperty, FontWeights.Bold));
            s.Setters.Add(new Setter(TextBlock.TextWrappingProperty, TextWrapping.WrapWithOverflow));
            s.Setters.Add(new Setter(TextBlock.IsHitTestVisibleProperty, false));
            return s;
        }

        /// <summary>
        /// Creates the default border style used by column headers.
        /// </summary>
        /// <returns>The default column-header border style.</returns>
        private static Style GetDefaultColumnHeaderBorderStyle()
        {
            var columnHeaderBackground = new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(SystemColors.ControlLightLightColor, 0.25), new GradientStop(Color.FromArgb(255, 237, 238, 243), 1) },
                new Point(0, 0), new Point(0, 1));

            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter(Border.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            s.Setters.Add(new Setter(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch));
            s.Setters.Add(new Setter(Border.BackgroundProperty, columnHeaderBackground));
            s.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(0.5)));
            s.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(204, 206, 219))));
            s.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
            s.Setters.Add(new Setter(Border.MarginProperty, new Thickness(-0.5)));
            s.Setters.Add(new Setter(Border.MinHeightProperty, 23.0));
            s.Setters.Add(new Setter(Border.SnapsToDevicePixelsProperty, true));

            var mouseOverTrigger = new Trigger { Property = IsMouseOverProperty, Value = true };
            mouseOverTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(201, 222, 245))));
            s.Triggers.Add(mouseOverTrigger);

            var keyboardFocusTrigger = new Trigger { Property = IsKeyboardFocusWithinProperty, Value = true };
            keyboardFocusTrigger.Setters.Add(new Setter(Border.BackgroundProperty, new SolidColorBrush(Color.FromRgb(201, 222, 245))));
            s.Triggers.Add(keyboardFocusTrigger);

            return s;
        }

        /// <summary>
        /// Creates the default text style used by row headers.
        /// </summary>
        /// <returns>The default row-header text style.</returns>
        private static Style GetDefaultRowHeaderTextblockStyle()
        {
            var s = new Style(typeof(TextBlock));
            s.Setters.Add(new Setter(TextBlock.HorizontalAlignmentProperty, HorizontalAlignment.Left));
            s.Setters.Add(new Setter(TextBlock.VerticalAlignmentProperty, VerticalAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.TextAlignmentProperty, TextAlignment.Center));
            s.Setters.Add(new Setter(TextBlock.BackgroundProperty, Brushes.Transparent));
            s.Setters.Add(new Setter(TextBlock.ForegroundProperty, Brushes.Black));
            s.Setters.Add(new Setter(TextBlock.MarginProperty, new Thickness(2, 0, 0, 0)));
            s.Setters.Add(new Setter(TextBlock.IsHitTestVisibleProperty, false));
            return s;
        }

        /// <summary>
        /// Creates the default border style used by row headers.
        /// </summary>
        /// <returns>The default row-header border style.</returns>
        private static Style GetDefaultRowHeaderBorderStyle()
        {
            var rowHeaderBackground = new LinearGradientBrush(
                new GradientStopCollection { new GradientStop(SystemColors.ControlLightLightColor, 0.25), new GradientStop(Color.FromArgb(255, 237, 238, 243), 1) },
                new Point(0, 0), new Point(1, 0));

            var s = new Style(typeof(Border));
            s.Setters.Add(new Setter(Border.HorizontalAlignmentProperty, HorizontalAlignment.Stretch));
            s.Setters.Add(new Setter(Border.VerticalAlignmentProperty, VerticalAlignment.Stretch));
            s.Setters.Add(new Setter(Border.BackgroundProperty, rowHeaderBackground));
            s.Setters.Add(new Setter(Border.CornerRadiusProperty, new CornerRadius(0)));
            s.Setters.Add(new Setter(Border.BorderBrushProperty, new SolidColorBrush(Color.FromRgb(204, 206, 219))));
            s.Setters.Add(new Setter(Border.BorderThicknessProperty, new Thickness(1)));
            s.Setters.Add(new Setter(Border.MarginProperty, new Thickness(0, -0.5, 0, -0.5)));
            s.Setters.Add(new Setter(Border.SnapsToDevicePixelsProperty, true));
            return s;
        }

        #endregion
    }
}
