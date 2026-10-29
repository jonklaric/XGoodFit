using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using ExpressionParser;

namespace ExpressionParserControls
{
    /// <summary>
    /// A WPF UserControl that provides an interactive expression editor with operator buttons,
    /// a function help expander, and toolbar. The functions panel and errors expander are
    /// managed externally by the host (e.g., FieldCalculator).
    /// </summary>
    public partial class CalculatorControl : UserControl
    {
        /// <summary>
        /// Dictionary containing variable names and their associated result types for expression parsing.
        /// </summary>
        private Dictionary<string, ResultType> _variables = new Dictionary<string, ResultType>();

        /// <summary>
        /// Tracks the currently displayed function in the help expander.
        /// </summary>
        private FunctionDescriptor? _currentHelpFunction;

        /// <summary>
        /// Raised when the expression in the LexTextBox has changed.
        /// </summary>
        public event ExpressionChangedEventHandler? ExpressionChanged;

        /// <summary>
        /// Delegate for expression change notifications.
        /// </summary>
        public delegate void ExpressionChangedEventHandler();

        /// <summary>
        /// Raised when the f(x) toggle button is checked or unchecked.
        /// The bool parameter is true when checked (show functions), false when unchecked (hide functions).
        /// Hosts like FieldCalculator subscribe to this to show/hide an external functions panel.
        /// </summary>
        public event Action<bool>? FunctionsToggleChanged;

        /// <summary>
        /// Raised when a function hyperlink is clicked in the expression editor.
        /// The host should open the functions panel and select the function matching this help path.
        /// </summary>
        public event Action<string>? HelpDocumentRequested;

        #region Dependency Properties

        /// <summary>
        /// Identifies the HasErrors dependency property.
        /// True when the current expression contains parse errors.
        /// </summary>
        public static readonly DependencyProperty HasErrorsProperty = DependencyProperty.Register(
            nameof(HasErrors), typeof(bool), typeof(CalculatorControl),
            new PropertyMetadata(false));

        /// <summary>
        /// Gets whether the current expression contains parse errors.
        /// </summary>
        public bool HasErrors
        {
            get => (bool)GetValue(HasErrorsProperty);
            private set => SetValue(HasErrorsProperty, value);
        }

        /// <summary>
        /// Identifies the ErrorCount dependency property.
        /// The number of parse errors in the current expression.
        /// </summary>
        public static readonly DependencyProperty ErrorCountProperty = DependencyProperty.Register(
            nameof(ErrorCount), typeof(int), typeof(CalculatorControl),
            new PropertyMetadata(0));

        /// <summary>
        /// Gets the number of parse errors in the current expression.
        /// </summary>
        public int ErrorCount
        {
            get => (int)GetValue(ErrorCountProperty);
            private set => SetValue(ErrorCountProperty, value);
        }

        #endregion

        /// <summary>
        /// Initializes a new instance of the <see cref="CalculatorControl"/> class.
        /// </summary>
        public CalculatorControl()
        {
            this.InitializeComponent();
        }

        #region Public API

        /// <summary>
        /// Gets the internal ExpressionControl for external wiring (e.g., linking to an AvailableFunctions panel).
        /// </summary>
        public ExpressionControl ExpressionEditor => LexTextBox;

        /// <summary>
        /// Sets the expression text in the LexTextBox.
        /// </summary>
        /// <param name="expressionText">The expression to display.</param>
        public void SetExpressionText(string expressionText)
        {
            this.LexTextBox.Text = expressionText;
        }

        /// <summary>
        /// Gets the current expression text from the LexTextBox.
        /// </summary>
        /// <returns>The current expression text.</returns>
        public string GetExpressionText()
        {
            return this.LexTextBox.Text;
        }

        /// <summary>
        /// Sets the available variables used in parsing the expression.
        /// </summary>
        /// <param name="availableVariables">A dictionary of variable names and types.</param>
        public void SetVariables(Dictionary<string, ResultType> availableVariables)
        {
            _variables = availableVariables;
        }

        /// <summary>
        /// Inserts a custom string of text into the LexTextBox.
        /// </summary>
        /// <param name="textToInsert">The text to insert.</param>
        public void InsertText(string textToInsert)
        {
            this.LexTextBox.InsertText(textToInsert);
        }

        /// <summary>
        /// Returns the parsed tree generated from the current expression.
        /// </summary>
        /// <returns>The parsed expression tree as an IParserNode.</returns>
        public IParserNode GetParseTree()
        {
            return ExpressionParser.Parser.Parser.Parse(this.LexTextBox.GetTokenList, !(this.IsCaseSensitiveCheckbox.IsChecked ?? false), _variables);
        }

        /// <summary>
        /// Gets the current list of parse errors from the expression.
        /// Returns an empty list if the expression is valid or cannot be parsed.
        /// </summary>
        /// <returns>List of parse errors.</returns>
        public IList<ParseError> GetErrors()
        {
            IParserNode? parseNode = null;
            try
            {
                parseNode = GetParseTree();
            }
            catch (Exception ex) when (ex is not OutOfMemoryException and not StackOverflowException)
            {
                // Parse failed entirely — return empty errors rather than crashing
                return Array.Empty<ParseError>();
            }

            if (parseNode == null)
                return Array.Empty<ParseError>();

            return parseNode.GetErrors;
        }

