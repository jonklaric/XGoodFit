using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using GenericControls;
using WpfCompat;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for RecentFilesDialog.xaml
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// XGoodFit port: the dependency properties are styled properties.
    /// </para>
    /// </remarks>
    public partial class RecentFilesDialog : MetroWindow
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="RecentFilesDialog"/> class.
        /// </summary>
        public RecentFilesDialog()
        {
            // This call is required by the designer.
            InitializeComponent();
            // XGoodFit port: upstream's plain DataGrid has no context menus.
            PlainDataGrid.SuppressContextMenus(MyDataGrid);
        }

        /// <summary>
        /// Dependency property for the recent files list.
        /// </summary>
        public static readonly StyledProperty<RecentFiles?> FilesProperty = AvaloniaProperty.Register<RecentFilesDialog, RecentFiles?>(nameof(Files), null);

        /// <summary>
        /// Gets or sets the recent files list.
        /// </summary>
        public RecentFiles Files
        {
            get { return (RecentFiles)GetValue(FilesProperty)!; }
            set { SetValue(FilesProperty, value); }
        }

        /// <summary>
        /// Dependency property for the project file image. 
        /// </summary>
        public static readonly StyledProperty<ImageSource?> FileImageProperty = AvaloniaProperty.Register<RecentFilesDialog, ImageSource?>(nameof(FileImage), null);

        /// <summary>
        /// The project file image. 
        /// </summary>
        public ImageSource FileImage
        {
            get { return (ImageSource)GetValue(FileImageProperty)!; }
            set { SetValue(FileImageProperty, value); }
        }

        /// <summary>
        /// Open the clicked recent file.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void OpenButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyDataGrid.SelectedItem == null || Files == null) return;
            Files.OpenItem(((RecentFileItem)MyDataGrid.SelectedItem).FilePath);
            Close();
        }

        /// <summary>
        /// Clear all recent files.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void ClearListButton_Click(object sender, RoutedEventArgs e)
        {
            if (Files == null) return;
            if (GenericControls.MessageBox.Show("Are you sure you want to clear the Recent Files List? This action is permanent.", "Clear the Recent Files List?", MessageBoxButton.YesNoCancel, MessageBoxImage.Question) == MessageBoxResult.Yes)
            {
                Files.ClearAll();
                Close();
            }
        }

        /// <summary>
        /// When the selection is changed, update open button.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event arguments.</param>
        private void MyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            OpenButton.IsEnabled = MyDataGrid.SelectedItem != null;
        }

    }
}
