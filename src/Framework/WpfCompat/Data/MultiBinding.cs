using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Avalonia;
using Avalonia.Data;
using Avalonia.Markup.Xaml;
using Avalonia.Metadata;

namespace WpfCompat.Data;

/// <summary>
/// WPF's <c>MultiBinding</c> with its two-way conversion: the target takes the converted values of
/// several bindings and, when the target changes, its converter's <c>ConvertBack</c> updates the
/// two-way ones. Write it where WPF's goes:
/// <c>&lt;x:Y.Value&gt;&lt;wpfd:MultiBinding Converter="…" Mode="TwoWay"&gt;&lt;Binding …/&gt;…&lt;/wpfd:MultiBinding&gt;&lt;/x:Y.Value&gt;</c>,
/// with <c>xmlns:wpfd="using:WpfCompat.Data"</c>.
/// </summary>
/// <remarks>
/// <para>
/// Avalonia's <c>MultiBinding</c> is one-way. This one binds each of its bindings to a hidden attached
/// property of the target element, so they resolve names and data contexts as bindings on the element
/// itself do, and gives the target a binding to an object that holds the converted value:
/// </para>
/// <list type="bullet">
/// <item>When a binding's value changes, the converter's <see cref="IMultiValueConverter.Convert"/>
/// gives the target its new value.</item>
/// <item>When the target changes otherwise, as when the user edits it, <see cref="IMultiValueConverter.ConvertBack"/>
/// gives each binding a value, and the bindings that are two-way write theirs to their sources. A
/// binding whose mode is Default takes the multi-binding's, as in WPF. A value of
/// <see cref="BindingOperations.DoNothing"/> leaves its binding as it is.</item>
/// <item>The converter's result <see cref="AvaloniaProperty.UnsetValue"/> gives the target the
/// <see cref="FallbackValue"/>, and null the <see cref="TargetNullValue"/>, when they're set.</item>
/// </list>
/// <para>
/// The target types passed to <c>ConvertBack</c> are those of the bindings' current values (WPF passes
/// their source properties' types). Conversions use <see cref="ConverterCulture"/>, or WpfCompat's
/// <see cref="FormattingCulture"/> as WPF's bindings use en-US.
/// </para>
/// <para>XGoodFit addition, for upstream's two-way multi-bindings (OxyPlotControls' colour editors).</para>
/// </remarks>
public sealed class MultiBinding
{
    /// <summary>The bindings whose values the converter combines.</summary>
    [Content]
    public IList<BindingBase> Bindings { get; } = new List<BindingBase>();

    /// <summary>The converter: WPF's two-way <see cref="IMultiValueConverter"/>.</summary>
    public IMultiValueConverter? Converter { get; set; }

    /// <summary>The parameter passed to the converter.</summary>
    public object? ConverterParameter { get; set; }

    /// <summary>The culture passed to the converter; WpfCompat's <see cref="FormattingCulture"/> when null.</summary>
    public CultureInfo? ConverterCulture { get; set; }

    /// <summary>The direction: OneWay or TwoWay (Default is the target property's default).</summary>
    public BindingMode Mode { get; set; }

    /// <summary>When the target updates the sources (the target binding's trigger).</summary>
    public UpdateSourceTrigger UpdateSourceTrigger { get; set; }

    /// <summary>The target's value when the converter returns <see cref="AvaloniaProperty.UnsetValue"/>.</summary>
    public object? FallbackValue { get; set; } = AvaloniaProperty.UnsetValue;

    /// <summary>The target's value when the converter returns null.</summary>
    public object? TargetNullValue { get; set; } = AvaloniaProperty.UnsetValue;

    /// <summary>Binds the bindings to the element being loaded, and returns the binding for its property.</summary>
    public BindingBase ProvideValue(IServiceProvider serviceProvider)
    {
        var target = serviceProvider.GetService(typeof(IProvideValueTarget)) as IProvideValueTarget;
        if (target?.TargetObject is not AvaloniaObject element || target.TargetProperty is not AvaloniaProperty property)
        {
            throw new InvalidOperationException("WpfCompat's MultiBinding can only set an Avalonia property.");
        }
        return CreateBinding(element, property);
    }

