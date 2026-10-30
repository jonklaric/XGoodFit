# The Messenger keeps every message source alive after its messages are removed

**Target:** [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework), `src/FrameworkInterfaces/Messaging/Messenger.cs`. Found in 1.0.5 at commit [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93), still `main` on 2026-10-31.

**Type:** bug (memory leak) · **Severity:** medium

**Verification:** reproduced against the published `RMC.Wpf.Framework.Core` 1.0.5 package. A small program added a message for a source object, removed it (or cleared the source's messages), dropped its own reference and forced a garbage collection: the source was still alive in both cases, with no messages left. After `Clear()` it was collected.

## Summary

The Messenger singleton stores messages in a dictionary keyed by their source, which is usually an element. `Remove(message)` and `Clear(source)` empty the source's inner dictionary but leave the source as a key. So every element that has ever had a message stays reachable from a static object, and through `ElementBase.ParentCollection` and `ElementCollectionBase.ParentProject`, so does its project. Only `Clear()` releases them, and the shell calls that only when opening a project.

## Details and how to reproduce

```csharp
static WeakReference Run(bool useClear)
{
    var source = new object();   // stands for an element
    var m = new BasicMessageItem(MessageType.Error, "The name cannot be blank.", source, "Input Data", "x", "Name", "TEST-ERR-001");
    Messenger.GetInstance().Add(m);
    if (useClear) Messenger.GetInstance().Clear(source); else Messenger.GetInstance().Remove(m);
    return new WeakReference(source);
}
// after each call: GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect();
```

Output:

```
after Remove(message): messages left 0, source still alive: True
after Clear(source): messages left 0, source still alive: True
after Clear(): sources still alive: False, False
```

In an application, elements add and remove validation messages all the time (for example `ElementBase`'s name checks), and RMC-BestFit's elements call `_messenger.Clear(this)` in `Open` and in `Delete` (for example [`InputData.cs` line 1871](https://github.com/USACE-RMC/RMC-BestFit/blob/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269/src/RMC.BestFit.UI/Elements/InputData/InputData.cs#L1871)). So the call meant to clean up a deleted element keeps it in memory, and a project replaced through the New command (`NewCommandBinding_Executed` calls `CloseProject` but not `Clear()`) stays until another project is opened. XGoodFit found this as one of the leaks that grew a test run that opens many example projects past 12 GB.

## Why this is an issue and not intended behaviour

- A source with no messages has nothing left for the Messenger to hold. Nothing reads an empty entry: `AllMessageItems()` and the events only use messages.
- The Messenger is a process-wide singleton (`GetInstance()`), so its references never go away on their own.

## Where in the source

- The dictionary: [`src/FrameworkInterfaces/Messaging/Messenger.cs` line 64](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Messaging/Messenger.cs#L64)
- `Remove`, [lines 493-519](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Messaging/Messenger.cs#L493-L519):
  ```csharp
  removed = _messagesBySource[message.Source].Remove(message.Code);
  ```
- `Clear(object source)`, [lines 554-573](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Messaging/Messenger.cs#L554-L573):
  ```csharp
  toNotify = _messagesBySource[source].Values.ToArray();
  _messagesBySource[source].Clear();
  ```
- `Clear()`, which does release them ([line 535](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Messaging/Messenger.cs#L535)), is called only from `MainWindow.OpenProject`: [`src/FrameworkUI/Main Window/MainWindow.xaml.cs` line 1574](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkUI/Main%20Window/MainWindow.xaml.cs#L1574).
- The path to the project: [`ElementBase.ParentCollection`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Project/ElementBase.cs#L195) and [`ElementCollectionBase.ParentProject`](https://github.com/USACE-RMC/WPF-Framework/blob/7f897e107db4a25cca29127ce9d564898c5fde93/src/FrameworkInterfaces/Project/ElementCollectionBase.cs#L48).

## Suggested fix

Forget a source once it has no messages, inside the existing lock:

```csharp
// Remove
removed = _messagesBySource[message.Source].Remove(message.Code);
if (_messagesBySource[message.Source].Count == 0) _messagesBySource.Remove(message.Source);

// Clear(object source)
toNotify = _messagesBySource[source].Values.ToArray();
_messagesBySource.Remove(source);
```

`Add` already creates the entry when a source has none, so nothing else changes. Upstream's `MessengerTests` don't depend on empty entries being kept. Holding sources weakly (`ConditionalWeakTable`) would be an alternative, but messages are meant to be shown until removed, so the simple fix is enough.

## How XGoodFit handles it

XGoodFit fixed it in its port in commit [`b0aa899`](https://github.com/jonklaric/XGoodFit/commit/b0aa8999cdd5f50029a1472946bda7b1aa6d5a14): [`src/Framework/FrameworkInterfaces/Messaging/Messenger.cs` lines 519-521 and 577-578](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/src/Framework/FrameworkInterfaces/Messaging/Messenger.cs#L519-L521), with tests in [`tests/Framework/FrameworkUI.Tests/MessengerTests.cs`](https://github.com/jonklaric/XGoodFit/blob/5f913ad063782422048c7cf2109b081417f85faa/tests/Framework/FrameworkUI.Tests/MessengerTests.cs).
