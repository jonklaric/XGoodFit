using AvalonDock.Layout;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;
using FrameworkInterfaces;
using FrameworkUI.ProjectExplorer;

namespace FrameworkUI.Tests;

/// <summary>
/// Renders the shell in every theme to <c>artifacts/screenshots</c>, to compare with upstream's gallery
/// screenshots (<c>upstream/WPF-Framework/docs/assets/gallery/frameworkui-shell-*.png</c>) and the
/// examples' (<c>sample-data/RMC-BestFit/examples/**/screenshots</c>): <c>shell-*.png</c> is the main
/// window with a project, a document, messages and the properties window; the other files show its
/// menus, the explorer's context menu and the dialogs. Run them alone with
/// <c>FrameworkUI.Tests --filter "TestCategory=Capture"</c>.
/// </summary>
[TestClass]
public sealed class ShellCaptures
{
    private static readonly ThemeColor[] Themes = [ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue];

    // The project upstream's gallery screenshot shows: one collection with one element.
    internal static TestProject GalleryProject() => new(
        Path.Combine(Path.GetTempPath(), "Framework Gallery.test"),
        ("Hazard Functions", ["Example Hazard"])) { Name = "Framework Gallery" };

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureShell_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            string folder = ScreenshotFolder();
            foreach (ThemeColor theme in Themes)
            {
                // As upstream's applications: the theme is set before the main window is created.
                ThemeManager.SetTheme(theme);
                Shell.UseTemporaryFolder(() => UserSettings.ColorTheme = theme.ToString());
                var (window, controller) = Shell.Open(GalleryProject());
                try
                {
                    // Messages: as in the gallery screenshot, events the window shows once asked to.
                    var messenger = FrameworkInterfaces.Messaging.Messenger.GetInstance();
                    messenger.Clear();
                    messenger.Add(new BasicMessageItem(MessageType.Event, "The project 'Framework Gallery' was opened.", controller.Project, "Project", "Framework Gallery", "OpenProject"));
                    // The element's document, opened by a double click on its node.
                    var element = controller.Project.ElementCollections![0][0];
                    ElementNode node = ElementNode.FindElementNode(element, controller)!;
                    window.DoubleClick(node.NodeHeader);
                    Settle();
                    var document = window.MainDock.Layout.Descendents().OfType<LayoutDocument>().Single();
                    // Then a click in the document, which takes the focus from the explorer: its selected
                    // node is highlighted while the explorer doesn't have the focus, as in the gallery's.
                    window.Click((TestDocument)document.Content!);
                    Save(window, Path.Combine(folder, $"shell-{theme.ToString().ToLowerInvariant()}.png"));
                }
                finally
                {
                    Shell.Close(window);
                }
            }
            ThemeManager.SetTheme(ThemeColor.Light);
        });
    }

    internal static void Save(TopLevel topLevel, string path)
    {
        Settle();
        using var frame = topLevel.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
    }

    // Lets layout run and animations finish: the headless render timer uses real time.
    internal static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(200);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // artifacts/screenshots under the repository root (git-ignored).
    internal static string ScreenshotFolder()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "XGoodFit.slnx")))
        {
            directory = directory.Parent;
        }
        string folder = Path.Combine(directory?.FullName ?? AppContext.BaseDirectory, "artifacts", "screenshots");
        Directory.CreateDirectory(folder);
        return folder;
    }
}
