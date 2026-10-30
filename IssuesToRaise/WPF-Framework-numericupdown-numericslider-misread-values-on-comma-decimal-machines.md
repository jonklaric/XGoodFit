# NumericUpDown and NumericSlider corrupt their value on comma-decimal machines when the host doesn't set FrameworkElement.Language

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/GenericControls/General Controls/NumericUpDown.xaml(.cs)` and `NumericSlider.xaml(.cs)`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30. **Affects** [RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) 2.0.1, which uses both controls and doesn't set `FrameworkElement.Language`.

**Type:** wrong result · **Severity:** high

**Verification:** reproduced in WPF with the published `RMC.Wpf.Framework.Controls` 1.0.5 package. It was built from `05acb1e`, which differs from `7f897e1` only in documentation. The controls were created in code and laid out without a window, with `Thread.CurrentCulture` set; this is what an app without a `Language` override gets.

## Summary

Both controls format their text in the machine's culture (`NumberFormatHelper.FormatDouble`, `CultureInfo.CurrentCulture`), but the text box's two-way binding back to `Value` has no `ConverterCulture`. So WPF converts it in the element's `Language`, which is en-US unless the host app overrides it. On a machine whose decimal separator is a comma, the formatted text is read back as a different number, and the control writes that into `Value`. With `DecimalPlaces = 4`, 23 becomes 230,000, is shown clamped to the maximum, and that text is misread again, ending at 1,000,000.

## Details and how to reproduce

```csharp
// net10.0-windows WPF app, PackageReference RMC.Wpf.Framework.Controls 1.0.5, no LanguageProperty override.
Thread.CurrentThread.CurrentCulture = CultureInfo.GetCultureInfo("de-DE");
var slider = new GenericControls.NumericSlider { DecimalPlaces = 4 };   // as RMC-BestFit's split-index slider
slider.Minimum = 0; slider.Maximum = 100;
slider.Value = 23;
// let the dispatcher run (Dispatcher.Invoke(() => { }, DispatcherPriority.ApplicationIdle))
// Expected: slider.Value == 23, text "23,0000"
// Actual:   slider.Value == 1000000, text "1000000"
```

Results:

| Control and settings | Action | en-US | de-DE | fr-FR |
|---|---|---|---|---|
| NumericSlider, `DecimalPlaces=4`, 0-100 | `Value = 23` | 23 | **1,000,000** | **1,000,000** |
| NumericSlider, same | user types `12,5` (`12.5` in en-US) | 12.5 | **1,000,000** | **1,000,000** |
| NumericUpDown, `DecimalPlaces=2`, 0-10 | `Value = 1` | 1 | **1,000** | **1,000** |
| NumericUpDown, `DecimalPlaces=0`, `ThousandsSeparator=True`, 1-1,000,000 | `Value = 1500` | 1,500 | **2** | 1,500 |

How it happens under de-DE: `Value = 23` makes `SetText` write `"23,0000"` into `NumericTextBox.Text`. The two-way binding converts that with en-US rules, where `,` is a group separator, giving 230000. That's above the maximum, so `SetText` shows the maximum, `"100,0000"`, which is read back as 1000000. With `ThousandsSeparator`, `"1.500"` is read as 1.5, so the control shows `"2"`, which becomes 2. Under fr-FR the narrow no-break space group separator doesn't parse in en-US, so that case keeps 1,500.

Effect in RMC-BestFit 2.0.1, which never sets `FrameworkElement.Language`:

- Input data, hypothesis tests: the "Split Index" `NumericSlider` (`DecimalPlaces="4"`, [`InputDataControl.xaml` line 322](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml#L322)) gets its minimum, maximum and value from data indices ([`InputDataControl.xaml.cs` lines 741-743](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L741-L743)). It then passes `(int)IndexSlider.Value` to `SummaryHypothesisTest` ([line 3636](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L3636)). On a comma-decimal machine the index becomes 10,000 times the maximum, so the two-sample hypothesis tests are computed for a split far outside the data.
- Time-series analysis: "Training time steps" is a `NumericUpDown` with `ThousandsSeparator="True"`, 1 to 1,000,000, two-way bound to the model ([`TimeSeriesAnalysisPropertiesControl.xaml` lines 335-336](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/TimeSeriesAnalysis/TimeSeriesAnalysisPropertiesControl.xaml#L335-L336)). Any value of 1,000 or more is divided by 1,000 on de-DE, it-IT, es-ES, nl-NL and other dot-grouping machines.

## Why this is an issue and not intended behaviour

- WPF-Framework already decided how this should work. [`tests/GenericControls.Tests/XamlBindingCultureTests.cs` lines 6-21](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/GenericControls.Tests/XamlBindingCultureTests.cs#L6-L21) says: "Phase 3 added `ConverterCulture={x:Static glob:CultureInfo.CurrentCulture}` to numeric bindings in shared framework controls so they parse user input under the user's locale even in host apps that don't call `FrameworkElement.LanguageProperty.OverrideMetadata`." Its list of guarded files ([lines 29-40](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/GenericControls.Tests/XamlBindingCultureTests.cs#L29-L40)) includes `NumericTextBox2.xaml` and the numeric property controls, but not `NumericUpDown.xaml` or `NumericSlider.xaml`. They were missed.
- [`tests/CULTURE_SMOKE_TESTS.md`, Test 5](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/CULTURE_SMOKE_TESTS.md#L69-L77) expects typing `1,5` in a `NumericUpDown` under German settings to give 1.5.
- Every WPF-Framework demo app overrides `FrameworkElement.LanguageProperty` with the current culture (for example [`src/GenericControls.Demo/App.xaml.cs` lines 31-40](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls.Demo/App.xaml.cs#L31-L40)). So the smoke tests pass in the demos, and the bug only shows in a host without the override, such as RMC-BestFit.
- The control's own validation already parses with the current culture (`NumberFormatHelper.TryParseDouble`), so the control disagrees with itself.

## Where in the source

- The bindings, without `ConverterCulture`:
  - [`src/GenericControls/General Controls/NumericUpDown.xaml` lines 60-62](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml#L60-L62)
  - [`NumericSlider.xaml` line 17](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericSlider.xaml#L17)
  ```xml
  <local:NumericTextBox x:Name="NumericTextBox" Grid.Column="0"
                        Text="{Binding ElementName=NumericUpDownControl, Path=Value, Mode=TwoWay}" .../>
  ```
- The formatting, in the current culture:
  - [`NumericUpDown.xaml.cs` lines 83-104](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericUpDown.xaml.cs#L83-L104)
  - [`NumericSlider.xaml.cs` lines 76-98](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/General%20Controls/NumericSlider.xaml.cs#L76-L98)
  ```csharp
  thisControl.NumericTextBox.Text = NumberFormatHelper.FormatDouble(valueToFormat, thisControl.FormatString);
  ```
  - `FormatDouble` uses `CultureInfo.CurrentCulture`: [`src/GenericControls/Utilities/NumberFormatHelper.cs` lines 167-170](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Utilities/NumberFormatHelper.cs#L167-L170).

## Suggested fix

Do what Phase 3 did for the other numeric controls:

```xml
<UserControl ... xmlns:glob="clr-namespace:System.Globalization;assembly=mscorlib">
  <local:NumericTextBox x:Name="NumericTextBox"
      Text="{Binding ElementName=NumericUpDownControl, Path=Value, Mode=TwoWay,
                     ConverterCulture={x:Static glob:CultureInfo.CurrentCulture}}" .../>
