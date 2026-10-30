using System.Collections.ObjectModel;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using FrameworkUI.ProjectExplorer;
using Numerics.Distributions;
using WpfCompat;
using Themes;

namespace FrameworkUI.Demo
{
    /// <summary>
    /// XGoodFit addition: the demo rendered headlessly to PNG files, as upstream's gallery screenshots
    /// were taken (upstream/WPF-Framework/docs/assets/gallery/README.md): the shell with the Framework
    /// Gallery project and its Example Hazard in each theme, its menus and its dialogs.
    /// </summary>
    internal static class Capture
    {
        private static readonly ThemeColor[] Themes = [ThemeColor.Blue, ThemeColor.Light, ThemeColor.Dark];

        public static int Run(string folder)
        {
            AppBuilder.Configure<App>()
                .UseSkia()
                .UseThemeFonts()
                .UseHarfBuzz()
                .UseHeadless(new AvaloniaHeadlessPlatformOptions { UseHeadlessDrawing = false })
                .SetupWithoutStarting();

            // Draw popups (menus, context menus) in the window, so its frame includes them.
            Application.Current!.Styles.Add(new Style(x => x.Is<Popup>())
            {
                Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) },
            });
            // Show the Metro title bar, which the window hides where the platform draws its own, as the
            // headless one does.
            Application.Current.Styles.Add(new Style(x => x.Is<Window>().PropertyEquals(Window.IsExtendedIntoWindowDecorationsProperty, false).Template().OfType<Grid>().Name("TitleBar"))
            {
                Setters = { new Setter(Visual.IsVisibleProperty, true) },
            });

            // The hazard document's plot toolbar asks before re-theming its plot when the theme changes
            // (OxyPlotThemeManager.ConfirmThemeChange, a modal question nobody answers here): answer OK,
            // as "Don't show this message again" does.
            OxyPlotControls.OxyPlotThemeManager.SuppressThemeChangeWarning = true;

            // The shell's settings, layout, recent files and log go to a temporary folder, not the user's.
            string state = Path.Combine(Path.GetTempPath(), "FrameworkUI.Demo-capture-" + Guid.NewGuid().ToString("N"));
            UseFolder(state);
            Directory.CreateDirectory(folder);
            DemoProject project = DemoProject.GetInstance();
            try
            {
                FrameworkUI.MainWindow mainWindow = App.CreateMainWindow();
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Width = 1300;
                mainWindow.Height = 700;
                mainWindow.Show();
                Settle();

                // As the gallery's: the project named Framework Gallery, and an Example Hazard added with
                // Project > New Hazard Element..., which opens its document: Log-Pearson Type III with
                // mean (log) 3, standard deviation (log) 0.5, skew (log) 0, an effective record length of
                // 100 and uncertainty, computed.
                project.Name = "Framework Gallery";
                var hazards = project.ElementCollections[0];
                var hazard = new HazardElement("Example Hazard", hazards);
                hazards.Add(hazard);
                hazard.ParentDistribution = new LogPearsonTypeIII(3, 0.5, 0);
                hazard.EffectiveRecordLength = 100;
                hazard.IsUncertain = true;
                hazard.Estimate();
                // Saved, as the gallery's project was: no unsaved marks on its nodes and document.
                project.Save();
                Settle();

                // The main window's named parts (internal to FrameworkUI, as WPF's x:Name fields are).
                var recentFiles = mainWindow.FindControl<RecentFiles>("RecentFiles");
                var windowMenu = mainWindow.FindControl<MenuItem>("WindowMenuItem");
                var openWindows = mainWindow.FindControl<OpenWindows>("OpenWindows");
                var mainMenu = mainWindow.FindControl<Menu>("MainMenu");

                foreach (ThemeColor theme in Themes)
                {
                    // As Tools > Options switches it.
                    UserSettings.ColorTheme = theme.ToString();
                    ThemeManager.SetTheme(theme);
                    Save(mainWindow, folder, "shell", theme);

                    MenuItem fileMenu = recentFiles.FileMenu;
                    fileMenu.Open();
                    Save(mainWindow, folder, "file-menu", theme);
                    mainMenu.Close();

                    windowMenu.Open();
                    Save(mainWindow, folder, "window-menu", theme);
                    mainMenu.Close();

                    ElementNode node = ElementNode.FindElementNode(hazard, mainWindow.ProjectNode);
                    node.ContextMenu.Placement = PlacementMode.Bottom;
                    node.ContextMenu.Open(node);
                    Save(mainWindow, folder, "explorer-menu", theme);
                    node.ContextMenu.Close();

                    Save(new OptionsDialog(mainWindow), folder, "options", theme, show: true);
                    Save(new SaveElementsDialog { ElementItems = new ObservableCollection<UnsavedElement> { new(hazard) } }, folder, "save-changes", theme, show: true);
                    Save(new OpenWindowsDialog { Windows = openWindows, Icon = mainWindow.Icon }, folder, "windows", theme, show: true);
                    var recentImage = Application.Current.TryFindResource("ApplicationImage") as IImage;
                    Save(new RecentFilesDialog { Files = recentFiles, FileImage = recentImage, Icon = WindowIconSource.ToWindowIcon(recentImage) }, folder, "recent-files", theme, show: true);
                }

                // The window stays open: closing it would ask whether to save the project, and wait
                // for an answer. The process ends here.
                return 0;
            }
            finally
            {
                TryDelete(project.FullFileName);
                try
                {
                    Directory.Delete(state, recursive: true);
                }
                catch (IOException)
                {
                }
            }
        }

        // Points the shell's files at a folder.
        private static void UseFolder(string folder)
        {
            ShellPublicVariables.UserSettingsFolderPath = Path.Combine(folder, "UserSettings") + Path.DirectorySeparatorChar;
            ShellPublicVariables.UserSettingsFilePath = ShellPublicVariables.UserSettingsFolderPath + "UserSettings.xml";
            ShellPublicVariables.AvalonDockLayoutFolderPath = Path.Combine(folder, "Layout") + Path.DirectorySeparatorChar;
            ShellPublicVariables.DefaultAvalonDockLayoutFilePath = ShellPublicVariables.AvalonDockLayoutFolderPath + "AvalonDock.xml";
            ShellPublicVariables.RecentFileListFolderPath = Path.Combine(folder, "RecentFiles") + Path.DirectorySeparatorChar;
            ShellPublicVariables.RecentFileListFilePath = ShellPublicVariables.RecentFileListFolderPath + "RecentFileList.xml";
            ShellPublicVariables.MessageLogFolderPath = Path.Combine(folder, "MessageLog") + Path.DirectorySeparatorChar;
            ShellPublicVariables.MessageLogFilePath = ShellPublicVariables.MessageLogFolderPath + "MessageLog.txt";
        }

        private static void TryDelete(string path)
        {
            try
            {
                if (!string.IsNullOrEmpty(path) && File.Exists(path)) File.Delete(path);
            }
            catch (IOException)
            {
            }
            catch (UnauthorizedAccessException)
            {
            }
        }

        private static void Save(Window window, string folder, string view, ThemeColor theme, bool show = false)
        {
            if (show)
            {
                window.Show();
            }
            Settle();
            string path = Path.Combine(folder, $"frameworkui-{view}-{theme.ToString().ToLowerInvariant()}.png");
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
            frame.Save(path, new PngBitmapEncoderOptions());
            Console.WriteLine(path);
            if (show)
            {
                window.Close();
            }
        }

        // Lets layout run and fade-in animations finish: the headless render timer uses real time.
        private static void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(300);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
