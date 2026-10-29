// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SerializationRegressionTests.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Regression tests for OxyPlot serialization round-trips and edge cases.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf.Tests
{
    using System;
    using System.Threading;
    using System.Xml.Linq;

    using NUnit.Framework;

    using OxyPlot.Wpf;
    using OxyPlot.Wpf.Serialization;

    /// <summary>
    /// Regression tests for OxyPlot XML serialization and deserialization.
    /// </summary>
    [TestFixture]
    public class SerializationRegressionTests
    {
        /// <summary>
        /// Verifies that a PlotModel with title, subtitle, one LinearAxis, and one LineSeries
        /// survives a full serialize/deserialize round-trip with all properties preserved.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void PlotSerializer_RoundTrip_PreservesProperties()
        {
            // Arrange
            var plot = new Plot();
            plot.Title = "Test Title";
            plot.Subtitle = "Test Subtitle";

            var axis = new LinearAxis { Title = "X Axis", Position = OxyPlot.Axes.AxisPosition.Bottom };
            plot.Axes.Add(axis);

            var series = new LineSeries { Title = "Test Series" };
            plot.Series.Add(series);

            // Force the plot to synchronize its internal model
            plot.InvalidatePlot(true);

            // Act
            var element = PlotSerializer.ToXElement(plot);

            var plot2 = new Plot();
            PlotSerializer.FromXElement(plot2, element);

            // Assert
            Assert.AreEqual("Test Title", plot2.Title);
            Assert.AreEqual("Test Subtitle", plot2.Subtitle);
            Assert.AreEqual(1, plot2.Axes.Count);
            Assert.AreEqual(1, plot2.Series.Count);
            Assert.AreEqual("X Axis", plot2.Axes[0].Title);
        }

        /// <summary>
        /// Verifies that GetColorAttribute returns false and does not throw when given
        /// a malformed color string that cannot be parsed.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void GetColorAttribute_MalformedColorString_ReturnsFalseWithoutThrowing()
        {
            // Arrange
            var element = new XElement("Test");
            element.SetAttributeValue("Color", "not_a_color");

            // Act & Assert - should not throw
            bool result = PlotSerializer.GetColorAttribute(element, "Color", out var color);

            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that GetDataPointAttribute returns false when given invalid data
        /// that cannot be parsed as a data point.
        /// </summary>
        [Test]
        public void GetDataPointAttribute_InvalidData_ReturnsFalse()
        {
            // Arrange
            var element = new XElement("Test");
            element.SetAttributeValue("Point", "not,valid,data");

            // Act
            bool result = PlotSerializer.GetDataPointAttribute(element, "Point", out var dp);

            // Assert
            Assert.That(result, Is.False);
        }

        /// <summary>
        /// Verifies that a CategoryColorAxis can be serialized and deserialized
        /// with its type preserved through the round-trip.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void CategoryColorAxis_RoundTrip_PreservesAxisType()
        {
            // Arrange
            var plot = new Plot();
            var catAxis = new CategoryColorAxis { Title = "Category Color" };
            plot.Axes.Add(catAxis);
            plot.InvalidatePlot(true);

            // Act
            var element = PlotSerializer.ToXElement(plot);

            var plot2 = new Plot();
            PlotSerializer.FromXElement(plot2, element);

            // Assert
            Assert.AreEqual(1, plot2.Axes.Count);
            Assert.That(plot2.Axes[0], Is.InstanceOf<CategoryColorAxis>());
            Assert.AreEqual("Category Color", plot2.Axes[0].Title);
        }

        /// <summary>
        /// Verifies that a ScatterSeries can be serialized and deserialized
        /// with its type preserved through the round-trip.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void ScatterSeries_RoundTrip_PreservesSeriesType()
        {
            // Arrange
            var plot = new Plot();
            var scatter = new ScatterSeries { Title = "Scatter Test" };
            plot.Series.Add(scatter);
            plot.InvalidatePlot(true);

            // Act
            var element = PlotSerializer.ToXElement(plot);

            var plot2 = new Plot();
            PlotSerializer.FromXElement(plot2, element);

            // Assert
            Assert.AreEqual(1, plot2.Series.Count);
            Assert.That(plot2.Series[0], Is.InstanceOf<ScatterSeries>());
            Assert.AreEqual("Scatter Test", plot2.Series[0].Title);
        }

        /// <summary>
        /// Verifies that serializing an annotation with Name set to null does not
        /// throw a NullReferenceException.
        /// </summary>
        [Test]
        [Apartment(ApartmentState.STA)]
        public void AnnotationSerializer_NullName_DoesNotThrow()
        {
            // Arrange
            var plot = new Plot();
            var annotation = new LineAnnotation();
            annotation.Name = null;
            plot.Annotations.Add(annotation);
            plot.InvalidatePlot(true);

            // Act & Assert - should not throw NullReferenceException
            XElement element = null;
            Assert.DoesNotThrow(() =>
            {
                element = PlotSerializer.ToXElement(plot);
            });

            Assert.That(element, Is.Not.Null);
        }
    }
}
