[<- Previous: Undo/Redo](undo-redo.md) | [Back to Index](index.md) | [Next: Numeric Controls ->](numeric-controls.md)

# GenericControls Library

## Overview

The GenericControls library (`GenericControls` namespace) provides a comprehensive set of reusable WPF controls for .NET 10.0 desktop applications. It includes numeric input with validation, color selection, date/time picking, extended data grids with clipboard support, property editor panels, themed windows, and a collection of value converters.

**Project reference:**
```xml
<ProjectReference Include="..\GenericControls\GenericControls.csproj" />
```

**XAML namespace:**
```xml
xmlns:gc="clr-namespace:GenericControls;assembly=GenericControls"
```

---

## NumericTextBox

A text input control restricted to numeric values with built-in validation for range, format, and sign constraints. Supports culture-aware decimal and thousands separators via `NumberFormatHelper`.

### XAML Usage

```xml
<gc:NumericTextBox
    Text="{Binding FlowRate, Mode=TwoWay, UpdateSourceTrigger=LostFocus}"
    CanBeBlank="False"
    CanBeNegative="True"
    IsWholeNumber="False"
    AllowScientificNotation="True"
    MinValue="0"
    MaxValue="10000"
    BoundsAreExclusive="False" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` (DP) | `""` | The raw text content, suitable for two-way binding. |
| `CanBeBlank` | `bool` | `false` | Whether the input may be left empty. |
| `CanBeNegative` | `bool` | `false` | Whether negative numbers are permitted. |
| `IsWholeNumber` | `bool` | `false` | Restricts input to integers when `true`. |
| `AllowScientificNotation` | `bool` | `false` | Allows `e`/`E` notation (e.g., `1.5e-3`). |
| `MinValue` | `double` | `double.MinValue` | Lower bound for validation. |
| `MaxValue` | `double` | `double.MaxValue` | Upper bound for validation. |
| `BoundsAreExclusive` | `bool` | `false` | When `true`, values equal to the bounds are invalid. |
| `IsReadOnly` | `bool` (DP) | `false` | Disables editing. |
| `ValueIsValid` | `bool` (DP, read-only) | `true` | Whether the current value passes all validation. |
| `AboveMaxValue` | `bool` (DP, read-only) | `false` | Whether the value exceeds `MaxValue`. |
| `BelowMinValue` | `bool` (DP, read-only) | `false` | Whether the value is below `MinValue`. |
| `InvalidText` | `bool` (DP, read-only) | `false` | Whether the text cannot be parsed as a number. |

### Events

| Event | Description |
|-------|-------------|
| `TextChanged` | Raised when the text content changes. |

### Value Retrieval Methods

```csharp
double val = myNumericTextBox.GetValueAsDouble();
int    ival = myNumericTextBox.GetValueAsInteger();
float  fval = myNumericTextBox.GetValueAsSingle();
bool   ok   = myNumericTextBox.IsValidDouble();
```

---

## NumericUpDown

A spinner control with increment/decrement buttons, built on top of `NumericTextBox`. Mimics the behavior of the WinForms `NumericUpDown` control.

### XAML Usage

```xml
<gc:NumericUpDown
    Value="{Binding Iterations, Mode=TwoWay}"
    Minimum="1"
    Maximum="10000"
    Increment="100"
    DecimalPlaces="0"
    ThousandsSeparator="True" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Value` | `double` (DP) | `0.0` | The current numeric value. |
| `Minimum` | `double` | `-100` | Lower bound. |
| `Maximum` | `double` | `100` | Upper bound. |
| `Increment` | `double` | `1` | Step size per button click. Must be positive. |
| `DecimalPlaces` | `int` | `0` | Number of decimal places displayed (0--99). |
| `ThousandsSeparator` | `bool` | `false` | Whether to display group separators. |

---

## ColorPicker

An HSV color picker with a saturation/value preview area, hue slider, ARGB sliders, and preset color swatches.

### XAML Usage

