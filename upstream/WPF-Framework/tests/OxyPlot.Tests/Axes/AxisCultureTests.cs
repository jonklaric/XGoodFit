// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AxisCultureTests.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Verifies that axis interval calculation works under non-US cultures.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Tests
{
    using System;
    using System.Globalization;
    using System.Threading;

    using NUnit.Framework;

    using OxyPlot.Axes;

    /// <summary>
    /// The Axis.cs:CalculateActualInterval helper uses a `removeNoise` lambda that round-trips
    /// doubles through `ToString("e14")` + `double.Parse`. Without explicit InvariantCulture
    /// the round-trip is culture-sensitive — same culture both legs avoids exceptions but
    /// drifts ActualMajorStep across machines if one ever computes the interval and another
    /// reads/uses the formatted value.
    /// </summary>
    [TestFixture]
    public class AxisCultureTests
    {
        /// <summary>
        /// A plot rendered under de-DE must produce the same `ActualMajorStep` as under en-US.
        /// This is the strongest assertion that the `removeNoise` lambda is culture-invariant.
        /// </summary>
        [Test]
        public void Axis_ActualMajorStep_MatchesEnUs_UnderGermanCulture()
        {
            double enUsMajorStep = ComputeMajorStepUnderCulture("en-US", 0.1, 9.9);
            double deDeMajorStep = ComputeMajorStepUnderCulture("de-DE", 0.1, 9.9);
            AssertBitExact(enUsMajorStep, deDeMajorStep, "ActualMajorStep");
        }

        [Test]
        public void Axis_ActualMajorStep_MatchesEnUs_UnderFrenchCulture()
        {
            double enUsMajorStep = ComputeMajorStepUnderCulture("en-US", 1234.5678, 5678.1234);
            double frFrMajorStep = ComputeMajorStepUnderCulture("fr-FR", 1234.5678, 5678.1234);
            AssertBitExact(enUsMajorStep, frFrMajorStep, "ActualMajorStep");
        }

        /// <summary>
        /// Turkish has unusual casing rules ("i"/"İ"); historically caused regressions in
        /// code that uppercases format strings. Verify axis math is unaffected.
        /// </summary>
        [Test]
        public void Axis_ActualMajorStep_MatchesEnUs_UnderTurkishCulture()
        {
            double enUsMajorStep = ComputeMajorStepUnderCulture("en-US", -3.14159, 3.14159);
            double trTrMajorStep = ComputeMajorStepUnderCulture("tr-TR", -3.14159, 3.14159);
            AssertBitExact(enUsMajorStep, trTrMajorStep, "ActualMajorStep");
        }

        [Test]
        public void Axis_RenderToSvg_DoesNotThrow_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var plot = new PlotModel { Title = "Culture Test" };
                plot.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Minimum = 0.1,
                    Maximum = 9.9,
                });
                plot.Axes.Add(new LinearAxis
                {
                    Position = AxisPosition.Left,
                    Minimum = 1234.5678,
                    Maximum = 5678.1234,
                });

                Assert.DoesNotThrow(() =>
                {
                    string svg = SvgExporter.ExportToString(plot, 800, 500, true);
                    Assert.That(svg, Is.Not.Null.And.Not.Empty);
                });
            });
        }

        [Test]
        public void Axis_UpdateThenFormatValue_DoesNotThrow_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var model = new PlotModel();
                var axis = new LinearAxis
                {
                    Position = AxisPosition.Bottom,
                    Minimum = 0.1,
                    Maximum = 1000.5,
                };
                model.Axes.Add(axis);

                Assert.DoesNotThrow(() =>
                {
                    ((IPlotModel)model).Update(true);
                    // FormatValue exercises ActualCulture path (uses CurrentCulture by default)
                    string label = axis.FormatValue(123.456);
                    Assert.That(label, Is.Not.Null.And.Not.Empty);
                });
            });
        }

        /// <summary>
        /// Builds a single-axis plot under the given culture, renders to SVG (which forces
        /// `UpdateIntervals → CalculateActualInterval → removeNoise`), and returns the
        /// resulting `ActualMajorStep`.
        /// </summary>
        private static double ComputeMajorStepUnderCulture(string cultureName, double min, double max)
        {
            double majorStep = 0;
            RunUnderCulture(cultureName, () =>
            {
                var plot = new PlotModel();
                var axis = new LinearAxis { Position = AxisPosition.Bottom, Minimum = min, Maximum = max };
                plot.Axes.Add(axis);
                plot.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Minimum = min, Maximum = max });
                // Rendering forces full interval computation (rendering loops through UpdateIntervals).
                _ = SvgExporter.ExportToString(plot, 800, 500, false);
                majorStep = axis.ActualMajorStep;
            });
            return majorStep;
        }

        private static void AssertBitExact(double expected, double actual, string label)
        {
            long expectedBits = BitConverter.DoubleToInt64Bits(expected);
            long actualBits = BitConverter.DoubleToInt64Bits(actual);
            Assert.That(actualBits, Is.EqualTo(expectedBits),
                $"{label}: expected {expected:R} (bits {expectedBits}), got {actual:R} (bits {actualBits})");
        }

        private static void RunUnderCulture(string cultureName, Action body)
        {
            var prevCulture = Thread.CurrentThread.CurrentCulture;
            try
            {
                var culture = new CultureInfo(cultureName);
                Thread.CurrentThread.CurrentCulture = culture;
                CultureInfo.CurrentCulture = culture;
                body();
            }
            finally
            {
                Thread.CurrentThread.CurrentCulture = prevCulture;
                CultureInfo.CurrentCulture = prevCulture;
            }
        }
    }
}