        /// <summary>
        /// Shows the function help expander with details for the given function descriptor.
        /// Called by external hosts (e.g., FieldCalculator) when a function is selected
        /// in the functions panel.
        /// </summary>
        /// <param name="func">The function descriptor to display help for.</param>
        public void ShowFunctionHelp(FunctionDescriptor func)
        {
            _currentHelpFunction = func;

            HelpFunctionName.Text = func.Name;
            HelpSyntax.Text = func.Syntax;
            HelpReturns.Text = func.Returns;
            HelpDescription.Text = func.Description;
            HelpExample.Text = func.Example;
            FunctionHelpHeader.Text = $"Function: {func.Name}";

            FunctionHelpExpander.Visibility = Visibility.Visible;
            FunctionHelpExpander.IsExpanded = true;
        }

        /// <summary>
        /// Sets the f(x) toggle button state without firing the FunctionsToggleChanged event.
        /// Used by the host to sync the toggle state.
        /// </summary>
        /// <param name="isChecked">True to check, false to uncheck.</param>
        public void SetFunctionsToggle(bool isChecked)
        {
            FunctionsToggleButton.IsChecked = isChecked;
        }

        #endregion

        #region Operator Button Handlers

        /// <summary>
        /// Inserts the plus operator into the LexTextBox.
        /// </summary>
        private void AddButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("+");
        }

        /// <summary>
        /// Inserts the minus operator into the LexTextBox.
        /// </summary>
        private void SubtractButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("-");
        }

        /// <summary>
        /// Inserts the multiplication operator into the LexTextBox.
        /// </summary>
        private void MultiplyButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("*");
        }

        /// <summary>
        /// Inserts the division operator into the LexTextBox.
        /// </summary>
        private void DivideButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("/");
        }

        /// <summary>
        /// Inserts the exponent operator into the LexTextBox.
        /// </summary>
        private void ExponentButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("^");
        }

        /// <summary>
        /// Inserts the equals sign into the LexTextBox.
        /// </summary>
        private void EqualsButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("=");
        }

        /// <summary>
        /// Inserts the less-than operator into the LexTextBox.
        /// </summary>
        private void LessThanButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText("<");
        }

        /// <summary>
        /// Inserts the greater-than operator into the LexTextBox.
        /// </summary>
        private void GreaterThanButton_Click(object sender, RoutedEventArgs e)
        {
            this.LexTextBox.InsertText(">");
        }

        #endregion

        #region Expression Changed & Error Display

        /// <summary>
        /// Called when the expression in the LexTextBox changes.
        /// Updates error state properties and fires the ExpressionChanged event.
        /// </summary>
        private void LexTextBox_ExpressionChanged(List<Token> tokenList)
        {
            UpdateErrorDisplay();
            ExpressionChanged?.Invoke();
        }

        /// <summary>
        /// Re-parses the expression when the case-sensitivity setting is changed.
        /// </summary>
        private void IsCaseSensitiveCheckbox_Checked(object sender, RoutedEventArgs e)
        {
            this.LexTextBox_ExpressionChanged(this.LexTextBox.GetTokenList);
        }

        /// <summary>
        /// Parses the current expression, updates HasErrors/ErrorCount properties,
        /// and populates the errors expander with any parse errors.
        /// Shows the expander when errors exist but does not force it open —
        /// if the user has manually expanded it, it stays open.
        /// Collapses the expander when the expression is error-free.
        /// </summary>
        private void UpdateErrorDisplay()
        {
            var errors = GetErrors();
            HasErrors = errors.Count > 0;
            ErrorCount = errors.Count;

            if (errors.Count > 0)
            {
                ErrorsList.ItemsSource = errors;
                ErrorsExpanderHeader.Text = $"Expression Errors ({errors.Count})";
                ErrorsExpander.Visibility = Visibility.Visible;
            }
            else
            {
                ErrorsExpander.Visibility = Visibility.Collapsed;
                ErrorsExpander.IsExpanded = false;
                ErrorsList.ItemsSource = null;
            }
        }

        #endregion

        #region Functions Toggle

        /// <summary>
        /// Fires FunctionsToggleChanged when the toggle is checked.
        /// </summary>
        private void FunctionsToggleButton_Checked(object sender, RoutedEventArgs e)
        {
            FunctionsToggleChanged?.Invoke(true);
        }

        /// <summary>
        /// Fires FunctionsToggleChanged when the toggle is unchecked.
        /// </summary>
        private void FunctionsToggleButton_Unchecked(object sender, RoutedEventArgs e)
        {
            FunctionsToggleChanged?.Invoke(false);
        }

        #endregion

        #region Function Help Expander

        /// <summary>
        /// Handles navigation to a help document from within the LexTextBox.
        /// Fires HelpDocumentRequested so the host can open the functions panel
        /// and select the appropriate function.
        /// </summary>
        /// <param name="helpDocumentPath">The path to the help document.</param>
        private void LexTextBox_HelpDocumentCalled(string helpDocumentPath)
        {
            HelpDocumentRequested?.Invoke(helpDocumentPath);
        }

        #endregion
    }

}
