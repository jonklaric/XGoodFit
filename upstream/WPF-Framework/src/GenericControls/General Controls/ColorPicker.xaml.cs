using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
    /// </remarks>
    public partial class ColorPicker:UserControl
    {

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
        /// <see cref="System.Windows.Media.Color"/>. Bindings and code-behind must
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
        /// </remarks>
        public static readonly DependencyProperty ColorProperty = DependencyProperty.Register(nameof(Color), typeof(SolidColorBrush), typeof(ColorPicker), new UIPropertyMetadata(Brushes.Black, ColorCallback));

        /// <summary>
        /// Callback executed when the <see cref="Color"/> property changes.
        /// Updates marker and slider portion and color previews.
        /// </summary>
        /// <param name="d">Object triggering event.</param>
        /// <param name="e">Event arguments.</param>
        private static void ColorCallback(DependencyObject d, DependencyPropertyChangedEventArgs e)
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
                thisControl.MaxAStop.Color = System.Windows.Media.Color.FromRgb(newColor.Color.R, newColor.Color.G, newColor.Color.B);

            if (thisControl.MinRStop is not null)
                thisControl.MinRStop.Color = System.Windows.Media.Color.FromRgb(0, newColor.Color.G, newColor.Color.B);
            if (thisControl.MaxRStop is not null)
                thisControl.MaxRStop.Color = System.Windows.Media.Color.FromRgb(255, newColor.Color.G, newColor.Color.B);

            if (thisControl.MinGStop is not null)
                thisControl.MinGStop.Color = System.Windows.Media.Color.FromRgb(newColor.Color.R, 0, newColor.Color.B);
            if (thisControl.MaxGStop is not null)
                thisControl.MaxGStop.Color = System.Windows.Media.Color.FromRgb(newColor.Color.R, 255, newColor.Color.B);

            if (thisControl.MinBStop is not null)
                thisControl.MinBStop.Color = System.Windows.Media.Color.FromRgb(newColor.Color.R, newColor.Color.G, 0);
            if (thisControl.MaxBStop is not null)
                thisControl.MaxBStop.Color = System.Windows.Media.Color.FromRgb(newColor.Color.R, newColor.Color.G, 255);
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
        }

        /// <summary>
        /// Builds the color gradient used in the vertical color slider when the template is applied. 
        /// </summary>
        public override void OnApplyTemplate()
        {

            base.OnApplyTemplate();
            var pickerBrush = new LinearGradientBrush() { StartPoint = new Point(0.5d, 0d), EndPoint = new Point(0.5d, 1d), ColorInterpolationMode = ColorInterpolationMode.SRgbLinearInterpolation };

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
        /// Handles the <see cref="FrameworkElement.Loaded"/> event to initialize marker and slider if necessary.
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
        private void PART_ColorSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
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
        private void PreviewBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            this.PreviewBorder.Cursor = Cursors.Cross;
        }

        /// <summary>
        /// Updates the preview border marker on mouse click.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void PreviewBorder_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
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
        private void PreviewBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                this.PreviewBorder.Cursor = Cursors.ScrollAll;
                var p = e.GetPosition(this.PreviewBorder);
                UpdateMarkerPosition(p);
                _updateMarker = false;
                _updateSlider = false;
                Color = DetermineColorFromPreview(_colorPosition);
                _updateMarker = true;
                _updateSlider = true;
                Mouse.Synchronize();
            }
        }

        /// <summary>
        /// Resets cursor and updates color when mouse leaves the preview border.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse event arguments.</param>
        private void PreviewBorder_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
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
            if (p.X > this.PreviewBorder.ActualWidth)
                p.X = this.PreviewBorder.ActualWidth;
            if (p.X < 0d)
                p.X = 0d;
            if (p.Y > this.PreviewBorder.ActualHeight)
                p.Y = this.PreviewBorder.ActualHeight;
            if (p.Y < 0d)
                p.Y = 0d;
            _markerTransform.X = p.X;
            _markerTransform.Y = p.Y;
            if (this.PreviewBorder.ActualWidth > 0d)
                p.X /= this.PreviewBorder.ActualWidth;
            if (this.PreviewBorder.ActualHeight > 0d)
                p.Y /= this.PreviewBorder.ActualHeight;
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
            if (p.X > 1d)
                p.X = 1d;
            if (p.X < 0d)
                p.X = 0d;
            if (p.Y > 1d)
                p.Y = 1d;
            if (p.Y < 0d)
                p.Y = 0d;
            // 
            p.X *= this.PreviewBorder.ActualWidth;
            p.Y *= this.PreviewBorder.ActualHeight;
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
            baseColor.A = (Color is null || Color.Color.A == 0) ? (byte)255 : Color.Color.A;
            return new SolidColorBrush(baseColor);
        }

        /// <summary>
        /// Handles color picking from preset color swatches.
        /// </summary>
        /// <param name="sender">The object that raised the event.</param>
        /// <param name="e">Mouse button event arguments.</param>
        private void Rectangle_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            Rectangle r = (Rectangle)sender;
            var c = ((SolidColorBrush)r.Fill).Color;
            c.A = (Color is null || Color.Color.A == 0) ? (byte)255 : Color.Color.A;
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
            return System.Windows.Media.Color.FromArgb(255, (byte)Math.Round(r * 255d), (byte)Math.Round(g * 255d), (byte)Math.Round(b * 255d));

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