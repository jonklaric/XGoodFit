using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

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


        #region WPF32Window Wrapper Class

        /// <summary>
        /// Helper wrapper class to convert a WPF window to a IWin32Window.
        /// </summary>
        private class Wpf32Window : IWin32Window
        {
            /// <summary>
            /// Gets the window handle.
            /// </summary>
            public IntPtr Handle { get; private set; }

            /// <summary>
            /// Initializes a new instance of the <see cref="Wpf32Window"/> class.
            /// </summary>
            /// <param name="wpfWindow">The WPF window to wrap.</param>
            public Wpf32Window(Window wpfWindow)
            {
                if (wpfWindow is null)
                    wpfWindow = GetDefaultOwnerWindow();
                Handle = new WindowInteropHelper(wpfWindow).Handle;
            }

            /// <summary>
            /// Gets the default owner window from the current application.
            /// </summary>
            /// <returns>The application's main window, or null if none exists.</returns>
            private Window GetDefaultOwnerWindow()
            {
                Window defaultWindow = null;
                if (Application.Current is not null && Application.Current.MainWindow is not null)
                {
                    defaultWindow = Application.Current.MainWindow;
                }
                return defaultWindow;
            }
        }

        #endregion

        #endregion

        /// <summary>
        /// Show folder browser dialog.
        /// </summary>
        /// <param name="owner">The owner of the dialog box.</param>
        /// <returns>True if a folder was selected; otherwise, false.</returns>
        public bool ShowDialog(Window owner = null)
        {
            var w32owner = new Wpf32Window(owner);
            if (Environment.OSVersion.Version.Major >= 6)
            {
                try
                {
                    return ShowVistaDialog(w32owner);
                }
                catch (Exception ex) when (ex is InvalidCastException || ex is System.Runtime.InteropServices.COMException)
                {
                    return ShowLegacyDialog(w32owner);
                }
            }
            else
            {
                return ShowLegacyDialog(w32owner);
            }
        }

        /// <summary>
        /// If OS is newer than Vista, then show dialog.
        /// </summary>
        /// <param name="owner">The owner that opened the dialog as a IWin32Window.</param>
        /// <returns>True if a folder was selected; otherwise, false.</returns>
        private bool ShowVistaDialog(IWin32Window owner)
        {
            NativeMethods.IFileDialog frm = (NativeMethods.IFileDialog)new NativeMethods.FileOpenDialogRCW();
            NativeMethods.IShellItem initialFolderShellItem = default;
            NativeMethods.IShellItem defaultFolderShellItem = default;
            NativeMethods.IShellItem resultShellItem = default;

            try
            {
                var options = default(uint);
                frm.GetOptions(ref options);
                options = options | NativeMethods.FOS_PICKFOLDERS | NativeMethods.FOS_FORCEFILESYSTEM | NativeMethods.FOS_NOVALIDATE | NativeMethods.FOS_NOTESTFILECREATE | NativeMethods.FOS_DONTADDTORECENT;
                frm.SetOptions(options);
                if (Title is not null)
                {
                    frm.SetTitle(Title);
                }
                if (InitialDirectory is not null)
                {
                    var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                    if (NativeMethods.SHCreateItemFromParsingName(InitialDirectory, IntPtr.Zero, ref riid, ref initialFolderShellItem) == NativeMethods.S_OK)
                    {
                        frm.SetFolder(initialFolderShellItem);
                    }
                }
                if (DefaultDirectory is not null)
                {
                    var riid = new Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE");
                    if (NativeMethods.SHCreateItemFromParsingName(DefaultDirectory, IntPtr.Zero, ref riid, ref defaultFolderShellItem) == NativeMethods.S_OK)
                    {
                        frm.SetDefaultFolder(defaultFolderShellItem);
                    }
                }

                if (frm.Show(owner.Handle) == NativeMethods.S_OK)
                {
                    if (frm.GetResult(ref resultShellItem) == NativeMethods.S_OK)
                    {
                        IntPtr pszString = IntPtr.Zero;
                        if (resultShellItem.GetDisplayName(NativeMethods.SIGDN_FILESYSPATH, ref pszString) == NativeMethods.S_OK)
                        {
                            if (pszString != IntPtr.Zero)
                            {
                                try
                                {
                                    SelectedFolder = Marshal.PtrToStringAuto(pszString);
                                    return true;
                                }
                                finally
                                {
                                    Marshal.FreeCoTaskMem(pszString);
                                }
                            }
                        }
                    }
                }
                return false;
            }
            finally
            {
                // Release COM-callable wrappers explicitly. Without this the dialog's COM
                // objects stay alive until the next GC, accumulating handles under repeated
                // folder-pick usage (e.g. from a property editor).
                if (resultShellItem != null) Marshal.ReleaseComObject(resultShellItem);
                if (defaultFolderShellItem != null) Marshal.ReleaseComObject(defaultFolderShellItem);
                if (initialFolderShellItem != null) Marshal.ReleaseComObject(initialFolderShellItem);
                if (frm != null) Marshal.ReleaseComObject(frm);
            }
        }

        /// <summary>
        /// If the user is calling this method from a version of Windows older than Vista, then show legacy dialog.
        /// </summary>
        /// <param name="owner">The owner that opened the dialog as a IWin32Window.</param>
        /// <returns>True if a folder was selected; otherwise, false.</returns>
        private bool ShowLegacyDialog(IWin32Window owner)
        {
            var frm = new SaveFileDialog();
            frm.CheckFileExists = false;
            frm.CheckPathExists = true;
            frm.CreatePrompt = false;
            frm.Filter = "|" + Guid.Empty.ToString();
            frm.FileName = "any";
            if (InitialDirectory is not null)
            {
                frm.InitialDirectory = InitialDirectory;
            }
            frm.OverwritePrompt = false;
            frm.Title = "Select Folder";
            frm.ValidateNames = false;
            if (frm.ShowDialog((Window)owner) is { } arg1 && arg1 == true)
            {
                SelectedFolder = System.IO.Path.GetDirectoryName(frm.FileName);
                return true;
            }
            else
            {
                return false;
            }
        }

    }

    #region Native Methods for Creating Dialog Window

    /// <summary>
    /// Contains native Windows API methods and COM interfaces for file dialog functionality.
    /// </summary>
    internal sealed class NativeMethods
    {
        /// <summary>
        /// Private constructor to prevent instantiation.
        /// </summary>
        private NativeMethods()
        {
        }

        #region Constants

        /// <summary>Folder picker mode flag.</summary>
        public const uint FOS_PICKFOLDERS = 32U;
        /// <summary>Force file system items only.</summary>
        public const uint FOS_FORCEFILESYSTEM = 64U;
        /// <summary>Do not validate items.</summary>
        public const uint FOS_NOVALIDATE = 256U;
        /// <summary>Do not test file creation.</summary>
        public const uint FOS_NOTESTFILECREATE = 65536U;
        /// <summary>Do not add to recent documents.</summary>
        public const uint FOS_DONTADDTORECENT = 33554432U;
        /// <summary>Success return code.</summary>
        public const uint S_OK = 0U;
        /// <summary>File system path display name flag.</summary>
        public const uint SIGDN_FILESYSPATH = 0x80058000U;

        #endregion

        #region COM

        /// <summary>
        /// COM class wrapper for the native file open dialog.
        /// </summary>
        [ComImport]
        [ClassInterface(ClassInterfaceType.None)]
        [TypeLibType(TypeLibTypeFlags.FCanCreate)]
        [Guid("DC1C5A9C-E88A-4DDE-A5A1-60F82A20AEF7")]
        internal class FileOpenDialogRCW
        {
        }

        /// <summary>
        /// COM interface for the Windows file dialog.
        /// </summary>
        [ComImport]
        [Guid("42F85136-DB7E-439C-85F1-E4075D135FC8")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IFileDialog
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            [PreserveSig]
            uint Show([In][Optional] IntPtr hwndOwner);
            // IModalWindow 

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypes([In] uint cFileTypes, [In][MarshalAs(UnmanagedType.LPArray)] IntPtr rgFilterSpec);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileTypeIndex([In] uint iFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileTypeIndex(ref uint piFileType);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Advise([In][MarshalAs(UnmanagedType.Interface)] IntPtr pfde, ref uint pdwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Unadvise([In] uint dwCookie);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOptions([In] uint fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetOptions(ref uint fos);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            void SetDefaultFolder([In][MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFolder([In][MarshalAs(UnmanagedType.Interface)] IShellItem psi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFolder([MarshalAs(UnmanagedType.Interface)] ref IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetCurrentSelection([MarshalAs(UnmanagedType.Interface)] ref IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileName([In][MarshalAs(UnmanagedType.LPWStr)] string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetFileName([MarshalAs(UnmanagedType.LPWStr)] ref string pszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetTitle([In][MarshalAs(UnmanagedType.LPWStr)] string pszTitle);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetOkButtonLabel([In][MarshalAs(UnmanagedType.LPWStr)] string pszText);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFileNameLabel([In][MarshalAs(UnmanagedType.LPWStr)] string pszLabel);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetResult([MarshalAs(UnmanagedType.Interface)] ref IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint AddPlace([In][MarshalAs(UnmanagedType.Interface)] IShellItem psi, uint fdap);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetDefaultExtension([In][MarshalAs(UnmanagedType.LPWStr)] string pszDefaultExtension);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Close([MarshalAs(UnmanagedType.Error)] uint hr);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetClientGuid([In] ref Guid guid);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint ClearClientData();

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint SetFilter([MarshalAs(UnmanagedType.Interface)] IntPtr pFilter);
        }

        /// <summary>
        /// COM interface for shell items.
        /// </summary>
        [ComImport]
        [Guid("43826D1E-E718-42EE-BC55-A1E261C37BFE")]
        [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
        internal interface IShellItem
        {
            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint BindToHandler([In] IntPtr pbc, [In] ref Guid rbhid, [In] ref Guid riid, [MarshalAs(UnmanagedType.Interface)] out IntPtr ppvOut);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetParent([MarshalAs(UnmanagedType.Interface)] ref IShellItem ppsi);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetDisplayName([In] uint sigdnName, ref IntPtr ppszName);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint GetAttributes([In] uint sfgaoMask, ref uint psfgaoAttribs);

            [MethodImpl(MethodImplOptions.InternalCall, MethodCodeType = MethodCodeType.Runtime)]
            uint Compare([In][MarshalAs(UnmanagedType.Interface)] IShellItem psi, [In] uint hint, ref int piOrder);
        }

        #endregion

        /// <summary>
        /// Creates a shell item from a parsing name.
        /// </summary>
        /// <param name="pszPath">The path to parse.</param>
        /// <param name="pbc">Bind context.</param>
        /// <param name="riid">Reference to the interface ID.</param>
        /// <param name="ppv">The resulting shell item.</param>
        /// <returns>HRESULT status code.</returns>
        [DllImport("shell32.dll", CharSet = CharSet.Unicode, SetLastError = true)]
        internal static extern int SHCreateItemFromParsingName([MarshalAs(UnmanagedType.LPWStr)] string pszPath, IntPtr pbc, ref Guid riid, [MarshalAs(UnmanagedType.Interface)] ref IShellItem ppv);

    }
}

#endregion
