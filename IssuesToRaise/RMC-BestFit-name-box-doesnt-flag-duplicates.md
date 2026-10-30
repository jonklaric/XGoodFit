# Most properties panels compute the existing names but never give them to the name box

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.App/GUI` (the properties panels of the 10 analysis types). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; not run.

## Summary

WPF-Framework's `NameTextPropertyControl` flags a name that's already taken, using its `ExistingNames` property. The Input Data and Time Series properties panels bind it. The other 10 panels with a name box don't: 9 of them even compute `ExistingNames` when the name box gets focus, but nothing passes it on, and the Coincident Frequency panel doesn't compute it. Typing a duplicate name there isn't flagged in the box. The element's own validation still reports it in the message window.

## Details and how to reproduce

1. Create two bivariate analyses, "A" and "B".
2. In B's properties, rename it to "A".
3. Expected: the name box marks the name as taken, as it does for Input Data and Time Series. Actual, by reading: the box accepts it, and the message window then reports "A … with the name 'A' already exists and must be unique."

## Why this is an issue and not intended behaviour

The panels go to the trouble of computing the list, and two panels show the intended result. The difference between panels looks like an omission.

## Where in the source

- The Bivariate panel: the name box at [`src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.xaml` lines 63-66](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.xaml#L63-L66) has no `ExistingNames`; the list is computed in `Name_GotFocus` ([`BivariateAnalysisPropertiesControl.xaml.cs` line 336](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.xaml.cs#L336)):
  ```csharp
  ExistingNames = Element.ParentCollection.GetElementNames(Element).ToArray();
  ```
- The binding as it should be, in the Input Data panel: [`src/RMC.BestFit.App/GUI/InputData/InputDataPropertiesControl.xaml` line 47](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/InputData/InputDataPropertiesControl.xaml#L47):
  ```xml
  ExistingNames="{Binding ExistingNames, UpdateSourceTrigger=PropertyChanged, Mode=OneWay}"
  ```
- The other panels that compute but don't bind it: Fitting Analysis, Univariate, Bulletin 17C, Composite, Mixture, Point Process, Rating Curve and Time Series Analysis (`*PropertiesControl.xaml.cs`, `Name_GotFocus`). The Coincident Frequency panel has a name box and no `ExistingNames` at all.
- How the box uses it: [WPF-Framework `src/GenericControls/Properties Controls/NameTextPropertyControl.xaml` line 37](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/GenericControls/Properties%20Controls/NameTextPropertyControl.xaml#L37) (`InvalidStrings="{Binding ExistingNames, ElementName=NameTextPropertyEditor, …}"`).
- The element's own check: [WPF-Framework `src/FrameworkInterfaces/Project/ElementBase.cs` lines 531-551](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Project/ElementBase.cs#L531-L551).

## Suggested fix

Add the same binding to the name box in the 10 panels, and add the `ExistingNames` property and its `Name_GotFocus` computation to the Coincident Frequency panel. No compatibility risk.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps upstream's panels as they are for now, and notes it: [`src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.axaml` lines 14-15](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/BestFit/RMC.BestFit.App/GUI/BivariateAnalysis/Bivariate/BivariateAnalysisPropertiesControl.axaml#L14-L15) ("the name's ExistingNames isn't bound").
