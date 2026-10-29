using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using FrameworkInterfaces;
using Xceed.Wpf.AvalonDock.Layout;

namespace FrameworkUI
{
    /// <summary>
    /// A class for displaying open windows in a menu.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// </remarks>
    public class OpenWindows : Separator
    {
        /// <summary>
        /// Construct the OpenWindows class
        /// </summary>
        public OpenWindows()
        {
            Loaded += ConnectToMenu;
        }

        /// <summary>
        /// Get the observable collection of open windows.
        /// </summary>
        public ObservableCollection<OpenWindowItem> Collection { get; private set; } = new ObservableCollection<OpenWindowItem>();

        /// <summary>
        /// Gets or sets the parent window menu.
        /// </summary>
        public MenuItem? WindowMenu { get; set; }

        /// <summary>
        /// Gets or sets the number of open windows to show in the menu.
        /// </summary>
        public int NumberOfWindowsToDisplay { get; set; }

        /// <summary>
        /// When closing all windows, suppress the on closing action for individual windows.
        /// </summary>
        private bool ClosingAllWindows;

        /// <summary>
        /// Determines if the closing of windows has been canceled.
        /// </summary>
        public bool CancelClosing { get; private set; } = false;

        /// <summary>
        /// Gets the index of the open window item containing the specified document.
        /// </summary>
        /// <param name="document">AvalonDock document.</param>
        public int WindowIndexOf(LayoutDocument document)
        {
            for (int i = 0; i < Collection.Count; i++)
            {
                var doc = Collection[i].Document;
                if (doc != null && doc.Equals(document)) return i;
            }
            return -1;
        }

        /// <summary>
        /// Gets the index of the open window item containing the specified element.
        /// </summary>
        /// <param name="element">Element bound to document.</param>
        public int WindowIndexOf(IElement element)
        {
            for (int i = 0; i < Collection.Count; i++)
            {
                var collectionElement = Collection[i].Element;
                if (collectionElement == null) continue;
                if (collectionElement.ParentCollection.Name == element.ParentCollection.Name && collectionElement.Name == element.Name)
                {
                    return i;
                }
            }
            return -1;
        }

        /// <summary>
        /// Open and new window.
        /// </summary>
        /// <param name="document">AvalonDock document.</param>
        /// <param name="element">Element that is bound to the document.</param>
        public void Open(LayoutDocument document, IElement element)
        {
            // Add document to observable collection
            document.Closing += Document_Closing;
            document.Closed += Document_Closed;
            Collection.Add(new OpenWindowItem(document, element) { MenuItem = new MenuItem() });
        }

        /// <summary>
        /// On closing, determine if the document needs to be saved.
        /// </summary>
        private void Document_Closing(object? sender, CancelEventArgs e)
        {
            CancelClosing = false;
            if (ClosingAllWindows == false && ShellPublicVariables.SimulationInProgress == false)
            {
                if (sender is not LayoutDocument senderDocument) return;
                int index = WindowIndexOf(senderDocument);
                // Check if the document needs to be saved
                var element = index >= 0 ? Collection[index].Element : null;
                if (index >= 0 && element != null && element.IsDirty == true)
                {
                    // Open the Save Window Dialog
                    var unSavedElementItems = new ObservableCollection<UnsavedElement>();
                    unSavedElementItems.Add(new UnsavedElement(element));
                    var saveElementsDialog = new SaveElementsDialog() { ElementItems = unSavedElementItems };
                    saveElementsDialog.ShowDialog();
                    if (saveElementsDialog.Result == SaveElementsDialog.DialogResultType.Cancel)
                    {
                        // If the user clicks Cancel or closes the dialog, then cancel closing
                        e.Cancel = true;
                        CancelClosing = true;
                    }
                }
            }
        }

        /// <summary>
        /// After the document is closed, remove document from Window menu and observable collection.
        /// </summary>
        private void Document_Closed(object? sender, EventArgs e)
        {
            if (sender is not LayoutDocument senderDocument) return;
            int index = WindowIndexOf(senderDocument);
            if (index < 0) return;
            if (index <= NumberOfWindowsToDisplay - 1)
            {
                WindowMenu?.Items.Remove(Collection[index].MenuItem);
            }
            var document = Collection[index].Document;
            if (document != null)
            {
                document.Closing -= Document_Closing;
                document.Closed -= Document_Closed;
            }
            Collection.RemoveAt(index);
        }

        /// <summary>
        /// Active the open window.
        /// </summary>
        /// <param name="index">Zero-based index of the element to activate.</param>
        public void Activate(int index)
        {
            if (index < 0) return;
            if (index > Collection.Count - 1) return;
            var document = Collection[index].Document;
            if (document != null) document.IsActive = true;
        }

        /// <summary>
        /// Close an open window.
        /// </summary>
        /// <param name="index">Zero-based index of the element to close.</param>
        public void Close(int index)
        {
            Collection[index].Document?.Close();
        }

