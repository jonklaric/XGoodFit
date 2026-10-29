using System.Globalization;
using System.Windows;
using System.Windows.Media;
using Xunit;

namespace GenericControls.Tests.Converters
{
    /// <summary>
    /// Tests for converter classes in the GenericControls library.
    /// </summary>
    public class ConverterTests
    {
        /// <summary>
        /// Culture info used for converter testing.
        /// </summary>
        private readonly CultureInfo _culture = CultureInfo.InvariantCulture;

        #region ReverseBooleanConverter Tests

        /// <summary>
        /// Tests that ReverseBooleanConverter.Convert returns false when converting true.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_Convert_TrueReturnsFalse()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.Convert(true, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.Convert returns true when converting false.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_Convert_FalseReturnsTrue()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.Convert(false, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.Convert returns null when converting null.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_Convert_NullReturnsNull()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.Convert(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.Convert returns null when converting an invalid type.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.Convert("not a bool", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.Convert returns null when converting an integer.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_Convert_IntReturnsNull()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.Convert(1, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.ConvertBack returns false when converting true.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_ConvertBack_TrueReturnsFalse()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.ConvertBack(true, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.ConvertBack returns true when converting false.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_ConvertBack_FalseReturnsTrue()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.ConvertBack(false, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.ConvertBack returns null when converting null.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.ConvertBack(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ReverseBooleanConverter.ConvertBack returns null when converting an invalid type.
        /// </summary>
        [Fact]
        public void ReverseBooleanConverter_ConvertBack_InvalidTypeReturnsNull()
        {
            var converter = new ReverseBooleanConverter();
            var result = converter.ConvertBack("string", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        #endregion

        #region BooleanToVisibilityConverter Tests

        /// <summary>
        /// Tests that BooleanToVisibilityConverter has correct default values for TrueValue and FalseValue.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_DefaultValues()
        {
            var converter = new BooleanToVisibilityConverter();
            Assert.Equal(Visibility.Visible, converter.TrueValue);
            Assert.Equal(Visibility.Collapsed, converter.FalseValue);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.Convert returns Visible when converting true.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_Convert_TrueReturnsVisible()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert(true, typeof(Visibility), null, _culture);
            Assert.Equal(Visibility.Visible, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.Convert returns Collapsed when converting false.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_Convert_FalseReturnsCollapsed()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert(false, typeof(Visibility), null, _culture);
            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.Convert returns the FalseValue when converting null.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_Convert_NullReturnsFalseValue()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert(null, typeof(Visibility), null, _culture);
            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.Convert returns the FalseValue when converting an invalid type.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_Convert_InvalidTypeReturnsFalseValue()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.Convert("not a bool", typeof(Visibility), null, _culture);
            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.Convert works with custom TrueValue and FalseValue settings.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_Convert_CustomValues()
        {
            var converter = new BooleanToVisibilityConverter
            {
                TrueValue = Visibility.Hidden,
                FalseValue = Visibility.Visible
            };
            Assert.Equal(Visibility.Hidden, converter.Convert(true, typeof(Visibility), null, _culture));
            Assert.Equal(Visibility.Visible, converter.Convert(false, typeof(Visibility), null, _culture));
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.ConvertBack returns true when converting Visible.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_ConvertBack_VisibleReturnsTrue()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.ConvertBack(Visibility.Visible, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.ConvertBack returns false when converting Collapsed.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_ConvertBack_CollapsedReturnsFalse()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.ConvertBack(Visibility.Collapsed, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.ConvertBack returns false when converting Hidden.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_ConvertBack_HiddenReturnsFalse()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.ConvertBack(Visibility.Hidden, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.ConvertBack returns false when converting null.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_ConvertBack_NullReturnsFalse()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.ConvertBack(null, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.ConvertBack returns false when converting an invalid type.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_ConvertBack_InvalidTypeReturnsFalse()
        {
            var converter = new BooleanToVisibilityConverter();
            var result = converter.ConvertBack("string", typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToVisibilityConverter.ConvertBack works with custom TrueValue setting.
        /// </summary>
        [Fact]
        public void BooleanToVisibilityConverter_ConvertBack_CustomTrueValue()
        {
            var converter = new BooleanToVisibilityConverter
            {
                TrueValue = Visibility.Hidden
            };
            var result = converter.ConvertBack(Visibility.Hidden, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        #endregion

        #region BooleanToColorConverter Tests

        /// <summary>
        /// Tests that BooleanToColorConverter has correct default values for TrueValue and FalseValue.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_DefaultValues()
        {
            var converter = new BooleanToColorConverter();
            Assert.Equal(Colors.Black, converter.TrueValue);
            Assert.Equal(Colors.Transparent, converter.FalseValue);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.Convert returns the TrueValue color when converting true.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_Convert_TrueReturnsTrueColor()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.Convert(true, typeof(Color), null, _culture);
            Assert.Equal(Colors.Black, result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.Convert returns the FalseValue color when converting false.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_Convert_FalseReturnsFalseColor()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.Convert(false, typeof(Color), null, _culture);
            Assert.Equal(Colors.Transparent, result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.Convert returns null when converting null.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_Convert_NullReturnsNull()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.Convert(null, typeof(Color), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.Convert returns null when converting an invalid type.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.Convert("string", typeof(Color), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.Convert works with custom color values.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_Convert_CustomColors()
        {
            var converter = new BooleanToColorConverter
            {
                TrueValue = Colors.Red,
                FalseValue = Colors.Blue
            };
            Assert.Equal(Colors.Red, converter.Convert(true, typeof(Color), null, _culture));
            Assert.Equal(Colors.Blue, converter.Convert(false, typeof(Color), null, _culture));
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.ConvertBack returns true when converting the TrueValue color.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_ConvertBack_TrueColorReturnsTrue()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.ConvertBack(Colors.Black, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.ConvertBack returns false when converting the FalseValue color.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_ConvertBack_FalseColorReturnsFalse()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.ConvertBack(Colors.Transparent, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.ConvertBack returns null when converting null.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.ConvertBack(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.ConvertBack returns null when converting an unknown color.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_ConvertBack_UnknownColorReturnsNull()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.ConvertBack(Colors.Green, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToColorConverter.ConvertBack returns null when converting an invalid type.
        /// </summary>
        [Fact]
        public void BooleanToColorConverter_ConvertBack_InvalidTypeReturnsNull()
        {
            var converter = new BooleanToColorConverter();
            var result = converter.ConvertBack("string", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        #endregion

        #region BooleanToBrushConverter Tests

        /// <summary>
        /// Tests that BooleanToBrushConverter has correct default values for TrueValue and FalseValue.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_DefaultValues()
        {
            var converter = new BooleanToBrushConverter();
            Assert.Equal(Brushes.Black, converter.TrueValue);
            Assert.Equal(Brushes.Transparent, converter.FalseValue);
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.Convert returns the TrueValue brush when converting true.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_Convert_TrueReturnsTrueBrush()
        {
            var converter = new BooleanToBrushConverter();
            var result = converter.Convert(true, typeof(Brush), null, _culture);
            Assert.Equal(Brushes.Black, result);
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.Convert returns the FalseValue brush when converting false.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_Convert_FalseReturnsFalseBrush()
        {
            var converter = new BooleanToBrushConverter();
            var result = converter.Convert(false, typeof(Brush), null, _culture);
            Assert.Equal(Brushes.Transparent, result);
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.Convert returns null when converting null.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_Convert_NullReturnsNull()
        {
            var converter = new BooleanToBrushConverter();
            var result = converter.Convert(null, typeof(Brush), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.Convert returns null when converting an invalid type.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new BooleanToBrushConverter();
            var result = converter.Convert(123, typeof(Brush), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.Convert works with custom brush values.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_Convert_CustomBrushes()
        {
            var converter = new BooleanToBrushConverter
            {
                TrueValue = Brushes.Red,
                FalseValue = Brushes.Blue
            };
            Assert.Equal(Brushes.Red, converter.Convert(true, typeof(Brush), null, _culture));
            Assert.Equal(Brushes.Blue, converter.Convert(false, typeof(Brush), null, _culture));
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.ConvertBack returns null when converting null.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new BooleanToBrushConverter();
            var result = converter.ConvertBack(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToBrushConverter.ConvertBack returns null when converting an invalid type.
        /// </summary>
        [Fact]
        public void BooleanToBrushConverter_ConvertBack_InvalidTypeReturnsNull()
        {
            var converter = new BooleanToBrushConverter();
            var result = converter.ConvertBack("string", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        #endregion

        #region BooleanToTextConverter Tests

        /// <summary>
        /// Tests that BooleanToTextConverter default values.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_DefaultValues()
        {
            var converter = new BooleanToTextConverter();
            Assert.Equal("", converter.TrueValue);
            Assert.Equal("", converter.FalseValue);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.Convert true returns true text.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_Convert_TrueReturnsTrueText()
        {
            var converter = new BooleanToTextConverter
            {
                TrueValue = "Yes",
                FalseValue = "No"
            };
            var result = converter.Convert(true, typeof(string), null, _culture);
            Assert.Equal("Yes", result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.Convert false returns false text.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_Convert_FalseReturnsFalseText()
        {
            var converter = new BooleanToTextConverter
            {
                TrueValue = "Yes",
                FalseValue = "No"
            };
            var result = converter.Convert(false, typeof(string), null, _culture);
            Assert.Equal("No", result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_Convert_NullReturnsNull()
        {
            var converter = new BooleanToTextConverter();
            var result = converter.Convert(null, typeof(string), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.Convert invalid type returns null.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new BooleanToTextConverter();
            var result = converter.Convert(42, typeof(string), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.ConvertBack true text returns true.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_ConvertBack_TrueTextReturnsTrue()
        {
            var converter = new BooleanToTextConverter
            {
                TrueValue = "Yes",
                FalseValue = "No"
            };
            var result = converter.ConvertBack("Yes", typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.ConvertBack false text returns false.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_ConvertBack_FalseTextReturnsFalse()
        {
            var converter = new BooleanToTextConverter
            {
                TrueValue = "Yes",
                FalseValue = "No"
            };
            var result = converter.ConvertBack("No", typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new BooleanToTextConverter();
            var result = converter.ConvertBack(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.ConvertBack unknown text returns null.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_ConvertBack_UnknownTextReturnsNull()
        {
            var converter = new BooleanToTextConverter
            {
                TrueValue = "Yes",
                FalseValue = "No"
            };
            var result = converter.ConvertBack("Maybe", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.ConvertBack non string value uses to string.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_ConvertBack_NonStringValueUsesToString()
        {
            var converter = new BooleanToTextConverter
            {
                TrueValue = "42",
                FalseValue = "0"
            };
            var result = converter.ConvertBack(42, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that BooleanToTextConverter.ConvertBack empty string matches default true.
        /// </summary>
        [Fact]
        public void BooleanToTextConverter_ConvertBack_EmptyStringMatchesDefaultTrue()
        {
            var converter = new BooleanToTextConverter(); // Defaults are empty strings
            var result = converter.ConvertBack("", typeof(bool), null, _culture);
            // Both TrueValue and FalseValue are empty, TrueValue is checked first
            Assert.Equal(true, result);
        }

        #endregion

        #region BooleanToDoubleConverter Tests

        /// <summary>
        /// Tests that BooleanToDoubleConverter default values.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_DefaultValues()
        {
            var converter = new BooleanToDoubleConverter();
            Assert.Equal(0d, converter.TrueValue);
            Assert.Equal(0d, converter.FalseValue);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.Convert true returns true value.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_Convert_TrueReturnsTrueValue()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 1.0,
                FalseValue = 0.0
            };
            var result = converter.Convert(true, typeof(double), null, _culture);
            Assert.Equal(1.0, result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.Convert false returns false value.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_Convert_FalseReturnsFalseValue()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 1.0,
                FalseValue = 0.0
            };
            var result = converter.Convert(false, typeof(double), null, _culture);
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_Convert_NullReturnsNull()
        {
            var converter = new BooleanToDoubleConverter();
            var result = converter.Convert(null, typeof(double), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.Convert invalid type returns null.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new BooleanToDoubleConverter();
            var result = converter.Convert("string", typeof(double), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.Convert custom values.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_Convert_CustomValues()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 100.5,
                FalseValue = -50.25
            };
            Assert.Equal(100.5, converter.Convert(true, typeof(double), null, _culture));
            Assert.Equal(-50.25, converter.Convert(false, typeof(double), null, _culture));
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.ConvertBack true value returns true.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_ConvertBack_TrueValueReturnsTrue()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 1.0,
                FalseValue = 0.0
            };
            var result = converter.ConvertBack(1.0, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.ConvertBack false value returns false.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_ConvertBack_FalseValueReturnsFalse()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 1.0,
                FalseValue = 0.0
            };
            var result = converter.ConvertBack(0.0, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new BooleanToDoubleConverter();
            var result = converter.ConvertBack(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.ConvertBack unknown value returns null.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_ConvertBack_UnknownValueReturnsNull()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 1.0,
                FalseValue = 0.0
            };
            var result = converter.ConvertBack(2.0, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.ConvertBack string value parsed.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_ConvertBack_StringValueParsed()
        {
            var converter = new BooleanToDoubleConverter
            {
                TrueValue = 1.0,
                FalseValue = 0.0
            };
            var result = converter.ConvertBack("1.0", typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that BooleanToDoubleConverter.ConvertBack invalid string returns null.
        /// </summary>
        [Fact]
        public void BooleanToDoubleConverter_ConvertBack_InvalidStringReturnsNull()
        {
            var converter = new BooleanToDoubleConverter();
            var result = converter.ConvertBack("not a number", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        #endregion

        #region VisibilityToBooleanConverter Tests

        /// <summary>
        /// Tests that VisibilityToBooleanConverter default values.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_DefaultValues()
        {
            var converter = new VisibilityToBooleanConverter();
            Assert.True(converter.VisibleValue);
            Assert.False(converter.CollapsedValue);
            Assert.False(converter.HiddenValue);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.Convert visible returns true.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_Convert_VisibleReturnsTrue()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.Convert(Visibility.Visible, typeof(bool), null, _culture);
            Assert.Equal(true, result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.Convert collapsed returns false.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_Convert_CollapsedReturnsFalse()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.Convert(Visibility.Collapsed, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.Convert hidden returns false.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_Convert_HiddenReturnsFalse()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.Convert(Visibility.Hidden, typeof(bool), null, _culture);
            Assert.Equal(false, result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_Convert_NullReturnsNull()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.Convert(null, typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.Convert invalid type returns null.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.Convert("string", typeof(bool), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.Convert custom values.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_Convert_CustomValues()
        {
            var converter = new VisibilityToBooleanConverter
            {
                VisibleValue = false,
                CollapsedValue = true,
                HiddenValue = true
            };
            Assert.Equal(false, converter.Convert(Visibility.Visible, typeof(bool), null, _culture));
            Assert.Equal(true, converter.Convert(Visibility.Collapsed, typeof(bool), null, _culture));
            Assert.Equal(true, converter.Convert(Visibility.Hidden, typeof(bool), null, _culture));
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.ConvertBack true returns visible.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_ConvertBack_TrueReturnsVisible()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.ConvertBack(true, typeof(Visibility), null, _culture);
            Assert.Equal(Visibility.Visible, result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.ConvertBack false returns collapsed.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_ConvertBack_FalseReturnsCollapsed()
        {
            var converter = new VisibilityToBooleanConverter();
            var result = converter.ConvertBack(false, typeof(Visibility), null, _culture);
            // Note: Both Collapsed and Hidden are false by default, Collapsed checked first
            Assert.Equal(Visibility.Collapsed, result);
        }

        /// <summary>
        /// Tests that VisibilityToBooleanConverter.ConvertBack custom config.
        /// </summary>
        [Fact]
        public void VisibilityToBooleanConverter_ConvertBack_CustomConfig()
        {
            var converter = new VisibilityToBooleanConverter
            {
                VisibleValue = false,
                HiddenValue = true,
                CollapsedValue = false
            };
            var result = converter.ConvertBack(true, typeof(Visibility), null, _culture);
            Assert.Equal(Visibility.Hidden, result);
        }

        #endregion

        #region ColorToByteConverter Tests

        /// <summary>
        /// Tests that ColorToByteConverter default component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_DefaultComponent()
        {
            var converter = new ColorToByteConverter();
            Assert.Equal(ColorComponent.R, converter.Component);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert extracts red component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_ExtractsRedComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.R };
            var brush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));
            var result = converter.Convert(brush, typeof(byte), null, _culture);
            Assert.Equal((byte)128, result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert extracts green component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_ExtractsGreenComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.G };
            var brush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));
            var result = converter.Convert(brush, typeof(byte), null, _culture);
            Assert.Equal((byte)64, result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert extracts blue component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_ExtractsBlueComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.B };
            var brush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));
            var result = converter.Convert(brush, typeof(byte), null, _culture);
            Assert.Equal((byte)32, result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert extracts alpha component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_ExtractsAlphaComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.A };
            var brush = new SolidColorBrush(Color.FromArgb(200, 128, 64, 32));
            var result = converter.Convert(brush, typeof(byte), null, _culture);
            Assert.Equal((byte)200, result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_NullReturnsNull()
        {
            var converter = new ColorToByteConverter();
            var result = converter.Convert(null, typeof(byte), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert invalid type returns null.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_InvalidTypeReturnsNull()
        {
            var converter = new ColorToByteConverter();
            var result = converter.Convert("string", typeof(byte), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.Convert color object returns null.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_Convert_ColorObjectReturnsNull()
        {
            var converter = new ColorToByteConverter();
            var result = converter.Convert(Colors.Red, typeof(byte), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack updates red component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_UpdatesRedComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.R };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 100, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture); // Sets internal state
            var result = converter.ConvertBack(200, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(200, result.Color.R);
            Assert.Equal(64, result.Color.G);
            Assert.Equal(32, result.Color.B);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack updates green component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_UpdatesGreenComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.G };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);
            var result = converter.ConvertBack(200, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(128, result.Color.R);
            Assert.Equal(200, result.Color.G);
            Assert.Equal(32, result.Color.B);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack updates blue component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_UpdatesBlueComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.B };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);
            var result = converter.ConvertBack(200, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(128, result.Color.R);
            Assert.Equal(64, result.Color.G);
            Assert.Equal(200, result.Color.B);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack updates alpha component.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_UpdatesAlphaComponent()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.A };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 128, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);
            var result = converter.ConvertBack(100, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(100, result.Color.A);
            Assert.Equal(128, result.Color.R);
            Assert.Equal(64, result.Color.G);
            Assert.Equal(32, result.Color.B);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new ColorToByteConverter();
            var result = converter.ConvertBack(null, typeof(SolidColorBrush), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack no color set returns transparent.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_NoColorSetReturnsTransparent()
        {
            var converter = new ColorToByteConverter();
            var result = converter.ConvertBack(100, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(0, result.Color.A);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack string value parsed.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_StringValueParsed()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.R };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 100, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);
            var result = converter.ConvertBack("200", typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(200, result.Color.R);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack value clamped to byte range.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_ValueClampedToByteRange()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.R };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 100, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);

            // Value > 255 should result in byteValue = 0 (doesn't satisfy the condition)
            var result = converter.ConvertBack(300, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(0, result.Color.R);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack negative value results in zero.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_NegativeValueResultsInZero()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.R };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 100, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);
            var result = converter.ConvertBack(-10, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(0, result.Color.R);
        }

        /// <summary>
        /// Tests that ColorToByteConverter.ConvertBack double value rounded.
        /// </summary>
        [Fact]
        public void ColorToByteConverter_ConvertBack_DoubleValueRounded()
        {
            var converter = new ColorToByteConverter { Component = ColorComponent.R };
            var originalBrush = new SolidColorBrush(Color.FromArgb(255, 100, 64, 32));
            converter.Convert(originalBrush, typeof(byte), null, _culture);
            var result = converter.ConvertBack(150.7, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(151, result.Color.R); // Rounded
        }

        #endregion

        #region ColorToSolidBrushConverter Tests

        /// <summary>
        /// Tests that ColorToSolidBrushConverter.Convert color to brush.
        /// </summary>
        [Fact]
        public void ColorToSolidBrushConverter_Convert_ColorToBrush()
        {
            var converter = new ColorToSolidBrushConverter();
            var result = converter.Convert(Colors.Red, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(Colors.Red, result.Color);
        }

        /// <summary>
        /// Tests that ColorToSolidBrushConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void ColorToSolidBrushConverter_Convert_NullReturnsNull()
        {
            var converter = new ColorToSolidBrushConverter();
            var result = converter.Convert(null, typeof(SolidColorBrush), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ColorToSolidBrushConverter.Convert custom color.
        /// </summary>
        [Fact]
        public void ColorToSolidBrushConverter_Convert_CustomColor()
        {
            var converter = new ColorToSolidBrushConverter();
            var color = Color.FromArgb(128, 50, 100, 150);
            var result = converter.Convert(color, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(128, result.Color.A);
            Assert.Equal(50, result.Color.R);
            Assert.Equal(100, result.Color.G);
            Assert.Equal(150, result.Color.B);
        }

        /// <summary>
        /// Tests that ColorToSolidBrushConverter.ConvertBack brush to color.
        /// </summary>
        [Fact]
        public void ColorToSolidBrushConverter_ConvertBack_BrushToColor()
        {
            var converter = new ColorToSolidBrushConverter();
            var brush = new SolidColorBrush(Colors.Blue);
            var result = converter.ConvertBack(brush, typeof(Color), null, _culture);
            Assert.Equal(Colors.Blue, result);
        }

        /// <summary>
        /// Tests that ColorToSolidBrushConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void ColorToSolidBrushConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new ColorToSolidBrushConverter();
            var result = converter.ConvertBack(null, typeof(Color), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that ColorToSolidBrushConverter round trip.
        /// </summary>
        [Fact]
        public void ColorToSolidBrushConverter_RoundTrip()
        {
            var converter = new ColorToSolidBrushConverter();
            var originalColor = Color.FromArgb(200, 75, 125, 175);
            var brush = converter.Convert(originalColor, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            var resultColor = (Color)converter.ConvertBack(brush, typeof(Color), null, _culture);
            Assert.Equal(originalColor, resultColor);
        }

        #endregion

        #region GridlineColorLightConverter Tests

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert null returns default brush.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_NullReturnsDefaultBrush()
        {
            var converter = new GridlineColorLightConverter();
            var result = converter.Convert(null, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(51, result.Color.A);
            Assert.Equal(0, result.Color.R);
            Assert.Equal(0, result.Color.G);
            Assert.Equal(0, result.Color.B);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert invalid type returns default brush.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_InvalidTypeReturnsDefaultBrush()
        {
            var converter = new GridlineColorLightConverter();
            var result = converter.Convert("string", typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(51, result.Color.A);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert fully opaque brush gets alpha51.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_FullyOpaqueBrushGetsAlpha51()
        {
            var converter = new GridlineColorLightConverter();
            var brush = new SolidColorBrush(Color.FromArgb(255, 100, 150, 200));
            var result = converter.Convert(brush, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(51, result.Color.A);
            Assert.Equal(100, result.Color.R);
            Assert.Equal(150, result.Color.G);
            Assert.Equal(200, result.Color.B);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert semi transparent brush gets alpha51.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_SemiTransparentBrushGetsAlpha51()
        {
            var converter = new GridlineColorLightConverter();
            var brush = new SolidColorBrush(Color.FromArgb(100, 100, 150, 200));
            var result = converter.Convert(brush, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(51, result.Color.A);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert very transparent brush gets reduced alpha.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_VeryTransparentBrushGetsReducedAlpha()
        {
            var converter = new GridlineColorLightConverter();
            var brush = new SolidColorBrush(Color.FromArgb(50, 100, 150, 200));
            var result = converter.Convert(brush, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            // Alpha = 50 * 0.2 = 10
            Assert.Equal(10, result.Color.A);
            Assert.Equal(100, result.Color.R);
            Assert.Equal(150, result.Color.G);
            Assert.Equal(200, result.Color.B);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert alpha exactly51 gets alpha51.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_AlphaExactly51GetsAlpha51()
        {
            var converter = new GridlineColorLightConverter();
            var brush = new SolidColorBrush(Color.FromArgb(51, 100, 150, 200));
            var result = converter.Convert(brush, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(51, result.Color.A);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.Convert alpha zero stays zero.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_Convert_AlphaZeroStaysZero()
        {
            var converter = new GridlineColorLightConverter();
            var brush = new SolidColorBrush(Color.FromArgb(0, 100, 150, 200));
            var result = converter.Convert(brush, typeof(SolidColorBrush), null, _culture) as SolidColorBrush;
            Assert.NotNull(result);
            Assert.Equal(0, result.Color.A);
        }

        /// <summary>
        /// Tests that GridlineColorLightConverter.ConvertBack throws not implemented exception.
        /// </summary>
        [Fact]
        public void GridlineColorLightConverter_ConvertBack_ThrowsNotImplementedException()
        {
            var converter = new GridlineColorLightConverter();
            Assert.Throws<NotImplementedException>(() =>
                converter.ConvertBack(null, typeof(SolidColorBrush), null, _culture));
        }

        #endregion

        #region FontToFontFamilyConverter Tests

        /// <summary>
        /// Tests that FontToFontFamilyConverter.Convert string to font family.
        /// </summary>
        [Fact]
        public void FontToFontFamilyConverter_Convert_StringToFontFamily()
        {
            var converter = new FontToFontFamilyConverter();
            var result = converter.Convert("Arial", typeof(FontFamily), null, _culture) as FontFamily;
            Assert.NotNull(result);
            Assert.Equal("Arial", result.Source);
        }

        /// <summary>
        /// Tests that FontToFontFamilyConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void FontToFontFamilyConverter_Convert_NullReturnsNull()
        {
            var converter = new FontToFontFamilyConverter();
            var result = converter.Convert(null, typeof(FontFamily), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that FontToFontFamilyConverter.Convert int uses to string.
        /// </summary>
        [Fact]
        public void FontToFontFamilyConverter_Convert_IntUsesToString()
        {
            var converter = new FontToFontFamilyConverter();
            var result = converter.Convert(123, typeof(FontFamily), null, _culture) as FontFamily;
            Assert.NotNull(result);
            Assert.Equal("123", result.Source);
        }

        /// <summary>
        /// Tests that FontToFontFamilyConverter.ConvertBack font family to string.
        /// </summary>
        [Fact]
        public void FontToFontFamilyConverter_ConvertBack_FontFamilyToString()
        {
            var converter = new FontToFontFamilyConverter();
            var fontFamily = new FontFamily("Times New Roman");
            var result = converter.ConvertBack(fontFamily, typeof(string), null, _culture);
            Assert.Equal("Times New Roman", result);
        }

        /// <summary>
        /// Tests that FontToFontFamilyConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void FontToFontFamilyConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new FontToFontFamilyConverter();
            var result = converter.ConvertBack(null, typeof(string), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that FontToFontFamilyConverter round trip.
        /// </summary>
        [Fact]
        public void FontToFontFamilyConverter_RoundTrip()
        {
            var converter = new FontToFontFamilyConverter();
            var originalName = "Courier New";
            var fontFamily = converter.Convert(originalName, typeof(FontFamily), null, _culture) as FontFamily;
            var resultName = converter.ConvertBack(fontFamily, typeof(string), null, _culture);
            Assert.Equal(originalName, resultName);
        }

        #endregion

        #region FontFamilyToFontStringConverter Tests

        /// <summary>
        /// Tests that FontFamilyToFontStringConverter.Convert font family to string.
        /// </summary>
        [Fact]
        public void FontFamilyToFontStringConverter_Convert_FontFamilyToString()
        {
            var converter = new FontFamilyToFontStringConverter();
            var fontFamily = new FontFamily("Verdana");
            var result = converter.Convert(fontFamily, typeof(string), null, _culture);
            Assert.Equal("Verdana", result);
        }

        /// <summary>
        /// Tests that FontFamilyToFontStringConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void FontFamilyToFontStringConverter_Convert_NullReturnsNull()
        {
            var converter = new FontFamilyToFontStringConverter();
            var result = converter.Convert(null, typeof(string), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that FontFamilyToFontStringConverter.ConvertBack string to font family.
        /// </summary>
        [Fact]
        public void FontFamilyToFontStringConverter_ConvertBack_StringToFontFamily()
        {
            var converter = new FontFamilyToFontStringConverter();
            var result = converter.ConvertBack("Georgia", typeof(FontFamily), null, _culture) as FontFamily;
            Assert.NotNull(result);
            Assert.Equal("Georgia", result.Source);
        }

        /// <summary>
        /// Tests that FontFamilyToFontStringConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void FontFamilyToFontStringConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new FontFamilyToFontStringConverter();
            var result = converter.ConvertBack(null, typeof(FontFamily), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that FontFamilyToFontStringConverter.ConvertBack int uses to string.
        /// </summary>
        [Fact]
        public void FontFamilyToFontStringConverter_ConvertBack_IntUsesToString()
        {
            var converter = new FontFamilyToFontStringConverter();
            var result = converter.ConvertBack(456, typeof(FontFamily), null, _culture) as FontFamily;
            Assert.NotNull(result);
            Assert.Equal("456", result.Source);
        }

        /// <summary>
        /// Tests that FontFamilyToFontStringConverter round trip.
        /// </summary>
        [Fact]
        public void FontFamilyToFontStringConverter_RoundTrip()
        {
            var converter = new FontFamilyToFontStringConverter();
            var fontFamily = new FontFamily("Tahoma");
            var name = converter.Convert(fontFamily, typeof(string), null, _culture) as string;
            var resultFamily = converter.ConvertBack(name, typeof(FontFamily), null, _culture) as FontFamily;
            Assert.Equal(fontFamily.Source, resultFamily!.Source);
        }

        #endregion

        #region IntToDoubleConverter Tests

        /// <summary>
        /// Tests that IntToDoubleConverter.Convert int to double.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_Convert_IntToDouble()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.Convert(42, typeof(double), null, _culture);
            Assert.Equal(42.0, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_Convert_NullReturnsNull()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.Convert(null, typeof(double), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.Convert zero returns zero double.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_Convert_ZeroReturnsZeroDouble()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.Convert(0, typeof(double), null, _culture);
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.Convert negative int.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_Convert_NegativeInt()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.Convert(-100, typeof(double), null, _culture);
            Assert.Equal(-100.0, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.Convert max int.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_Convert_MaxInt()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.Convert(int.MaxValue, typeof(double), null, _culture);
            Assert.Equal((double)int.MaxValue, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.Convert min int.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_Convert_MinInt()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.Convert(int.MinValue, typeof(double), null, _culture);
            Assert.Equal((double)int.MinValue, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.ConvertBack double to int.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_ConvertBack_DoubleToInt()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.ConvertBack(42.0, typeof(int), null, _culture);
            Assert.Equal(42, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.ConvertBack(null, typeof(int), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.ConvertBack double rounded to int.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_ConvertBack_DoubleRoundedToInt()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.ConvertBack(42.9, typeof(int), null, _culture);
            Assert.Equal(43, result); // Convert.ToInt32 uses banker's rounding
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.ConvertBack zero double.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_ConvertBack_ZeroDouble()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.ConvertBack(0.0, typeof(int), null, _culture);
            Assert.Equal(0, result);
        }

        /// <summary>
        /// Tests that IntToDoubleConverter.ConvertBack negative double.
        /// </summary>
        [Fact]
        public void IntToDoubleConverter_ConvertBack_NegativeDouble()
        {
            var converter = new IntToDoubleConverter();
            var result = converter.ConvertBack(-50.5, typeof(int), null, _culture);
            Assert.Equal(-50, result);
        }

        #endregion

        #region DoubleToStringConverter Tests

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert string to double.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_StringToDouble()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert("42.5", typeof(double), null, _culture);
            Assert.Equal(42.5, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert null returns null.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_NullReturnsNull()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert(null, typeof(double), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert integer string.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_IntegerString()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert("100", typeof(double), null, _culture);
            Assert.Equal(100.0, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert negative string.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_NegativeString()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert("-75.25", typeof(double), null, _culture);
            Assert.Equal(-75.25, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert scientific notation.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_ScientificNotation()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert("1.5E2", typeof(double), null, _culture);
            Assert.Equal(150.0, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert invalid string returns zero.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_InvalidStringReturnsZero()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert("not a number", typeof(double), null, _culture);
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert empty string returns zero.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_EmptyStringReturnsZero()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert("", typeof(double), null, _culture);
            Assert.Equal(0.0, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.Convert double value.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_Convert_DoubleValue()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.Convert(123.456, typeof(double), null, _culture);
            Assert.Equal(123.456, result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.ConvertBack double to string.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_ConvertBack_DoubleToString()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.ConvertBack(42.5, typeof(string), null, _culture);
            Assert.Equal("42.5", result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.ConvertBack null returns null.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_ConvertBack_NullReturnsNull()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.ConvertBack(null, typeof(string), null, _culture);
            Assert.Null(result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.ConvertBack integer double.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_ConvertBack_IntegerDouble()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.ConvertBack(100.0, typeof(string), null, _culture);
            Assert.Equal("100", result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.ConvertBack negative value.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_ConvertBack_NegativeValue()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.ConvertBack(-75.25, typeof(string), null, _culture);
            Assert.Equal("-75.25", result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter.ConvertBack zero.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_ConvertBack_Zero()
        {
            var converter = new DoubleToStringConverter();
            var result = converter.ConvertBack(0.0, typeof(string), null, _culture);
            Assert.Equal("0", result);
        }

        /// <summary>
        /// Tests that DoubleToStringConverter round trip.
        /// </summary>
        [Fact]
        public void DoubleToStringConverter_RoundTrip()
        {
            var converter = new DoubleToStringConverter();
            var original = "123.456";
            var doubleValue = (double)converter.Convert(original, typeof(double), null, _culture);
            var result = converter.ConvertBack(doubleValue, typeof(string), null, _culture);
            Assert.Equal("123.456", result);
        }

        #endregion
    }
}
