using System.ComponentModel;
using System.Reflection;
using FrameworkInterfaces.Undo.Actions;

namespace FrameworkInterfaces.Undo
{
    /// <summary>
    /// Provides a bridge between objects that implement <see cref="INotifyPropertyChanged"/> and
    /// an <see cref="IUndoManager"/>, automatically creating undo actions for property changes.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class monitors property changes on an object and creates undoable actions that can
    /// restore the previous property values. It's designed for integrating external objects
    /// (such as third-party controls or model objects) with the undo/redo system without
    /// requiring modifications to those objects.
    /// </para>
    /// <para>
    /// The bridge captures property values before and after changes, creating <see cref="PropertyChangeAction"/>
    /// instances that restore the previous value on undo and re-apply the new value on redo.
    /// <see cref="PropertyChangeAction"/> supports time-window merging (500ms) so that rapid changes
    /// to the same property (e.g., typing in a TextBox) are coalesced into a single undo entry.
    /// </para>
    /// <para>
    /// <b>Property Filtering:</b> You can control which properties are monitored by providing
    /// either an inclusion list (only monitor specified properties) or an exclusion list
    /// (monitor all except specified properties).
    /// </para>
    /// <para>
    /// <b>Thread Safety:</b> This class is not thread-safe. Property change events should
    /// originate from the same thread that created the bridge.
    /// </para>
    /// <para>
    /// <b>Authors:</b>
    /// <list type="bullet">
    ///     <item><description>Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil</description></item>
    /// </list>
    /// </para>
    /// </remarks>
    /// <example>
    /// <code>
    /// // Basic usage with an external model object
    /// public class MyElement : ElementBase
    /// {
    ///     private ExternalModel _model;
    ///     private UndoableStateBridge _modelBridge;
    ///
    ///     public MyElement()
    ///     {
    ///         _model = new ExternalModel();
    ///
    ///         // Monitor all property changes on the model
    ///         _modelBridge = new UndoableStateBridge(
    ///             _model,
    ///             () => IsUndoEnabled ? UndoManager : null,
    ///             "model settings",
    ///             this
    ///         );
    ///     }
    /// }
    ///
    /// // Usage with property filtering - only monitor specific properties
    /// _modelBridge = new UndoableStateBridge(
    ///     _model,
    ///     () => IsUndoEnabled ? UndoManager : null,
    ///     "model settings",
    ///     this,
    ///     includedProperties: new[] { "Name", "Value", "IsEnabled" }
    /// );
    ///
    /// // Usage with exclusion list - monitor all except specified properties
    /// _modelBridge = new UndoableStateBridge(
    ///     _model,
    ///     () => IsUndoEnabled ? UndoManager : null,
    ///     "model settings",
    ///     this,
    ///     excludedProperties: new[] { "IsSelected", "IsDirty" }
    /// );
    /// </code>
    /// </example>
    public class UndoableStateBridge : IDisposable
    {
        #region Fields

        /// <summary>
        /// The object being monitored for property changes.
        /// </summary>
        private readonly INotifyPropertyChanged _source;

        /// <summary>
        /// A function that returns the current undo manager, or null if undo is disabled.
        /// Using a delegate allows dynamic checking of IsUndoEnabled.
        /// </summary>
        private readonly Func<IUndoManager?> _getUndoManager;

        /// <summary>
        /// A human-readable description of the source object for use in undo action descriptions.
        /// </summary>
        private readonly string _sourceDescription;

        /// <summary>
        /// The target object that owns this source, used for undo action association.
        /// </summary>
        private readonly object? _target;

        /// <summary>
        /// Set of property names to include in monitoring. If null, all properties are monitored
        /// (subject to exclusion list).
        /// </summary>
        private readonly HashSet<string>? _includedProperties;

        /// <summary>
        /// Set of property names to exclude from monitoring.
        /// </summary>
        private readonly HashSet<string> _excludedProperties;

