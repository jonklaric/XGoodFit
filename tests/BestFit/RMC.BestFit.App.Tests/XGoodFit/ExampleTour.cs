using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AvalonDock;
using AvalonDock.Layout;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkInterfaces.Messaging;
using FrameworkUI;
using FrameworkUI.ProjectExplorer;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.UI;
using RMC_BestFit;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit's end-to-end tour of RMC-BestFit's example projects: an example opened in the main window, as a
    /// user opens it, then each of its elements in the project explorer's order. For each element: a double click
    /// on its node opens its document, with its properties beside it; every tab of both is shown; the window is
    /// captured to artifacts/screenshots/examples/&lt;example&gt;/; and the document is closed again. Looking
    /// changes nothing: no element becomes dirty, gains an undo step or loses its estimate, the Message Window
    /// gains no errors, and no dialog opens. Views not ported yet (placeholders) are skipped and listed.
    /// </summary>
    /// <remarks>
    /// The test classes, ExampleTour*Tests (one per group of examples, in category Examples), run with the Tour
    /// group of scripts/run-app-tests.sh, each in its own process, and in CI's job of their own on every OS.
    /// </remarks>
    internal static class ExampleTour
    {
        /// <summary>The test category of the tour's tests.</summary>
        public const string Category = "Examples";

        /// <summary>The file in artifacts/logs/app-tests/ that lists the elements skipped because they aren't ported yet.</summary>
        public const string NotPortedFile = "tour-not-ported.txt";

        /// <summary>The text of a view that isn't ported yet (GUI/Support/PortPlaceholder.cs).</summary>
        private const string PlaceholderText = "isn't ported to XGoodFit yet";

        /// <summary>Tours the example project named <paramref name="example"/>.</summary>
        /// <returns>The elements whose views aren't ported yet, by name.</returns>
        public static IReadOnlyList<string> Run(string example)
        {
            var skipped = new List<string>();
            BestFitShell.Start();
            MainWindow window = null;
            try
            {
                (window, MainProjectNode node, BestFitProject project) = BestFitShell.Open(BestFitShell.Example(example));
                Assert.IsFalse(project.IsDirty, example + ": opening changes nothing");
                var elements = project.ElementCollections.SelectMany(collection => collection.ToList()).ToList();
                Assert.AreNotEqual(0, elements.Count, example + " has elements");
                for (int i = 0; i < elements.Count; i++)
                {
                    if (!Visit(window, node, elements[i], example, i + 1))
                        skipped.Add(elements[i].Name);
                }
                Assert.IsFalse(project.IsDirty, example + ": touring it changed nothing");
            }
            finally
            {
                BestFitShell.Close(window);
                // The shell clears the Message Window when it opens a project (MainWindow.OpenProject).
                Messenger.GetInstance().Clear();
            }
            if (skipped.Count > 0)
            {
                // The test platform shows passing tests' output only on request, so the list goes to a file that
                // scripts/run-app-tests.sh prints after the tour.
                string line = $"{example}: not ported yet, skipped: {string.Join(", ", skipped)}";
                Console.WriteLine(line);
                string logs = Path.Combine(BestFitShell.RepositoryRoot, "artifacts", "logs", "app-tests");
                Directory.CreateDirectory(logs);
                File.AppendAllText(Path.Combine(logs, NotPortedFile), line + Environment.NewLine);
            }
            return skipped;
        }

        /// <summary>Opens one element's document and properties, shows their tabs, captures them and closes them.</summary>
        /// <returns>False when its view isn't ported yet.</returns>
        private static bool Visit(MainWindow window, MainProjectNode node, IElement element, string example, int index)
        {
            string name = example + ": " + element.Name;
            bool? estimated = Estimated(element);
            Messenger messenger = Messenger.GetInstance();
            var errorsBefore = messenger.AllMessageItems().Where(m => m.Type == MessageType.Error).ToHashSet();

            // Any window but the main one is a dialog nobody expects: note it and close it, so it can't wait.
            var dialogs = new List<string>();
            using IDisposable watch = Window.WindowOpenedEvent.AddClassHandler<Window>((opened, _) =>
            {
                if (opened is MainWindow) return;
                dialogs.Add(opened.Title ?? opened.GetType().Name);
                Dispatcher.UIThread.Post(opened.Close, DispatcherPriority.Background);
            });

            ElementNode elementNode = ElementNode.FindElementNode(element, node);
            Assert.IsNotNull(elementNode, name + " is in the project explorer");
            window.DoubleClick(elementNode.NodeHeader);
            BestFitShell.Frame();
            LayoutDocument document = Documents(window).SingleOrDefault(d => d.Content is Control c && ReferenceEquals(node.GetControlElement(c), element));
            Assert.IsNotNull(document, name + ": its document opened");
            var view = (Control)document.Content;

            bool ported = !Shows(view, PlaceholderText);
            if (ported)
            {
                ShowEveryTab(view);
                Control properties = PropertiesPane(window)?.Content as Control;
                Assert.IsNotNull(properties, name + ": its properties are shown");
                Assert.AreSame(element, node.GetControlElement(properties), name + ": the properties are its own");
                ShowEveryTab(properties);
                Directory.CreateDirectory(Path.Combine(BestFitShell.RepositoryRoot, "artifacts", "screenshots", "examples", example));
                BestFitShell.Capture(window, $"examples/{example}/{index:00}-{FileName(element.Name)}");
            }

            document.Close();
            BestFitShell.Frame();
            Assert.IsFalse(Documents(window).Contains(document), name + ": its document closed");

            Assert.AreEqual(0, dialogs.Count, name + ": no dialog opened, but " + string.Join(", ", dialogs));
            Assert.IsFalse(element.IsDirty, name + " is unchanged");
            if (element is ElementBase elementBase)
                Assert.IsFalse(elementBase.UndoManager.CanUndo, name + " has nothing to undo");
            Assert.AreEqual(estimated, Estimated(element), name + " is as estimated as it was");
            var errors = messenger.AllMessageItems().Where(m => m.Type == MessageType.Error && !errorsBefore.Contains(m)).ToList();
            Assert.AreEqual(0, errors.Count, name + ": no new errors, but " + string.Join(" | ", errors.Select(m => m.Description)));
            return ported;
        }

        /// <summary>
        /// Shows every tab of <paramref name="root"/>, and of the tab controls those tabs show, in turn, leaving
        /// each tab control on its first tab again.
        /// </summary>
        private static void ShowEveryTab(Control root)
        {
            var shown = new HashSet<TabItem>();
            var firsts = new List<(TabControl Tabs, object Item)>();
            for (int i = 0; i < 200; i++)
            {
                TabItem next = root.GetVisualDescendants().OfType<TabItem>()
                    .FirstOrDefault(tab => !shown.Contains(tab) && tab.IsEffectivelyVisible && tab.IsEffectivelyEnabled);
                if (next == null) break;
                shown.Add(next);
                if (next.Parent is TabControl tabs && !firsts.Any(f => f.Tabs == tabs))
                    firsts.Add((tabs, tabs.SelectedItem));
                next.IsSelected = true;
                BestFitShell.Frame();
            }
            foreach (var (tabs, item) in Enumerable.Reverse(firsts))
                tabs.SelectedItem = item;
            BestFitShell.Frame();
        }

        /// <summary>Whether a text block under <paramref name="root"/> shows <paramref name="text"/>.</summary>
        private static bool Shows(Control root, string text) =>
            root.GetVisualDescendants().OfType<TextBlock>().Any(block => block.Text?.Contains(text, StringComparison.Ordinal) == true);

        /// <summary>An analysis element's IsEstimated, or null for elements without one.</summary>
        private static bool? Estimated(IElement element) =>
            element.GetType().GetProperty("IsEstimated")?.GetValue(element) as bool?;

        private static List<LayoutDocument> Documents(MainWindow window) =>
            window.FindControl<DockingManager>("MainDock").Layout.Descendents().OfType<LayoutDocument>().ToList();

        private static LayoutAnchorable PropertiesPane(MainWindow window) =>
            window.FindControl<DockingManager>("MainDock").Layout.Descendents().OfType<LayoutAnchorable>()
                .SingleOrDefault(pane => pane.ContentId == ShellPublicVariables.PropertiesWindowContentID);

        /// <summary>An element's name as a file name on every OS.</summary>
        private static string FileName(string name)
        {
            char[] invalid = Path.GetInvalidFileNameChars().Concat(new[] { '<', '>', ':', '"', '/', '\\', '|', '?', '*' }).ToArray();
            string clean = new string(name.Select(c => invalid.Contains(c) ? '-' : c).ToArray()).Trim();
            return clean.Length > 80 ? clean.Substring(0, 80) : clean;
        }
    }
}
