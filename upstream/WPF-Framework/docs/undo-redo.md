# Undo/Redo System

[<- Previous: Themes](themes.md) | [Back to Index](index.md) | [Next: Generic Controls ->](generic-controls.md)

## Overview

The WPF Framework provides a per-element undo/redo system in the `FrameworkInterfaces.Undo` namespace. Each element maintains its own undo stack, enabling Visual Studio-style per-document undo where Ctrl+Z operates on the active document.

The system is built on three core abstractions:

- **`IUndoManager`** -- Manages undo and redo stacks for a specific scope (element, collection, or project). Supports action merging, transactions, and save-point tracking.
- **`IUndoableAction`** -- Represents a single reversible operation with `Execute()`, `Undo()`, and merge support.
- **Bridge classes** -- `UndoableStateBridge` and `UndoableCollectionBridge` automatically record property and collection changes from `INotifyPropertyChanged` / `INotifyCollectionChanged` objects.

## Quick Start

### Record a property change

The simplest way to add undo support is to use `RecordPropertyChange` in your `ElementBase` subclass:

```csharp
public class MyElement : ElementBase
{
    private string _title = "";

    public string Title
    {
        get => _title;
        set
        {
            if (_title != value)
            {
                var oldValue = _title;
                _title = value;
                RecordPropertyChange(nameof(Title), oldValue, value);
            }
        }
    }
}
```

`RecordPropertyChange` handles everything: it creates a `PropertyChangeAction`, records it with the element's `UndoManager`, raises `PropertyChanged`, and sets `IsDirty = true`.

### Undo and redo

```csharp
element.UndoManager.Undo();   // Reverts the most recent action
element.UndoManager.Redo();   // Re-applies the most recently undone action
```

## Core Concepts

### UndoManager API

`UndoManager` is the default implementation of `IUndoManager`. Each `ElementBase` lazily creates its own instance.

| Member | Type | Description |
|--------|------|-------------|
| `CanUndo` | `bool` | Whether the undo stack has any actions |
| `CanRedo` | `bool` | Whether the redo stack has any actions |
| `UndoDescription` | `string?` | Description of the next undo action (for tooltips) |
| `RedoDescription` | `string?` | Description of the next redo action (for tooltips) |
| `UndoStack` | `IReadOnlyList<IUndoableAction>` | All undoable actions, most recent first |
| `RedoStack` | `IReadOnlyList<IUndoableAction>` | All redoable actions, most recent first |
| `MaxUndoLevels` | `int` | Maximum undo depth. Default: **100** |
| `IsExecutingAction` | `bool` | True during undo/redo execution (prevents re-recording) |
| `HasChangedSinceSave` | `bool` | Whether state differs from the last save point |
| `ExecuteAction(action)` | `void` | Executes an action and records it |
| `RecordAction(action)` | `void` | Records an already-executed action |
| `Undo()` | `void` | Undoes the most recent action |
| `UndoTo(action)` | `void` | Undoes multiple actions up to a specific action |
| `Redo()` | `void` | Redoes the most recently undone action |
| `RedoTo(action)` | `void` | Redoes multiple actions up to a specific action |
| `Clear()` | `void` | Clears both stacks |
| `MarkSavePoint()` | `void` | Marks the current state as saved |
| `BeginTransaction(desc)` | `IDisposable` | Groups multiple actions into one undo operation |
| `StateChanged` | `event` | Raised when undo/redo state changes |

`UndoManager` implements `INotifyPropertyChanged` and raises property change notifications for `CanUndo`, `CanRedo`, `UndoDescription`, `RedoDescription`, and `HasChangedSinceSave` whenever the stacks change.

### Action types

| Class | Namespace | Description |
|-------|-----------|-------------|
| `PropertyChangeAction` | `Undo.Actions` | Records a property value change via reflection. Supports 500ms merge window for rapid successive changes to the same property on the same target. |
| `DelegateAction` | `Undo.Actions` | Accepts `Action` delegates for execute and undo. Does not support merging. |
| `CompositeAction` | `Undo.Actions` | Groups multiple actions from `BeginTransaction` into a single undoable unit. Created automatically by the transaction system. |

