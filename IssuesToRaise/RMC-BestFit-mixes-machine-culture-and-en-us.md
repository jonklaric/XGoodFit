# On non-US machines RMC-BestFit mixes two cultures: bound fields use en-US, charts and framework controls use the machine's

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/App.xaml.cs` (application start-up). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-30. Related WPF-Framework issues are linked below.

**Type:** user interface · **Severity:** medium (some of the consequences linked below are wrong results)

**Verification:** verified by reading the source. The chart and bound-field mismatch was observed on an en-AU Windows machine. The value-corrupting consequences were reproduced in WPF (see the linked issues).

## Summary

RMC-BestFit doesn't override `FrameworkElement.LanguageProperty`, so every WPF binding formats and parses in en-US. Much of what it shows, though, goes through code that uses `CultureInfo.CurrentCulture`: OxyPlot's axis labels, WPF-Framework's `NumberFormatHelper`, `NumericUpDown`/`NumericSlider` formatting, the time-series table's date column, and converters that parse user input. On a machine with any other regional format, one view mixes two cultures. Where one side formats and the other parses, values change.

## Details and how to reproduce

On Windows with regional format English (Australia), open an example with a time series: the chart's date axis shows `1/01/2011` while the bound fields show `1/1/2011`. With German (Germany) the differences become wrong values:

- the input data's split-index slider and the time-series analysis's "Training time steps" read back their own formatted text as a different number ([WPF-Framework-numericupdown-numericslider-misread-values-on-comma-decimal-machines.md](WPF-Framework-numericupdown-numericslider-misread-values-on-comma-decimal-machines.md));
- the time-series editor shows `1234.5` but reads an edit to `1234.6` as 12346 ([WPF-Framework-timeseriestable-values-shown-en-us-but-parsed-in-machine-culture.md](WPF-Framework-timeseriestable-values-shown-en-us-but-parsed-in-machine-culture.md));
- the input data's hypothesis-test p-values are formatted by code-behind with the machine's culture, giving `0,5000` ([`InputDataControl.xaml.cs` lines 3636-3645](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataControl.xaml.cs#L3636-L3645)), while result columns bound with `StringFormat` elsewhere in the app show `0.5000`.

## Why this is an issue and not intended behaviour

- Every demo application in WPF-Framework sets WPF's binding language to the current culture, for example [`src/GenericControls.Demo/App.xaml.cs` lines 31-40](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls.Demo/App.xaml.cs#L31-L40) ("Set WPF to use the current culture for all bindings (international number support)"). WPF-Framework's controls are developed and smoke-tested ([`tests/CULTURE_SMOKE_TESTS.md`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/CULTURE_SMOKE_TESTS.md)) with that override in place. RMC-BestFit's `App` doesn't have it ([`App.xaml.cs` lines 57-89](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/App.xaml.cs#L57-L89)).
- WPF-Framework did add `ConverterCulture=CurrentCulture` to some numeric bindings "so they parse user input under the user's locale even in host apps that don't call `FrameworkElement.LanguageProperty.OverrideMetadata`" ([`XamlBindingCultureTests.cs` lines 6-21](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/tests/GenericControls.Tests/XamlBindingCultureTests.cs#L6-L21)). It missed some controls, and RMC-BestFit's own XAML (hundreds of `StringFormat` bindings) still uses en-US.
- Users in most of the world see numbers and dates in two formats in one window, and in places get wrong values.

## Where in the source

- [`src/RMC.BestFit.App/App.xaml.cs` lines 57-89](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/App.xaml.cs#L57-L89): start-up sets the theme and creates the main window; there's no `LanguageProperty.OverrideMetadata`.

## Suggested fix

Choose one culture for the whole UI:

1. **The machine's culture, as WPF-Framework's demos do.** Add to `App`'s constructor:
   ```csharp
   FrameworkElement.LanguageProperty.OverrideMetadata(typeof(FrameworkElement),
       new FrameworkPropertyMetadata(XmlLanguage.GetLanguage(CultureInfo.CurrentCulture.IetfLanguageTag)));
   ```
   Bound fields then match the charts and the framework's code. Check that nothing persisted goes through a binding's text: files already use invariant formats, except project and element dates ([WPF-Framework-dates-depend-on-current-culture.md](WPF-Framework-dates-depend-on-current-culture.md)).
2. **en-US everywhere.** Set `CultureInfo.DefaultThreadCurrentCulture`/`CurrentCulture` to en-US at start-up. This is simpler and consistent, but ignores the user's regional settings.

Either way, fixing the linked WPF-Framework issues makes the controls safe in any host.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) reproduces WPF's behaviour for now: en-US in bound fields through `WpfCompat.FormattingCulture`, with the machine culture elsewhere. The choice of one culture is an open decision (D8) in [`docs/migration-plan.md`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L427) ("Chart axes follow the OS culture in the WPF app … while bound fields use en-US").
