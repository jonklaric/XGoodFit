# Culture Smoke Tests — Manual Verification

This file documents the manual smoke tests for international number/date formatting that cannot be automated easily. Most culture handling is covered by unit tests (see `tests/*/CultureRoundTripTests.cs`, `DBFCultureRoundTripTests.cs`, `NumberFormatHelperCultureTests.cs`, etc.). These tests cover what unit tests cannot:

- AvalonDock layout serialization (no test project exists for AvalonDock)
- End-to-end WPF binding behavior with the user's actual Windows region setting
- Visual confirmation that displays render correctly across cultures

## Setup — switch Windows to German region

1. **Settings → Time & language → Language & region → Regional format**
2. Choose **German (Germany)** and click **Apply**
3. **Sign out and back in** (some apps cache the locale at startup)
4. Verify: open Calculator, type a decimal value — should show comma as decimal separator

## Test 1 — AvalonDock layout round-trip (HIGHEST PRIORITY)

**Why this is manual**: AvalonDock has no test project in the solution. The fix at `LayoutContent.cs:745, 749` (PreviousContainerIndex / InitialContainerIndex `int.Parse` with InvariantCulture) only manifests when reloading a layout file written under one culture on a machine running another. Since these are integers without decimal separators in the default .NET formatting, this is largely defensive — but worth verifying.

**Steps:**
1. Run **FrameworkUI.Demo** (or any host that uses AvalonDock for docking)
2. Drag a panel out to undock it (creates a floating window) — note its position
3. Save layout via **File → Save** (or whatever the host's save action is)
4. Close the application
5. Restart the application
6. Open the saved layout file
7. **Verify**: the previously-floating panel returns to the same position with the same size

**Expected file content sanity check** (open the saved layout `.xml` in a text editor):
- All numeric attributes should use `.` as decimal separator (e.g., `FloatingLeft="123.45"`)
- No `,` decimal separators in any numeric attribute regardless of Windows region

## Test 2 — OxyPlot settings round-trip

**Steps:**
1. Run **OxyPlotControls.Demo**
2. Configure a plot with non-integer axis ranges (e.g., Minimum=0.1, Maximum=99.9, MajorStep=0.5)
3. Save plot settings via the demo's save UI
4. Close, restart
5. Load the saved plot file
6. **Verify**: axis ranges restored exactly, plot looks identical

**Expected file content** (open the saved settings `.xml`):
- All `Minimum`, `Maximum`, `MajorStep` etc. attributes use `.` decimal separator
- File loads identically on a colleague's en-US machine

## Test 3 — DAG graph save/load (covered by automated tests, also worth manual)

**Steps:**
1. Run **DAG.Demo**
2. Create a graph with several nodes at non-integer positions (drag to fractional coordinates)
3. Save the graph
4. Close, restart, reload
5. **Verify**: nodes appear in the same positions

## Test 4 — DBF export

**Steps:**
1. Run **DatabaseControls.Demo**
2. Open or create a table with a numeric column containing values like `1234.5678`, `-987.654321`, `1e-10`
3. Export the table as DBF
4. Open the exported `.dbf` file in **Microsoft Excel** (or ArcGIS, QGIS — any DBF reader)
5. **Verify**: values appear correctly in Excel (American format with `.` decimals); not parsed as text

**Hex check** (optional — use a hex editor like HxD):
- Numeric column data should contain ASCII strings like `1.23456789012e+003` (period decimal)
- **Should NOT** contain `1,23456789012e+003` (comma decimal) regardless of Windows region

## Test 5 — User input (NumericTextBox / property controls)

**Steps:**
1. Run any demo with `NumericTextBox`, `NumericUpDown`, or property-grid controls (e.g., **GenericControls.Demo**)
2. Click a numeric field and type **1,5** (German format)
3. Tab away to commit
4. **Verify**: the underlying value is `1.5`, the field redisplays as `1,5`
5. Type **1.234,56** (German with thousands separator)
6. **Verify**: parses as `1234.56`, redisplays as `1.234,56`

## Test 6 — DataGrid copy-paste cross-culture

**Steps:**
1. Run **GenericControls.Demo** with a `CopyPasteDataGrid` showing numeric data
2. Copy a row containing `1.5` (Note: copy text from another app or have a colleague send a US-formatted CSV row)
3. Paste into the grid
4. **Verify**: parses correctly via the dual-culture fallback in `CopyPasteDataGrid` and `DataTableView.ConvertToColumnType`
5. Type `1,5` directly into a cell
6. **Verify**: also parses correctly (CurrentCulture leg)

## Test 7 — Distribution Selector statistics display

**Steps:**
1. Run **NumericControls.Demo** → Distribution Selector
2. Choose any distribution and load sample data
3. **Verify**: the summary statistics table displays values with German decimal separator (e.g., `1,2345`)

## Reverting

After testing, switch Windows back to your normal region:
- **Settings → Time & language → Language & region → Regional format → English (United States)**
- Sign out / back in

## Reporting issues

If any of the above fails under German region, file an issue with:
- Test number and step
- Expected vs actual behavior
- Screenshot of the failure
- Contents of the saved file (if applicable)

Other relevant cultures to test if you have the time:
- **fr-FR** (French) — comma decimal, narrow no-break space thousands
- **tr-TR** (Turkish) — has unusual `i`/`İ` casing rules that historically caused .NET regressions
- **fa-IR** (Persian) — uses non-Western digits `۰-۹`
- **ar-SA** (Arabic) — RTL layout, may need separate visual testing