```xml
<gc:ColorPicker
    Color="{Binding LineColor, Mode=TwoWay}" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Color` | `SolidColorBrush` (DP) | `Brushes.Black` | The selected color. Note: this is a `SolidColorBrush`, not `System.Windows.Media.Color`. |

> **Important:** Do not set `DataContext = this` in controls that host a `ColorPicker` -- it breaks external bindings. The `ColorPicker` intentionally avoids setting its own `DataContext`.

---

## NameTextBox

A text input control with validation for character limits, disallowed characters, and duplicate string detection. Ideal for entity naming fields where uniqueness and format constraints are required.

### XAML Usage

```xml
<gc:NameTextBox
    Text="{Binding ProjectName, Mode=TwoWay, UpdateSourceTrigger=LostFocus}"
    CanBeBlank="False"
    CharacterLimit="128"
    InvalidStrings="{Binding ExistingNames}" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Text` | `string` (DP) | `""` | The user-entered text. |
| `CharacterLimit` | `int` (DP) | `64` | Maximum allowed length. Set to `-1` for no limit. |
| `CanBeBlank` | `bool` (DP) | `false` | Whether empty text is valid. |
| `InvalidCharacters` | `char[]` (DP) | Invalid filename chars + `'`, `[`, `]` | Characters that are rejected. |
| `InvalidStrings` | `string[]` (DP) | `[]` | Strings that trigger a duplicate name error. |
| `IsValid` | `bool` (DP) | `true` | Whether the current text passes all validation rules. |

### Validation

The control validates automatically on every text change. Validation checks run in order:

1. Character limit exceeded
2. Blank text (when `CanBeBlank` is `false`)
3. Duplicate name (text matches an entry in `InvalidStrings`)
4. Invalid character detected

When validation fails, `IsValid` is set to `false` and a descriptive tooltip is shown. Call `GetErrorMessages()` to retrieve all current error strings programmatically.

---

## Date and Time Controls

### DateAndTimePickerControl

A combined calendar and clock control for selecting both date and time.

```xml
<gc:DateAndTimePickerControl
    DateAndTime="{Binding StartDate, Mode=TwoWay}"
    Is24Hour="True"
    HasSeconds="True" />
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DateAndTime` | `DateTime` (DP) | `1980-07-30 12:00` | The selected date and time. |
| `Is24Hour` | `bool` (DP) | `false` | Use 24-hour clock format. |
| `HasSeconds` | `bool` (DP) | `false` | Enable seconds selection. |

### DateAndTimeTextBoxControl

A text-based date/time input with parsing and formatting.

```xml
<gc:DateAndTimeTextBoxControl
    SelectedDateTime="{Binding EventTime, Mode=TwoWay}" />
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SelectedDateTime` | `DateTime` (DP) | `2017-08-07 20:35:23` | The selected date and time. |

### ClockControl

An analog clock face for time selection, used internally by `DateAndTimePickerControl` but also available standalone.

---

## CopyPasteDataGrid

An extended `DataGrid` with built-in clipboard copy/paste, row add/insert/delete, column sorting, and context menu support. This is the foundation for all data grid controls in the framework.

### XAML Usage

```xml
<gc:CopyPasteDataGrid
    ItemsSource="{Binding DataRows}"
    CanUserAddInsertDeleteRows="True"
    IsReadOnly="False" />
```

### Features

- **Clipboard operations:** Copy, Copy with Headers, and Paste via context menu or keyboard shortcuts (Ctrl+C, Ctrl+V)
- **Row manipulation:** Add, Insert, and Delete rows via context menu or toolbar
- **Column sorting:** Sort ascending, descending, or clear sort via column header context menu
- **Custom context menu items:** Add application-specific menu items via `CustomMenuItems`
- **Row type inference:** Automatically determines row type from `ItemsSource`, or set explicitly via `RowType`
- **Paste with row expansion:** When `PasteAddsRows` is `true`, pasting data beyond the current row count adds new rows automatically

### Key Properties