        /// <summary>
        /// Cache of PropertyInfo objects for the source type, keyed by property name.
        /// </summary>
        private readonly Dictionary<string, PropertyInfo> _propertyCache;

        /// <summary>
        /// Shadow copy of property values, used to capture the "before" state when a property changes.
        /// </summary>
        private readonly Dictionary<string, object?> _shadowValues;

        /// <summary>
        /// Indicates whether this instance has been disposed.
        /// </summary>
        private bool _disposed;

        /// <summary>
        /// Optional callback invoked when a new forward action is recorded to the undo manager.
        /// Not called during undo/redo replay.
        /// </summary>
        private readonly Action? _onActionRecorded;

        /// <summary>
        /// Number of currently active <see cref="RecordingSuspension"/> scopes. Used to
        /// support nested SuspendRecording calls correctly: only the outermost
        /// transition (0→1) actually unsubscribes from PropertyChanged, and only the
        /// outermost dispose (1→0) re-subscribes. Without this, two nested suspensions
        /// would unsubscribe once but re-subscribe twice on dispose, leaving the
        /// handler subscribed twice and recording every property change as TWO
        /// undo actions.
        /// </summary>
        private int _suspendCount;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="UndoableStateBridge"/> class.
        /// </summary>
        /// <param name="source">
        /// The object to monitor for property changes. Must implement <see cref="INotifyPropertyChanged"/>.
        /// </param>
        /// <param name="getUndoManager">
        /// A function that returns the <see cref="IUndoManager"/> to record actions to,
        /// or null if undo recording should be temporarily disabled.
        /// </param>
        /// <param name="sourceDescription">
        /// A human-readable description of the source object (e.g., "chart settings", "plot options").
        /// Used in undo action descriptions like "Change chart settings".
        /// </param>
        /// <param name="target">
        /// Optional. The object that owns this source, used for action association and filtering
        /// in the undo manager.
        /// </param>
        /// <param name="includedProperties">
        /// Optional. If specified, only these property names will be monitored for changes.
        /// Cannot be used together with <paramref name="excludedProperties"/>.
        /// </param>
        /// <param name="excludedProperties">
        /// Optional. Property names to exclude from monitoring. Common exclusions include
        /// "IsSelected", "IsDirty", or other transient state properties.
        /// </param>
        /// <param name="onActionRecorded">
        /// Optional. A callback invoked each time a new forward action is recorded to the undo manager.
        /// This is NOT called during undo/redo replay. Use this to notify the owning element
        /// (e.g., to call <c>SetIsDirty(true)</c>) when a monitored property changes.
        /// </param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="source"/> or <paramref name="getUndoManager"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when both <paramref name="includedProperties"/> and <paramref name="excludedProperties"/>
        /// are specified.
        /// </exception>
        public UndoableStateBridge(
            INotifyPropertyChanged source,
            Func<IUndoManager?> getUndoManager,
            string sourceDescription = "settings",
            object? target = null,
            IEnumerable<string>? includedProperties = null,
            IEnumerable<string>? excludedProperties = null,
            Action? onActionRecorded = null)
        {
            _source = source ?? throw new ArgumentNullException(nameof(source));
            _getUndoManager = getUndoManager ?? throw new ArgumentNullException(nameof(getUndoManager));
            _sourceDescription = sourceDescription ?? "settings";
            _target = target;

            // Validate that both inclusion and exclusion lists aren't specified
            if (includedProperties != null && excludedProperties != null)
            {
                throw new ArgumentException(
                    "Cannot specify both includedProperties and excludedProperties. Use one or the other.",
                    nameof(excludedProperties));
            }

            _includedProperties = includedProperties != null
                ? new HashSet<string>(includedProperties, StringComparer.Ordinal)
                : null;

            _excludedProperties = excludedProperties != null
                ? new HashSet<string>(excludedProperties, StringComparer.Ordinal)
                : new HashSet<string>(StringComparer.Ordinal);

            _onActionRecorded = onActionRecorded;

            // Build property cache for the source type
            _propertyCache = new Dictionary<string, PropertyInfo>(StringComparer.Ordinal);
            foreach (var prop in source.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance))
            {
                if (prop.CanRead && prop.CanWrite)
                {
                    _propertyCache[prop.Name] = prop;
                }
            }

