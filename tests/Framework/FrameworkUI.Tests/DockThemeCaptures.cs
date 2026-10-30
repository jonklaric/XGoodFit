using AvalonDock;
using AvalonDock.Controls;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace FrameworkUI.Tests;

/// <summary>
/// Renders the shell's docking chrome in every theme to <c>artifacts/screenshots</c>, to compare with
/// upstream's gallery screenshots (<c>upstream/WPF-Framework/docs/assets/gallery/frameworkui-shell-*.png</c>):
/// <c>dock-shell-*.png</c> has the screenshots' layout and pane sizes with the Project Explorer active,
/// <c>dock-shell-*-document-active.png</c> the same with the document active, as the Dark and Blue
/// screenshots have it. The other files show states the screenshots don't. Run them alone with
/// <c>FrameworkUI.Tests --filter "TestCategory=Capture"</c>.
/// </summary>
[TestClass]
public sealed class DockThemeCaptures
{
    // Upstream's default layout with the pane sizes and documents of the gallery screenshots: a 1300 x 700
    // window whose dock area starts 80 pixels down, below the title bar, menu and tool bar.
    private const string CaptureLayout = """
        <?xml version="1.0" encoding="utf-8"?>
        <LayoutRoot>
          <RootPanel Orientation="Horizontal">
            <LayoutAnchorablePane DockWidth="180">
              <LayoutAnchorable AutoHideWidth="250" AutoHideHeight="250" AutoHideMinWidth="100" AutoHideMinHeight="100" CanDockAsTabbedDocument="False" Title="  Project Explorer" IsSelected="True" ContentId="ProjectExplorer" CanClose="False" />
            </LayoutAnchorablePane>
            <LayoutPanel Orientation="Vertical">
              <LayoutPanel Orientation="Horizontal">
                <LayoutDocumentPaneGroup Orientation="Horizontal">
                  <LayoutDocumentPane>
                    <LayoutDocument Title="Example Hazard" IsSelected="True" IsLastFocusedDocument="True" ContentId="Hazard" CanClose="True" />
                    <LayoutDocument Title="Hazard Functions" ContentId="Functions" CanClose="True" />
                  </LayoutDocumentPane>
                </LayoutDocumentPaneGroup>
              </LayoutPanel>
              <LayoutAnchorablePane DockHeight="110">
                <LayoutAnchorable AutoHideWidth="200" AutoHideHeight="200" AutoHideMinWidth="100" AutoHideMinHeight="100" CanDockAsTabbedDocument="False" Title="  Message Window" IsSelected="True" ContentId="MessageWindow" CanClose="False" />
              </LayoutAnchorablePane>
            </LayoutPanel>
            <LayoutAnchorablePane DockWidth="347">
              <LayoutAnchorable AutoHideWidth="250" AutoHideHeight="250" AutoHideMinWidth="100" AutoHideMinHeight="100" CanDockAsTabbedDocument="False" Title="  Properties" IsSelected="True" ContentId="PropertiesWindow" CanClose="False" />
            </LayoutAnchorablePane>
          </RootPanel>
          <TopSide />
          <RightSide />
          <LeftSide />
          <BottomSide />
          <FloatingWindows />
          <Hidden />
        </LayoutRoot>
        """;

