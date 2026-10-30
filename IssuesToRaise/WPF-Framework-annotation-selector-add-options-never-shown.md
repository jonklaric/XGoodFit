# The annotation selector never shows its "Add … Annotation" options: the lookup can't see into the popup

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/OxyPlotControls/Annotations/AnnotationSelectorControl.xaml.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low

**Verification:** verified by reading the source.

## Summary

When a plot is assigned, the control looks for the `ItemsControl` named `SpecialOptions` in the combo box's template, to add eight "Add Arrow/Text/Line/Rectangle/Ellipse/Point/Polygon/Polyline Annotation" items. It searches with `VisualTreeHelper`, but `SpecialOptions` is inside the template's `Popup`. A `Popup`'s child isn't a visual child of the popup (it lives in a separate popup root), so the search returns `null` and the items are never added.

## Details and how to reproduce

Open a plot's properties, go to Annotations, and open the annotation drop-down. **Expected:** the existing annotations, a separator and the eight "Add … Annotation" entries. **Actual:** only the existing annotations. Annotations can still be added with the plot toolbar's annotation tool.

## Why this is an issue and not intended behaviour

About 170 lines build those eight items and their handlers, so they are meant to appear. The template reserves the `SpecialOptions` host in the drop-down (`Grid.Row="1"` under the list).

## Where in the source

- [`AnnotationSelectorControl.xaml.cs` lines 108-128](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Annotations/AnnotationSelectorControl.xaml.cs#L108-L128)
  ```csharp
  thisControl.AnnotationPropertyControlComboBox.ApplyTemplate();
  var cntrl = FindElementByName<ItemsControl>(thisControl.AnnotationPropertyControlComboBox, "SpecialOptions");
  if (cntrl != null && cntrl.Items.Count == 0) { /* add the eight items */ }
  ```
- The search: [lines 395-412](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Annotations/AnnotationSelectorControl.xaml.cs#L395-L412) (`VisualTreeHelper.GetChildrenCount` and `GetChild`).
- `SpecialOptions` inside the `Popup`: [`src/OxyPlotControls/Themes/OxyPlotControlsTheme.xaml` lines 118-136](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/OxyPlotControls/Themes/OxyPlotControlsTheme.xaml#L118-L136) (also in `Resources/ResourceDictionary.xaml` line 157).

## Suggested fix

Named template parts are found through the template's name scope, popup included:

```csharp
var combo = thisControl.AnnotationPropertyControlComboBox;
combo.ApplyTemplate();
var cntrl = combo.Template?.FindName("SpecialOptions", combo) as ItemsControl;
```

No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the visual-tree lookup in its OxyPlotControls port (branch `t3/port-oxyplot-controls`, not yet on `main`), so the options are missing there as well.