| Property | Type | Description |
|----------|------|-------------|
| `CanUserAddInsertDeleteRows` | `bool` | Enables or disables Add/Insert/Delete row operations. Default `true`. |
| `RowType` | `Type` | Explicit row type for creating new rows. If `null`, inferred from `ItemsSource`. |

### Events

| Event | Signature | Description |
|-------|-----------|-------------|
| `PreviewPasteData` | `(string[][] clipboardData, ref bool cancelPaste)` | Raised before paste. Set `cancelPaste` to cancel. |
| `DataPasted` | `()` | Raised after paste completes. |
| `PreviewAddRows` | `(int startRowIndex, int nRows, ref bool cancelAddRows)` | Raised before rows are added. |
| `RowsAdded` | `(int startrow, int numrows)` | Raised after rows are added. |
| `PreviewDeleteRows` | `(List<int> rowindices, ref bool cancel)` | Raised before rows are deleted. |
| `RowsDeleted` | `(List<int> rowindices)` | Raised after rows are deleted. |

> **Requirement:** `ItemsSource` must implement `IList`. Non-continuous cell selections will produce an error on paste.

---

## ValidationDataGrid

Extends `CopyPasteDataGrid` with per-cell validation support. Cells with validation errors are highlighted with configurable border and background colors, and error messages appear as tooltips.

### XAML Usage

```xml
<gc:ValidationDataGrid
    ItemsSource="{Binding ValidatedRows}"
    ErrorCellBorderBrush="Red"
    ErrorCellBackgroundBrush="#FFF7B6AF" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `ErrorCellBorderBrush` | `Brush` (DP) | `Brushes.Red` | Border color for cells with errors. |
| `ErrorCellBackgroundBrush` | `Brush` (DP) | Light red | Background color for cells with errors. |

### DataGridRowItem Pattern

To use `ValidationDataGrid`, create a row item class that derives from `DataGridRowItem`:

```csharp
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
```

**Key members of `DataGridRowItem`:**

| Member | Description |
|--------|-------------|
| `AddValidationRules()` | Abstract. Override to define validation rules using `AddRule`. |
| `AddRule(propertyName, errorCondition, errorMessage, associatedProperties)` | Registers a validation rule. Multiple rules per property are supported. |
| `PropertyDisplayName(propertyName)` | Abstract. Returns the column header display name. |
| `IsGridDisplayable(propertyName)` | Abstract. Returns whether the property should appear as a column. |
| `UniqueRule(propertyName, errorMessage)` | Built-in helper that checks for duplicate values across all rows. |
| `OrderRule<T, DT>(callback, propertyName, ascending, canBeEqual)` | Built-in helper that validates ordering constraints between adjacent rows. |
| `ForceValidation()` | Validates all rules on all properties. |
| `RuleMap` | Dictionary mapping property names to `PropertyRule` instances. |

---

## DataGridToolbar

A toolbar control that provides standard DataGrid operations as icon buttons. Bind it to a `CopyPasteDataGrid` instance to automatically wire up Add, Insert, Delete, Select All, Copy, Copy with Headers, and Paste actions.

### XAML Usage

```xml
<gc:DataGridToolbar DataGrid="{Binding ElementName=MyDataGrid}" />

<gc:CopyPasteDataGrid x:Name="MyDataGrid"
    ItemsSource="{Binding Items}" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `DataGrid` | `CopyPasteDataGrid` (DP) | `null` | The data grid to control. |
| `ToolOrientation` | `Orientation` (DP) | `Horizontal` | Layout direction of toolbar buttons. |
| `BackgroundColor` | `SolidColorBrush` (DP) | `Transparent` | Toolbar background color. |
| `CustomButtons` | `ObservableCollection<Button>` | Empty | Collection for adding custom toolbar buttons. |

---

## Property Controls

A family of labeled property editor controls that display a title on the left and an input on the right, connected by a leader line. These are designed for property panels and settings UIs.

### NumericPropertyControl

