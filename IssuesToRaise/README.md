# Upstream issues to raise

XGoodFit ports USACE's RMC-BestFit from WPF to Avalonia. While porting it, and the WPF-Framework, Numerics and HEC-DSS code it depends on, we found problems in those upstream projects. Most affect upstream's own Windows application as well. Each file here is one issue, written for the upstream maintainers, who don't need to know XGoodFit. **Nothing has been posted anywhere.** Raising them is the project owner's decision.

Each file has the same sections: target and version, type and severity, verification, summary, how to reproduce, why it's a bug rather than intended behaviour, where in the source (permalinks at the commits below), a suggested fix, and how XGoodFit handles it. Links to XGoodFit files are permalinks at [`fcb3902`](https://github.com/jonklaric/XGoodFit/tree/fcb3902c2937662ed5f1b3869e0ea019e88e3c63), or at [`a7aced1`](https://github.com/jonklaric/XGoodFit/tree/a7aced180aba4493b1f8f7b676db89543ae9a0a7) for the issues found while porting RMC.BestFit.App's Input Data and Fitting views, or at [`5f913ad`](https://github.com/jonklaric/XGoodFit/tree/5f913ad063782422048c7cf2109b081417f85faa) for those found while porting its Bivariate and Time Series Data views, or at [`149b96b`](https://github.com/jonklaric/XGoodFit/tree/149b96b320911b89c8b64cc786b3a4d1828896e6) for those found while porting its remaining views and testing the examples' walkthroughs, and for the third-party issue. The three found by the reproducibility check (2026-10-31) link to XGoodFit's `main`.

## What was checked

| Repository | Version | Commit checked | Still the latest on 2026-10-30? |
|---|---|---|---|
| [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) | 2.0.1 | [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) | yes (`main`) |
| [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) | 1.0.5 | [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93) | yes (`main`). The 1.0.5 NuGet packages were built from `05acb1e`, which differs only in documentation |
| [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics) | 2.2.0 | [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0) (tag `v2.2.0`) | yes (`main`) |
| [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) | 2.0.3 | [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd) | yes (`main`) |
| [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss) | 7-JA-7 | [`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7) | no: `main` is 11 commits ahead ([`f14554d`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/f14554de39a0bfe9c5c0b150e5fd66cfb78d289c), 2026-08-07). Every hec-dss issue here was checked against `main` too; leads already fixed there are listed under "Considered and excluded" |

RMC-BestFit, WPF-Framework and Numerics were checked again on 2026-10-31, for the RMC.BestFit.App issues and the Messenger issue, and again later that day for the issues written up from the Pending list: `main` was unchanged each time.

The third-party issue was checked against [wieslawsoltes/Dock](https://github.com/wieslawsoltes/Dock) 12.1.0.13 (the `Dock.Avalonia.AvalonDock` NuGet package XGoodFit uses), built from [`78da173`](https://github.com/wieslawsoltes/Dock/tree/78da173545501e796f37bc8528e9e55d9cbfd991) (tag `v12.1.0.13`), which was still `master` on 2026-10-31.

**Severity:** *high* means it changes or loses users' data or results, or corrupts memory. *medium* means a visible malfunction, or a wrong result in a narrower case. *low* means cosmetic, latent, a test or build problem, or cleanup.

**Verified:** *Reproduced* means it was run against upstream's own code: a throwaway program, the published WPF-Framework 1.0.5 packages, RMC-BestFit 2.0.0's shipped binaries, or upstream code built from these commits. *Read* means verified by reading the source; where it was also seen in CI or in XGoodFit's port, the issue says so.

## Index

### USACE-RMC/RMC-BestFit

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| Editing an Input Data row can change an identical earlier row instead, and lose the value typed | [RMC-BestFit-grid-row-edits-change-the-wrong-model-row.md](RMC-BestFit-grid-row-edits-change-the-wrong-model-row.md) | data loss | high | Reproduced (upstream 2.0.1 model) |
| "B17C Example #3" opens empty, and saving it discards its stored results | [RMC-BestFit-b17c-example-3-loses-results-on-save.md](RMC-BestFit-b17c-example-3-loses-results-on-save.md) | data loss | medium | Reproduced (upstream 2.0.1 code) |
| Editing XY ordinates during a background reprocess clears `IsEstimated` | [RMC-BestFit-reprocess-race-clears-isestimated.md](RMC-BestFit-reprocess-race-clears-isestimated.md) | bug, test reliability | medium | Reproduced (13/20 and 12/20 runs) |
| Switching a bivariate analysis's point estimator can clear `IsEstimated` (validation sorts the series the reprocess is reading) | [RMC-BestFit-bivariate-point-estimator-race-clears-isestimated.md](RMC-BestFit-bivariate-point-estimator-race-clears-isestimated.md) | bug (race condition), test reliability | medium | Reproduced (3/400 runs); seen in upstream's tests in XGoodFit's CI |
| The UI mixes the machine's culture and en-US | [RMC-BestFit-mixes-machine-culture-and-en-us.md](RMC-BestFit-mixes-machine-culture-and-en-us.md) | user interface | medium | Read; seen on en-AU |
| The batch run window can be closed during a run, leaving the run going with no way to cancel it | [RMC-BestFit-batch-run-window-closes-during-run.md](RMC-BestFit-batch-run-window-closes-during-run.md) | bug | medium | Reproduced (WPF command routing); rest Read |
| After undoing a bivariate model change, edits in the Output tab's XY editor are lost | [RMC-BestFit-bivariate-undo-detaches-xy-editor.md](RMC-BestFit-bivariate-undo-detaches-xy-editor.md) | data loss | medium | Read |
| Changing a composite's component can't be undone, and a later undo silently reverts it | [RMC-BestFit-composite-component-change-not-undoable.md](RMC-BestFit-composite-component-change-not-undoable.md) | data loss | medium | Reproduced (upstream's element logic in XGoodFit's port); seen in its view |
| Water-year block maxima are indexed by the event's calendar year: years repeat and go missing | [RMC-BestFit-water-year-block-maxima-indexed-by-calendar-year.md](RMC-BestFit-water-year-block-maxima-indexed-by-calendar-year.md) | wrong result | medium | Reproduced (upstream 2.0.1 model); matches the shipped example |
| With low outliers, Distribution Fitting can't reach the Exponential's maximum: the location is capped at the smallest flow, outliers included (Waimea's 1% AEP flow 15% too high) | [RMC-BestFit-fitting-location-bound-includes-low-outliers.md](RMC-BestFit-fitting-location-bound-includes-low-outliers.md) | wrong result | medium | Reproduced (upstream 2.0.1 WPF element layer and model; its own likelihood) |
| Bulletin 17C's penalized GMM fits depend on the machine: on macOS x64 one lands on a different solution (1-in-a-million flood 35% higher) and still reports Success | [RMC-BestFit-b17c-penalized-gmm-stops-at-tolerance-or-evaluation-limit.md](RMC-BestFit-b17c-penalized-gmm-stops-at-tolerance-or-evaluation-limit.md) | wrong result (machine-dependent) | medium | Reproduced (upstream 2.0.1 model on Windows and Linux; XGoodFit's reproducibility workflow on four runners) |
| Two model tests depend on wall-clock timing | [RMC-BestFit-timing-dependent-model-tests.md](RMC-BestFit-timing-dependent-model-tests.md) | test reliability | low | Read; failed on CI, not locally |
| Distribution Fitting's maximum-likelihood fits stop short of the maximum: parameters good to about 1e-4 | [RMC-BestFit-fitting-mle-stops-short-of-the-maximum.md](RMC-BestFit-fitting-mle-stops-short-of-the-maximum.md) | numerical precision | low | Reproduced (upstream 2.0.1 WPF element layer, against the closed-form MLE) |
| Closed document views stay subscribed to their elements, so they're never released | [RMC-BestFit-views-not-released-after-close-or-replace.md](RMC-BestFit-views-not-released-after-close-or-replace.md) | bug (memory leak) | low | Read |
| Properties panels' source lists: five never let go of the project when closed before loading, and eight stop following it after an unload | [RMC-BestFit-properties-panels-source-list-subscriptions.md](RMC-BestFit-properties-panels-source-list-subscriptions.md) | bug (memory leak; stale list) | low | Read; the stale list seen in XGoodFit's port |
| One click on "Nonstationary" makes two undo steps, and the first leaves a state the user never had | [RMC-BestFit-nonstationary-click-makes-two-undo-steps.md](RMC-BestFit-nonstationary-click-makes-two-undo-steps.md) | bug | low | Reproduced (upstream's element logic in XGoodFit's port) |
| Grid column header styles are rebuilt on top of themselves | [RMC-BestFit-header-styles-based-on-themselves.md](RMC-BestFit-header-styles-based-on-themselves.md) | bug (resource growth) | low | Read |
| The shell never sees the icon key of Time Series, Input Data and Fitting Analysis elements | [RMC-BestFit-element-icons-not-found-by-shell.md](RMC-BestFit-element-icons-not-found-by-shell.md) | user interface | low | Reproduced (C# rule); effect Read |
| The prior distribution editor's title is "Select Distribution for :" for stationary distributions | [RMC-BestFit-prior-editor-title-empty-for-stationary-distributions.md](RMC-BestFit-prior-editor-title-empty-for-stationary-distributions.md) | user interface | low | Read |
| The MCMC trace plot without warm-up still shows the last warm-up draw | [RMC-BestFit-trace-plot-includes-last-warm-up-draw.md](RMC-BestFit-trace-plot-includes-last-warm-up-draw.md) | user interface | low | Read |
| The Input Data and Time Series summary statistics separators are black, so they vanish in the Dark theme | [RMC-BestFit-summary-statistics-separators-black-in-dark.md](RMC-BestFit-summary-statistics-separators-black-in-dark.md) | user interface | low | Read |
| New element names suggested from the element count can already be taken | [RMC-BestFit-default-names-can-clash.md](RMC-BestFit-default-names-can-clash.md) | user interface | low | Read |
| Analysis views report their main plot as current while a diagnostic tab is showing | [RMC-BestFit-current-plot-ignores-diagnostic-tabs.md](RMC-BestFit-current-plot-ignores-diagnostic-tabs.md) | user interface | low | Read; seen in XGoodFit's port (Rating Curve, Time Series Analysis) |
| The bivariate X-Y plot's contour levels come from a racy parallel maximum | [RMC-BestFit-bivariate-contour-levels-race.md](RMC-BestFit-bivariate-contour-levels-race.md) | bug (race condition) | low | Reproduced (same loop shape) |
| The alternatives lists enable unestimated analyses, don't sort by name, and don't follow a time series' data | [RMC-BestFit-alternative-lists-bindings.md](RMC-BestFit-alternative-lists-bindings.md) | user interface | low | Read |
| Time series plots repeat axis labels for small values and short records | [RMC-BestFit-time-series-axis-labels-repeat.md](RMC-BestFit-time-series-axis-labels-repeat.md) | user interface | low | Read; corrected by upstream's own exporter |
| The DSS Pathname "…" button does nothing when the DSS file is missing, and DSS paths are stored absolute | [RMC-BestFit-dss-pathname-button-silent-when-file-missing.md](RMC-BestFit-dss-pathname-button-silent-when-file-missing.md) | user interface | low | Read (example project checked) |
| Most properties panels compute the existing names but never give them to the name box | [RMC-BestFit-name-box-doesnt-flag-duplicates.md](RMC-BestFit-name-box-doesnt-flag-duplicates.md) | user interface | low | Read |
| Frequency plots drop zero-valued uncertain and interval data on linear axes, and keep them on log axes (eight views) | [RMC-BestFit-uncertain-interval-near-zero-filter-inverted.md](RMC-BestFit-uncertain-interval-near-zero-filter-inverted.md) | user interface | low | Read |
| Bulletin 17C's penalty grids never highlight an invalid mean or MSE | [RMC-BestFit-b17c-penalty-grids-never-flag-invalid-values.md](RMC-BestFit-b17c-penalty-grids-never-flag-invalid-values.md) | user interface | low | Read |
| Bulletin 17C's frequency plot legend says "Computed" whatever the point estimator | [RMC-BestFit-b17c-legend-says-computed-for-mean-parameters.md](RMC-BestFit-b17c-legend-says-computed-for-mean-parameters.md) | user interface | low | Read |
| Bulletin 17C labels its GMM results "Posterior" and its penalties "Quantile Prior" | [RMC-BestFit-b17c-gmm-results-labelled-posterior-and-prior.md](RMC-BestFit-b17c-gmm-results-labelled-posterior-and-prior.md) | user interface | low | Read (and the examples' figure data) |
| Bulletin 17C's "Log₁₀" column header wraps onto two lines | [RMC-BestFit-b17c-log10-header-wraps.md](RMC-BestFit-b17c-log10-header-wraps.md) | user interface | low | Reproduced (WPF text layout) |
| A composite analysis's invalid-weight highlight can never show | [RMC-BestFit-composite-invalid-weight-highlight-never-shows.md](RMC-BestFit-composite-invalid-weight-highlight-never-shows.md) | user interface | low | Read |
| The Point Process view keeps showing the AMS points of an undone Total Years | [RMC-BestFit-point-process-ams-not-refreshed-after-undo.md](RMC-BestFit-point-process-ams-not-refreshed-after-undo.md) | user interface | low | Read |
| The examples' "native" figures don't show what the application shows | [RMC-BestFit-example-figures-differ-from-app.md](RMC-BestFit-example-figures-differ-from-app.md) | documentation | low | Read |
| Seven example walkthroughs describe earlier states of their shipped projects | [RMC-BestFit-walkthroughs-describe-earlier-example-states.md](RMC-BestFit-walkthroughs-describe-earlier-example-states.md) | documentation | low | Read (the shipped files); seen in XGoodFit's walkthrough tests |
| `Resources/BestFitStyles.xaml` is never used | [RMC-BestFit-unused-bestfitstyles-dictionary.md](RMC-BestFit-unused-bestfitstyles-dictionary.md) | bug (cleanup) | low | Read |
| Small fixes in RMC.BestFit.App: latent null dereferences and a stale-view reset, an unthemed progress bar, a two-way visibility binding, a dead width binding, unused code and handlers, a copied progress name and stale documentation | [RMC-BestFit-cleanup-latent-and-trivial.md](RMC-BestFit-cleanup-latent-and-trivial.md) | bug (cleanup) | low | Read |

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
| The Messenger keeps every message source alive after its messages are removed (elements and their projects stay in memory) | [WPF-Framework-messenger-keeps-sources-alive.md](WPF-Framework-messenger-keeps-sources-alive.md) | bug (memory leak) | medium | Reproduced (1.0.5 package) |
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
| Opening a project saved with both explorer layouts leaks the layout that isn't shown (also affects RMC-BestFit) | [WPF-Framework-explorer-keeps-unshown-layout-subscribed.md](WPF-Framework-explorer-keeps-unshown-layout-subscribed.md) | bug (memory leak) | low | Read; fixed and tested in XGoodFit's port |
| OxyPlotToolbar never detaches its annotation handlers, so a plot keeps every toolbar (and view) that showed it alive (also affects RMC-BestFit) | [WPF-Framework-oxyplottoolbar-annotation-handlers-keep-views-alive.md](WPF-Framework-oxyplottoolbar-annotation-handlers-keep-views-alive.md) | bug (memory leak) | low | Read; seen in XGoodFit's memory checks |
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

### Third party (not USACE or HEC)

These are in libraries XGoodFit uses that RMC-BestFit doesn't, so they don't affect upstream's application. They're written in the same format, for those projects' maintainers. Whether to report them is the project owner's decision.

| Issue | File | Type | Severity | Verified |
|---|---|---|---|---|
| Dock.Avalonia.AvalonDock 12.1.0.13 ([wieslawsoltes/Dock](https://github.com/wieslawsoltes/Dock)): a document's previous content stays the DockingManager's logical child when its `Content` changes, so closed views leak | [Dock-Avalonia-AvalonDock-replaced-content-stays-logical-child.md](Dock-Avalonia-AvalonDock-replaced-content-stays-logical-child.md) | bug (memory leak) | medium | Reproduced (the 12.1.0.13 package) |

## Pending

The reports of every porting agent that finished by 2026-10-30 are covered, including FrameworkUI's and OxyPlotControls', which arrived while this was being written. The 16 leads from the RMC.BestFit.App port's Input Data and Fitting Analysis work were added on 2026-10-31, and the 15 from its Bivariate and Time Series Data work the same day.

The 24 leads from the ports of the Univariate, Bulletin 17C, Point Process, Mixture, Composite, Coincident Frequency, Rating Curve and Time Series Analysis views, from the App tests' memory work and from the walkthrough tests were checked and written up on 2026-10-31: 16 new issues (one of them third party), additions to three existing ones and cross-links in two more, and no exclusions. How each was grouped is under "Considered and excluded".

Findings from later work can be added in the same format.

These leads are still to be checked and written up:

| Lead | Where | Why it looks like a bug |
|---|---|---|
| HEC's Dss-C test failed once on macOS arm64: two float arrays compared unequal where both values print the same (hec-dss, 7-JA-7) | `test/Dss-C/source/testExpandedTimesIrreg.c` line 100 ("Location 5": a 1,184,437-value irregular series, stored, then read back with hour granularity); the compare is `heclib/heclib_c/src/SemiPublic/zcompareDataSets.c` lines 387-396 | XGoodFit's CI (run 41173927264, 2026-10-31, job "Portable layer (macos-arm64)") logged "Floats do not match at element 1184436; First value: 1184436.000000, second value: 1184436.000000", then "Fail in testExpandedTimesIrreg, Location 5". The same commit's test passed on the run before and on Linux and Windows. Values that print the same but compare unequal suggest the compare read past the retrieved values (element 1184436 is the last), into memory that changed. Compare `hec-dss-quality-flags-vary-between-runs.md`, also memory that varies between runs. Not reproduced yet. |

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
| `GetCurrentPlot` checks the inner X-Y tab before the diagnostic tabs (Bivariate, Univariate) | RMC-BestFit | Broader than reported: seven analysis views have the same order. One issue. |
| The Time Series view's static black brush isn't frozen | RMC-BestFit | Harmless with one UI thread. Folded into the separators issue, since the same brush makes the Dark-theme separators invisible there too. |
| The HEC-DSS example stores its author's absolute DSS path | RMC-BestFit | Partly excluded: the example's own instructions tell users to browse to the shipped file again. The silent "…" button and the absolute paths are filed. |
| Time Series view's `_isLoaded` reset; the Bivariate panel's unused members, empty handler and copied progress name; the dead "Minimum" check | RMC-BestFit | Folded into the RMC.BestFit.App cleanup checklist: latent or trivial. The progress name is copied into six panels. |
| The alternatives list's `IsEnabled` binding, its sort, and `HasData` | RMC-BestFit | Combined into one issue. |
| The Y-axis `"N0"` format and the `"MMM-yyyy"` date format | RMC-BestFit | Combined into one issue. |
| `ParameterSetsControl` never unsubscribes; closing Input Data or Fitting Analysis doesn't clear `Element` | RMC-BestFit | Combined into one issue. The second lead is broader than reported: 11 of the 12 document views aren't cleared. |
| Prior grids' header styles and the summary statistics header styles | RMC-BestFit | Combined into one issue, with the three other places that use the same pattern. |
| `BatchRunWindow` blocks only the close command, not Alt+F4 | RMC-BestFit | Broader than reported: the close command isn't blocked either, because `MetroWindow`'s own binding runs first. Written up in the batch run window issue. |
| The inverted near-zero filter in the Univariate, B17C, Composite and Coincident Frequency views | RMC-BestFit | Broader than reported: the same two lines are in eight views (also Input Data, Fitting Analysis, Mixture and Point Process, whose AMS series has them too). One issue. |
| Properties panels closed or replaced before they load stay subscribed (Input Data, Bivariate); the Rating Curve panel stops following the project's time series after it unloads | RMC-BestFit | Combined into one issue: both come from subscribing in `ElementCallback` and unsubscribing only in `Unloaded`, and have one fix. Both broader than reported: five panels keep their handlers (also Univariate, B17C, Rating Curve) and eight stop following after an unload. It's a file of its own rather than part of the document views' leak issue because the cause, the shell path and the fix differ; the two issues link to each other. |
| B17C's "Log₁₀" header wraps | RMC-BestFit | Its own low issue rather than a clean-up item: it's visible, where the clean-up checklist has no visible effect. |
| The classic time series airline fit, the Orestimba POT walkthrough, the five univariate walkthroughs, and Bayes Example #1's figure and table | RMC-BestFit | Combined into one documentation issue (seven walkthroughs), cross-linked with the example-figures issue. |
| The exporter's blue section headers in the `*-current.png` panel captures | RMC-BestFit | Folded into the example-figures issue: the same merge order. |
| Rating Curve and Time Series Analysis views' current plot, confirmed by running | RMC-BestFit | Folded into the current-plot issue, whose verification now says so. |
| The Composite panel's dead width binding; Coincident Frequency's unused `BayesianOutputControl_ShowPropertyAttributes`; Rating Curve's unused `CredibleInterval_`/`OutputLength_PreviewMouseLeftButtonDown` | RMC-BestFit | Folded into the RMC.BestFit.App clean-up checklist (items 12-14): no visible effect beyond a default field width. |

## Reproductions

The throwaway programs used for the "Reproduced" entries aren't committed. Each issue shows the essential code and its output. They were run on Windows 11 x64, .NET 10.0.14, against: upstream source files compiled unchanged (dates); the `RMC.Wpf.Framework.Controls`/`Models` 1.0.5 NuGet packages (the WPF controls, the terms window and WAL); RMC-BestFit 2.0.0's shipped `Hec.Dss.dll` and 7-IU-6 `hecdss.dll`; hec-dss 7-JA-7 built from source; and RMC-BestFit's model built from `bfce2dd` (the race, and the grid row edits). WPF windows were created off-screen or laid out without being shown. For the element icons and the batch run window, small programs with the same type shapes as upstream's classes checked the C# rule for default interface members and WPF's command routing (.NET 10.0.14 and WindowsDesktop 10.0.14). The Messenger leak was reproduced against the published `RMC.Wpf.Framework.Core` 1.0.5 package, and the bivariate maximum race with a program of the same loop shape. The bivariate undo issue wasn't run: it needs the WPF element and its undo manager.

The 2026-10-31 write-up of the Pending leads used throwaway MSTest tests in XGoodFit's test projects, on the same machine and runtime. In `RMC.BestFit.UI.Tests`: the water-year indexes and the bivariate point-estimator race, against upstream's `RMC.BestFit` and Numerics 2.2.0 compiled unchanged; the Nonstationary and composite undo, against XGoodFit's port of upstream's elements, whose only changes are the UI types, with upstream's undo manager logic. In `FrameworkUI.Tests`: the Dock issue, against the published `Dock.Avalonia.AvalonDock` 12.1.0.13 package under Avalonia.Headless 12.1.4. The Log₁₀ header was measured by a small WPF program (WindowsDesktop 10.0.14). The example projects were read with SQLite in read-only mode.