        /// <summary>
        /// Close all open windows.
        /// </summary>
        public void CloseAllWindows()
        {
            // Check if any open windows need to be saved
            CheckIfWindowsNeedToBeSaved();
            if (CancelClosing == true) return;
            // Close all windows
            ClosingAllWindows = true;
            for (int i = Collection.Count - 1; i >= 0; i -= 1)
                Close(i);
            ClosingAllWindows = false;
        }

        /// <summary>
        /// Enable all open windows.
        /// </summary>
        public void EnableAllWindows()
        {
            for (int i = 0; i < Collection.Count; i++)
            {
                var document = Collection[i].Document;
                if (document != null)
                {
                    document.IsEnabled = true;
                    if (document.Content is Control control)
                    {
                        control.IsEnabled = true;
                    }
                }
            }
        }

        /// <summary>
        /// Disable all open windows.
        /// </summary>
        public void DisableAllWindows()
        {
            for (int i = 0; i < Collection.Count; i++)
            {
                var document = Collection[i].Document;
                if (document != null)
                {
                    document.IsEnabled = false;
                    if (document.Content is Control control)
                    {
                        control.IsEnabled = false;
                    }
                }
            }
        }

        /// <summary>
        /// Check if there are any open windows that need to be saved.
        /// If true, a dialog will open asking the user if they would like to save.
        /// </summary>
        public void CheckIfWindowsNeedToBeSaved()
        {
            CancelClosing = false;
            // Check if there any unsaved open documents
            var unSavedElementItems = new ObservableCollection<UnsavedElement>();
            for (int i = 0; i < Collection.Count; i++)
            {
                var element = Collection[i].Element;
                if (element == null) continue;
                if (element.IsDirty == true)
                    unSavedElementItems.Add(new UnsavedElement(element));
            }
            // If there is, open the Save Window Dialog
            if (unSavedElementItems.Count > 0)
            {
                var saveElementsDialog = new SaveElementsDialog() { ElementItems = unSavedElementItems };
                saveElementsDialog.ShowDialog();
                if (saveElementsDialog.Result == SaveElementsDialog.DialogResultType.Cancel)
                {
                    // If the user clicks Cancel or closes the dialog, then cancel closing
                    CancelClosing = true;
                }
            }
        }

        /// <summary>
        /// Connect to the parent window menu item.
        /// </summary>
        private void ConnectToMenu(object sender, RoutedEventArgs e)
        {
            MenuItem? parentItem = Parent as MenuItem;
            if (parentItem == null) throw new ApplicationException("Parent must be a MenuItem");
            if (WindowMenu != null && WindowMenu.Equals(parentItem)) return;
            if (WindowMenu != null) WindowMenu.SubmenuOpened -= WindowMenu_SubMenuOpened;
            WindowMenu = parentItem;
            WindowMenu.SubmenuOpened += WindowMenu_SubMenuOpened;
        }

        /// <summary>
        /// When the parent window menu item is opened, build the list of open windows.
        /// </summary>
        private void WindowMenu_SubMenuOpened(object sender, RoutedEventArgs e)
        {
            RemoveMenuItems();
            LoadMenuItems();
        }

        /// <summary>
        /// Remove all open window menu items.
        /// </summary>
        private void RemoveMenuItems()
        {
            if (WindowMenu == null) return;
            // clear the list of menu items
            for (int i = Collection.Count - 1; i >= 0; i -= 1)
            {
                if (WindowMenu.Items.Contains(Collection[i].MenuItem) == true)
                {
                    WindowMenu.Items.Remove(Collection[i].MenuItem);
                }
            }
        }

        /// <summary>
        /// Load all open window menu items.
        /// </summary>
        private void LoadMenuItems()
        {
            if (WindowMenu == null) return;
            int iMenuItem = WindowMenu.Items.IndexOf(this);
            for (int i = 0; i < Collection.Count; i++)
            {
                if (i <= NumberOfWindowsToDisplay - 1)
                {
                    string header = i + 1 + " " + Collection[i].Name;
                    var menuItem = new MenuItem() { Header = header };
                    Collection[i].MenuItem = menuItem;
                    var document = Collection[i].Document;
                    if (document != null)
                    {
                        menuItem.Icon = new Image() { Source = document.IconSource };
                        if (document.IsSelected == true) menuItem.IsChecked = true;
                    }
                    menuItem.Click += MenuItem_Click;
                    // add menu item
                    iMenuItem += 1;
                    WindowMenu.Items.Insert(iMenuItem, menuItem);
                }
            }
        }

        /// <summary>
        /// On click, select document.
        /// </summary>
        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            if (WindowMenu == null) return;
            int iMenuItem = WindowMenu.Items.IndexOf(this);
            int iSender = WindowMenu.Items.IndexOf((MenuItem)sender);
            int iItem = iSender - iMenuItem - 1;
            if (iItem >= 0 && iItem < Collection.Count)
            {
                var document = Collection[iItem].Document;
                if (document != null) document.IsSelected = true;
            }
        }

    }
}