    /// <summary>
    /// Binds the bindings to <paramref name="target"/> and returns the binding to give its
    /// <paramref name="property"/>, as WPF's <c>BindingOperations.SetBinding(target, property, multiBinding)</c>
    /// does in one step: <c>target.Bind(property, multiBinding.CreateBinding(target, property))</c>.
    /// </summary>
    public BindingBase CreateBinding(AvaloniaObject target, AvaloniaProperty property)
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(property);
        BindingMode mode = Mode == BindingMode.Default ? property.GetMetadata(target.GetType()).DefaultBindingMode : Mode;
        var expression = new Expression(this, target, property.PropertyType, mode);
        return new Avalonia.Data.Binding(nameof(Expression.Value))
        {
            Source = expression,
            Mode = mode,
            UpdateSourceTrigger = UpdateSourceTrigger,
        };
    }

    // The converted value, which the target binds to, and the bindings' values, from hidden attached
    // properties of the target.
    private sealed class Expression : INotifyPropertyChanged
    {
        private readonly MultiBinding _binding;
        private readonly AvaloniaObject _target;
        private readonly Type _targetType;
        private readonly AttachedProperty<object?>[] _slots;
        private readonly BindingMode[] _modes;
        private readonly object?[] _values;
        private readonly bool _twoWay;
        private object? _value;
        private bool _updatingSources;

        public Expression(MultiBinding binding, AvaloniaObject target, Type targetType, BindingMode mode)
        {
            _binding = binding;
            _target = target;
            _targetType = targetType;
            _twoWay = mode is BindingMode.TwoWay or BindingMode.OneWayToSource;
            int count = binding.Bindings.Count;
            _slots = Slots.Allocate(target, count);
            _modes = new BindingMode[count];
            _values = new object?[count];
            Array.Fill(_values, AvaloniaProperty.UnsetValue);
            target.PropertyChanged += OnTargetPropertyChanged;
            for (int i = 0; i < count; i++)
            {
                BindingBase child = binding.Bindings[i];
                if (child is ReflectionBinding reflection)
                {
                    if (reflection.Mode == BindingMode.Default)
                    {
                        reflection.Mode = mode;
                    }
                    _modes[i] = reflection.Mode;
                }
                else if (child is CompiledBinding compiled)
                {
                    if (compiled.Mode == BindingMode.Default)
                    {
                        compiled.Mode = mode;
                    }
                    _modes[i] = compiled.Mode;
                }
                target.Bind(_slots[i], child);
            }
            _value = Convert();
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        public object? Value
        {
            get => _value;
            set
            {
                _value = value;
                UpdateSources(value);
            }
        }

        private CultureInfo Culture => _binding.ConverterCulture ?? FormattingCulture.Current;

        private void OnTargetPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
        {
            int index = Array.IndexOf(_slots, e.Property);
            if (index < 0)
            {
                return;
            }
            _values[index] = e.NewValue;
            if (_updatingSources)
            {
                return;
            }
            object? value = Convert();
            if (value == BindingOperations.DoNothing)
            {
                return;
            }
            _value = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(nameof(Value)));
        }

        private object? Convert()
        {
            if (_binding.Converter is not { } converter)
            {
                return AvaloniaProperty.UnsetValue;
            }
            object? value = converter.Convert(_values.ToArray(), _targetType, _binding.ConverterParameter, Culture);
            if (value == AvaloniaProperty.UnsetValue && _binding.FallbackValue != AvaloniaProperty.UnsetValue)
            {
                return _binding.FallbackValue;
            }
            if (value is null && _binding.TargetNullValue != AvaloniaProperty.UnsetValue)
            {
                return _binding.TargetNullValue;
            }
            return value;
        }

        private void UpdateSources(object? value)
        {
            if (!_twoWay || _binding.Converter is not { } converter)
            {
                return;
            }
            Type[] types = _values.Select(v => v?.GetType() ?? typeof(object)).ToArray();
            object?[]? values = converter.ConvertBack(value!, types, _binding.ConverterParameter, Culture);
            if (values is null)
            {
                return;
            }
            _updatingSources = true;
            try
            {
                for (int i = 0; i < Math.Min(values.Length, _slots.Length); i++)
                {
                    if (values[i] == BindingOperations.DoNothing || _modes[i] is not (BindingMode.TwoWay or BindingMode.OneWayToSource))
                    {
                        continue;
                    }
                    _target.SetValue(_slots[i], values[i]);
                }
            }
            finally
            {
                _updatingSources = false;
            }
        }
    }

    // The hidden attached properties the bindings bind to: each multi-binding on an element takes the
    // next free ones.
    private static class Slots
    {
        private static readonly AttachedProperty<object?> Slot0 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot0));
        private static readonly AttachedProperty<object?> Slot1 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot1));
        private static readonly AttachedProperty<object?> Slot2 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot2));
        private static readonly AttachedProperty<object?> Slot3 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot3));
        private static readonly AttachedProperty<object?> Slot4 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot4));
        private static readonly AttachedProperty<object?> Slot5 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot5));
        private static readonly AttachedProperty<object?> Slot6 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot6));
        private static readonly AttachedProperty<object?> Slot7 = AvaloniaProperty.RegisterAttached<MultiBinding, AvaloniaObject, object?>(nameof(Slot7));
        private static readonly AttachedProperty<object?>[] All = [Slot0, Slot1, Slot2, Slot3, Slot4, Slot5, Slot6, Slot7];
        private static readonly ConditionalWeakTable<AvaloniaObject, StrongBox<int>> Used = new();

        public static AttachedProperty<object?>[] Allocate(AvaloniaObject target, int count)
        {
            StrongBox<int> used = Used.GetOrCreateValue(target);
            if (used.Value + count > All.Length)
            {
                throw new InvalidOperationException($"WpfCompat's MultiBinding binds at most {All.Length} bindings on one element.");
            }
            var slots = new AttachedProperty<object?>[count];
            for (int i = 0; i < count; i++)
            {
                slots[i] = All[used.Value++];
            }
            return slots;
        }
    }
}
