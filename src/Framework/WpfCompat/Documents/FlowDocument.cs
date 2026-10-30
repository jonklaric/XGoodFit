using System.Collections.ObjectModel;
using System.Text;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Media;

namespace WpfCompat.Documents;

/// <summary>
/// The part of WPF's flow documents (<c>System.Windows.Documents</c>) the ported code builds for read-only
/// rich text boxes: paragraphs of runs, line breaks and hyperlinks, with their font and margin settings.
/// <see cref="Controls.RichTextBox"/> shows them.
/// </summary>
/// <remarks>
/// Avalonia has no flow documents. This model keeps the ported code's document building as it was; it
/// doesn't lay out pages, tables, lists or floaters, which nothing ported uses.
/// </remarks>
public sealed class FlowDocument
{
    /// <summary>Creates an empty document.</summary>
    public FlowDocument()
    {
        Blocks.CollectionChanged += (_, _) => Changed?.Invoke(this, EventArgs.Empty);
        ContentStart = new TextPointer(this, isStart: true);
        ContentEnd = new TextPointer(this, isStart: false);
    }

    /// <summary>Creates a document with one block.</summary>
    public FlowDocument(Block block) : this() => Blocks.Add(block);

    /// <summary>The document's paragraphs.</summary>
    public ObservableCollection<Block> Blocks { get; } = new();

    /// <summary>The document's font, or null for the viewer's.</summary>
    public FontFamily? FontFamily { get; set; }

    /// <summary>The document's font size, or NaN for the viewer's.</summary>
    public double FontSize { get; set; } = double.NaN;

    /// <summary>The space around the document's text, as WPF's <c>PagePadding</c>.</summary>
    public Thickness PagePadding { get; set; } = new(5, 0, 5, 0);

    /// <summary>The start of the document's content.</summary>
    public TextPointer ContentStart { get; }

    /// <summary>The end of the document's content.</summary>
    public TextPointer ContentEnd { get; }

    /// <summary>Raised when blocks are added or removed, so the viewer shows them.</summary>
    public event EventHandler? Changed;

    /// <summary>The document's text: its paragraphs, each followed by a line break, as WPF's <c>TextRange.Text</c>.</summary>
    internal string Text
    {
        get
        {
            var text = new StringBuilder();
            foreach (var block in Blocks.OfType<Paragraph>())
            {
                block.AppendText(text);
                text.Append(Environment.NewLine);
            }
            return text.ToString();
        }
    }
}

/// <summary>A position in a <see cref="FlowDocument"/>: only its content's start and end.</summary>
public sealed class TextPointer
{
    internal TextPointer(FlowDocument document, bool isStart)
    {
        Document = document;
        IsStart = isStart;
    }

    internal FlowDocument Document { get; }

    internal bool IsStart { get; }
}

/// <summary>
/// WPF's <c>TextRange</c> over a whole <see cref="FlowDocument"/>: its text, which setting replaces
/// with one paragraph.
/// </summary>
public sealed class TextRange
{
    private readonly FlowDocument _document;

    /// <summary>The range between <paramref name="start"/> and <paramref name="end"/>, which must span a whole document.</summary>
    public TextRange(TextPointer start, TextPointer end)
    {
        if (!start.IsStart || end.IsStart || start.Document != end.Document)
        {
            throw new NotSupportedException("WpfCompat's TextRange spans a whole document, from ContentStart to ContentEnd.");
        }
        _document = start.Document;
    }

    /// <summary>The range's text. Setting it replaces the document's content with one paragraph.</summary>
    public string Text
    {
        get => _document.Text;
        set
        {
            _document.Blocks.Clear();
            _document.Blocks.Add(new Paragraph(new Run(value ?? string.Empty)));
        }
    }
}

/// <summary>A block of a <see cref="FlowDocument"/>, as WPF's <c>Block</c>.</summary>
public abstract class Block
{
    /// <summary>The space around the block. NaN sides take WPF's default (none at the document's edges).</summary>
    public Thickness Margin { get; set; } = new(double.NaN);

    /// <summary>The block's font size, or NaN for the document's.</summary>
    public double FontSize { get; set; } = double.NaN;

    /// <summary>The block's font weight, or null for the document's.</summary>
    public FontWeight? FontWeight { get; set; }

    /// <summary>The block's font style, or null for the document's.</summary>
    public FontStyle? FontStyle { get; set; }

    /// <summary>The block's font, or null for the document's.</summary>
    public FontFamily? FontFamily { get; set; }

    /// <summary>The block's text colour, or null for the document's.</summary>
    public IBrush? Foreground { get; set; }

