using System;
using System.ComponentModel;

namespace FrameworkInterfaces
{
    /// <summary>
    /// Provides utility methods for property change notification and value comparison.
    /// </summary>
    /// <remarks>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public static class Methods
    {
        /// <summary>
        /// Sets a string property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">The target field to update.</param>
        /// <param name="propertyChanged">The PropertyChanged event handler.</param>
        /// <param name="source">The source object raising the event.</param>
        /// <param name="action">Optional action to execute after setting the value.</param>
        /// <param name="memberName">The name of the calling property (auto-populated).</param>
        public static void SetString(string value, ref string target, PropertyChangedEventHandler? propertyChanged = null, object? source = null, Action<string>? action = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            value = value ?? string.Empty;
            if (value == target) return;
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a boolean property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">The target field to update.</param>
        /// <param name="propertyChanged">The PropertyChanged event handler.</param>
        /// <param name="source">The source object raising the event.</param>
        /// <param name="action">Optional action to execute after setting the value.</param>
        /// <param name="memberName">The name of the calling property (auto-populated).</param>
        public static void SetBoolean(bool value, ref bool target, PropertyChangedEventHandler? propertyChanged = null, object? source = null, Action<string>? action = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (value == target) return;
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets an integer property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">The target field to update.</param>
        /// <param name="propertyChanged">The PropertyChanged event handler.</param>
        /// <param name="source">The source object raising the event.</param>
        /// <param name="action">Optional action to execute after setting the value.</param>
        /// <param name="memberName">The name of the calling property (auto-populated).</param>
        public static void SetInteger(int value, ref int target, PropertyChangedEventHandler? propertyChanged = null, object? source = null, Action<string>? action = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (value == target) return;
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a short property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">The target field to update.</param>
        /// <param name="propertyChanged">The PropertyChanged event handler.</param>
        /// <param name="source">The source object raising the event.</param>
        /// <param name="action">Optional action to execute after setting the value.</param>
        /// <param name="memberName">The name of the calling property (auto-populated).</param>
        public static void SetShort(short value, ref short target, PropertyChangedEventHandler? propertyChanged = null, object? source = null, Action<string>? action = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (value == target) return;
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a byte property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">The target field to update.</param>
        /// <param name="propertyChanged">The PropertyChanged event handler.</param>
        /// <param name="source">The source object raising the event.</param>
        /// <param name="action">Optional action to execute after setting the value.</param>
        /// <param name="memberName">The name of the calling property (auto-populated).</param>
        public static void SetByte(byte value, ref byte target, PropertyChangedEventHandler? propertyChanged = null, object? source = null, Action<string>? action = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (value == target) return;
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a DateTime property value and raises the PropertyChanged event if the value has changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">The target field to update.</param>
        /// <param name="propertyChanged">The PropertyChanged event handler.</param>
        /// <param name="source">The source object raising the event.</param>
        /// <param name="action">Optional action to execute after setting the value.</param>
        /// <param name="memberName">The name of the calling property (auto-populated).</param>
        public static void SetDateTime(DateTime value, ref DateTime target, PropertyChangedEventHandler? propertyChanged = null, object? source = null, Action<string>? action = null, [System.Runtime.CompilerServices.CallerMemberName] string memberName = "")
        {
            if (DateTime.Equals(value, target)) return;
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

    }
}

