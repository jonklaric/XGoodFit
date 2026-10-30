using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data.SQLite;
using System.IO;
using System.Linq;
using System.Threading;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Headless;
using Avalonia.Markup.Xaml.Styling;
using Avalonia.Threading;
using FrameworkInterfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: opens RMC-BestFit's example projects and shows views of their elements headlessly,
    /// for the tests of the ported views.
    /// </summary>
    /// <remarks>
    /// Examples open as tests/BestFit/RMC.BestFit.FileCompat.Tests opens them: a copy in a temporary folder,
    /// on a project instance of its own (not the application's singleton). They come from sample-data/
    /// (scripts/fetch-sample-data.sh RMC-BestFit); without them the tests are inconclusive, or fail where CI
    /// requires them (XGOODFIT_REQUIRE_HECDSS=1).
    /// </remarks>
    internal static class ViewTesting
    {
        /// <summary>The examples folder, sample-data/RMC-BestFit/examples, or null if it isn't there.</summary>
        internal static string ExamplesFolder { get; } = FindExamples();

        private static string FindExamples()
        {
            for (var directory = new DirectoryInfo(AppContext.BaseDirectory); directory != null; directory = directory.Parent)
            {
                string candidate = Path.Combine(directory.FullName, "sample-data", "RMC-BestFit", "examples");
                if (Directory.Exists(candidate)) return candidate;
            }
            return null;
        }

        /// <summary>The full path of an example, or an inconclusive (or failed, where required) test if it's missing.</summary>
        /// <param name="relativePath">The example's path under the examples folder, with forward slashes.</param>
        /// <returns>The path.</returns>
        internal static string Example(string relativePath)
        {
            string path = ExamplesFolder == null ? null : Path.Combine(ExamplesFolder, relativePath.Replace('/', Path.DirectorySeparatorChar));
            if (path == null || !File.Exists(path))
            {
                string message = "These tests need RMC-BestFit's example projects (scripts/fetch-sample-data.sh RMC-BestFit): " + relativePath;
                if (Environment.GetEnvironmentVariable("XGOODFIT_REQUIRE_HECDSS") == "1") Assert.Fail(message);
                Assert.Inconclusive(message);
            }
            return path;
        }

        /// <summary>Opens a copy of an example. Dispose the result to close it and delete the copy.</summary>
        /// <param name="relativePath">The example's path under the examples folder.</param>
        /// <returns>The opened project.</returns>
        internal static OpenedProject Open(string relativePath) => new OpenedProject(Example(relativePath));

        /// <summary>Every element of a project, collection by collection.</summary>
        /// <param name="project">The project.</param>
        /// <returns>The elements.</returns>
        internal static IEnumerable<IElement> Elements(IProject project) =>
            (project.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(ElementsOf);

        private static IEnumerable<IElement> ElementsOf(IElementCollection collection) =>
            collection.Concat((collection.ElementCollections ?? Enumerable.Empty<IElementCollection>()).SelectMany(ElementsOf));

        /// <summary>
        /// Adds what the application's ThemeService adds and WpfTestHost doesn't: the theme's window defaults
        /// (Themes' Resources/Defaults.axaml: Segoe UI 12, WPF's shape strokes) to the application's styles, and
        /// Themes' control templates after the components' themes, which ThemeService merges after App.axaml's
        /// dictionaries (so Themes' PropertiesTabItemStyle wins over OxyPlotControls', as in the application).
        /// </summary>
        /// <returns>Dispose it to remove them.</returns>
        internal static IDisposable ApplyThemeDefaults()
        {
            var defaults = new StyleInclude(new Uri("avares://Themes/")) { Source = new Uri("avares://Themes/Resources/Defaults.axaml") };
            Application.Current.Styles.Add(defaults);
            var source = new Uri("avares://Themes/Resources/Controls/Merged.axaml");
            var templates = new ResourceInclude(source) { Source = source };
            Application.Current.Resources.MergedDictionaries.Add(templates);
            return new Removal(() =>
            {
                Application.Current.Resources.MergedDictionaries.Remove(templates);
                Application.Current.Styles.Remove(defaults);
            });
        }

        /// <summary>
        /// Sets the thread's culture to en-US, in which the examples were saved, as the file-compatibility tests do:
        /// upstream reads saved dates in the current culture (a project saved in en-US loses its dates in en-AU).
        /// </summary>
        /// <returns>Dispose it to restore the culture.</returns>
        internal static IDisposable UseExamplesCulture()
        {
            System.Globalization.CultureInfo culture = System.Globalization.CultureInfo.CurrentCulture, uiCulture = System.Globalization.CultureInfo.CurrentUICulture;
            System.Globalization.CultureInfo.CurrentCulture = System.Globalization.CultureInfo.CurrentUICulture = new System.Globalization.CultureInfo("en-US");
            return new Removal(() =>
            {
                System.Globalization.CultureInfo.CurrentCulture = culture;
                System.Globalization.CultureInfo.CurrentUICulture = uiCulture;
            });
        }

        private sealed class Removal : IDisposable
        {
            private Action _remove;

            internal Removal(Action remove) => _remove = remove;

            public void Dispose()
            {
                _remove?.Invoke();
                _remove = null;
            }
        }

        /// <summary>Shows a control in a window of the given size, and lets layout, bindings and rendering settle.</summary>
        /// <param name="content">The control.</param>
        /// <param name="width">The window's width.</param>
        /// <param name="height">The window's height.</param>
        /// <returns>The window; close it in a finally block.</returns>
        internal static Window Show(Control content, double width = 1200, double height = 800)
        {
            var window = new Window { Width = width, Height = height, Content = content };
            ReleaseContentOnClose(window);
            window.Show();
            Settle();
            return window;
        }

        /// <summary>
        /// Takes a window's content out once the window is closed. A closed window that is still in memory goes on
        /// receiving the application's resource changes (Avalonia's TopLevel listens to them weakly), so without this the
        /// next resource change, a test's cleanup removing its dictionaries say, re-resolved every resource of the
        /// closed views, at about a second a test.
        /// </summary>
        /// <param name="window">The window.</param>
        internal static void ReleaseContentOnClose(Window window) => window.Closed += (_, _) => window.Content = null;

        /// <summary>
        /// Lets the views a test closed go, and collects them, so that a run of every test stays small. The helpers call
        /// it where they close a test's projects and windows (OpenedProject's disposal, SharedControlsTesting's
        /// ReleaseAppResources, BestFitShell's Close and OpenExample). A control closed right after a state change
        /// stays with the animation clock until a render tick with real time elapsed, and the garbage collector's own
        /// full collections come late, after the views of several examples have piled up.
        /// </summary>
        internal static void Collect()
        {
            Dispatcher.UIThread.RunJobs();
            Thread.Sleep(50);
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            Dispatcher.UIThread.RunJobs();
            GC.Collect();
        }

        /// <summary>Runs the dispatcher's queued work and renders a frame, twice, so posted work and layout settle.</summary>
        internal static void Settle()
        {
            for (int i = 0; i < 2; i++)
            {
                Dispatcher.UIThread.RunJobs();
                AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            }
            Dispatcher.UIThread.RunJobs();
        }

        /// <summary>Records the property changes an object raises.</summary>
        /// <param name="source">The object.</param>
        /// <returns>The names of the changed properties, as they are raised.</returns>
        internal static List<string> RecordChanges(INotifyPropertyChanged source)
        {
            var names = new List<string>();
            source.PropertyChanged += (_, e) => names.Add(e.PropertyName);
            return names;
        }

        /// <summary>An opened copy of an example project.</summary>
        internal sealed class OpenedProject : IDisposable
        {
            private readonly string _folder;

            internal OpenedProject(string example)
            {
                _folder = Directory.CreateTempSubdirectory("xgoodfit-app-tests-").FullName;
                string copy = Path.Combine(_folder, Path.GetFileName(example));
                File.Copy(example, copy);
                Project = (BestFitProject)Activator.CreateInstance(typeof(BestFitProject), nonPublic: true);
                Project.FullFileName = copy;
                Project.Open();
            }

            /// <summary>The project.</summary>
            internal BestFitProject Project { get; }

            /// <summary>The project's elements of a type.</summary>
            /// <typeparam name="T">The element type.</typeparam>
            /// <returns>The elements.</returns>
            internal IEnumerable<T> Elements<T>() => ViewTesting.Elements(Project).OfType<T>();

            /// <summary>The project's element of a type with a name.</summary>
            /// <typeparam name="T">The element type.</typeparam>
            /// <param name="name">The element's name.</param>
            /// <returns>The element.</returns>
            internal T Element<T>(string name) where T : IElement => Elements<T>().Single(e => e.Name == name);

            /// <summary>
            /// Closes the project, has the Messenger forget its messages (which keep their sources, the elements, and
            /// so the project and the views on it), as the application does when it opens the next project
            /// (MainWindow.OpenProject), and deletes the copy.
            /// </summary>
            public void Dispose()
            {
                Project.Close();
                FrameworkInterfaces.Messaging.Messenger.GetInstance().Clear();
                Collect();
                SQLiteConnection.ClearAllPools();
                for (int attempt = 0; attempt < 20 && Directory.Exists(_folder); attempt++)
                {
                    try
                    {
                        Directory.Delete(_folder, recursive: true);
                    }
                    catch (Exception ex) when (ex is IOException || ex is UnauthorizedAccessException)
                    {
                        GC.Collect();
                        GC.WaitForPendingFinalizers();
                        Thread.Sleep(100);
                    }
                }
            }
        }
    }
}
