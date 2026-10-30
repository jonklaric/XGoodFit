# Upstream issues to raise

XGoodFit ports USACE's RMC-BestFit from WPF to Avalonia. While porting it, and the WPF-Framework, Numerics and HEC-DSS code it depends on, we found problems in those upstream projects. Most affect upstream's own Windows application as well. Each file here is one issue, written for the upstream maintainers, who don't need to know XGoodFit. **Nothing has been posted anywhere.** Raising them is the project owner's decision.

Each file has the same sections: target and version, type and severity, verification, summary, how to reproduce, why it's a bug rather than intended behaviour, where in the source (permalinks at the commits below), a suggested fix, and how XGoodFit handles it. Links to XGoodFit files are permalinks at [`fcb3902`](https://github.com/jonklaric/XGoodFit/tree/fcb3902c2937662ed5f1b3869e0ea019e88e3c63), or at [`a7aced1`](https://github.com/jonklaric/XGoodFit/tree/a7aced180aba4493b1f8f7b676db89543ae9a0a7) for the issues found while porting RMC.BestFit.App.

## What was checked

| Repository | Version | Commit checked | Still the latest on 2026-10-30? |
|---|---|---|---|
| [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) | 2.0.1 | [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) | yes (`main`) |
| [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) | 1.0.5 | [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93) | yes (`main`). The 1.0.5 NuGet packages were built from `05acb1e`, which differs only in documentation |
| [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics) | 2.2.0 | [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0) (tag `v2.2.0`) | yes (`main`) |
| [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) | 2.0.3 | [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd) | yes (`main`) |
| [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss) | 7-JA-7 | [`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7) | no: `main` is 11 commits ahead ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07). Every hec-dss issue here was checked against `main` too; leads already fixed there are listed under "Considered and excluded" |

RMC-BestFit, WPF-Framework and Numerics were checked again on 2026-10-31, for the RMC.BestFit.App issues: `main` was unchanged.

**Severity:** *high* means it changes or loses users' data or results, or corrupts memory. *medium* means a visible malfunction, or a wrong result in a narrower case. *low* means cosmetic, latent, a test or build problem, or cleanup.

**Verified:** *Reproduced* means it was run against upstream's own code: a throwaway program, the published WPF-Framework 1.0.5 packages, RMC-BestFit 2.0.0's shipped binaries, or upstream code built from these commits. *Read* means verified by reading the source; where it was also seen in CI or in XGoodFit's port, the issue says so.

## Index

### USACE-RMC/RMC-BestFit

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| Editing an Input Data row can change an identical earlier row instead, and lose the value typed | [RMC-BestFit-grid-row-edits-change-the-wrong-model-row.md](RMC-BestFit-grid-row-edits-change-the-wrong-model-row.md) | data loss | high | Reproduced (upstream 2.0.1 model) |
| "B17C Example #3" opens empty, and saving it discards its stored results | [RMC-BestFit-b17c-example-3-loses-results-on-save.md](RMC-BestFit-b17c-example-3-loses-results-on-save.md) | data loss | medium | Reproduced (upstream 2.0.1 code) |
| Editing XY ordinates during a background reprocess clears `IsEstimated` | [RMC-BestFit-reprocess-race-clears-isestimated.md](RMC-BestFit-reprocess-race-clears-isestimated.md) | bug, test reliability | medium | Reproduced (13/20 and 12/20 runs) |
| The UI mixes the machine's culture and en-US | [RMC-BestFit-mixes-machine-culture-and-en-us.md](RMC-BestFit-mixes-machine-culture-and-en-us.md) | user interface | medium | Read; seen on en-AU |
| The batch run window can be closed during a run, leaving the run going with no way to cancel it | [RMC-BestFit-batch-run-window-closes-during-run.md](RMC-BestFit-batch-run-window-closes-during-run.md) | bug | medium | Reproduced (WPF command routing); rest Read |
| Two model tests depend on wall-clock timing | [RMC-BestFit-timing-dependent-model-tests.md](RMC-BestFit-timing-dependent-model-tests.md) | test reliability | low | Read; failed on CI, not locally |
| Closed document views stay subscribed to their elements, so they're never released | [RMC-BestFit-views-not-released-after-close-or-replace.md](RMC-BestFit-views-not-released-after-close-or-replace.md) | bug (memory leak) | low | Read |
| Grid column header styles are rebuilt on top of themselves | [RMC-BestFit-header-styles-based-on-themselves.md](RMC-BestFit-header-styles-based-on-themselves.md) | bug (resource growth) | low | Read |
| The shell never sees the icon key of Time Series, Input Data and Fitting Analysis elements | [RMC-BestFit-element-icons-not-found-by-shell.md](RMC-BestFit-element-icons-not-found-by-shell.md) | user interface | low | Reproduced (C# rule); effect Read |
| The prior distribution editor's title is "Select Distribution for :" for stationary distributions | [RMC-BestFit-prior-editor-title-empty-for-stationary-distributions.md](RMC-BestFit-prior-editor-title-empty-for-stationary-distributions.md) | user interface | low | Read |
| The MCMC trace plot without warm-up still shows the last warm-up draw | [RMC-BestFit-trace-plot-includes-last-warm-up-draw.md](RMC-BestFit-trace-plot-includes-last-warm-up-draw.md) | user interface | low | Read |
| The Input Data summary statistics separators are black, so they vanish in the Dark theme | [RMC-BestFit-summary-statistics-separators-black-in-dark.md](RMC-BestFit-summary-statistics-separators-black-in-dark.md) | user interface | low | Read |
| New element names suggested from the element count can already be taken | [RMC-BestFit-default-names-can-clash.md](RMC-BestFit-default-names-can-clash.md) | user interface | low | Read |
| `Resources/BestFitStyles.xaml` is never used | [RMC-BestFit-unused-bestfitstyles-dictionary.md](RMC-BestFit-unused-bestfitstyles-dictionary.md) | bug (cleanup) | low | Read |
| Small fixes in RMC.BestFit.App: two latent null dereferences, an unthemed progress bar, a two-way visibility binding, an unused converter and stale documentation | [RMC-BestFit-cleanup-latent-and-trivial.md](RMC-BestFit-cleanup-latent-and-trivial.md) | bug (cleanup) | low | Read |

### USACE-RMC/WPF-Framework

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| Project and element dates are stored in the machine's culture; lost or swapped elsewhere (also affects RMC-BestFit) | [WPF-Framework-dates-depend-on-current-culture.md](WPF-Framework-dates-depend-on-current-culture.md) | data loss | high | Reproduced |
| NumericUpDown/NumericSlider corrupt their value on comma-decimal machines | [WPF-Framework-numericupdown-numericslider-misread-values-on-comma-decimal-machines.md](WPF-Framework-numericupdown-numericslider-misread-values-on-comma-decimal-machines.md) | wrong result | high | Reproduced (WPF) |
| TimeSeriesTable shows values in en-US but parses edits in the machine's culture | [WPF-Framework-timeseriestable-values-shown-en-us-but-parsed-in-machine-culture.md](WPF-Framework-timeseriestable-values-shown-en-us-but-parsed-in-machine-culture.md) | wrong result | high | Reproduced (WPF) |
| The "dual-culture" paste parsing never falls back ("1.5" gives 15 on de-DE) | [WPF-Framework-dual-culture-parse-never-falls-back.md](WPF-Framework-dual-culture-parse-never-falls-back.md) | wrong result | medium | Reproduced |
| Time-series math functions ignore selected rows ("Applies to rows 0, 0.") | [WPF-Framework-math-editor-ignores-selected-rows.md](WPF-Framework-math-editor-ignores-selected-rows.md) | bug | medium | Read |
| Terms "I agree" unlocks before the terms are loaded | [WPF-Framework-terms-agree-unlocks-before-scrolling.md](WPF-Framework-terms-agree-unlocks-before-scrolling.md) | bug | medium | Reproduced (WPF) |
| The axis editor rewrites `StringFormat` just by showing an axis | [WPF-Framework-axis-editor-rewrites-stringformat.md](WPF-Framework-axis-editor-rewrites-stringformat.md) | bug | medium | Read |
| NumericUpDown/NumericSlider lose their DecimalPlaces/ThousandsSeparator formatting | [WPF-Framework-numericupdown-formatting-replaced-by-binding-text.md](WPF-Framework-numericupdown-formatting-replaced-by-binding-text.md) | user interface | low | Reproduced (WPF) |
| NumericUpDown's arrows don't repeat when held | [WPF-Framework-numericupdown-arrows-do-not-repeat.md](WPF-Framework-numericupdown-arrows-do-not-repeat.md) | user interface | low | Read |
| NumericTextBox2's error border never shows | [WPF-Framework-numerictextbox2-error-border-never-shows.md](WPF-Framework-numerictextbox2-error-border-never-shows.md) | user interface | low | Reproduced (WPF) |
| StringListPropertyControl keeps an edit cancelled with Escape | [WPF-Framework-stringlistpropertycontrol-escape-keeps-edit.md](WPF-Framework-stringlistpropertycontrol-escape-keeps-edit.md) | bug | low | Reproduced (WPF) |
| DistributionSelectorControl ignores `ExpandPlot` | [WPF-Framework-distributionselector-expandplot-ignored.md](WPF-Framework-distributionselector-expandplot-ignored.md) | user interface | low | Read |
| DistributionSelectorControl doesn't flag invalid parameters set from code | [WPF-Framework-distributionselector-invalid-parameters-not-flagged-from-code.md](WPF-Framework-distributionselector-invalid-parameters-not-flagged-from-code.md) | user interface | low | Read |
| TimeSeriesTable's math button binds to a missing `PresetsPopup` | [WPF-Framework-timeseriestable-math-button-binds-missing-presetspopup.md](WPF-Framework-timeseriestable-math-button-binds-missing-presetspopup.md) | user interface | low | Read |
| Validation messages start with a blank line | [WPF-Framework-propertyrule-message-leading-newline.md](WPF-Framework-propertyrule-message-leading-newline.md) | user interface | low | Read |
| TableViewer's Escape handler is unreachable | [WPF-Framework-tableviewer-escape-unreachable.md](WPF-Framework-tableviewer-escape-unreachable.md) | bug | low | Read |
| Select by Attribute help links two images it doesn't have | [WPF-Framework-selectbyattribute-help-missing-images.md](WPF-Framework-selectbyattribute-help-missing-images.md) | documentation | low | Read |
| Column statistics leave the wait cursor on | [WPF-Framework-numericcolumnstats-wait-cursor-stuck.md](WPF-Framework-numericcolumnstats-wait-cursor-stuck.md) | user interface | low | Read |
| Dark theme: tab hover (and three unused keys) use Light colours | [WPF-Framework-dark-theme-uses-light-colours-for-tab-hover.md](WPF-Framework-dark-theme-uses-light-colours-for-tab-hover.md) | user interface | low | Read |
| Project Explorer: keyboard selection isn't highlighted | [WPF-Framework-project-explorer-keyboard-selection-not-highlighted.md](WPF-Framework-project-explorer-keyboard-selection-not-highlighted.md) | user interface | low | Read |
| Message window filter buttons don't update their text | [WPF-Framework-message-window-filter-buttons-not-refreshed.md](WPF-Framework-message-window-filter-buttons-not-refreshed.md) | user interface | low | Read |
| A project's layout is ignored without the default layout file | [WPF-Framework-project-layout-ignored-without-default-layout-file.md](WPF-Framework-project-layout-ignored-without-default-layout-file.md) | bug | low | Read |
| `MainWindow.ProjectNode`: menus accumulate; a non-empty project opens every document | [WPF-Framework-mainwindow-projectnode-assignment-side-effects.md](WPF-Framework-mainwindow-projectnode-assignment-side-effects.md) | bug | low | Read |
| Dropping a file with a short path throws | [WPF-Framework-mainwindow-drop-of-short-path-throws.md](WPF-Framework-mainwindow-drop-of-short-path-throws.md) | bug | low | Read |
| TabControl Bottom/Right placement overlaps the content | [WPF-Framework-tabcontrol-bottom-right-placement-overlaps-content.md](WPF-Framework-tabcontrol-bottom-right-placement-overlaps-content.md) | user interface | low | Read |
| Axis Minimum/Maximum raise no PropertyChanged (no undo) | [WPF-Framework-axis-minimum-maximum-raise-no-propertychanged.md](WPF-Framework-axis-minimum-maximum-raise-no-propertychanged.md) | bug | low | Read |
| Annotation selector never shows its "Add … Annotation" options | [WPF-Framework-annotation-selector-add-options-never-shown.md](WPF-Framework-annotation-selector-add-options-never-shown.md) | bug | low | Read |
| Bar series settings don't restore the label font | [WPF-Framework-bar-series-font-not-restored.md](WPF-Framework-bar-series-font-not-restored.md) | bug | low | Read |
| OxyPlot.Tests pass only on en-US Windows | [WPF-Framework-oxyplot-tests-need-en-us-windows.md](WPF-Framework-oxyplot-tests-need-en-us-windows.md) | test reliability | low | Read; failures seen on en-AU |
| Cleanup checklist: unused code, styles that never apply, dangling resources (13 items) | [WPF-Framework-cleanup-unused-code-and-dangling-resources.md](WPF-Framework-cleanup-unused-code-and-dangling-resources.md) | bug (cleanup) | low | Read |

The cleanup checklist covers: implicit NameDialog/MessageBoxWindow styles that never apply; the `PDF` image and `RecentProjects` icon that resolve to nothing; three icons missing `x:Shared="False"`; `BooleanToBrushConverter.ConvertBack`; StringListPropertyControl's `// Exit Sub`; the unused italic-for-NaN converter; the uncertain table's bindings to missing columns; UtilityFunctions' unused MenuItem helpers; `OptionsDialog_Closing`; GenericSeriesControl's dead `PlotChanged`; the demo's `ExportMenuItem_Click`; and 35 unused DatabaseControls images.

### USACE-RMC/Numerics

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| An allocation test asserts an exact byte count | [Numerics-allocation-test-asserts-exact-byte-count.md](Numerics-allocation-test-asserts-exact-byte-count.md) | test reliability | low | Read; failed once on CI |
| Three bit-for-bit tests fail on arm64 | [Numerics-bitwise-tests-fail-on-arm64.md](Numerics-bitwise-tests-fail-on-arm64.md) | test reliability | low | Read; failed on CI (macOS arm64) |

### HydrologicEngineeringCenter/hec-dss-dotnet

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| Native declarations don't match any hec-dss release since March 2025 | [hec-dss-dotnet-native-declarations-out-of-date.md](hec-dss-dotnet-native-declarations-out-of-date.md) | bug (memory corruption) | high | Read (every declaration against 7-JA-7) |
| Please add a licence | [hec-dss-dotnet-add-a-licence.md](hec-dss-dotnet-add-a-licence.md) | licence | medium | Read |
| The project only builds on Windows; no macOS native | [hec-dss-dotnet-build-requires-windows.md](hec-dss-dotnet-build-requires-windows.md) | build | medium | Read; `sh` syntax error reproduced |
| Catalogue filter passed without a terminating null | [hec-dss-dotnet-catalog-filter-not-null-terminated.md](hec-dss-dotnet-catalog-filter-not-null-terminated.md) | bug | low | Read |
| `Qualities` isn't trimmed to the values read | [hec-dss-dotnet-quality-array-not-trimmed.md](hec-dss-dotnet-quality-array-not-trimmed.md) | bug | low | Reproduced |
| `IsDPartARange()` is true for `TS-PATTERN` | [hec-dss-dotnet-isdpartarange-true-for-ts-pattern.md](hec-dss-dotnet-isdpartarange-true-for-ts-pattern.md) | bug | low | Read |

### HydrologicEngineeringCenter/hec-dss

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| A NULL `timeZoneName` crashes the store functions (still on `main`) | [hec-dss-null-time-zone-crashes.md](hec-dss-null-time-zone-crashes.md) | bug (crash) | medium | Reproduced |
| Some records' quality flags change on every read | [hec-dss-quality-flags-vary-between-runs.md](hec-dss-quality-flags-vary-between-runs.md) | bug | medium | Reproduced (7-IU-6 and 7-JA-7) |
| DSS 6 files no longer give -700 from `hec_dss_open` | [hec-dss-dss6-open-returns-internal-error-not-700.md](hec-dss-dss6-open-returns-internal-error-not-700.md) | bug | low | Reproduced |
| Hard-coded `/tmp` for squeeze and sorted catalogues | [hec-dss-fixed-tmp-paths.md](hec-dss-fixed-tmp-paths.md) | bug | low | Read |
| Configure always downloads unpinned test data | [hec-dss-configure-downloads-unpinned-test-data.md](hec-dss-configure-downloads-unpinned-test-data.md) | build | low | Read |
| `hecdss_test.c` misses `<unistd.h>` (GCC 14+, Clang 16+) | [hec-dss-hecdss-test-c-missing-unistd-include.md](hec-dss-hecdss-test-c-missing-unistd-include.md) | build | low | Read; hit by XGoodFit's Linux build |
| `hec_dss_tsRetrieve` compares dates with `"\0"` by pointer | [hec-dss-tsretrieve-empty-dates-compared-by-pointer.md](hec-dss-tsretrieve-empty-dates-compared-by-pointer.md) | bug | low | Read |

## Pending

None. The reports of every porting agent that finished by 2026-10-30 are covered, including FrameworkUI's and OxyPlotControls', which arrived while this was being written. The 16 leads from the RMC.BestFit.App port's Input Data and Fitting Analysis work were added on 2026-10-31. Findings from later work can be added in the same format.

## Considered and excluded

| Lead | Target | Why it's not an upstream issue (or not a separate one) |
|---|---|---|
| Project `LastModified` written one save late | RMC-BestFit | The cascade does stamp it after the row is written, but the app saves only dirty projects (`MainWindow.SaveProject`), and any element edit makes the project dirty, so it's stamped before writing. Only code calling `Project.Save()` on a clean project, as XGoodFit's reference tool did, writes the previous save's time. |
| The default connection converts files to WAL and leaves `-wal`/`-shm` files | WPF-Framework (DatabaseManager) / RMC-BestFit | Deliberate ("Designed to optimize insertion time"). All 32 examples are already WAL, the side files are removed on close, and a read-only copy opens fine. What remains is SQLite's documented caveat for several machines on one network share, which wasn't observed. |
| BestFit's DSS error names `hecdss.dll` | RMC-BestFit | RMC-BestFit is Windows-only, where the name is right. |
| `FileReleaseProbe`/`ExclusiveFileProbe` rely on Windows file sharing | RMC-BestFit | The tests are in the WPF, Windows-only test project, where `FileShare.None` works. Port-only. |
| `Documentation_LocalLinksResolve` | RMC-BestFit | Its links resolve in upstream's repository; it fails only in XGoodFit's vendored copy, which leaves out those folders. |
| Tests in category `External` download from live services | RMC-BestFit | Already categorised so CI can leave them out. |
| `ActualPropertyWidth` bindings point at a missing property (FrameworkUI's `ProjectPropertiesControl`, the demo's `HazardPropertiesControl`) | WPF-Framework | **False lead.** `TextPropertyControl` (line 207), `ContentPropertyControl` and `NumericPropertyControl` all define it and implement `INotifyPropertyChanged`; the agent's check was cut short by `head`. XGoodFit's port comments said the binding fails; they're corrected, and a FrameworkUI test checks the columns line up. |
| The already-open check compares `Process.MainWindowTitle` | WPF-Framework | Empty only on macOS and Linux; upstream is Windows-only. Port-only. |
| OxyPlotToolbar's theme-change confirmation is modal | WPF-Framework | Intended, and `OxyPlotThemeManager.SuppressThemeChangeWarning` exists for automated hosts. |
| `CsvReader` types every column as text | WPF-Framework | CSV carries no types; a design limitation (used only by the demo), a feature request at most. |
| `_hitTestResult.VisualHit is TextBox` is nearly always false | WPF-Framework | Intent unclear, and no user-visible effect found. |
| `d.GetType() != typeof(MainWindow)` ignores subclasses | WPF-Framework | A codebase-wide pattern, and no subclass exists. |
| `OpenWindows.ConnectToMenu` throws if its parent isn't a MenuItem | WPF-Framework | Only on API misuse. |
| Element node subclasses have no implicit theme | WPF-Framework | Port-only: WPF falls back to Aero2. |
| TableViewer's "Failed to open database" uses WPF's MessageBox | WPF-Framework | A style inconsistency only. |
| Demo apps bind formatting to the current culture | WPF-Framework | Intended (and cited as evidence in the culture issues). |
| `WindowNonClientFrameThickness` margin | WPF-Framework | Not visible in WPF; a port detail. |
| Time-series date column in the machine's culture; chart axes against bound fields | WPF-Framework / RMC-BestFit | Folded into the TimeSeriesTable and RMC-BestFit culture issues. |
| OxyPlot tests need STA | WPF-Framework | Folded into the OxyPlot.Tests issue. |
| Uncertain table's merged-header widths; StringList `// Exit Sub`; italic converter; dead code and unused images | WPF-Framework | Folded into the cleanup checklist (none has a visible effect in WPF). |
| U+202F before AM/PM in saved dates on macOS/Linux | WPF-Framework | Folded into the dates issue as a note; .NET on Windows writes U+0020 (checked). |
| `DssWriter.Write(TimeSeriesProfile)` throws before its body | hec-dss-dotnet | An intentional `NotImplementedException` stub; only a compiler warning. |
| `deps/zlib` aliases `ZLIB::ZLIBSTATIC` to the shared zlib (unbundled `libz` on macOS) | hec-dss | Fixed on `main` by #361 (2026-06-17); waiting only for a release. |
| zlib's CMake renames `zconf.h` in its source folder | hec-dss / zlib | zlib's documented behaviour; it only matters to a vendored copy. |
| `hec_dss_tsStoreRegular` drops quality for doubles, and leaks the float copy (`allocated[...]` without `= 1`) | hec-dss | Found while verifying; both fixed on `main` by #365. |
| `hec_dss_tsRetrieve` copies quality without NULL or width checks | hec-dss | Fixed on `main` by #365 (the wrapper's own misuse is in the declarations issue). |
| Numerics' `LiveServiceIntegration` tests | Numerics | Upstream's CI already excludes them. |
| `BatchRunWindow`'s local `TextBlock` and `Image` styles have no `BasedOn` | RMC-BestFit | The Themes library defines no implicit style for those types, so nothing is lost. Only the progress bar's style drops a theme style; it's in the RMC.BestFit.App cleanup checklist. |
| Trace control's unchecked `MarkovChains.Count()`; `QuantilePriorsControl`'s two-way `Visibility`; unguarded `Plot.Equals` in `GetPropertiesControl`; the batch window's unused converter; `DocumentClosed`'s summary | RMC-BestFit | Folded into the RMC.BestFit.App cleanup checklist: latent or trivial. |
| `ParameterSetsControl` never unsubscribes; closing Input Data or Fitting Analysis doesn't clear `Element` | RMC-BestFit | Combined into one issue. The second lead is broader than reported: 11 of the 12 document views aren't cleared. |
| Prior grids' header styles and the summary statistics header styles | RMC-BestFit | Combined into one issue, with the three other places that use the same pattern. |
| `BatchRunWindow` blocks only the close command, not Alt+F4 | RMC-BestFit | Broader than reported: the close command isn't blocked either, because `MetroWindow`'s own binding runs first. Written up in the batch run window issue. |

## Reproductions

The throwaway programs used for the "Reproduced" entries aren't committed. Each issue shows the essential code and its output. They were run on Windows 11 x64, .NET 10.0.14, against: upstream source files compiled unchanged (dates); the `RMC.Wpf.Framework.Controls`/`Models` 1.0.5 NuGet packages (the WPF controls, the terms window and WAL); RMC-BestFit 2.0.0's shipped `Hec.Dss.dll` and 7-IU-6 `hecdss.dll`; hec-dss 7-JA-7 built from source; and RMC-BestFit's model built from `bfce2dd` (the race, and the grid row edits). WPF windows were created off-screen or laid out without being shown. For the element icons and the batch run window, small programs with the same type shapes as upstream's classes checked the C# rule for default interface members and WPF's command routing (.NET 10.0.14 and WindowsDesktop 10.0.14).