## Property Change Tracking

### Using RecordPropertyChange (ElementBase)

For properties on your `ElementBase` subclass, `RecordPropertyChange` is the recommended approach:

```csharp
public override string Description
{
    get => _description;
    set
    {
        if (_description != value)
        {
            var oldValue = _description;
            _description = value;
            RecordPropertyChange(nameof(Description), oldValue, value);
        }
    }
}
```

`RecordPropertyChange` checks `IsUndoEnabled` and `UndoManager.IsExecutingAction` before recording. It always raises `PropertyChanged` and calls `SetIsDirty(true)`.

### Using PropertyChangeAction manually

For finer control, create a `PropertyChangeAction` directly:

```csharp
var action = new PropertyChangeAction(
    target: myObject,           // The object whose property changed
    propertyName: "Score",      // Property name (must exist on target)
    oldValue: 10,               // Previous value
    newValue: 20                // New value
);
undoManager.RecordAction(action);
```

`PropertyChangeAction` uses reflection to set property values during undo/redo. It supports time-window merging: two `PropertyChangeAction` instances for the same target and property are merged if they occur within 500ms of each other. This coalesces rapid typing into a single undo entry. The merge window is configurable via the static field `PropertyChangeAction.MergeWindowMilliseconds`.

### Using DelegateAction

For operations that cannot be expressed as simple property changes:

```csharp
var item = new DataPoint(x, y);
var action = new DelegateAction(
    description: "Add data point",
    execute: () => collection.Add(item),
    undo: () => collection.Remove(item),
    target: myElement
);
undoManager.ExecuteAction(action);  // Executes the action AND records it
```

`DelegateAction` does not support merging. For merge support, implement `IUndoableAction` directly.

## UndoableStateBridge

`UndoableStateBridge` monitors an `INotifyPropertyChanged` object and automatically creates `PropertyChangeAction` entries for every property change. This is the primary mechanism for integrating third-party objects or model classes with the undo system without modifying those classes.

### Basic usage

```csharp
public class MyElement : ElementBase
{
    private readonly ExternalModel _model;
    private readonly UndoableStateBridge _modelBridge;

    public MyElement(string name, IElementCollection parent) : base(name, parent)
    {
        _model = new ExternalModel();

        _modelBridge = new UndoableStateBridge(
            source: _model,
            getUndoManager: () => IsUndoEnabled ? UndoManager : null,
            sourceDescription: "model settings",
            target: this
        );
    }
}
```

### Constructor parameters

```csharp
public UndoableStateBridge(
    INotifyPropertyChanged source,
    Func<IUndoManager?> getUndoManager,
    string sourceDescription = "settings",
    object? target = null,
    IEnumerable<string>? includedProperties = null,
    IEnumerable<string>? excludedProperties = null,
    Action? onActionRecorded = null)
```

| Parameter | Description |
|-----------|-------------|
| `source` | The object to monitor. Must implement `INotifyPropertyChanged`. |
| `getUndoManager` | Factory function returning the current `IUndoManager`, or `null` to disable recording. Called on every property change, allowing dynamic enable/disable. |
| `sourceDescription` | Human-readable label for undo descriptions (e.g., "Change model settings"). Default: `"settings"`. |
| `target` | Optional owner object for action association. |
| `includedProperties` | If specified, only these properties are monitored. Cannot be used with `excludedProperties`. |
| `excludedProperties` | Properties to exclude from monitoring. Cannot be used with `includedProperties`. |
| `onActionRecorded` | Optional callback invoked each time a new forward action is recorded (not during undo/redo). Useful for calling `SetIsDirty(true)`. |

### Property filtering

Monitor only specific properties:

