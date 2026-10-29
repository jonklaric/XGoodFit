using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace DAG
{
    /// <summary>
    /// Provides utility methods for property change notification and value comparison.
    /// </summary>
    /// <remarks>
    /// This class contains helper methods that simplify implementing <see cref="INotifyPropertyChanged"/>
    /// by providing standardized setters for common types that automatically raise property change events.
    /// </remarks>
    public static class Utilities
    {
        /// <summary>
        /// Compares two strings for equality, treating null and empty strings as equivalent.
        /// </summary>
        /// <param name="a">The first string to compare.</param>
        /// <param name="b">The second string to compare.</param>
        /// <returns>
        /// <c>true</c> if both strings are equal, or if both are null/empty; otherwise, <c>false</c>.
        /// </returns>
        /// <example>
        /// <code>
        /// bool result1 = Utilities.AreEqual(null, "");     // true
        /// bool result2 = Utilities.AreEqual("foo", "foo"); // true
        /// bool result3 = Utilities.AreEqual("foo", "bar"); // false
        /// </code>
        /// </example>
        public static bool AreEqual(string a, string b)
        {
            return string.IsNullOrEmpty(a) ? string.IsNullOrEmpty(b) : string.Equals(a, b);
        }

        /// <summary>
        /// Sets a string property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        public static void SetString(string value, ref string target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (AreEqual(value, target)) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a boolean property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        public static void SetBoolean(bool value, ref bool target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (value == target) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets an integer property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        public static void SetInteger(int value, ref int target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (value == target) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a short property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        public static void SetShort(short value, ref short target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (value == target) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a byte property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        public static void SetByte(byte value, ref byte target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (value == target) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a float property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        /// <remarks>
        /// Uses an epsilon of 5.5E-7 for comparison to handle floating-point precision issues.
        /// Values within this tolerance are considered equal.
        /// </remarks>
        public static void SetFloat(float value, ref float target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (Math.Abs(value - target) <= 5.5E-7) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a double property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        /// <remarks>
        /// Uses an epsilon of 1E-15 for comparison to handle floating-point precision issues.
        /// Values within this tolerance are considered equal.
        /// </remarks>
        public static void SetDouble(double value, ref double target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (Math.Abs(value - target) <= 1E-15) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }

        /// <summary>
        /// Sets a DateTime property value and raises the property changed event if the value changed.
        /// </summary>
        /// <param name="value">The new value to set.</param>
        /// <param name="target">A reference to the backing field.</param>
        /// <param name="propertyChanged">The property changed event handler to invoke.</param>
        /// <param name="source">The object that owns the property (sender for the event).</param>
        /// <param name="action">An optional action to invoke when the value changes.</param>
        /// <param name="memberName">The property name (automatically provided by the compiler).</param>
        public static void SetDateTime(DateTime value, ref DateTime target, PropertyChangedEventHandler propertyChanged = null, object source = null, Action<string> action = null, [CallerMemberName] string memberName = "")
        {
            if (DateTime.Equals(value, target)) { return; }
            target = value;
            action?.Invoke(memberName);
            propertyChanged?.Invoke(source, new PropertyChangedEventArgs(memberName));
        }
    }
}
