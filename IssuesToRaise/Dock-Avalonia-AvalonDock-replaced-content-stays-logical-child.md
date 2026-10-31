# Dock.Avalonia.AvalonDock: a document's previous content stays the DockingManager's logical child when its Content changes, so closed views leak

> **Third party, not USACE.** This is about [wieslawsoltes/Dock](https://github.com/wieslawsoltes/Dock), the docking library XGoodFit uses in place of WPF's AvalonDock. It's written for the Dock project's maintainers. Whether to report it is the project owner's decision.

**Target:** [wieslawsoltes/Dock](https://github.com/wieslawsoltes/Dock), package `Dock.Avalonia.AvalonDock` (the AvalonDock-compatible API), `src/Dock.Avalonia.AvalonDock/Controls/LayoutItem.cs`, `Infrastructure/LayoutItemRegistry.cs`, `DockingManager.cs` and `DockingManager.LogicalChildren.cs`. Found in 12.1.0.13 from NuGet, built from commit [`78da173`](https://github.com/wieslawsoltes/Dock/tree/78da173545501e796f37bc8528e9e55d9cbfd991) (the package's repository commit and tag `v12.1.0.13`), still `master` on 2026-10-31. With Avalonia 12.1.4.

**Type:** bug (memory leak) · **Severity:** medium

**Verification:** reproduced against the published package. A throwaway headless Avalonia test (Avalonia.Headless 12.1.4, Windows 11, .NET 10.0.14) put a `DockingManager` with one `LayoutDocumentPane` in a window, added a `LayoutDocument` whose `Content` was a `Border`, and checked the manager's `LogicalChildrenPublic`, the content's logical parent and, after forced collections, a `WeakReference` to the content.

## Summary

When the adapter creates a document's `LayoutItem`, it adds the document's `Content` to the manager's logical children (`InternalAddLogicalChild`), which makes the manager the content's logical parent. When the item is detached, it removes `item.Model`. But when a document's `Content` changes, `LayoutItem.SynchronizeModel` publishes the new content as `Model` without touching the logical children. So:

- the previous content stays among the manager's logical children, with the manager as its logical parent;
- the new content isn't added;
- when the item is later detached, `item.Model` is the new content (or null), so the previous content is never removed.

In Avalonia, a control's logical parent is also its inheritance parent, and `AvaloniaObject` keeps its inheritance children in a strong list. So the previous content stays in memory, with everything it references, as long as the manager does.

Some AvalonDock applications clear a closed document's content in its `Closed` handler (`document.Content = null`): USACE's WPF-Framework does, and so does XGoodFit's port of it. With this package every closed document's view then stays in memory for the window's lifetime. WPF's AvalonDock holds its logical children through weak references, so the same code doesn't leak there; the adapter's own list is weak too (`List<WeakReference>`), but the parent link isn't.

## Details and how to reproduce

The test's core:

```csharp
var pane = new LayoutDocumentPane();
var dock = new DockingManager { Layout = new LayoutRoot { RootPanel = new LayoutPanel(pane) } };
var window = new Window { Content = dock, Width = 800, Height = 600 };
window.Show();

var view = new Border { Child = new TextBlock { Text = "view" } };
var doc = new LayoutDocument { Title = "Doc", Content = view };
pane.Children.Add(doc);
doc.IsActive = true;
Dispatcher.UIThread.RunJobs();
if (clearContent) doc.Closed += (s, _) => ((LayoutDocument)s!).Content = null!;
doc.Close();
Dispatcher.UIThread.RunJobs();
// then: is `view` in dock.LogicalChildrenPublic, what is its LogicalParent, and is it collected?

// a third case: an open document's Content replaced
var a = new Border(); var b = new Border();
var doc2 = new LayoutDocument { Title = "Doc", Content = a };
pane.Children.Add(doc2);
Dispatcher.UIThread.RunJobs();
doc2.Content = b;
Dispatcher.UIThread.RunJobs();
```

Output:

```
open: view among the dock's logical children True, its logical parent DockingManager
closed (Content cleared in Closed: False): among the logical children False, logical parent null, doc.Root null, doc.Content the view
  Content cleared in Closed: False; view collected after close: False
open: view among the dock's logical children True, its logical parent DockingManager
closed (Content cleared in Closed: True): among the logical children True, logical parent DockingManager, doc.Root null, doc.Content null
  Content cleared in Closed: True; view collected after close: False
open document's Content replaced: old content among the logical children True, its logical parent DockingManager; new content among them False
```

So closing a document without changing its content does take the content out, and the leak needs a content change. (In the first case the view wasn't collected either while the window was open: the closed `LayoutDocument` still referenced it as its `Content`, and something kept the document. That wasn't traced.) In XGoodFit, calling the internal `InternalRemoveLogicalChild` for the old content after clearing it lets the closed view be collected, which XGoodFit's own test checks; so the logical child is what keeps it.

## Why this is an issue and not intended behaviour

- The adapter means the manager not to keep its logical children alive: `_publicLogicalChildren` is a `List<WeakReference>`, as in WPF's AvalonDock. The parent link it sets with them defeats that.
- It already handles a changing property this way elsewhere: `ReplaceLogicalChild` removes the old value and adds the new one for the side panels, the layout root panel and the auto-hide window. A document's or tool's `Content` is the same kind of change, and the item already observes it (`ModelChanged` → `SynchronizeModel` for "Content").
- After a content change, the new content isn't a logical child at all, unlike content set before the item was created, so the two paths give different logical trees for the same layout.

## Where in the source

- The add, when an item is created: [`src/Dock.Avalonia.AvalonDock/Infrastructure/LayoutItemRegistry.cs` line 76](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/Infrastructure/LayoutItemRegistry.cs#L76), in `Create` ([lines 60-89](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/Infrastructure/LayoutItemRegistry.cs#L60-L89)):
  ```csharp
  if (content.Content is InputElement logicalContent) _manager.InternalAddLogicalChild(logicalContent);
  ```
- The content change, which only republishes `Model`: [`src/Dock.Avalonia.AvalonDock/Controls/LayoutItem.cs` lines 271-297](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/Controls/LayoutItem.cs#L271-L297):
  ```csharp
  if (!ReferenceEquals(_model, content.Content))
  {
      ...
      SetAndRaise(ModelProperty, ref _model, content.Content);
  ```
  reached from the registry's `ModelChanged` for "Content" ([`LayoutItemRegistry.cs` lines 264-282](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/Infrastructure/LayoutItemRegistry.cs#L264-L282)).
- The removal, of whatever `Model` is by then: [`src/Dock.Avalonia.AvalonDock/DockingManager.cs` lines 665-669](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/DockingManager.cs#L665-L669), called from `LayoutItem.Detach` ([`LayoutItem.cs` lines 169-192](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/Controls/LayoutItem.cs#L169-L192)) before it clears `Model`:
  ```csharp
  internal void DetachLayoutItem(LayoutItem item)
  {
      base.LogicalChildren.Remove(item);
      if (item.Model is InputElement content) InternalRemoveLogicalChild(content);
  }
  ```
- The weak list and the strong parent link: [`src/Dock.Avalonia.AvalonDock/DockingManager.LogicalChildren.cs` lines 13 and 34-54](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/DockingManager.LogicalChildren.cs#L13-L54) (`logical.SetParent(this)` at line 39), and `ReplaceLogicalChild`, [lines 67-71](https://github.com/wieslawsoltes/Dock/blob/78da173545501e796f37bc8528e9e55d9cbfd991/src/Dock.Avalonia.AvalonDock/DockingManager.LogicalChildren.cs#L67-L71).
- Avalonia 12.1.4: `StyledElement`'s `ISetLogicalParent.SetParent` sets `InheritanceParent = parent` ([`src/Avalonia.Base/StyledElement.cs` lines 447-460](https://github.com/AvaloniaUI/Avalonia/blob/12.1.4/src/Avalonia.Base/StyledElement.cs#L447-L460)), and the parent adds the child to its `List<AvaloniaObject> _inheritanceChildren` ([`src/Avalonia.Base/AvaloniaObject.cs` lines 65-83 and 614-618](https://github.com/AvaloniaUI/Avalonia/blob/12.1.4/src/Avalonia.Base/AvaloniaObject.cs#L65-L83)).
- WPF's AvalonDock, for comparison, as vendored in USACE's WPF-Framework: [`src/AvalonDock/Xceed.Wpf.AvalonDock/DockingManager.cs` line 805](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/AvalonDock/Xceed.Wpf.AvalonDock/DockingManager.cs#L805) (`List<WeakReference> _logicalChildren`).

## Suggested fix

Move the logical child when the content changes, as `ReplaceLogicalChild` does for the manager's own properties:

```csharp
// LayoutItem.SynchronizeModel, where _model changes
object? previous = _model;
SetAndRaise(ModelProperty, ref _model, content.Content);
if (Manager is { } manager)
{
    if (previous is InputElement old) manager.InternalRemoveLogicalChild(old);
    if (_model is InputElement current) manager.InternalAddLogicalChild(current);
}
```

Or have the registry track the content it registered for each item and remove that one in `DetachLayoutItem`, whatever `Model` is by then. A test: replace an open document's `Content` and check that the old content's `LogicalParent` is null and the new content's is the manager; close a document whose `Closed` handler clears its content and check the content is collected.

## How XGoodFit handles it

XGoodFit works around it: after its `Document_Closed` clears a closed document's content, as WPF-Framework's does, `MainWindow.ReleaseClosedContent` calls the package's internal `InternalRemoveLogicalChild` by reflection if the content is still among the manager's logical children ([`src/Framework/FrameworkUI/Main Window/MainWindow.axaml.cs` lines 1383-1417](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/Framework/FrameworkUI/Main%20Window/MainWindow.axaml.cs#L1383-L1417)). [`MainWindowTests.ClosingADocument_LetsItsViewGo`](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/Framework/FrameworkUI.Tests/MainWindowTests.cs#L107-L161) checks that a closed document's view is collected. If a later package drops the internal method, the workaround does nothing and that test fails.
