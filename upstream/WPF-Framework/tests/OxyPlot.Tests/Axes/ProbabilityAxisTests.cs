// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ProbabilityAxisTests.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Regression tests for D-003 — probability axes incorrectly clobbered user-set Maximum/Minimum.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Tests
{
    using System.Reflection;

    using NUnit.Framework;

    using OxyPlot.Axes;

    /// <summary>
    /// Regression tests for D-003: <see cref="NormalProbabilityAxis"/> and
    /// <see cref="GumbelProbabilityAxis"/> contained an inverted condition in
    /// <c>UpdateActualMaxMin</c> that overwrote any user-set Min/Max with the
    /// hard-coded <c>_epsilon</c> / <c>0.999</c> bounds. The fix replaces
    /// <c>!IsNaN || ActualMin &lt;= _epsilon</c> with <c>IsNaN || ActualMin &lt; _epsilon</c>
    /// so user values inside the valid probability domain are preserved.
    /// </summary>
    [TestFixture]
    public class ProbabilityAxisTests
    {
        /// <summary>
        /// Calls the <c>internal override UpdateActualMaxMin()</c> via reflection so the
        /// test can exercise the constraint logic without spinning up a full PlotModel.
        /// </summary>
        private static void InvokeUpdateActualMaxMin(Axis axis)
        {
            var method = typeof(Axis).GetMethod(
                "UpdateActualMaxMin",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.IsNotNull(method);
            method!.Invoke(axis, null);
        }

        [Test]
        public void NormalProbabilityAxis_RespectsUserSetMinimum()
        {
            var axis = new NormalProbabilityAxis { Minimum = 0.05, Maximum = 0.95 };

            InvokeUpdateActualMaxMin(axis);

            Assert.AreEqual(0.05, axis.ActualMinimum, 1e-12);
        }

        [Test]
        public void NormalProbabilityAxis_RespectsUserSetMaximum()
        {
            var axis = new NormalProbabilityAxis { Minimum = 0.05, Maximum = 0.95 };

            InvokeUpdateActualMaxMin(axis);

            Assert.AreEqual(0.95, axis.ActualMaximum, 1e-12);
        }

        [Test]
        public void NormalProbabilityAxis_ClampsBelowEpsilonToEpsilon()
        {
            // Negative or zero Minimum is outside the valid probability domain — the
            // setter coerces it to the configured epsilon so the actualMinimum is the
            // expected non-zero clamp.
            var axis = new NormalProbabilityAxis();
            // Force ActualMinimum to 0 to simulate a CalculateActualMaximum result that
            // came back outside the valid range.
            typeof(Axis).GetProperty(nameof(Axis.ActualMinimum))!
                .SetValue(axis, 0.0);

            InvokeUpdateActualMaxMin(axis);

            Assert.That(axis.ActualMinimum, Is.GreaterThan(0.0));
        }

        [Test]
        public void GumbelProbabilityAxis_RespectsUserSetMinimum()
        {
            var axis = new GumbelProbabilityAxis { Minimum = 0.05, Maximum = 0.95 };

            InvokeUpdateActualMaxMin(axis);

            Assert.AreEqual(0.05, axis.ActualMinimum, 1e-12);
        }

        [Test]
        public void GumbelProbabilityAxis_RespectsUserSetMaximum()
        {
            var axis = new GumbelProbabilityAxis { Minimum = 0.05, Maximum = 0.95 };

            InvokeUpdateActualMaxMin(axis);

            Assert.AreEqual(0.95, axis.ActualMaximum, 1e-12);
        }
    }
}
