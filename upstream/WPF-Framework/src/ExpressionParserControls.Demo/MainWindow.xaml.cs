using System;
using System.Collections.Generic;
using System.Windows;
using ExpressionParser;

namespace ExpressionParserControls.Demo
{

    /// <summary>
    /// Represents the main window of the application, which contains a calculator control
    /// </summary>
    public partial class MainWindow:Window
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="MainWindow"/> class.
        /// </summary>
        public MainWindow()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Handles the expression changed event from a CalculatorControl instance.
        /// Parses the expression and displays the result or error messages in the associated UI text block.
        /// </summary>
        private void CalculatorControl_ExpressionChanged()
        {
            IParserNode result;
            try
            {
                result = this.testCalc.GetParseTree();
            }
            catch (Exception ex)
            {
                // GetParseTree can throw on pathological input (stack overflow on deeply
                // nested expressions, unexpected token patterns). Surface the message.
                this.ResultTextBlock.Text = "Parse failed: " + ex.Message;
                return;
            }
            this.ResultTextBlock.Text = "";
            // check for parse errors
            if (result == null)
            {
                this.ResultTextBlock.Text = "Error expression returns nothing";
                return;
            }
            List<ParseError> errorList = result.GetErrors;
            // check that output type is correct
            if (errorList.Count > 0)
            {
                this.ResultTextBlock.Text = "Error in expression";
            }
            else
            {
                if (result.ContainsVariable())
                {
                    this.ResultTextBlock.Text = "Error defining variables";
                }
                else
                {
                    try
                    {
                        this.ResultTextBlock.Text = "Example: result equal to '" + result.Evaluate().Result?.ToString() + "'";
                    }
                    catch (Exception ex)
                    {
                        // Evaluate can raise InvalidCastException / FormatException for
                        // operations that mix incompatible types at runtime.
                        this.ResultTextBlock.Text = "Evaluation failed: " + ex.Message;
                    }
                }
            }
        }
    }
}