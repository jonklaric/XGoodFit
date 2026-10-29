#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type

using System.Collections.ObjectModel;
using GenericControls;

namespace Documentation.Tests.Snippets
{
    /// <summary>
    /// Validates that all C# code snippets in docs/generic-controls.md compile correctly.
    /// </summary>
    public class GenericControlSnippets
    {
        // ---------------------------------------------------------------
        // Snippet: NumericTextBox value retrieval methods
        // ---------------------------------------------------------------
        public void Snippet_NumericTextBoxValueRetrieval()
        {
            var myNumericTextBox = new NumericTextBox();

            double val = myNumericTextBox.GetValueAsDouble();
            int ival = myNumericTextBox.GetValueAsInteger();
            float fval = myNumericTextBox.GetValueAsSingle();
            bool ok = myNumericTextBox.IsValidDouble();
        }

        // ---------------------------------------------------------------
        // Snippet: NumberFormatHelper key methods -- Parsing
        // ---------------------------------------------------------------
        public void Snippet_NumberFormatHelper_Parsing()
        {
            string text = "123.45";
            bool success = NumberFormatHelper.TryParseDouble(text, out double result);
            bool success2 = NumberFormatHelper.TryParseSingle(text, out float result2);
            bool success3 = NumberFormatHelper.TryParseInt("42", out int result3);
        }

        // ---------------------------------------------------------------
        // Snippet: NumberFormatHelper key methods -- Formatting
        // ---------------------------------------------------------------
        public void Snippet_NumberFormatHelper_Formatting()
        {
            double value = 1234.5678;
            string formatted = NumberFormatHelper.FormatDouble(value);
            string formatted2 = NumberFormatHelper.FormatDouble(value, "F2");
            string formatted3 = NumberFormatHelper.FormatDouble(value, decimalPlaces: 3, useThousandsSeparator: true);
        }

        // ---------------------------------------------------------------
        // Snippet: NumberFormatHelper key methods -- Input validation
        // ---------------------------------------------------------------
        public void Snippet_NumberFormatHelper_InputValidation()
        {
            string inputText = "5";
            string currentText = "12";
            int selectionStart = 2;
            string selectedText = "";

            bool valid = NumberFormatHelper.IsValidNumericInput(inputText, currentText, selectionStart,
                selectedText, allowNegative: true, allowDecimal: true, allowScientific: false);
            bool partial = NumberFormatHelper.IsValidPartialNumber(currentText);
        }

        // ---------------------------------------------------------------
        // Snippet: NumberFormatHelper key methods -- Culture properties
        // ---------------------------------------------------------------
        public void Snippet_NumberFormatHelper_CultureProperties()
        {
            string decSep = NumberFormatHelper.DecimalSeparator;   // e.g., "." or ","
            string grpSep = NumberFormatHelper.GroupSeparator;      // e.g., "," or "."
            string negSign = NumberFormatHelper.NegativeSign;       // e.g., "-"
        }

        // ---------------------------------------------------------------
        // Snippet: NumberFormatHelper key methods -- International support
        // ---------------------------------------------------------------
        public void Snippet_NumberFormatHelper_InternationalSupport()
        {
            char c = '5';
            bool isDigit = NumberFormatHelper.IsNumericDigit(c);     // Any Unicode digit
            string normalized = NumberFormatHelper.NormalizeDigits("123"); // Convert to 0-9
            System.Windows.FlowDirection dir = NumberFormatHelper.CurrentFlowDirection; // RTL support
        }

        // ---------------------------------------------------------------
        // Snippet: DataGridRowItem pattern for ValidationDataGrid
        // ---------------------------------------------------------------
        // (This is verified by the MyRowItem class below)
    }

    // ---------------------------------------------------------------
    // Snippet: DataGridRowItem subclass
    // ---------------------------------------------------------------
    public class MyRowItem : DataGridRowItem
    {
        private double _value;

        public MyRowItem(ObservableCollection<object> parentList)
            : base(parentList) { }

        public double Value
        {
            get => _value;
            set { _value = value; NotifyPropertyChanged(); }
        }

        public override void AddValidationRules()
        {
            AddRule(nameof(Value),
                () => Value < 0,
                "Value must be non-negative.");

            AddRule(nameof(Value),
                () => UniqueRule(nameof(Value), "Value must be unique."),
                "Value must be unique.");
        }

        public override string PropertyDisplayName(string propertyName) =>
            propertyName switch
            {
                nameof(Value) => "Flow (cfs)",
                _ => propertyName
            };

        public override bool IsGridDisplayable(string propertyName) =>
            propertyName != nameof(RuleMap);
    }
}