    /// <summary>The block's text alignment.</summary>
    public TextAlignment TextAlignment { get; set; } = TextAlignment.Left;
}

/// <summary>A paragraph of inlines, as WPF's <c>Paragraph</c>.</summary>
public sealed class Paragraph : Block
{
    /// <summary>Creates an empty paragraph.</summary>
    public Paragraph()
    {
    }

    /// <summary>Creates a paragraph with one inline.</summary>
    public Paragraph(Inline inline) => Inlines.Add(inline);

    /// <summary>The paragraph's runs, line breaks and hyperlinks.</summary>
    public List<Inline> Inlines { get; } = new();

    internal void AppendText(StringBuilder text)
    {
        foreach (var inline in Inlines)
        {
            inline.AppendText(text);
        }
    }
}

/// <summary>A piece of a paragraph, as WPF's <c>Inline</c>.</summary>
public abstract class Inline
{
    /// <summary>The inline's font size, or NaN for its paragraph's.</summary>
    public double FontSize { get; set; } = double.NaN;

    /// <summary>The inline's font weight, or null for its paragraph's.</summary>
    public FontWeight? FontWeight { get; set; }

    /// <summary>The inline's font style, or null for its paragraph's.</summary>
    public FontStyle? FontStyle { get; set; }

    /// <summary>The inline's font, or null for its paragraph's.</summary>
    public FontFamily? FontFamily { get; set; }

    /// <summary>The inline's text colour, or null for its paragraph's.</summary>
    public IBrush? Foreground { get; set; }

    /// <summary>The inline's text decorations, or null for none.</summary>
    public TextDecorationCollection? TextDecorations { get; set; }

    internal abstract void AppendText(StringBuilder text);
}

/// <summary>A run of text, as WPF's <c>Run</c>.</summary>
public sealed class Run : Inline
{
    /// <summary>Creates an empty run.</summary>
    public Run()
    {
    }

    /// <summary>Creates a run of <paramref name="text"/>.</summary>
    public Run(string text) => Text = text;

    /// <summary>The run's text.</summary>
    public string Text { get; set; } = string.Empty;

    internal override void AppendText(StringBuilder text) => text.Append(Text);
}

/// <summary>A line break, as WPF's <c>LineBreak</c>.</summary>
public sealed class LineBreak : Inline
{
    internal override void AppendText(StringBuilder text) => text.Append(Environment.NewLine);
}

/// <summary>A link to <see cref="NavigateUri"/>, as WPF's <c>Hyperlink</c>.</summary>
public sealed class Hyperlink : Inline
{
    /// <summary>Creates an empty link.</summary>
    public Hyperlink()
    {
    }

    /// <summary>Creates a link showing <paramref name="inline"/>.</summary>
    public Hyperlink(Inline inline) => Inlines.Add(inline);

    /// <summary>The link's content.</summary>
    public List<Inline> Inlines { get; } = new();

    /// <summary>Where the link goes.</summary>
    public Uri? NavigateUri { get; set; }

    /// <summary>
    /// Whether the link can be clicked, as WPF's <c>ContentElement.IsEnabled</c>: a disabled link doesn't
    /// raise <see cref="RequestNavigate"/>. Unlike WPF's, it isn't greyed out.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>Raised when the link is clicked, in a rich text box whose document is enabled.</summary>
    public event RequestNavigateEventHandler? RequestNavigate;

    internal void RaiseRequestNavigate(object sender)
    {
        if (NavigateUri != null && IsEnabled)
        {
            RequestNavigate?.Invoke(sender, new RequestNavigateEventArgs(NavigateUri, null));
        }
    }

    internal override void AppendText(StringBuilder text)
    {
        foreach (var inline in Inlines)
        {
            inline.AppendText(text);
        }
    }
}

/// <summary>The handler of <see cref="Hyperlink.RequestNavigate"/>, as WPF's.</summary>
public delegate void RequestNavigateEventHandler(object sender, RequestNavigateEventArgs e);

/// <summary>The arguments of <see cref="Hyperlink.RequestNavigate"/>, as WPF's <c>System.Windows.Navigation.RequestNavigateEventArgs</c>.</summary>
public sealed class RequestNavigateEventArgs : RoutedEventArgs
{
    /// <summary>Creates the arguments for navigating to <paramref name="uri"/>.</summary>
    public RequestNavigateEventArgs(Uri uri, string? target)
    {
        Uri = uri;
        Target = target;
    }

    /// <summary>Where to go.</summary>
    public Uri Uri { get; }

    /// <summary>The frame to go in, which nothing ported uses.</summary>
    public string? Target { get; }
}
