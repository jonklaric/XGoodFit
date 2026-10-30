using System;
using System.Collections.Generic;
using System.Data.SQLite;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text.Json;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using OxyPlotControls;
using RMC.BestFit.UI;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: hosts the shared analysis controls (GUI/Support/Controls) as the analysis views do,
    /// on the example projects' stored results, with the application's resources in a theme.
    /// </summary>
    internal static class SharedControlsTesting
    {
        /// <summary>The application's resource dictionaries, in App.axaml's order (the theme's come after them).</summary>
        private static readonly (string Assembly, string Path)[] AppDictionaries =
        {
            ("FrameworkUI", "Icons/IconDictionary.axaml"),
            ("GenericControls", "Themes/GenericControlsTheme.axaml"),
            ("OxyPlotControls", "Themes/OxyPlotControlsTheme.axaml"),
            ("NumericControls", "Themes/NumericControlsTheme.axaml"),
            ("DatabaseControls", "Themes/DatabaseControlsTheme.axaml"),
            ("RMC.BestFit.UI", "Resources/IconDictionary.axaml"),
            ("XGoodFit", "Resources/IconDictionary.axaml"),
            ("GenericControls", "Resources/ResourceDictionary.axaml"),
        };

        private static readonly List<ResourceInclude> _included = new List<ResourceInclude>();

        private static Style _overlayPopups;

        /// <summary>The repository's root (where XGoodFit.slnx is).</summary>
        public static string RepositoryRoot { get; } = FindRepositoryRoot();

        /// <summary>RMC-BestFit's examples (scripts/fetch-sample-data.sh RMC-BestFit).</summary>
        public static string ExamplesFolder => Path.Combine(RepositoryRoot, "sample-data", "RMC-BestFit", "examples");

        /// <summary>
        /// An example's file. Without the sample data the test is inconclusive, or fails where
        /// XGOODFIT_REQUIRE_HECDSS=1 (CI), as the file-compatibility tests do.
        /// </summary>
        public static string Example(string relative)
        {
            string path = Path.Combine(ExamplesFolder, relative);
            if (!File.Exists(path))
            {
                string message = $"These tests need RMC-BestFit's example projects (scripts/fetch-sample-data.sh RMC-BestFit): {path}";
                if (Environment.GetEnvironmentVariable("XGOODFIT_REQUIRE_HECDSS") == "1") Assert.Fail(message);
                Assert.Inconclusive(message);
            }
            return path;
        }

        /// <summary>Opens a copy of an example project, on an instance of its own, as File &gt; Open does.</summary>
        public static BestFitProject Open(string relative)
        {
            string example = Example(relative);
            string folder = Path.Combine(Path.GetTempPath(), "XGoodFitSharedControls", Guid.NewGuid().ToString("N").Substring(0, 12));
            Directory.CreateDirectory(folder);
            string copy = Path.Combine(folder, Path.GetFileName(example));
            File.Copy(example, copy);
            var project = (BestFitProject)Activator.CreateInstance(typeof(BestFitProject), nonPublic: true);
            project.FullFileName = copy;
            project.Open();
            return project;
        }

        /// <summary>Closes a project <see cref="Open"/> opened and deletes its copy.</summary>
        public static void Close(BestFitProject project)
        {
            string folder = Path.GetDirectoryName(project.FullFileName);
            project.Close();
            SQLiteConnection.ClearAllPools();
            for (int attempt = 0; attempt < 10 && Directory.Exists(folder); attempt++)
            {
                try
                {
                    Directory.Delete(folder, recursive: true);
                }
                catch (IOException)
                {
                    Thread.Sleep(100);
                }
                catch (UnauthorizedAccessException)
                {
                    Thread.Sleep(100);
                }
            }
        }

        /// <summary>The project's element of type T with the name given.</summary>
        public static T Element<T>(BestFitProject project, string name) where T : class, IElement =>
            Elements(project).OfType<T>().Single(element => element.Name == name);

        /// <summary>Every element of the project.</summary>
        public static IEnumerable<IElement> Elements(BestFitProject project) =>
            (project.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(ElementsOf);

        private static IEnumerable<IElement> ElementsOf(IElementCollection collection) =>
            collection.Concat((collection.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(ElementsOf));

        /// <summary>
        /// The application's resources as App.axaml merges them, and the theme as the application sets it
        /// (FrameworkUI's ThemeManager). The plot toolbar's theme-change question isn't asked.
        /// </summary>
        public static void UseAppResources(ThemeColor theme)
        {
            var merged = Application.Current.Resources.MergedDictionaries;
            if (_included.Count == 0)
            {
                foreach (var (assembly, path) in AppDictionaries)
                {
                    var source = new Uri($"avares://{assembly}/{path}");
                    var dictionary = new ResourceInclude(source) { Source = source };
                    merged.Insert(_included.Count, dictionary);
                    _included.Add(dictionary);
                }
            }
            OxyPlotThemeManager.SuppressThemeChangeWarning = true;
            ThemeManager.SetTheme(theme);
        }

        /// <summary>Removes the dictionaries <see cref="UseAppResources"/> added, and draws popups in their windows again.</summary>
        public static void ReleaseAppResources()
        {
            foreach (var dictionary in _included)
                Application.Current.Resources.MergedDictionaries.Remove(dictionary);
            _included.Clear();
            if (_overlayPopups != null)
            {
                Application.Current.Styles.Remove(_overlayPopups);
                _overlayPopups = null;
            }
        }

        /// <summary>Draws popups in their window's overlay, so a capture of the window includes them.</summary>
        public static void OverlayPopups()
        {
            if (_overlayPopups != null) return;
            _overlayPopups = new Style(x => x.Is<Popup>()) { Setters = { new Setter(Popup.ShouldUseOverlayLayerProperty, true) } };
            Application.Current.Styles.Add(_overlayPopups);
        }

        /// <summary>Shows content in a window of the size given, with the theme's background, and lays it out.</summary>
        public static Window Show(Control content, double width, double height)
        {
            var root = new Border { Child = content };
            root.Bind(Border.BackgroundProperty, root.GetResourceObservable("EnvironmentWindowBackground"));
            WpfCompat.Input.CommandManager.InvalidateRequerySuggested();
            var window = new Window { Width = width, Height = height, Content = root };
            window.Show();
            window.Activate();
            Settle();
            return window;
        }

        /// <summary>Runs the dispatcher's jobs and renders, twice: layout, posted work and the rendered scene are done.</summary>
        public static void Settle()
        {
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        }

        /// <summary>Presses and releases the left button at a point of the window.</summary>
        public static void Click(Window window, Point point)
        {
            Settle();
            window.MouseMove(point);
            window.MouseDown(point, MouseButton.Left);
            window.MouseUp(point, MouseButton.Left);
            Settle();
        }

        /// <summary>The window point at the middle of a control.</summary>
        public static Point Centre(Window window, Control control) =>
            control.TranslatePoint(new Point(control.Bounds.Width / 2, control.Bounds.Height / 2), window).Value;

        /// <summary>
        /// A point of the window over which input reaches <paramref name="control"/>, nearest its middle: the
        /// toolbar's buttons hit only on their icons.
        /// </summary>
        public static Point HitPoint(Window window, Control control)
        {
            Point centre = Centre(window, control);
            var candidates = new List<Point>();
            for (double dx = -control.Bounds.Width / 2 + 1; dx < control.Bounds.Width / 2; dx++)
                for (double dy = -control.Bounds.Height / 2 + 1; dy < control.Bounds.Height / 2; dy++)
                    candidates.Add(centre + new Vector(dx, dy));
            foreach (Point candidate in candidates.OrderBy(p => Point.Distance(p, centre)))
            {
                if (window.InputHitTest(candidate) is Visual hit && (hit == control || hit.GetVisualAncestors().Contains(control)))
                    return candidate;
            }
            return centre;
        }

        /// <summary>Saves the window's rendered frame to artifacts/screenshots/<paramref name="name"/>.png.</summary>
        public static string Capture(Window window, string name)
        {
            // The headless render timer uses real time: let the hover and expander transitions finish.
            Settle();
            Thread.Sleep(250);
            Settle();
            string folder = Path.Combine(RepositoryRoot, "artifacts", "screenshots");
            Directory.CreateDirectory(folder);
            string path = Path.Combine(folder, name + ".png");
            using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
            frame.Save(path, new PngBitmapEncoderOptions());
            return path;
        }

        /// <summary>A native RMC-BestFit plot export's capture data (screenshots/*.json.gz beside an example).</summary>
        public static JsonElement NativePlot(string relative)
        {
            string path = Example(relative);
            using var stream = new GZipStream(File.OpenRead(path), CompressionMode.Decompress);
            using var document = JsonDocument.Parse(stream);
            return document.RootElement.Clone();
        }

        private static string FindRepositoryRoot()
        {
            var directory = new DirectoryInfo(AppContext.BaseDirectory);
            while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XGoodFit.slnx")))
                directory = directory.Parent;
            return directory?.FullName ?? AppContext.BaseDirectory;
        }
    }
}
