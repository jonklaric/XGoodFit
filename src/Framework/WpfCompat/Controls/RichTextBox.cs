using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Media.Immutable;
using WpfCompat.Documents;
using AvaloniaDocuments = Avalonia.Controls.Documents;

namespace WpfCompat.Controls;

/// <summary>
/// WPF's <c>RichTextBox</c> for the read-only uses the ported code has: it shows a
/// <see cref="FlowDocument"/> (paragraphs of runs, line breaks and hyperlinks), scrolls, and reports its
/// scroll position. Each paragraph is a selectable, wrapping text block.
/// </summary>
/// <remarks>
/// <list type="bullet">
/// <item>Editing isn't supported: the upstream code that edits rich text (the expression editors) is
/// ported onto AvaloniaEdit instead.</item>
/// <item>As in WPF, hyperlinks are clickable only while <see cref="IsDocumentEnabled"/> is true; they
/// raise <see cref="Hyperlink.RequestNavigate"/>.</item>
/// <item>A paragraph's automatic (NaN) margins are one line of its font, as WPF's are, and adjacent
/// margins collapse.</item>
/// <item>Text wraps to the visible width, as a flow document does, so the horizontal scroll bar never
/// shows; unless the document has a <see cref="FlowDocument.PageWidth"/>, which lays the text out that
/// wide and scrolls it sideways as the horizontal scroll bar setting says, as in WPF.</item>
/// <item>Themes' RichTextBox theme gives it the ported colours; without a theme it has a plain look.</item>
/// </list>
/// </remarks>
public class RichTextBox : TemplatedControl
{
    /// <summary>Defines <see cref="Document"/>.</summary>
    public static readonly StyledProperty<FlowDocument> DocumentProperty =
        AvaloniaProperty.Register<RichTextBox, FlowDocument>(nameof(Document));

