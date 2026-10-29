# Control Gallery

Explore the application shell, editors, plotting tools, and graph controls in the WPF Framework demo applications. Each screenshot shows a running demo with its sample data or a small synthetic example.

Most controls are shown in the Blue theme. The shell comparison also shows Light and Dark; property editors and the expression calculator use Light, and the flow graph uses its demo's default appearance.

## Contents

- [FrameworkUI](#frameworkui)
  - [Application Shell](#application-shell)
  - [Theme Comparison](#theme-comparison)
  - [Hazard Document](#hazard-document)
- [GenericControls](#genericcontrols)
  - [Input Controls](#input-controls)
  - [Date, Time, and Color](#date-time-and-color)
  - [Property Editors](#property-editors)
  - [Data Grid](#data-grid)
- [NumericControls](#numericcontrols)
  - [Distribution Selector](#distribution-selector)
  - [Uncertain Curve Editor](#uncertain-curve-editor)
  - [Uncertain Table Editor](#uncertain-table-editor)
  - [Curve Editor](#curve-editor)
  - [Probability Ordinates](#probability-ordinates)
  - [Time Series](#time-series)
  - [Bin Definitions](#bin-definitions)
  - [Bivariate CDF](#bivariate-cdf)
- [OxyPlotControls](#oxyplotcontrols)
  - [Plot Toolbar](#plot-toolbar)
  - [Plot Properties](#plot-properties)
  - [Series Selector](#series-selector)
- [DatabaseControls](#databasecontrols)
  - [Table Viewer](#table-viewer)
- [ExpressionParserControls](#expressionparsercontrols)
  - [Calculator Control](#calculator-control)
- [DAGControls](#dagcontrols)
  - [Flow Graph Canvas](#flow-graph-canvas)

## FrameworkUI

Demo source: `src/FrameworkUI.Demo/`

### Application Shell

![FrameworkUI shell in blue theme](assets/gallery/frameworkui-shell-blue.png)

Application shell with project explorer, docking documents, properties, messages, menus, and toolbar.

See: [Architecture](architecture.md), [Themes](themes.md)

### Theme Comparison

![FrameworkUI shell in light theme](assets/gallery/frameworkui-shell-light.png)

Light theme capture of the same shell layout.

![FrameworkUI shell in dark theme](assets/gallery/frameworkui-shell-dark.png)

Dark theme capture of the same shell layout.

See: [Themes](themes.md)

### Hazard Document

![FrameworkUI hazard document in blue theme](assets/gallery/frameworkui-hazard-document-blue.png)

Hazard document with an OxyPlot chart, vertical plot toolbar, and editable properties. Separate document tabs provide tabular results and parameter sets.

See: [OxyPlot Controls](oxyplot-controls.md), [Generic Controls](generic-controls.md)

## GenericControls

Demo source: `src/GenericControls.Demo/`

### Input Controls

![Generic input controls in blue theme](assets/gallery/generic-input-controls-blue.png)

Name, numeric, slider, resizable text, grid length, and thickness input controls.

See: [Generic Controls](generic-controls.md)

### Date, Time, and Color

![Generic date, time, and color controls in blue theme](assets/gallery/generic-date-time-color-blue.png)

Clock, date/time inputs, color picker, and color picker popup.

See: [Generic Controls](generic-controls.md)

### Property Editors

![Generic property editors in light theme](assets/gallery/generic-property-editors-light.png)

Property editors for text, numbers, booleans, fonts, colors, lines, alignment, points, dates, and custom content.

See: [Generic Controls](generic-controls.md)

### Data Grid

![Generic data grid controls in blue theme](assets/gallery/generic-datagrid-blue.png)

Data grid toolbar and copy/paste data grid controls for editable tabular workflows.

See: [Generic Controls](generic-controls.md)


## NumericControls

Demo source: `src/NumericControls.Demo/`

### Distribution Selector

![Numeric distribution selector in blue theme](assets/gallery/numeric-distribution-selector-blue.png)

Normal distribution selector with parameters, a density plot and sample histogram, and summary statistics.

See: [Numeric Controls](numeric-controls.md)

### Uncertain Curve Editor

![Numeric uncertain curve editor in blue theme](assets/gallery/numeric-uncertain-curve-editor-blue.png)

Curve editor for ordered data with uncertainty. This illustrative example uses Normal distributions at six ordinates, with a standard deviation of 1 at each ordinate.

See: [Numeric Controls](numeric-controls.md)

### Uncertain Table Editor

![Numeric uncertain table editor in blue theme](assets/gallery/numeric-uncertain-table-editor-blue.png)

Table editor showing the same six illustrative Normal distributions as the uncertain curve example.

See: [Numeric Controls](numeric-controls.md)

### Curve Editor

![Numeric curve editor in blue theme](assets/gallery/numeric-curve-editor-blue.png)

Curve editor for ordered numeric data.

See: [Numeric Controls](numeric-controls.md)

### Probability Ordinates

![Numeric probability ordinates in blue theme](assets/gallery/numeric-probability-ordinates-blue.png)

Editor for probability ordinate sets.

See: [Numeric Controls](numeric-controls.md)

### Time Series

![Numeric time series table in blue theme](assets/gallery/numeric-time-series-blue.png)

Time series table showing the demo's built-in hourly example, with a constant value of 15 beginning July 30, 1980.

See: [Numeric Controls](numeric-controls.md)

### Bin Definitions

![Numeric bin definitions in blue theme](assets/gallery/numeric-bin-definitions-blue.png)

Editor for bin definitions used by numeric workflows.

See: [Numeric Controls](numeric-controls.md)

### Bivariate CDF

![Numeric bivariate CDF in blue theme](assets/gallery/numeric-bivariate-cdf-blue.png)

Bivariate empirical CDF control with its Plot tab selected.

See: [Numeric Controls](numeric-controls.md)

## OxyPlotControls

Demo source: `src/OxyPlotControls.Demo/`

### Plot Toolbar

![OxyPlot toolbar in blue theme](assets/gallery/oxyplot-toolbar-blue.png)

Heat map example with a vertical toolbar for pan, zoom, annotation, export, and properties.

See: [OxyPlot Controls](oxyplot-controls.md)

### Plot Properties

![OxyPlot properties in blue theme](assets/gallery/oxyplot-properties-blue.png)

Line-series example beside the properties panel, showing plot title, typography, plot area, and background settings.

See: [OxyPlot Controls](oxyplot-controls.md)

### Series Selector

![OxyPlot series selector in blue theme](assets/gallery/oxyplot-series-selector-blue.png)

Series selector for line, scatter, histogram, bar, box plot, area, heat map, pie, and large-data examples.

See: [OxyPlot Controls](oxyplot-controls.md)

## DatabaseControls

Demo source: `src/DatabaseControls.Demo/`

### Table Viewer

![Database table viewer in blue theme](assets/gallery/database-table-viewer-blue.png)

Table viewer displaying the [synthetic sample CSV](assets/gallery/samples/synthetic-gallery-sample.csv), with file/table selection, editable cells, row/column/cell selection, autofit columns, and field calculator access.

See: [Database Controls](database-controls.md)

## ExpressionParserControls

Demo source: `src/ExpressionParserControls.Demo/`

### Calculator Control

![Expression calculator in light theme](assets/gallery/expression-calculator-light.png)

Calculator control with an expression editor, operator buttons, case sensitivity option, and evaluated result output.

See: [Database Controls](database-controls.md)

## DAGControls

Demo source: `src/DAG.Demo/`

### Flow Graph Canvas

![DAG flow graph canvas with sample nodes and input and output connectors](assets/gallery/dag-flowgraph-canvas.png)

Flow graph canvas with sample nodes and their input and output connectors. The demo supports adding nodes from the canvas context menu and arranging them on the grid.

See: [DAG Controls](dag-controls.md)
