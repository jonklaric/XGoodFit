using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

namespace GenericControls
{
    /// <summary>
    /// An adorner that displays a semi-transparent visual representation of a UIElement during drag operations.
    /// </summary>
    /// <remarks>
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
    public class DragAdorner : Adorner
    {
        /// <summary>
        /// Vertical offset to position the adorner relative to the cursor.
        /// </summary>
        private const double VerticalCursorOffset = 17.5d;

        /// <summary>
        /// Maximum width for the rendered adorner to prevent oversized ghost images.
        /// </summary>
        private const double MaxRenderWidth = 500d;

        /// <summary>
        /// Maximum height for the rendered adorner to prevent oversized ghost images.
        /// </summary>
        private const double MaxRenderHeight = 400d;

        private Brush _vBrush;
        private Point _location;

        /// <summary>
        /// Initializes a new instance of the <see cref="DragAdorner"/> class.
        /// </summary>
        /// <param name="adornedElement">The UIElement to display as a ghost image.</param>
        /// <param name="offset">The offset from the mouse pointer.</param>
        public DragAdorner(UIElement adornedElement, Point offset) : base(adornedElement)
        {
            IsHitTestVisible = false;
            Focusable = false;
            _vBrush = new VisualBrush(AdornedElement) { Stretch = Stretch.None, AlignmentX = AlignmentX.Left };
            _vBrush.Opacity = 0.8d;
        }

        /// <summary>
        /// Updates the position of the adorner on the screen.
        /// </summary>
        /// <param name="location">The new top-left location of the ghost element.</param>
        public void UpdatePosition(Point location)
        {
            _location = new Point(location.X, location.Y - VerticalCursorOffset);
            InvalidateVisual();
        }

        /// <summary>
        /// Draws the visual representation of the drag adorner.
        /// </summary>
        /// <param name="dc">The drawing context to render into.</param>
        protected override void OnRender(DrawingContext dc)
        {
            dc.PushOpacityMask(new LinearGradientBrush(Colors.White, Colors.Transparent, 45d));
            dc.DrawRectangle(_vBrush, null, new Rect(_location.X, _location.Y, Math.Min(RenderSize.Width, MaxRenderWidth), Math.Min(RenderSize.Height, MaxRenderHeight)));
            dc.Pop();
        }
    }
}