    /// <summary>Defines <see cref="IsReadOnly"/>.</summary>
    public static readonly StyledProperty<bool> IsReadOnlyProperty =
        AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsReadOnly), false);

    /// <summary>Defines <see cref="IsDocumentEnabled"/>.</summary>
    public static readonly StyledProperty<bool> IsDocumentEnabledProperty =
        AvaloniaProperty.Register<RichTextBox, bool>(nameof(IsDocumentEnabled), false);

    /// <summary>Defines <see cref="VerticalScrollBarVisibility"/>.</summary>
    public static readonly StyledProperty<ScrollBarVisibility> VerticalScrollBarVisibilityProperty =
        AvaloniaProperty.Register<RichTextBox, ScrollBarVisibility>(nameof(VerticalScrollBarVisibility), ScrollBarVisibility.Hidden);

    /// <summary>Defines <see cref="HorizontalScrollBarVisibility"/>.</summary>
    public static readonly StyledProperty<ScrollBarVisibility> HorizontalScrollBarVisibilityProperty =
        AvaloniaProperty.Register<RichTextBox, ScrollBarVisibility>(nameof(HorizontalScrollBarVisibility), ScrollBarVisibility.Hidden);

    /// <summary>Defines <see cref="SelectionBrush"/>.</summary>
    public static readonly StyledProperty<IBrush?> SelectionBrushProperty =
        AvaloniaProperty.Register<RichTextBox, IBrush?>(nameof(SelectionBrush));

    /// <summary>Defines <see cref="CaretBrush"/>.</summary>
    public static readonly StyledProperty<IBrush?> CaretBrushProperty =
        AvaloniaProperty.Register<RichTextBox, IBrush?>(nameof(CaretBrush));

    // WPF's hyperlink colour (SystemColors.HotTrackBrush).
    private static readonly IBrush HyperlinkBrush = new ImmutableSolidColorBrush(Color.FromRgb(0x00, 0x66, 0xCC));

    private ScrollViewer? _scrollViewer;
    private StackPanel? _blocks;
    private FlowDocument? _shownDocument;

    static RichTextBox()
    {
        TemplateProperty.OverrideDefaultValue<RichTextBox>(new FuncControlTemplate<RichTextBox>((box, scope) =>
            new Border
            {
                [!Border.BackgroundProperty] = box[!BackgroundProperty],
                [!Border.BorderBrushProperty] = box[!BorderBrushProperty],
                [!Border.BorderThicknessProperty] = box[!BorderThicknessProperty],
                Child = new ScrollViewer
                {
                    Name = "PART_ScrollViewer",
                    [!ScrollViewer.VerticalScrollBarVisibilityProperty] = box[!VerticalScrollBarVisibilityProperty],
                    // A flow document wraps to the visible width whatever its horizontal scroll bar setting,
                    // so the text is laid out no wider than the viewer.
                    HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
                    [!ScrollViewer.PaddingProperty] = box[!PaddingProperty],
                    Content = new StackPanel { Name = "PART_Blocks" }.RegisterInNameScope(scope),
                }.RegisterInNameScope(scope),
            }));
        DocumentProperty.Changed.AddClassHandler<RichTextBox>((box, _) => box.ShowDocument());
        IsDocumentEnabledProperty.Changed.AddClassHandler<RichTextBox>((box, _) => box.ShowDocument());
        SelectionBrushProperty.Changed.AddClassHandler<RichTextBox>((box, _) => box.ShowDocument());
        HorizontalScrollBarVisibilityProperty.Changed.AddClassHandler<RichTextBox>((box, _) => box.ShowDocument());
    }

    /// <summary>Creates a rich text box with an empty document, as WPF's.</summary>
    public RichTextBox()
    {
        Document = new FlowDocument();
    }

    /// <summary>The document shown.</summary>
    public FlowDocument Document
    {
        get => GetValue(DocumentProperty);
        set => SetValue(DocumentProperty, value);
    }

    /// <summary>Whether the user can't edit the text. Only read-only text is supported.</summary>
    public bool IsReadOnly
    {
        get => GetValue(IsReadOnlyProperty);
        set => SetValue(IsReadOnlyProperty, value);
    }

    /// <summary>Whether the document's hyperlinks are clickable, as WPF's.</summary>
    public bool IsDocumentEnabled
    {
        get => GetValue(IsDocumentEnabledProperty);
        set => SetValue(IsDocumentEnabledProperty, value);
    }

    /// <summary>When the vertical scroll bar shows.</summary>
    public ScrollBarVisibility VerticalScrollBarVisibility
    {
        get => GetValue(VerticalScrollBarVisibilityProperty);
        set => SetValue(VerticalScrollBarVisibilityProperty, value);
    }

    /// <summary>When the horizontal scroll bar shows.</summary>
    public ScrollBarVisibility HorizontalScrollBarVisibility
    {
        get => GetValue(HorizontalScrollBarVisibilityProperty);
        set => SetValue(HorizontalScrollBarVisibilityProperty, value);
    }

    /// <summary>The selected text's highlight.</summary>
    public IBrush? SelectionBrush
    {
        get => GetValue(SelectionBrushProperty);
        set => SetValue(SelectionBrushProperty, value);
    }

    /// <summary>The caret's colour, kept for the ported code; read-only text has no caret.</summary>
    public IBrush? CaretBrush
    {
        get => GetValue(CaretBrushProperty);
        set => SetValue(CaretBrushProperty, value);
    }

    /// <summary>How far the content is scrolled down.</summary>
    public double VerticalOffset => _scrollViewer?.Offset.Y ?? 0;

    /// <summary>How far the content is scrolled right.</summary>
    public double HorizontalOffset => _scrollViewer?.Offset.X ?? 0;

    /// <summary>The visible height.</summary>
    public double ViewportHeight => _scrollViewer?.Viewport.Height ?? 0;

    /// <summary>The visible width.</summary>
    public double ViewportWidth => _scrollViewer?.Viewport.Width ?? 0;

    /// <summary>The content's height.</summary>
    public double ExtentHeight => _scrollViewer?.Extent.Height ?? 0;

    /// <summary>The content's width.</summary>
    public double ExtentWidth => _scrollViewer?.Extent.Width ?? 0;

    /// <summary>Raised when the content scrolls or its size changes, as WPF's <c>ScrollViewer.ScrollChanged</c>.</summary>
    public event EventHandler<ScrollChangedEventArgs>? ScrollChanged;

    /// <summary>Scrolls to the top.</summary>
    public void ScrollToHome() => _scrollViewer?.ScrollToHome();

    /// <summary>Scrolls to the bottom.</summary>
    public void ScrollToEnd() => _scrollViewer?.ScrollToEnd();

    /// <inheritdoc/>
    protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
    {
        base.OnApplyTemplate(e);
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged -= OnScrollChanged;
        }
        _scrollViewer = e.NameScope.Find<ScrollViewer>("PART_ScrollViewer");
        _blocks = e.NameScope.Find<StackPanel>("PART_Blocks");
        if (_scrollViewer != null)
        {
            _scrollViewer.ScrollChanged += OnScrollChanged;
        }
        _shownDocument = null;
        ShowDocument();
    }

    private void OnScrollChanged(object? sender, ScrollChangedEventArgs e) => ScrollChanged?.Invoke(this, e);

    private void OnDocumentChanged(object? sender, EventArgs e) => ShowDocument();

    private void ShowDocument()
    {
        if (_shownDocument != Document)
        {
            if (_shownDocument != null)
            {
                _shownDocument.Changed -= OnDocumentChanged;
            }
            _shownDocument = Document;
            if (_shownDocument != null)
            {
                _shownDocument.Changed += OnDocumentChanged;
            }
        }
        if (_blocks == null)
        {
            return;
        }
        _blocks.Children.Clear();
        var document = Document;
        if (document == null)
        {
            return;
        }
        _blocks.Margin = document.PagePadding;
        // A page width lays the text out that wide, left-aligned, and scrolls it sideways where the box is
        // narrower, as WPF's does; without one the text wraps to the visible width.
        bool hasPageWidth = !double.IsNaN(document.PageWidth);
        _blocks.Width = hasPageWidth ? Math.Max(0, document.PageWidth - document.PagePadding.Left - document.PagePadding.Right) : double.NaN;
        _blocks.HorizontalAlignment = hasPageWidth ? HorizontalAlignment.Left : HorizontalAlignment.Stretch;
        if (_scrollViewer != null)
        {
            _scrollViewer.HorizontalScrollBarVisibility = hasPageWidth ? HorizontalScrollBarVisibility : ScrollBarVisibility.Disabled;
        }
        double fontSize = !double.IsNaN(document.FontSize) ? document.FontSize : FontSize;
        double previousBottom = 0;
        var paragraphs = document.Blocks.OfType<Paragraph>().ToList();
        for (int i = 0; i < paragraphs.Count; i++)
        {
            var paragraph = paragraphs[i];
            double paragraphFontSize = !double.IsNaN(paragraph.FontSize) ? paragraph.FontSize : fontSize;
            Thickness margin = Resolve(paragraph.Margin, paragraphFontSize);
            var block = new SelectableTextBlock
            {
                TextWrapping = TextWrapping.Wrap,
                TextAlignment = paragraph.TextAlignment,
                // Adjacent paragraph margins collapse, as WPF's do.
                Margin = new Thickness(margin.Left, i == 0 ? margin.Top : Math.Max(0, margin.Top - previousBottom), margin.Right, margin.Bottom),
            };
            if (SelectionBrush != null)
            {
                block.SelectionBrush = SelectionBrush;
            }
            Apply(block, document.FontFamily, document.FontSize, null, null, null);
            Apply(block, paragraph.FontFamily, paragraph.FontSize, paragraph.FontWeight, paragraph.FontStyle, paragraph.Foreground);
            foreach (var inline in paragraph.Inlines)
            {
                block.Inlines!.Add(Convert(inline));
            }
            _blocks.Children.Add(block);
            previousBottom = margin.Bottom;
        }
    }

    // NaN (WPF's automatic) sides are one line of the font, as WPF lays out paragraphs.
    private static Thickness Resolve(Thickness margin, double fontSize) => new(
        double.IsNaN(margin.Left) ? 0 : margin.Left,
        double.IsNaN(margin.Top) ? fontSize : margin.Top,
        double.IsNaN(margin.Right) ? 0 : margin.Right,
        double.IsNaN(margin.Bottom) ? fontSize : margin.Bottom);

    private static void Apply(TextBlock block, FontFamily? family, double size, FontWeight? weight, FontStyle? style, IBrush? foreground)
    {
        if (family != null) block.FontFamily = family;
        if (!double.IsNaN(size)) block.FontSize = size;
        if (weight is { } w) block.FontWeight = w;
        if (style is { } s) block.FontStyle = s;
        if (foreground != null) block.Foreground = foreground;
    }

    private AvaloniaDocuments.Inline Convert(Inline inline)
    {
        AvaloniaDocuments.Inline result;
        switch (inline)
        {
            case Documents.Run run:
                result = new AvaloniaDocuments.Run(run.Text);
                break;
            case Documents.LineBreak:
                return new AvaloniaDocuments.LineBreak();
            case Hyperlink link:
                result = Link(link);
                break;
            default:
                result = new AvaloniaDocuments.Run(string.Empty);
                break;
        }
        if (inline.FontFamily != null) result.FontFamily = inline.FontFamily;
        if (!double.IsNaN(inline.FontSize)) result.FontSize = inline.FontSize;
        if (inline.FontWeight is { } weight) result.FontWeight = weight;
        if (inline.FontStyle is { } style) result.FontStyle = style;
        if (inline.Foreground != null) result.Foreground = inline.Foreground;
        if (inline.TextDecorations != null) result.TextDecorations = inline.TextDecorations;
        return result;
    }

    // A hyperlink is underlined in WPF's link colour; while the document is enabled it's clickable.
    private AvaloniaDocuments.Inline Link(Hyperlink link)
    {
        if (!IsDocumentEnabled)
        {
            var span = new AvaloniaDocuments.Span { Foreground = HyperlinkBrush, TextDecorations = TextDecorations.Underline };
            foreach (var inline in link.Inlines)
            {
                span.Inlines.Add(Convert(inline));
            }
            return span;
        }
        var text = new TextBlock
        {
            // Transparent, not null: Avalonia hit-tests a text block without a background only where it draws.
            Background = Brushes.Transparent,
            Foreground = HyperlinkBrush,
            TextDecorations = TextDecorations.Underline,
            Cursor = new Cursor(StandardCursorType.Hand),
            VerticalAlignment = VerticalAlignment.Bottom,
        };
        foreach (var inline in link.Inlines)
        {
            text.Inlines!.Add(Convert(inline));
        }
        text.PointerPressed += (_, e) =>
        {
            if (e.GetCurrentPoint(text).Properties.IsLeftButtonPressed)
            {
                link.RaiseRequestNavigate(link);
                e.Handled = true;
            }
        };
        return new AvaloniaDocuments.InlineUIContainer(text);
    }
}
