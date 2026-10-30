using Avalonia;
using Avalonia.Collections;
using Avalonia.Controls;
using Avalonia.Media;

namespace WpfCompat.Shapes;

/// <summary>
/// WPF's <c>System.Windows.Shapes.Line</c>, laid out and drawn as WPF lays out and draws it.
/// </summary>
/// <remarks>
/// <para>
/// Ported XAML uses Avalonia's <c>Line</c> (with <c>StartPoint</c> and <c>EndPoint</c>) where it draws
/// the same. This one is for the lines that don't, which are stretched lines:
/// </para>
/// <list type="bullet">
/// <item>A horizontal or vertical line has no proportions to keep, so WPF stretches it as with
/// <c>Fill</c> whatever its <see cref="Stretch"/>. Avalonia's <c>Uniform</c> scales it to nothing, so a
/// property row's dot leader or a dash-style preview would draw nothing.</item>
/// <item>WPF fits the stroke inside the element: a stretched line is inset by half the stroke's thickness,
/// and the element is as thick as the stroke. Avalonia stretches the bare geometry, so a horizontal line's
/// element is 0 high, and its stroke spills out of it.</item>
/// <item>A dash array of zeros draws nothing in WPF, as the "no line" dash style in a line-style list
/// shows, but a solid line in Avalonia.</item>
/// </list>
/// <para>
/// Layout follows WPF's <c>Shape.MeasureOverride</c>, <c>ArrangeOverride</c> and <c>GetStretchMetrics</c>.
/// Line caps are flat, WPF's default; no ported XAML sets others.
/// </para>
/// </remarks>
public class Line : Control
{
    /// <summary>Defines the <see cref="X1"/> property.</summary>
    public static readonly StyledProperty<double> X1Property = AvaloniaProperty.Register<Line, double>(nameof(X1));

    /// <summary>Defines the <see cref="Y1"/> property.</summary>
    public static readonly StyledProperty<double> Y1Property = AvaloniaProperty.Register<Line, double>(nameof(Y1));

    /// <summary>Defines the <see cref="X2"/> property.</summary>
    public static readonly StyledProperty<double> X2Property = AvaloniaProperty.Register<Line, double>(nameof(X2));

    /// <summary>Defines the <see cref="Y2"/> property.</summary>
    public static readonly StyledProperty<double> Y2Property = AvaloniaProperty.Register<Line, double>(nameof(Y2));

    /// <summary>Defines the <see cref="Stroke"/> property.</summary>
    public static readonly StyledProperty<IBrush?> StrokeProperty = AvaloniaProperty.Register<Line, IBrush?>(nameof(Stroke));

    /// <summary>Defines the <see cref="StrokeThickness"/> property: 1 by default, as in WPF.</summary>
    public static readonly StyledProperty<double> StrokeThicknessProperty = AvaloniaProperty.Register<Line, double>(nameof(StrokeThickness), 1d);

    /// <summary>Defines the <see cref="StrokeDashArray"/> property.</summary>
    public static readonly StyledProperty<AvaloniaList<double>?> StrokeDashArrayProperty =
        AvaloniaProperty.Register<Line, AvaloniaList<double>?>(nameof(StrokeDashArray));

    /// <summary>Defines the <see cref="StrokeDashOffset"/> property.</summary>
    public static readonly StyledProperty<double> StrokeDashOffsetProperty = AvaloniaProperty.Register<Line, double>(nameof(StrokeDashOffset));

    /// <summary>Defines the <see cref="Stretch"/> property: None by default, as in WPF.</summary>
    public static readonly StyledProperty<Stretch> StretchProperty = AvaloniaProperty.Register<Line, Stretch>(nameof(Stretch), Stretch.None);

    // The stretch from the line's coordinates to the element's, set when the line is arranged.
    private Matrix _stretchMatrix = Matrix.Identity;

    // Whether layout hit a numerical error, after which WPF draws nothing.
    private bool _isInvalid;

    static Line()
    {
        AffectsMeasure<Line>(X1Property, Y1Property, X2Property, Y2Property, StrokeProperty, StrokeThicknessProperty, StretchProperty);
        AffectsRender<Line>(X1Property, Y1Property, X2Property, Y2Property, StrokeProperty, StrokeThicknessProperty,
            StrokeDashArrayProperty, StrokeDashOffsetProperty, StretchProperty);
    }

