// --------------------------------------------------------------------------------------------------------------------
// <copyright file="CultureRoundTripTests.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Verifies plot serialization survives a round-trip under non-US cultures.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf.Tests
{
    using System;
    using System.Globalization;
    using System.Threading;
    using System.Xml.Linq;

    using NUnit.Framework;

    using OxyPlot.Wpf;
    using OxyPlot.Wpf.Serialization;

    /// <summary>
    /// On a German machine (decimal comma), naive ToString/Parse without InvariantCulture
    /// would write "1,5" instead of "1.5", breaking interchange across machines.
    /// </summary>
    [TestFixture]
    public class CultureRoundTripTests
    {
        // Each value has a decimal point in its G17 mantissa, so under bare (non-Invariant)
        // ToString on de-DE it would serialize with a comma separator, then fail or drift on
        // Invariant parse. MaxValue/MinValue specifically catch a 1-ULP precision drift that
        // occurs when bare TryParse on de-DE reads an Invariant-formatted string.
        private static readonly double[] TrickyValues =
        {
            1234.5678,
            -987.654321,
            1.23456789e-10,
            1.23456789e+15,
            0.1,
            double.MaxValue,
            double.MinValue,
            1e-300,
            -1e+250,
        };

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PlotSerializer_RoundTrip_PreservesAxisBoundsBitExact_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var plot = BuildPlot(min: 1234.5678, max: 9876.54321, majorStep: 0.1);
                plot.InvalidatePlot(true);

                XElement element = PlotSerializer.ToXElement(plot);

                var plot2 = new Plot();
                PlotSerializer.FromXElement(plot2, element);

                Assert.That(plot2.Axes.Count, Is.EqualTo(plot.Axes.Count));
                AssertBitExact(plot.Axes[0].Minimum, plot2.Axes[0].Minimum, "Axes[0].Minimum");
                AssertBitExact(plot.Axes[0].Maximum, plot2.Axes[0].Maximum, "Axes[0].Maximum");
                AssertBitExact(plot.Axes[0].MajorStep, plot2.Axes[0].MajorStep, "Axes[0].MajorStep");
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PlotSerializer_SerializedXml_UsesPeriodAsDecimalSeparator_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                var plot = BuildPlot(min: 1.5, max: 2.5, majorStep: 0.25);
                plot.InvalidatePlot(true);

                XElement element = PlotSerializer.ToXElement(plot);
                string xml = element.ToString();

                Assert.That(xml, Does.Contain("1.5"), $"Min '1.5' missing:\n{xml}");
                Assert.That(xml, Does.Contain("2.5"), $"Max '2.5' missing");
                Assert.That(xml, Does.Contain("0.25"), $"MajorStep '0.25' missing");
                StringAssert.DoesNotContain("1,5", xml, "German-formatted decimal leaked into serialized XML");
                StringAssert.DoesNotContain("0,25", xml, "German-formatted decimal leaked into serialized XML");
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PlotSerializer_LoadsUsCultureFileUnderGermanCulture()
        {
            // A user in Germany must be able to load a settings file produced in the US.
            XElement element = null;
            Plot sourcePlot = null;
            RunUnderCulture("en-US", () =>
            {
                sourcePlot = BuildPlot(min: 100.5, max: 999.999, majorStep: 0.1);
                sourcePlot.InvalidatePlot(true);
                element = PlotSerializer.ToXElement(sourcePlot);
            });

            RunUnderCulture("de-DE", () =>
            {
                var plot2 = new Plot();
                PlotSerializer.FromXElement(plot2, element);
                AssertBitExact(sourcePlot.Axes[0].Minimum, plot2.Axes[0].Minimum, "Min");
                AssertBitExact(sourcePlot.Axes[0].Maximum, plot2.Axes[0].Maximum, "Max");
                AssertBitExact(sourcePlot.Axes[0].MajorStep, plot2.Axes[0].MajorStep, "MajorStep");
            });
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void PlotSerializer_RoundTrip_PreservesTrickyDoubleValues_UnderGermanCulture()
        {
            RunUnderCulture("de-DE", () =>
            {
                foreach (double v in TrickyValues)
                {
                    var plot = BuildPlot(min: v, max: v + 1.0, majorStep: 0.1);
                    plot.InvalidatePlot(true);

                    XElement element = PlotSerializer.ToXElement(plot);
                    var plot2 = new Plot();
                    PlotSerializer.FromXElement(plot2, element);

                    AssertBitExact(plot.Axes[0].Minimum, plot2.Axes[0].Minimum, $"Min for {v:R}");
                }
            });
        }

        private static Plot BuildPlot(double min, double max, double majorStep)
        {
            var plot = new Plot();
            var xAxis = new LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Bottom,
                Minimum = min,
                Maximum = max,
                MajorStep = majorStep,
            };
            plot.Axes.Add(xAxis);
            var yAxis = new LinearAxis
            {
                Position = OxyPlot.Axes.AxisPosition.Left,
                Minimum = min,
                Maximum = max,
            };
            plot.Axes.Add(yAxis);
            return plot;
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

        private static void AssertBitExact(double expected, double actual, string label)
        {
            long expectedBits = BitConverter.DoubleToInt64Bits(expected);
            long actualBits = BitConverter.DoubleToInt64Bits(actual);
            Assert.That(actualBits, Is.EqualTo(expectedBits),
                $"{label}: expected {expected:R} (bits {expectedBits}), got {actual:R} (bits {actualBits})");
        }
    }
}
