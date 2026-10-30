using Avalonia.Input;

namespace WpfCompat.Input;

/// <summary>WPF's standard cursors (<c>System.Windows.Input.Cursors</c>), as Avalonia cursors.</summary>
public static class Cursors
{
    /// <summary>The normal arrow.</summary>
    public static Cursor Arrow { get; } = new(StandardCursorType.Arrow);

    /// <summary>The arrow with a busy indicator.</summary>
    public static Cursor AppStarting { get; } = new(StandardCursorType.AppStarting);

    /// <summary>The crosshair.</summary>
    public static Cursor Cross { get; } = new(StandardCursorType.Cross);

    /// <summary>The pointing hand.</summary>
    public static Cursor Hand { get; } = new(StandardCursorType.Hand);

    /// <summary>The arrow with a question mark.</summary>
    public static Cursor Help { get; } = new(StandardCursorType.Help);

    /// <summary>The text cursor.</summary>
    public static Cursor IBeam { get; } = new(StandardCursorType.Ibeam);

    /// <summary>The not-allowed cursor.</summary>
    public static Cursor No { get; } = new(StandardCursorType.No);

    /// <summary>No cursor.</summary>
    public static Cursor None { get; } = new(StandardCursorType.None);

    /// <summary>The four-way scroll cursor. Avalonia has none, so this is the four-way move cursor.</summary>
    public static Cursor ScrollAll { get; } = new(StandardCursorType.SizeAll);

    /// <summary>The four-way move cursor.</summary>
    public static Cursor SizeAll { get; } = new(StandardCursorType.SizeAll);

    /// <summary>The diagonal resize cursor, top right to bottom left.</summary>
    public static Cursor SizeNESW { get; } = new(StandardCursorType.TopRightCorner);

    /// <summary>The vertical resize cursor.</summary>
    public static Cursor SizeNS { get; } = new(StandardCursorType.SizeNorthSouth);

    /// <summary>The diagonal resize cursor, top left to bottom right.</summary>
    public static Cursor SizeNWSE { get; } = new(StandardCursorType.TopLeftCorner);

    /// <summary>The horizontal resize cursor.</summary>
    public static Cursor SizeWE { get; } = new(StandardCursorType.SizeWestEast);

    /// <summary>The up arrow.</summary>
    public static Cursor UpArrow { get; } = new(StandardCursorType.UpArrow);

    /// <summary>The busy cursor.</summary>
    public static Cursor Wait { get; } = new(StandardCursorType.Wait);
}
