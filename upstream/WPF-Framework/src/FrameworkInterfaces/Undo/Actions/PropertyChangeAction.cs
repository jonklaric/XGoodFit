using System;
using System.Diagnostics;
using System.Reflection;

namespace FrameworkInterfaces.Undo.Actions
{
    /// <summary>
    /// Represents a property value change that can be undone.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This action captures the old and new values of a property change
    /// and can restore the old value on undo.
    /// </para>
    /// <para>
    /// Supports merging of rapid changes to the same property (within a configurable
    /// time window) to avoid creating too many undo entries during typing.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public class PropertyChangeAction : IUndoableAction
    {
        #region Fields

        private readonly object _target;
        private readonly string _propertyName;
        private readonly object? _oldValue;
        private object? _newValue;
        private readonly PropertyInfo _propertyInfo;
        private readonly object _syncLock = new object();

        // Monotonic timestamp for the merge-window check. Captured separately from
        // the public DateTime Timestamp so the merge logic is immune to system clock
        // adjustments (NTP corrections, DST transitions). Stopwatch.GetTimestamp() is
        // a high-resolution monotonic counter that never moves backward.
        private readonly long _monotonicTicks;

        private static int _mergeWindowMilliseconds = 500;

        /// <summary>
        /// Gets or sets the time window in milliseconds for merging rapid changes to the same property.
        /// Must be greater than or equal to zero. Default is 500 ms.
        /// </summary>
        /// <remarks>
        /// <para>
        /// The merge-window comparison in <see cref="CanMergeWith(IUndoableAction)"/> is
        /// computed against <see cref="System.Diagnostics.Stopwatch.GetTimestamp"/>, a
        /// high-resolution monotonic counter — <i>not</i> against
        /// <see cref="System.DateTime.Now"/>. This makes the merge logic immune to system
        /// clock adjustments (NTP corrections, manual time changes, DST transitions). A
        /// wall-clock jump can never disable a legitimate merge or, worse, accept a stale
        /// merge whose true elapsed time exceeds this window.
        /// </para>
        /// <para>
        /// The public <see cref="Timestamp"/> property remains a <see cref="System.DateTime"/>
        /// for display purposes; only the merge-window calculation uses the monotonic ticks.
        /// </para>
        /// </remarks>
        public static int MergeWindowMilliseconds
        {
            get => _mergeWindowMilliseconds;
            set => _mergeWindowMilliseconds = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value), "MergeWindowMilliseconds must be >= 0.");
        }

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the <see cref="PropertyChangeAction"/> class.
        /// </summary>
        /// <param name="target">The object whose property changed.</param>
        /// <param name="propertyName">The name of the property that changed.</param>
        /// <param name="oldValue">The previous value of the property.</param>
        /// <param name="newValue">The new value of the property.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="target"/> or <paramref name="propertyName"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">
        /// Thrown when the property is not found on the target object.
        /// </exception>
        public PropertyChangeAction(object target, string propertyName, object? oldValue, object? newValue)
        {
            _target = target ?? throw new ArgumentNullException(nameof(target));
            _propertyName = propertyName ?? throw new ArgumentNullException(nameof(propertyName));
            _oldValue = oldValue;
            _newValue = newValue;

            var propertyInfo = target.GetType().GetProperty(propertyName);
            if (propertyInfo == null)
            {
                throw new ArgumentException($"Property '{propertyName}' not found on type '{target.GetType().Name}'", nameof(propertyName));
            }
            _propertyInfo = propertyInfo;

            _monotonicTicks = Stopwatch.GetTimestamp();
            Timestamp = DateTime.Now;
        }

        #endregion

        #region Properties

        /// <inheritdoc/>
        public string Description
        {
            get
            {
                // Try to get a meaningful name from the target
                string? targetName = null;
                if (_target is IElement element)
                {
                    targetName = element.DisplayName;
                }
                else if (_target is IMetaData metaData)
                {
                    targetName = metaData.Name;
                }

                if (!string.IsNullOrEmpty(targetName))
                {
                    return $"Change {_propertyName} on {targetName}";
                }

                return $"Change {_propertyName}";
            }
        }

        /// <inheritdoc/>
        public DateTime Timestamp { get; private set; }

        /// <inheritdoc/>
        public object Target => _target;

        /// <summary>
        /// Gets the name of the property that changed.
        /// </summary>
        public string PropertyName => _propertyName;

        /// <summary>
        /// Gets the old value of the property.
        /// </summary>
        public object? OldValue => _oldValue;

        /// <summary>
        /// Gets the new value of the property.
        /// </summary>
        public object? NewValue => _newValue;

        #endregion

        #region Public Methods

        /// <inheritdoc/>
        public void Execute()
        {
            lock (_syncLock)
            {
                _propertyInfo.SetValue(_target, _newValue);
            }
        }

        /// <inheritdoc/>
        public void Undo()
        {
            lock (_syncLock)
            {
                _propertyInfo.SetValue(_target, _oldValue);
            }
        }

        /// <inheritdoc/>
        public bool CanMergeWith(IUndoableAction other)
        {
            // Only merge with other PropertyChangeActions
            if (!(other is PropertyChangeAction pca)) return false;

            // Must be same target and property
            if (!ReferenceEquals(pca._target, _target)) return false;
            if (pca._propertyName != _propertyName) return false;

            // Must be within the merge window. Use the monotonic counter rather than
            // wall-clock DateTime so NTP corrections / DST transitions don't disable
            // legitimate merges (or, worse, accept stale ones if the clock jumps).
            var deltaTicks = pca._monotonicTicks - _monotonicTicks;
            if (deltaTicks < 0) return false;
            var deltaMs = (deltaTicks * 1000.0) / Stopwatch.Frequency;
            if (deltaMs > MergeWindowMilliseconds) return false;

            return true;
        }

        /// <inheritdoc/>
        public IUndoableAction MergeWith(IUndoableAction other)
        {
            if (!(other is PropertyChangeAction pca)) return this;

            // Keep old value from this action, new value from other action.
            // The new instance's monotonic timestamp captures "now" in its ctor, but
            // we override Timestamp (DateTime) to match the other action so the
            // user-visible timestamp shows the latest sub-edit.
            return new PropertyChangeAction(_target, _propertyName, _oldValue, pca._newValue)
            {
                Timestamp = pca.Timestamp // Use the later timestamp
            };
        }

        #endregion
    }
}
