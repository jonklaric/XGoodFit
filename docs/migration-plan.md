# RMC-BestFit Cross-Platform Port (WPF → Avalonia): Migration Plan

**Status:** v0.4 · 2026-10-30 · **XGoodFit** ([github.com/jonklaric/XGoodFit](https://github.com/jonklaric/XGoodFit), MIT) · decisions in [§12](#12-decisions-resolved-2026-10-29) · after-1.0 items in [§13](#13-post-10-backlog) · Phase 0 in progress ([§7](#phase-0-groundwork--de-risking-spikes-m))

**Upstream baselines analysed** (shallow clones, `HEAD` of default branch on 2026-10-29):

| Repo | Commit | Version |
|---|---|---|
| [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) | `bfce2dd` | 2.0.1 |
| [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) | `7f897e1` | 1.0.5 |
| [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics) | `7e8e8d1` | 2.2.0 |
| [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss) | `1da5976` | 7-JA-7 (newest release, not `HEAD`; see §7, Phase 0) |
| [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) | `f8f576f` | 2.0.3 |

---

## 0. Summary

- **Most of BestFit is already cross-platform.** The statistical engine (`RMC.BestFit`, 71K lines), the REST/MCP API, and `RMC.Numerics` already target plain `net10.0`, and have **no** Windows-only APIs. Only the desktop layers are Windows-bound: `RMC.BestFit.UI` (light WPF use), `RMC.BestFit.App` (heavy WPF use), and the `RMC.Wpf.Framework.*` packages they sit on.
- **The real work is porting the WPF-Framework subset BestFit uses, then the App.** That is roughly **110K lines of C# and 34K lines of XAML** to touch, plus ~31K lines of tests. The vendored **AvalonDock** (40K lines) and **OxyPlot.Wpf** (26K lines) are *replaced*, not ported.
- **Truly Windows-only code is small:**
  - five P/Invokes (`user32`/`shell32`);
  - a Registry-based recent-files list;
  - Win32 file and folder dialogs;
  - one `WebBrowser`;
  - a few `RichTextBox`es;
  - an XPS exporter BestFit never calls.
- **The hard parts are structural, not Windows-specific:**
  1. **Excel-like DataGrids.** These rely on cell selection, `SelectedCells` and `CanUserAddRows`. Avalonia's official DataGrid is deprecated and row-only. → Use **ProDataGrid** (MIT).
  2. **A full custom VS2013 theme.** It has Light, Dark and Blue variants, ~260 control templates, ~800 triggers and ~2,300 `DynamicResource` references.
  3. **A forked OxyPlot.** It has custom probability axes, and its plot settings are **serialized into `.bestfit` project files**.
  4. **Large code-behinds.** `InputDataControl.xaml.cs` is 3.7K lines and `OxyPlotToolbar.xaml.cs` is 4.8K. They are full of synchronous dialogs: ~210 `MessageBox`, `ShowDialog`, file-dialog and clipboard call sites.
  5. **The HEC-DSS native library.** It needs macOS and Linux-arm64 builds.
- **Recommended approach: a faithful 1:1 port.** Use Avalonia **12.1** with an MIT-only stack:
  - Dock.Avalonia
  - ProDataGrid
  - the OxyPlot fork's own WPF layer, ported (with oxyplot-avalonia as the reference)
  - AvaloniaEdit

  Keep the same project, class and file structure, namespaces and code-behind flow, so logic is preserved and upstream changes can be re-ported release by release. Reproduce the VS2013 look by porting the theme's palettes and templates. Prove parity with automated screenshot diffs and file round-trip tests.
- **Decisions made ([§12](#12-decisions-resolved-2026-10-29)):**
  - An unofficial community port named **XGoodFit**, MIT-licensed.
  - One Avalonia codebase for Windows x64, macOS (Apple Silicon and Intel) and Linux x64.
  - The same look on every OS.
  - Free and open-source dependencies only, plus your Apple Developer ID for signing.
  - DSS import on every platform at 1.0.
  - Velopack for installers and updates.
  - US date and number formats for now.
  - Only the parts of WPF-Framework that BestFit needs.

---

## 1. Goals, non-goals, principles

**Goals**
- Run RMC-BestFit natively on **Windows x64, macOS (Apple Silicon and Intel) and Linux x64** from a single Avalonia codebase, which also replaces the WPF app on Windows.
- Release it as an **unofficial community port under its own name** (§1.1).
- Make it **as visually close to the WPF original as possible**: Light, Dark and Blue VS2013 themes, the same layout, the same plots.
- Change **no** statistical logic and remove **no** functionality.
- Keep `.bestfit` files **bidirectionally compatible** with the Windows WPF release.

**Non-goals (for 1.0):** redesigning the UI; refactoring to MVVM; adding features; changing the model library or Numerics.

**Guiding principles**
1. **Port, don't redesign.** Keep file, class, member and namespace names identical. Re-type, don't restructure: avoid MVVM extraction, renames and "improvements" (they can come later as separate, reviewable changes). Upstream deltas then map 1:1 onto the port.
2. **Leave the model library untouched.** Build `RMC.BestFit` and `RMC.Numerics` 2.2.0 from the vendored upstream source. Numerics' tag `v2.2.0` is the exact source of the NuGet package upstream uses; the root `Directory.Build.rsp` turns on RMC-BestFit's own `UseLocalRmcNumerics` switch.
3. **Data compatibility is non-negotiable.** Both apps must produce the same SQLite content and the **same plot-settings XML**.
4. **Look-alike over native look.** Reproduce the VS2013 themes. Accept only unavoidable OS differences: font rasterization, and the Linux title bar.
5. **Use an OSS-only dependency stack** (MIT, OFL or public domain) with pinned versions. Wrap third-party controls behind the framework's existing APIs so they can be swapped.
6. **Show parity, don't just claim it.** Use CI on all three OSes, ported tests, file round-trip suites and screenshot diffs against WPF.

### 1.1 Name and identity (D3)

- **Name: XGoodFit** (chosen 2026-10-29). Keep "RMC" and "USACE" out of product names, since they suggest an official release. Run a quick trademark search before the first public release.
- **Only the branding layer changes:** window title, About box, executable and `.app` names, bundle ID and Velopack pack ID, settings folder, installer and update-feed names. Library and namespace names (`RMC.BestFit.*`, `FrameworkUI`, …) stay identical to upstream, so upstream diffs stay small. A working codename is used until the name is chosen.
- **Keep the `.bestfit` extension**, because files are shared with RMC-BestFit. Register as an additional "Open with" handler instead of taking over the default when RMC-BestFit is installed.
- **Credit and disclaimer** in the About box and README: "Based on RMC-BestFit by the USACE Risk Management Center. Not affiliated with or endorsed by USACE." The 0BSD licence doesn't require attribution; this is about clarity.
- **Licence: MIT** (chosen), plus `THIRD_PARTY_NOTICES.md`. It lists the 0BSD upstream projects and the bundled MIT/OFL components (Avalonia, Dock, ProDataGrid, OxyPlot, AvaloniaEdit, Velopack, Selawik, Cascadia Mono, Caladea, Carlito) as they are adopted, and the BSD-3-Clause HTML Renderer core inside Avalonia.HtmlRenderer (free and open source, as D5 requires).

---

## 2. What the analysis found

### 2.1 Projects and their portability

| Project | Target | Status | Notes |
|---|---|---|---|
| `RMC.BestFit` (model lib) | `net10.0` | ✅ portable | 123 files / 71K lines; published on NuGet (`RMC.BestFit`) |
| `RMC.BestFit.Api` (REST + MCP) | `net10.0` | ✅ portable | Out of scope; just add Linux/macOS CI |
| `RMC.BestFit.Tests`, `.Api.Tests`, `.Verification` | `net10.0` | ✅ portable | Run them on all OSes (reproducibility check) |
| `RMC.Numerics` (NuGet 2.2.0) | `net10.0;net9.0;net8.0;net481` | ✅ portable | No Windows APIs; online-connectivity probe is plain HTTP |
| `RMC.BestFit.UI` (project/serialization layer) | `net10.0-windows` + WPF | 🟡 light | WPF is used only for icons (`ImageSource`), `Colors`, and **OxyPlot.Wpf `Plot` objects + `PlotSerializer`** (15 files); 1 XAML icon dictionary |
| `RMC.BestFit.App` (desktop shell) | `net10.0-windows` WinExe | 🔴 full port | 109 C# files / 39.9K lines + 46 XAML / 8.1K lines; heavy code-behind |
| `RMC.BestFit.UI.Tests` / `.App.Tests` | `net10.0-windows` (MSTest) | 🔴 port | 711 and 465 tests; 608 are `[STATestMethod]` |
| `tools/PlotReferenceExporter` | `net10.0-windows` | 🔴 port | Exports reference plot and panel images; seed for the parity harness |
| WPF-Framework: `DAG`, `DatabaseManager`, `ExpressionParser`, `OxyPlot` (forked core) | `net10.0` | ✅ portable | Compile from source; validate on macOS/Linux (CI is Windows-only today) |
| WPF-Framework: everything else | `net10.0-windows` + WPF | 🔴 port / replace | See §2.2 |

All three upstream repos run CI **on Windows only**, so "portable" code has never been exercised on macOS or Linux.

### 2.2 WPF-bound code that must be ported or replaced

| Component | C# lines | XAML lines | Plan |
|---|---:|---:|---|
| `FrameworkInterfaces` | 7,210 | – | Small change: drop `System.Windows.Media` (`ImageSource`, `Color`) |
| `Themes` (Light/Dark/Blue engine) | 1,812 | 5,898 | Port palettes 1:1; rewrite 144 templates as `ControlTheme`s |
| `FrameworkUI` (shell) | 12,873 | 4,238 | Port; `MainWindow.xaml.cs` alone is 2,891 lines |
| `GenericControls` | 19,622 | 4,628 | Port the ~32 types BestFit uses (+ their dependencies) |
| `NumericControls` | 10,902 | 2,874 | Port the ~12 types BestFit uses (+ dependencies) |
| `OxyPlotControls` | 11,907 | 5,365 | Port; `OxyPlotToolbar.xaml.cs` is 4,798 lines |
| `DatabaseControls` + `ExpressionParserControls` | 10,352 | 2,050 | Port `TableViewer` and the dialogs it opens |
| `SoftwareUpdate` + `.Updater` | 4,133 | – | **Replace** with Velopack (D7) |
| `AvalonDock` + VS2013 theme (vendored) | 34,844 | 5,547 | **Replace** with Dock.Avalonia + custom VS2013 theme |
| `OxyPlot.Wpf` + `.Wpf.Shared` (vendored fork) | 25,774 | 78 | **Port**, keeping the fork's classes and the 3,613-line `Serialization`; oxyplot-avalonia is the reference for the Avalonia parts (Phase 4) |
| `RMC.BestFit.UI` / `RMC.BestFit.App` | 30,510 / 39,913 | 380 / 8,124 | Light-touch / full port |

**WPF idioms that need real translation** (counts across the code to port):

| Idiom | Count |
|---|---:|
| `DynamicResource` | ~2,270 |
| `UpdateSourceTrigger=PropertyChanged` | ~870 |
| DataGrid references | ~840 |
| `DependencyProperty` references | ~670 |
| Style/template trigger constructs | ~560 |
| `ControlTemplate`s | ~260 |
| `DataTrigger` / `MultiDataTrigger` | ~250 |
| `Visibility.Hidden` | 177 |
| `MessageBox.Show` | 105 |
| `VisualTreeHelper` | ~60 |
| File dialogs | 47 |
| `Window.ShowDialog` | 35 |
| `RoutedCommand` / `CommandBinding` | ~25 |
| `ValidationRule` | 18 |
| `Clipboard` | 15 |
| `Storyboard` / `EventTrigger` | ~20 |
| `FolderBrowser` (COM) | 9 |

### 2.3 Windows-only code (complete list for the BestFit path)

| Where | What | Cross-platform replacement |
|---|---|---|
| `FrameworkUI/.../Node.cs` | `user32!GetDoubleClickTime` | `PointerPressedEventArgs.ClickCount` / platform settings |
| `FrameworkUI/Recent Files/RecentFiles.cs` | `shell32!SHGetFileInfo` (file-type icon) | App's own `.bestfit` icon |
| `GenericControls/.../FolderBrowser.cs` | `IShellItem` COM folder dialog | `StorageProvider.OpenFolderPickerAsync` |
| `GenericControls/.../RecentFileList.cs` | MRU stored in **Registry** | JSON in per-user app-data dir; one-time import from the Registry on Windows |
| `Microsoft.Win32.Open/SaveFileDialog` (47 sites) | Win32 dialogs | `StorageProvider` pickers (WPF filter-string adapter) |
| `DatabaseControls/.../CalculatorHelpWindow` | `WebBrowser` (IE) | **`Avalonia.HtmlRenderer`** (chosen 2026-10-30: managed, the same on every OS, testable headlessly; the help pages are plain HTML 4) |
| `ExpressionParserControls/ExpressionControl` | `RichTextBox` + `FlowDocument` (syntax colouring) | **AvaloniaEdit** |
| App reports (`MCMCReport`, `GMMReport`, USGS raw text), Terms & Conditions | Read-only `RichTextBox` | `SelectableTextBlock` / read-only `TextBox` (monospace) |
| AvalonDock (`HwndHost`, `HwndSource`, `WindowInteropHelper`) | Win32 windowing | Replaced by Dock.Avalonia |
| `OxyPlot.Wpf/XpsExporter` | XPS printing | **Not used by BestFit**; drop (PDF export remains) |
| `RMC.BestFit.UI/.../DssTimeSeriesReader.cs` + `Hec.Dss` | P/Invoke to native `hecdss` | Same P/Invoke; ship `hecdss` natives for every RID (§9.2) |
| `ApplicationSettingsBase` (`Properties.Settings`) | `user.config` | JSON settings; optional import of existing values |
| Updater (`SoftwareUpdate.Updater.exe`, `.exe` assumptions) | Windows file-swap updater | Velopack (D7) |

### 2.4 Critical constraints discovered

1. **`.bestfit` files are SQLite databases.** They are opened via `DatabaseManager` → `System.Data.SQLite` 2.0.2 plus `SourceGear.sqlite3`. In 2.0, everything is a P/Invoke to `e_sqlite3`, with natives for win, osx and linux (x64 and arm64), so it should work everywhere. *Validate in Phase 0.* Database passwords can't be supported cross-platform (the legacy codec was removed upstream in 2020). BestFit never calls the password API.
2. **Plot customizations are persisted as XML inside project files.** `PlotSerializer.ToXElement(OxyPlot.Wpf.Plot)` writes them into SQLite columns, and there is a legacy-format fallback. The Avalonia port must read and write **exactly** the same XML.
3. **The OxyPlot fork's core has custom additions:** `NormalProbabilityAxis` and `GumbelProbabilityAxis` (the exceedance-probability axes), plus modified series and `DataPoint` types. Stock `OxyPlot.Avalonia` (which needs `OxyPlot.Core` ≥ 2.2.0 from NuGet) can't be used as-is. Its source must be compiled against the fork. *The port ports the fork's own WPF layer instead (Phase 4).*
4. **DataGrid semantics:**

   | Feature | Uses |
   |---|---:|
   | `SelectionUnit="Cell"` | 53 |
   | `SelectionUnit="CellOrRowHeader"` | 15 |
   | `SelectedCells` | 116 |
   | `CanUserAddRows` | 55 |
   | `DataGridTemplateColumn` | 97 |
   | `CellStyle` | 487 |

   The official Avalonia DataGrid supports **none** of the first three (row selection only, no add-row, no paste) and is **deprecated**.
5. **Synchronous modal UX everywhere.** WPF's `ShowDialog`/`MessageBox` run a nested dispatcher loop. Avalonia's dialogs are `async`. See §5.3.
6. **Hidden WPF behaviours the port must reproduce:**
   - WPF bindings format with **en-US** unless `Language` is overridden, and it isn't here. That's why the screenshot shows `4/3/2026 6:31:11 AM`. Avalonia uses the OS culture.
   - `Visibility.Hidden` (keeps layout space) has no Avalonia equivalent.
   - `ValidationRule` doesn't exist in Avalonia.
   - The default `UpdateSourceTrigger` differs for `TextBox.Text` (only 6 bindings are affected).
7. **HEC-DSS** (DSS time-series import) only resolves today through a sibling checkout and a Windows-only pre-build step.
   - HEC publishes win/linux x64 natives for every version, but macOS only for `7-JA-7` (single-arch, needs macOS 14+, unbundled `libz` rpath).
   - There's **no linux-arm64** build and **no NuGet package**.
   - The C library is MIT and builds with CMake (C only), so it can be built in CI.

---

## 3. Approaches considered

| Option | Effort | Pros | Cons |
|---|---|---|---|
| **A. Native Avalonia port** *(recommended; what you asked for)* | High | Fully open source (matches 0BSD); idiomatic; long-term maintainable; free dependency stack | Largest effort; needs disciplined parity testing |
| B. Avalonia XPF (commercial WPF-compatible runtime) | Low–Med | "Change one SDK reference"; keeps XAML; Avalonia's own blog says XPF is often the better start for apps heavy in DependencyProperty, DataTrigger and code-behind, which this app is | Per-app licence (Internal €9.5K, Business €29.5K, Enterprise from €124.5K); closed runtime, so outside contributors can't build it freely; `HwndHost` (used by AvalonDock's auto-hide) "unlikely" to be supported; no OSS/government programme found |
| C. Uno / MAUI / web rewrite | Very high | — | Uno uses WinUI XAML (not WPF), MAUI has no Linux desktop, a web app discards the UI; none preserves the look cheaply |

---

## 4. Target architecture

### 4.1 Repository layout

```
crossplatformrmcbestfit/
├─ upstream/                 # vendored upstream code, byte-identical to the pins, NEVER edited
│  ├─ README.md              # cites each original repo, commit, version and licence
│  ├─ RMC-BestFit/           # 2.0.1 @ bfce2dd    (model lib + reference WPF sources)
│  ├─ WPF-Framework/         # 1.0.5 @ 7f897e1    (portable libs + reference WPF sources)
│  ├─ Numerics/              # v2.2.0 @ 7e8e8d1   (RMC.Numerics, built from source)
│  ├─ hec-dss/               # 7-JA-7 @ 1da5976   (HEC-DSS C library, built per RID with CMake)
│  ├─ hec-dss-dotnet/        # 2.0.3 @ f8f576f    (Hec.Dss .NET wrapper and its tests)
│  └─ zlib/                  # v1.3.1 @ 51b7f2a   (the version hec-dss 7-JA-7 builds with)
├─ upstream.lock             # pinned upstream commits, folders and exclusions
├─ scripts/vendor-upstream.sh   # re-vendors a pin and checks every file is byte-identical
├─ sample-data/              # downloaded test data (git-ignored): BestFit examples, HEC DSS files
├─ sample-data.lock · scripts/fetch-sample-data.sh
├─ src/
│  ├─ Framework/             # Avalonia port of the WPF-Framework subset (same namespaces)
│  │  ├─ WpfCompat/          # thin shims: Visibility, dialogs, clipboard, dispatcher, commands, culture
│  │  ├─ FrameworkInterfaces/  Themes/  GenericControls/  NumericControls/
│  │  ├─ OxyPlot/            # the fork's OxyPlot.Wpf and OxyPlot.Wpf.Shared, ported (Phase 4)
│  │  ├─ OxyPlotControls/  DatabaseControls/  ExpressionParserControls/
│  │  ├─ FrameworkUI/        # shell on Dock.Avalonia
│  │  └─ SoftwareUpdate/
│  └─ BestFit/
│     ├─ RMC.BestFit.UI/     # project layer (the ported OxyPlot.Wpf, icon keys)
│     └─ RMC.BestFit.App/    # the desktop app
├─ tests/                    # ported UI/App tests on Avalonia.Headless + compat/parity suites
├─ tools/                    # ParityCapture.Wpf (Windows), ParityCapture.Avalonia, parity report
├─ native/                   # HEC-DSS build scripts → runtimes/<rid>/native
├─ packaging/                # Velopack packaging notes, per-OS templates (scripts/package.sh)
├─ docs/                     # this plan, porting rules, ADRs, Avalonia docs snapshot
└─ Directory.Build.props · Directory.Packages.props · global.json
```

**Standalone (D11): all code needed to build is in this repository.** The upstream code is vendored, not fetched or linked as submodules:
- `scripts/vendor-upstream.sh` copies only the needed folders of each pinned commit: about 4,300 files and 80 MB.
- It checks that every staged file is byte-identical to the upstream blob. `upstream/.gitattributes` and per-pin exclusions keep git from rewriting line endings.
- Submodules would have downloaded RMC-BestFit's whole history, over 1 GB.
- Sample data isn't code and isn't vendored: BestFit's 455 MB of example projects, and HEC's sample DSS files, which have no licence. `scripts/fetch-sample-data.sh` downloads it into `sample-data/` for the test suites.
- Third-party NuGet packages (SQLite, Avalonia, …) are restored at pinned versions.
- Empty `Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` and `.editorconfig` files in `upstream/` keep XGoodFit's own settings away from upstream code.

The portable upstream projects compile **directly from `upstream/` via `ProjectReference`**, never copied. They are the OxyPlot core fork, `DatabaseManager`, `ExpressionParser` and `RMC.BestFit`. HEC's `Hec.Dss` wrapper sources also compile unmodified, but through an XGoodFit project, because the wrapper's own project file downloads Windows-only binaries. The native `hecdss` library is built from `upstream/hec-dss` with CMake (§9.2). Ported files keep the upstream relative path, so `diff upstream/<file>.xaml src/<file>.axaml` is always meaningful. Product branding comes from a few properties in `Directory.Build.props`: name, executable, bundle ID, Velopack pack ID and settings folder. The final name can be dropped in at any time (§1.1).

### 4.2 Dependency stack (all MIT/OFL/zlib/public domain; versions as of 2026-10-29)

| Need | Package | Version | Notes |
|---|---|---|---|
| UI framework | `Avalonia`, `Avalonia.Desktop`, `Avalonia.Themes.Simple` | 12.1.4 | Base templates for anything we don't restyle |
| Docking | `Dock.Avalonia.AvalonDock` (wieslawsoltes) | 12.1.0.13 | AvalonDock's API (namespace `AvalonDock`) and layout XML over Dock's controls: tabs, tool panes, auto-hide, floating, drag-and-drop docking |
| Data grid | `ProDataGrid` (wieslawsoltes) | 12.1.0.x | Cell, `CellOrRowHeader` … selection; `SelectedCells`; `CanUserAddRows`; TSV copy **and paste** |
| Charts | The fork's `OxyPlot.Wpf`, ported (`src/Framework/OxyPlot`) | fork | Declarative `Plot` with Avalonia-element series and axes, on the fork's core; oxyplot-avalonia is the reference |
| Code editor | `Avalonia.AvaloniaEdit` | 12.0.0 | Expression editor with syntax colouring |
| HTML help | `Avalonia.HtmlRenderer` | 12.0.0 | MIT, with the BSD-3-Clause HTML Renderer core; no native web view to ship (WebView2, WebKitGTK, WKWebView) |
| Behaviours (DataTriggers) | `Xaml.Behaviors.Avalonia` | *verify v12* | `DataTriggerBehavior` for the hardest DataTriggers |
| Tests | `Avalonia.Headless` (+ `Verify.Avalonia` optional) | 12.1.4 | MSTest via a manual `HeadlessUnitTestSession` |
| SQLite | `System.Data.SQLite` + `SourceGear.sqlite3` | 2.0.x / 3.50.x | Same as upstream; fallback `Microsoft.Data.Sqlite` |
| DSS import | HEC `hec-dss` (C) + `Hec.Dss` wrapper (C#), from pinned source; zlib | 7-JA-7 / 2.0.3; 1.3.1 | Natives built per RID in CI (§9.2). The wrapper has no licence file and is treated as MIT for now (R17) |
| Fonts | Selawik · Cascadia Mono · Caladea · Carlito (all OFL) | 1.01 · 2407.24 · Google Fonts `6157dfa` | Substitutes for Segoe UI, Consolas, Cambria and Calibri off Windows (`Themes.ThemeFonts`) |
| Packaging/updates | `Velopack` (D7) | 1.2.x | Setup/MSI, `.app`/`.pkg`, AppImage; GitHub Releases; deltas; signing and notarization |

Rejected or avoided:
- Avalonia **Pro** TreeDataGrid / Rich Text Editor (€899/seat/yr; needs a licence key, so outside contributors couldn't build).
- **XPF**, see §3.
- The official **deprecated** `Avalonia.Controls.DataGrid`.

### 4.3 Component mapping

| WPF (today) | Avalonia (port) |
|---|---|
| `MetroWindow` / `MetroDialogWindow` (custom chrome) | `Window` subclass. Windows/macOS: extended client area + drawn caption buttons. Linux X11: native decorations (X11 ignores client-area extension) |
| AvalonDock + VS2013 theme, `XmlLayoutSerializer` | `Dock.Avalonia.AvalonDock` with the same layout classes and `XmlLayoutSerializer`, plus a VS2013 Light/Dark/Blue dock theme. Projects store AvalonDock layout XML (`AvalonDockLayout`), so it must stay readable both ways: a sample project's layout loads and saves byte for byte |
| `Themes` (swap colour dictionaries + `DynamicResource`) | Same keys in 3 palettes; `ThemeVariant` Light/Dark + custom `new ThemeVariant("Blue", ThemeVariant.Light)`; `ThemeService.SetTheme` API preserved |
| WPF `ControlTemplate` + `Trigger`s | `ControlTheme` + nested `^:pointerover` / `:pressed` / `:checked` / `:disabled` selectors |
| `DataTrigger` / `MultiDataTrigger` | Bound style classes (`Classes.x="{Binding}"`), converters, or `DataTriggerBehavior` |
| `Storyboard` / `EventTrigger` | `Transitions` / style `Animations` |
| `CopyPasteDataGrid` / `ValidationDataGrid` (WPF DataGrid) | Same classes re-based on ProDataGrid |
| OxyPlot.Wpf `Plot` + `PlotSerializer` | The same classes, ported, in the `OxyPlot.Wpf` namespace (byte-identical XML) |
| OxyPlot.Wpf zoom adorner, `DragAdorner` / `InsertionAdorner` | The ported zoom rectangle; `AdornerLayer` |
| `RoutedCommand` / `ApplicationCommands` / `CommandBinding` | WpfCompat's routed commands, which route as WPF's do (Cmd on macOS) |
| `GenericControls.MessageBox`, `ShowDialog` | Same classes and themed `Window`s; they wait, as in WPF (§5.3) |
| `RichTextBox` (editable expression) | AvaloniaEdit |
| `RichTextBox` (read-only reports) | `SelectableTextBlock` with inlines / read-only monospace `TextBox` |
| `WebBrowser` | `NativeWebView` or `HtmlPanel` (or open in system browser) |
| `ToolBar`, `StatusBar`, `GroupBox` | Small templated controls with ported look (no direct equivalents) |
| `ListView` (no `GridView`) | `ListBox` |
| `Visibility` (incl. `Hidden`) | `IsVisible`; `Hidden` via a compat attached property (Opacity 0 + no hit-testing) |
| `pack://application:,,,/…` | `avares://Assembly/…` |
| `Dispatcher.Invoke` / `BeginInvoke` | `Dispatcher.UIThread.Invoke` / `Post` |
| `VisualTreeHelper` | `GetVisualParent` / `GetVisualChildren` / `FindAncestorOfType<T>` (compat facade) |
| Registry MRU, `user.config` | JSON in `AppPaths` (per-OS conventions) |
| `ImageSource` in `IElement` | Icon resource key (or Avalonia `IImage`), resolved in the view layer |

### 4.4 The `WpfCompat` shim layer

Keep the 40K-line App code-behind close to upstream by centralising API differences in one small, well-tested assembly:
- `Visibility` enum + attached property/extension.
- `MessageBox.Show(...)` facade.
- WPF-style `OpenFileDialog`/`SaveFileDialog` classes (`Filter` string parser → `FilePickerFileType`).
- Folder picker.
- `Clipboard` facade (text/TSV and PNG).
- `Dispatcher` helpers and a `VisualTreeHelper` facade.
- `ApplicationCommands`-style static commands with per-OS gestures.
- Binding-culture helper.
- `AppPaths`.

Shims are for **high-frequency, semantically equivalent** APIs only. Everything else is ported idiomatically.

---

## 5. Porting rules (applied file by file)

### 5.1 Per-file recipe
1. `.xaml` → `.axaml`; root `xmlns` → `https://github.com/avaloniaui`; keep `x:Class`, element names and event-handler names.
2. `System.Windows.*` → `Avalonia.*` usings; `DependencyProperty` → `StyledProperty` (styleable) or `DirectProperty` (field-backed); property-changed callbacks → `OnPropertyChanged` override; coercion → the `coerce:` parameter.
3. Triggers → pseudo-class selectors. DataTriggers → bound classes, converters or behaviours, in that order of preference. Never reshape the data model to avoid a trigger.
4. Events: `Mouse*` → `Pointer*`; `Preview*` → `AddHandler(..., RoutingStrategies.Tunnel)`; double-click via `ClickCount`.
5. Resources: theme keys stay identical (`{DynamicResource Button.Static.Background}`); `pack://` → `avares://`.
6. Bindings:
   - Start with reflection bindings (`<AvaloniaUseCompiledBindingsByDefault>false`); adopt `x:DataType` later.
   - Keep every explicit `UpdateSourceTrigger`, and add `LostFocus` where WPF's `TextBox` default applied.
   - `RelativeSource FindAncestor` works (or `$parent[T]`).
7. Dialogs, clipboard and pickers go through `WpfCompat` (see §5.3).
8. DataGrid → ProDataGrid, through `CopyPasteDataGrid`: `CellStyle` / `RowStyle` / `ColumnHeaderStyle` → `CellTheme` / `RowTheme` / `ColumnHeaderTheme`; a column's `ElementStyle` / `EditingElementStyle` → `DataGridColumnStyles`; a header's `ContentTemplate` → `ColumnHeaderContent.Template`; `Selector.IsSelectionActive` → `WpfSelector.IsSelectionActive`; `ContainerFromIndex` → `GetRow`; alternating rows → `:nth-child(2n)` (porting guide §3–4).
9. Done = it builds without warnings, its ported tests pass, its screenshot parity is within tolerance in all 3 themes, and a behaviour checklist has been run against the WPF app.

### 5.2 Behavioural gotchas (must not silently change logic)

| WPF behaviour | Avalonia default | Port rule |
|---|---|---|
| Bindings format with **en-US** (no `Language` override in this code) | OS culture | Route all binding/`StringFormat` formatting through one `FormattingCulture` setting, defaulting to en-US for parity (D8; mechanism verified in Phase 0). Code-behind keeps `CurrentCulture` as today. Switching to the OS locale later is then a setting plus a test pass (§13). **Charts are different:** OxyPlot axis labels already use the OS culture in the WPF app (verified on an en-AU machine), so plots keep `CurrentCulture` |
| `TextBox.Text` default `UpdateSourceTrigger=LostFocus` | Immediate | Add explicit `LostFocus` to the 6 affected bindings and to custom text controls that relied on it |
| `Visibility.Hidden` reserves space (177 uses) | No equivalent | Compat `Hidden` = `Opacity=0` + `IsHitTestVisible=false` |
| `ValidationRule` (18), `ValidatesOnDataErrors` (2) | Not supported | Converter or exception-based data validation, same messages |
| Synchronous `ShowDialog` / `MessageBox` (nested loop) | `async` | §5.3 |
| `Loaded`, focus scopes, keyboard navigation, mouse-wheel routing | Subtly different | Covered by ported tests + UAT scripts |
| DataGrid edit-commit timing (`CommitEdit` 6×, `BeginEdit` 2×) | ProDataGrid semantics | Explicit tests per grid |

### 5.3 Dialog strategy (decided by Phase 0 Spike D; revised 2026-10-30)
- **Default: synchronous, as in WPF.** WpfCompat gives the ported code WPF's blocking calls:
  - `ShowDialog()` returning `DialogResult`, `MessageBox.Show`, the file and folder dialogs, and the clipboard.
  - They wait in a nested dispatcher frame: Avalonia 12's public `Dispatcher.PushFrame`, the mechanism WPF itself uses.
  - The volume decided it: 604 `Visibility` assignments, 103 `MessageBox.Show`, 33 `ShowDialog()`, 40 file dialogs and 18 clipboard calls. Upstream's control flow (property setters, deep call chains, `Closing` handlers) stays as it is.
  - Nested frames work on Win32, macOS, X11 and Wayland. One X11 repaint issue is open (#22195), and the Linux test pass must cover it.
- **Async where it's natural:** new code, and sites where a nested loop causes trouble, can await Avalonia's APIs directly.
- **Window `Closing`:** cancel, then show the "save changes?" prompt, then call `Close()` again if needed.

---

## 6. Visual fidelity strategy

For 1.0, every OS gets the same VS2013 look, including the in-window menu bar (D4); macOS-native conventions come after 1.0 (§13).

1. **Palettes 1:1.** Port `LightColors`/`DarkColors`/`BlueColors.xaml` (~460 resources each) and `ColorKeys.cs` verbatim. Only the XML namespace changes.
2. **Control templates.** Port each of the ~31 theme XAML files (Button … DataGrid, Calendar, Menu, TabControl, TreeView, ScrollBar, Expander, MetroWindowStyle) to `ControlTheme`s that reproduce the WPF visual tree: borders, padding, glyph paths, sizes. Then port the FrameworkUI-, OxyPlotControls- and App-level styles (`BestFitStyles.xaml`, `IconDictionary.xaml`).
3. **Docking chrome.** Build a VS2013-style Dock theme (pane headers with the dotted grip, pin/close glyphs, document tab strip and overflow) in all 3 variants. *Done (2026-10-30): `FrameworkUI/Themes/VS2013/Dock`, XGoodFit's own theme from upstream's colours and key names; against upstream's screenshots, pane edges, captions and tabs match within a pixel and colours within JPEG noise.*
4. **Typography.** WPF defaults are Segoe UI at 12 DIP. Use real Segoe UI/Consolas on Windows. Elsewhere, use embedded **Selawik** (Segoe UI metric fallback; check its shorter line height) and **Cascadia Mono**. Turn on subpixel antialiasing on Windows to match ClearType. *Done (2026-10-30):* `Themes.ThemeFonts.UseThemeFonts()` embeds Selawik, Cascadia Mono, and Caladea and Carlito (metric-compatible with Cambria and Calibri, which upstream names for its f(x) icon glyphs and one expander header). Outside Windows it maps each Windows font to its substitute and makes Selawik the default family; every application builder calls it (the App too). Before it, a named Windows font fell back to the platform's default, which differs per OS and is missing on GitHub's Linux runner.
5. **Geometry.** Both frameworks use 96-DIP units, so sizes, margins and vector icons (~1,000 `Path` geometries) carry over unchanged.
6. **Window chrome.**
   - Windows: custom caption matching `MetroWindow`.
   - macOS: extended client area with native traffic lights.
   - Linux: native decorations (the only intentional visual difference).
7. **Plots.** Use the same OxyPlot core, so layout maths are identical. Differences come only from text measurement and the renderer; tune them via the parity harness.
8. **Parity harness** (built in Phase 0, used in every phase):
   - `ParityCapture.Wpf` runs on Windows and extends the existing `PlotReferenceExporter`/`PanelExporter`. It opens each example `.bestfit`, renders each document view, properties panel and dialog at fixed sizes in Light, Dark and Blue, and saves PNGs.
   - `ParityCapture.Avalonia` captures the same views headlessly (`UseSkia`, `CaptureRenderedFrame`).
   - A report pairs the images side by side with a perceptual diff score. Baselines are versioned, and the 285 images under `examples/**/screenshots` are extra references.

---

## 7. Phased roadmap

Sizes are relative, for one developer with AI-assisted porting: S ≈ ≤2 wk, M ≈ 2–4 wk, L ≈ 4–8 wk, XL ≈ 8+ wk. The rough order of magnitude is **6–12 months to a 1.0**, to be re-estimated at the end of Phase 0. Phases 2–5 can run in parallel streams once Phase 1 lands.

### Phase 0: Groundwork & de-risking spikes (M)

**Groundwork**
- Repo scaffold (§4.1), central package versions, `global.json` (.NET 10).
- CI matrix (GitHub-hosted runners):
  - `windows-latest` (x64)
  - `macos-latest` (arm64)
  - an Intel macOS image while GitHub still offers one; otherwise test x64 under Rosetta 2
  - `ubuntu-latest` (x64)
- **Dev and test machines (D9):**
  - **Windows x64:** this laptop (.NET 10 SDK installed).
  - **macOS:** the M4 Pro MacBook Pro over Tailscale, for arm64 and (via Rosetta 2) x64. Turn on SSH (macOS "Remote Login" or Tailscale SSH) with key-based login, so builds and tests can be driven from Windows.
  - **Linux x64:** a Linux machine on the tailnet, WSL2 + WSLg on this laptop (needs an Ubuntu distro), or a Hyper-V Ubuntu desktop VM.
  - Don't register personal machines as self-hosted runners for a public repo: pull requests from forks could run code on them.
- Snapshot the 8 Avalonia WPF-migration pages (`…/migration/wpf/*.md`) into `docs/reference/avalonia/` for offline AI context. Optionally configure Avalonia's free **Build MCP** (`https://docs-mcp.avaloniaui.net/mcp`, which provides `lookup_wpf_to_avalonia_mapping` and `analyze_wpf_project`).
- **Portable-layer validation** on all OSes and architectures:
  - Run `RMC.BestFit.Tests`, `DatabaseManager.Tests`, `ExpressionParser.Tests` and `OxyPlot.Tests`.
  - Open all 32 example `.bestfit` files with `System.Data.SQLite` 2.0.x.
  - Record any floating-point differences versus Windows.
- **HEC-DSS:**
  - Build `hecdss` 7-JA-7 from source for win-x64, linux-x64, osx-arm64 and osx-x64 (CMake, C only). Point CMake at the pinned zlib and test data instead of letting it download them. DSS import is required on every platform at 1.0 (D6), so this is on the critical path.
  - Fix the macOS `libz` linkage.
  - Build `Hec.Dss` for `net10.0` and pack it as a local NuGet with `runtimes/<rid>/native`.
  - On every OS, run HEC's C and .NET tests, and read BestFit's example DSS file (`examples/1-time-series-data/5-hec-dss-import/dss-example-data.dss`).
  - **Done (2026-10-30):** `native/build-hecdss.sh` builds the natives for all four RIDs, and `src/Hec.Dss` is a solution project (it packs too) shipping them as `runtimes/<rid>/native` assets. HEC's C tests and `tests/Hec.Dss.Tests` (126 tests) pass on Windows x64, Linux x64, macOS arm64 and macOS x64 under Rosetta. BestFit's example file reads bit for bit as BestFit 2 saved it, and on Windows it reads identically with BestFit's own 7-IU-6 wrapper and native (findings below). CI builds the natives and runs both suites on each OS.

**Spikes** (each ends in an ADR, go/no-go)

| Spike | Topic | What to prove |
|---|---|---|
| A | Grid | `CopyPasteDataGrid` on ProDataGrid: rectangular cell selection, `SelectedCells`, add-row placeholder, TSV copy/paste with Excel, validation styling, keyboard navigation. |
| B | Docking | The FrameworkUI layout on Dock.Avalonia (explorer, documents, properties, messages), with auto-hide, float, close and persistence, plus one VS2013-Blue pane header and tab. |
| C | Plotting | The fork's plotting layer on Avalonia (planned as vendored OxyPlot.Avalonia; the fork's WPF layer was ported instead, Phase 4), with the probability axes. Render a univariate "Frequency" plot from an example project and diff it against the WPF capture. |
| D | Dialogs | Async vs nested-loop modal dialogs and pickers on Windows, macOS, Linux X11 and XWayland. |
| E | Theming | Runtime Light/Dark/Blue switching with ported palettes; Button, ComboBox, CheckBox and DataGrid header templates diffed against WPF. |
| F | Fonts and culture | Selawik/Cascadia fallback metrics; en-US binding culture. |

**Exit:** a parity harness v0 producing a report, all spikes decided, and an updated estimate.

**Progress (2026-10-30)**

| Item | Status |
|---|---|
| Repo scaffold: `upstream.lock` + `scripts/fetch-upstream.sh`, central props, `global.json`, `XGoodFit.slnx`, CI workflow, `scripts/run-upstream-tests.sh` | ✅ Pushed to `main` (`7f2bde4`); fetching was later replaced by vendoring (D11) |
| Portable layer on Windows x64 (en-AU), Linux x64 (Ubuntu 26.04 GNOME, en-US) and macOS arm64 (M4 Pro, macOS 26.6), plus GitHub-hosted CI on all three | ✅ Same results everywhere. RMC.BestFit model 3,546/3,546 · DatabaseManager 301/301 · ExpressionParser 240/240 · OxyPlot core 3,276/3,276 runnable on en-US Windows (Windows only, see below) |
| All 32 example `.bestfit` projects through the upstream SQLite stack (System.Data.SQLite 2.0.2, SQLite 3.50.4) | ✅ They open on all three OSes, and **all 146 tables read identically** on Windows x64, Linux x64 and macOS arm64 (`tools/ProjectProbe` fingerprints) |
| Standalone repository (D11): upstream code vendored in `upstream/`, byte-identical to the pins; clone and `dotnet build` with no other downloads besides NuGet | ✅ RMC-BestFit, WPF-Framework, Numerics, hec-dss, hec-dss-dotnet and zlib vendored (2026-10-30). RMC.Numerics now builds from source, and RMC.BestFit 3,546/3,546 pass against it. Numerics' own suite: 2,864/2,864 on x64, excluding live-service tests as upstream does; on arm64, 3 bit-for-bit tests are skipped (findings below) |
| HEC-DSS (D6) | ✅ Natives built from source for win-x64, linux-x64, osx-arm64 and osx-x64 (zlib static, no downloads); `src/Hec.Dss` with a ported `DssNative.cs`; HEC's C tests and the wrapper's tests pass on Windows, Linux and macOS (x64 under Rosetta); BestFit's example DSS file reads bit for bit as BestFit 2 saved it, and as BestFit's own 7-IU-6 build reads it (2026-10-30) |
| CI (GitHub-hosted Windows, macOS arm64, Linux) | ✅ Green, including a job that fails if any OS reads the example projects differently |
| Spikes A–F, parity harness | Not started |

**Findings so far**

- **Chart axes follow the OS culture in the WPF app.** On an en-AU machine OxyPlot renders `1/01/2011`, while bound fields use en-US. The port keeps that split (§5.2).
- **The same locale formats dates differently by OS.**
  - .NET on Windows uses Windows' regional data: en-AU is `d/MM/yyyy`, so `2/01/2011`.
  - On macOS and Linux it uses ICU data: en-AU is `d/M/yyyy`, so `2/1/2011`.
  - .NET on macOS does pick up the system locale even when launched without `LANG`.
  - So Windows output matches WPF exactly, and macOS/Linux follow their native conventions. Cross-OS screenshot comparisons must pin one culture.
- **On arm64, RMC.Numerics' bit-for-bit tests differ from the x64 references (R9).** On GitHub's macOS arm64 runner, three Numerics tests that assert exact equality fail:
  - two NUTS sampler draws differ at the 13th significant digit (`0.06320733141719788` vs `0.06320733141716985`);
  - one Weibull constraint differs at the 16th (`6.684491850934669` vs `6.684491850934662`).
  - This is ordinary cross-architecture floating-point behaviour, not a port defect. The model's 3,546 tolerance-based tests pass on arm64. `scripts/run-upstream-tests.sh` skips those three tests on arm64, and the Phase 9 reproducibility report must quantify the differences.
- **Numerics' live-service tests are excluded**, as in upstream's own CI (`TestCategory!=LiveServiceIntegration`). They download from USGS and other services, and one failed in CI when USGS returned HTTP 503.
- **One Numerics allocation test is excluded.** `OutsideSupport_AllocatesOnlyBoundsEnumeration` asserts an exact number of bytes allocated, which varies with the runtime's background JIT work; it failed once on a Linux CI runner (6,408 vs 6,400). It measures allocation behaviour, not results.
- **Upstream's OxyPlot tests can't run outside Windows.** The test assembly requires an STA thread, and elsewhere NUnit silently runs nothing.
  - `scripts/run-upstream-tests.sh` skips that suite outside Windows, and fails any suite that runs zero tests.
  - A cross-platform wrapper for those tests belongs to Spike C.
- **Four upstream model tests are excluded.**
  - `RunAsync_MultipleAnalyses_Parallel` asserts a 250 ms wall-clock limit, so it fails intermittently on both OSes. It measures speed, not results.
  - `XYOrdinatesChange_EstimatedAnalysis_PreservesMcmcResults` races with itself, so it failed once on a Windows CI runner. Setting `XYOrdinates` starts a fire-and-forget reprocess (`AnalysisBase.ReprocessIfEstimated`), while the test calls `CreateFrequencyAnalysisResultsAsync` directly; a failed background run clears `IsEstimated`. Worth reporting upstream: the same pattern could reach the app if anything else rebuilds results while a reprocess is in flight.
  - `Cancel_DuringParallelLoop_StopsAndReportsCanceled` cancels a run 50 ms after starting it, with work sized for a Debug build. In Release on a fast machine the run can finish first, so it failed once on a Windows CI runner (2026-10-30). It measures timing, not results.
  - `Documentation_LocalLinksResolve` checks links into folders that are deliberately not vendored.
- **The upstream default connection uses WAL journal mode.** It converts any non-WAL database when opened and creates `-wal`/`-shm` side files. Tools such as the probe therefore read temporary copies, and the file-compatibility suite must do the same.
- **HEC-DSS usage is read-only.** BestFit reads DSS files only: open, catalogue paths, read time series. It goes through HEC's `Hec.Dss` wrapper (2.0.3, net9.0, 31 native calls), plus one direct call, `hec_dss_tsGetDateTimeRange`, which is present in the current C library.
  - The wrapper ships only win-x64 and linux-x64 natives (HEC build 7-IU-6), and its pre-build step (PowerShell + 7-Zip download) only works on Windows.
  - **Approach:**
    1. ✅ Vendor `hec-dss`, `hec-dss-dotnet` and zlib (`upstream.lock`); pin HEC's sample data (`sample-data.lock`).
    2. ✅ Build `hecdss` from source (CMake, C only) in CI for win-x64, linux-x64, osx-arm64 and osx-x64. zlib is linked statically everywhere: HEC's own `deps/zlib` aliases its static target to zlib's shared library on Linux and macOS, which is where the unbundled `libz` rpath came from. `native/hecdss/CMakeLists.txt` replaces HEC's top level and builds zlib from a copy, because zlib's CMake renames `zconf.h` in its source folder.
    3. ✅ Compile the wrapper's own sources in an XGoodFit project that packs those natives under `runtimes/<rid>/native`, without its Windows-only pre-build, and with a ported `DssNative.cs` that matches 7-JA-7 (below).
    4. ✅ Validate with the wrapper's tests and a sample DSS file on every OS.
  - **Which C library version: release 7-JA-7, with corrected wrapper declarations.** BestFit ships HEC's prebuilt 7-IU-6, which has no source tag.
    - **No hec-dss release matches wrapper 2.0.3.** In March and April 2025 (HEC PRs #271 and #280), five of the 31 functions it declares gained trailing parameters, and in November 2025 (#341) a sixth gained one in the middle. The wrapper was never updated:
      - the reads `hec_dss_tsRetrieve` and `hec_dss_pdRetrieve` gained a time-zone output buffer and its length;
      - the writes `hec_dss_tsStoreRegular`, `hec_dss_tsStoreIregular` and `hec_dss_pdStore` gained a time zone, and the first two a storage flag;
      - `hec_dss_gridStore` gained `compressionSize` among its parameters.
    - **Pairing 2.0.3 with any later native is unsafe.** The missing arguments are leftover stack values, and the read functions write the record's time zone through that "buffer" whenever a record has one. BestFit's import uses `hec_dss_tsRetrieve`, and its tests create DSS files with the store functions.
    - **The last matching source isn't practical.** That is `ee6dff5` (February 2025), and its `hecdss` links HEC's Fortran library, so every platform would need a Fortran toolchain, and macOS builds would have to bundle the Fortran runtime.
    - **HEC's `main` can't be followed either.** After 7-JA-7 it also inserts two parameters (`cnotesBuffer`, `cnoteSize`) in the middle of `hec_dss_tsRetrieve`.
    - **So: 7-JA-7**, the newest release: CMake, C only, no Fortran. HEC's CI builds and tests it on Windows, Linux and macOS. It is paired with a ported `DssNative.cs` that declares the six functions with their current parameters, and keeps the old C# signatures as overloads that behave like 7-IU-6 (no time zone, passed as `""` because 7-JA-7 calls `strlen` on it; `REPLACE_ALL` and `DELETE_INSERT` storage as at `ee6dff5`; grids not pre-compressed), so every other wrapper file compiles unmodified. The new `hec_dss_api_version()` lets that file reject a mismatched native at load time ("0.3.0" for 7-JA-7), with the `DllNotFoundException` BestFit already handles; the same checked library answers `RMC.BestFit.UI`'s own `DllImport("hecdss")`.
    - **The other 22 declared functions match 7-JA-7 parameter for parameter** (a test checks every declaration against `hecdss.h`). Three more no longer exist: `hec_dss_convertToVersion7`, used only by `DssWriter`'s v6 conversion, and two `hec_dss_deprecated_ifltab*` helpers that nothing calls. .NET binds P/Invokes on first call, so they're harmless.
    - **DSS 6 files aren't a regression.** 7-JA-7 dropped DSS 6 support, and BestFit can't import them today either: 7-IU-6's `hec_dss_open` returns -700. 7-JA-7's fails inside `zopen` instead, which would read "Error opening DSS file", so `DssNative.cs` maps it back to -700 and BestFit's "This library only supports DSS version 7 files".
    - **Windows parity with 7-IU-6:** checked against BestFit 2's own `Hec.Dss.dll` and `hecdss.dll` (byte-identical to HEC's 7-IU-6), loaded side by side when `XGOODFIT_BESTFIT_REFERENCE` names a BestFit install. BestFit's example reads identically (every record, series and block read), and 20 of HEC's files read the same, with two expected differences: HEC's DSS-255 fix gives three series in `CantSaveEdits4.dss` the units and type 7-IU-6 left blank (the values are identical), and some records' quality flags change on every run in both libraries (BestFit doesn't read them).
    - **Found while building:** HEC's library writes to a fixed `/tmp` on Linux and macOS when squeezing files or sorting catalogues (BestFit's unsorted catalogue never does). Linux natives need the glibc they were built with or later, so release builds should come from an older distribution (Phase 8). The macOS dylibs are only ad-hoc signed, so the app's signing must cover them (Phase 8). BestFit's error message names `hecdss.dll`; the App port should word it for every OS.
    - **Upstream quirks kept:** the wrapper passes catalogue filters without a terminating null; the quality array isn't trimmed to the values read; `IsDPartARange()` is true for `TS-PATTERN`.
  - **Build-time downloads:** hec-dss's CMake build fetches zlib (tag v1.3.1) and HEC's test data (unpinned `master`) while configuring. XGoodFit's own top-level CMake file (`native/hecdss`) uses the vendored zlib and the pinned test data instead, so it needs no network access.
  - **Licensing:** the C library (`hec-dss`) is MIT, but the .NET wrapper repo (`hec-dss-dotnet`) has **no licence**: no file, no source headers, no GitHub licence metadata. HEC's sample data (`dss-test-data`) has none either, but it's only used by tests and never redistributed.
    - **Working assumption (2026-10-29): treat the wrapper as MIT, like the C library, for now.** Confirm with HEC before the first public release (R17).
    - Fallback: implement the small read-only API slice BestFit uses (`DssReader`, catalogue, `DssPath`, `TimeSeries`) directly over the MIT C library, verified against the official wrapper on the same DSS files.

### Phase 1: Foundation (M)
- `WpfCompat` shims (§4.4), with unit tests.
- `FrameworkInterfaces` with no WPF dependency (icon key or `IImage`, a portable colour type), `Messenger`, `UndoManager` (unchanged logic).
- `Themes`: palettes, `ThemeService`/`ThemeManager` API and theme persistence; embedded fonts and global WPF-equivalent defaults.

**Exit:** a theme-switching demo window with parity screenshots of the palettes.

**Progress (2026-10-30)**

| Item | Status |
|---|---|
| Build layout: ported projects in `src/` overlay their upstream project. Upstream C# compiles unchanged unless a ported copy exists at the same relative path (`Directory.Build.targets`); XAML and code-behind are always ported | ✅ |
| Avalonia 12.1.4, with compiled bindings off by default (§5.1) | ✅ |
| `Themes`: Light/Blue/Dark palettes generated from upstream (`scripts/port-palettes.sh`); `ThemeService`, `ThemeResourceHelper`, `ThemeChangedEventArgs` and the 3 converters ported; shared resources in `Merged.axaml` | ✅ Control themes follow in Phase 3 |
| `FrameworkInterfaces`: 23 files compile unchanged from upstream; 6 ported, changing only their `using` lines, plus no `Freeze()` for the Messenger's brushes. A project alias keeps WPF's `ImageSource` name for Avalonia's `IImage` | ✅ |
| `Themes.Tests` (headless, MSTest): every palette key matches upstream in all 3 themes; theme switching updates open windows; WPF's default font | ✅ 10/10 on Windows |
| WPF-equivalent defaults: every window gets Segoe UI at 12 DIP, falling back to the embedded Selawik where Segoe UI isn't installed. Selawik 1.01 is vendored from its release download (`upstream-assets.lock`, SHA-256-checked) | ✅ `ThemeService.Initialize` adds the styles (`Resources/Defaults.axaml`) |
| Theme gallery (`tools/ThemeGallery`): all 322 palette keys as live swatches next to standard controls, with a theme switcher. `ThemeGallery capture <folder>` renders it in each theme headlessly | ✅ Phase 1 exit: the demo window switches Light/Blue/Dark |
| `WpfCompat` shims | Started (`src/Framework/WpfCompat`, 12 tests):<ul><li>WPF's routed commands: `RoutedCommand`, `CommandBinding` and every element's `CommandBindings`; `ApplicationCommands` with WPF's key gestures (Cmd on macOS), and `SystemCommands`. The `{wpf:Command …}` markup extension routes a button's command from the button and a menu item's from the focused element, as WPF does.</li><li>`ResizeMode`, and `Visibility` (with Hidden) as an attached and an extension property.</li><li>Synchronous `ShowDialog()` and `DialogResult`, WPF's message-box enums, the file and folder dialogs, and the clipboard (§5.3).</li><li>WPF members Avalonia lacks: `ToolTip`, `SelectionLength`, one-argument `TryFindResource`, `Application.MainWindow`/`Windows`/`Shutdown()`, `Cursors` and `Mouse.OverrideCursor`, `SystemParameters`, and `x:Array`.</li></ul>The rest follow as Phase 2–3 code needs them |

### Phase 2: Walking-skeleton shell (`FrameworkUI`) (L)
- `MainWindow` with menus, a toolbar with undo/redo history drop-downs, key bindings, recent files, auto-backup and file compaction.
- Dock layout and theme.
- Project Explorer: tree, element icons, context menus, rename, drag-drop insertion adorner.
- Properties host and Message Window (filters, grid).
- Dialogs: Options, Save-unsaved, Open-windows, About/Terms.
- Port `FrameworkUI.Demo` as the living smoke test.

**Exit:** the demo app runs on 3 OSes × 3 themes, with shell parity within tolerance.

**Progress (2026-10-30):**
- Docking uses `Dock.Avalonia.AvalonDock`, which keeps AvalonDock's API and its layout XML. A prototype loaded the layout stored in a sample project (pixel and star pane sizes, a pane group, documents) and saved it back byte for byte, apart from the encoding declaration of the string writer used.
- `FrameworkUI` has started (opt-in overlay): its VS2013 palettes and icons, `ThemeManager`, user settings, utilities and the default dock layout. Settings live in the per-user application data folder on every OS (`WpfCompat.AppPaths`): upstream preferred a folder next to the executable, which Velopack replaces on update and which macOS bundles and AppImages make read-only.
- `SoftwareUpdate` compiles from upstream unchanged; installing through Velopack comes with packaging.
- The VS2013 dock theme (Light, Dark, Blue) covers captions, tool and document tabs, auto-hide tabs, floating windows, the navigator and the docking targets. `DockThemeSwitcher.Apply(MainDock, color)` replaces MainWindow's three AvalonDock theme dictionaries in its `ThemeChanged` (drop `_avalonDockThemeDictionary` and the dummy `Vs2013BlueTheme`); `AvalonDockTheme` stays in `Application.Styles`. Left as they are: the auto-hide flyout's caption stays inactive (the adapter doesn't activate its pane), tool panes show the document-style docking targets, and the drop preview has no accent border.
- The shell is ported: `MainWindow` (menus, the toolbar with undo and redo history, key bindings, recent files), the Project Explorer (tree, icons, context menus, rename, drag-and-drop insertion), the properties host, the Message Window (filters, grid), and the Options, Save Changes and Open Windows dialogs, with 30 tests.
- `tools/FrameworkUI.Demo` is upstream's demo, ported as the shell's smoke test. It's complete now that OxyPlotControls, NumericControls and DatabaseControls are ported: the hazard document's plot toolbar and plot properties panel, its parameter sets table and its probability ordinates editor. `FrameworkUI.Demo capture <folder>` renders the shell, its menus and dialogs in every theme; they match upstream's gallery screenshots (`frameworkui-shell-*.png`, `frameworkui-hazard-document-blue.png`).
- Also ported: auto-backup, file compaction and its progress window, and the About and Terms and Conditions windows. Upstream's read-only rich text (the terms, the GMM and MCMC reports, USGS raw text) shows in WpfCompat's `RichTextBox`, a small flow-document viewer (paragraphs, runs, line breaks, links), which Themes styles.

### Phase 3: Control library (`Themes` templates + `GenericControls`) (XL)
- Restyle every standard control; add the new `ToolBar`, `StatusBar` and `GroupBox` look-alikes.
- `CopyPasteDataGrid`, `ValidationDataGrid` and `DataGridToolbar`.
- Numeric text box, up-down and slider; `ColorPicker`; the property controls (Boolean, Text, Numeric, DateTime, Content, ResizableText, NameText); `FileSelectorControl`; `NameDialog`; `MessageBox`; `MetroWindow`/`MetroDialogWindow`; `SeparatorWithHeader`; adorners; converters.
- Port `GenericControls.Demo`.

**Exit:** each control has a demo page, screenshot parity, and behaviour tests (grid copy/paste and add-row in particular).

**Progress (2026-10-30):** 25 of the 27 `Themes` control theme files are ported: DataGrid (ports of WPF's own grid, row and cell templates, for ProDataGrid) and Button, Calendar, CheckBox, ComboBox, Expander, GridSplitter, GroupBox, Label, ListBox, ListView, Menu (with ContextMenu), MetroWindowStyle, ProgressBar, RadioButton, ScrollBar (with ScrollViewer), Separator, Slider, StackPanel, TabControl (with the three keyed TabItem styles BestFit uses), TextBlock, TextBox, ToolBar (the style keys upstream gives ordinary buttons), ToolTip and TreeView.
- The other two belong elsewhere:
  - RichTextBox: its colours and context menu go to AvaloniaEdit and WpfCompat's read-only `RichTextBox` (Phases 2 and 5).
  - StatusBar: not ported, as nothing upstream uses one and Avalonia has no StatusBar control.
- The window chrome (MetroWindowStyle and MetroDialogStyle) draws the VS2013 title bar in an extended client area:
  - Windows: the title bar has Avalonia's TitleBar role, which gives native dragging, snapping and the system menu.
  - macOS: the native traffic lights stay.
  - Linux: the native title bar stays, and the drawn one hides.
  - Caption buttons run MetroWindow's command handlers through WpfCompat's routed commands, as in WPF.
- `GenericControls` has everything BestFit uses (187 tests):
  - The data grids on ProDataGrid: `CopyPasteDataGrid` (WPF's `Items`, `GetRow`/`GetCell`, Excel copy and paste with WpfCompat's clipboard, WPF's cell keyboard navigation, generated columns that keep their themes), `ValidationDataGrid` and `DataGridToolbar`.
  - The numeric text boxes, up-down and slider, the colour picker and popup, the date and time controls, `ResizableTextBoxControl`, `SeparatorWithHeader`, every property control and `PropertyAttributes`, `InsertionAdorner` and `ComboBoxItemTemplateSelector`, with their gallery pages.
  - Not ported (D10): ThicknessControl, GridLengthControl, Point3DPropertyControl, CalendarWeekRulePropertyControl, the directory and folder selectors, DragAdorner and RecentFileList.
  - Gaps: Shift+click after keyboard navigation extends from the last clicked cell (ProDataGrid keeps its click anchor private); Tab while editing goes to the next writable cell; ProDataGrid has no `RowHeaderStyle` or `AlternatingRowBackground` (about 8 uses, covered by row themes).
- **An upstream bug to report (D8):** NumericUpDown and NumericSlider format their text in the machine's culture, but their binding parses en-US. With decimal places on a comma-decimal machine, "23,0000" reads as 230000 (BestFit's split-index slider uses 4 decimal places). The port keeps it; the OS-locale work (§13) should fix it in both.
- `GenericControls` has started. Its upstream files are nearly all WPF code, so the project opts in to upstream files (`UpstreamCompileMode=OptIn`) and gains them one port at a time. Ported so far:
  - `MetroWindow` and `MetroDialogWindow`, with headless tests for their caption buttons and ResizeMode. The theme gallery runs in a `MetroWindow`.
  - `MessageBox` (with its "don't show again" option), `NameDialog` and `NameTextBox`, tested headlessly and captured by the gallery.
  - The converters, `NumberFormatHelper`, `GeneralMethods` and `FolderBrowser` (on WpfCompat's folder dialog); the icon and resource dictionaries; the theme's control styles.
  - Not ported: `ValidationRules.cs`, which nothing BestFit uses needs.
- Behaviour gaps to close when the controls that need them are ported:
  - Slider track clicks: WPF moves the thumb by LargeChange, unless IsMoveToPointEnabled; Avalonia always moves it to the pointer. GenericControls' NumericSlider needs WPF's default, and its ColorPicker uses move-to-point.
  - ComboBox drop-downs: WPF's maximum height is a third of the screen; the theme assumes a 1080-pixel screen until WpfCompat can read the real one.
  - Plain Expanders: upstream's two (in ExpressionParserControls' calculator) use WPF's Aero2 look, which needs porting with them.
- `Button.axaml` records the conversion rules. WPF styles with templates become ControlThemes that also set the defaults WPF took from the system (Aero2) style underneath; triggers become pseudo-class selectors; `TargetName` setters become `/template/` selectors.
- Each file's header notes what had to change and why. Examples: Avalonia finds template parts by name rather than by command; it can't restyle a grid's row definitions, so TabControl has one template per tab placement; ListView, which Avalonia lacks, becomes a ListBox theme.
- WPF defaults that the ported XAML relies on and Avalonia lacks are restored in one place:
  - `WpfDefaults`: shapes draw a 1-pixel stroke.
  - `Defaults.axaml`: the default font, and the menu separator style WPF applies by key.
  - `Merged.axaml`: the WPF system colours and sizes (17-pixel scroll bars, repeat-button timing).
- The theme gallery shows each control in all three themes, including open menus, context menus and combo box drop-downs. `Themes.Tests` checks that every implicit theme loads and applies its template, and behaviour a screenshot can't show, such as the stroke defaults and the tree view's expander.

### Phase 4: Plotting (L)
- The fork's WPF layer (`OxyPlot.Wpf`, `.Wpf.Shared`) ported, with its additions: the probability axes and its modified series and annotations.
- `Serialization` port with **golden XML fixtures captured from WPF**. Deserialize then re-serialize must give byte-identical XML, including the legacy-format fallback.
- PNG/SVG/PDF export and copy-image-to-clipboard.
- `OxyPlotControls`: `OxyPlotToolbar` (pointer, pan, zoom, extents, add annotation, screenshot, export, settings), properties editors (axes, series, annotations), `SavePlotImageDialog`, `OxyPlotThemeManager`.

**Exit:** every plot type BestFit uses has parity, and the serialization fixtures are green on 3 OSes.

**Progress (2026-10-30):** the fork's WPF layer is ported, rather than vendoring OxyPlot.Avalonia (decided 2026-10-30). That keeps the fork's own classes and changes (the declarative `Plot` with dirty-tracked synchronisation, the DrawingVisual backend, the probability axes, `ExtrapolationLineSeries`, `Serialization`), with oxyplot-avalonia as the reference for the Avalonia parts.
- `src/Framework/OxyPlot/OxyPlot.Wpf.Shared` and `OxyPlot.Wpf` overlay their upstream projects in opt-in mode, and keep the `OxyPlot.Wpf` namespace and every type and member name. The fork's OxyPlot core compiles unchanged from `upstream/`.
- Dependency properties are styled properties with the same defaults and callbacks; 11 upstream files compile unchanged. A script did the regular conversion, and each file's notes say what else changed.
- The DrawingVisual backend draws into XGoodFit's `DrawingVisual`, a recording its host replays, because Avalonia's own recorded drawing can't hold bitmaps (heat maps). The Canvas backend fills an Avalonia canvas.
- Exports: PNG through Avalonia's `RenderTargetBitmap`, with the resolution chunk WPF's encoder wrote. The fork's PDF and SVG exports draw text as outlines; Avalonia lays the text out and SkiaSharp gives the glyph outlines. XPS and XAML export aren't ported (BestFit doesn't use them).
- `Serialization` writes WPF's text forms through new WpfCompat converters (`ColorConverter`, `BrushConverter`, `FontWeightConverter`, `FontFamilyConverter`, `ThicknessConverter`) and keeps WPF's three-state `Visibility` on series.
- `OxyPlot.Wpf.Tests` (26 tests) renders both backends headlessly, exports PNG, SVG and PDF, drives the plot with the mouse (zoom, pan, tracker, context menu), and checks property synchronisation. Its golden fixtures come from the WPF fork itself (`tools/OxyPlotFixtures.Wpf`, Windows only), for a frequency plot and for one of every axis, series and annotation type. The port writes the same XML byte for byte and reads it back unchanged.
- A finding from the fixtures: WPF applies `generic.xaml`'s style to a plot as soon as it's made, even outside a window, so off-screen plots save and draw a white background. The port makes that white the default.
- Still to do: the legacy-format fallback in `RMC.BestFit.UI` (Phase 6), `OxyPlotControls`, and screenshot parity.

### Phase 5: Numeric, database & expression controls (L)
- `NumericControls`: `DistributionSelectorControl`, `ProbabilityOrdinatesControl`, `TimeSeriesTable`, `UncertainOrderedTableEditor`, … (plus their dependencies).
- `DatabaseControls.TableViewer`, plus Field Calculator, Find & Replace, Column Stats and Select-by-Attribute.
- `ExpressionControl` on AvaloniaEdit, and Calculator Help (HTML).

**Progress (2026-10-30):** `ExpressionParserControls` is ported. The expression editor runs on AvaloniaEdit, colouring the same tokens as upstream's RichTextBox runs (parentheses by depth, functions with help as links), with the calculator panel and the function reference. Headless tests cover typing, paste, the buttons and errors, and the gallery's capture matches upstream's screenshot.
- `NumericControls` has what BestFit uses: `ProbabilityOrdinatesControl`, `TimeSeriesTable` (with `MathEditorControl` and `NumericEntryDialog`), `UncertainOrderedTableEditor` and `DistributionSelectorControl`, with 46 tests and captures in three themes that match upstream's gallery (the distribution selector differs only in its unseeded sample data). Not ported (D10): the ordered and uncertain data selectors and table editors that wrap a plot, `Selector`, the selector popups (BestFit builds its own), `BivariateEmpiricalControl` and the bin definition control.
  - Upstream bugs kept: `ExpandPlot` is never read; the math button binds to a missing `PresetsPopup`; the math popup reads "Applies to rows 0, 0." and changes nothing when value cells are selected; validation messages after the first start with a newline; an invalid distribution set from code doesn't flag its parameter cells.
  - Found while porting, fixed in the shared grids: scroll bars were disabled in every grid, the filler header stayed with hidden column headers, wrapped headers kept a stale height, and two-way cell bindings wrote formatted (rounded) values back into the data.
  - The time-series date column formats in the machine's culture, as upstream, while numbers use en-US (§13, with D8).
- `DatabaseControls` is ported: `TableViewer` (a hand-drawn grid, not a DataGrid) and every window it opens (find and replace, the field calculator, select by attribute, the calculator help, the column statistics), with 35 tests and captures in three themes. Edits through the viewer write the same SQLite values as DatabaseManager does directly, and the calculator's results equal upstream's ExpressionParser. The help shows in Avalonia.HtmlRenderer. The column statistics views have OxyPlotControls' plot toolbar and properties panel, as upstream.
  - Upstream quirks kept: Escape never clears the copy selection; `SelectByAttribute.html` links pictures that exist only in the field calculator's help folder; the numeric statistics leave the wait cursor on when a classification gives no breaks; `CsvReader` types every column as text.

**Exit:** demo-page parity and `TableViewer` edit/undo tests.

### Phase 6: BestFit project layer (`RMC.BestFit.UI`) (M)
- The ported `OxyPlot.Wpf` `Plot` types (same names), icon keys and portable colours; `DssTimeSeriesReader` on the new natives.
- Port `RMC.BestFit.UI.Tests` (711 tests) to Avalonia.Headless.
- **File-compatibility suite:** open and save every example project, then compare to the WPF-saved files table by table and cell by cell, with exact plot XML.

**Exit:** ported tests green on 3 OSes; file suite green.

**Progress (2026-10-30):** `src/BestFit/RMC.BestFit.UI` is ported. 16 of its 38 C# files change: Avalonia's colours, brushes, thicknesses and images, WpfCompat's `ColorConverter` and `Visibility`, Avalonia's binding API for the axis-title defaults, and no Windows-only attribute. The other 22 compile unchanged. Its 13 element icons are an `.axaml` dictionary that follows the theme.
- `tests/BestFit/RMC.BestFit.UI.Tests` runs upstream's tests unchanged, 1,131 of them with their data rows. Upstream's `[STATestMethod]` tests run on the headless UI thread, and the public API baseline differs only in the image type (`Avalonia.Media.IImage` for WPF's `ImageSource`). Two compatibility tests open example projects from the fetched sample data.
- On Windows all 1,131 of upstream's tests pass, the 36 DSS reader tests included (on the 7-JA-7 natives), and XGoodFit's icon test. `dotnet test` leaves out the two in category External, which download from live services (USGS, NOAA) and fail when one is down.
- **File-compatibility suite: done (2026-10-30).** `tools/BestFitFileCompat.Wpf` (Windows only) builds upstream's own WPF `RMC.BestFit.UI` 2.0.1, unedited, against `RMC.Wpf.Framework` 1.0.5 from nuget.org, opens and saves every example project as File > Save does, and describes the saved file cell by cell; `tests/BestFit/RMC.BestFit.FileCompat.Tests` does the same with the port on every OS against that golden data. On Windows all 32 examples match: 146 tables and 9,115 cells, plot XML byte for byte, numbers bit for bit, and the 579 element states after opening and reopening. CI reruns the WPF reference on Windows and prints each OS's summary.
  - Outside Windows the suite reports, without failing: XML that RMC-BestFit stores has the OS's line ends (`XElement.ToString()`), the path separator in `FullFileName`, and last-bit rounding in statistics computed in memory. WPF opening files the port saved on macOS or Linux belongs to Phase 9's bidirectional matrix.
  - Upstream behaviour found: the project's `LastModified` is written one save late; in `bulletin-17c-bayesian-examples`, saving "B17C Example #3" (a 2.0-beta row that opens invalid) clears its `AnalysisResults` and 352 KB of `MCMCResults` (worth reporting upstream); `Tools.DateFromString` and `DateToUniversalString` read and write the projects' and elements' `CreationDate` and `LastModified` in the current culture, so on a non-US machine (en-AU, say) the examples' US dates are lost (saved as 1/01/0001) or have day and month swapped. The port keeps all three, as upstream; the dates need a decision (D8).
  - Regenerate `Golden/` when bumping RMC-BestFit or the framework packages (§10). The 1.0.5 packages were built from WPF-Framework `05acb1e`, the vendored pin is `7f897e1`; the saves don't differ.

### Phase 7: BestFit App, in vertical slices (XL)
Each slice includes its XAML and code-behind, its share of `RMC.BestFit.App.Tests` (465 total), parity screenshots in 3 themes, and the matching `examples/` walkthrough executed as a UAT script.

1. **Bootstrap:** `App`, `MainProjectNode` (1.6K lines), `IconDictionary`, `BestFitStyles`, file-open arguments, online-help launcher.
2. **Time Series Data:** USGS/GHCN/CHMN/ABOM download, DSS path selector, raw-text views, alternatives.
3. **Input Data:** `InputDataControl`, 3.7K lines.
4. **Distribution Fitting Analysis.**
5. **Shared Bayesian support controls:**
   - Priors: parameter and quantile priors, Bayesian options/output.
   - Diagnostics plots: MCMC traces, histogram, kernel density, autocorrelation, mean likelihood, bivariate heat map, influence.
   - Reports and tables: parameter sets, ordinates, MCMC and GMM reports.
6. **Univariate analyses:** Bayesian/MCMC, then Bulletin 17C, Mixture, Composite and Point Process.
7. **Bivariate & Coincident Frequency.**
8. **Rating Curve.**
9. **Time Series Analysis** (ARIMA/ARIMAX).
10. **Batch Run window.**

**Exit:** every examples walkthrough completes on 3 OSes, and App tests are green.

**Progress (2026-10-30):** the application's skeleton is in place, so the GUI areas can be ported in parallel.
- `src/BestFit/RMC.BestFit.App` (opt-in overlay) builds `XGoodFit` (the executable; namespace `RMC_BestFit` as upstream): `Program` (Velopack's hook, then Avalonia), `App` ported (theme, version 2.0.1 written to projects as upstream's, the version 1.0 warning, files to open through `FileOpenRequests`, Velopack updates when installed, the auto-check), the application's icon dictionary, and the 51 upstream support files that compile unchanged (combo box items, grid rows, result points). `RMC.BestFit.UI` grants its internals to `XGoodFit` as it does to `RMC-BestFit`.
- Placeholders (marked "XGoodFit port: placeholder") stand in for `MainProjectNode` and each area's document and properties views, with the members `MainProjectNode` uses, until those are ported. `Resources/BestFitStyles.xaml` isn't ported: nothing merges it or uses its keys.
- `tests/BestFit/RMC.BestFit.App.Tests` (opt-in): 271 of upstream's tests (the support classes) pass unchanged, with the ported `WpfTestHost` and a headless `[STATestMethod]`, and a startup test makes the main window.

### Phase 8: Platform integration & distribution (L)
- Self-contained publish per RID (trimming off: reflection-heavy). ReadyToRun is optional.
- **Windows:** Velopack `Setup.exe` plus a portable zip; `.bestfit` registered as an extra handler (§1.1); import of MRU and settings from an existing RMC-BestFit install.
  - Free-only (D5) means builds start **unsigned**, so SmartScreen shows "unknown publisher".
  - Apply to the SignPath Foundation (free code signing for open-source projects), or document the "More info → Run anyway" step (R15).
- **macOS:** `.app` (`.icns`, `Info.plist` document types for `.bestfit`), osx-arm64 + osx-x64, hardened runtime (`allow-jit`), Cmd-based shortcuts.
  - Sign and notarize with your Apple Developer ID (D5), using Velopack's signing and notary options.
  - macOS packages must be built on macOS: a GitHub macOS runner with the certificates in encrypted secrets.
  - Velopack `.pkg` / `.app`, plus a DMG if wanted.
- **Linux:** Velopack AppImage (+ tarball), `.desktop` + MIME + icons; checks on GNOME/KDE, X11 and XWayland, including the portal file pickers.
- **Updates:** Velopack, from this repo's GitHub Releases (with deltas), replacing `SoftwareUpdate` (D7).

**Exit:** CI produces installable artifacts for every OS, and the update flow is tested end-to-end.

**Progress (2026-10-30):** packaging is in place, with `tools/ThemeGallery` as the stand-in app until `RMC.BestFit.App` is ported (`APP_PROJECT` in `scripts/package.sh`).
- `scripts/package.sh` builds, per runtime, a self-contained untrimmed publish with that runtime's HEC-DSS native, packed by Velopack: Windows `Setup.exe` (per-user) and a portable zip; macOS `.pkg` and a portable `.app` (osx-arm64 and osx-x64, `.icns`, `.bestfit` exported with the Alternate rank, hardened runtime with .NET's entitlements); Linux AppImage and tarball (`.desktop`, `application/x-bestfit`). Each runtime is its own update channel; deltas work (tested on Windows).
- `scripts/check-package.sh` checks a package without installing it: structure, signature, and the packaged app run headless. Verified on Windows, Linux, and macOS arm64 and x64 (Rosetta).
- `src/XGoodFit.Platform`: Velopack's startup hook, Windows' per-user "Open with" registration (never the default, removed on uninstall), the update service, and files to open from the command line or Finder (`FileOpenRequests`).
- `.github/workflows/package.yml` packs on each OS (Linux on ubuntu-22.04 for an older glibc) on manual runs and `v*` tags, and drafts a release on tags. Apple signing and notarization need the secrets `APPLE_DEVELOPER_ID_CERTIFICATE`, `APPLE_DEVELOPER_ID_CERTIFICATE_PASSWORD`, `APPLE_ID`, `APPLE_TEAM_ID` and `APPLE_APP_SPECIFIC_PASSWORD`; without them macOS builds are ad-hoc signed. `docs/install.md` is the per-OS install page.
- Still open: the workflow on real runners and a real release-to-release update; Developer ID signing and notarization (if notarization rejects Velopack's `codesign --deep`, use `--signDisableDeep`); importing MRU and settings from an existing RMC-BestFit install; checks on KDE and X11; SignPath; a DMG if wanted; XGoodFit's own icon (the packages use RMC-BestFit's as a placeholder, `XGoodFitIcon` in `Directory.Build.props`). The bundle declares macOS 12.0, the highest minimum among its binaries; set it to .NET 10's own minimum before release.

### Phase 9: Verification & release (M)
- Numerical reproducibility report across RIDs (model tests + scoped verification runs).
- Visual parity sign-off (view × theme × OS).
- **Bidirectional file matrix** (WPF 2.0.x ↔ port).
- Performance benchmarks versus WPF: large time-series grids, MCMC plots, startup.
- Keyboard and accessibility pass.
- Per-OS install docs and a "known differences" page.
- RC → beta testers → 1.0.

---

## 8. Testing & verification

| Layer | How |
|---|---|
| Model/numerics | Upstream suites unchanged, run on win/mac/linux × x64/arm64; differences beyond tolerance are reported upstream (this is life-safety software) |
| Framework controls | Unit and headless tests for compat shims, grid clipboard, add-row, validation, undo/redo |
| Project layer | Ported `RMC.BestFit.UI.Tests` (711) on Avalonia.Headless; most become plain tests once WPF/STA is gone |
| App | Ported `RMC.BestFit.App.Tests` (465); MSTest via a manual `HeadlessUnitTestSession` |
| File compatibility | Round-trip all 32 example projects; exact plot-XML fixtures from WPF; WPF opens port-saved files (Windows CI job) |
| Visual parity | Parity harness (§6.8): per view × theme, perceptual diff with thresholds; reviewed in PRs |
| Acceptance | The `examples/` tutorial walkthroughs become UAT scripts run on each OS |

---

## 9. Platform integration & distribution

### 9.1 Per-OS concerns
- **Paths:** one `AppPaths` helper.

  | OS | Location |
  |---|---|
  | Windows | `%AppData%`, `%LocalAppData%` |
  | macOS | `~/Library/Application Support/XGoodFit` |
  | Linux | XDG dirs (`~/.config`, `~/.local/share`) |

  Linux file systems are case-sensitive, so resource and asset names must match exactly.
- **Shortcuts and menus:**
  - Use `PlatformHotkeyConfiguration` so Ctrl becomes Cmd on macOS.
  - The in-window menu is kept on every OS for 1.0 (D4); the macOS global menu (`NativeMenu`) comes after 1.0 (§13).
- **File association:** `.bestfit` on all three OSes, including opening files from Finder or a file manager.

### 9.2 HEC-DSS natives
- Build `libhecdss` per RID in CI from the pinned `hec-dss` source (7-JA-7) and zlib, linked statically (done: `native/build-hecdss.sh`; the library loads only the OS's C runtime).
- Ship the natives under `runtimes/<rid>/native/`.
- If they're missing at runtime, DSS import shows a clear "not available on this platform" message instead of crashing.

### 9.3 Updates
The existing updater:
- reads a `SHA256:` token from the GitHub release body;
- downloads a zip;
- swaps files using `SoftwareUpdate.Updater.exe`.

On macOS, swapping files inside a signed `.app` breaks the signature.

**Decision (D7): Velopack** (MIT), on every OS. It covers:
- Windows Setup/MSI, macOS `.app`/`.pkg` and Linux AppImage packages;
- GitHub Releases as the update feed;
- delta updates;
- built-in signing and notarization.

The port publishes releases from its own repository. `SoftwareUpdate`'s `IUpdateService` stays, so the main window's update UI is unchanged; `XGoodFit.Platform.VelopackUpdateService` implements it (same states, events and skipped versions), and upstream's `GitHubUpdateService` and updater exe go unused. Velopack is pinned at 1.2.161.

Velopack is a fast-moving 1.x release, so pin its version. Note that its macOS updater checks file hashes, not the code signature.

---

## 10. Tracking upstream

USACE is actively developing BestFit (2.0.1 shipped recently).

1. **Baseline pins.** `upstream.lock` records the upstream commit every ported file came from, and `upstream/` holds that exact code.
2. **For each upstream release:**
   - Bump the commit in `upstream.lock` and run `scripts/vendor-upstream.sh <name>`. The new upstream code is staged, byte-identical to the new commit.
   - Review the upstream changes with `git diff --cached -- upstream/RMC-BestFit/src/RMC.BestFit.App upstream/RMC-BestFit/src/RMC.BestFit.UI` (and the framework).
   - Port each hunk to the mirrored file in `src/`, and commit the re-vendored code and the port together.
   - Re-run the parity and file suites.
3. **Model-library changes need no porting** (they are compiled from the pinned source or taken from NuGet).
4. **Keep the 1:1 structure.** This is what makes step 2 cheap.
5. **Move the HEC-DSS pins and the ported `DssNative.cs` together.** Before bumping `hec-dss`, compare every function the wrapper declares with the new `hecdss.h`, parameter for parameter, and update `DssNative.cs` to match. Then re-run the DSS suites on every OS. HEC's `main` after 7-JA-7 already changes `hec_dss_tsRetrieve` again (Phase 0 findings).

---

## 11. Risk register

| # | Risk | L | I | Mitigation |
|---|---|:-:|:-:|---|
| R1 | ProDataGrid gaps or abandonment (single maintainer, created Jul 2025) | M | H | Spike A; pin; wrap behind `CopyPasteDataGrid` API; MIT, so we can fork |
| R2 | The ported plotting layer doesn't render identically to WPF's | M | H | Spike C; WPF-written fixtures; parity harness |
| R3 | Plot-XML incompatibility breaks `.bestfit` round-trip | M | H | Golden WPF fixtures; exact-XML tests; schema frozen |
| R4 | Async dialog conversion changes control flow | M | M–H | Spike D; sync-compat for complex sites; tests per site |
| R5 | HEC-DSS natives needed on every OS at 1.0 (D6), but HEC publishes macOS only for 7-JA-7 (`libz` rpath) and the pre-build step is Windows-only | M | H | Build from source in CI for win-x64, linux-x64, osx-arm64 and osx-x64; sign with the app; keep a clear "not available" message as a safety net |
| R6 | Dock.Avalonia can't match AvalonDock look or behaviour (auto-hide, Wayland floating) | M | M | Spike B; custom theme (done 2026-10-30; matches the screenshots within a pixel); managed floating windows on Linux |
| R7 | Silent behaviour changes (culture, `UpdateSourceTrigger`, `Hidden`, validation, focus, `Loaded`) | H | M | §5.2 rules, grep/analyzer checks, ported tests, UAT |
| R8 | Upstream drift during a multi-month port | H | M | §10 process |
| R9 | Cross-platform floating-point differences in results | L–M | H | Run test/verification suites per RID; document tolerances |
| R10 | macOS signing/notarization pipeline (Developer ID provided, D5) breaks or leaks credentials | L | M | Velopack signing + notarization on GitHub macOS runners; certificates and notary key in encrypted secrets; Gatekeeper test on a clean user account |
| R11 | Linux: no custom chrome on X11; Wayland backend experimental | H | L | Native decorations; default XWayland |
| R12 | Font metrics (Selawik vs Segoe UI line height) cause clipping | M | L–M | Embedded fonts; parity harness |
| R13 | Performance (big grids, dense plots) | M | M | Benchmarks in Phases 0 and 7; virtualization tuning |
| R14 | `System.Data.SQLite` 2.0 untested upstream on macOS/Linux | L–M | H | Phase 0 validation; fallback `Microsoft.Data.Sqlite` inside `DatabaseManager` |
| R15 | Unsigned Windows builds (free-only, D5) trigger SmartScreen warnings | H | L–M | Apply to the SignPath Foundation (free signing for OSS); otherwise document "More info → Run anyway" |
| R16 | Name conflicts with a trademark, or users mistake the port for an official USACE release | L | M | Distinct name plus trademark search; disclaimer in the About box and README (§1.1) |
| R17 | HEC's .NET wrapper (`hec-dss-dotnet`) has no licence, so it may not be safe to redistribute; DSS import is required at 1.0 (D6) | M | H | Working assumption: treat it as MIT, like the C library (2026-10-29); confirm with HEC before the first public release. Fallback: own read-only wrapper over the MIT C library, verified against the official one |
| R18 | HEC changes `hecdss` function signatures without updating the .NET wrapper (already happened: 2.0.3 matches no hec-dss release), so a mismatched pair corrupts memory instead of failing | H | H | Mitigated (2026-10-30): ported `DssNative.cs` matching the pinned native; a mismatched native is rejected at load (`hec_dss_api_version()`); a test compares every declaration with `hecdss.h`, parameter for parameter; DSS read tests on every OS (§10) |

---

## 12. Decisions (resolved 2026-10-29)

| # | Decision | Answer | What it changes |
|---|---|---|---|
| D1 | Goal and home | Own community port, in this repo | Monorepo layout (§4.1); upstream vendored at the pins in `upstream.lock` (D11) |
| D2 | Platforms | Windows x64, macOS arm64 + x64, Linux x64; more later | CI matrix and HEC-DSS natives for 4 RIDs; arm64 Linux/Windows moved to §13 |
| D3 | Windows | One Avalonia codebase, replacing WPF on Windows too; a new product name is welcome | Branding layer and naming (§1.1) |
| D4 | Look on macOS/Linux | Identical look for 1.0; native macOS conventions later | In-window menu everywhere, Cmd shortcuts on macOS; native conventions in §13 |
| D5 | Budget | Free/open-source only; you provide an Apple Developer ID | macOS signed and notarized; Windows unsigned unless free OSS signing is obtained (R15) |
| D6 | DSS import | Required on every platform at 1.0 | HEC-DSS natives built and signed for all 4 RIDs, on Phase 0's critical path (R5) |
| D7 | Install and update | Velopack | Replaces `SoftwareUpdate`; this repo's GitHub Releases are the feed (§9.3) |
| D8 | Locale | US formats for parity now; follow the OS locale eventually | Central `FormattingCulture` (default en-US) from Phase 1 (§5.2); the switch is in §13 |
| D9 | Test hardware | M4 Pro MacBook Pro over Tailscale | macOS arm64 dev/test, x64 via Rosetta 2; Linux options in Phase 0 |
| D10 | Framework scope | BestFit only; the whole framework later | Port only what BestFit uses; keep notes for a later framework port (§13) |
| D11 | Self-containment (2026-10-30) | As standalone as possible: all code needed to build lives in this repo, citing the original repos | Upstream code vendored byte-identically in `upstream/` with a provenance README (§4.1); RMC.Numerics built from source; only NuGet packages and optional sample data are downloaded |

### 12.1 Follow-up items (resolved 2026-10-29)

| Item | Answer |
|---|---|
| Product name | **XGoodFit** |
| Repository and licence | Public GitHub repo [jonklaric/XGoodFit](https://github.com/jonklaric/XGoodFit), **MIT** |
| Linux x64 test machine | A dedicated Ubuntu 26.04 GNOME (Wayland) desktop on the tailnet; other Linux machines tested ad hoc |
| macOS test machine | The M4 Pro MacBook Pro, used periodically, as it's also a working machine |
| Remote access | SSH with a dedicated, revocable key; all test files live under `~/xgoodfit-dev` (private .NET SDK, no system changes) |
| HEC-DSS .NET wrapper licence | Treat it as MIT, like the HEC-DSS C library, for now (R17) |

---

## 13. Post-1.0 backlog

- **macOS-native conventions (D4):** global menu bar (`NativeMenu`), the app menu (About, Settings, Quit), standard Mac shortcuts and window behaviour.
- **OS locale for dates and numbers (D8), with a user override.** First test parsing and formatting under e.g. en-AU and de-DE, including grid paste and CSV import.
- **More architectures (D2):** Linux arm64 and Windows arm64, which need HEC-DSS natives for those RIDs.
- **The rest of WPF-Framework (D10)**, reusing the BestFit lessons, for RMC-RFA, TotalRisk and LifeSim.
- **Avalonia's native Wayland backend**, once it's no longer experimental.
- **Gradual MVVM extraction and compiled bindings**, where they don't make re-porting upstream changes harder.

---

## Appendix A: References

**Avalonia migration docs** (each page is also served as Markdown by appending `.md`)
- Index: https://docs.avaloniaui.net/docs/migration/wpf.md
- Pages under the same path: `cheat-sheet`, `controls`, `data-templates`, `events`, `layout`, `properties`, `styling`.

**Avalonia API and how-to pages**
- Avalonia 12 breaking changes: https://docs.avaloniaui.net/docs/avalonia12-breaking-changes
- Theme variants: https://docs.avaloniaui.net/docs/styling/theme-variants
- Control themes: https://docs.avaloniaui.net/docs/styling/control-themes
- DataGrid (deprecated notice): https://docs.avaloniaui.net/controls/data-display/structured-data/datagrid
- Build MCP (AI migration tooling): https://docs.avaloniaui.net/tools/ai-tools/build-mcp

**Libraries**
- Dock: https://github.com/wieslawsoltes/Dock
- ProDataGrid: https://github.com/wieslawsoltes/ProDataGrid
- OxyPlot.Avalonia (the reference for the OxyPlot port): https://github.com/oxyplot/oxyplot-avalonia
- AvaloniaEdit: https://www.nuget.org/packages/Avalonia.AvaloniaEdit
- Velopack: https://github.com/velopack/velopack

**HEC-DSS and SQLite**
- HEC-DSS: https://github.com/HydrologicEngineeringCenter/hec-dss
- HEC-DSS .NET wrapper: https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet
- System.Data.SQLite 2.0 news: https://system.data.sqlite.org/home/doc/trunk/www/news.md

**XPF and fonts**
- Avalonia XPF: https://avaloniaui.net/xpf
- Selawik: https://github.com/microsoft/Selawik
- Cascadia: https://github.com/microsoft/cascadia-code
