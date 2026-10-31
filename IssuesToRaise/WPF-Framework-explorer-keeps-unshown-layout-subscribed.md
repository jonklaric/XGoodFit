# Opening a project saved with both explorer layouts leaks the layout that isn't shown

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkUI/Project Explorer/View Models/ProjectNode.cs` (`Load`) and `ElementNodeCollection.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-31. Also affects RMC-BestFit, whose project is a singleton.

**Type:** bug (memory leak) · **Severity:** low

**Verification:** verified by reading the source. XGoodFit's port fixed it, with a test that counts the listeners on the project's collections after such a load: one per collection with the fix ([`tests/Framework/FrameworkUI.Tests/ExplorerTests.cs` lines 52-75](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/tests/Framework/FrameworkUI.Tests/ExplorerTests.cs#L52-L75)). 26 of RMC-BestFit's 32 example projects store both layouts.

## Summary

A project stores its Project Explorer layout twice: the current one and the previous one, as a backup. `ProjectNode.Load` builds a node tree from each (`TryCreateLayoutCandidate`), chooses one, and shows it. Building a tree sets each collection node's `ElementCollection`, which subscribes the node to the project collection's `ElementAdded` and `ElementRemoved`. The tree that isn't chosen is dropped without being detached, so its nodes stay subscribed. They stay alive as long as the project's collections and keep adding nodes for every element added. For RMC-BestFit, whose project and collections are created once and reused for every project, that's the application's lifetime, and each open of such a project adds another hidden tree.

## Details and how to reproduce

1. Open a project whose `ProjectExplorerLayout` and `ProjectExplorerLayoutPrevious` are both set (any project saved twice; for example most of RMC-BestFit's examples).
2. Count the handlers on a collection's `ElementAdded` (in a debugger, or a test like XGoodFit's). Expected: one per collection, the shown tree's node. Actual, by reading: two, and one more for every further open of such a project.
3. Add an element: each hidden tree creates an `ElementNode` for it, which subscribes to the element in turn.

Only the shown tree is detached on the next load (`ClearChildNodesForRetry`).

## Why this is an issue and not intended behaviour

`TryCreateLayoutCandidate`'s remarks say the candidate "is not installed into `Node.ChildNodes` until `Load` chooses it, so a failed current layout cannot leave partial nodes behind": the design means a candidate to be disposable. But building it already subscribes it to the project. `ClearChildNodesForRetry` shows the cleanup a dropped tree needs, setting each collection node's `ElementCollection` to null, and applies it only to the shown tree.

## Where in the source

- `Load`, which builds both candidates and installs one: [`src/FrameworkUI/Project Explorer/View Models/ProjectNode.cs` lines 145-177](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ProjectNode.cs#L145-L177):
  ```csharp
  var currentCandidate = TryCreateLayoutCandidate(Project.ProjectExplorerLayout, "current");
  var previousCandidate = TryCreateLayoutCandidate(Project.ProjectExplorerLayoutPrevious, "previous");
  var chosenCandidate = ChooseLayoutCandidate(currentCandidate, previousCandidate);
  ```
- `TryCreateLayoutCandidate` and its remarks: [lines 179-225](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ProjectNode.cs#L179-L225); `ChooseLayoutCandidate`, [lines 233-245](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ProjectNode.cs#L233-L245).
- The subscription made by building a node: [`ElementNodeCollection.cs` lines 40-81](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ElementNodeCollection.cs#L40-L81) (`newElementCollection.ElementAdded += thisControl.ElementAdded;` at line 79).
- The cleanup, for the shown tree only: [`ProjectNode.cs` lines 356-367](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Project%20Explorer/View%20Models/ProjectNode.cs#L356-L367).
- RMC-BestFit's collections, created once and reused: [`src/RMC.BestFit.UI/BestFitProject.cs` line 50](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/BestFitProject.cs#L50) and [lines 586-594](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/BestFitProject.cs#L586-L594); `Close` clears them ([lines 969-982](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/BestFitProject.cs#L969-L982)) but keeps their subscribers.

## Suggested fix

After choosing, detach the candidate that wasn't chosen, as `ClearChildNodesForRetry` does for the shown one, including nested collection nodes:

```csharp
foreach (var candidate in new[] { currentCandidate, previousCandidate })
    if (candidate != null && !ReferenceEquals(candidate, chosenCandidate))
        ReleaseCollectionNodes(candidate.Nodes);

private static void ReleaseCollectionNodes(IList<Node> nodes)
{
    foreach (var node in nodes)
    {
        if (node is not ElementNodeCollection collection) continue;
        ReleaseCollectionNodes(new List<Node>(collection.ChildNodes));
        collection.ElementCollection = null;
    }
}
```

`ClearChildNodesForRetry` could use the same helper: it nulls only the top-level collection nodes, so the nested ones made for a collection's child collections would stay subscribed too. Building the second candidate lazily, only when the first is unusable or flat, would also avoid the work. No compatibility risk: the saved layouts are unchanged.

## How XGoodFit handles it

XGoodFit fixed it in its port in commit [`3f417e2`](https://github.com/jonklaric/XGoodFit/commit/3f417e298bff9e075956fa9b5c9ef26c7caf601f), with the code above: [`src/Framework/FrameworkUI/Project Explorer/View Models/ProjectNode.cs` lines 158-168 and 729-737](https://github.com/jonklaric/XGoodFit/blob/149b96b320911b89c8b64cc786b3a4d1828896e6/src/Framework/FrameworkUI/Project%20Explorer/View%20Models/ProjectNode.cs#L158-L168).
