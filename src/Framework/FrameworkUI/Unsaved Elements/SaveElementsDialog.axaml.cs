using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Interactivity;
using GenericControls;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for SaveElementsDialog.xaml
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// XGoodFit port: the dependency property is a styled property, whose CLR setter fills the grid as
    /// upstream's does. The grid stands in for a plain WPF DataGrid (<see cref="PlainDataGrid"/>).
    /// </para>
    /// </remarks>
    public partial class SaveElementsDialog : MetroWindow
    {
        /// <summary>
        /// Construct a new save elements dialog.
        /// </summary>
        public SaveElementsDialog()
        {
            // This call is required by the designer.
            InitializeComponent();
            // XGoodFit port: upstream's plain DataGrid has no context menus.
            PlainDataGrid.SuppressContextMenus(MyDataGrid);
        }

        /// <summary>
        /// Dependency property for the save elements list.
        /// </summary>
        public static readonly StyledProperty<ObservableCollection<UnsavedElement>?> ElementItemsProperty = AvaloniaProperty.Register<SaveElementsDialog, ObservableCollection<UnsavedElement>?>(nameof(ElementItems), null);

        /// <summary>
        /// Gets or sets the save elements list.
        /// </summary>
        public ObservableCollection<UnsavedElement> ElementItems
        {
            get { return (ObservableCollection<UnsavedElement>)GetValue(ElementItemsProperty)!; }
            set
            {
                SetValue(ElementItemsProperty, value);
                if (MyDataGrid != null)
                {
                    MyDataGrid.ItemsSource = null!;
                    MyDataGrid.ItemsSource = ElementItems;
                }
            }
        }

        /// <summary>
        /// Enumeration of dialog result types.
        /// </summary>
        public enum DialogResultType
        {
            /// <summary>The user chose to save the elements before the pending action proceeds.</summary>
            YesSave,
            /// <summary>The user chose to skip saving and let the pending action proceed.</summary>
            NoSave,
            /// <summary>The user cancelled the pending action; no save occurred.</summary>
            Cancel
        }

        /// <summary>
        /// Gets the save elements dialog result.
        /// </summary>
        public DialogResultType Result { get; private set; } = DialogResultType.Cancel;

        /// <summary>
        /// On Save, save all elements, set dialog result to true and close.
        /// </summary>
        private void Btn_Save_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Save all elements
                for (int i = 0; i < ElementItems.Count; i++)
                    ElementItems[i].Element.Save();
                Result = DialogResultType.YesSave;
            }
            catch (Exception ex)
            {
                Result = DialogResultType.Cancel;
                GenericControls.MessageBox.Show(ex.ToString());
            }
            Close();
        }

        /// <summary>
        /// On Don't Save, set dialog result to true and close. Do not save.
        /// </summary>
        private void Btn_DontSave_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Instead of saving, restore the last saved inputs from disk.
                for (int i = 0; i < ElementItems.Count; i++)
                    ElementItems[i].Element.Open();
                Result = DialogResultType.NoSave;
            }
            catch (Exception ex)
            {
                Result = DialogResultType.Cancel;
                GenericControls.MessageBox.Show(ex.ToString());
            }
            Close();
        }

        /// <summary>
        /// On Cancel, set dialog result to false and close.
        /// </summary>
        private void Btn_Cancel_Click(object sender, RoutedEventArgs e)
        {
            Result = DialogResultType.Cancel;
            Close();
        }

    }
}