    /// <summary>The x-coordinate of the line's start.</summary>
    public double X1
    {
        get => GetValue(X1Property);
        set => SetValue(X1Property, value);
    }

    /// <summary>The y-coordinate of the line's start.</summary>
    public double Y1
    {
        get => GetValue(Y1Property);
        set => SetValue(Y1Property, value);
    }

    /// <summary>The x-coordinate of the line's end.</summary>
    public double X2
    {
        get => GetValue(X2Property);
        set => SetValue(X2Property, value);
    }

    /// <summary>The y-coordinate of the line's end.</summary>
    public double Y2
    {
        get => GetValue(Y2Property);
        set => SetValue(Y2Property, value);
    }

    /// <summary>The brush the line is drawn with.</summary>
    public IBrush? Stroke
    {
        get => GetValue(StrokeProperty);
        set => SetValue(StrokeProperty, value);
    }

    /// <summary>The line's thickness.</summary>
    public double StrokeThickness
    {
        get => GetValue(StrokeThicknessProperty);
        set => SetValue(StrokeThicknessProperty, value);
    }

    /// <summary>The lengths of dashes and gaps, in multiples of <see cref="StrokeThickness"/>.</summary>
    public AvaloniaList<double>? StrokeDashArray
    {
        get => GetValue(StrokeDashArrayProperty);
        set => SetValue(StrokeDashArrayProperty, value);
    }

    /// <summary>Where the dash pattern starts, in multiples of <see cref="StrokeThickness"/>.</summary>
    public double StrokeDashOffset
    {
        get => GetValue(StrokeDashOffsetProperty);
        set => SetValue(StrokeDashOffsetProperty, value);
    }