```csharp
var bridge = new UndoableStateBridge(
    _model,
    () => IsUndoEnabled ? UndoManager : null,
    "plot options",
    this,
    includedProperties: new[] { "Title", "XAxisLabel", "YAxisLabel" }
);
```

Exclude transient properties:

```csharp
var bridge = new UndoableStateBridge(
    _model,
    () => IsUndoEnabled ? UndoManager : null,
    "chart settings",
    this,
    excludedProperties: new[] { "IsSelected", "IsDirty", "IsExpanded" }
);
```

You can also modify the exclusion list at runtime:

```csharp
bridge.ExcludeProperty("TransientProperty");  // Stop monitoring
bridge.IncludeProperty("TransientProperty");   // Resume monitoring
```

Both methods throw `InvalidOperationException` if the bridge was constructed with an inclusion list.

### Suspending recording

Use `SuspendRecording()` to temporarily disable undo recording during bulk operations, initialization, or data loading. It returns an `IDisposable` that resumes recording and updates shadow values when disposed:

```csharp
using (bridge.SuspendRecording())
{
    model.Property1 = loadedValue1;
    model.Property2 = loadedValue2;
    model.Property3 = loadedValue3;
}
// Recording resumes; shadow values reflect the new state
```

### Merge behavior

`UndoableStateBridge` creates `PropertyChangeAction` instances, which support time-window merging. Rapid changes to the same property within 500ms are coalesced into a single undo entry. For example, typing in a `TextBox` bound to a monitored property produces one undo entry instead of one per keystroke.

### Disposal

`UndoableStateBridge` implements `IDisposable`. Call `Dispose()` to unsubscribe from the source's `PropertyChanged` event:

```csharp
_modelBridge.Dispose();
```

## UndoableCollectionBridge

`UndoableCollectionBridge<T>` monitors an `IList<T>` that also implements `INotifyCollectionChanged` and automatically creates undo actions for Add, Remove, Replace, Move, and Reset operations.

### Basic usage

```csharp
public class MyElement : ElementBase
{
    private readonly ObservableCollection<double> _values;
    private readonly UndoableCollectionBridge<double> _valuesBridge;

    public MyElement(string name, IElementCollection parent) : base(name, parent)
    {
        _values = new ObservableCollection<double>();

        _valuesBridge = new UndoableCollectionBridge<double>(
            collection: _values,
            getUndoManager: () => IsUndoEnabled ? UndoManager : null,
            collectionDescription: "values",
            target: this
        );
    }
}
```

### Constructor parameters

```csharp
public UndoableCollectionBridge(
    IList<T> collection,
    Func<IUndoManager?> getUndoManager,
    string collectionDescription = "collection",
    object? target = null)
```

| Parameter | Description |
|-----------|-------------|
| `collection` | The collection to monitor. Must implement both `IList<T>` and `INotifyCollectionChanged`. |
| `getUndoManager` | Factory function returning the current `IUndoManager`, or `null` to disable recording. |
| `collectionDescription` | Label used in undo descriptions (e.g., "Add to values", "Clear values"). |
| `target` | Optional owner object for action association. |

### Supported operations

| Collection Change | Undo Action | Redo Action |
|-------------------|-------------|-------------|
| Add | Remove the added items | Re-insert at original indices |
| Remove | Re-insert at original indices | Remove again |
| Replace | Restore old values | Apply new values |
| Reset (Clear) | Restore the full pre-clear state from shadow copy | Re-apply the post-clear state |
| Move | Move item back to original index | Move item to new index |

### BulkRestoreWrapper

When a Reset action is undone or redone, the bridge must clear the collection and re-add all items. Without wrapping, each individual Clear and Add call fires `CollectionChanged`, potentially triggering expensive operations on intermediate states.

Set `BulkRestoreWrapper` to suppress events during the restore:

```csharp
_valuesBridge.BulkRestoreWrapper = (restoreAction) =>
{
    _values.SuppressCollectionChanged = true;
    restoreAction();
    _values.SuppressCollectionChanged = false;
    _values.RaiseCollectionChangedReset();
};
```

