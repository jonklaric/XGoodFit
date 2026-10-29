namespace FrameworkInterfaces.Undo
{
    /// <summary>
    /// Represents an element that supports undo/redo operations.
    /// </summary>
    /// <remarks>
    /// <para>
    /// This interface extends <see cref="IElement"/> to add undo/redo support.
    /// Implementing this interface is optional - elements that don't implement it
    /// will continue to work without undo support.
    /// </para>
    /// <para>
    /// When implementing this interface, property setters should call
    /// <see cref="ElementBase.RecordPropertyChange"/> to record changes
    /// for undo purposes.
    /// </para>
    /// <para>
    /// <b> Authors: </b>
    /// <list type="bullet">
    ///     <item> Haden Smith, USACE Risk Management Center, cole.h.smith@usace.army.mil </item>
    /// </list>
    /// </para>
    /// </remarks>
    public interface IUndoableElement : IElement
    {
        /// <summary>
        /// Gets the undo manager for this element.
        /// </summary>
        /// <remarks>
        /// Each element has its own undo manager, enabling Visual Studio-style
        /// per-document undo where Ctrl+Z operates on the active document.
        /// </remarks>
        IUndoManager UndoManager { get; }

        /// <summary>
        /// Gets or sets whether undo recording is enabled for this element.
        /// </summary>
        /// <remarks>
        /// Set to false to temporarily disable undo recording, for example
        /// during bulk operations or loading from disk.
        /// </remarks>
        bool IsUndoEnabled { get; set; }
    }
}