    /// <summary>How the line fills the element.</summary>
    public Stretch Stretch
    {
        get => GetValue(StretchProperty);
        set => SetValue(StretchProperty, value);
    }

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        Size newSize = Stretch == Stretch.None
            ? GetNaturalSize()
            : GetStretchMetrics(Stretch, GetStrokeThickness(), availableSize, GetGeometryBounds(), out _);
        _isInvalid = SizeIsInvalidOrEmpty(newSize);
        return _isInvalid ? default : newSize;
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        Size newSize;
        if (Stretch == Stretch.None)
        {
            _stretchMatrix = Matrix.Identity;
            newSize = finalSize;
        }
        else
        {
            newSize = GetStretchMetrics(Stretch, GetStrokeThickness(), finalSize, GetGeometryBounds(), out _stretchMatrix);
        }
        _isInvalid = SizeIsInvalidOrEmpty(newSize);
        return _isInvalid ? default : newSize;
    }

    /// <inheritdoc/>
    public override void Render(DrawingContext context)
    {
        if (_isInvalid || IsPenNoOp())
        {
            return;
        }

        IDashStyle? dashStyle = null;
        if (StrokeDashArray is { Count: > 0 } dashes)
        {
            if (dashes.All(d => d == 0))
            {
                // Dashes and gaps of no length: WPF draws nothing.
                return;
            }
            dashStyle = new DashStyle(dashes, StrokeDashOffset);
        }

        var pen = new Pen(Stroke, Math.Abs(StrokeThickness), dashStyle, PenLineCap.Flat);
        context.DrawLine(pen, new Point(X1, Y1).Transform(_stretchMatrix), new Point(X2, Y2).Transform(_stretchMatrix));
    }

    // WPF's Shape.IsPenNoOp: whether the line has no stroke to draw (DoubleUtil.IsZero's tolerance).
    private bool IsPenNoOp() => Stroke is null || double.IsNaN(StrokeThickness) || Math.Abs(StrokeThickness) < 10.0 * 2.2204460492503131e-016;

    // WPF's Shape.GetStrokeThickness: 0 without a stroke.
    private double GetStrokeThickness() => IsPenNoOp() ? 0 : Math.Abs(StrokeThickness);

    // The bounds of the line geometry, as WPF's LineGeometry.Bounds.
    private Rect GetGeometryBounds() =>
        new(Math.Min(X1, X2), Math.Min(Y1, Y2), Math.Abs(X2 - X1), Math.Abs(Y2 - Y1));

    // WPF's Shape.GetNaturalSize: the bottom-right corner of the stroked line's bounds (without dashes).
    private Size GetNaturalSize()
    {
        Rect bounds = GetGeometryBounds();
        if (!IsPenNoOp())
        {
            var start = new Point(X1, Y1);
            var end = new Point(X2, Y2);
            var direction = new Vector(X2 - X1, Y2 - Y1);
            double length = direction.Length;
            if (length == 0)
            {
                // A line of no length has nothing to draw with flat caps, so WPF's bounds are empty.
                return default;
            }

            // The flat-capped stroke is a rectangle around the line, half the thickness either side.
            Vector side = new Vector(-direction.Y, direction.X) * (GetStrokeThickness() / 2 / length);
            Point[] corners = [start + side, start - side, end + side, end - side];
            bounds = new Rect(
                new Point(corners.Min(c => c.X), corners.Min(c => c.Y)),
                new Point(corners.Max(c => c.X), corners.Max(c => c.Y)));
        }
        return new Size(Math.Max(bounds.Right, 0), Math.Max(bounds.Bottom, 0));
    }

    // WPF's Shape.GetStretchMetrics, returning the stretched size and the matrix that stretches the line.
    private static Size GetStretchMetrics(Stretch mode, double strokeThickness, Size availableSize, Rect geometryBounds, out Matrix stretchMatrix)
    {
        double margin = strokeThickness / 2;
        bool hasThinDimension = false;

        // Initialization for mode == Fill
        double xScale = Math.Max(availableSize.Width - strokeThickness, 0);
        double yScale = Math.Max(availableSize.Height - strokeThickness, 0);
        double dX = margin - geometryBounds.Left;
        double dY = margin - geometryBounds.Top;

        // Compute the scale factors from the geometry to the size. If our shape is "thin", i.e. a
        // horizontal or vertical line, we can ignore non-fill stretches.
        if (geometryBounds.Width > xScale * double.Epsilon)
        {
            xScale /= geometryBounds.Width;
        }
        else
        {
            xScale = 1;
            // We can ignore uniform and uniform-to-fill stretches if we have a vertical line.
            if (geometryBounds.Width == 0)
            {
                hasThinDimension = true;
            }
        }

        if (geometryBounds.Height > yScale * double.Epsilon)
        {
            yScale /= geometryBounds.Height;
        }
        else
        {
            yScale = 1;
            // We can ignore uniform and uniform-to-fill stretches if we have a horizontal line.
            if (geometryBounds.Height == 0)
            {
                hasThinDimension = true;
            }
        }

        // We are initialized for Fill, but for the other modes, if one of our dimensions is thin,
        // uniform stretches are meaningless, so we treat the stretch as fill.
        if (mode != Stretch.Fill && !hasThinDimension)
        {
            if (mode == Stretch.Uniform)
            {
                if (yScale > xScale)
                {
                    // Resize to fit the size's width
                    yScale = xScale;
                }
                else
                {
                    // Resize to fit the size's height
                    xScale = yScale;
                }
            }
            else
            {
                if (xScale > yScale)
                {
                    // Resize to fill the size vertically, spilling out horizontally
                    yScale = xScale;
                }
                else
                {
                    // Resize to fill the size horizontally, spilling out vertically
                    xScale = yScale;
                }
            }
        }

        // WPF scales about the geometry's top-left corner, then translates by (dX, dY).
        stretchMatrix = Matrix.CreateTranslation(-geometryBounds.Left, -geometryBounds.Top)
            * Matrix.CreateScale(xScale, yScale)
            * Matrix.CreateTranslation(geometryBounds.Left + dX, geometryBounds.Top + dY);
        return new Size(geometryBounds.Width * xScale + strokeThickness, geometryBounds.Height * yScale + strokeThickness);
    }

    // WPF's Shape.SizeIsInvalidOrEmpty, and infinite sizes, which Avalonia's layout rejects.
    private static bool SizeIsInvalidOrEmpty(Size size) =>
        double.IsNaN(size.Width) || double.IsNaN(size.Height) || double.IsInfinity(size.Width) || double.IsInfinity(size.Height);
}