```xml
<gc:NumericPropertyControl
    Title="Flow Rate"
    Number="{Binding FlowRate, Mode=TwoWay}"
    MinValue="0"
    MaxValue="100000"
    BoundsAreExclusive="False"
    CanHaveNegative="False"
    IsWholeNumber="False"
    CanHaveNaN="False" />
```

Key properties: `Number` (DP, `double`), `Title` (DP), `MinValue` (DP), `MaxValue` (DP), `BoundsAreExclusive` (DP), `CanHaveNegative` (DP), `CanHaveNaN` (DP), `IsWholeNumber` (DP), `IsTextBoxEnabled` (DP), `PropertyWidth` (DP), `ShowLeaderLine` (DP), `ValueIsValid`.

### Other Property Controls

| Control | Purpose | Key Property |
|---------|---------|-------------|
| `BooleanPropertyControl` | Checkbox with label | `IsChecked` |
| `TextPropertyControl` | Text input with label | `Text` |
| `ColorPropertyControl` | Color picker with label | `Color` (SolidColorBrush) |
| `FileSelectorControl` | File path with browse button | `FilePath` |
| `FolderSelectorControl` | Folder path with browse button | `FolderPath` |
| `DirectorySelectorControl` | Directory selection | `Directory` |
| `FontSelectorControl` | Font family picker | `SelectedFont` |
| `FontWeightSelectorControl` | Font weight picker | `SelectedFontWeight` |
| `LineStyleSelectorControl` | Line dash style picker | `SelectedLineStyle` |
| `LineWidthSelectorControl` | Line width picker | `SelectedLineWidth` |
| `HorizontalAlignmentControl` | Alignment picker | `SelectedAlignment` |
| `VerticalAlignmentControl` | Vertical alignment picker | `SelectedAlignment` |
| `DateTimePropertyControl` | Date/time with label | `DateTime` |
| `NumericSliderPropertyControl` | Slider with label | `Value` |
| `PointPropertyControl` | X/Y point input | `Point` |
| `Point3DPropertyControl` | X/Y/Z point input | `Point3D` |
| `ContentPropertyControl` | Generic content host | `Content` |
| `ResizeableTextPropertyControl` | Resizable text input | `Text` |
| `StringListPropertyControl` | String list editor | `Items` |
| `NameTextPropertyControl` | Name text with label | `Text` |
| `NumericAutoPropertyControl` | Numeric with auto toggle | `Number`, `IsAuto` |
| `NumericPropertySelectorControl` | Numeric with selector | `Number` |
| `CalendarWeekRulePropertyControl` | Calendar week rule | `SelectedRule` |

All property controls share common layout properties: `Title`, `PropertyWidth`, `MinPropertyWidth`, `MaxPropertyWidth`, and `ShowLeaderLine`.

---

## Converters

The following `IValueConverter` implementations are available for XAML bindings:

| Converter | Description |
|-----------|-------------|
| `ReverseBooleanConverter` | Inverts a boolean value. |
| `BooleanToColorConverter` | Maps `true`/`false` to configurable `Color` values. |
| `BooleanToBrushConverter` | Maps `true`/`false` to configurable `Brush` values. |
| `BooleanToTextConverter` | Maps `true`/`false` to configurable strings. |
| `BooleanToDoubleConverter` | Maps `true`/`false` to configurable doubles. |
| `BooleanToVisibilityConverter` | Located in `GenericControls` namespace. |
| `VisibilityToBooleanConverter` | Converts `Visibility` to `bool`. |
| `InRangeConverter` | Returns `true` if a value is within a defined range. |
| `AlwaysVisibleConverter` | Always returns `Visibility.Visible`. |
| `TimeTextConverter` | Formats time values for display. |
| `DataGridWidthConverter` | Converts widths for DataGrid columns. |
| `DoubleToThicknessConverter` | Converts `double` to `Thickness`. |
| `ThicknessToDoubleConverter` | Converts `Thickness` to `double`. |
| `ColorToSolidBrushConverter` | Converts `Color` to `SolidColorBrush`. |
| `FontToFontFamilyConverter` | Converts font string to `FontFamily`. |
| `FontFamilyToFontStringConverter` | Converts `FontFamily` to string. |
| `IntToDoubleConverter` | Converts `int` to `double`. |
| `DoubleToStringConverter` | Converts `double` to formatted string. |
| `StringToDoubleConverter` | Parses string to `double`. |
| `DoubleToDataGridLengthConverter` | Converts `double` to `DataGridLength`. |
| `DoubleToGridLengthConverter` | Converts `double` to `GridLength`. |
| `DoubleToCornerRadiusConverter` | Converts `double` to `CornerRadius`. |
| `VectorToPointConverter` | Converts `Vector` to `Point`. |
| `TabSizeConverter` | `IMultiValueConverter` for tab sizing. |
| `DoubleToNAConverter` | Displays `NaN` as "N/A". |
| `StringToNAConverter` | Displays empty strings as "N/A". |
| `HorizontalAlignmentToTextAlignmentConverter` | Maps alignment enums. |
| `GridlineColorLightConverter` | Lightens a color for grid lines. |

