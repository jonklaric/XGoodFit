using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using DatabaseManager;
using ExpressionParser;
using ExpressionParser.Parser;

namespace DatabaseControls
{
    /// <summary>
    /// A window that provides "Select By Attribute" functionality for data tables.
    /// Allows users to select rows in a data table based on boolean expression criteria.
    /// This window uses the expression parser to evaluate conditions and return matching row indices.
    /// </summary>
    public partial class SelectByAttribute : GenericControls.MetroWindow
    {
        //Private _firstparts As String
        //Private _secondparts As String
        //Private _caretIndex As Integer
        //Private _selectedtext As String

        /// <summary>
        /// Reference to the data table view being queried.
        /// </summary>
        private readonly DataTableView _dbView;

        //Private ReadOnly _wfgrid As WpfCustomViewer

        /// <summary>
        /// List of row indices that match the selection criteria.
        /// Populated when the Execute button is clicked and the expression evaluates to true for specific rows.
        /// </summary>
        private readonly List<int> _selection;

        //Private _parseerrors As List(Of String)

        /// <summary>
        /// Initializes a new instance of the <see cref="SelectByAttribute"/> class.
        /// </summary>
        /// <param name="view">The data table view to perform selection on.</param>
        public SelectByAttribute(DataTableView view)
        {
            // This call is required by the designer.
            InitializeComponent();

            // Set themed select by attribute icon
            if (FindResource("SelectByAttributeIcon") is Canvas iconCanvas)
            {
                iconCanvas.Measure(new Size(16, 16));
                iconCanvas.Arrange(new Rect(0, 0, 16, 16));
                var rtb = new RenderTargetBitmap(16, 16, 96, 96, PixelFormats.Pbgra32);
                rtb.Render(iconCanvas);
                Icon = rtb;
            }

            //_wfgrid = wpfviewer
            //AddHandler ExpressionWindow.ParseSuccess, AddressOf DisplayResult
            //AddHandler ExpressionWindow.ErrorsFound, AddressOf ErrorsFound
            //AddHandler AvailableFunctions.TextToAdd, AddressOf UpdateTextbox
            //Grid.SetRow(ExpressionWindow.GetTextBlock, 2)
            //ExpressionGrid.Children.Add(ExpressionWindow.GetTextBlock)

            _dbView = view;
            _selection = new List<int>();

            //SelectedField.Text = "Select from " & System.IO.Path.GetFileNameWithoutExtension(_dbView.ParentDatabase.DataBasePath) & " where:"
            //ExpressionWindow.SetHeaders = _dbView.ColumnNames.ToList()
            //ExpressionWindow.SetHeaderTypes = _dbView.ColumnTypes.ToList()
            //ExpressionWindow.SetOutputType = GetType(Boolean)
            //If _dbView.NumberOfRows > 0 Then ExpressionWindow.SetDataForFirstRow = _dbView.GetRow(0)
            //For i = 0 To _dbView.ColumnNames.Count - 1
            //    AvailableFields.Items.Add(_dbView.ColumnNames(i))
            //Next

            var vars = new Dictionary<string, ResultType>();
            for (int i = 0; i < _dbView.ColumnNames.Length; i++)
            {
                if (vars.ContainsKey(_dbView.ColumnNames[i])) continue;
                vars.Add(_dbView.ColumnNames[i], Parser.TypeToResultType(_dbView.ColumnTypes[i]));
            }
            ExpressionCalculator.SetVariables(vars);

            // Wire up event handlers
            ExecuteButton.Click += ExecuteButton_Click;
            ContentRendered += SelectByAttribute_ContentRendered;
        }

        /// <summary>
        /// Gets the list of row indices that matched the selection criteria.
        /// This property is populated after the Execute button is clicked and the expression is evaluated.
        /// </summary>
        /// <value>A list of zero-based row indices that satisfy the selection expression.</value>
        public List<int> GetSelectedRows
        {
            get
            {
                return _selection;
            }
        }

        //Private Sub DisplayResult()
        //    'resulttextblock.Text = "Example: First data record (row id = 0) would be equal to '" & ExpressionWindow.GetResult & "'"
        //    ErrorLogButton.IsEnabled = False
        //    If _dbView.NumberOfRows > 0 Then ExecuteButton.IsEnabled = True
        //End Sub

        //Private Sub ErrorsFound(treeIsNothing As Boolean)
        //    If Not treeIsNothing Then
        //        _parseErrors = ExpressionWindow.GetTree.GetParseErrors
        //        _parseErrors.AddRange(ExpressionWindow.GetTree.GetErrorMessages)
        //        ErrorLogButton.IsEnabled = True
        //    End If
        //    ExecuteButton.IsEnabled = False
        //    resultTextBlock.Text = ExpressionWindow.GetResult
        //End Sub

        //Sub UpdateTextbox(texttoinsert As String)
        //    If IsNothing(_selectedtext) Then
        //    Else
        //        ExpressionWindow.Text = ExpressionWindow.Text.Remove(_caretIndex, _selectedtext.Count)
        //    End If
        //    _firstparts = Strings.Left(ExpressionWindow.Text, _caretIndex)
        //    _secondparts = Strings.Right(ExpressionWindow.Text, ExpressionWindow.Text.Length - _caretIndex)
        //    inserttext(texttoinsert)
        //End Sub

