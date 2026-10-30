namespace WpfCompat;

/// <summary>The buttons a message box shows: WPF's <c>System.Windows.MessageBoxButton</c>.</summary>
public enum MessageBoxButton
{
    /// <summary>OK.</summary>
    OK = 0,

    /// <summary>OK and Cancel.</summary>
    OKCancel = 1,

    /// <summary>Yes, No and Cancel.</summary>
    YesNoCancel = 3,

    /// <summary>Yes and No.</summary>
    YesNo = 4,
}

/// <summary>The icon a message box shows: WPF's <c>System.Windows.MessageBoxImage</c>.</summary>
public enum MessageBoxImage
{
    /// <summary>No icon.</summary>
    None = 0,

    /// <summary>A stop sign (as <see cref="Error"/>).</summary>
    Hand = 16,

    /// <summary>A question mark.</summary>
    Question = 32,

    /// <summary>A warning sign (as <see cref="Warning"/>).</summary>
    Exclamation = 48,

    /// <summary>An information sign (as <see cref="Information"/>).</summary>
    Asterisk = 64,

    /// <summary>A stop sign.</summary>
    Stop = Hand,

    /// <summary>An error sign.</summary>
    Error = Hand,

    /// <summary>A warning sign.</summary>
    Warning = Exclamation,

    /// <summary>An information sign.</summary>
    Information = Asterisk,
}

/// <summary>The button the user pressed in a message box: WPF's <c>System.Windows.MessageBoxResult</c>.</summary>
public enum MessageBoxResult
{
    /// <summary>No result: the message box was closed another way.</summary>
    None = 0,

    /// <summary>OK.</summary>
    OK = 1,

    /// <summary>Cancel.</summary>
    Cancel = 2,

    /// <summary>Yes.</summary>
    Yes = 6,

    /// <summary>No.</summary>
    No = 7,
}
