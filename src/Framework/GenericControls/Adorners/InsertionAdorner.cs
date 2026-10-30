using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;

namespace GenericControls
{
    /// <summary>
    /// An adorner that displays an insertion marker indicating where a dragged item will be dropped.
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
    /// <para>
    /// Based on code developed at Zag Studios (http://www.zagstudio.com/blog/488).
    /// </para>
    /// <para>
    /// XGoodFit port: Avalonia's adorners are controls in an <see cref="AdornerLayer"/>, which places each
    /// over its adorned element (<see cref="AdornerLayer.SetAdornedElement"/>), so this is a control with
    /// WPF's <c>Adorner.AdornedElement</c>. Avalonia's pens and geometries can't be frozen; these are
    /// created once and never changed. Transforms are pushed as matrices, which Avalonia pops by disposal.
    /// </para>
    /// </remarks>
    public class InsertionAdorner : Control
    {
        private readonly bool isSeparatorHorizontal;

        /// <summary>
        /// Gets or sets whether the insertion point is in the first half of the adorned element.
        /// </summary>
        public bool IsInFirstHalf
        {
            get
            {
                return _isInFirstHalf;
            }
            set
            {
                _isInFirstHalf = value;
            }
        }
        private bool _isInFirstHalf;
        private readonly AdornerLayer _adornerLayer;
        private static readonly Pen _pen;
        private static readonly PathGeometry _triangleGeometry;

        // Create the pen and triangle in a static constructor and freeze them to improve performance.
        /// <summary>
        /// Static constructor initializes shared Pen and Geometry resources and freezes them to improve performance.
        /// </summary>
        static InsertionAdorner()
        {
            _pen = new Pen(Brushes.Gray, 2d);

            var firstLine = new LineSegment { Point = new Point(0d, -5), IsStroked = false };
            var secondLine = new LineSegment { Point = new Point(0d, 5d), IsStroked = false };

            var figure = new PathFigure();
            figure.StartPoint = new Point(5d, 0d);
            figure.Segments.Add(firstLine);
            figure.Segments.Add(secondLine);

            _triangleGeometry = new PathGeometry();
            _triangleGeometry.Figures.Add(figure);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertionAdorner"/> class.
        /// </summary>
        /// <param name="isSeparatorHorizontal">Indicates whether the insertion line is horizontal or vertical.</param>
        /// <param name="isInFirstHalf">Whether the insertion target is in the first half of the container.</param>
        /// <param name="adornedElement">The element being adorned.</param>
        /// <param name="adornerLayer">The adorner layer that will display the insertion marker.</param>
        public InsertionAdorner(bool isSeparatorHorizontal, bool isInFirstHalf, Visual adornedElement, AdornerLayer adornerLayer)
        {
            this.isSeparatorHorizontal = isSeparatorHorizontal;
            IsInFirstHalf = isInFirstHalf;
            _adornerLayer = adornerLayer;
            IsHitTestVisible = false;

            // XGoodFit port: what WPF's Adorner constructor and AdornerLayer.Add do.
            AdornedElement = adornedElement;
            AdornerLayer.SetAdornedElement(this, adornedElement);
            _adornerLayer.Children.Add(this);
        }

        /// <summary>
        /// Gets the element this adorner is bound to.
        /// </summary>
        /// <remarks>XGoodFit addition: WPF's <c>Adorner.AdornedElement</c>.</remarks>
        public Visual AdornedElement { get; }

        /// <summary>
        /// This draws one line and two triangles at each end of the line.
        /// </summary>
        /// <param name="drawingContext">The drawing context used to render visuals.</param>
        public override void Render(DrawingContext drawingContext)
        {
            var startPoint = default(Point);
            var endPoint = default(Point);

            CalculateStartAndEndPoint(ref startPoint, ref endPoint);
            drawingContext.DrawLine(_pen, startPoint, endPoint);

            if (isSeparatorHorizontal)
            {
                DrawTriangle(drawingContext, startPoint, 0d);
                DrawTriangle(drawingContext, endPoint, 180d);
            }
            else
            {
                DrawTriangle(drawingContext, startPoint, 90d);
                DrawTriangle(drawingContext, endPoint, -90);
            }
        }

        /// <summary>
        /// Draws a triangle at specified location and angle.
        /// </summary>
        /// <param name="drawingContext">Drawing context used for rendering.</param>
        /// <param name="origin">The origin point of the triangle.</param>
        /// <param name="angle">The angle to rotate the triangle.</param>
        private void DrawTriangle(DrawingContext drawingContext, Point origin, double angle)
        {
            using (drawingContext.PushTransform(Matrix.CreateTranslation(origin.X, origin.Y)))
            using (drawingContext.PushTransform(Matrix.CreateRotation(Matrix.ToRadians(angle))))
            {
                drawingContext.DrawGeometry(_pen.Brush, null, _triangleGeometry);
            }
        }

        /// <summary>
        /// Calculates the start and end points of the insertion line based on orientation and position.
        /// </summary>
        /// <param name="startPoint">Returns the calculated start point.</param>
        /// <param name="endPoint">Returns the calculated end point.</param>
        private void CalculateStartAndEndPoint(ref Point startPoint, ref Point endPoint)
        {
            startPoint = new Point();
            endPoint = new Point();

            // XGoodFit port: Avalonia's render size is the bounds' size, and its points are immutable.
            double eWidth = AdornedElement.Bounds.Width;
            double eHeight = AdornedElement.Bounds.Height;

            if (isSeparatorHorizontal)
            {
                endPoint = endPoint.WithX(eWidth);
                if (!IsInFirstHalf)
                {
                    startPoint = startPoint.WithY(eHeight);
                    endPoint = endPoint.WithY(eHeight);
                }
            }
            else
            {
                endPoint = endPoint.WithY(eHeight);
                if (!IsInFirstHalf)
                {
                    startPoint = startPoint.WithX(eWidth);
                    endPoint = endPoint.WithX(eWidth);
                }
            }
        }

        /// <summary>
        /// Detaches the adorner from the adorner layer.
        /// </summary>
        public void Detach()
        {
            _adornerLayer.Children.Remove(this);
        }

    }
}