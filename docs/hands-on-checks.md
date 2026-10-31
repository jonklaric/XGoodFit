# Hands-on checks on Windows, macOS and Linux

XGoodFit's automated tests run the real application headlessly on all three systems in CI. They cover:
- every view against RMC-BestFit's own exported plots;
- every element of the 32 example projects, opened through the main window;
- the examples' walkthroughs, step by step;
- the project files, saved as RMC-BestFit 2.0.1 saves them.

Headless runs don't use the operating system's windows, dialogs, clipboard or display, so the checks below need a person. They take about an hour per system. Note anything that looks or behaves differently from RMC-BestFit, with the system, the build, and a screenshot.

## Getting a build

- **A test package:** open the repository's **Actions** tab, then the latest **Package** run, and download `xgoodfit-windows-x64`, `xgoodfit-macos` or `xgoodfit-linux-x64` from its artifacts. Install it as [install.md](install.md) describes. Test builds aren't signed, so Windows SmartScreen and macOS Gatekeeper ask first.
- **From source:** `dotnet run --project src/BestFit/RMC.BestFit.App -c Release`, after `scripts/fetch-sample-data.sh RMC-BestFit` and `native/build-hecdss.sh` (see the README).

The examples are in `sample-data/RMC-BestFit/examples/`, each with its walkthrough (`*.md`).

## The checks

| # | Check | What to look for |
|---|---|---|
| 1 | **Start** XGoodFit from the Start menu, Applications or the AppImage. | The main window opens with the project explorer, a blank project and the properties pane. Help, About shows the version. |
| 2 | **Open a project** with File, Open Project...: `1-time-series-data/1-usgs-download/usgs-download-example.bestfit`. | The system's own file dialog. The project's elements in the explorer, and its name in the window title. |
| 3 | **Open from the file manager:** register XGoodFit (install.md), then double-click a `.bestfit` file, or use Open With. | XGoodFit opens the file. With XGoodFit already running, macOS sends the file to it; on Windows and Linux a second XGoodFit starts, as a second RMC-BestFit does. |
| 4 | **Recent projects:** close XGoodFit, start it again, and open the File menu. | The project is listed there and opens. |
| 5 | **Follow a walkthrough by hand:** `4-univariate-distribution-analysis/1-univariate-analysis/1-information-expansion/viglione-et-al-2013.md`. Open its analyses, change the settings it changes, and press **Estimate**. | The progress shows and can be cancelled. The results match the walkthrough's figures and tables by eye. |
| 6 | **Plots:** in any analysis, zoom with the wheel, pan by dragging, zoom to a box, and reset. Use the toolbar's image buttons to save a PNG and an SVG, and to copy the image. | Smooth zoom and pan. The save dialog writes the files, which open in other applications. Paste the copied image into another application. |
| 7 | **Tables and the clipboard:** select cells in a result table and copy them (Ctrl+C, or Cmd+C on macOS), then paste them into a spreadsheet. In Input Data's exact data grid, paste a column from a spreadsheet, then undo. | Values and headers arrive as text in US formats. The paste fills the rows, and undo restores them. |
| 8 | **Keyboard:** Save Project, Undo and Redo (Ctrl+S, Ctrl+Z, Ctrl+Y; Cmd on macOS), F2 to rename an element in the explorer, and Delete to delete it, then undo. | The shortcuts work as RMC-BestFit's do. On macOS, note any shortcut that needs Ctrl instead of Cmd. |
| 9 | **Docking:** drag a pane (the properties, the Message Window) out to float, onto a second monitor if you have one, then dock it back. Close a pane and reopen it from View. Restart. | Panes float and dock back. The layout is kept after a restart. |
| 10 | **Themes:** Tools, Options...: Light, Dark, Blue. Restart. | Every view and dialog follows the theme, with no unreadable text. The theme is kept. |
| 11 | **Display scaling:** run at 150 % and 200 % (Windows' Display settings; a Retina or scaled display on macOS; fractional scaling on GNOME or KDE). | Crisp text and icons, nothing cut off or overlapping, and plots sharp. |
| 12 | **HEC-DSS import:** open `1-time-series-data/5-hec-dss-import/hec-dss-import-example.bestfit`, and in a time series' properties browse to its `.dss` file and import a path, as its walkthrough says. | The file dialog, the path selector, and the imported series. |
| 13 | **Downloads (needs the internet):** in a new time series, download from USGS, GHCN, CHMN and ABOM with the sites in `1-time-series-data`'s walkthroughs. | Each downloads and plots. Note any error message. |
| 14 | **Unsaved changes:** edit something, then close the project or XGoodFit. | The question about saving, which Cancel, Save and Don't Save each answer properly. |
| 15 | **Save and reopen:** save a changed example under a new name, open it again in XGoodFit, and, on Windows, open it in RMC-BestFit 2.0.1 too. | Both open it with the change. |
| 16 | **Help:** each Help menu item. | The documents open in the browser. Terms and About show. |
| 17 | **Batch run:** the explorer's Univariate Distribution Analysis menu, Batch Run, with two analyses. | Progress for each, and Cancel. The window can't be closed while it runs. |

## Known differences, not to report

- **Dates on a non-US system:** Created On and Last Modified show 1/1/0001 for the examples. Their dates are stored in US format, and RMC-BestFit does the same on such a system (`IssuesToRaise/WPF-Framework-dates-depend-on-current-culture.md`). Whether to fix it in XGoodFit is still to decide.
- **Fonts:** off Windows, Selawik stands in for Segoe UI, so text is a little different in width.
- **Signing:** test packages aren't signed (see install.md).
- **Walkthrough figures that differ:** a few walkthroughs describe earlier fits than their projects hold (Bayes Example #1, the classic time series' airline fit). XGoodFit shows what the project holds, as RMC-BestFit does.
