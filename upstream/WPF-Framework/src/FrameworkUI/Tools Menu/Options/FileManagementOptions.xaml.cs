using System.Windows;
using System.Windows.Controls;

namespace FrameworkUI
{
    /// <summary>
    /// Interaction logic for FileManagementOptions.xaml providing file management settings configuration.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public partial class FileManagementOptions : UserControl
    {

        /// <summary>
        /// Initializes a new instance of the <see cref="FileManagementOptions"/> class.
        /// </summary>
        public FileManagementOptions()
        {
            // This call is required by the designer.
            InitializeComponent();
            // Add any initialization after the InitializeComponent() call.
        }

        /// <summary>
        /// Dependency property for the compress project file on close boolean.
        /// </summary>
        public static DependencyProperty CompressProjectFileOnCloseProperty = DependencyProperty.Register(nameof(CompressProjectFileOnClose), typeof(bool), typeof(FileManagementOptions), new UIPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to compress the project file on close.
        /// </summary>
        public bool CompressProjectFileOnClose
        {
            get { return (bool)GetValue(CompressProjectFileOnCloseProperty); }
            set { SetValue(CompressProjectFileOnCloseProperty, value); }
        }

        /// <summary>
        /// Dependency property for the create auto-recover backup boolean.
        /// </summary>
        public static DependencyProperty CreateAutoRecoverBackupProperty = DependencyProperty.Register(nameof(CreateAutoRecoverBackup), typeof(bool), typeof(FileManagementOptions), new UIPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to create an AutoRecover backup file.
        /// </summary>
        public bool CreateAutoRecoverBackup
        {
            get { return (bool)GetValue(CreateAutoRecoverBackupProperty); }
            set { SetValue(CreateAutoRecoverBackupProperty, value); }
        }

        /// <summary>
        /// Dependency property for the AutoRecover interval.
        /// </summary>
        public static DependencyProperty AutoRecoverIntervalProperty = DependencyProperty.Register(nameof(AutoRecoverInterval), typeof(int), typeof(FileManagementOptions), new UIPropertyMetadata(10));

        /// <summary>
        /// Gets or sets the AutoRecover interval in minutes.
        /// </summary>
        public int AutoRecoverInterval
        {
            get { return (int)GetValue(AutoRecoverIntervalProperty); }
            set { SetValue(AutoRecoverIntervalProperty, value); }
        }

        /// <summary>
        /// Dependency property for the keep last backup version on close boolean.
        /// </summary>
        public static DependencyProperty KeepLastBackupVersionProperty = DependencyProperty.Register(nameof(KeepLastBackupVersion), typeof(bool), typeof(FileManagementOptions), new UIPropertyMetadata(true));

        /// <summary>
        /// Gets or sets whether to keep the last backup version if the file were to unexpectedly close.
        /// </summary>
        public bool KeepLastBackupVersion
        {
            get { return (bool)GetValue(KeepLastBackupVersionProperty); }
            set { SetValue(KeepLastBackupVersionProperty, value); }
        }

    }
}
