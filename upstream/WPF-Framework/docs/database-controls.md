# Database Controls

[<- Previous: OxyPlot Controls](oxyplot-controls.md) | [Back to Index](index.md) | [Next: DAG Controls ->](dag-controls.md)

## Overview

The database subsystem consists of three component groups that work together to provide tabular data management:

1. **DatabaseManager** -- a platform-agnostic model library providing a unified interface for reading, editing, and exporting tabular data across multiple storage formats.
2. **DatabaseControls** -- WPF controls for viewing, editing, searching, and analyzing database tables.
3. **ExpressionParser + ExpressionParserControls** -- a mathematical expression engine with WPF editor controls, used by the field calculator for expression-based column operations.

## DatabaseManager

**Namespace:** `DatabaseManager`
**Target:** `net10.0` (no Windows dependency)

### Supported Formats

| Class | Format | Notes |
|---|---|---|
| `SQLiteManager` | SQLite (.db, .sqlite) | Full read/write with optional password encryption |
| `CsvReader` | CSV (.csv) | Loads entire file into memory via `InMemoryReader` |
| `DbfReader` | dBASE (.dbf) | Binary parsing of legacy shapefile attribute tables |
| `InMemoryReader` | `DataTable` | Wraps an in-memory `DataTable` for the `DatabaseManager` interface |

### Abstract Classes

#### DatabaseManager

The abstract base class that all format-specific readers extend. Defines the contract for database lifecycle and table access.

```csharp
public abstract class DatabaseManager
{
    public string DataBasePath { get; }
    public bool DataBaseOpen { get; }
    public string[] TableNames { get; }

    public abstract void Open();
    public abstract void Close();
    public abstract DataTableView GetTableManager(string tableName);
    public abstract string[] GetTableNames();
    public abstract long GetStoredNumberOfRows(string tableName);
    public abstract int GetStoredNumberOfColumns(string tableName);

    // Static utility
    public static bool IsNumericType(Type typeToTest);
    public static void ConvertCsvToSqLite(string inputFile, string outputFile,
        string outputTableName, bool hasHeaders, int dataLineStartIndex,
        bool fieldsEnclosedInQuotes);
}
```

**Events:**

- `EditsSaved(string tableName, List<TableEdit> editsSaved)` -- raised after edits are committed to the database
- `PreviewEditsSaved(string tableName, ref bool cancel)` -- raised before committing, allowing cancellation

#### DataTableView

The abstract table view that provides cell-level access, edit tracking with undo/redo, and export capabilities.

```csharp
public abstract class DataTableView
{
    // Properties
    public DatabaseManager ParentDatabase { get; }
    public string TableName { get; }
    public string[] ColumnNames { get; }
    public Type[] ColumnTypes { get; }
    public int NumberOfRows { get; }

    // Cell access
    public object GetCell(int columnIndex, int rowIndex);
    public void EditCell(int rowIndex, int columnIndex, object value);

    // Editing
    public void ApplyEdits();       // Commit pending edits to storage
    public void CancelEdits();      // Discard pending edits
    public void UndoEdit();         // Undo the last edit
    public void RedoEdit();         // Redo the last undone edit

    // Structural operations
    public void AddRow();
    public void DeleteRow(int rowIndex);
    public void AddColumn(string name, Type type);
    public void DeleteColumn(int columnIndex);

    // Export
    public void ExportToExcel(string filePath);
    public void ExportToCsv(string filePath);
}
```

**Events:**

- `RowsDeleted(int[] rowIndices)` / `RowsAdded(int[] rowIndices)`
- `ColumnsDeleted(int[] columnIndices)` / `ColumnsAdded(int[] columnIndices)`
- `EditAdded(TableEdit edit)`

### Opening Databases

