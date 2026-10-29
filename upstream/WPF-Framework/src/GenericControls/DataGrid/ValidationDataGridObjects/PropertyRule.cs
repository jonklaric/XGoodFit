using System.ComponentModel;

namespace GenericControls
{
    /// <summary>
    /// Represents a validation rule for a property, with support for multiple error conditions and messages.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class PropertyRule : INotifyPropertyChanged
    {

        #region Construction

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyRule"/> class.
        /// </summary>
        /// <param name="rule">A function that returns true when an error condition is met.</param>
        /// <param name="message">The error message to display if the function returns true.</param>
        public PropertyRule(Func<bool> rule, string message)
        {
            _rules.Add(new Rule(rule, message));
        }

        #endregion

        #region Members

        /// <summary>
        /// Occurs when a property value changes. Required for UI binding to update error states.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;
        private readonly List<Rule> _rules = new List<Rule>();
        private bool _hasError = false;
        private string _errorMessage = string.Empty;

        /// <summary>
        /// Gets or sets a value indicating whether the property has a validation error.
        /// </summary>
        public bool HasError
        {
            get
            {
                return _hasError;
            }
            set
            {
                if (_hasError != value)
                {
                    _hasError = value;
                    NotifyPropertyChanged(nameof(HasError));
                }
            }
        }

        /// <summary>
        /// Gets or sets the error message describing the validation failure.
        /// </summary>
        public string ErrorMessage
        {
            get
            {
                return _errorMessage;
            }
            set
            {
                if ((_errorMessage ?? "") != (value ?? ""))
                {
                    _errorMessage = value;
                    NotifyPropertyChanged(nameof(ErrorMessage));
                }
            }
        }

        /// <summary>
        /// Gets the list of validation rules for this property.
        /// </summary>
        public List<Rule> Rules
        {
            get
            {
                return _rules;
            }
        }

        /// <summary>
        /// Class for the property rule. Each rule has a function and an error message.
        /// </summary>
        public class Rule
        {
            /// <summary>
            /// The expression that evaluates to true when an error condition is met.
            /// </summary>
            public readonly Func<bool> Expression;

            /// <summary>
            /// The error message to display when the expression returns true.
            /// </summary>
            public readonly string Message;

            /// <summary>
            /// Indicates whether this rule currently has an error.
            /// </summary>
            public bool HasError;

            /// <summary>
            /// Initializes a validation rule with an expression and error message.
            /// </summary>
            /// <param name="expression">The expression that returns <c>true</c> when the rule fails.</param>
            /// <param name="message">The message to display for the failed rule.</param>
            internal Rule(Func<bool> expression, string message)
            {
                Expression = expression;
                Message = message;
            }
        }

        #endregion

        #region Methods

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        protected void NotifyPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Adds a validation rule to the property.
        /// </summary>
        /// <param name="rule">A function that returns true when an error condition is met.</param>
        /// <param name="message">The error message to display if the function returns true.</param>
        internal void AddRule(Func<bool> rule, string message)
        {
            _rules.Add(new Rule(rule, message));
        }



        /// <summary>
        /// Executes all validation rules for this property and updates the error state.
        /// </summary>
        internal void ExecuteRules()
        {
            ErrorMessage = "";
            HasError = false;
            try
            {
                for (int i = 0, loopTo = _rules.Count - 1; i <= loopTo; i++)
                {
                    if (_rules[i].Expression() == true)
                    {
                        HasError = true;
                        ErrorMessage += i == 0 ? _rules[i].Message : Environment.NewLine + _rules[i].Message;
                    }
                }
            }
            catch (Exception e)
            {
                _errorMessage = e.Message;
                HasError = true;
            }
        }

        #endregion

    }
}
