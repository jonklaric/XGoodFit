using System;
using System.IO;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.App.Tests.GUI.Support;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the application's startup (App.CreateMainWindow, the body of upstream's
    /// Application_Startup), headlessly, with the shell's files in a temporary folder.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class AppStartupTests
    {
        [STATestMethod]
        public void CreateMainWindow_ShowsTheShell_ForBestFitsProject()
        {
            string folder = Path.Combine(AppContext.BaseDirectory, "AppStartupTests", Guid.NewGuid().ToString("N"));
            ShellPublicVariables.UserSettingsFolderPath = Path.Combine(folder, "UserSettings") + Path.DirectorySeparatorChar;
            ShellPublicVariables.UserSettingsFilePath = ShellPublicVariables.UserSettingsFolderPath + "UserSettings.xml";
            ShellPublicVariables.AvalonDockLayoutFolderPath = Path.Combine(folder, "Layout") + Path.DirectorySeparatorChar;
            ShellPublicVariables.DefaultAvalonDockLayoutFilePath = ShellPublicVariables.AvalonDockLayoutFolderPath + "AvalonDock.xml";
            ShellPublicVariables.RecentFileListFolderPath = Path.Combine(folder, "RecentFiles") + Path.DirectorySeparatorChar;
            ShellPublicVariables.RecentFileListFilePath = ShellPublicVariables.RecentFileListFolderPath + "RecentFileList.xml";
            ShellPublicVariables.MessageLogFolderPath = Path.Combine(folder, "MessageLog") + Path.DirectorySeparatorChar;
            ShellPublicVariables.MessageLogFilePath = ShellPublicVariables.MessageLogFolderPath + "MessageLog.txt";
            UserSettings.CreateAutoRecoverBackup = false;
            WpfTestHost.EnsureResources();
            FrameworkUI.MainWindow window = null;
            try
            {
                window = RMC_BestFit.App.CreateMainWindow();
                window.Show();
                WpfTestHost.DrainDispatcher();

                Assert.IsInstanceOfType(window.ProjectNode, typeof(MainProjectNode));
                Assert.AreSame(RMC.BestFit.UI.BestFitProject.GetInstance(), window.ProjectNode.Project);
                Assert.IsNotNull(window.ProjectNode.Theme, "the explorer's root has the tree item theme");
                Assert.AreEqual(".bestfit", ShellPublicVariables.SoftwareExtension);
                Assert.AreEqual("2.0.1", ShellPublicVariables.SoftwareVersion, "projects save upstream's version");
                Assert.IsTrue(Directory.Exists(ShellPublicVariables.UserSettingsFolderPath), "the settings folder is made, as upstream's startup makes it");
                window.Close();
            }
            finally
            {
                // The window's explorer stays subscribed to the application's project otherwise (see BestFitShell.Release).
                if (window != null)
                {
                    if (window.IsVisible) window.Close();
                    BestFitShell.Release(window);
                }
                WpfTestHost.ReleaseResources();
            }
        }
    }
}
