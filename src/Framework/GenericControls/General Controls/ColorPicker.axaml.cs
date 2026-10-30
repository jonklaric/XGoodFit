using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Shapes;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media;
using WpfCompat.Input;

namespace GenericControls
{
    /// <summary>
    /// A user control for selecting colors using an HSV color picker interface with sliders and swatches.
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
    /// XGoodFit port: the colour is an Avalonia styled property, and its change callback a class handler.
    /// Colours and points are immutable in Avalonia, so the code sets new ones where upstream changed
    /// a component. Mouse events are pointer events, and the cursors are WpfCompat's.
    /// </para>
    /// </remarks>
    public partial class ColorPicker:UserControl
    {

        // XGoodFit port: Avalonia names only controls, so these are the named brushes and gradient stops
        // of upstream's XAML, which the constructor finds through the controls that hold them.
        internal DrawingBrush PreviewDrawingBrush => (DrawingBrush)this.PreviewBorder.Background;
        internal SolidColorBrush GradBrush1;
        internal GradientStop GradStop2;
        internal GradientStop MaxAStop;
        internal GradientStop MinRStop;
        internal GradientStop MaxRStop;
        internal GradientStop MinGStop;
        internal GradientStop MaxGStop;
        internal GradientStop MinBStop;
        internal GradientStop MaxBStop;

        private readonly TranslateTransform _markerTransform = new TranslateTransform();
        private Point _colorPosition;
        private bool _updateMarker = true;
        private bool _updateSlider = true;
        private bool _isLoaded = false;

        /// <summary>
        /// Dependency property for the selected color.
        /// </summary>
        /// <remarks>
        /// <para>
        /// <b>Property type is <see cref="SolidColorBrush"/></b>, not
        /// <see cref="Avalonia.Media.Color"/>. Bindings and code-behind must
        /// supply (and read back) a <c>SolidColorBrush</c>; assigning a raw
        /// <c>Color</c> will not type-check at the DP level. Callers that hold a
        /// <c>Color</c> should wrap it: <c>new SolidColorBrush(myColor)</c>.
        /// </para>
        /// <para>
        /// <b>Grayscale-hue retention:</b> when a grayscale color (saturation ≈ 0)
        /// is assigned, the slider's hue position is intentionally preserved from
        /// the prior selection rather than snapping back to red (hue = 0). This
        /// keeps the slider stable while the user moves the marker through the
        /// neutral column of the color preview, so subsequent saturation/value
        /// changes do not jump the chosen hue. For the assignment path, see
        /// <c>UpdateSliderPosition</c>.
        /// </para>
        /// <para>
        /// XGoodFit port: the default is a black <see cref="SolidColorBrush"/>: Avalonia's
        /// <see cref="Brushes.Black"/> is an immutable brush, not a <see cref="SolidColorBrush"/>.
        /// </para>
        /// </remarks>
        public static readonly StyledProperty<SolidColorBrush> ColorProperty = AvaloniaProperty.Register<ColorPicker, SolidColorBrush>(nameof(Color), new SolidColorBrush(Colors.Black));

        static ColorPicker()
        {
            // XGoodFit port: the property-changed callback of upstream's metadata.
            ColorProperty.Changed.AddClassHandler<ColorPicker>(ColorCallback);
        }

        /// <summary>
        /// Callback executed when the <see cref="Color"/> property changes.
        /// Updates marker and slider portion and color previews.
        /// </summary>
        /// <param name="d">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private static void ColorCallback(AvaloniaObject d, AvaloniaPropertyChangedEventArgs e)
        {
            if (d == null)
                return;
            if (d.GetType() != typeof(ColorPicker))
                return;
            ColorPicker thisControl = (ColorPicker)d;
            // 
            if (e.NewValue == null)
                return;
            SolidColorBrush newColor = e.NewValue as SolidColorBrush;
            if (newColor == null)
                return;
            if (thisControl._updateMarker)
                thisControl.UpdateMarkerPosition(newColor.Color);
            if (thisControl._updateSlider)
                thisControl.UpdateSliderPosition(newColor.Color);
            // 
            thisControl.PreviewDrawingBrush.Opacity = newColor.Color.A / 255d;
            // 
            if (thisControl.MaxAStop is not null)
                thisControl.MaxAStop.Color = Avalonia.Media.Color.FromRgb(newColor.Color.R, newColor.Color.G, newColor.Color.B);

            if (thisControl.MinRStop is not null)
                thisControl.MinRStop.Color = Avalonia.Media.Color.FromRgb(0, newColor.Color.G, newColor.Color.B);
            if (thisControl.MaxRStop is not null)
                thisControl.MaxRStop.Color = Avalonia.Media.Color.FromRgb(255, newColor.Color.G, newColor.Color.B);

            if (thisControl.MinGStop is not null)
                thisControl.MinGStop.Color = Avalonia.Media.Color.FromRgb(newColor.Color.R, 0, newColor.Color.B);
            if (thisControl.MaxGStop is not null)
                thisControl.MaxGStop.Color = Avalonia.Media.Color.FromRgb(newColor.Color.R, 255, newColor.Color.B);

            if (thisControl.MinBStop is not null)
                thisControl.MinBStop.Color = Avalonia.Media.Color.FromRgb(newColor.Color.R, newColor.Color.G, 0);
            if (thisControl.MaxBStop is not null)
                thisControl.MaxBStop.Color = Avalonia.Media.Color.FromRgb(newColor.Color.R, newColor.Color.G, 255);
        }

