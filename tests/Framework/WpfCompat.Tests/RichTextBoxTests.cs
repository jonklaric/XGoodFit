using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Documents;
using Avalonia.Headless;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using WpfCompat.Documents;
using RichTextBox = WpfCompat.Controls.RichTextBox;

namespace WpfCompat.Tests;

/// <summary>WpfCompat's read-only RichTextBox and its FlowDocument, as the ported code uses them.</summary>
[TestClass]
public sealed class RichTextBoxTests
{
    [TestMethod]
    public void Document_ShowsEachParagraph_AndTextRangeReadsAndReplacesIt()
    {
        HeadlessSession.Run(() =>
        {
            var document = new FlowDocument();
            document.Blocks.Add(new Documents.Paragraph(new Documents.Run("Heading")) { FontSize = 18, Margin = new Avalonia.Thickness(0) });
            document.Blocks.Add(new Documents.Paragraph(new Documents.Run("Body text.")) { Margin = new Avalonia.Thickness(0, 0, 0, 10) });
            var box = new RichTextBox { Width = 300, Height = 200, Document = document, IsReadOnly = true };
            var window = new Window { Content = box };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var blocks = box.GetVisualDescendants().OfType<SelectableTextBlock>().ToList();
            Assert.AreEqual(2, blocks.Count);
            Assert.AreEqual(18, blocks[0].FontSize);
            Assert.AreEqual("Body text.", blocks[1].Inlines!.OfType<Avalonia.Controls.Documents.Run>().Single().Text);

            var range = new TextRange(document.ContentStart, document.ContentEnd);
            Assert.AreEqual("Heading" + Environment.NewLine + "Body text." + Environment.NewLine, range.Text);
            range.Text = "Raw USGS text";
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(1, box.GetVisualDescendants().OfType<SelectableTextBlock>().Count(), "setting the range's text replaces the document");
            window.Close();
        });
    }

    [TestMethod]
    public void PageWidth_LaysTheTextOutThatWide_AndScrollsItSideways()
    {
        HeadlessSession.Run(() =>
        {
            // BestFit's MCMC and GMM reports: a monospaced report on a 1200-pixel page, in a narrower box.
            string line = string.Join(" ", Enumerable.Repeat("column", 40));
            var document = new FlowDocument { PageWidth = 1200 };
            document.Blocks.Add(new Documents.Paragraph(new Documents.Run(line + "\n" + line)));
            var box = new RichTextBox
            {
                Width = 400, Height = 200, Document = document, IsReadOnly = true,
                HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            };
            var window = new Window { Content = box };
            window.Show();
            Dispatcher.UIThread.RunJobs();

            var block = box.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
            Assert.AreEqual(1200 - 10, block.Bounds.Width, "the text is laid out on the page, inside its padding");
            Assert.AreEqual(1200, box.ExtentWidth, 0.5, "the page scrolls sideways");
            Assert.IsTrue(box.ViewportWidth < box.ExtentWidth);
            var viewer = box.GetVisualDescendants().OfType<ScrollViewer>().First();
            Assert.AreEqual(Avalonia.Controls.Primitives.ScrollBarVisibility.Auto, viewer.HorizontalScrollBarVisibility, "the box's scroll bar setting applies");

            document.PageWidth = double.NaN;
            box.Document = new FlowDocument(new Documents.Paragraph(new Documents.Run(line)));
            Dispatcher.UIThread.RunJobs();
            block = box.GetVisualDescendants().OfType<SelectableTextBlock>().Single();
            Assert.IsTrue(block.Bounds.Width <= box.ViewportWidth, "without a page width the text wraps to the box");
            window.Close();
        });
    }

    [TestMethod]
    public void Hyperlink_IsClickable_OnlyWhileTheDocumentIsEnabled()
    {
        HeadlessSession.Run(() =>
        {
            var link = new Hyperlink(new Documents.Run("Zenodo")) { NavigateUri = new Uri("https://zenodo.org/") };
            Uri? navigated = null;
            link.RequestNavigate += (_, e) => navigated = e.Uri;
            var box = new RichTextBox { Width = 300, Height = 100, Document = new FlowDocument(new Documents.Paragraph(link)) };
            var window = new Window { Width = 300, Height = 100, Content = box };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            Assert.AreEqual(0, box.GetVisualDescendants().OfType<TextBlock>().Count(t => t is not SelectableTextBlock), "not clickable: no link control");

            box.IsDocumentEnabled = true;
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            var linkText = box.GetVisualDescendants().OfType<TextBlock>().Single(t => t is not SelectableTextBlock);
            var center = linkText.TranslatePoint(new Avalonia.Point(linkText.Bounds.Width / 2, linkText.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            Assert.AreEqual(new Uri("https://zenodo.org/"), navigated);
            window.Close();
        });
    }

    [TestMethod]
    public void Hyperlink_DoesNotNavigate_WhileDisabled()
    {
        HeadlessSession.Run(() =>
        {
            var link = new Hyperlink(new Documents.Run("Zenodo")) { NavigateUri = new Uri("https://zenodo.org/"), IsEnabled = false };
            int navigations = 0;
            link.RequestNavigate += (_, e) => navigations++;
            var box = new RichTextBox { Width = 300, Height = 100, Document = new FlowDocument(new Documents.Paragraph(link)), IsDocumentEnabled = true };
            var window = new Window { Width = 300, Height = 100, Content = box };
            window.Show();
            Dispatcher.UIThread.RunJobs();
            window.UpdateLayout();
            AvaloniaHeadlessPlatform.ForceRenderTimerTick();
            var linkText = box.GetVisualDescendants().OfType<TextBlock>().Single(t => t is not SelectableTextBlock);
            var center = linkText.TranslatePoint(new Avalonia.Point(linkText.Bounds.Width / 2, linkText.Bounds.Height / 2), window)!.Value;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            Assert.AreEqual(0, navigations, "a disabled link doesn't navigate, as WPF's");

            link.IsEnabled = true;
            window.MouseDown(center, MouseButton.Left);
            window.MouseUp(center, MouseButton.Left);
            Assert.AreEqual(1, navigations, "once enabled again it does");
            window.Close();
        });
    }
}
