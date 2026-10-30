# MainWindow.ProjectNode: Tools and Help menu items accumulate, and a non-empty project opens every document first

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Main Window/MainWindow.xaml.cs` (`ProjectNode_PropertyChangedCallback` and the menu-item dependency properties). Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-30.

**Type:** bug · **Severity:** low (latent: RMC-BestFit 2.0.1 sets `ProjectNode` once, with an empty project)

**Verification:** verified by reading the source; also seen in XGoodFit's port of the shell, whose tests create several main windows.

## Summary

Three problems on the path that runs when `MainWindow.ProjectNode` is assigned:

1. The callback clears `ProjectMenuItems` before adding the new node's items, but not `ToolsMenuItems` or `HelpMenuItems`. A second assignment appends a second copy of every Tools and Help entry.
2. The three collections are the dependency properties' default values (`new FrameworkPropertyMetadata(new ObservableCollection<MenuItem>())`), so every `MainWindow` in the process shares them.
3. For each existing element the callback calls `NodeAdded`, which opens a document unless `_openingProject` is set, and only `OpenProject` sets it. Assigning a node whose project already has elements opens a document for every element, saves the explorer layout and logs "The … was created." for each. Then `LoadLayout()` replaces the documents.

## Details and how to reproduce

- Set `mainWindow.ProjectNode` twice, or create two `MainWindow`s. **Actual:** the Tools and Help menus list each item twice.
- Set `mainWindow.ProjectNode = new MyProjectNode(projectWithElements)`. **Actual:** every element's view is built and opened, "was created" events appear in the message window, and the layout load then drops those documents.

RMC-BestFit 2.0.1 sets `ProjectNode` once at start-up with an empty dummy project ([`App.xaml.cs` lines 80-89](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.App/App.xaml.cs#L80-L89)), and opening a file goes through `OpenProject`, which reuses the node. So it isn't affected today. Other hosts, tests and future changes are.

## Why this is an issue and not intended behaviour

- The theme-change refresh clears all three collections before refilling them ([lines 168-185](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L168-L185)), so the missing clears here are an inconsistency.
- A mutable reference-type default in `PropertyMetadata` is a known WPF pitfall: the instance is shared by all owners.
- `NodeAdded` documents its rule: "Opens the document for the new node unless the project is currently opening". Loading an existing project is opening it.

## Where in the source

- [`MainWindow.xaml.cs` lines 266-368](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L266-L368)
  ```csharp
  thisControl.ProjectMenuItems.Clear();                          // line 272: only this one
  ...
  foreach (var item in newValue.ToolsMenuItems) thisControl.ToolsMenuItems.Add(item);   // lines 323-324
  foreach (var item in newValue.HelpMenuItems)  thisControl.HelpMenuItems.Add(item);    // lines 326-327
  ...
  if (elementNode != null) thisControl.NodeAdded(elementNode);    // line 353, _openingProject is false here
  ...
  thisControl.LoadLayout();                                       // line 364
  ```
- The shared defaults: [lines 466, 480 and 494](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L466).
- `NodeAdded`'s check: [lines 879-900](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L879-L900).

## Suggested fix

- Clear `ToolsMenuItems` and `HelpMenuItems` with `ProjectMenuItems` at the top of the callback.
- Register the properties with a `null` default and create the collections in the constructor (`SetCurrentValue(ToolsMenuItemsProperty, new ObservableCollection<MenuItem>())`).
- Set `_openingProject = true` around the `NodeAdded` loop in the callback, as `OpenProject` does.

No file format is involved.

## How XGoodFit handles it

XGoodFit (a port of RMC-BestFit to Avalonia) keeps the callback in its FrameworkUI shell port (branch `t3/port-frameworkui-shell`, not yet on `main`). It detaches the shared collections when a window closes and gives test windows their own collections.