        /// <summary>
        /// Gets or sets the selected color as a <see cref="SolidColorBrush"/>.
        /// </summary>
        public SolidColorBrush Color
        {
            get
            {
                return (SolidColorBrush)this.GetValue(ColorProperty);
            }
            set
            {
                this.SetValue(ColorProperty, value);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ColorPicker"/> class.
        /// </summary>
        public ColorPicker()
        {
            // This call is required by the designer.
            this.InitializeComponent();

            // Note: Do NOT set DataContext = this here, as it would break
            // external bindings from parent controls (e.g., {Binding ColorProperty})
            this.Loaded += Color_Picker_Loaded;

            var previewDrawing = (DrawingGroup)this.PreviewDrawingBrush.Drawing;
            this.GradBrush1 = (SolidColorBrush)((GeometryDrawing)previewDrawing.Children[0]).Brush;
            this.GradStop2 = ((LinearGradientBrush)((GeometryDrawing)previewDrawing.Children[1]).Brush).GradientStops[1];
            this.MaxAStop = ((LinearGradientBrush)this.ASlider.Background).GradientStops[1];
            this.MinRStop = ((LinearGradientBrush)this.RSlider.Background).GradientStops[0];
            this.MaxRStop = ((LinearGradientBrush)this.RSlider.Background).GradientStops[1];
            this.MinGStop = ((LinearGradientBrush)this.GSlider.Background).GradientStops[0];
            this.MaxGStop = ((LinearGradientBrush)this.GSlider.Background).GradientStops[1];
            this.MinBStop = ((LinearGradientBrush)this.BSlider.Background).GradientStops[0];
            this.MaxBStop = ((LinearGradientBrush)this.BSlider.Background).GradientStops[1];

            // XGoodFit port: upstream's implicit Rectangle style had a MouseLeftButtonUp EventSetter, which
            // Avalonia's styles can't have. WPF raises the event on the rectangle under the pointer when the
            // button is released; Avalonia captures the pointer on a press, so the press handler releases it.
            foreach (Rectangle quickColor in this.QuickColorsPanel.Children.OfType<Rectangle>())
            {
                quickColor.PointerPressed += (sender, e) => e.Pointer.Capture(null);
                quickColor.PointerReleased += Rectangle_MouseLeftButtonUp;
            }
        }

        /// <summary>
        /// Builds the color gradient used in the vertical color slider when the template is applied. 
        /// </summary>
        /// <param name="e">The template's details.</param>
        /// <remarks>
        /// XGoodFit port: Avalonia's OnApplyTemplate is protected and takes the template's details. Its
        /// gradient points are relative points; it has no ColorInterpolationMode, and interpolates in sRGB,
        /// as WPF's SRgbLinearInterpolation.
        /// </remarks>
        protected override void OnApplyTemplate(TemplateAppliedEventArgs e)
        {

            base.OnApplyTemplate(e);
            var pickerBrush = new LinearGradientBrush() { StartPoint = new RelativePoint(0.5d, 0d, RelativeUnit.Relative), EndPoint = new RelativePoint(0.5d, 1d, RelativeUnit.Relative) };

            var colorsList = GenerateHsvSpectrum();
            double stopIncrement = 1d / colorsList.Count;

            int i;
            var loopTo = colorsList.Count - 1;
            for (i = 0; i <= loopTo; i++)
                pickerBrush.GradientStops.Add(new GradientStop(colorsList[i], i * stopIncrement));

            pickerBrush.GradientStops[i - 1].Offset = 1.0d;
            this.PART_ColorSlider.Background = pickerBrush;
            // PART_SpectrumDisplay.Fill = pickerBrush
            // 
            var SliderColor = ColorPicker.ConvertHsvToRgb(360d - this.PART_ColorSlider.Value, 1d, 1d);
            this.GradBrush1.Color = SliderColor;
            this.GradStop2.Color = SliderColor;
            // 
        }

        /// <summary>
        /// Handles the <see cref="Control.Loaded"/> event to initialize marker and slider if necessary.
        /// </summary>
        /// <param name="sender">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private void Color_Picker_Loaded(object sender, RoutedEventArgs e)
        {
            if (_isLoaded)
                return;
            if (Color == null)
                return;
            if (_updateMarker)
                UpdateMarkerPosition(Color.Color);
            if (_updateSlider)
                UpdateSliderPosition(Color.Color);
            // 
            _isLoaded = true;
        }

        /// <summary>
        /// Updates color based on slider hue change.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Event arguments containing the old and new slider values.</param>
        private void PART_ColorSlider_ValueChanged(object sender, RangeBaseValueChangedEventArgs e)
        {
            var SliderColor = ConvertHsvToRgb(360d - e.NewValue, 1d, 1d);
            this.GradBrush1.Color = SliderColor;
            this.GradStop2.Color = SliderColor;
            _updateSlider = false;
            Color = DetermineColorFromPreview(_colorPosition);
            _updateSlider = true;
            // UpdateControls(, False)
        }

        /// <summary>
        /// Identifies the preview border when cursor is moving.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void PreviewBorder_MouseUp(object sender, PointerReleasedEventArgs e)
        {
            this.PreviewBorder.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// Updates the preview border marker on mouse click.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void PreviewBorder_MouseLeftButtonDown(object sender, PointerPressedEventArgs e)
        {
            // XGoodFit port: the handler had only the left button's presses. WPF doesn't capture the mouse
            // here, so the preview stops following it once it leaves; Avalonia captures the pointer on a
            // press, so this releases it.
            if (e.GetCurrentPoint(this.PreviewBorder).Properties.PointerUpdateKind != PointerUpdateKind.LeftButtonPressed)
                return;
            e.Pointer.Capture(null);
            this.PreviewBorder.Cursor = Cursors.ScrollAll;
            var p = e.GetPosition(this.PreviewBorder);
            UpdateMarkerPosition(p);
            _updateMarker = false;
            _updateSlider = false;
            Color = DetermineColorFromPreview(_colorPosition);
            _updateMarker = true;
            _updateSlider = true;
            // UpdateControls(False, False)
        }

        /// <summary>
        /// Updates the preview border marker when dragging.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void PreviewBorder_MouseMove(object sender, PointerEventArgs e)
        {
            if (e.GetCurrentPoint(this.PreviewBorder).Properties.IsLeftButtonPressed)
            {
                this.PreviewBorder.Cursor = Cursors.ScrollAll;
                var p = e.GetPosition(this.PreviewBorder);
                UpdateMarkerPosition(p);
                _updateMarker = false;
                _updateSlider = false;
                Color = DetermineColorFromPreview(_colorPosition);
                _updateMarker = true;
                _updateSlider = true;
                // XGoodFit port: Avalonia updates the pointer-over state itself, so there's no Mouse.Synchronize().
            }
        }

        /// <summary>
        /// Resets cursor and updates color when mouse leaves the preview border.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void PreviewBorder_MouseLeave(object sender, PointerEventArgs e)
        {
            if (e.GetCurrentPoint(this.PreviewBorder).Properties.IsLeftButtonPressed)
            {
                this.PreviewBorder.Cursor = Cursors.Cross;
                var p = e.GetPosition(this.PreviewBorder);
                UpdateMarkerPosition(p);
                _updateMarker = false;
                _updateSlider = false;
                Color = DetermineColorFromPreview(_colorPosition);
                _updateMarker = true;
                _updateSlider = true;
            }
        }

        /// <summary>
        /// Updates the marker position given a point.
        /// </summary>
        /// <param name="p">The point representing the new marker position.</param>
        private void UpdateMarkerPosition(Point p)
        {
            // XGoodFit port: Avalonia's points are immutable, and its actual size is the bounds' size.
            if (p.X > this.PreviewBorder.Bounds.Width)
                p = p.WithX(this.PreviewBorder.Bounds.Width);
            if (p.X < 0d)
                p = p.WithX(0d);
            if (p.Y > this.PreviewBorder.Bounds.Height)
                p = p.WithY(this.PreviewBorder.Bounds.Height);
            if (p.Y < 0d)
                p = p.WithY(0d);
            _markerTransform.X = p.X;
            _markerTransform.Y = p.Y;
            if (this.PreviewBorder.Bounds.Width > 0d)
                p = p.WithX(p.X / this.PreviewBorder.Bounds.Width);
            if (this.PreviewBorder.Bounds.Height > 0d)
                p = p.WithY(p.Y / this.PreviewBorder.Bounds.Height);
            _colorPosition = p;
            // 
            this.PART_ColorMarker1.RenderTransform = _markerTransform;
            this.PART_ColorMarker2.RenderTransform = _markerTransform;
        }

        /// <summary>
        /// Updates the marker position based on a <see cref="Color"/>
        /// </summary>
        /// <param name="theColor">The color to set the marker position for.</param>
        private void UpdateMarkerPosition(Color theColor)
        {
            _colorPosition = default;
            var hsv = ConvertRgbToHsv(theColor.R, theColor.G, theColor.B);
            var p = new Point(hsv.S, 1d - hsv.V);
            // XGoodFit port: Avalonia's points are immutable, and its actual size is the bounds' size.
            if (p.X > 1d)
                p = p.WithX(1d);
            if (p.X < 0d)
                p = p.WithX(0d);
            if (p.Y > 1d)
                p = p.WithY(1d);
            if (p.Y < 0d)
                p = p.WithY(0d);
            //
            p = p.WithX(p.X * this.PreviewBorder.Bounds.Width);
            p = p.WithY(p.Y * this.PreviewBorder.Bounds.Height);
            UpdateMarkerPosition(p);
        }

        /// <summary>
        /// Updates the slider position based on a <see cref="Color"/>
        /// </summary>
        /// <param name="newColor">The color to set the slider position for.</param>
        private void UpdateSliderPosition(Color newColor)
        {
            this.PART_ColorSlider.ValueChanged -= this.PART_ColorSlider_ValueChanged;
            var hsv = ConvertRgbToHsv(newColor.R, newColor.G, newColor.B);
            // Invariant: actualHue = 360 - slider.Value (see PART_ColorSlider_ValueChanged
            // and DetermineColorFromPreview). For grayscale (S ≈ 0), hue is undefined —
            // preserve the previous slider position instead of snapping to red.
            if (hsv.S > 0.00000001d)
                this.PART_ColorSlider.Value = 360d - hsv.H;
            var SliderColor = ColorPicker.ConvertHsvToRgb(360d - this.PART_ColorSlider.Value, 1d, 1d);
            this.GradBrush1.Color = SliderColor;
            this.GradStop2.Color = SliderColor;
            this.PART_ColorSlider.ValueChanged += this.PART_ColorSlider_ValueChanged;
        }

        /// <summary>
        /// Determines the color from current marker location and slider hue.
        /// </summary>
        /// <param name="p">The point representing the marker position in the preview area.</param>
        /// <returns>A <see cref="SolidColorBrush"/> representing the selected color.</returns>
        private SolidColorBrush DetermineColorFromPreview(Point p)
        {
            var hsv = new HsvColor(360d - this.PART_ColorSlider.Value, p.X, 1d - p.Y);
            var baseColor = ConvertHsvToRgb(hsv.H, hsv.S, hsv.V);
            // Preserve current alpha — but if it is 0 (fully transparent) the user
            // would see no change, so snap to opaque so the pick is visible.
            // XGoodFit port: Avalonia's colours are immutable.
            baseColor = Avalonia.Media.Color.FromArgb((Color is null || Color.Color.A == 0) ? (byte)255 : Color.Color.A, baseColor.R, baseColor.G, baseColor.B);
            return new SolidColorBrush(baseColor);
        }

        /// <summary>
        /// Handles color picking from preset color swatches.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void Rectangle_MouseLeftButtonUp(object sender, PointerReleasedEventArgs e)
        {
            // XGoodFit port: the handler had only the left button. A brush from XAML text is an immutable
            // solid brush in Avalonia, and colours are immutable.
            if (e.InitialPressMouseButton != MouseButton.Left)
                return;
            Rectangle r = (Rectangle)sender;
            var c = ((ISolidColorBrush)r.Fill).Color;
            c = Avalonia.Media.Color.FromArgb((Color is null || Color.Color.A == 0) ? (byte)255 : Color.Color.A, c.R, c.G, c.B);
            Color = new SolidColorBrush(c);
        }

        /// <summary>
        /// Repositions the marker if the preview border size changes.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Size changed event arguments.</param>
        private void PreviewBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (Color is not null)
                UpdateMarkerPosition(Color.Color);
        }


        #region HSV Stuff

        /// <summary>
        /// Generates a list of colors with hues ranging from 0 to 360
        /// and a saturation and value of 1.
        /// </summary>
        /// <returns>A list of colors representing the HSV spectrum.</returns>
        public static List<Color> GenerateHsvSpectrum()
        {
            var colorsList = new List<Color>(8);
            // 
            for (int i = 0; i <= 28; i++)
                colorsList.Add(ConvertHsvToRgb(i * 12, 1d, 1d));
            colorsList.Add(ConvertHsvToRgb(0d, 1d, 1d));
            // 
            return colorsList;
        }

        /// <summary>
        /// Converts an HSV color to an RGB color.
        /// </summary>
        /// <param name="h">The hue component (0-360).</param>
        /// <param name="s">The saturation component (0-1).</param>
        /// <param name="v">The value/brightness component (0-1).</param>
        /// <returns>A <see cref="Color"/> representing the RGB equivalent.</returns>
        public static Color ConvertHsvToRgb(double h, double s, double v)
        {

            double r;
            double g;
            double b;

            if (Math.Abs(s) < 0.00000001d)
            {
                r = v;
                g = v;
                b = v;
            }
            else
            {
                int i;
                double f;
                double p;
                double q;
                double t;

                h = Math.Abs(h - 360d) < 0.00000001d ? 0d : h / 60d;

                i = (int)Math.Round(Math.Truncate(h));
                f = h - i;
                // 
                p = v * (1.0d - s);
                q = v * (1.0d - s * f);
                t = v * (1.0d - s * (1.0d - f));
                // 
                switch (i)
                {
                    case 0:
                        {
                            r = v;
                            g = t;
                            b = p;
                            break;
                        }
                    case 1:
                        {
                            r = q;
                            g = v;
                            b = p;
                            break;
                        }
                    case 2:
                        {
                            r = p;
                            g = v;
                            b = t;
                            break;
                        }
                    case 3:
                        {
                            r = p;
                            g = q;
                            b = v;
                            break;
                        }
                    case 4:
                        {
                            r = t;
                            g = p;
                            b = v;
                            break;
                        }

                    default:
                        {
                            r = v;
                            g = p;
                            b = q;
                            break;
                        }
                }
            }
            // 
            return Avalonia.Media.Color.FromArgb(255, (byte)Math.Round(r * 255d), (byte)Math.Round(g * 255d), (byte)Math.Round(b * 255d));

        }

        /// <summary>
        /// Converts an RGB color to an HSV color.
        /// </summary>
        /// <param name="r">The red component (0-255).</param>
        /// <param name="g">The green component (0-255).</param>
        /// <param name="b">The blue component (0-255).</param>
        /// <returns>An <see cref="HsvColor"/> struct representing the HSV equivalent.</returns>
        public static HsvColor ConvertRgbToHsv(int r, int g, int b)
        {

            double delta;
            double min;
            double h = 0d;
            double s;
            double v;

            min = Math.Min(Math.Min(r, g), b);
            v = Math.Max(Math.Max(r, g), b);
            delta = v - min;

            s = Math.Abs(v) < 0.00000001d ? 0d : delta / v;

            if (Math.Abs(s - 0d) < 0.00000001d)
            {
                h = 0.0d;
            }
            else
            {
                if (Math.Abs(r - v) < 0.00000001d)
                {
                    h = (g - b) / delta;
                }
                else if (Math.Abs(g - v) < 0.00000001d)
                {
                    h = 2d + (b - r) / delta;
                }
                else if (Math.Abs(b - v) < 0.00000001d)
                {
                    h = 4d + (r - g) / delta;
                }
                // 
                h *= 60d;
                if (h < 0.0d)
                    h += 360d;
            }
            // 
            return new HsvColor() { H = h, S = s, V = v / 255d };

        }

        /// <summary>
        /// Represents a color in HSV (Hue, Saturation, Value) format.
        /// </summary>
        public struct HsvColor
        {
            /// <summary>The hue component (0-360).</summary>
            public double H;
            /// <summary>The saturation component (0-1).</summary>
            public double S;
            /// <summary>The value/brightness component (0-1).</summary>
            public double V;

            /// <summary>
            /// Initializes a new instance of the <see cref="HsvColor"/> struct.
            /// </summary>
            /// <param name="h">The hue component (0-360).</param>
            /// <param name="s">The saturation component (0-1).</param>
            /// <param name="v">The value/brightness component (0-1).</param>
            public HsvColor(double h, double s, double v)
            {
                H = h;
                S = s;
                V = v;
            }
        }

        #endregion

    }
}