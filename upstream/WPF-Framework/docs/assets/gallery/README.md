# Gallery Screenshot Checklist

These screenshots support [the public control gallery](../../gallery.md). Capture the running demo applications in `src/` and keep filenames, theme labels, and captions consistent with the images.

## FrameworkUI

- `frameworkui-shell-blue.png`
- `frameworkui-shell-light.png`
- `frameworkui-shell-dark.png`
- `frameworkui-hazard-document-blue.png`

Launch `FrameworkUI.Demo`. The demo creates a blank temporary project on startup. Use **Project > New Hazard Element...**, or right-click **Hazard Functions** and choose **New Hazard Element...**, to add a named sample hazard. Double-click the new element to open its document. Use the demo's distribution and property controls to display a meaningful example.

Choose the theme under **Tools > Options > Application**. Keep the same window size, docking layout, selected document, and data for the Blue, Light, and Dark shell captures. Capture the hazard document in Blue with its chart and properties visible; its tabular results and parameter sets are separate tabs.

The captured **Example Hazard** uses Log-Pearson Type III with mean (log) `3`, standard deviation (log) `0.5`, skew (log) `0`, effective record length `100`, and uncertainty enabled. These settings form an illustrative demo example.

## GenericControls

- `generic-input-controls-blue.png`
- `generic-date-time-color-blue.png`
- `generic-property-editors-light.png`
- `generic-datagrid-blue.png`

Launch `GenericControls.Demo` and select the matching control groups. Use Blue for the input, date/time/color, and data grid images. Use Light for the property editor image. Keep representative controls and values visible without clipping.

## NumericControls

- `numeric-distribution-selector-blue.png`
- `numeric-uncertain-curve-editor-blue.png`
- `numeric-uncertain-table-editor-blue.png`
- `numeric-curve-editor-blue.png`
- `numeric-probability-ordinates-blue.png`
- `numeric-time-series-blue.png`
- `numeric-bin-definitions-blue.png`
- `numeric-bivariate-cdf-blue.png`

Launch `NumericControls.Demo`, select Blue, and visit each corresponding tab. Use the sample values initialized by the demo except for the uncertainty example described below. Ensure tables, distribution parameters, and plots have finished rendering before capture.

The uncertain curve and uncertain table captures share six illustrative Normal distributions entered through the demo UI: X values `[0, 2, 4, 6, 8, 10]`, means `[30, 24, 19, 15, 12, 10]`, and standard deviation `1` for every row. These are demonstration values, not measured data.

On **Time Series**, select the built-in **Hourly** example: constant value `15`, beginning `1980-07-30`. The pictured data comes from the local Hourly example. The separate USGS option retrieves external data. On **Bivariate CDF**, select the **Plot** tab.

## OxyPlotControls

- `oxyplot-toolbar-blue.png`
- `oxyplot-properties-blue.png`
- `oxyplot-series-selector-blue.png`

Launch `OxyPlotControls.Demo` and select Blue. Select **Heat Map Series** for the toolbar capture. Use **Line Series** with **General Plot Settings** visible for the properties capture. Open the **Series Type** list for the selector capture, keeping the line-series example visible behind it.

## DatabaseControls

- `database-table-viewer-blue.png`

Launch `DatabaseControls.Demo`, select Blue, and load the [synthetic sample CSV](samples/synthetic-gallery-sample.csv). The capture uses 12 fictional records with **Sample ID**, **Category**, **Value**, **Units**, and **Source** columns. Each row is labeled **Synthetic** and uses **example units**. These records demonstrate the table controls and are not observed measurements or production data. The demo starts without a loaded table.

## ExpressionParserControls

- `expression-calculator-light.png`

Launch `ExpressionParserControls.Demo`. This demo initializes Light and has no theme selector. Enter `((120 + 80) / 2) ^ 2` so the editor, operator controls, and evaluated result `10000` are visible. This standalone demo does not host the optional function browser.

## DAGControls

- `dag-flowgraph-canvas.png`

Launch `DAG.Demo` and retain its default appearance; it does not expose the framework theme selector. The canvas starts empty. Add sample nodes from the toolbar or canvas context menu and arrange them with room for their labels and input/output connectors. Capture the actual populated canvas.

The demo's **Save** and **Load** handlers are placeholders and cannot save or open graph files. Connection creation is intended to use a drag from an output circle to an input circle, but this workflow was not verified for the gallery capture. The screenshot and caption therefore describe the visible nodes and connectors without claiming a successful connection.

## Capture Notes

The 18 new screenshots were captured on **2026-10-07** from Release demo builds at commit `05acb1e`. The four preexisting GenericControls images were retained. The new screenshots retain their native pixel resolution, with focused regions cropped from JPEG captures and saved as PNG.

- Capture actual demo windows or focused control regions, with text readable at the gallery's displayed size.
- Prefer 1200–1600 px wide PNGs for full windows when the display permits. Keep focused controls at their native resolution; do not upscale smaller captures.
- Use the demos' bundled or initialized sample inputs where available. The database table is an explicitly synthetic gallery example.
- Keep window dimensions and framing consistent within each group, especially the shell theme comparison.
- Wait for layout and rendering to settle. Move the pointer away from the focal content and dismiss transient tooltips or unrelated dialogs.
- Avoid local paths, personal data, debug windows, and unrelated desktop content.
- Inspect every image for cropped labels, blank plots, selection artifacts, and unintended horizontal scrolling.
- Verify that every image referenced by `docs/gallery.md` exists and that its theme matches its filename and alternative text.
