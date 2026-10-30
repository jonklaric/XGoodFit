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
}
