using System;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using Themes;

namespace RMC_BestFit
{
    /// <summary>
    /// XGoodFit addition: the application rendered headlessly to PNG files, for <c>scripts/check-package.sh</c>
    /// (which checks that a packaged copy starts, renders and loads the HEC-DSS library) and for reviewing
    /// the look: <c>XGoodFit capture &lt;folder&gt; [project]</c>.
    /// </summary>
    /// <remarks>
    /// In each theme it renders the main window, its Project and Help menus, the project explorer's context
    /// menu on the analyses and the About window, with the blank project the application starts with, or a
    /// copy of the given project. The shell's settings, layout, recent files and log go to a temporary
    /// folder, so neither the user's settings nor the project change. Then it reports whether the HEC-DSS
    /// native library loads.
    /// </remarks>
    internal static class Capture
    {
        /// <summary>The themes, in Tools > Options' order.</summary>
        private static readonly ThemeColor[] Themes = [ThemeColor.Light, ThemeColor.Blue, ThemeColor.Dark];

        /// <summary>Renders the application to <paramref name="folder"/>.</summary>
        /// <param name="folder">The folder the PNG files go to, made if needed.</param>
        /// <param name="project">A project to open (a copy of it), or null for the blank project.</param>
        /// <returns>The exit code.</returns>
        public static int Run(string folder, string project)
        {
            AppBuilder.Configure<App>()
                .UseSkia()
                .UseHarfBuzz()
                .UseThemeFonts()
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

            string state = Path.Combine(Path.GetTempPath(), "XGoodFit-capture-" + Guid.NewGuid().ToString("N"));
            UseFolder(state);
            Directory.CreateDirectory(folder);
            try
            {
                MainWindow mainWindow = App.CreateMainWindow();
                // Plot toolbars ask before re-theming their plots when the theme changes, a modal question
                // nobody answers here: answer OK, as "Don't show this message again" does.
                OxyPlotControls.OxyPlotThemeManager.SuppressThemeChangeWarning = true;
                // The size of upstream's screenshot (docs/images/rmc-bestfit-gui.png).
                mainWindow.WindowState = WindowState.Normal;
                mainWindow.Width = 1920;
                mainWindow.Height = 1152;
                mainWindow.Show();
                Settle();

                if (project != null)
                {
                    string copy = Path.Combine(state, Path.GetFileName(project));
                    File.Copy(project, copy);
                    mainWindow.OpenProject(copy);
                    Settle();
                }

                var mainMenu = mainWindow.FindControl<Menu>("MainMenu")!;
                var projectMenu = mainWindow.FindControl<MenuItem>("ProjectMenu")!;
                var helpMenu = mainWindow.FindControl<MenuItem>("HelpMenu")!;
                foreach (ThemeColor theme in Themes)
                {
                    // As Tools > Options switches it.
                    UserSettings.ColorTheme = theme.ToString();
                    ThemeManager.SetTheme(theme);
                    Save(mainWindow, folder, "main", theme);

                    projectMenu.Open();
                    Save(mainWindow, folder, "project-menu", theme);
                    mainMenu.Close();

                    helpMenu.Open();
                    Save(mainWindow, folder, "help-menu", theme);
                    mainMenu.Close();

                    // The univariate analyses' menu: their New items and Batch Run.
                    var analyses = mainWindow.ProjectNode.ChildNodes.OfType<ElementNodeCollection>()
                        .FirstOrDefault(node => node.ElementCollection is RMC.BestFit.UI.UnivariateAnalysisCollection);
                    if (analyses?.ContextMenu != null)
                    {
                        analyses.ContextMenu.Placement = PlacementMode.Bottom;
                        analyses.ContextMenu.Open(analyses);
                        Save(mainWindow, folder, "explorer-menu", theme);
                        analyses.ContextMenu.Close();
                    }

                    Save(MainProjectNode.CreateAboutWindow(), folder, "about", theme, show: true);
                }

                ReportHecDss();
                // The main window stays open: closing it would ask whether to save the project, and wait for
                // an answer. The process ends here.
                return 0;
            }
            finally
            {
                try
                {
                    Directory.Delete(state, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        /// <summary>Points the shell's files at a folder.</summary>
        /// <param name="folder">The folder.</param>
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

        /// <summary>Reports whether the HEC-DSS native library loads, which shows that a package ships it.</summary>
        private static void ReportHecDss()
        {
            try
            {
                Hec.Dss.DssGlobals.SetProgramName("XGoodFit");
                Console.WriteLine("HEC-DSS native library: loaded");
            }
            catch (DllNotFoundException e)
            {
                Console.WriteLine($"HEC-DSS native library: not available ({e.Message})");
            }
        }

        /// <summary>Renders a window to a PNG file.</summary>
        /// <param name="window">The window.</param>
        /// <param name="folder">The folder the file goes to.</param>
        /// <param name="view">What the file shows, in its name.</param>
        /// <param name="theme">The theme, in its name.</param>
        /// <param name="show">Whether to show the window first, and close it afterwards.</param>
        private static void Save(Window window, string folder, string view, ThemeColor theme, bool show = false)
        {
            if (show)
            {
                window.Show();
            }
            Settle();
            string path = Path.Combine(folder, $"xgoodfit-{view}-{theme.ToString().ToLowerInvariant()}.png");
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
            frame.Save(path, new PngBitmapEncoderOptions());
            Console.WriteLine(path);
            if (show)
            {
                window.Close();
            }
        }

        /// <summary>Lets layout run and fade-in animations finish: the headless render timer uses real time.</summary>
        private static void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(300);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }
    }
}