If `BulkRestoreWrapper` is `null` (the default), the restore loop runs without wrapping.

### Disposal

`UndoableCollectionBridge<T>` implements `IDisposable`. Always dispose when the collection is no longer needed:

```csharp
_valuesBridge.Dispose();
```

## Composite Actions (Transactions)

Use `BeginTransaction` to group multiple operations into a single undo entry:

```csharp
using (undoManager.BeginTransaction("Delete selected elements"))
{
    foreach (var element in selectedElements)
    {
        element.Delete();
    }
}
// All deletes are now a single undo operation
```

`BeginTransaction` returns an `IDisposable`. When disposed, it commits all recorded actions as a single `CompositeAction` on the undo stack.

### Nested transactions

Nested calls to `BeginTransaction` are supported. Inner transactions return a no-op disposable; all actions accumulate in the outermost transaction:

```csharp
using (undoManager.BeginTransaction("Outer operation"))
{
    // ... some changes ...

    using (undoManager.BeginTransaction("Inner operation"))
    {
        // ... more changes ...
    }
    // Inner dispose is a no-op

    // ... even more changes ...
}
// All changes committed as one CompositeAction
```

### Transaction rollback

To cancel a transaction and undo all changes made within it, call `RollbackTransaction()` on the concrete `UndoManager` before the scope disposes:

```csharp
var manager = (UndoManager)undoManager;
var transaction = manager.BeginTransaction("Risky operation");
try
{
    // Perform changes...
    manager.CommitTransaction();  // Explicit commit
}
catch
{
    manager.RollbackTransaction();  // Undo all changes
    throw;
}
```

Note that if using a `using` block, the automatic dispose calls `CommitTransaction()`. Call `RollbackTransaction()` before dispose to prevent the commit.

## Temporarily Disabling Undo

### Via IsUndoEnabled on ElementBase

Toggle `IsUndoEnabled` on the element to prevent recording:

```csharp
element.IsUndoEnabled = false;
try
{
    element.Name = "Loading...";  // Not recorded
    element.Description = "...";  // Not recorded
}
finally
{
    element.IsUndoEnabled = true;
}
```

### Via the UndoManager factory function

Both bridge classes accept a `Func<IUndoManager?>` that returns `null` to disable recording:

```csharp
// Recording is disabled when getUndoManager returns null
new UndoableStateBridge(
    _model,
    () => IsUndoEnabled ? UndoManager : null,  // null disables recording
    "settings"
);
```

### Via UndoableStateBridge.SuspendRecording

For targeted suspension on a specific bridge:

```csharp
using (bridge.SuspendRecording())
{
    // Changes are not recorded
    // Shadow values update on resume
}
```

### Via IsExecutingAction

`UndoManager.IsExecutingAction` is `true` during undo/redo execution. All recording mechanisms (bridges, `RecordPropertyChange`, `RecordAction`) check this flag and skip recording to prevent infinite recursion.

## Configuration

### MaxUndoLevels

Control the maximum depth of the undo stack:

```csharp
undoManager.MaxUndoLevels = 50;  // Default is 100
```

When the stack exceeds this limit, the oldest actions are trimmed.

### Save point tracking

Mark the current state as saved to track unsaved changes:

```csharp
// After saving
undoManager.MarkSavePoint();

// Check for unsaved changes
if (undoManager.HasChangedSinceSave)
{
    // Prompt to save
}
```

### Events

Subscribe to `StateChanged` for any undo/redo state change:

```csharp
undoManager.StateChanged += (sender, e) =>
{
    saveButton.IsEnabled = undoManager.HasChangedSinceSave;
    undoButton.IsEnabled = undoManager.CanUndo;
    redoButton.IsEnabled = undoManager.CanRedo;
};
```

## Creating Custom Actions

Implement `IUndoableAction` for specialized undo behavior:

