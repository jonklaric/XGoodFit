#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type

using System.Data;
using DatabaseManager;
using ExpressionParser;
using ExpressionParser.Parser;

namespace Documentation.Tests.Snippets
{
    /// <summary>
    /// Validates that all C# code snippets in docs/database-controls.md compile correctly.
    /// </summary>
    public class DatabaseSnippets
    {
        // ---------------------------------------------------------------
        // Snippet: Opening Databases -- SQLite
        // ---------------------------------------------------------------
        public void Snippet_OpenSQLite()
        {
            // SQLite
            var db = new SQLiteManager("data.sqlite");
            db.Open();
            DataTableView table = db.GetTableManager("MyTable");
            object val = table.GetCell(0, 0);

            // SQLite with password
            // Replace with actual credentials - never hardcode passwords
            var db2 = new SQLiteManager("encrypted.sqlite", "<your-password-here>");
        }

        // ---------------------------------------------------------------
        // Snippet: Opening Databases -- CSV
        // ---------------------------------------------------------------
        public void Snippet_OpenCsv()
        {
            // CSV
            var csv = new CsvReader("data.csv", hasHeaders: true,
                dataLineStartIndex: 0, fieldsEnclosedInQuotes: false);
            DataTableView table = csv.GetTableManager(csv.TableNames[0]);
        }

        // ---------------------------------------------------------------
        // Snippet: Opening Databases -- DBF
        // ---------------------------------------------------------------
        public void Snippet_OpenDbf()
        {
            // DBF
            var dbf = new DbfReader("attributes.dbf");
            dbf.Open();
        }

        // ---------------------------------------------------------------
        // Snippet: Opening Databases -- In-memory DataTable
        // ---------------------------------------------------------------
        public void Snippet_OpenInMemory()
        {
            // In-memory DataTable
            var dt = new DataTable("Results");
            dt.Columns.Add("Value", typeof(double));
            dt.Rows.Add(3.14);
            var mem = new InMemoryReader(dt);
        }

        // ---------------------------------------------------------------
        // Snippet: Edit Tracking with Undo/Redo
        // ---------------------------------------------------------------
        public void Snippet_EditTracking()
        {
            var db = new SQLiteManager("data.sqlite");
            db.Open();
            var table = db.GetTableManager("MyTable");

            table.EditCell(0, 0, "NewValue");   // Records a CellEdit
            table.UndoEdit();                    // Reverts to previous value
            table.RedoEdit();                    // Reapplies "NewValue"
            table.ApplyEdits();                  // Commits all edits to the database
        }

        // ---------------------------------------------------------------
        // Snippet: ExpressionParser -- Parse and evaluate
        // ---------------------------------------------------------------
        public void Snippet_ExpressionParser()
        {
            // Parse from string
            IParserNode node = Parser.Parse("IF([x] > 10, 'high', 'low')",
                ignoreCase: true,
                availableVariables: new Dictionary<string, ResultType>
                {
                    ["x"] = ResultType.Double
                });

            // Set variable values before evaluation
            foreach (var varNode in node.GetVariableNodes())
            {
                varNode.SetValue(42.0);
            }

            // Evaluate
            ParseNodeResult result = node.Evaluate();
            // result.Result => "high", result.Type => ResultType.String
        }

        // ---------------------------------------------------------------
        // Snippet: CalculatorControl
        // ---------------------------------------------------------------
        public void Snippet_CalculatorControl()
        {
            var calculator = new ExpressionParserControls.CalculatorControl();

            // Set available variables
            calculator.SetVariables(new Dictionary<string, ResultType>
            {
                ["flow"] = ResultType.Double,
                ["name"] = ResultType.String
            });

            // Get/set expression text
            calculator.SetExpressionText("IF([flow] > 100, 'high', 'low')");
            string expr = calculator.GetExpressionText();

            // Listen for changes
            calculator.ExpressionChanged += () => { /* re-evaluate */ };
        }
    }
}
