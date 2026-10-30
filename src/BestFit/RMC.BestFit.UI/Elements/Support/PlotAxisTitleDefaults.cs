using System;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.Data.Converters;
using OxyPlot.Wpf;

// XGoodFit port: Avalonia's binding API. A binding is cleared by disposing its expression, which
// removes it, as WPF's BindingOperations.ClearBinding did; clearing the value alone would leave it
// bound. WPF also clears a one-way binding when its target is given a local value, which is how
// the user's edit of a bound title (in the plot properties editor) detaches the axis; Avalonia keeps
// the binding, which would put the label back at its next change. So TitleBinding stops the binding
// from setting the title again once anything else sets it, and IsDefaultManaged counts such a
// released binding as cleared. (Disposing it would reset the title.)

namespace RMC.BestFit.UI
{
    /// <summary>
    /// Applies data-label default titles to plot axes without overwriting user-customized titles.
    /// </summary>
    /// <remarks>
    /// <para>
    ///     Authors:
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </para>
    /// <para>
    /// This helper is intentionally scoped to axes whose titles are driven by
    /// <see cref="InputData.UnitLabel"/>, <see cref="InputData.IndexLabel"/>, or
    /// <see cref="TimeSeriesElement.UnitLabel"/>. It preserves the beta.3 behavior where
    /// source-label edits update axes that still contain their automatic default title, while
    /// axes edited by the user remain detached from the source label.
    /// </para>
    /// </remarks>
    public static class PlotAxisTitleDefaults
    {
        /// <summary>
        /// Stores the most recent automatic title applied to each axis.
        /// </summary>
        /// <remarks>
        /// A weak table avoids extending the lifetime of WPF axis objects owned by plots.
        /// </remarks>
        private static readonly ConditionalWeakTable<Axis, TitleState> AxisStates = new ConditionalWeakTable<Axis, TitleState>();

        /// <summary>
        /// XGoodFit port: the label binding of each bound axis (see <see cref="TitleBinding"/>).
        /// </summary>
        private static readonly ConditionalWeakTable<Axis, TitleBinding> TitleBindings = new ConditionalWeakTable<Axis, TitleBinding>();

