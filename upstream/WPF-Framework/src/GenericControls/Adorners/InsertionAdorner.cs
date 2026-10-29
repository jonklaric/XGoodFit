using System.Windows;
using System.Windows.Documents;
using System.Windows.Media;

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
    /// </remarks>
    public class InsertionAdorner : Adorner
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
            _pen.Freeze();

            var firstLine = new LineSegment(new Point(0d, -5), false);
            firstLine.Freeze();
            var secondLine = new LineSegment(new Point(0d, 5d), false);
            secondLine.Freeze();

            var figure = new PathFigure();
            figure.StartPoint = new Point(5d, 0d);
            figure.Segments.Add(firstLine);
            figure.Segments.Add(secondLine);
            figure.Freeze();

            _triangleGeometry = new PathGeometry();
            _triangleGeometry.Figures.Add(figure);
            _triangleGeometry.Freeze();
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InsertionAdorner"/> class.
        /// </summary>
        /// <param name="isSeparatorHorizontal">Indicates whether the insertion line is horizontal or vertical.</param>
        /// <param name="isInFirstHalf">Whether the insertion target is in the first half of the container.</param>
        /// <param name="adornedElement">The UIElement being adorned.</param>
        /// <param name="adornerLayer">The adorner layer that will display the insertion marker.</param>
        public InsertionAdorner(bool isSeparatorHorizontal, bool isInFirstHalf, UIElement adornedElement, AdornerLayer adornerLayer) : base(adornedElement)
        {
            this.isSeparatorHorizontal = isSeparatorHorizontal;
            IsInFirstHalf = isInFirstHalf;
            _adornerLayer = adornerLayer;
            IsHitTestVisible = false;

            _adornerLayer.Add(this);
        }

        /// <summary>
        /// This draws one line and two triangles at each end of the line.
        /// </summary>
        /// <param name="drawingContext">The drawing context used to render visuals.</param>
        protected override void OnRender(DrawingContext drawingContext)
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
            drawingContext.PushTransform(new TranslateTransform(origin.X, origin.Y));
            drawingContext.PushTransform(new RotateTransform(angle));

            drawingContext.DrawGeometry(_pen.Brush, null, _triangleGeometry);

            drawingContext.Pop();
            drawingContext.Pop();
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

            double eWidth = AdornedElement.RenderSize.Width;
            double eHeight = AdornedElement.RenderSize.Height;

            if (isSeparatorHorizontal)
            {
                endPoint.X = eWidth;
                if (!IsInFirstHalf)
                {
                    startPoint.Y = eHeight;
                    endPoint.Y = eHeight;
                }
            }
            else
            {
                endPoint.Y = eHeight;
                if (!IsInFirstHalf)
                {
                    startPoint.X = eWidth;
                    endPoint.X = eWidth;
                }
            }
        }

        /// <summary>
        /// Detaches the adorner from the adorner layer.
        /// </summary>
        public void Detach()
        {
            _adornerLayer.Remove(this);
        }

    }
}