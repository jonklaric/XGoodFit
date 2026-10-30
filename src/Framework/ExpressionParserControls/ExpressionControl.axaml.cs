using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using AvaloniaEdit;
using AvaloniaEdit.Document;
using AvaloniaEdit.Editing;
using AvaloniaEdit.Rendering;
using ExpressionParser;
using WpfCompat;

namespace ExpressionParserControls
{
    /// <summary>
    /// Represents a control for editing and displaying mathematical or logical expressions.
    /// Provides real-time syntax highlighting, token parsing, and support for hyperlinks to help documentation.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: upstream edits in a WPF RichTextBox and, on every change, rebuilds its document from
    /// a coloured run per token (and a hyperlink per function with help), then puts the caret back. The
    /// port edits in AvaloniaEdit's <see cref="TextEditor"/>: the document stays, and
    /// <see cref="ExpressionColorizer"/> colours the same tokens the same way when the editor draws. The
    /// rest follows upstream: the token list, events, caret position, the 7 parenthesis colours by depth,
    /// line breaks from a paste becoming spaces, and the text shown uncoloured until the first change
    /// after the control loads. The editor takes no line breaks or tabs, as upstream's
    /// (<c>AcceptsReturn</c> and <c>AcceptsTab</c> false), so Enter reaches a dialog's default button.
    /// </remarks>
    public partial class ExpressionControl : UserControl
    {
        /// <summary>
        /// Default parenthesis colors used when theme resources are not available.
        /// </summary>
        private static readonly ISolidColorBrush[] DefaultParenthesisColors = new[] { Brushes.Black, Brushes.Green, Brushes.Purple, Brushes.OrangeRed, Brushes.CornflowerBlue, Brushes.DarkGoldenrod, Brushes.Red };

        /// <summary>
        /// Predefined colors for highlighting parentheses in the expression.
        /// Resolved from theme DynamicResources at load time, with hardcoded fallbacks.
        /// </summary>
        private ISolidColorBrush[] _parenthesisColors = DefaultParenthesisColors;

        /// <summary>
        /// Flag indicating whether the expression is currently being formatted.
        /// </summary>
        private bool _formattingExpression = false;

        /// <summary>
        /// Avalonia property backing the <see cref="Text"/> property.
        /// </summary>
        public static readonly StyledProperty<string> TextProperty =
            AvaloniaProperty.Register<ExpressionControl, string>(nameof(Text), "", defaultBindingMode: BindingMode.TwoWay);

        static ExpressionControl()
        {
            TextProperty.Changed.AddClassHandler<ExpressionControl>(OnTextPropertyChanged);
        }

        /// <summary>
        /// Synchronizes the rich text editor when the bound text value changes.
        /// </summary>
        /// <param name="control">The expression control whose text changed.</param>
        /// <param name="e">The property change details.</param>
        private static void OnTextPropertyChanged(ExpressionControl control, AvaloniaPropertyChangedEventArgs e)
        {
            string newText = (string?)e.NewValue ?? "";
            if (control._formattingExpression)
                return;
            if (!control.IsLoaded)
            {
                // XGoodFit port: upstream's document showed the text through a bound run until it loaded.
                control._formattingExpression = true;
                try
                {
                    control.ExpressionTextBox.Text = newText;
                }
                finally
                {
                    control._formattingExpression = false;
                }
                return;
            }

            string currentText = control.ExpressionTextBox.Text.Trim(Environment.NewLine.ToCharArray());

            if (currentText != newText)
            {
                // try/finally so an exception during document rebuild (Blocks.Clear/Add or
                // the TextChanged reformat) doesn't leave _formattingExpression stuck at true,
                // which would permanently lock the editor.
                control._formattingExpression = true;
                try
                {
                    control.ExpressionTextBox.Document.Replace(0, control.ExpressionTextBox.Document.TextLength, newText);
                }
                finally
                {
                    control._formattingExpression = false;
                }
                // Trigger a TextChanged to reformat with syntax highlighting
                control.ExpressionTextBox_TextChanged(control.ExpressionTextBox, EventArgs.Empty);
            }
        }

