# Project Explorer: an element node selected with the keyboard isn't highlighted while the explorer has focus

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Themes/VS2013/Controls/ProjectExplorer.xaml` (`ElementNodeStyle`) and `src/FrameworkUI/Project Explorer/View Models/ExplorerTreeView.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** user interface · **Severity:** low

**Verification:** verified by reading the source; also seen in XGoodFit's port of the explorer.

## Summary

`ElementNodeStyle` highlights a node only through `IsNodeSelected`, or through `IsSelected` combined with `IsSelectionActive = False` (focus elsewhere). It has no trigger for `IsSelected` alone. `IsNodeSelected` is set only by the explorer's mouse handlers. So when the selection moves with the arrow keys, or is set from code, the newly selected element node has no highlight while the explorer has focus, and a node clicked earlier keeps its highlight.

## Details and how to reproduce

In RMC-BestFit's Project Explorer, click an element (it's highlighted), then press the Down arrow. **Expected:** the highlight moves to the next element. **Actual:** the clicked element stays highlighted and the selected one isn't, until focus leaves the explorer (then the `IsSelectionActive = False` trigger shows it).

## Why this is an issue and not intended behaviour

The other node style in the same file, `TreeViewItemStyle` (project and collection nodes), has a plain `IsSelected = true` trigger ([line 136](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Themes/VS2013/Controls/ProjectExplorer.xaml#L136)), and `ElementNodeStyle` already handles the unfocused case. Only the focused case for keyboard or code selection is missing.

## Where in the source

- [`ProjectExplorer.xaml` lines 199-222](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Themes/VS2013/Controls/ProjectExplorer.xaml#L199-L222): the `IsNodeSelected` triggers and the `IsSelected` + `IsSelectionActive=False` multi-trigger.
- `IsNodeSelected` is set only in the mouse handlers: [`ExplorerTreeView.cs` lines 123-205](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ExplorerTreeView.cs#L123-L205) (`Me_PreviewMouseLeftButtonDown`) and [`Node.cs` lines 627-651](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/Node.cs#L627-L651) (right button).

## Suggested fix

Keep `IsNodeSelected` in step with the tree's selection when it changes for any reason other than a multi-select click. For example, in `OnSelectedItemChanged`, when no modifier key is pressed, clear the multi-selection and mark the new item. Alternatively, add `<Trigger Property="IsSelected" Value="True">` with the highlight to `ElementNodeStyle`, as `TreeViewItemStyle` has. No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the style's triggers in its FrameworkUI shell port (branch `t3/port-frameworkui-shell`, not yet on `main`), where it's recorded as an upstream quirk.
