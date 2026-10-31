# One click on "Nonstationary" makes two undo steps, and the first leaves a state the user never had

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs` (`IsNonstationary`) and `src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs` (`RecordDistributionUndo`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** bug · **Severity:** low

**Verification:** reproduced. A throwaway MSTest test created a `UnivariateAnalysis` element with 30 years of input data, set `UnivariateDistribution.IsNonstationary = true` once (what the panel's check box does through its two-way binding), and undid it. It ran in XGoodFit's port of the element, whose only changes from upstream's file are the UI types (Avalonia's brushes and images for WPF's); the model library and the undo manager's logic are upstream's, compiled unchanged.

## Summary

Turning **Nonstationary** on also sets `ParameterTimeIndex` to the middle of the data's years. The element records an undo step for each property notification whose snapshot differs from the last one. The setter raises `IsNonstationary` first and changes `ParameterTimeIndex` after, so one click records two steps. The first Undo restores only the time index: the analysis stays nonstationary, with a time index of 0. A second Undo is needed to get back to where the user was.

## Details and how to reproduce

In the application: open a univariate analysis with input data, tick **Nonstationary** in **Properties > Options**, then press Undo once. Expected: Nonstationary is off again. Actual: it's still ticked, and the parameter time index shows 0.

The test's output:

```
before: IsNonstationary=False, ParameterTimeIndex=0, undo stack=0 []
after one IsNonstationary = true: IsNonstationary=True, ParameterTimeIndex=2005, undo stack=2 [Change ParameterTimeIndex | Change IsNonstationary]
after Undo #1: IsNonstationary=True, ParameterTimeIndex=0, undo stack=1 [Change IsNonstationary]
after Undo #2: IsNonstationary=False, ParameterTimeIndex=0, undo stack=0 []
```

The intermediate state is one a user can act on: estimating it fits a nonstationary model and reports the frequency curve at year 0, far outside the data.

## Why this is an issue and not intended behaviour

`RecordDistributionUndo`'s documentation says it deduplicates "cascaded PropertyChanged events from a single user action … only the first event that detects a diff records the action; subsequent events find no diff and are no-ops". That holds only if the first notification comes after the whole cascade. Here the setter raises `IsNonstationary` before it sets `ParameterTimeIndex` (and the trend models' start index), so the later notification does find a diff. `DelegateAction` doesn't merge with the step before it, so the two stay separate.

## Where in the source

- The setter, which notifies and then changes more state: [`src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs` lines 400-443](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/UnivariateDistribution/UnivariateDistribution.cs#L400-L443):
  ```csharp
  _isNonstationary = value;
  RaisePropertyChange(nameof(IsNonstationary));
  ...
  ParameterTimeIndex = (int)Math.Ceiling(
      (DataFrame.ExactSeries.MinimumIndex() + DataFrame.ExactSeries.MaximumIndex()) / 2.0);   // lines 427-428
  ```
- The recording, one step per notification with a new snapshot: [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs` lines 1638-1656](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs#L1638-L1656), called from `InnerAnalysis_PropertyChanged` ([lines 566-569](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs#L566-L569)) for the names in `DistributionUndoProperties`, which include both `IsNonstationary` and `ParameterTimeIndex` ([lines 334-340](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs#L334-L340)). The documentation quoted above is at [lines 1626-1628](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs#L1626-L1628).
- No merging: WPF-Framework's [`DelegateAction.CanMergeWith`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Undo/Actions/DelegateAction.cs#L161-L165) returns false, and [`UndoManager.RecordActionInternal`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Undo/UndoManager.cs#L435-L456) pushes each action separately outside a transaction.

## Suggested fix

Any of:

1. In the setter, make every change before the first notification: assign `_isNonstationary`, `_parameterTimeIndex` and the trend models' start index (calling `SetParameterValues` as the `ParameterTimeIndex` setter does), then raise `IsNonstationary` and `ParameterTimeIndex`. The first notification's snapshot then holds the whole change, the second finds no diff, and the one step is named "Change IsNonstationary".
2. Or, in the element, coalesce a cascade: on the first distribution notification, post one `RecordDistributionUndo` to the dispatcher and ignore further notifications until it runs. The notifications are raised one after another within the setter, not nested, so the record has to wait until the setter returns.

Option 2 fixes every cascading setter at once. The same check is worth doing for the other cascading setters in `DistributionUndoProperties`, such as `UseDefaultFlatPriors`, which calls `SetDefaultParameters()` after its own notification ([`ModelBase.cs` lines 67-79](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit/Models/Support/ModelBase.cs#L67-L79)). No file-format risk.

## How XGoodFit handles it

XGoodFit compiles upstream's model unchanged ([`src/BestFit/RMC.BestFit.UI/RMC.BestFit.UI.csproj` line 35](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.UI/RMC.BestFit.UI.csproj#L35)) and keeps the element's undo code as upstream ([`src/BestFit/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs` line 1642](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/BestFit/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateAnalysis.cs#L1642)), so it has the same behaviour for now.
