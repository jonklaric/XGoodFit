#pragma warning disable CS0219 // Variable is assigned but its value is never used
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type
#pragma warning disable CS8602 // Dereference of a possibly null reference
#pragma warning disable CS8603 // Possible null reference return
#pragma warning disable CS8618 // Non-nullable field must contain a non-null value
#pragma warning disable CS0414 // Field is assigned but its value is never used
#pragma warning disable CS0067 // Event is declared to satisfy an interface in snippet stubs

using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Data;
using FrameworkInterfaces;
using FrameworkInterfaces.Undo;
using FrameworkInterfaces.Undo.Actions;

namespace Documentation.Tests.Snippets.UndoRedo
{
    // ---------------------------------------------------------------
    // Stub types
    // ---------------------------------------------------------------
    internal class ExternalModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;
        public string Property1 { get; set; } = "";
        public string Property2 { get; set; } = "";
        public string Property3 { get; set; } = "";
    }

    internal class DataPoint
    {
        public double X { get; set; }
        public double Y { get; set; }
        public DataPoint(double x, double y) { X = x; Y = y; }
    }

    // Stub for the custom action example -- DataTable extension
    internal static class DataTableExtensions
    {
        public static void SwapColumns(this DataTable table, int a, int b) { }
    }

    // ---------------------------------------------------------------
    // Snippet: Record a property change (Quick Start)
    // ---------------------------------------------------------------
    public abstract class MyElementForTitle : ElementBase
    {
        private string _title = "";

        public MyElementForTitle(string name, IElementCollection parent) : base(name, parent) { }

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

    /// <summary>
    /// Validates that all C# code snippets in docs/undo-redo.md compile correctly.
    /// </summary>
    public class UndoRedoSnippets
    {
        // ---------------------------------------------------------------
        // Snippet: Undo and redo
        // ---------------------------------------------------------------
        public void Snippet_UndoAndRedo(IUndoManager undoManager)
        {
            undoManager.Undo();   // Reverts the most recent action
            undoManager.Redo();   // Re-applies the most recently undone action
        }

        // ---------------------------------------------------------------
        // Snippet: RecordPropertyChange for Description (Property Change Tracking)
        // This is just the pattern -- shown via the MyElementForTitle class above
        // ---------------------------------------------------------------

        // ---------------------------------------------------------------
        // Snippet: Using PropertyChangeAction manually
        // ---------------------------------------------------------------
        public void Snippet_PropertyChangeActionManual(IUndoManager undoManager)
        {
            object myObject = new object();
            var action = new PropertyChangeAction(
                target: myObject,           // The object whose property changed
                propertyName: "Score",      // Property name (must exist on target)
                oldValue: 10,               // Previous value
                newValue: 20                // New value
            );
            undoManager.RecordAction(action);
        }

        // ---------------------------------------------------------------
        // Snippet: Using DelegateAction
        // ---------------------------------------------------------------
        public void Snippet_DelegateAction(IUndoManager undoManager, ElementBase myElement)
        {
            var collection = new List<DataPoint>();
            double x = 1.0, y = 2.0;
            var item = new DataPoint(x, y);
            var action = new DelegateAction(
                description: "Add data point",
                execute: () => collection.Add(item),
                undo: () => collection.Remove(item),
                target: myElement
            );
            undoManager.ExecuteAction(action);  // Executes the action AND records it
        }

        // ---------------------------------------------------------------
        // Snippet: UndoableStateBridge basic usage
        // ---------------------------------------------------------------
        public void Snippet_UndoableStateBridge_Basic()
        {
            // Verifying the constructor compiles with the documented parameters
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();
            bool isUndoEnabled = true;

            var bridge = new UndoableStateBridge(
                source: model,
                getUndoManager: () => isUndoEnabled ? um : null,
                sourceDescription: "model settings",
                target: (object)null
            );
        }

        // ---------------------------------------------------------------
        // Snippet: UndoableStateBridge property filtering (included)
        // ---------------------------------------------------------------
        public void Snippet_UndoableStateBridge_IncludedProperties()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();
            bool isUndoEnabled = true;

            var bridge = new UndoableStateBridge(
                model,
                () => isUndoEnabled ? um : null,
                "plot options",
                null,
                includedProperties: new[] { "Title", "XAxisLabel", "YAxisLabel" }
            );
        }

        // ---------------------------------------------------------------
        // Snippet: UndoableStateBridge property filtering (excluded)
        // ---------------------------------------------------------------
        public void Snippet_UndoableStateBridge_ExcludedProperties()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();
            bool isUndoEnabled = true;

            var bridge = new UndoableStateBridge(
                model,
                () => isUndoEnabled ? um : null,
                "chart settings",
                null,
                excludedProperties: new[] { "IsSelected", "IsDirty", "IsExpanded" }
            );
        }

        // ---------------------------------------------------------------
        // Snippet: ExcludeProperty / IncludeProperty runtime modification
        // ---------------------------------------------------------------
        public void Snippet_UndoableStateBridge_RuntimeModification()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();

            var bridge = new UndoableStateBridge(
                model,
                () => um,
                "settings"
            );

            bridge.ExcludeProperty("TransientProperty");  // Stop monitoring
            bridge.IncludeProperty("TransientProperty");   // Resume monitoring
        }

        // ---------------------------------------------------------------
        // Snippet: SuspendRecording
        // ---------------------------------------------------------------
        public void Snippet_SuspendRecording()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();

            var bridge = new UndoableStateBridge(model, () => um, "settings");

            using (bridge.SuspendRecording())
            {
                model.Property1 = "loadedValue1";
                model.Property2 = "loadedValue2";
                model.Property3 = "loadedValue3";
            }
            // Recording resumes; shadow values reflect the new state
        }

        // ---------------------------------------------------------------
        // Snippet: UndoableStateBridge Disposal
        // ---------------------------------------------------------------
        public void Snippet_UndoableStateBridge_Dispose()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();

            var bridge = new UndoableStateBridge(model, () => um, "settings");
            bridge.Dispose();
        }

        // ---------------------------------------------------------------
        // Snippet: UndoableCollectionBridge basic usage
        // ---------------------------------------------------------------
        public void Snippet_UndoableCollectionBridge_Basic()
        {
            var values = new ObservableCollection<double>();
            IUndoManager um = new UndoManager();
            bool isUndoEnabled = true;

            var valuesBridge = new UndoableCollectionBridge<double>(
                collection: values,
                getUndoManager: () => isUndoEnabled ? um : null,
                collectionDescription: "values",
                target: (object)null
            );
        }

        // ---------------------------------------------------------------
        // Snippet: UndoableCollectionBridge Disposal
        // ---------------------------------------------------------------
        public void Snippet_UndoableCollectionBridge_Dispose()
        {
            var values = new ObservableCollection<double>();
            IUndoManager um = new UndoManager();

            var bridge = new UndoableCollectionBridge<double>(values, () => um, "values");
            bridge.Dispose();
        }

        // ---------------------------------------------------------------
        // Snippet: Composite Actions (Transactions) -- BeginTransaction
        // ---------------------------------------------------------------
        public void Snippet_BeginTransaction(IUndoManager undoManager)
        {
            var selectedElements = new List<IElement>();

            using (undoManager.BeginTransaction("Delete selected elements"))
            {
                foreach (var element in selectedElements)
                {
                    element.Delete();
                }
            }
            // All deletes are now a single undo operation
        }

        // ---------------------------------------------------------------
        // Snippet: Nested transactions
        // ---------------------------------------------------------------
        public void Snippet_NestedTransactions(IUndoManager undoManager)
        {
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
        }

        // ---------------------------------------------------------------
        // Snippet: Transaction rollback
        // ---------------------------------------------------------------
        public void Snippet_TransactionRollback(UndoManager undoManager)
        {
            var transaction = undoManager.BeginTransaction("Risky operation");
            try
            {
                // Perform changes...
                undoManager.CommitTransaction();  // Explicit commit
            }
            catch
            {
                undoManager.RollbackTransaction();  // Undo all changes
                throw;
            }
        }

        // ---------------------------------------------------------------
        // Snippet: Temporarily disabling undo -- IsUndoEnabled
        // ---------------------------------------------------------------
        public void Snippet_DisableUndoViaIsUndoEnabled(ElementBase element)
        {
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
        }

        // ---------------------------------------------------------------
        // Snippet: Temporarily disabling undo -- via factory function
        // ---------------------------------------------------------------
        public void Snippet_DisableUndoViaFactory()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();
            bool isUndoEnabled = true;

            // Recording is disabled when getUndoManager returns null
            new UndoableStateBridge(
                model,
                () => isUndoEnabled ? um : null,  // null disables recording
                "settings"
            );
        }

        // ---------------------------------------------------------------
        // Snippet: Temporarily disabling undo -- SuspendRecording
        // ---------------------------------------------------------------
        public void Snippet_SuspendRecordingBridge()
        {
            var model = new ExternalModel();
            IUndoManager um = new UndoManager();

            var bridge = new UndoableStateBridge(model, () => um, "settings");

            using (bridge.SuspendRecording())
            {
                // Changes are not recorded
                // Shadow values update on resume
            }
        }

        // ---------------------------------------------------------------
        // Snippet: Configuration -- MaxUndoLevels
        // ---------------------------------------------------------------
        public void Snippet_MaxUndoLevels(UndoManager undoManager)
        {
            undoManager.MaxUndoLevels = 50;  // Default is 100
        }

        // ---------------------------------------------------------------
        // Snippet: Configuration -- Save point tracking
        // ---------------------------------------------------------------
        public void Snippet_SavePointTracking(IUndoManager undoManager)
        {
            // After saving
            undoManager.MarkSavePoint();

            // Check for unsaved changes
            if (undoManager.HasChangedSinceSave)
            {
                // Prompt to save
            }
        }

        // ---------------------------------------------------------------
        // Snippet: Configuration -- Events (StateChanged)
        // ---------------------------------------------------------------
        public void Snippet_StateChangedEvent(IUndoManager undoManager)
        {
            var saveButton = new System.Windows.Controls.Button();
            var undoButton = new System.Windows.Controls.Button();
            var redoButton = new System.Windows.Controls.Button();

            undoManager.StateChanged += (sender, e) =>
            {
                saveButton.IsEnabled = undoManager.HasChangedSinceSave;
                undoButton.IsEnabled = undoManager.CanUndo;
                redoButton.IsEnabled = undoManager.CanRedo;
            };
        }

        // ---------------------------------------------------------------
        // Snippet: Creating Custom Actions -- SwapColumnsAction
        // ---------------------------------------------------------------
        // (see SwapColumnsAction class below)
    }

    // ---------------------------------------------------------------
    // Snippet: Custom IUndoableAction implementation
    // ---------------------------------------------------------------
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
        public object Target => _table;

        public void Execute() => _table.SwapColumns(_colA, _colB);
        public void Undo() => _table.SwapColumns(_colB, _colA);  // Same operation reverses itself

        public bool CanMergeWith(IUndoableAction other) => false;
        public IUndoableAction MergeWith(IUndoableAction other) => this;
    }

    // ---------------------------------------------------------------
    // Snippet: Implementing merge support
    // ---------------------------------------------------------------
    public class SwapColumnsActionWithMerge : IUndoableAction
    {
        private readonly DataTable _table;
        private readonly int _colA;
        private readonly int _colB;

        public SwapColumnsActionWithMerge(DataTable table, int colA, int colB)
        {
            _table = table;
            _colA = colA;
            _colB = colB;
            Timestamp = DateTime.Now;
        }

        public string Description => $"Swap columns {_colA} and {_colB}";
        public DateTime Timestamp { get; }
        public object Target => _table;

        public void Execute() => _table.SwapColumns(_colA, _colB);
        public void Undo() => _table.SwapColumns(_colB, _colA);

        public bool CanMergeWith(IUndoableAction other)
        {
            if (other is not SwapColumnsActionWithMerge sca) return false;
            if (!ReferenceEquals(sca._table, _table)) return false;

            // Merge within 500ms
            var timeDiff = (sca.Timestamp - Timestamp).TotalMilliseconds;
            return timeDiff >= 0 && timeDiff <= 500;
        }

        public IUndoableAction MergeWith(IUndoableAction other)
        {
            // Return a new action that captures the combined effect
            return new SwapColumnsActionWithMerge(_table, _colA, ((SwapColumnsActionWithMerge)other)._colB);
        }
    }
}
