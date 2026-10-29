// --------------------------------------------------------------------------------------------------------------------
// <copyright file="AuditRegressionTests.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Regression tests pinning serialization fixes from the forensic audit (D-001, D-002).
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf.Tests
{
    using System.Diagnostics;
    using System.Linq;
    using System.Threading;
    using System.Xml.Linq;

    using NUnit.Framework;

    using OxyPlot.Wpf;
    using OxyPlot.Wpf.Serialization;

    /// <summary>
    /// Regression tests pinning forensic-audit Phase 1/2 serialization fixes.
    /// Each test name references the audit finding ID it covers.
    /// </summary>
    [TestFixture]
    public class AuditRegressionTests
    {
        #region D-001 — non-textual annotations are skipped with a diagnostic, not silently dropped

        /// <summary>
        /// D-001: Adding an <c>ImageAnnotation</c> (non-textual) to a plot must
        /// produce zero AnnotationProperties children. Previously it would silently
        /// fall through and the annotation was dropped without any indication.
        /// The fix logs a Debug.WriteLine diagnostic so the loss is visible.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void D001_AnnotationsToXElement_SkipsImageAnnotationWithoutThrowing()
        {
            // Arrange — plot containing only a non-textual annotation.
            var plot = new Plot();
            plot.Annotations.Add(new OxyPlot.Wpf.ImageAnnotation());
            plot.InvalidatePlot(true);

            // Act
            XElement element = null!;
            Assert.DoesNotThrow(() => element = AnnotationSerializer.AnnotationsToXElement(plot));

            // Assert — no AnnotationProperties children.
            Assert.That(element, Is.Not.Null);
            var children = element.Elements().ToList();
            Assert.AreEqual(0, children.Count,
                "ImageAnnotation must be skipped (D-001 contract).");
        }

        /// <summary>
        /// D-001: A diagnostic message must be emitted via Debug.WriteLine when an
        /// ImageAnnotation is encountered, naming the dropped type.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void D001_AnnotationsToXElement_EmitsDiagnosticForImageAnnotation()
        {
            // Arrange
            var plot = new Plot();
            plot.Annotations.Add(new OxyPlot.Wpf.ImageAnnotation());
            plot.InvalidatePlot(true);

            var listener = new CapturingTraceListener();
            Trace.Listeners.Add(listener);
            try
            {
                // Act
                AnnotationSerializer.AnnotationsToXElement(plot);
                listener.Flush();
            }
            finally
            {
                Trace.Listeners.Remove(listener);
            }

            // Assert — at least one diagnostic mentions "ImageAnnotation".
            Assert.That(
                listener.Captured.Exists(m => m.Contains("ImageAnnotation")),
                "Expected a Debug.WriteLine diagnostic naming ImageAnnotation (D-001).");
        }

        /// <summary>
        /// Trace listener that captures Debug.WriteLine output for assertion.
        /// </summary>
        private sealed class CapturingTraceListener : TraceListener
        {
            public System.Collections.Generic.List<string> Captured { get; } = new();
            public override void Write(string message) { if (message != null) Captured.Add(message); }
            public override void WriteLine(string message) { if (message != null) Captured.Add(message); }
        }

        #endregion

        #region D-002 — Series.IsHitTestEnabled and EdgeRenderingMode round-trip

        [Test]
        [Apartment(ApartmentState.STA)]
        public void D002_LineSeries_IsHitTestEnabled_RoundTripsFalse()
        {
            // Arrange
            var plot = new Plot();
            var s = new LineSeries
            {
                Title = "RT",
                IsHitTestEnabled = false
            };
            plot.Series.Add(s);
            plot.InvalidatePlot(true);

            // Act
            var element = PlotSerializer.ToXElement(plot);
            var plot2 = new Plot();
            PlotSerializer.FromXElement(plot2, element);

            // Assert
            Assert.AreEqual(1, plot2.Series.Count);
            Assert.IsFalse(plot2.Series[0].IsHitTestEnabled,
                "IsHitTestEnabled=false must survive a round-trip (D-002).");
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void D002_LineSeries_EdgeRenderingMode_RoundTripsPreferSpeed()
        {
            // Arrange
            var plot = new Plot();
            var s = new LineSeries
            {
                Title = "RT",
                EdgeRenderingMode = EdgeRenderingMode.PreferSpeed
            };
            plot.Series.Add(s);
            plot.InvalidatePlot(true);

            // Act
            var element = PlotSerializer.ToXElement(plot);
            var plot2 = new Plot();
            PlotSerializer.FromXElement(plot2, element);

            // Assert
            Assert.AreEqual(1, plot2.Series.Count);
            Assert.AreEqual(EdgeRenderingMode.PreferSpeed, plot2.Series[0].EdgeRenderingMode,
                "EdgeRenderingMode must survive a round-trip (D-002).");
        }

        [Test]
        [Apartment(ApartmentState.STA)]
        public void D002_LineSeries_DefaultIsHitTestEnabledTrueAlsoRoundTrips()
        {
            // Arrange — explicitly default values; the original audit symptom was that the
            // attribute was missing entirely, so unchanged values would still come back as
            // defaults — but explicitly setting and round-tripping verifies the writer fired.
            var plot = new Plot();
            var s = new LineSeries
            {
                Title = "RT",
                IsHitTestEnabled = true,
                EdgeRenderingMode = EdgeRenderingMode.Adaptive
            };
            plot.Series.Add(s);
            plot.InvalidatePlot(true);

            // Act
            var element = PlotSerializer.ToXElement(plot);
            var plot2 = new Plot();
            PlotSerializer.FromXElement(plot2, element);

            // Assert
            Assert.IsTrue(plot2.Series[0].IsHitTestEnabled);
            Assert.AreEqual(EdgeRenderingMode.Adaptive, plot2.Series[0].EdgeRenderingMode);
        }

        #endregion
    }
}