            // Initialize shadow values with current property values
            _shadowValues = new Dictionary<string, object?>(StringComparer.Ordinal);
            UpdateShadowValues();

            // Subscribe to property changes
            _source.PropertyChanged += OnPropertyChanged;
        }

        #endregion

        #region Properties

        /// <summary>
        /// Gets the object being monitored by this bridge.
        /// </summary>
        /// <value>
        /// The <see cref="INotifyPropertyChanged"/> object that this bridge is attached to.
        /// </value>
        public INotifyPropertyChanged Source => _source;

        /// <summary>
        /// Gets the description used for this source in undo action descriptions.
        /// </summary>
        /// <value>
        /// A human-readable description of the source object.
        /// </value>
        public string SourceDescription => _sourceDescription;

        /// <summary>
        /// Gets a value indicating whether this bridge has been disposed.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance has been disposed; otherwise, <c>false</c>.
        /// </value>
        public bool IsDisposed => _disposed;

        #endregion

        #region Event Handlers

        /// <summary>
        /// Handles the <see cref="INotifyPropertyChanged.PropertyChanged"/> event
        /// by creating and recording appropriate undo actions.
        /// </summary>
        /// <param name="sender">The source of the event (the monitored object).</param>
        /// <param name="e">
        /// The <see cref="PropertyChangedEventArgs"/> containing the name of the property that changed.
        /// </param>
        private void OnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (_disposed) return;
            if (string.IsNullOrEmpty(e.PropertyName)) return;

            // Check if we should monitor this property
            if (!ShouldMonitorProperty(e.PropertyName)) return;

            // Get the undo manager
            var undoManager = _getUndoManager();

            // If no undo manager or currently executing an undo/redo, just update shadow
            if (undoManager == null || undoManager.IsExecutingAction)
            {
                UpdateShadowValue(e.PropertyName);
                return;
            }

            // Create and record the undo action
            var action = CreatePropertyChangeAction(e.PropertyName);
            if (action != null)
            {
                undoManager.RecordAction(action);
                _onActionRecorded?.Invoke();
            }

