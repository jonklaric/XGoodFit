using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the application's main window for the tests, as App.CreateMainWindow makes it,
    /// with the shell's files in a temporary folder (as FrameworkUI.Tests' Shell does), the application's
    /// resources (App.axaml's) in the test application, and copies of the example projects.
    /// </summary>
    internal static class BestFitShell
    {
        /// <summary>App.axaml's resource dictionaries, in its order.</summary>
        private static readonly string[] ApplicationDictionaries =
        [
            "avares://FrameworkUI/Icons/IconDictionary.axaml",
            "avares://GenericControls/Themes/GenericControlsTheme.axaml",
            "avares://OxyPlotControls/Themes/OxyPlotControlsTheme.axaml",
            "avares://NumericControls/Themes/NumericControlsTheme.axaml",
            "avares://DatabaseControls/Themes/DatabaseControlsTheme.axaml",
            "avares://RMC.BestFit.UI/Resources/IconDictionary.axaml",
            "avares://XGoodFit/Resources/IconDictionary.axaml",
            "avares://GenericControls/Resources/ResourceDictionary.axaml",
        ];

        private static readonly List<ResourceInclude> _dictionaries = [];
        private static readonly List<IStyle> _captureStyles = [];

        /// <summary>The temporary folder the shell's files and the project copies go in for the current test.</summary>
        public static string Folder { get; private set; } = string.Empty;

        /// <summary>The repository's root, which XGoodFit.slnx marks.</summary>
        public static string RepositoryRoot
        {
            get
            {
                var directory = new DirectoryInfo(AppContext.BaseDirectory);
                while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XGoodFit.slnx")))
                {
                    directory = directory.Parent;
                }
                Assert.IsNotNull(directory, "the tests run inside the repository");
                return directory.FullName;
            }
        }

        /// <summary>The example projects (scripts/fetch-sample-data.sh RMC-BestFit), by path.</summary>
        public static IReadOnlyList<string> Examples()
        {
            string folder = Path.Combine(RepositoryRoot, "sample-data", "RMC-BestFit", "examples");
            Assert.IsTrue(Directory.Exists(folder), $"no example projects in {folder}: run scripts/fetch-sample-data.sh RMC-BestFit");
            return Directory.GetFiles(folder, "*.bestfit", SearchOption.AllDirectories).OrderBy(path => path, StringComparer.Ordinal).ToList();
        }

        /// <summary>The example project named <paramref name="name"/> (its file name without the extension).</summary>
        public static string Example(string name) => Examples().Single(path => Path.GetFileNameWithoutExtension(path) == name);

        /// <summary>
        /// Gives the test application the application's resources and points the shell's files at a new
        /// temporary folder, with the default user settings saved there (no auto-backup), as
        /// FrameworkUI.Tests' Shell.UseTemporaryFolder does.
        /// </summary>
        public static void Start()
        {
            var resources = Application.Current!.Resources;
            for (int i = 0; i < ApplicationDictionaries.Length; i++)
            {
                var source = new Uri(ApplicationDictionaries[i]);
                var dictionary = new ResourceInclude(source) { Source = source };
                // Before any theme dictionaries, which ThemeManager adds at the end, as the application's.
                resources.MergedDictionaries.Insert(i, dictionary);
                _dictionaries.Add(dictionary);
            }
            resources["BestFit"] = LoadBitmap("avares://XGoodFit/Resources/BestFit_Icon.ico");
            resources["USACE"] = LoadBitmap("avares://XGoodFit/Resources/USACE.png");

            Folder = Path.Combine(AppContext.BaseDirectory, "BestFitShell", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Folder);
            ShellPublicVariables.UserSettingsFolderPath = Path.Combine(Folder, "UserSettings") + Path.DirectorySeparatorChar;
            ShellPublicVariables.UserSettingsFilePath = ShellPublicVariables.UserSettingsFolderPath + "UserSettings.xml";
            ShellPublicVariables.AvalonDockLayoutFolderPath = Path.Combine(Folder, "Layout") + Path.DirectorySeparatorChar;
            ShellPublicVariables.DefaultAvalonDockLayoutFilePath = ShellPublicVariables.AvalonDockLayoutFolderPath + "AvalonDock.xml";
            ShellPublicVariables.RecentFileListFolderPath = Path.Combine(Folder, "RecentFiles") + Path.DirectorySeparatorChar;
            ShellPublicVariables.RecentFileListFilePath = ShellPublicVariables.RecentFileListFolderPath + "RecentFileList.xml";
            ShellPublicVariables.MessageLogFolderPath = Path.Combine(Folder, "MessageLog") + Path.DirectorySeparatorChar;
            ShellPublicVariables.MessageLogFilePath = ShellPublicVariables.MessageLogFolderPath + "MessageLog.txt";
            UserSettings.RestoreDefaults();
            UserSettings.CreateAutoRecoverBackup = false;
            Directory.CreateDirectory(ShellPublicVariables.UserSettingsFolderPath);
            UserSettings.Save(ShellPublicVariables.UserSettingsFilePath);
        }

        /// <summary>
        /// Creates and shows the main window, as App.CreateMainWindow does (the theme, the software's
        /// version and extension, the blank project, the MainProjectNode with the tree item theme), and opens
        /// a copy of <paramref name="example"/> in it with <see cref="MainWindow.OpenProject"/>, or keeps the
        /// blank project for null.
        /// </summary>
        /// <remarks>
        /// Upstream's menu item collections are shared by every main window (dependency property defaults),
        /// and a window adds its Tools and Help items to them without clearing them: upstream makes one
        /// window. Each test window gets its own collections, so one test's items don't carry over.
        /// </remarks>
        public static (MainWindow Window, MainProjectNode Node, BestFitProject Project) Open(string example, double width = 1920, double height = 1152)
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            ShellPublicVariables.SoftwareVersionDate = RMC_BestFit.App.ProductVersionDate;
            ShellPublicVariables.SoftwareVersion = RMC_BestFit.App.ProductVersion;
            ShellPublicVariables.SoftwareExtension = ".bestfit";
            var project = BestFitProject.GetInstance();
            project.CreateNewDummyProject();

            var window = new MainWindow { WindowState = WindowState.Normal, Width = width, Height = height };
            window.ProjectMenuItems = [];
            window.ToolsMenuItems = [];
            window.HelpMenuItems = [];
            var node = new MainProjectNode(project) { Theme = (ControlTheme)Application.Current!.FindResource("TreeViewItemStyle")! };
            window.ProjectNode = node;
            // Plot toolbars ask before re-theming their plots, a modal question nobody answers in a test.
            OxyPlotControls.OxyPlotThemeManager.SuppressThemeChangeWarning = true;
            // Without a desktop lifetime, WpfCompat's dialogs and commands take the window that has the
            // focus (its command manager follows the focus once started): this one, not a closed one.
            WpfCompat.Input.CommandManager.InvalidateRequerySuggested();
            window.Show();
            window.Activate();
            Frame();
            window.Click(node.NodeHeader);
            if (example != null)
            {
                string copy = Path.Combine(Folder, Path.GetFileName(example));
                File.Copy(example, copy, overwrite: true);
                window.OpenProject(copy);
                Frame();
            }
            return (window, node, project);
        }

        /// <summary>
        /// Closes <paramref name="window"/> without the question about unsaved changes (the project copy is
        /// saved first), lets go of the application's resources and deletes the temporary folder.
        /// </summary>
        public static void Close(MainWindow window)
        {
            try
            {
                if (window != null)
                {
                    var project = BestFitProject.GetInstance();
                    if (project.IsDirty && File.Exists(project.FullFileName) && project.FullFileName.StartsWith(Folder, StringComparison.Ordinal))
                    {
                        project.Save();
                    }
                    foreach (Window other in OpenWindows().Where(w => w != window))
                    {
                        other.Close();
                    }
                    if (window.IsVisible)
                    {
                        window.Close();
                    }
                    Frame();
                }
            }
            finally
            {
                ThemeManager.SetTheme(ThemeColor.Light);
                foreach (IStyle style in _captureStyles)
                {
                    Application.Current!.Styles.Remove(style);
                }
                _captureStyles.Clear();
                foreach (ResourceInclude dictionary in _dictionaries)
                {
                    Application.Current!.Resources.MergedDictionaries.Remove(dictionary);
                }
                _dictionaries.Clear();
                Application.Current!.Resources.Remove("BestFit");
                Application.Current.Resources.Remove("USACE");
                try
                {
                    Directory.Delete(Folder, recursive: true);
                }
                catch (IOException)
                {
                }
                catch (UnauthorizedAccessException)
                {
                }
            }
        }

        /// <summary>
        /// Draws popups (menus, context menus) in their windows, so captures include them, and shows the
        /// Metro title bar the headless platform hides, as the capture mode does. <see cref="Close"/> removes them.
        /// </summary>
        public static void UseCaptureStyles()
        {
            IStyle overlay = new Style(x => x.Is<Popup>())
            {
                Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) },
            };
            IStyle titleBar = new Style(x => x.Is<Window>().PropertyEquals(Window.IsExtendedIntoWindowDecorationsProperty, false).Template().OfType<Grid>().Name("TitleBar"))
            {
                Setters = { new Setter(Visual.IsVisibleProperty, true) },
            };
            Application.Current!.Styles.Add(overlay);
            Application.Current.Styles.Add(titleBar);
            _captureStyles.Add(overlay);
            _captureStyles.Add(titleBar);
        }

        /// <summary>Renders <paramref name="window"/> to artifacts/screenshots/<paramref name="name"/>.png.</summary>
        public static string Capture(Window window, string name)
        {
            Settle();
            string folder = Path.Combine(RepositoryRoot, "artifacts", "screenshots");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, name + ".png");
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
            frame.Save(path, new PngBitmapEncoderOptions());
            return path;
        }

        /// <summary>Lets layout run and animations finish (the headless render timer uses real time), then renders.</summary>
        public static void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(300);
            Frame();
        }

        /// <summary>Runs the queued jobs and renders a frame, so that input hits what's shown.</summary>
        public static void Frame()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Runs the dispatcher until <paramref name="condition"/> holds, or fails after <paramref name="timeout"/>.</summary>
        public static void WaitUntil(Func<bool> condition, TimeSpan timeout, string what)
        {
            DateTime end = DateTime.UtcNow + timeout;
            while (!condition())
            {
                Assert.IsTrue(DateTime.UtcNow < end, "timed out waiting for " + what);
                Dispatcher.UIThread.RunJobs();
                Thread.Sleep(20);
            }
            Frame();
        }

        /// <summary>The windows the test has open (the headless session has no window list of its own).</summary>
        public static IEnumerable<Window> OpenWindows() => _opened.Where(window => window.IsVisible).ToList();

        private static readonly List<Window> _opened = [];

        static BestFitShell()
        {
            Window.WindowOpenedEvent.AddClassHandler<Window>((window, _) => _opened.Add(window));
            Window.WindowClosedEvent.AddClassHandler<Window>((window, _) => _opened.Remove(window));
        }

        /// <summary>Runs <paramref name="answer"/> on the next window of type T that opens, once it's shown.</summary>
        public static IDisposable WhenOpened<T>(Action<T> answer) where T : Window
        {
            IDisposable handler = null;
            handler = Window.WindowOpenedEvent.AddClassHandler<T>((window, _) =>
            {
                handler?.Dispose();
                Dispatcher.UIThread.Post(() => answer(window), DispatcherPriority.Background);
            });
            return handler;
        }

        /// <summary>Clicks the button of <paramref name="window"/> that shows <paramref name="content"/>.</summary>
        public static void ClickButton(Window window, string content)
        {
            window.GetVisualDescendants().OfType<Button>().First(button => Equals(button.Content, content) || (button.Content as TextBlock)?.Text == content)
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Clicks a menu item, as a user does once its menu is open.</summary>
        public static void Click(MenuItem item)
        {
            item.RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent));
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Clicks the left button at the centre of <paramref name="visual"/>.</summary>
        public static void Click(this TopLevel window, Visual visual)
        {
            Frame();
            Point at = visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Frame();
        }

        /// <summary>Clicks the right button at the centre of <paramref name="visual"/>, which opens its context menu.</summary>
        public static void RightClick(this TopLevel window, Visual visual)
        {
            Frame();
            Point at = visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)!.Value;
            window.MouseDown(at, MouseButton.Right);
            window.MouseUp(at, MouseButton.Right);
            Frame();
        }

        /// <summary>Double-clicks the left button at the centre of <paramref name="visual"/>.</summary>
        public static void DoubleClick(this TopLevel window, Visual visual)
        {
            Frame();
            Point at = visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), window)!.Value;
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            window.MouseDown(at, MouseButton.Left);
            window.MouseUp(at, MouseButton.Left);
            Frame();
        }

        /// <summary>
        /// The delegates handling <paramref name="item"/>'s Click event, from Avalonia's handler table (which it
        /// keeps private): the Help items' handlers capture the address they open.
        /// </summary>
        public static IReadOnlyList<Delegate> ClickHandlers(MenuItem item)
        {
            FieldInfo table = typeof(Interactive).GetField("_eventHandlers", BindingFlags.NonPublic | BindingFlags.Instance);
            Assert.IsNotNull(table, "Avalonia's Interactive keeps its handlers in _eventHandlers");
            var handlers = new List<Delegate>();
            if (table.GetValue(item) is IDictionary byEvent && byEvent[MenuItem.ClickEvent] is IEnumerable subscriptions)
            {
                foreach (object subscription in subscriptions)
                {
                    if (subscription.GetType().GetProperty("Handler")?.GetValue(subscription) is Delegate handler)
                    {
                        handlers.Add(handler);
                    }
                }
            }
            return handlers;
        }

        /// <summary>The value a handler captured in the variable or parameter <paramref name="name"/>.</summary>
        public static object Captured(Delegate handler, string name) =>
            handler.Target?.GetType().GetField(name, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(handler.Target);

        private static Bitmap LoadBitmap(string uri)
        {
            using Stream stream = AssetLoader.Open(new Uri(uri));
            return new Bitmap(stream);
        }
    }
}
