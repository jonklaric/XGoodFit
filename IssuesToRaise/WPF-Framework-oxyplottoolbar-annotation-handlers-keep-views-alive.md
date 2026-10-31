# OxyPlotToolbar never detaches its annotation handlers, so a plot keeps every toolbar that showed it alive

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/OxyPlotControls/OxyPlotToolbar.xaml.cs` (`InitializePlot`, `AttachAnnotationHandlers`, `PlotModelAnnotationCollectionChanged`). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-31. Also affects RMC-BestFit, whose plots belong to its elements.

**Type:** bug (memory leak) · **Severity:** low

**Verification:** verified by reading the source. Seen in XGoodFit's port, which keeps the toolbar's code: its walkthrough tests' memory checks found closed views kept alive through these handlers, and the tests now take them off by reflection before checking ([`WalkthroughTesting.cs` lines 224-226 and 276-300](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/WalkthroughTesting.cs#L276-L300)).

## Summary

When the toolbar's `Plot` is set, it wires each of the plot's annotations for dragging: it adds lambdas to the annotation's internal `MouseDown`, `MouseMove` and `MouseUp` events, and does the same for annotations added later. The lambdas capture the toolbar. When the toolbar's `Plot` changes or the toolbar unloads, it removes its handlers from the plot model and the annotation collection, but not from the annotations. So each annotation holds every toolbar that ever showed its plot, and each toolbar holds its parent view.

In an application where the plot outlives the view, as in RMC-BestFit, whose plots belong to the elements and are kept by each element's `PlotUndoManager`, every view that showed an annotated plot stays in memory, with its grids and plots, until the element goes. Many of RMC-BestFit's default plots have annotations: the ACF and PACF plots' confidence lines, the Input Data view's threshold diagnostics, and lines on the Time Series, Rating Curve and Time Series Analysis plots.

## Details and how to reproduce

1. In RMC-BestFit, open a time series element (its ACF plot has two line annotations), close it, and repeat a few times.
2. Take a `WeakReference` to a closed view's `OxyPlotToolbar`, force a collection, and check it. Expected: collected once the view is closed (and the view released, see below). Actual, by reading: alive, reachable from the element's plot → annotation → `InternalAnnotation.MouseDown` → the lambda's closure → the toolbar → its parent chain → the view.

RMC-BestFit also keeps closed views subscribed to their elements ([filed separately](RMC-BestFit-views-not-released-after-close-or-replace.md)), so fixing that alone doesn't release them: these handlers keep them too.

## Why this is an issue and not intended behaviour

- The `Unloaded` handler's documentation says "Plot-side event subscriptions are managed by `InitializePlot` (the Plot dependency-property changed callback), so they are detached automatically when the Plot binding is cleared." The annotation handlers are plot-side subscriptions too, and aren't.
- The toolbar already avoids keeping annotations alive itself, tracking the wired annotations in a `ConditionalWeakTable` ("Tracks toolbar mouse-handler wiring without keeping removed annotations alive"). The reverse reference, annotation to toolbar, was missed.

## Where in the source

- `InitializePlot`: [`src/OxyPlotControls/OxyPlotToolbar.xaml.cs` lines 189-269](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L189-L269). The old plot's branch removes the model's mouse handlers, the collection handler and the layout handler (lines 197-212); the new plot's branch wires every existing annotation (lines 225-228):
  ```csharp
  newPlot.Annotations.CollectionChanged += oxyToolBar.PlotModelAnnotationCollectionChanged;
  foreach (var annotation in newPlot.Annotations)
  {
      oxyToolBar.AttachAnnotationHandlers(annotation);
  }
  ```
- `AttachAnnotationHandlers`, [lines 840-845](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L840-L845), and `PlotModelAnnotationCollectionChanged`, [lines 862-1415](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L862-L1415), which adds the lambdas for each annotation type, from [line 876](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L876) (`newArrow.InternalAnnotation.MouseDown += (s, ae) => { … }`) to [line 1406](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L1406). Nothing in the file removes them.
- The `Unloaded` handler and its documentation: [lines 128-138](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L128-L138); the weak table, [lines 406-409](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/OxyPlotToolbar.xaml.cs#L406-L409).
- In RMC-BestFit, for example the ACF plot's annotations: [`src/RMC.BestFit.App/GUI/Support/Controls/AutocorrelationControl.xaml.cs` lines 304-327 and 377](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/GUI/Support/Controls/AutocorrelationControl.xaml.cs#L304-L327), on a plot the element owns.

## Suggested fix

Keep the handlers the toolbar adds, and remove them when it lets go of the plot:

```csharp
// one record per wired annotation
private readonly List<(OxyPlot.Annotations.Annotation Target, EventHandler<OxyMouseDownEventArgs> Down,
    EventHandler<OxyMouseEventArgs> Move, EventHandler<OxyMouseEventArgs> Up)> _annotationHandlers = new();

private void DetachAnnotationHandlers()
{
    foreach (var h in _annotationHandlers)
    {
        h.Target.MouseDown -= h.Down;
        h.Target.MouseMove -= h.Move;
        h.Target.MouseUp -= h.Up;
    }
    _annotationHandlers.Clear();
    _annotationsWithMouseHandlers.Clear();
}
```

Call it in `InitializePlot`'s old-plot branch, in `Unloaded` (re-wiring in `Loaded` if the plot is still set), and for annotations removed from the collection (`e.OldItems`, and `Reset`). Or route the three events through one weak handler per annotation. No compatibility risk: dragging works the same on the current plot.

## How XGoodFit handles it

XGoodFit's Avalonia port keeps the toolbar's code for now ([`src/Framework/OxyPlotControls/OxyPlotToolbar.axaml.cs` lines 902 and 924](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/Framework/OxyPlotControls/OxyPlotToolbar.axaml.cs#L902-L924)), so the application has the leak. Its walkthrough tests remove the handlers of toolbars no longer in a window, by reflection, before their memory checks ([`tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/WalkthroughTesting.cs` lines 276-300](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/BestFit/RMC.BestFit.App.Tests/XGoodFit/WalkthroughTesting.cs#L276-L300)).
