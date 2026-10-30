using Avalonia;
using Avalonia.Controls;
using WpfCompat;

namespace GenericControls
{

    /// <summary>
    /// Provides a WPF-compatible folder browser dialog for selecting directories.
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
    /// <item><description>
    ///     Josip Medved (original implementation)
    /// </description></item>
    /// </list>
    /// </para>
    /// <para>
    /// Originally created in 2-02-2012 by Josip Medved. Modified for WPF compatibility by Haden Smith in July 2019.
    /// June 2020 - Added support for Window rather than IWin32Window.
    /// </para>
    /// <para>
    /// XGoodFit port: upstream shows the Windows shell's folder dialog through COM (falling back to the
    /// legacy one). The port shows each platform's folder dialog through WpfCompat's
    /// <see cref="OpenFolderDialog"/>, which waits for the user as upstream's does. That dialog has one
    /// start folder: <see cref="InitialDirectory"/>, or else <see cref="DefaultDirectory"/> (upstream's
    /// shell dialog preferred the folder it last showed to the default).
    /// </para>
    /// </remarks>
    public class FolderBrowser
    {

        #region Members

        /// <summary>
        /// Gets/sets title of dialog.
        /// </summary>
        public string Title { get; set; }

        /// <summary>
        /// Gets/sets folder in which dialog will be open.
        /// </summary>
        public string InitialDirectory { get; set; }

        /// <summary>
        /// Gets/sets directory in which dialog will be open if there is no recent directory available.
        /// </summary>
        public string DefaultDirectory { get; set; }

        /// <summary>
        /// Gets selected folder.
        /// </summary>
        public string SelectedFolder { get; set; }

        #endregion

        /// <summary>
        /// Show folder browser dialog.
        /// </summary>
        /// <param name="owner">The owner of the dialog box.</param>
        /// <returns>True if a folder was selected; otherwise, false.</returns>
        public bool ShowDialog(Window owner = null)
        {
            // As upstream's: without an owner, the dialog belongs to the main window.
            if (owner is null && Application.Current is not null && Application.Current.MainWindow is not null)
            {
                owner = Application.Current.MainWindow;
            }

            var dialog = new OpenFolderDialog
            {
                Title = Title,
                InitialDirectory = InitialDirectory ?? DefaultDirectory,
            };
            if (dialog.ShowDialog(owner) == true)
            {
                SelectedFolder = dialog.FolderName;
                return true;
            }
            return false;
        }
    }
}