```

Do the same in `NumericSlider.xaml`, and add both files to `XamlBindingCultureTests.XamlFilesWithCultureBinding`.

A more robust alternative: don't round-trip the formatted text through the binding at all. Let `SetText` set the text while a flag suppresses the target-to-source update, and parse user input explicitly with `NumberFormatHelper.TryParseDouble`. That also fixes the formatting being replaced described in [WPF-Framework-numericupdown-formatting-replaced-by-binding-text.md](WPF-Framework-numericupdown-formatting-replaced-by-binding-text.md).

For RMC-BestFit, a stopgap is the `LanguageProperty` override every WPF-Framework demo has. It changes how every bound field in BestFit is displayed, though, so it's a product decision ([RMC-BestFit-mixes-machine-culture-and-en-us.md](RMC-BestFit-mixes-machine-culture-and-en-us.md)).

No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) reproduces WPF's binding culture (en-US, `WpfCompat.FormattingCulture`) and keeps this behaviour in its ports of [`NumericUpDown.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/General%20Controls/NumericUpDown.axaml) and [`NumericSlider.axaml`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Framework/GenericControls/General%20Controls/NumericSlider.axaml). It's listed as an upstream bug to report in [`docs/migration-plan.md`, Phase 3](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L533). The plan's later work on the OS locale (decision D8) would fix it in both projects.
