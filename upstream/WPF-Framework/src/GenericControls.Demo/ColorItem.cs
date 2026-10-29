#nullable enable

using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Media;

namespace GenericControls.Demo
{
    /// <summary>
    /// Represents a named color item for use in color collection demonstrations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This class implements <see cref="INotifyPropertyChanged"/> to support
    /// two-way data binding with WPF controls. It is used in the Color Collection
    /// tab of the demo application to demonstrate dynamic color editing with
    /// popup ColorPicker controls.
    /// </para>
    /// <para>
    ///     <b> Authors: </b>
    /// <list type="bullet">
    /// <item><description>
    ///     Woodrow Fields, USACE Risk Management Center, woodrow.l.fields@usace.army.mil
    /// </description></item>
    /// <item><description>
    ///     Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil
    /// </description></item>
    /// </list>
    /// </para>
    /// </remarks>
    public class ColorItem : INotifyPropertyChanged
    {
        #region Private Fields

        private string _name = "";
        private SolidColorBrush _colorBrush = new SolidColorBrush(Colors.White);

        #endregion

        #region Properties

        /// <summary>
        /// Gets or sets the display name for this color item.
        /// </summary>
        /// <value>
        /// A string representing the name of the color (e.g., "Primary", "Accent").
        /// </value>
        public string Name
        {
            get => _name;
            set => SetProperty(ref _name, value);
        }

        /// <summary>
        /// Gets or sets the color value as a <see cref="SolidColorBrush"/>.
        /// </summary>
        /// <value>
        /// A <see cref="SolidColorBrush"/> representing the color value.
        /// Changes to this property trigger <see cref="PropertyChanged"/> notifications.
        /// </value>
        public SolidColorBrush ColorBrush
        {
            get => _colorBrush;
            set => SetProperty(ref _colorBrush, value);
        }

        #endregion

        #region INotifyPropertyChanged Implementation

        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler? PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event for the specified property.
        /// </summary>
        /// <param name="propertyName">
        /// The name of the property that changed. This is automatically provided by the compiler
        /// when using <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Sets the property value and raises <see cref="PropertyChanged"/> if the value changed.
        /// </summary>
        /// <typeparam name="T">The type of the property.</typeparam>
        /// <param name="field">Reference to the backing field.</param>
        /// <param name="value">The new value to set.</param>
        /// <param name="propertyName">
        /// The name of the property. This is automatically provided by the compiler
        /// when using <see cref="CallerMemberNameAttribute"/>.
        /// </param>
        /// <returns>
        /// <c>true</c> if the value was changed; otherwise, <c>false</c>.
        /// </returns>
        protected bool SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
        {
            if (Equals(field, value))
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        #endregion
    }
}
