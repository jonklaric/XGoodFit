# Changing a composite's component can't be undone, and a later undo silently reverts it

**Target:** [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), `src/RMC.BestFit.UI/Elements/UnivariateAnalysis/CompositeAnalysis.cs` (`WeightedAnalysis_PropertyChanged`, `RecordAnalysesUndo`). Found in 2.0.1 at commit [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269), still `main` on 2026-10-31.

**Type:** data loss · **Severity:** medium

**Verification:** reproduced. A throwaway MSTest test built a Mixture composite with two components, "Comp A" and "Comp B", weighted 0.5 each, in a collection with a third analysis, "Comp C". It switched row 0 to Comp C (what the row's combo box does), edited row 1's weight, and undid once. It ran in XGoodFit's port of the element, whose only changes from upstream's file are the UI types (Avalonia's brushes and images for WPF's); `WeightedUnivariateAnalysis` and the undo manager are upstream's, compiled unchanged. XGoodFit's port of the view also showed it on the Mixture of Flood Types example: switch a row's analysis, edit a weight, undo, and the original analysis comes back.

## Summary

The composite element records undo steps by comparing a snapshot of its component rows (names and weights) with the last one. A weight edit and a row added or removed record a step and refresh the snapshot. Changing which analysis a row uses doesn't: it clears the results and marks the element changed, but records nothing and leaves the snapshot stale. So:

- Undo can't take back a component change; the undo stack is empty after it.
- The next recorded edit, for example a weight, compares against the stale snapshot. Its undo step restores the snapshot from before the component change, so undoing the weight edit also puts the old component back, without saying so.

## Details and how to reproduce

The test's output:

```
start: rows [Comp A 0.5, Comp B 0.5], undo stack=0 []
after switching row 0 to Comp C: rows [Comp C 0.5, Comp B 0.5], undo stack=0 []
after editing row 1's weight to 0.4: rows [Comp C 0.5, Comp B 0.4], undo stack=1 [Change Weight]
after one Undo: rows [Comp A 0.5, Comp B 0.5], undo stack=0 []
```

In the application: open "Mixture of Flood Types" in `examples/4-univariate-distribution-analysis/5-composite-analysis/mixed-population-examples.bestfit`, choose another analysis in a row's combo box, change another row's weight, and press Undo. Expected: only the weight goes back. Actual: the weight and the first row's analysis both go back. The component change also clears the composite's results, which the user then re-estimates against a component they didn't choose if they don't notice the row.

## Why this is an issue and not intended behaviour

- The other two ways of editing the rows, adding or removing one and editing a weight, each record a step through `RecordAnalysesUndo`, and its documentation says it records "the difference between the current Analyses collection … and the rolling snapshot baseline". A component change is part of that collection's serialised state (`WeightedUnivariateAnalysis.ToXElement` writes the analysis's name), so it should be recorded the same way.
- An undo step that changes something the user didn't do in that step loses work silently: the grid shows the reverted row, but nothing says it changed.

## Where in the source

- The component branch, which records nothing: [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/CompositeAnalysis.cs` lines 1503-1510](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/CompositeAnalysis.cs#L1503-L1510):
  ```csharp
  if (e.PropertyName == nameof(WeightedUnivariateAnalysis.UnivariateAnalysis))
  {
      SyncAndEstimateModelWeights();
      if (!UndoManager.IsExecutingAction) ClearResults();
      ValidateFunctions();
      RaisePropertyChange(nameof(Analyses));
      return;
  }
  ```
- The weight branch, which does record: [lines 1523-1536](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/CompositeAnalysis.cs#L1523-L1536) (`RecordAnalysesUndo("Weight")` at line 1531); the collection change, [line 1435](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/CompositeAnalysis.cs#L1435).
- The snapshot is refreshed only when a step is recorded: [lines 1734-1753](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/CompositeAnalysis.cs#L1734-L1753):
  ```csharp
  var oldSnapshot = _analysesSnapshot;
  var action = new DelegateAction($"Change {propertyName}",
      () => RestoreAnalysesFromSnapshot(currentSnapshot),
      () => RestoreAnalysesFromSnapshot(oldSnapshot), this);
  ...
  _analysesSnapshot = currentSnapshot;
  ```
- What the snapshot holds: [`src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateSupport/WeightedUnivariateAnalysis.cs` lines 107-114](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/UnivariateAnalysis/UnivariateSupport/WeightedUnivariateAnalysis.cs#L107-L114).

## Suggested fix

Record the change in the component branch, before returning:

```csharp
if (e.PropertyName == nameof(WeightedUnivariateAnalysis.UnivariateAnalysis))
{
    RecordAnalysesUndo("UnivariateAnalysis");
    SyncAndEstimateModelWeights();
    ...
}
```

`RecordAnalysesUndo` already returns while an undo or redo runs, so restoring a snapshot doesn't record a new step. A regression test: switch a row's component, check one step is recorded, undo it, check the original component is back; then switch, edit a weight, undo once, and check the component stays switched. No file-format risk.

## How XGoodFit handles it

XGoodFit fixes this in its port: the project owner decided to fix upstream bugs that lose users' work and to record each change in [`docs/differences-from-rmc-bestfit.md`](../docs/differences-from-rmc-bestfit.md).