---

## NumberFormatHelper

A static utility class providing culture-aware number parsing and formatting. All numeric controls in the framework use this class internally instead of raw `double.TryParse` calls.

### Key Methods

```csharp
// Parsing
bool success = NumberFormatHelper.TryParseDouble(text, out double result);
bool success = NumberFormatHelper.TryParseSingle(text, out float result);
bool success = NumberFormatHelper.TryParseInt(text, out int result);

// Formatting
string formatted = NumberFormatHelper.FormatDouble(value);
string formatted = NumberFormatHelper.FormatDouble(value, "F2");
string formatted = NumberFormatHelper.FormatDouble(value, decimalPlaces: 3, useThousandsSeparator: true);

// Input validation
bool valid = NumberFormatHelper.IsValidNumericInput(inputText, currentText, selectionStart,
    selectedText, allowNegative: true, allowDecimal: true, allowScientific: false);
bool partial = NumberFormatHelper.IsValidPartialNumber(text);

// Culture properties
string decSep  = NumberFormatHelper.DecimalSeparator;   // e.g., "." or ","
string grpSep  = NumberFormatHelper.GroupSeparator;      // e.g., "," or "."
string negSign = NumberFormatHelper.NegativeSign;        // e.g., "-"

// International support
bool isDigit = NumberFormatHelper.IsNumericDigit(c);     // Any Unicode digit
string normalized = NumberFormatHelper.NormalizeDigits(text); // Convert to 0-9
FlowDirection dir = NumberFormatHelper.CurrentFlowDirection; // RTL support
```

---

## Demo Application

The **GenericControls.Demo** project (`src/GenericControls.Demo/`) provides a working showcase of all controls in the library. Build and run it to explore each control interactively:

```bash
dotnet run --project src/GenericControls.Demo/GenericControls.Demo.csproj
```

---

## Best Practices

1. **Always use `NumberFormatHelper`** for numeric parsing and formatting instead of raw `double.TryParse`. This ensures correct behavior across all locales.

2. **Bind `IsReadOnly` for conditional editing.** Both `NumericTextBox` and `CopyPasteDataGrid` expose `IsReadOnly` as a dependency property for XAML binding.

3. **Derive from `DataGridRowItem`** for validated data grids. This gives you per-cell error highlighting, tooltips, and built-in uniqueness/ordering rules.

4. **Use `DynamicResource` for theme-aware brushes.** Error brushes on `ValidationDataGrid` automatically respond to theme changes when bound to theme resource keys.

5. **Do not set `DataContext = this`** in UserControl constructors that host GenericControls. This breaks external bindings from parent controls.

6. **The `ColorPicker.Color` property is `SolidColorBrush`**, not `System.Windows.Media.Color`. Bind to brush-typed properties directly.

7. **`CopyPasteDataGrid` requires continuous cell selection** for paste operations. Non-continuous selections will display an error message.
