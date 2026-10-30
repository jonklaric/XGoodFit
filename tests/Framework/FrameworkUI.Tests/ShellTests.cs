using AvalonDock;
using AvalonDock.Layout;
using AvalonDock.Layout.Serialization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;

namespace FrameworkUI.Tests;

/// <summary>
/// The shell's foundations: FrameworkUI's palettes layered over Themes', and AvalonDock layout XML,
/// which projects store, read and written unchanged.
/// </summary>
[TestClass]
public sealed class ShellTests
{
    // The dock layout a sample RMC-BestFit project (usgs-download-example.bestfit) stores, as WPF's
    // AvalonDock wrote it: pane groups, pixel and star sizes, documents and activation times.
    internal const string StoredLayout = """
        <?xml version="1.0" encoding="utf-8"?>
        <LayoutRoot>
          <RootPanel Orientation="Horizontal">
            <LayoutAnchorablePaneGroup Orientation="Vertical" DockWidth="340">
              <LayoutAnchorablePane Id="2f3289cd-1c9d-4348-b6fa-151afdb1cc84" DockWidth="340" FloatingWidth="960" FloatingHeight="1040">
                <LayoutAnchorable AutoHideWidth="250" AutoHideHeight="250" AutoHideMinWidth="100" AutoHideMinHeight="100" CanDockAsTabbedDocument="False" Title="  Project Explorer" IsSelected="True" ContentId="ProjectExplorer" FloatingWidth="960" FloatingHeight="1040" CanClose="False" LastActivationTimeStamp="08/19/2026 15:54:21" />
              </LayoutAnchorablePane>
            </LayoutAnchorablePaneGroup>
            <LayoutPanel Orientation="Vertical" DockWidth="0.7805035506778567*">
              <LayoutPanel Orientation="Horizontal" DockHeight="0.825964252116651*">
                <LayoutDocumentPaneGroup Orientation="Horizontal">
                  <LayoutDocumentPane Id="fa7f14a5-c146-44a6-a506-b8ca7829a3ce">
                    <LayoutDocument Title="USGS - 01134500 - Daily Discharge" IsSelected="True" IsLastFocusedDocument="True" ContentId="Time Series Data" CanClose="True" LastActivationTimeStamp="08/19/2026 15:54:21" />
                    <LayoutDocument Title="USGS - 07010000 - Daily Stage" ContentId="Time Series Data" CanClose="True" LastActivationTimeStamp="08/19/2026 15:54:19" />
                  </LayoutDocumentPane>
                </LayoutDocumentPaneGroup>
              </LayoutPanel>
              <LayoutAnchorablePane Id="fe3710fa-2211-46e1-98c2-96c4a12486d0" DockHeight="185">
                <LayoutAnchorable AutoHideWidth="200" AutoHideHeight="200" AutoHideMinWidth="100" AutoHideMinHeight="100" CanDockAsTabbedDocument="False" Title="  Message Window" IsSelected="True" ContentId="MessageWindow" CanClose="False" LastActivationTimeStamp="02/16/2026 14:15:40" />
              </LayoutAnchorablePane>
            </LayoutPanel>
            <LayoutAnchorablePane Id="c99d649e-0810-4daf-bf8e-14b4a20f19d4" DockWidth="354.8" FloatingWidth="310" FloatingHeight="957" FloatingLeft="1889" FloatingTop="554">
              <LayoutAnchorable AutoHideWidth="250" AutoHideHeight="250" AutoHideMinWidth="100" AutoHideMinHeight="100" CanDockAsTabbedDocument="False" Title="  Properties" IsSelected="True" ContentId="PropertiesWindow" FloatingLeft="1889" FloatingTop="554" FloatingWidth="310" FloatingHeight="957" CanClose="False" LastActivationTimeStamp="07/15/2026 13:19:15" />
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

    private static string Lines(string xml) => xml.ReplaceLineEndings("\n").Trim();

    [TestMethod]
    public void SetTheme_LayersFrameworkUIsPaletteOverThemes()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            int merged = Application.Current!.Resources.MergedDictionaries.Count;

            ThemeManager.SetTheme(ThemeColor.Dark);
            Assert.AreEqual(ThemeColor.Dark, ThemeManager.CurrentThemeColor);
            Assert.AreEqual(merged, Application.Current.Resources.MergedDictionaries.Count, "the old palette is replaced, not added to");

            // FrameworkUI's Dark palette overrides some of Themes' keys (an upstream quirk, kept) and
            // adds the colour picker's.
            Assert.AreEqual(Color.Parse("#C9DEF5"), Brush("TabItem.MouseOver.Background"));
            Assert.AreEqual(Color.Parse("#3F3F46"), Brush("Button.Static.Border"));
            Assert.IsNotNull(Application.Current.FindResource("ColorPicker.Background"));

            ThemeManager.SetTheme(ThemeColor.Light);
            Assert.AreEqual(Color.Parse("#CCCEDB"), Brush("Button.Static.Border"));
        });
    }

    private static Color Brush(string key) => ((ISolidColorBrush)Application.Current!.FindResource(key)!).Color;

    [TestMethod]
    public void DefaultLayout_HasTheThreeToolWindows()
    {
        HeadlessSession.Run(() =>
        {
            var manager = Load(Properties.Resources.AvalonDock, out Window window);
            CollectionAssert.AreEqual(
                new[] { ShellPublicVariables.ProjectExplorerContentID, ShellPublicVariables.MessageWindowContentID, ShellPublicVariables.PropertiesWindowContentID },
                manager.Layout.Descendents().OfType<LayoutAnchorable>().Select(a => a.ContentId).ToArray());
            Assert.AreEqual(1, manager.Layout.Descendents().OfType<LayoutDocumentPane>().Count());
            window.Close();
        });
    }

    [TestMethod]
    public void StoredLayout_SavesBackUnchanged()
    {
        HeadlessSession.Run(() =>
        {
            var manager = Load(StoredLayout, out Window window);
            Assert.AreEqual(2, manager.Layout.Descendents().OfType<LayoutDocument>().Count());

            // As MainWindow.SaveLayout: serialize to a file, then read its bytes.
            string file = Path.Combine(Path.GetTempPath(), $"xgf-layout-{Guid.NewGuid():N}.xml");
            try
            {
                new XmlLayoutSerializer(manager).Serialize(file);
                string saved = UtilityFunctions.UTF8BytesToString(File.ReadAllBytes(file));
                Assert.AreEqual(Lines(StoredLayout), Lines(saved));
            }
            finally
            {
                File.Delete(file);
                window.Close();
            }
        });
    }

    private static DockingManager Load(string xml, out Window window)
    {
        var manager = new DockingManager();
        window = new Window { Width = 1300, Height = 700, Content = manager };
        window.Show();
        var serializer = new XmlLayoutSerializer(manager);
        serializer.LayoutSerializationCallback += (_, e) => e.Content = new TextBlock { Text = e.Model.Title };
        serializer.Deserialize(new StringReader(xml));
        Dispatcher.UIThread.RunJobs();
        return manager;
    }
}

/// <summary>The Terms and Conditions window: agreeing and pressing OK closes it with the agreement.</summary>
[TestClass]
public sealed class TermsAndConditionsTests
{
    [TestMethod]
    public void ReadingToTheEnd_AndAgreeing_ClosesWithTheAgreement()
    {
        HeadlessSession.Run(() =>
        {
            ThemeManager.SetTheme(ThemeColor.Light);
            var window = new TermsAndConditionsWindow();
            bool? result = null;
            bool enabledAtTheEnd = false;
            Dispatcher.UIThread.Post(() =>
            {
                window.TCURichTextBox.ScrollToEnd();
                Dispatcher.UIThread.RunJobs();
                window.UpdateLayout();
                enabledAtTheEnd = window.IAgreeCheckbox.IsEnabled;
                window.IAgreeCheckbox.IsChecked = true;
                window.OKButton.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            }, DispatcherPriority.Background);
            result = WpfCompat.DialogWindows.ShowDialog(window);

            Assert.IsTrue(window.TCURichTextBox.ExtentHeight > window.TCURichTextBox.ViewportHeight, "the default terms are longer than the box");
            Assert.IsTrue(enabledAtTheEnd, "reading to the end enables the agreement");
            Assert.IsTrue(window.UserAgreed);
            Assert.IsTrue(result);
        });
    }
}
