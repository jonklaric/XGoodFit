[<- Previous: Generic Controls](generic-controls.md) | [Back to Index](index.md) | [Next: OxyPlot Controls ->](oxyplot-controls.md)

# NumericControls Library

## Overview

The NumericControls library (`NumericControls` namespace) provides specialized WPF controls for statistical and numeric data management. It is designed for engineering and risk analysis applications requiring probability distribution configuration, ordered curve editing, uncertain data entry, time series management, and stratified sampling.

**Project reference:**
```xml
<ProjectReference Include="..\NumericControls\NumericControls.csproj" />
```

**XAML namespace:**
```xml
xmlns:nc="clr-namespace:NumericControls;assembly=NumericControls"
xmlns:uni="clr-namespace:NumericControls.Distributions.Univariate;assembly=NumericControls"
```

---

## Dependencies

NumericControls depends on several framework libraries and one NuGet dependency:

| Dependency | Type | Description |
|------------|------|-------------|
| **GenericControls** | Project reference | `ValidationDataGrid`, `NumericTextBox`, property controls |
| **OxyPlotControls** | Project reference | Plot integration for distribution and curve visualization |
| **Themes** | Project reference | Theme-aware styling and runtime theme switching |
| **OxyPlot / OxyPlot.Wpf** | Project reference | Chart rendering for PDF plots and curve previews |
| **[RMC.Numerics](https://github.com/USACE-RMC/Numerics)** | NuGet package | Statistical distribution types, ordered data structures, time series, and sampling utilities. Version is centrally managed in `Directory.Packages.props`. |

---

## DistributionSelectorControl

A comprehensive control for selecting, configuring, and visualizing univariate probability distributions. Provides a distribution type dropdown, interactive parameter editing, PDF plot, summary statistics, and optional parameter estimation from sample data.

### XAML Usage

```xml
<uni:DistributionSelectorControl
    SelectedDistribution="{Binding MyDistribution, Mode=TwoWay}"
    SampleData="{Binding MySampleData}"
    ShowPlot="True"
    ShowStatistics="True"
    ShowAxis="True"
    ShowAxisTitle="True"
    ExpandPlot="True"
    DistributionTitle="Annual Peak Flow" />
```

### Key Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SelectedDistribution` | `UnivariateDistributionBase` (DP) | `null` | The currently selected and configured distribution. Two-way bindable. |
| `Distributions` | `IList<UnivariateDistributionBase>` (DP) | All continuous types | The list of available distribution options. |
| `SampleData` | `double[]` (DP) | `null` | Sample data for histogram overlay and parameter estimation. |
| `BackgroundColor` | `Brush` (DP) | `White` | Control background color. |
| `ShowPlot` | `bool` (DP) | `true` | Whether the PDF plot is visible. |
| `ExpandPlot` | `bool` (DP) | `true` | Whether the plot expander is initially expanded. |
| `ShowAxis` | `bool` (DP) | `true` | Whether plot axes are visible. |
| `ShowAxisTitle` | `bool` (DP) | `true` | Whether axis titles are displayed. |
| `ShowAxisLabel` | `bool` (DP) | `false` | Whether axis tick labels are displayed. |
| `ShowStatistics` | `bool` (DP) | `true` | Whether the summary statistics table is visible. |
| `DistributionTitle` | `string` (DP) | `""` | Title text displayed above the distribution selector. |
| `ExpanderStyle` | `Style` (DP) | `null` | Custom style for the plot expander. |

### Features

- **Distribution selection:** Dropdown populated from `Distributions` property, defaulting to all continuous distribution types from the Numerics library.
- **Parameter editing:** Parameters displayed in a data grid with real-time validation. Invalid parameters are highlighted.
- **PDF visualization:** Interactive OxyPlot chart showing the probability density function. Automatically updates as parameters change.
- **Sample data overlay:** When `SampleData` is provided, a histogram of the sample data is plotted behind the PDF curve.
- **Parameter estimation:** When sample data is available and the selected distribution supports it, parameters can be estimated automatically.
- **Summary statistics:** Table comparing distribution statistics (mean, std dev, skewness, kurtosis, percentiles) with sample data statistics.
- **Goodness-of-fit:** Displays RMSE, Chi-Squared, and Kolmogorov-Smirnov test results when sample data is present.
- **Theme support:** Subscribes to `ThemeService.Instance.ThemeChanged` and applies matching OxyPlot themes automatically.

### Related Controls

- `DistributionWithSelectorControl` -- A combined view with a selector panel and a linked `DistributionSelectorControl`.
- `DistributionSelectorPopup` -- A popup version for inline distribution selection.
- `Selector` -- The standalone distribution type dropdown.

---

## Curve Editor Controls

### OrderedDataSelectorControl

A control that combines a `ValidationDataGrid` table editor with an OxyPlot chart for editing ordered X-Y paired data. Changes to the data grid automatically update the plot and vice versa.

```xml
<nc:OrderedDataSelectorControl
    SelectedOrderedData="{Binding MyCurve, Mode=TwoWay}"
    XAxisLabel="Discharge (cfs)"
    YAxisLabel="Stage (ft)"
    XColumnHeader="Discharge"
    YColumnHeader="Stage"
    OrderX="Ascending"
    IsStrictX="True"
    MinimumX="0"
    MaximumX="100000" />
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SelectedOrderedData` | `OrderedPairedData` (DP) | Empty | The ordered paired data collection. Two-way bindable. |
| `XAxisLabel` / `YAxisLabel` | `string` (DP) | `"X Axis"` / `"Y Axis"` | Plot axis labels. |
| `XColumnHeader` / `YColumnHeader` | `string` (DP) | `"X Data"` / `"Y Data"` | Data grid column headers. |
| `PlotTitle` | `string` (DP) | `null` | Plot title text. |
| `PlotLegendPosition` | `LegendPosition` (DP) | `BottomRight` | Legend placement on the plot. |
| `OrderX` / `OrderY` | `SortOrder` (DP) | `Ascending` | Sort order enforcement for each axis. |
| `IsStrictX` / `IsStrictY` | `bool` (DP) | `false` | Whether duplicate values are forbidden. |
| `MinimumX` / `MaximumX` | `double` (DP) | `double.MinValue` / `double.MaxValue` | Value bounds for X data. |
| `MinimumY` / `MaximumY` | `double` (DP) | `double.MinValue` / `double.MaxValue` | Value bounds for Y data. |
| `XAxisMinimum` / `XAxisMaximum` | `double` (DP) | `double.MinValue` / `double.MaxValue` | Plot axis range for X. |
| `YAxisMinimum` / `YAxisMaximum` | `double` (DP) | `double.MinValue` / `double.MaxValue` | Plot axis range for Y. |
| `IsReadOnly` | `bool` (DP) | `false` | Disables editing. |
| `TableWidth` | `GridLength` (DP) | `1*` | Width of the table portion in the split layout. |

### UncertainOrderedDataSelectorControl

Extends the curve editor concept for uncertain data, where each Y value is represented by a probability distribution rather than a single deterministic value. The control adds a distribution type selector and plots uncertainty bounds (min, max, mean, median, mode lines) around the curve.

```xml
<nc:UncertainOrderedDataSelectorControl
    SelectedUncertainOrderedData="{Binding MyUncertainCurve, Mode=TwoWay}"
    XAxisLabel="Exceedance Probability"
    YAxisLabel="Discharge (cfs)"
    XColumnHeader="Probability"
    YColumnHeader="Flow" />
```

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `SelectedUncertainOrderedData` | `UncertainOrderedPairedData` (DP) | Default instance | The uncertain ordered data. Two-way bindable. |
| `DistributionOptions` | `List<UnivariateDistributionType>` (DP) | Most continuous types | Available distribution types for uncertainty specification. |

The control shares the same axis, column header, ordering, and bounds properties as `OrderedDataSelectorControl`.

### UncertainOrderedDataTableEditor / UncertainTableEditor

Table-only editors for uncertain ordered data without the accompanying plot. Use these when plot visualization is handled separately.

---

## TimeSeriesTable

A control for displaying and editing time series data in a tabular format. Supports regular and irregular time intervals, date/time and value editing, and built-in mathematical operations on selected values.

### XAML Usage

```xml
<nc:TimeSeriesTable
    Series="{Binding MyTimeSeries, Mode=TwoWay}"
    XColumnHeader="Date"
    YColumnHeader="Discharge (cfs)"
    IsReadOnly="False"
    MinimumY="0"
    MaximumY="500000" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `Series` | `TimeSeries` (DP) | Empty | The time series data. |
| `XColumnHeader` | `string` (DP) | `"X Data"` | Header for the date/time column. |
| `YColumnHeader` | `string` (DP) | `"Y Data"` | Header for the value column. |
| `IsReadOnly` | `bool` (DP) | `false` | Disables editing. |
| `MinimumX` / `MaximumX` | `DateTime` (DP) | `DateTime.MinValue` / `DateTime.MaxValue` | Date range bounds. |
| `MinimumY` / `MaximumY` | `double` (DP) | `double.MinValue` / `double.MaxValue` | Value range bounds. |

### Events

| Event | Description |
|-------|-------------|
| `PreviewPasteData` | Raised before clipboard paste. Allows suppression for undo/redo batching. |
| `DataPasted` | Raised after paste completes. |
| `PreviewAddRows` | Raised before rows are added. |
| `RowsAdded` | Raised after rows are added. |
| `PreviewDeleteRows` | Raised before rows are deleted. |
| `RowsDeleted` | Raised after rows are deleted. |

### Features

- **Regular and irregular intervals:** For regular intervals (daily, monthly, etc.), dates are auto-calculated. For irregular intervals, dates are editable.
- **Math operations:** Right-click context menu provides mathematical functions (Add, Subtract, Multiply, Divide, Log, Exponentiate, Replace, Negate, Absolute Value, etc.) that can be applied to selected cells.
- **Undo/redo integration:** Events follow the suppress/unsuppress pattern for batching collection changes with the framework's undo/redo system.

---

## BinDefinitionControl

A control for defining stratified sampling bins. Displays a data grid where each row represents a stratification option with configurable parameters.

### XAML Usage

```xml
<nc:BinDefinitionControl
    StratificationOptionsCollection="{Binding Bins, Mode=TwoWay}" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `StratificationOptionsCollection` | `List<StratificationOptions>` (DP) | `null` | The collection of stratification bin definitions. |
| `ColumnHeaderStyle` | `Style` (DP) | `null` | Custom style for column headers. |
| `CellStyle` | `Style` (DP) | `null` | Custom style for data cells. |

---

## BivariateEmpiricalControl

A control for editing bivariate empirical cumulative distribution functions. Provides a two-dimensional data grid with row/column management, real-time validation, and plotting.

### XAML Usage

```xml
<nc:BivariateEmpiricalControl
    BivariateCDF="{Binding MyBivariateCDF, Mode=TwoWay}" />
```

### Properties

| Property | Type | Default | Description |
|----------|------|---------|-------------|
| `BivariateCDF` | `BivariateEmpirical` (DP) | `null` | The bivariate empirical distribution data. |

### Features

- **Row and column operations:** Add, insert, and delete both rows (X1 values) and columns (X2 values).
- **Toolbar buttons and context menu items** for column management are included alongside the standard row operations from `CopyPasteDataGrid`.
- **Validation:** Probabilities must be in [0, 1] range and X1/X2 values must be properly ordered.

---

## Theming

NumericControls participates in the framework's theme system. All controls use `DynamicResource` bindings to respond to runtime theme changes. The `DistributionSelectorControl` additionally subscribes to `ThemeService.Instance.ThemeChanged` to update its OxyPlot chart colors.

To include the NumericControls theme dictionary:

```xml
<ResourceDictionary Source="pack://application:,,,/NumericControls;component/Resources/ResourceDictionary.xaml" />
```

---

## Demo Application

The **NumericControls.Demo** project (`src/NumericControls.Demo/`) provides a working showcase of all controls in the library:

```bash
dotnet run --project src/NumericControls.Demo/NumericControls.Demo.csproj
```

> **Note:** The NumericControls.Demo restores [RMC.Numerics](https://github.com/USACE-RMC/Numerics) from NuGet through central package management.

---

## Best Practices and Troubleshooting

1. **Restore packages first.** NumericControls depends on the RMC.Numerics package. If package restore has not run, distribution selectors and curve editors will not build.

2. **Use two-way binding** on `SelectedDistribution`, `SelectedOrderedData`, `SelectedUncertainOrderedData`, and `Series` properties to ensure the control writes changes back to your view model.

3. **Handle events for undo/redo.** The `PreviewPasteData`/`DataPasted` and `PreviewAddRows`/`RowsAdded` event pairs follow a suppress/unsuppress pattern. Use `PreviewPasteData` to suppress collection change events, and `DataPasted` to unsuppress and raise a collection reset -- this enables atomic undo/redo of bulk operations.

4. **Configure validation bounds.** Set `MinimumX`, `MaximumX`, `MinimumY`, `MaximumY` on curve editor controls to enforce domain-specific constraints. Combine with `IsStrictX`/`IsStrictY` and `OrderX`/`OrderY` for complete ordering validation.

5. **The `DistributionSelectorControl` namespace is `NumericControls.Distributions.Univariate`**, not the base `NumericControls` namespace. Use the `uni:` prefix in XAML for this control.

6. **Time series date editing.** The date/time column in `TimeSeriesTable` is only editable when the time series uses `TimeInterval.Irregular`. For regular intervals, dates are computed automatically from the start date and interval.
