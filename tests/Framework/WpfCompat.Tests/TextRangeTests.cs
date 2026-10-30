using WpfCompat.Controls;
using WpfCompat.Documents;

namespace WpfCompat.Tests;

/// <summary>
/// WpfCompat's TextRange over a whole document, as upstream sets a read-only RichTextBox's text (the USGS
/// text tabs of the Time Series Data and Input Data views).
/// </summary>
[TestClass]
public sealed class TextRangeTests
{
    [TestMethod]
    public void SettingText_MakesAParagraphOfEachLine_AsWpfDoes()
    {
        HeadlessSession.Run(() =>
        {
            // WPF (checked in a WPF experiment): "line one\nline two\r\nline three" gives three paragraphs of one
            // run each, with automatic margins, so the lines are a line apart.
            var box = new RichTextBox();
            int changes = 0;
            box.Document.Changed += (_, _) => changes++;

            new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text = "line one\nline two\r\nline three";

            CollectionAssert.AreEqual(
                new[] { "line one", "line two", "line three" },
                box.Document.Blocks.Cast<Paragraph>().Select(p => ((Run)p.Inlines.Single()).Text).ToList());
            Assert.IsTrue(box.Document.Blocks.All(p => double.IsNaN(p.Margin.Top)), "automatic margins, as WPF's");
            Assert.AreEqual(1, changes, "the document changes once, so a box showing it lays it out once");
            Assert.AreEqual(
                string.Join(Environment.NewLine, "line one", "line two", "line three") + Environment.NewLine,
                new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text);

            new TextRange(box.Document.ContentStart, box.Document.ContentEnd).Text = string.Empty;
            Assert.AreEqual(1, box.Document.Blocks.Count, "an empty paragraph, as an empty document has");
        });
    }
}