```csharp
// SQLite
var db = new SQLiteManager("data.sqlite");
db.Open();
DataTableView table = db.GetTableManager("MyTable");
object val = table.GetCell(0, 0);

// SQLite with password
var db = new SQLiteManager("encrypted.sqlite", "password123");

// CSV
var csv = new CsvReader("data.csv", hasHeaders: true,
    dataLineStartIndex: 0, fieldsEnclosedInQuotes: false);
DataTableView table = csv.GetTableManager(csv.TableNames[0]);

// DBF
var dbf = new DbfReader("attributes.dbf");
dbf.Open();

// In-memory DataTable
var dt = new DataTable("Results");
dt.Columns.Add("Value", typeof(double));
dt.Rows.Add(3.14);
var mem = new InMemoryReader(dt);
```

### Edit Tracking with Undo/Redo

`DataTableView` maintains an internal edit stack with an index pointer. All cell, row, and column modifications are recorded as `TableEdit` objects. Calling `UndoEdit()` decrements the pointer; `RedoEdit()` increments it. When a new edit is made after undoing, all edits beyond the current pointer are discarded.

Edit types include `CellEdit`, `MultiCellEdit`, `AddRowEdit`, `DeleteRowEdit`, `AddColumnEdit`, `DeleteColumnEdit`, and their batch variants (`AddRowsEdit`, `DeleteRowsEdit`, etc.).

```csharp
table.EditCell(0, 0, "NewValue");  // Records a CellEdit
table.UndoEdit();                   // Reverts to previous value
table.RedoEdit();                   // Reapplies "NewValue"
table.ApplyEdits();                 // Commits all edits to the database
```

## DatabaseControls

**Namespace:** `DatabaseControls`
**Target:** `net10.0-windows`

### TableViewer

The primary control for displaying and editing tabular data. This is a custom virtualized grid implementation -- it is **not** built on the WPF `DataGrid` or `CopyPasteDataGrid`. It provides its own rendering, selection, scrolling, sorting, copy/paste, and context menu infrastructure.

The `TableViewer` operates on a `DataTableView` instance from DatabaseManager and supports multiple selection modes (cell, row, column, all, edit).

### FindAndReplace

A column-scoped search dialog with support for:

- Case-sensitive and whole-word matching
- Find next / find previous navigation
- Replace and replace all operations

### ColumnStatsWindow

Displays summary statistics for a selected column. Automatically determines the appropriate statistics panel based on the column data type:

- **Numeric columns** (`NumericColumnStats`) -- count, minimum, maximum, mean, standard deviation, and additional statistical measures powered by the RMC.Numerics package
- **Text columns** (`AlphabeticColumnStats`) -- count, unique values, and frequency distribution

Statistics can be filtered to selected rows only.

### FieldCalculator

An expression-based calculator for creating or updating table columns. Operates in two modes:

1. **Column creation/update** -- evaluates an expression for each row and writes the result to a new or existing column
2. **Select by attribute** -- evaluates a boolean expression to filter rows matching a condition

The field calculator uses the ExpressionParser engine and provides column names as variables in the expression context.

## ExpressionParser

**Namespace:** `ExpressionParser`
**Target:** `net10.0` (no Windows dependency)

A mathematical expression parsing library with Excel-like syntax. Tokenizes and parses expressions into an abstract syntax tree (AST) for evaluation.

### Tokenizer

`Lexer.TokenizeStringToList(string expression)` performs lexical analysis to identify numbers, identifiers, keywords, operators, and string literals. Returns a `List<Token>`.

### Parser

`Parser.Parse` builds an AST from tokens or a raw string, returning an `IParserNode` tree that can be evaluated.

```csharp
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
```

### IParserNode Interface

All AST nodes implement `IParserNode`:

```csharp
public interface IParserNode
{
    bool ContainsErrors { get; }
    List<ParseError> GetErrors { get; }
    ResultType OutputType { get; }

    ParseNodeResult Evaluate();
    IParserNode Simplify();
    bool ContainsVariable();
    List<VariableNode> GetVariableNodes();
}
```

### Supported Functions