            // Update shadow value to reflect the new state
            UpdateShadowValue(e.PropertyName);
        }

        #endregion

        #region Private Methods

        /// <summary>
        /// Determines whether the specified property should be monitored for changes.
        /// </summary>
        /// <param name="propertyName">The name of the property to check.</param>
        /// <returns>
        /// <c>true</c> if the property should be monitored; otherwise, <c>false</c>.
        /// </returns>
        private bool ShouldMonitorProperty(string propertyName)
        {
            // Must be in the property cache (readable and writable)
            if (!_propertyCache.ContainsKey(propertyName)) return false;

            // If inclusion list exists, property must be in it
            if (_includedProperties != null)
            {
                return _includedProperties.Contains(propertyName);
            }

            // Otherwise, property must not be in exclusion list
            return !_excludedProperties.Contains(propertyName);
        }

        /// <summary>
        /// Creates an undo action for a property change.
        /// </summary>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <returns>
        /// A <see cref="PropertyChangeAction"/> that restores the old value on undo and applies
        /// the new value on redo, or null if the values are equal or the property is invalid.
        /// <see cref="PropertyChangeAction"/> supports time-window merging (500ms) so that rapid
        /// changes to the same property (e.g., typing in a TextBox) are coalesced into a single
        /// undo entry.
        /// </returns>
        private IUndoableAction? CreatePropertyChangeAction(string propertyName)
        {
            if (!_propertyCache.TryGetValue(propertyName, out var propertyInfo))
            {
                return null;
            }

            // Get the old value from shadow copy
            _shadowValues.TryGetValue(propertyName, out var oldValue);

            // Get the current (new) value
            object? newValue;
            try
            {
                newValue = propertyInfo.GetValue(_source);
            }
            catch (System.Reflection.TargetInvocationException)
            {
                return null;
            }
            catch (Exception ex) when (ex is InvalidOperationException or ObjectDisposedException)
            {
                return null;
            }

            // Don't create action if values are equal
            if (Equals(oldValue, newValue))
            {
                return null;
            }

            // Use PropertyChangeAction which supports time-window merging (500ms)
            // for coalescing rapid changes like typing in a TextBox.
            return new PropertyChangeAction(_source, propertyName, oldValue, newValue);
        }

        /// <summary>
        /// Updates the shadow copy for a specific property with its current value.
        /// </summary>
        /// <param name="propertyName">The name of the property to update.</param>
        private void UpdateShadowValue(string propertyName)
        {
            if (!_propertyCache.TryGetValue(propertyName, out var propertyInfo))
            {
                return;
            }

            try
            {
                var value = propertyInfo.GetValue(_source);
                _shadowValues[propertyName] = value;
            }
            catch (Exception ex)
            {
                // Property getter may throw; just skip this property
                System.Diagnostics.Debug.WriteLine($"[UndoableStateBridge] Failed to read property: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates the shadow copy with current values for all monitored properties.
        /// </summary>
        private void UpdateShadowValues()
        {
            foreach (var kvp in _propertyCache)
            {
                if (ShouldMonitorProperty(kvp.Key))
                {
                    try
                    {
                        var value = kvp.Value.GetValue(_source);
                        _shadowValues[kvp.Key] = value;
                    }
                    catch (Exception ex)
                    {
                        // Property getter may throw; just skip this property
                        System.Diagnostics.Debug.WriteLine($"[UndoableStateBridge] Failed to read property: {ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region Public Methods

        /// <summary>
        /// Temporarily suspends undo recording for property changes.
        /// </summary>
        /// <returns>
        /// An <see cref="IDisposable"/> that, when disposed, resumes undo recording and
        /// updates the shadow values to the current state.
        /// </returns>
        /// <remarks>
        /// <para>
        /// Use this method when you need to make multiple property changes that should
        /// not be recorded as individual undo actions. When the returned disposable is
        /// disposed, the shadow values are updated to reflect the current state, effectively
        /// "accepting" all changes made during the suspension.
        /// </para>
        /// <para>
        /// This is useful during initialization, batch updates, or when loading data.
        /// </para>
        /// </remarks>
        /// <example>
        /// <code>
        /// using (bridge.SuspendRecording())
        /// {
        ///     model.Property1 = value1;
        ///     model.Property2 = value2;
        ///     model.Property3 = value3;
        /// }
        /// // Shadow values are now updated; future changes will be recorded
        /// </code>
        /// </example>
        public IDisposable SuspendRecording()
        {
            return new RecordingSuspension(this);
        }

        /// <summary>
        /// Adds a property to the exclusion list, preventing it from being monitored.
        /// </summary>
        /// <param name="propertyName">The name of the property to exclude.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an inclusion list is being used instead of an exclusion list.
        /// </exception>
        public void ExcludeProperty(string propertyName)
        {
            if (_includedProperties != null)
            {
                throw new InvalidOperationException(
                    "Cannot exclude properties when using an inclusion list. Remove the property from the inclusion list instead.");
            }

            _excludedProperties.Add(propertyName);
            _shadowValues.Remove(propertyName);
        }

        /// <summary>
        /// Removes a property from the exclusion list, allowing it to be monitored.
        /// </summary>
        /// <param name="propertyName">The name of the property to include.</param>
        /// <exception cref="InvalidOperationException">
        /// Thrown when an inclusion list is being used instead of an exclusion list.
        /// </exception>
        public void IncludeProperty(string propertyName)
        {
            if (_includedProperties != null)
            {
                throw new InvalidOperationException(
                    "Cannot modify exclusion list when using an inclusion list. Add the property to the inclusion list instead.");
            }

            if (_excludedProperties.Remove(propertyName))
            {
                UpdateShadowValue(propertyName);
            }
        }

        /// <summary>
        /// Refreshes the shadow values to match the current property values.
        /// </summary>
        /// <remarks>
        /// Call this method after making changes that should not be undoable,
        /// such as during initialization or after loading data.
        /// </remarks>
        public void RefreshShadowValues()
        {
            UpdateShadowValues();
        }

        #endregion

        #region IDisposable Implementation

        /// <summary>
        /// Releases all resources used by this <see cref="UndoableStateBridge"/>.
        /// </summary>
        /// <remarks>
        /// This method unsubscribes from the source object's <see cref="INotifyPropertyChanged.PropertyChanged"/>
        /// event and clears internal state.
        /// </remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases the unmanaged resources used by this <see cref="UndoableStateBridge"/>
        /// and optionally releases the managed resources.
        /// </summary>
        /// <param name="disposing">
        /// <c>true</c> to release both managed and unmanaged resources;
        /// <c>false</c> to release only unmanaged resources.
        /// </param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    // Unsubscribe from property changes
                    _source.PropertyChanged -= OnPropertyChanged;

                    // Clear internal state
                    _shadowValues.Clear();
                    _propertyCache.Clear();
                }

                _disposed = true;
            }
        }

        #endregion

        #region Nested Types

        /// <summary>
        /// A disposable helper class that temporarily suspends undo recording.
        /// </summary>
        private class RecordingSuspension : IDisposable
        {
            private readonly UndoableStateBridge _bridge;
            private bool _disposed;

            /// <summary>
            /// Initializes a new instance of the <see cref="RecordingSuspension"/> class.
            /// </summary>
            /// <param name="bridge">The bridge to suspend recording on.</param>
            public RecordingSuspension(UndoableStateBridge bridge)
            {
                _bridge = bridge;
                // Reference-counted: only the outermost suspension actually unsubscribes.
                // Inner SuspendRecording calls just bump the counter so a Dispose pair
                // does not double-subscribe on exit.
                if (System.Threading.Interlocked.Increment(ref _bridge._suspendCount) == 1)
                {
                    _bridge._source.PropertyChanged -= _bridge.OnPropertyChanged;
                }
            }

            /// <summary>
            /// Resumes undo recording and updates shadow values.
            /// </summary>
            /// <remarks>
            /// If the owning bridge has already been disposed, this skips the shadow-value refresh
            /// and re-subscription — touching the cleared caches would be wasted work, and
            /// re-subscribing would resurrect a handler on a disposed bridge.
            /// </remarks>
            public void Dispose()
            {
                if (!_disposed)
                {
                    _disposed = true;
                    GC.SuppressFinalize(this);

                    if (_bridge.IsDisposed)
                    {
                        // Bridge is gone — decrement the counter for accounting but
                        // skip the shadow-value refresh and re-subscription (would
                        // resurrect a handler on a disposed bridge).
                        System.Threading.Interlocked.Decrement(ref _bridge._suspendCount);
                        return;
                    }

                    // Reference-counted: only the outermost dispose (1→0) actually
                    // re-subscribes. Inner disposes just decrement the counter.
                    if (System.Threading.Interlocked.Decrement(ref _bridge._suspendCount) == 0)
                    {
                        // Update shadow values to current state
                        _bridge.UpdateShadowValues();

                        // Re-subscribe to events
                        _bridge._source.PropertyChanged += _bridge.OnPropertyChanged;
                    }
                }
            }
        }

        #endregion
    }
}
