using System.Collections.Generic;
using ExpressionParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Regression tests pinning forensic-audit Phase 2e Models corrections.
    /// Each test name references the audit finding ID it covers.
    /// </summary>
    [TestClass]
    public class AuditRegressionTests
    {
        /// <summary>
        /// C-001 — Numeric binary nodes / IF nodes used to widen a Single-typed branch
        /// only when its OutputType was exactly <c>ResultType.Double</c>. A declared
        /// Single variable (or any non-Double float) would fall through the float check
        /// and cause integer-arithmetic truncation. The fix tests against the
        /// <c>FloatingPoint</c> flag (Double | Single) so float operands stay floating.
        /// This test pins the resulting precision: 1.5 must remain 1.5, not become 1.
        /// </summary>
        [TestMethod]
        public void C001_IfNode_PreservesFloatPrecisionWhenAllOperandsAreSingle()
        {
            var availableVariables = new Dictionary<string, ResultType>
            {
                ["a"] = ResultType.Single,
                ["b"] = ResultType.Single,
            };
            var node = ExpressionParser.Parser.Parser.Parse("IF(true, [a], [b])", false, availableVariables);

            // Set variable values.
            var variables = node.GetVariableNodes();
            foreach (var v in variables)
            {
                if (v.VariableName == "a") v.SetValue(1.5);
                else if (v.VariableName == "b") v.SetValue(2.5);
            }

            // Act
            var result = node.Evaluate();

            // Assert — the IF picks the true branch (1.5) and float precision is preserved.
            Assert.AreEqual(1.5, System.Convert.ToDouble(result.Result), 0.0001,
                "Float operands in IF must preserve their fractional value (C-001).");
        }

        /// <summary>
        /// C-001 — companion test: numeric binary operations on float operands must also
        /// widen via the FloatingPoint flag rather than the inverted-type-equality check.
        /// </summary>
        [TestMethod]
        public void C001_NumericBinary_FloatOperandsPreservePrecision()
        {
            var availableVariables = new Dictionary<string, ResultType>
            {
                ["a"] = ResultType.Single,
                ["b"] = ResultType.Single,
            };
            var node = ExpressionParser.Parser.Parser.Parse("[a] + [b]", false, availableVariables);

            var variables = node.GetVariableNodes();
            foreach (var v in variables)
            {
                if (v.VariableName == "a") v.SetValue(1.25);
                else if (v.VariableName == "b") v.SetValue(2.5);
            }

            var result = node.Evaluate();

            // Pre-fix this would fall through to Integer arithmetic and produce 3 (rounded).
            Assert.AreEqual(3.75, System.Convert.ToDouble(result.Result), 0.0001,
                "Single + Single must remain a floating-point sum (C-001).");
        }
    }
}