        /// <summary>
        /// Gets or sets the text content of the expression editor.
        /// </summary>
        public string Text
        {
            get
            {
                return this.GetValue(TextProperty);
            }
            set
            {
                this.SetValue(TextProperty, value);
            }
        }

        /// <summary>
        /// Gets the list of tokens parsed from the current expression.
        /// </summary>
        public List<Token> GetTokenList { get; private set; } = new List<Token>();

        /// <summary>
        /// Raised when the expression changes and tokens are re-evaluated.
        /// </summary>
        public event ExpressionChangedEventHandler? ExpressionChanged;

        /// <summary>
        /// Delegate for handling expression changes.
        /// </summary>
        /// <param name="tokenList">List of tokens parsed from the expression.</param>
        public delegate void ExpressionChangedEventHandler(List<Token> tokenList);

        /// <summary>
        /// Raised when a help document is triggered by clicking a hyperlink in the expression editor.
        /// </summary>
        public event HelpDocumentCalledEventHandler? HelpDocumentCalled;

        /// <summary>
        /// Delegate for the HelpDocumentCalled event.
        /// </summary>
        /// <param name="helpDocumentPath">The path of the help document.</param>
        public delegate void HelpDocumentCalledEventHandler(string helpDocumentPath);

        /// <summary>
        /// Overflow color used when parenthesis nesting exceeds the color array length.
        /// </summary>
        private ISolidColorBrush _overflowColor = Brushes.Gray;

        // XGoodFit port: colours the tokens as upstream's runs and hyperlinks were coloured.
        private readonly ExpressionColorizer _colorizer;

        /// <summary>
        /// Initializes a new instance of the <see cref="ExpressionControl"/> class.
        /// </summary>
        public ExpressionControl()
        {
            // This call is required by the designer.
            this.InitializeComponent();

            // XGoodFit port: the editor's colouring, input rules and help links (see the remarks).
            _colorizer = new ExpressionColorizer(this);
            var textArea = this.ExpressionTextBox.TextArea;
            textArea.TextView.LineTransformers.Add(_colorizer);
            textArea.SetResourceReference(TextArea.SelectionBrushProperty, "TextBox.Selection.Background");
            textArea.SetResourceReference(TextArea.CaretBrushProperty, "TextBox.Caret");
            RemoveKeyBindings(textArea, Key.Enter, Key.Tab);
            this.ExpressionTextBox.TextChanged += ExpressionTextBox_TextChanged;
            textArea.AddHandler(PointerPressedEvent, HyperlinkMouseLeftButtonDown, RoutingStrategies.Tunnel);
            textArea.PointerMoved += (s, e) => _colorizer.Hover(this.FunctionAt(e));
            textArea.PointerExited += (s, e) => _colorizer.Hover(null);

            // Resolve theme-aware parenthesis colors after the control is loaded
            // so that DynamicResource keys from the application's merged dictionaries are available.
            this.Loaded += (s, e) => ResolveThemeColors();
        }

        // Removes the editor's key bindings (line breaks, indentation) for the keys upstream's editor
        // didn't accept, so the keys go on to the window as they did in WPF.
        private static void RemoveKeyBindings(TextArea textArea, params Key[] keys)
        {
            foreach (var handler in textArea.DefaultInputHandler.NestedInputHandlers.OfType<TextAreaInputHandler>().Append(textArea.DefaultInputHandler.Editing))
            {
                foreach (var binding in handler.KeyBindings.Where(b => b.Gesture is { } gesture && keys.Contains(gesture.Key)).ToList())
                {
                    handler.KeyBindings.Remove(binding);
                }
            }
        }

