using System.Collections;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;

namespace GenericControls
{
    /// <summary>
    /// A custom control for displaying and editing a list of strings using a DataGrid.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
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
    /// XGoodFit port: the properties are Avalonia styled properties, with upstream's callbacks. The
    /// grid's column has no XAML name, which Avalonia can't give a column, so <c>StringColumn</c> is the
    /// grid's first column. Cells are selected with ProDataGrid's cell infos, which also take the
    /// row and column indexes.
    /// </para>
    /// </remarks>
    public partial class StringListPropertyControl:UserControl
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="StringListPropertyControl"/> class.
        /// </summary>
        public StringListPropertyControl()
        {
            InitializeComponent();
            if (StringList == null)
            {
                StringList = new List<string>();
            }
        }

        /// <summary>
        /// Backing dependency property for <see cref="StringList"/>.
        /// </summary>
        // Default null — reference-type DP defaults are shared across all instances.
        public static readonly StyledProperty<IList<string>> StringListProperty = AvaloniaProperty.Register<StringListPropertyControl, IList<string>>(nameof(StringList), null);
        /// <summary>
        /// Gets/sets the list of strings displayed in the control.
        /// </summary>
        public IList<string> StringList
        {
            get
            {
                return (IList<string>)this.GetValue(StringListProperty);
            }
            set
            {
                this.SetValue(StringListProperty, value);
            }
        }
        private IList<object> _internalList;

        /// <summary>
        /// Backing dependency property for <see cref="Title"/>.
        /// </summary>
        public static readonly StyledProperty<string> TitleProperty = AvaloniaProperty.Register<StringListPropertyControl, string>(nameof(Title), "Title");

        /// <summary>
        /// Gets or sets the title displayed alongside the string list.
        /// </summary>
        public string Title
        {
            get
            {
                return (string)this.GetValue(TitleProperty);
            }
            set
            {
                this.SetValue(TitleProperty, value);
            }
        }

        static StringListPropertyControl()
        {
            // XGoodFit port: the property-changed callbacks of upstream's metadata.
            StringListProperty.Changed.AddClassHandler<StringListPropertyControl>(StringListPropertyChanged_Callback);
            AddRemoveEnabledProperty.Changed.AddClassHandler<StringListPropertyControl>(AddRemoveEnabledChanged_Callback);
        }

        /// <summary>
        /// The grid's string column (upstream's <c>StringColumn</c>, named in XAML).
        /// </summary>
        private DataGridColumn StringColumn => StringListDataGrid.Columns[0];