    // States the gallery screenshots don't show: two tools in a pane (its tabs), three documents, and
    // tool windows auto-hidden on the right and at the bottom.
    private const string StatesLayout = """
        <?xml version="1.0" encoding="utf-8"?>
        <LayoutRoot>
          <RootPanel Orientation="Horizontal">
            <LayoutAnchorablePane DockWidth="180">
              <LayoutAnchorable CanDockAsTabbedDocument="False" Title="  Project Explorer" IsSelected="True" ContentId="ProjectExplorer" CanClose="False" />
              <LayoutAnchorable CanDockAsTabbedDocument="False" Title="  Toolbox" ContentId="Toolbox" CanClose="False" />
            </LayoutAnchorablePane>
            <LayoutDocumentPaneGroup Orientation="Horizontal">
              <LayoutDocumentPane>
                <LayoutDocument Title="Example Hazard" IsSelected="True" IsLastFocusedDocument="True" ContentId="Hazard" CanClose="True" />
                <LayoutDocument Title="Hazard Functions" ContentId="Functions" CanClose="True" />
                <LayoutDocument Title="Analysis" ContentId="Analysis" CanClose="True" />
              </LayoutDocumentPane>
            </LayoutDocumentPaneGroup>
          </RootPanel>
          <TopSide />
          <RightSide>
            <LayoutAnchorGroup>
              <LayoutAnchorable AutoHideWidth="250" AutoHideHeight="250" CanDockAsTabbedDocument="False" Title="  Properties" ContentId="PropertiesWindow" CanClose="False" />
            </LayoutAnchorGroup>
          </RightSide>
          <LeftSide />
          <BottomSide>
            <LayoutAnchorGroup>
              <LayoutAnchorable AutoHideWidth="200" AutoHideHeight="200" CanDockAsTabbedDocument="False" Title="  Message Window" ContentId="MessageWindow" CanClose="False" />
            </LayoutAnchorGroup>
          </BottomSide>
          <FloatingWindows />
          <Hidden />
        </LayoutRoot>
        """;

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureShell_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            string folder = ScreenshotFolder();
            foreach (ThemeColor theme in new[] { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue })
            {
                ThemeManager.SetTheme(theme);
                string name = theme.ToString().ToLowerInvariant();
                foreach (bool documentActive in new[] { false, true })
                {
                    Window window = CreateShell(theme, CaptureLayout, out DockingManager manager);
                    LayoutContent active = documentActive
                        ? manager.Layout.Descendents().OfType<LayoutDocument>().First()
                        : manager.Layout.Descendents().OfType<LayoutAnchorable>().First(a => a.ContentId == ShellPublicVariables.ProjectExplorerContentID);
                    active.IsActive = true;
                    Save(window, Path.Combine(folder, documentActive ? $"dock-shell-{name}-document-active.png" : $"dock-shell-{name}.png"));
                    window.Close();
                }
            }
            ThemeManager.SetTheme(ThemeColor.Light);
        });
    }

    [TestMethod]
    [TestCategory("Capture")]
    public void CaptureStates_InEveryTheme()
    {
        HeadlessSession.Run(() =>
        {
            string folder = ScreenshotFolder();
            foreach (ThemeColor theme in new[] { ThemeColor.Light, ThemeColor.Dark, ThemeColor.Blue })
            {
                ThemeManager.SetTheme(theme);
                string name = theme.ToString().ToLowerInvariant();
                Window window = CreateShell(theme, StatesLayout, out DockingManager manager);
                manager.Layout.Descendents().OfType<LayoutDocument>().First().IsActive = true;
                Settle();

                // An unselected document tab under the pointer.
                window.MouseMove(Centre(window, FindTab(window, "Hazard Functions")));
                Save(window, Path.Combine(folder, $"dock-states-{name}.png"));

                // The pointer on the selected tab's close button, then on the Project Explorer's pin.
                window.MouseMove(Centre(window, CloseButton(FindTab(window, "Example Hazard"))));
                Save(window, Path.Combine(folder, $"dock-hover-close-{name}.png"));
                window.MouseMove(Centre(window, window.GetVisualDescendants().OfType<Button>().First(b => b.Name == "PART_PinButton")));
                Save(window, Path.Combine(folder, $"dock-hover-pin-{name}.png"));

                // The navigator (Ctrl+Tab).
                var navigator = new NavigatorWindow(manager);
                navigator.Show(window);
                Save(navigator, Path.Combine(folder, $"dock-navigator-{name}.png"));
                navigator.Close();

                // An auto-hidden tool window shown from its tab.
                window.MouseMove(new Point(5, 5));
                LayoutAnchorable properties = manager.Layout.Descendents().OfType<LayoutAnchorable>().First(a => a.ContentId == ShellPublicVariables.PropertiesWindowContentID);
                properties.IsActive = true;
                Save(window, Path.Combine(folder, $"dock-autohide-{name}.png"));

                // A floating tool window.
                manager.Layout.Descendents().OfType<LayoutAnchorable>().First(a => a.ContentId == "Toolbox").Float();
                Settle();
                foreach (Window floating in manager.FloatingWindows.OfType<Window>())
                {
                    Save(floating, Path.Combine(folder, $"dock-floating-{name}.png"));
                }
                window.Close();

                // The docking targets, while the Properties window is dragged over the documents.
                window = CreateShell(theme, CaptureLayout, out manager);
                Grid grip = window.GetVisualDescendants().OfType<Grid>().Last(g => g.Name == "PART_Grip");
                Point start = grip.TranslatePoint(new Point(150, 10), window)!.Value;
                window.MouseMove(start);
                window.MouseDown(start, MouseButton.Left);
                for (int step = 1; step <= 20; step++)
                {
                    window.MouseMove(new Point(start.X - step * 40, start.Y + step * 12), RawInputModifiers.LeftMouseButton);
                    Dispatcher.UIThread.RunJobs();
                }
                Save(window, Path.Combine(folder, $"dock-targets-{name}.png"));
                window.Close();
            }
            ThemeManager.SetTheme(ThemeColor.Light);
        });
    }

    // A window laid out as upstream's MainWindow (a 2-pixel margin around the docking manager, below 79
    // pixels for the menu and tool bar), with the given layout in the theme.
    private static Window CreateShell(ThemeColor theme, string layout, out DockingManager manager)
    {
        manager = new DockingManager
        {
            AllowMixedOrientation = true,
            BorderBrush = Brushes.Transparent,
            BorderThickness = new Thickness(0),
            Margin = new Thickness(2),
            // As upstream's MainWindow: the document's icon and title.
            DocumentHeaderTemplate = new FuncDataTemplate<LayoutContent>((_, _) => new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Children =
                {
                    new Image { [!Image.SourceProperty] = new Binding(nameof(LayoutContent.IconSource)), Margin = new Thickness(0, 0, 4, 0) },
                    new TextBlock { [!TextBlock.TextProperty] = new Binding(nameof(LayoutContent.Title)) },
                },
            }),
        };
        DockThemeSwitcher.Apply(manager, theme);
        var root = new DockPanel();
        var top = new Border { Height = 79 };
        DockPanel.SetDock(top, Avalonia.Controls.Dock.Top);
        root.Children.Add(top);
        root.Children.Add(manager);
        var window = new Window
        {
            Width = 1300,
            Height = 700,
            Content = new Border { BorderThickness = new Thickness(1), Child = root },
        };
        window.Show();

        var serializer = new XmlLayoutSerializer(manager);
        serializer.LayoutSerializationCallback += (_, e) => e.Content = new TextBlock
        {
            Text = e.Model.Title?.Trim(),
            Margin = new Thickness(8),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Top,
        };
        serializer.Deserialize(new StringReader(layout));
        foreach (LayoutDocument document in manager.Layout.Descendents().OfType<LayoutDocument>())
        {
            document.IconSource = (IImage?)Application.Current!.FindResource("ProjectPropertiesImage");
        }
        Dispatcher.UIThread.RunJobs();
        return window;
    }

    private static Dock.Avalonia.Controls.DocumentTabStripItem FindTab(Window window, string title) =>
        window.GetVisualDescendants().OfType<Dock.Avalonia.Controls.DocumentTabStripItem>()
            .First(t => t.GetVisualDescendants().OfType<TextBlock>().Any(b => b.Text == title));

    private static Button CloseButton(Dock.Avalonia.Controls.DocumentTabStripItem tab) =>
        tab.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.ContentPresenter>().First(p => p.Name == "PART_ClosePresenter")
            .GetVisualDescendants().OfType<Button>().First();

    private static Point Centre(Visual root, Visual visual) =>
        visual.TranslatePoint(new Point(visual.Bounds.Width / 2, visual.Bounds.Height / 2), root) ?? default;

    private static void Save(Window window, string path)
    {
        Settle();
        using var frame = window.CaptureRenderedFrame() ?? throw new InvalidOperationException("No frame was rendered.");
        frame.Save(path, new PngBitmapEncoderOptions());
    }

    // Lets layout run and animations finish: the headless render timer uses real time.
    private static void Settle()
    {
        Dispatcher.UIThread.RunJobs();
        Thread.Sleep(200);
        AvaloniaHeadlessPlatform.ForceRenderTimerTick();
        Dispatcher.UIThread.RunJobs();
    }

    // artifacts/screenshots under the repository root (git-ignored).
    private static string ScreenshotFolder()
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