        /// <summary>
        /// Resolves parenthesis and overflow colors from theme DynamicResources, falling back to defaults.
        /// </summary>
        private void ResolveThemeColors()
        {
            var colors = new ISolidColorBrush[7];
            for (int i = 0; i < colors.Length; i++)
            {
                colors[i] = this.TryFindResource($"ExpressionControl.Parenthesis.Color{i}") as ISolidColorBrush ?? DefaultParenthesisColors[i];
            }
            _parenthesisColors = colors;
            _overflowColor = this.TryFindResource("ExpressionControl.Parenthesis.Overflow") as ISolidColorBrush ?? Brushes.Gray;
        }

        /// <summary>
        /// Inserts text into current expression at the caret position.
        /// </summary>
        /// <param name="textToInsert">The text to insert.</param>
        /// <param name="focusRichTextBox">Whether to refocus the editor after insertion.</param>
        public void InsertText(string textToInsert, bool focusRichTextBox = true)
        {
            // XGoodFit port: upstream replaced the selection, and its reformat then put the caret after
            // the new text with nothing selected, which is what typing it does here.
            this.ExpressionTextBox.TextArea.Selection.ReplaceSelectionWithText(textToInsert);
            if (focusRichTextBox)
            {
                this.ExpressionTextBox.TextArea.Focus();
            }
        }

        /// <summary>
        /// Handles changes to the expression text box, performing syntax highlighting and token analysis
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The event data.</param>
        private void ExpressionTextBox_TextChanged(object? sender, EventArgs e)
        {
            if (this.IsLoaded == false)
                return;
            if (_formattingExpression == true)
                return;
            _formattingExpression = true;
            try
            {
                var editor = this.ExpressionTextBox;
                // Get caret position within the unformatted text string.
                string allRichText = editor.Text.Trim(Environment.NewLine.ToCharArray());
                string fromCaretText = editor.Text.Substring(editor.CaretOffset).Trim(Environment.NewLine.ToCharArray());
                // Handle the situation where data was potentially pasted.
                // XGoodFit port: upstream checked for a change that added more than one character. The
                // editor accepts no line breaks, so one in the text came from a paste.
                if (allRichText.Contains('\n') || allRichText.Contains('\r'))
                {
                    allRichText = allRichText.Replace(Environment.NewLine, " ").Replace("\n", " ");
                    fromCaretText = fromCaretText.Replace(Environment.NewLine, " ").Replace("\n", " ");
                }
                int caretTextPosition = allRichText.Length - fromCaretText.Length;
                // Get the token list.
                GetTokenList = Lexer.TokenizeStringToList(allRichText);
                // XGoodFit port: the document keeps the text (upstream rebuilt it from the tokens), so
                // only a paste's line breaks change it.
                if (editor.Text != allRichText)
                {
                    editor.Document.Replace(0, editor.Document.TextLength, allRichText);
                }
                editor.CaretOffset = Math.Clamp(caretTextPosition, 0, editor.Document.TextLength);
                _colorizer.Update(GetTokenList, _parenthesisColors, _overflowColor);
                editor.TextArea.TextView.Redraw();
                Text = allRichText;
            }
            finally
            {
                _formattingExpression = false;
            }
            //
            ExpressionChanged?.Invoke(GetTokenList);
        }

        // The function token with help under the pointer, or null.
        private Token? FunctionAt(PointerEventArgs e)
        {
            var position = this.ExpressionTextBox.GetPositionFromPoint(e.GetPosition(this.ExpressionTextBox));
            if (position is not { } location)
                return null;
            int offset = this.ExpressionTextBox.Document.GetOffset(location.Location);
            return _colorizer.FunctionAt(offset);
        }

        /// <summary>
        /// Handles the mouse left-button down event on a hyperlink, triggering help documentation.
        /// </summary>
        /// <param name="sender">The hyperlink sender.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void HyperlinkMouseLeftButtonDown(object? sender, PointerPressedEventArgs e)
        {
            // XGoodFit port: the pointer is over a function with help, where upstream had a hyperlink.
            if (!e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
                return;
            if (FunctionAt(e) is not { } function)
                return;
            // Base path on the app directory, not CurrentDirectory - the working
            // directory may not be the install directory at runtime.
            var navigateUri = new Uri(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, function.HelpDocPath));
            HelpDocumentCalled?.Invoke(navigateUri.AbsolutePath);
            e.Handled = true;
        }

