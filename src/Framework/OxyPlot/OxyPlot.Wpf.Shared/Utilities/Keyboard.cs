// --------------------------------------------------------------------------------------------------------------------
// <copyright file="Keyboard.cs" company="OxyPlot">
//   Copyright (c) 2020 OxyPlot contributors
// </copyright>
// <summary>
//   Provides utility methods related to the keyboard.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf
{
    using System;
    using Avalonia.Input;

    /// <summary>
    /// Provides utility methods related to the keyboard.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: Avalonia has no global keyboard state like WPF's <c>Keyboard.IsKeyDown</c>, so the
    /// modifier keys come from the input event being handled. <see cref="KeyModifiers.Meta"/> is the
    /// Windows key, except on macOS, where the Command key is Control, as in WpfCompat's
    /// <c>Keyboard.Modifiers</c>: the plot's Ctrl+drag and Ctrl+wheel are Command+drag and
    /// Command+wheel there.
    /// </remarks>
    internal static class Keyboard
    {
        /// <summary>
        /// Gets the current modifier keys.
        /// </summary>
        /// <param name="keyModifiers">The modifier keys of the input event.</param>
        /// <returns>A <see cref="OxyModifierKeys" /> value.</returns>
        public static OxyModifierKeys GetModifierKeys(KeyModifiers keyModifiers)
        {
            var modifiers = OxyModifierKeys.None;
            if ((keyModifiers & KeyModifiers.Shift) != 0)
            {
                modifiers |= OxyModifierKeys.Shift;
            }

            if ((keyModifiers & KeyModifiers.Control) != 0)
            {
                modifiers |= OxyModifierKeys.Control;
            }

            if ((keyModifiers & KeyModifiers.Alt) != 0)
            {
                modifiers |= OxyModifierKeys.Alt;
            }

            if ((keyModifiers & KeyModifiers.Meta) != 0)
            {
                modifiers |= OperatingSystem.IsMacOS() ? OxyModifierKeys.Control : OxyModifierKeys.Windows;
            }

            return modifiers;
        }
    }
}