| Category | Functions |
|---|---|
| Conditional | `IF`, `AND`, `OR`, `CONTAINS` |
| String | `RIGHT`, `LEFT`, `LEN`, `SUBSTRING`, `INDEXOF`/`INSTRING`, `CONCATENATE` |
| Numeric | `ROUND`, `ROUNDUP`/`CEILING`, `ROUNDDOWN`/`FLOOR`, `RAND`, `RANDBETWEEN`, `INCREMENT` |
| Conversion | `DBL`/`CDBL`, `INT`/`CINT`, `STR`/`CSTR`, `BOOL`/`CBOOL` |
| Operators | `+`, `-`, `*`, `/`, `%`/`MOD`, `&` (concatenation), `=`, `<>`, `<`, `>`, `<=`, `>=` |

Keywords are case-insensitive. A leading `=` at position 0 is silently ignored for Excel copy-paste compatibility.

## ExpressionParserControls

**Namespace:** `ExpressionParserControls`
**Target:** `net10.0-windows`

### ExpressionControl

A `RichTextBox`-based expression editor with real-time syntax highlighting, color-coded parentheses (cycling through a 7-color palette), and clickable function hyperlinks.

**Key members:**

- `Text` dependency property (`string`) -- the expression text, suitable for two-way binding
- `GetTokenList` property -- returns the current parsed token list
- `ExpressionChanged` event -- raised when the expression is re-tokenized
- `HelpDocumentCalled` event -- raised when a function hyperlink is clicked

### CalculatorControl

A `UserControl` providing a calculator-style button panel for arithmetic operations. Integrates with an internal `ExpressionControl` for expression building and parse tree generation.

```csharp
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
```

### AvailableFunctions

A browsable list of all parser functions. Supports inserting a selected function into the expression editor and navigating to help documentation.

## Dependencies

| Dependency | Type | Used By |
|---|---|---|
| [RMC.Numerics](https://github.com/USACE-RMC/Numerics) | NuGet package | ColumnStatsWindow (statistical distributions) |
| GenericControls | Project | MessageBox, MetroWindow, property controls |
| Themes | Project | Theme colors and styles |
| OxyPlot / OxyPlot.Wpf | Project | Chart rendering in table viewer |
| OxyPlotControls | Project | Plot integration in DatabaseControls |
| ExpressionParser | Project | Core parser for FieldCalculator and ExpressionParserControls |
| System.Data.SQLite | NuGet | SQLite database access |
| ClosedXML | NuGet | Excel export |

## Demo Applications

- **DatabaseControls.Demo** -- demonstrates the TableViewer, FindAndReplace, ColumnStatsWindow, and FieldCalculator with sample SQLite databases. Located in the Demos solution folder.
- **ExpressionParserControls.Demo** -- demonstrates the ExpressionControl, CalculatorControl, and AvailableFunctions panel. Located in the Demos solution folder.

## Best Practices

- **Open and close databases explicitly** -- `SQLiteManager` and `DbfReader` require explicit `Open()` and `Close()` calls. `DataTableView.ApplyEdits()` will open the database automatically if it is closed, but explicit lifecycle management is preferred.
- **Check `PreviewEditsSaved` for cancellation** -- subscribe to `PreviewEditsSaved` on the `DatabaseManager` instance to validate or cancel edits before they are committed to the database.
- **Use `InMemoryReader` for transient data** -- when working with computed or temporary tabular data, wrap a `DataTable` in an `InMemoryReader` to get the full `DataTableView` API (undo/redo, export) without a file-backed database.
- **CSV files load entirely into memory** -- `CsvReader` wraps data in an `InMemoryReader` internally, so large CSV files will consume proportional memory.
- **Guard against re-entrancy in ExpressionControl** -- the `_formattingExpression` flag prevents recursive formatting. When setting `Text` programmatically, be aware that it triggers a reformat and re-tokenize cycle.
- **Provide variable types to the parser** -- when using `Parser.Parse` with variables, supply the `availableVariables` dictionary so the parser can validate types and flag undeclared variable references.
