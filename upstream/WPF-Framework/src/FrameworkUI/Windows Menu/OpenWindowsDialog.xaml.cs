using System.Windows;
using System.Windows.Controls;
using GenericControls;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for OpenWindowsDialog.xaml
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public partial class OpenWindowsDialog : MetroWindow
    {
        /// <summary>
        /// Construct a new open windows dialog.
        /// </summary>
        public OpenWindowsDialog()
        {
            // This call is required by the designer.
            InitializeComponent();
        }

        /// <summary>
        /// Dependency property for the open windows list.
        /// </summary>
        public static DependencyProperty WindowsProperty = DependencyProperty.Register(nameof(Windows), typeof(OpenWindows), typeof(OpenWindowsDialog), new UIPropertyMetadata(null));

        /// <summary>
        /// Gets or sets the open windows list.
        /// </summary>
        public OpenWindows Windows
        {
            get { return (OpenWindows)GetValue(WindowsProperty); }
            set
            {
                SetValue(WindowsProperty, value);
                if (MyDataGrid != null)
                {
                    MyDataGrid.ItemsSource = null;
                    MyDataGrid.ItemsSource = Windows?.Collection;
                }
            }
        }

        /// <summary>
        /// Activate selected window.
        /// </summary>
        private void ActivateButton_Click(object sender, RoutedEventArgs e)
        {
            if (MyDataGrid.SelectedItem is OpenWindowItem item && item.Document != null)
            {
                item.Document.IsActive = true;
            }
        }

        /// <summary>
        /// Save selected windows.
        /// </summary>
        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = 0; i < MyDataGrid.SelectedItems.Count; i++)
            {
                if (MyDataGrid.SelectedItems[i] is OpenWindowItem item)
                    item.Element?.Save();
            }
        }

        /// <summary>
        /// Close selected windows.
        /// </summary>
        private void CloseWindowsButton_Click(object sender, RoutedEventArgs e)
        {
            for (int i = MyDataGrid.SelectedItems.Count - 1; i >= 0; i -= 1)
            {
                if (MyDataGrid.SelectedItems[i] is OpenWindowItem item && item.Document != null)
                {
                    int index = Windows.WindowIndexOf(item.Document);
                    Windows.Close(index);
                }
            }
        }

        /// <summary>
        /// When row is selected, update buttons.
        /// </summary>
        private void MyDataGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (MyDataGrid.SelectedItems.Count <= 0)
            {
                ActivateButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                CloseWindowsButton.IsEnabled = false;
            }
            else if (MyDataGrid.SelectedItems.Count == 1)
            {
                ActivateButton.IsEnabled = true;
                SaveButton.IsEnabled = false;
                CloseWindowsButton.IsEnabled = true;
                if (MyDataGrid.SelectedItem is OpenWindowItem selectedItem && selectedItem.Element?.IsDirty == true)
                {
                    SaveButton.IsEnabled = true;
                }
            }
            else if (MyDataGrid.SelectedItems.Count > 1)
            {
                ActivateButton.IsEnabled = false;
                SaveButton.IsEnabled = false;
                CloseWindowsButton.IsEnabled = true;
                for (int i = 0; i < MyDataGrid.SelectedItems.Count; i++)
                {
                    if (MyDataGrid.SelectedItems[i] is OpenWindowItem item && item.Element?.IsDirty == true)
                    {
                        SaveButton.IsEnabled = true;
                        break;
                    }
                }
            }
        }

    }
}