        /// <summary>
        /// Handles updates when the <see cref="StringList"/> property changes.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void StringListPropertyChanged_Callback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(StringListPropertyControl))
                return;
            StringListPropertyControl thisControl = (StringListPropertyControl)d;
            // 
            if (thisControl.StringListDataGrid == null)
                return;
            thisControl.StringListDataGrid.ItemsSource = (IEnumerable)null;
            if (e.NewValue == null)
                thisControl.StringList = new List<string>(); // Exit Sub
            thisControl._internalList = new List<object>(thisControl.StringList.Count);
            for (int i = 0, loopTo = thisControl.StringList.Count - 1; i <= loopTo; i++)
                thisControl._internalList.Add(new StringContainer(thisControl.StringList[i]));
            thisControl.StringListDataGrid.ItemsSource = thisControl._internalList;
        }

        /// <summary>
        /// Backing dependency property for <see cref="AddRemoveEnabled"/>.
        /// </summary>
        public static readonly StyledProperty<bool> AddRemoveEnabledProperty = AvaloniaProperty.Register<StringListPropertyControl, bool>(nameof(AddRemoveEnabled), true);

        /// <summary>
        /// Gets or sets whether add/remove buttons are enabled on the control.
        /// </summary>
        public bool AddRemoveEnabled
        {
            get
            {
                return (bool)this.GetValue(AddRemoveEnabledProperty);
            }
            set
            {
                this.SetValue(AddRemoveEnabledProperty, value);
            }
        }

        /// <summary>
        /// Callback to enable or disable the add/remove functionality in the DataGrid.
        /// </summary>
        /// <param name="d">The dependency object that changed.</param>
        /// <param name="e">Event arguments containing the old and new values.</param>
        private static void AddRemoveEnabledChanged_Callback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(StringListPropertyControl))
                return;
            StringListPropertyControl thisControl = (StringListPropertyControl)d;
            thisControl.StringListDataGrid.CanUserAddInsertDeleteRows = (bool)e.NewValue;
        }

        /// <summary>
        /// Selects all string cells in the DataGrid when the text block is clicked.
        /// </summary>
        /// <param name="sender">The text block that was clicked.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void TextBlock_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            this.StringListDataGrid.SelectedCells.Clear();
            int row = 0;
            int column = this.StringListDataGrid.Columns.IndexOf(this.StringColumn);
            foreach (var item in this.StringListDataGrid.Items)
                this.StringListDataGrid.SelectedCells.Add(new DataGridCellInfo(item, this.StringColumn, row++, column));
        }

        /// <summary>
        /// Extracts the current string values from the DataGrid into a list.
        /// </summary>
        /// <returns>A list of strings representing the current DataGrid content.</returns>
        private IList<string> GetStringList()
        {
            var result = new List<string>();
            for (int i = 0, loopTo = this.StringListDataGrid.Items.Count - 1; i <= loopTo; i++)
            {
                var c = this.StringListDataGrid.GetCell(i, 0);
                if (c == null)
                {
                    result.Add("");
                }
                // XGoodFit port: ProDataGrid shows a text cell in a TextBlock subclass
                // (DataGridSearchTextBlock), so the editor and text block are matched with "is", where
                // upstream compares exact types.
                else if (c.Content is TextBox)
                {
                    result.Add(((TextBox)c.Content).Text);
                }
                else if (c.Content is TextBlock)
                {
                    result.Add(((TextBlock)c.Content).Text);
                }
                else
                {
                    result.Add(c.Content.ToString());
                }
            }
            return result;
        }

        /// <summary>
        /// Updates the string list when cell editing ends.
        /// </summary>
        /// <param name="sender">The DataGrid control.</param>
        /// <param name="e">Cell edit ending event arguments.</param>
        private void StringListDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            // XGoodFit port: the list is read now, as upstream reads it (from the editor, so an edit
            // cancelled with Escape is kept, as in upstream), but set once the edit has ended. Setting
            // it resets the grid's items, and ProDataGrid commits an edit in progress when its items
            // change, which raises this event again, without end; WPF's grid doesn't.
            IList<string> list = GetStringList();
            Dispatcher.UIThread.Post(() => StringList = list);
        }

        /// <summary>
        /// Updates the string list when rows are added.
        /// </summary>
        /// <param name="startrow">The index of the first added row.</param>
        /// <param name="numrows">The number of rows added.</param>
        private void StringListDataGrid_RowsAdded(int startrow, int numrows)
        {
            StringList = GetStringList();
        }

        /// <summary>
        /// Updates the string list when data are pasted.
        /// </summary>
        private void StringListDataGrid_DataPasted()
        {
            StringList = GetStringList();
        }

        /// <summary>
        /// Internal container class used to wrap strings in the DataGrid.
        /// </summary>
        private class StringContainer
        {
            /// <summary>
            /// The string value.
            /// </summary>
            public string TheString { get; set; }

            /// <summary>
            /// Initializes a new instance of <see cref="StringContainer"/> with an empty string.
            /// </summary>
            public StringContainer()
            {
                TheString = "";
            }

            /// <summary>
            /// Initializes a new instance of <see cref="StringContainer"/> with a specific string.
            /// </summary>
            /// <param name="newString">The string value to initialize with.</param>
            public StringContainer(string newString)
            {
                TheString = newString;
            }
        }
    }
}