        //Sub InsertText(texttoinsert As String)
        //    ExpressionWindow.Text = _firstparts & texttoinsert & _secondparts
        //    ExpressionWindow.Focus()
        //    ExpressionWindow.CaretIndex = _firstparts.Length + texttoinsert.Length
        //    ExpressionWindow.Parse()
        //End Sub

        /// <summary>
        /// Handles the Click event of the ExecuteButton control.
        /// Evaluates the expression against all rows in the data table and populates the selection list.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="RoutedEventArgs"/> instance containing the event data.</param>
        private void ExecuteButton_Click(object sender, RoutedEventArgs e)
        {
            //Mouse.OverrideCursor = Cursors.Wait
            //Dim tree As FieldCalculationParser.ParseTreeNode = ExpressionWindow.GetTree
            //If Not tree Is Nothing Then
            //    If tree.containsVariable Then
            //        EVALUATECOLUMN(tree)
            //    Else
            //        MsgBox("Your Expression does not contain a variable, the resulting selection will be all rows or no rows...")
            //        EVALUATECOLUMN(tree)
            //    End If
            //End If
        }

        /// <summary>
        /// Evaluates the expression for each row in the data table.
        /// Rows where the expression evaluates to true are added to the selection list.
        /// </summary>
        /// <param name="t">The parser node representing the expression tree to evaluate.</param>
        private void EvaluateColumn(IParserNode t)
        {
            //    Dim headers As List(Of String) = t.GetHeaderNames
            //    Dim uniques As New List(Of String)
            //    For i As Int32 = 0 To headers.Count - 1
            //        If Not uniques.Contains(headers(i)) Then
            //            uniques.Add(headers(i))
            //        End If
            //    Next
            //    t.SetColNums(uniques)
            //    Dim types As New List(Of Type)
            //    Dim data As New List(Of Object())
            //    For i = 0 To uniques.Count - 1
            //        types.Add(_dbView.ColumnTypes(array.IndexOf(_dbView.ColumnNames, uniques(i)))) '_variablenames.IndexOf(uniques(i))))
            //        data.Add(_dbView.GetColumn(array.IndexOf(_dbView.ColumnNames, uniques(i))))
            //    Next
            //    Dim r(uniques.Count - 1) As Object
            //    Select Case t.Type
            //        Case FieldCalculationParser.TypeEnum.Bool
            //            For i = 0 To _dbView.NumberOfRows - 1
            //                For j = 0 To uniques.Count - 1
            //                    r(j) = data(j)(i)
            //                Next
            //                FieldCalculationParser.ParseTreeNode.RowOrCellNum = i
            //                t.Update(r)
            //                If t.Evaluate.GetResult Then
            //                    _selection.Add(i)
            //                End If
            //            Next
            //        Case FieldCalculationParser.TypeEnum.Str
            //            MsgBox("Cannot select by string, change the expression to yeild a boolean result")
            //            Mouse.OverrideCursor = Nothing
            //            Exit Sub
            //
            //        Case FieldCalculationParser.TypeEnum.Num
            //            MsgBox("Cannot select by numerical expression, change the expression to yeild a boolean result")
            //            Mouse.OverrideCursor = Nothing
            //            Exit Sub
            //    End Select
            //    Mouse.OverrideCursor = Nothing
            //    DialogResult = True
        }

        //Private Sub TextBox1_LostFocus(sender As Object, e As System.Windows.RoutedEventArgs) Handles ExpressionWindow.LostFocus
        //    _caretIndex = ExpressionWindow.CaretIndex
        //    _selectedtext = ExpressionWindow.SelectedText
        //End Sub

        //Private Sub EQ_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles EQButton.Click
        //    UpdateTextbox("=")
        //End Sub

        //Private Sub Exponent_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles ExponentButton.Click
        //    UpdateTextbox("^")
        //End Sub

        //Private Sub Divide_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles DivideButton.Click
        //    UpdateTextbox("/")
        //End Sub

        //Private Sub Multiply_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles MultiplyButton.Click
        //    UpdateTextbox("*")
        //End Sub

        //Private Sub Add_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles AddButton.Click
        //    UpdateTextbox("+")
        //End Sub

        //Private Sub Subtract_Click(sender As System.Object, e As System.Windows.RoutedEventArgs) Handles SubtractButton.Click
        //    UpdateTextbox("-")
        //End Sub

        //Private Sub AvailableFields_MouseDoubleClick(sender As Object, e As System.Windows.Input.MouseButtonEventArgs) Handles AvailableFields.MouseDoubleClick
        //    UpdateTextbox("[" & AvailableFields.SelectedItem & "]")
        //End Sub

        // ErrorLogButton removed — errors are now displayed inline by CalculatorControl's error expander.

        /// <summary>
        /// Handles the ContentRendered event of the SelectByAttribute window.
        /// Called after the window content has been rendered.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="EventArgs"/> instance containing the event data.</param>
        private void SelectByAttribute_ContentRendered(object? sender, EventArgs e)
        {
            //If ExpressionWindow.Text <> "" Then ExpressionWindow.Parse()
        }

        //Private Sub IsCaseSensitive_Checked(sender As Object, e As RoutedEventArgs) Handles IsCaseSensitive.Checked
        //    ExpressionWindow.IsCaseSensitive = IsCaseSensitive.IsChecked
        //End Sub

        //Private Sub IsCaseSensitive_UnChecked(sender As Object, e As RoutedEventArgs) Handles IsCaseSensitive.Unchecked
        //    ExpressionWindow.IsCaseSensitive = IsCaseSensitive.IsChecked
        //End Sub
    }
}