        /// <summary>
        /// XGoodFit addition: colours an expression's tokens when the editor draws them, as upstream's
        /// runs were coloured: parentheses by nesting depth, and functions with help as hyperlinks
        /// (underlined, in the theme's function colours, with a hand cursor on hover).
        /// </summary>
        private sealed class ExpressionColorizer(ExpressionControl owner) : DocumentColorizingTransformer
        {
            private List<Token> _tokens = new List<Token>();
            private readonly Dictionary<Token, IBrush> _parenthesisBrushes = new Dictionary<Token, IBrush>();
            private Token? _hovered;

            /// <summary>Takes the tokens and works out each parenthesis's colour, as upstream did.</summary>
            public void Update(List<Token> tokens, ISolidColorBrush[] parenthesisColors, ISolidColorBrush overflowColor)
            {
                _tokens = tokens;
                _parenthesisBrushes.Clear();
                int parenthesisColorPosition = 0;
                foreach (var t in tokens)
                {
                    if (t.Type == TokenType.LeftParenthesis)
                    {
                        _parenthesisBrushes[t] = parenthesisColorPosition > parenthesisColors.Length - 1 ? overflowColor : parenthesisColors[parenthesisColorPosition];
                        parenthesisColorPosition += 1;
                    }
                    else if (t.Type == TokenType.RightParenthesis)
                    {
                        parenthesisColorPosition -= 1;
                        if (parenthesisColorPosition < 0)
                            parenthesisColorPosition = 0;
                        _parenthesisBrushes[t] = parenthesisColorPosition > parenthesisColors.Length - 1 ? overflowColor : parenthesisColors[parenthesisColorPosition];
                    }
                }
                _hovered = null;
            }

            /// <summary>The function token with help at a document offset, or null.</summary>
            public Token? FunctionAt(int offset) =>
                _tokens.FirstOrDefault(t => IsHelpLink(t) && offset >= t.StartPosition && offset < t.StartPosition + t.TokenString.Length);

            /// <summary>Shows the hyperlink under the pointer as hovered.</summary>
            public void Hover(Token? token)
            {
                owner.ExpressionTextBox.TextArea.Cursor = token is null ? null : new Cursor(StandardCursorType.Hand);
                if (!Equals(token, _hovered))
                {
                    _hovered = token;
                    owner.ExpressionTextBox.TextArea.TextView.Redraw();
                }
            }

            private static bool IsHelpLink(Token t) => t.TokenGroup == TokenClass.Function && !string.IsNullOrEmpty(t.HelpDocPath);

            protected override void ColorizeLine(DocumentLine line)
            {
                foreach (var t in _tokens)
                {
                    int start = Math.Max(t.StartPosition, line.Offset);
                    int end = Math.Min(t.StartPosition + t.TokenString.Length, line.EndOffset);
                    if (start >= end)
                        continue;
                    if (_parenthesisBrushes.TryGetValue(t, out var brush))
                    {
                        ChangeLinePart(start, end, element => element.TextRunProperties.SetForegroundBrush(brush));
                    }
                    else if (IsHelpLink(t))
                    {
                        string key = Equals(t, _hovered) ? "ExpressionControl.Function.Hover" : "ExpressionControl.Function.Foreground";
                        var foreground = owner.TryFindResource(key) as IBrush;
                        ChangeLinePart(start, end, element =>
                        {
                            if (foreground != null)
                                element.TextRunProperties.SetForegroundBrush(foreground);
                            element.TextRunProperties.SetTextDecorations(TextDecorations.Underline);
                        });
                    }
                }
            }
        }
    }
}