```csharp
public class SwapColumnsAction : IUndoableAction
{
    private readonly DataTable _table;
    private readonly int _colA;
    private readonly int _colB;

    public SwapColumnsAction(DataTable table, int colA, int colB)
    {
        _table = table;
        _colA = colA;
        _colB = colB;
        Timestamp = DateTime.Now;
    }

    public string Description => $"Swap columns {_colA} and {_colB}";
    public DateTime Timestamp { get; }
    public object? Target => _table;

    public void Execute() => _table.SwapColumns(_colA, _colB);
    public void Undo() => _table.SwapColumns(_colB, _colA);  // Same operation reverses itself

    public bool CanMergeWith(IUndoableAction other) => false;
    public IUndoableAction MergeWith(IUndoableAction other) => this;
}
```

### IUndoableAction interface

| Member | Description |
|--------|-------------|
| `Description` | Human-readable label for UI display |
| `Timestamp` | When the action was created |
| `Target` | The object this action applies to (for stack association) |
| `Execute()` | Performs the action (called for initial execution and redo) |
| `Undo()` | Reverses the action |
| `CanMergeWith(other)` | Whether this action can be merged with another |
| `MergeWith(other)` | Returns a merged action combining both |

### Implementing merge support

To support merging (coalescing rapid changes), implement `CanMergeWith` and `MergeWith`:

```csharp
public bool CanMergeWith(IUndoableAction other)
{
    if (other is not SwapColumnsAction sca) return false;
    if (!ReferenceEquals(sca._table, _table)) return false;

    // Merge within 500ms
    var timeDiff = (sca.Timestamp - Timestamp).TotalMilliseconds;
    return timeDiff >= 0 && timeDiff <= 500;
}

public IUndoableAction MergeWith(IUndoableAction other)
{
    // Return a new action that captures the combined effect
    return new SwapColumnsAction(_table, _colA, ((SwapColumnsAction)other)._colB);
}
```

The `UndoManager` calls `CanMergeWith` on the top of the undo stack whenever a new action is recorded. If it returns `true`, the manager pops the existing action and pushes the result of `MergeWith`.

## Best Practices and Troubleshooting

**DO:**
- Use `RecordPropertyChange` for properties on `ElementBase` subclasses.
- Use `UndoableStateBridge` for third-party or external model objects.
- Use `UndoableCollectionBridge` for observable collections.
- Disable undo (`IsUndoEnabled = false`) during deserialization and bulk loading.
- Call `ClearUndoHistory()` after loading data from disk.
- Call `MarkUndoSavePoint()` after saving.
- Dispose bridge objects when they are no longer needed.

**DO NOT:**
- Record actions inside property setters that are called during undo/redo -- check `UndoManager.IsExecutingAction` first (bridges and `RecordPropertyChange` do this automatically).
- Call `ExecuteAction` from inside another `ExecuteAction` -- the inner call is ignored because `IsExecutingAction` is `true`.
- Mix `ExecuteAction` and `RecordAction` for the same operation -- use `ExecuteAction` when you want the manager to call `Execute()`, and `RecordAction` when you have already performed the change.

| Symptom | Cause | Fix |
|---------|-------|-----|
| Property changes not recorded | `IsUndoEnabled` is `false` or `getUndoManager` returns `null` | Verify the undo manager factory returns a non-null value |
| Undo causes infinite loop | Action's `Undo()` triggers `PropertyChanged`, which records another action | Check `IsExecutingAction` in your property setter, or use `RecordPropertyChange` which handles this |
| Typing produces one undo entry per keystroke | Not using `PropertyChangeAction` or merge window has expired | Ensure the bridge creates `PropertyChangeAction` (default behavior); check `MergeWindowMilliseconds` |
| Undo restores wrong value | Shadow values out of sync after un-monitored changes | Call `RefreshShadowValues()` on the bridge after un-recorded changes |
| Collection undo fires too many events | Reset undo triggers individual Add events | Set `BulkRestoreWrapper` on the collection bridge |