        /// <summary>
        /// Determines whether an axis title is still managed by an automatic data-label default.
        /// </summary>
        /// <param name="axis">The axis whose title should be evaluated.</param>
        /// <param name="currentDefaultTitle">The automatic title that should be applied now.</param>
        /// <param name="previousDefaultTitle">The automatic title that was valid before the source label changed.</param>
        /// <returns><c>true</c> when the axis can safely receive a new default title; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="axis"/> is null.</exception>
        /// <remarks>
        /// Blank titles are treated as a request to resume default management. Active WPF bindings
        /// are also default-managed because they were created by the App-side label binding path.
        /// </remarks>
        public static bool IsDefaultManaged(Axis axis, string currentDefaultTitle, string previousDefaultTitle = null)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));

            if (BindingOperations.GetBindingExpressionBase(axis, Axis.TitleProperty) != null
                && !(TitleBindings.TryGetValue(axis, out var titleBinding) && titleBinding.IsReleased))
            {
                return true;
            }

            string title = axis.Title;
            if (string.IsNullOrWhiteSpace(title))
            {
                return true;
            }

            if (TitleEquals(title, currentDefaultTitle) || TitleEquals(title, previousDefaultTitle))
            {
                return true;
            }

            return AxisStates.TryGetValue(axis, out var state) && TitleEquals(title, state.AutomaticTitle);
        }

        /// <summary>
        /// Sets a literal default title when the axis has not been customized by the user.
        /// </summary>
        /// <param name="axis">The axis whose title should be updated.</param>
        /// <param name="title">The automatic title to apply.</param>
        /// <param name="previousDefaultTitle">The automatic title that was valid before the source label changed.</param>
        /// <returns><c>true</c> when the title was applied; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="axis"/> is null.</exception>
        /// <remarks>
        /// Existing bindings are cleared only when the axis is still default-managed. Custom titles
        /// are left untouched.
        /// </remarks>
        public static bool SetTitleIfDefault(Axis axis, string title, string previousDefaultTitle = null)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));

            title ??= string.Empty;
            if (!IsDefaultManaged(axis, title, previousDefaultTitle))
            {
                return false;
            }

            if (TitleBindings.TryGetValue(axis, out var titleBinding)) titleBinding.Detach();
            BindingOperations.GetBindingExpressionBase(axis, Axis.TitleProperty)?.Dispose();
            axis.Title = title;
            RememberAutomaticTitle(axis, title);
            return true;
        }

        /// <summary>
        /// Binds an axis title to a source label when the axis has not been customized by the user.
        /// </summary>
        /// <param name="axis">The axis whose title should be bound.</param>
        /// <param name="source">The source object that exposes the label property.</param>
        /// <param name="sourcePropertyName">The label property name on <paramref name="source"/>.</param>
        /// <param name="currentDefaultTitle">The automatic title expected from the new binding.</param>
        /// <param name="previousDefaultTitle">The automatic title that was valid before the source label changed.</param>
        /// <param name="stringFormat">Optional WPF binding string format, such as <c>Marginal X - {0}</c>.</param>
        /// <returns><c>true</c> when the binding was applied; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown when <paramref name="axis"/> or <paramref name="source"/> is null.
        /// </exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="sourcePropertyName"/> is empty.</exception>
        /// <remarks>
        /// The binding is one-way because axis titles should follow source metadata until the user
        /// customizes the axis. Editing the axis title replaces the binding and detaches the axis.
        /// </remarks>
        public static bool BindTitleIfDefault(
            Axis axis,
            object source,
            string sourcePropertyName,
            string currentDefaultTitle,
            string previousDefaultTitle = null,
            string stringFormat = null)
        {
            if (axis == null) throw new ArgumentNullException(nameof(axis));
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrWhiteSpace(sourcePropertyName))
                throw new ArgumentException("The source property name cannot be empty.", nameof(sourcePropertyName));

            currentDefaultTitle ??= string.Empty;
            if (!IsDefaultManaged(axis, currentDefaultTitle, previousDefaultTitle))
            {
                return false;
            }

            // XGoodFit port: the binding's converter formats the label (as StringFormat would) and
            // notes each title it gives the axis, so that a title set by anything else releases the
            // binding, as WPF's local value cleared it.
            if (TitleBindings.TryGetValue(axis, out var previous)) previous.Detach();
            var titleBinding = new TitleBinding(axis, stringFormat);
            var binding = new Binding(sourcePropertyName)
            {
                Source = source,
                Mode = BindingMode.OneWay,
                UpdateSourceTrigger = UpdateSourceTrigger.PropertyChanged,
                Converter = titleBinding
            };

            axis.Bind(Axis.TitleProperty, binding);
            titleBinding.Attach();
            RememberAutomaticTitle(axis, currentDefaultTitle);
            return true;
        }

        /// <summary>
        /// Compares axis titles using the exact text shown to the user.
        /// </summary>
        /// <param name="left">The first title to compare.</param>
        /// <param name="right">The second title to compare.</param>
        /// <returns><c>true</c> when the titles are identical; otherwise, <c>false</c>.</returns>
        /// <remarks>
        /// Ordinal comparison avoids culture-dependent surprises in persisted plot labels.
        /// </remarks>
        private static bool TitleEquals(string left, string right)
        {
            return string.Equals(left ?? string.Empty, right ?? string.Empty, StringComparison.Ordinal);
        }

        /// <summary>
        /// Records the automatic title most recently applied to an axis.
        /// </summary>
        /// <param name="axis">The axis that received an automatic title.</param>
        /// <param name="title">The automatic title applied to the axis.</param>
        /// <remarks>
        /// The remembered title lets later refreshes recognize the axis as default-managed even
        /// after an App control switches between literal defaults and WPF bindings.
        /// </remarks>
        private static void RememberAutomaticTitle(Axis axis, string title)
        {
            AxisStates.GetOrCreateValue(axis).AutomaticTitle = title ?? string.Empty;
        }

        /// <summary>
        /// XGoodFit port: a label binding of an axis title that stops, as WPF's one-way binding was
        /// cleared, when anything else sets the title.
        /// </summary>
        /// <remarks>
        /// As the binding's converter, it formats the label (WPF's StringFormat) and notes the title it
        /// gives the axis. After a title change it didn't make (the plot properties editor, undo,
        /// loading plot settings) it's released: it gives the axis no more titles, and
        /// <see cref="IsDefaultManaged"/> doesn't count it. It isn't disposed, because disposing an
        /// Avalonia binding resets the title it holds. One difference remains: setting the title to the
        /// text it already has raises no change, so it doesn't release the binding, where WPF's local
        /// value would have cleared it.
        /// </remarks>
        private sealed class TitleBinding : IValueConverter
        {
            private readonly Axis _axis;
            private readonly string _stringFormat;
            private bool _expectingTitle;
            private object _expectedTitle;

            /// <summary>Whether a title set by something else has released the binding.</summary>
            public bool IsReleased { get; private set; }

            /// <summary>Creates the binding's converter for an axis.</summary>
            /// <param name="axis">The bound axis.</param>
            /// <param name="stringFormat">The format of the title, or null for the label itself.</param>
            public TitleBinding(Axis axis, string stringFormat)
            {
                _axis = axis;
                _stringFormat = stringFormat;
            }

            /// <summary>Starts watching the axis's title for changes the binding didn't make.</summary>
            public void Attach()
            {
                TitleBindings.AddOrUpdate(_axis, this);
                // Avalonia's property change event, which the axis's own INotifyPropertyChanged event hides.
                ((AvaloniaObject)_axis).PropertyChanged += Axis_PropertyChanged;
            }

            /// <summary>Stops watching the title and forgets the binding, which the caller replaces or clears.</summary>
            public void Detach()
            {
                IsReleased = true;
                ((AvaloniaObject)_axis).PropertyChanged -= Axis_PropertyChanged;
                if (TitleBindings.TryGetValue(_axis, out var current) && ReferenceEquals(current, this))
                    TitleBindings.Remove(_axis);
            }

            private void Axis_PropertyChanged(object sender, AvaloniaPropertyChangedEventArgs e)
            {
                if (e.Property != Axis.TitleProperty) return;
                if (_expectingTitle && Equals(e.NewValue, _expectedTitle))
                {
                    _expectingTitle = false;
                    return;
                }
                IsReleased = true;
                ((AvaloniaObject)_axis).PropertyChanged -= Axis_PropertyChanged;
            }

            /// <inheritdoc/>
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (IsReleased) return BindingOperations.DoNothing;
                object title = !string.IsNullOrEmpty(_stringFormat) ? string.Format(culture, _stringFormat, value) : value;
                _expectingTitle = true;
                _expectedTitle = title;
                return title;
            }

            /// <inheritdoc/>
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) =>
                BindingOperations.DoNothing;
        }

        /// <summary>
        /// Holds weakly associated automatic-title state for a single axis.
        /// </summary>
        /// <remarks>
        /// Instances are owned by <see cref="AxisStates"/> and disappear when the axis is collected.
        /// </remarks>
        private sealed class TitleState
        {
            /// <summary>
            /// Gets or sets the last automatic title applied through this helper.
            /// </summary>
            /// <remarks>
            /// An empty value means no automatic title has been recorded yet.
            /// </remarks>
            public string AutomaticTitle { get; set; } = string.Empty;
        }
    }
}
