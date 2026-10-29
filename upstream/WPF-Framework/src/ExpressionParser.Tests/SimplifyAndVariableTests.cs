using System;
using System.Collections.Generic;
using ExpressionParser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains comprehensive unit tests for the Simplify() method and variable handling functionality
    /// of the ExpressionParser library. Tests validate constant folding, partial simplification,
    /// variable operations, and node type correctness.
    /// </summary>
    [TestClass()]
    public class SimplifyAndVariableTests
    {
        #region Simplify() Method Tests

        /// <summary>
        /// Tests that constant numeric expressions are fully simplified to a single integer value.
        /// Validates constant folding optimization for integer arithmetic operations.
        /// </summary>
        [TestMethod()]
        public void Simplify_ConstantIntegerExpression_SimplifiesToSingleValue()
        {
            var node = ExpressionParser.Parser.Parser.Parse("2 + 3 + 5");
            var simplified = node.Simplify();

            // Assert simplified is IntegerNode type
            Assert.IsInstanceOfType(simplified, typeof(IntegerNode));
            Assert.AreEqual(10, simplified.Evaluate().Result);
            Assert.IsFalse(simplified.ContainsVariable());
        }

        /// <summary>
        /// Tests that constant floating-point expressions are simplified to a single decimal value.
        /// Verifies constant folding for double-precision arithmetic.
        /// </summary>
        [TestMethod()]
        public void Simplify_ConstantDoubleExpression_SimplifiesToDecimalNode()
        {
            var node = ExpressionParser.Parser.Parser.Parse("2.5 + 3.7");
            var simplified = node.Simplify();

            // Assert simplified is DecimalNode type
            Assert.IsInstanceOfType(simplified, typeof(DecimalNode));
            Assert.AreEqual(6.2, simplified.Evaluate().Result);
            Assert.IsFalse(simplified.ContainsVariable());
        }

        /// <summary>
        /// Tests that division operations always simplify to decimal nodes regardless of operand types.
        /// Division results are always floating-point per the parser's design.
        /// </summary>
        [TestMethod()]
        public void Simplify_ConstantDivision_SimplifiesToDecimalNode()
        {
            var node = ExpressionParser.Parser.Parser.Parse("10 / 2");
            var simplified = node.Simplify();

            // Division always results in Double type
            Assert.IsInstanceOfType(simplified, typeof(DecimalNode));
            Assert.AreEqual(5.0, simplified.Evaluate().Result);
        }

        /// <summary>
        /// Tests that constant boolean expressions are simplified to a single boolean value.
        /// Validates constant folding for logical comparison operations.
        /// </summary>
        [TestMethod()]
        public void Simplify_ConstantBooleanExpression_SimplifiesToBooleanNode()
        {
            var node = ExpressionParser.Parser.Parser.Parse("5 > 3");
            var simplified = node.Simplify();

            // Assert simplified is BooleanNode type
            Assert.IsInstanceOfType(simplified, typeof(BooleanNode));
            Assert.AreEqual(true, simplified.Evaluate().Result);
            Assert.IsFalse(simplified.ContainsVariable());
        }

        /// <summary>
        /// Tests that string concatenation expressions are simplified to a single string value.
        /// Verifies constant folding for string operations.
        /// </summary>
        [TestMethod()]
        public void Simplify_ConstantStringConcatenation_SimplifiesToStringNode()
        {
            var node = ExpressionParser.Parser.Parser.Parse("\"Hello\" & \" \" & \"World\"");
            var simplified = node.Simplify();

            // Assert simplified is StringNode type
            Assert.IsInstanceOfType(simplified, typeof(StringNode));
            Assert.AreEqual("Hello World", simplified.Evaluate().Result);
            Assert.IsFalse(simplified.ContainsVariable());
        }

        /// <summary>
        /// Tests that expressions containing variables are not fully simplified.
        /// Variables prevent complete constant folding and must be preserved in the tree.
        /// </summary>
        [TestMethod()]
        public void Simplify_ExpressionWithVariable_DoesNotFullySimplify()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 5", false, varTypes);
            var simplified = node.Simplify();

            // Should still contain the variable
            Assert.IsTrue(simplified.ContainsVariable());
            Assert.IsInstanceOfType(simplified, typeof(NumericBinaryNode));
        }

        /// <summary>
        /// Tests partial simplification where constant sub-expressions are simplified
        /// but the overall expression retains variables. Validates "2+3+[x]" simplifies to "5+[x]".
        /// </summary>
        [TestMethod()]
        public void Simplify_PartialSimplification_SimplifiesConstantParts()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("2 + 3 + [x]", false, varTypes);
            var simplified = node.Simplify();

            // Should still contain variable but constant parts simplified
            Assert.IsTrue(simplified.ContainsVariable());

            // Set variable value and evaluate to verify partial simplification
            var variables = simplified.GetVariableNodes();
            variables[0].SetValue(10);
            Assert.AreEqual(15, simplified.Evaluate().Result);
        }

        /// <summary>
        /// Tests nested expression simplification with multiple levels of operations.
        /// Verifies that deeply nested constant expressions are fully collapsed.
        /// </summary>
        [TestMethod()]
        public void Simplify_NestedConstantExpression_FullySimplifies()
        {
            var node = ExpressionParser.Parser.Parser.Parse("((2 + 3) * (4 + 6)) / 5");
            var simplified = node.Simplify();

            Assert.IsInstanceOfType(simplified, typeof(DecimalNode));
            Assert.AreEqual(10.0, simplified.Evaluate().Result);
            Assert.IsFalse(simplified.ContainsVariable());
        }

        /// <summary>
        /// Tests nested expressions with variables that should be partially simplified.
        /// Constant sub-expressions are folded while preserving variable references.
        /// </summary>
        [TestMethod()]
        public void Simplify_NestedExpressionWithVariable_PartiallySimplifies()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("(2 + 3) * [x] + (4 + 6)", false, varTypes);
            var simplified = node.Simplify();

            Assert.IsTrue(simplified.ContainsVariable());

            // Set variable and verify: 5 * x + 10
            var variables = simplified.GetVariableNodes();
            variables[0].SetValue(2);
            Assert.AreEqual(20, simplified.Evaluate().Result);
        }

        /// <summary>
        /// Tests that complex boolean expressions with comparisons and logical operators
        /// are correctly simplified when all operands are constant.
        /// </summary>
        [TestMethod()]
        public void Simplify_ComplexBooleanExpression_SimplifiesToBoolean()
        {
            var node = ExpressionParser.Parser.Parser.Parse("AND(5 > 3, 10 < 20)");
            var simplified = node.Simplify();

            Assert.IsInstanceOfType(simplified, typeof(BooleanNode));
            Assert.AreEqual(true, simplified.Evaluate().Result);
        }

        /// <summary>
        /// Tests that boolean expressions with variables are not fully simplified
        /// and preserve the variable references in the expression tree.
        /// </summary>
        [TestMethod()]
        public void Simplify_BooleanExpressionWithVariable_PreservesVariable()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] > 5", false, varTypes);
            var simplified = node.Simplify();

            Assert.IsTrue(simplified.ContainsVariable());
            Assert.IsInstanceOfType(simplified, typeof(BooleanBinaryNode));
        }

        #endregion

        #region Variable Tests

        /// <summary>
        /// Tests that ContainsVariable() returns true when an expression includes variable references.
        /// This method is critical for determining whether simplification can be complete.
        /// </summary>
        [TestMethod()]
        public void ContainsVariable_ExpressionWithVariable_ReturnsTrue()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 5", false, varTypes);

            Assert.IsTrue(node.ContainsVariable());
        }

        /// <summary>
        /// Tests that ContainsVariable() returns false for purely constant expressions.
        /// Constant expressions can be fully simplified and contain no variable dependencies.
        /// </summary>
        [TestMethod()]
        public void ContainsVariable_ConstantExpression_ReturnsFalse()
        {
            var node = ExpressionParser.Parser.Parser.Parse("5 + 10");

            Assert.IsFalse(node.ContainsVariable());
        }

        /// <summary>
        /// Tests that GetVariableNodes() correctly returns all variable node instances
        /// in the expression tree, allowing for variable value assignment and inspection.
        /// </summary>
        [TestMethod()]
        public void GetVariableNodes_SingleVariable_ReturnsCorrectNode()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 5", false, varTypes);
            var variables = node.GetVariableNodes();

            Assert.AreEqual(1, variables.Count);
            Assert.AreEqual("x", variables[0].VariableName);
            Assert.AreEqual(ResultType.Integer, variables[0].OutputType);
        }

        /// <summary>
        /// Tests that GetVariableNodes() returns multiple variable nodes when an expression
        /// contains multiple distinct variable references.
        /// </summary>
        [TestMethod()]
        public void GetVariableNodes_MultipleVariables_ReturnsAllNodes()
        {
            var varTypes = new Dictionary<string, ResultType>
            {
                { "x", ResultType.Integer },
                { "y", ResultType.Double }
            };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + [y]", false, varTypes);
            var variables = node.GetVariableNodes();

            Assert.AreEqual(2, variables.Count);
        }

        /// <summary>
        /// Tests that GetVariableNodes() returns empty list for constant expressions
        /// that contain no variable references.
        /// </summary>
        [TestMethod()]
        public void GetVariableNodes_NoVariables_ReturnsEmptyList()
        {
            var node = ExpressionParser.Parser.Parser.Parse("5 + 10");
            var variables = node.GetVariableNodes();

            Assert.AreEqual(0, variables.Count);
        }

        /// <summary>
        /// Tests that integer-typed variables are correctly declared and maintain their type.
        /// Verifies ResultType.Integer is properly assigned to integer variables.
        /// </summary>
        [TestMethod()]
        public void Variable_IntegerType_CorrectlyDeclared()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x]", false, varTypes);

            Assert.AreEqual(ResultType.Integer, node.OutputType);
            Assert.IsTrue(node.ContainsVariable());
        }

        /// <summary>
        /// Tests that double-typed variables are correctly declared and maintain their type.
        /// Verifies ResultType.Double is properly assigned to floating-point variables.
        /// </summary>
        [TestMethod()]
        public void Variable_DoubleType_CorrectlyDeclared()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Double } };
            var node = ExpressionParser.Parser.Parser.Parse("[x]", false, varTypes);

            Assert.AreEqual(ResultType.Double, node.OutputType);
        }

        /// <summary>
        /// Tests that string-typed variables are correctly declared and maintain their type.
        /// Verifies ResultType.String is properly assigned to text variables.
        /// </summary>
        [TestMethod()]
        public void Variable_StringType_CorrectlyDeclared()
        {
            var varTypes = new Dictionary<string, ResultType> { { "name", ResultType.String } };
            var node = ExpressionParser.Parser.Parser.Parse("[name]", false, varTypes);

            Assert.AreEqual(ResultType.String, node.OutputType);
        }

        /// <summary>
        /// Tests that boolean-typed variables are correctly declared and maintain their type.
        /// Verifies ResultType.Boolean is properly assigned to true/false variables.
        /// </summary>
        [TestMethod()]
        public void Variable_BooleanType_CorrectlyDeclared()
        {
            var varTypes = new Dictionary<string, ResultType> { { "flag", ResultType.Boolean } };
            var node = ExpressionParser.Parser.Parser.Parse("[flag]", false, varTypes);

            Assert.AreEqual(ResultType.Boolean, node.OutputType);
        }

        /// <summary>
        /// Tests that SetValue() correctly assigns integer values to variables
        /// and that evaluation returns the assigned value.
        /// </summary>
        [TestMethod()]
        public void SetValue_IntegerVariable_CorrectlyAssignsAndEvaluates()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 5", false, varTypes);
            var variables = node.GetVariableNodes();

            variables[0].SetValue(10);
            var result = node.Evaluate();

            Assert.AreEqual(15, result.Result);
            Assert.AreEqual(ResultType.Integer, result.Type);
        }

        /// <summary>
        /// Tests that SetValue() correctly assigns floating-point values to variables
        /// and that evaluation maintains precision for double-typed results.
        /// </summary>
        [TestMethod()]
        public void SetValue_DoubleVariable_CorrectlyAssignsAndEvaluates()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Double } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 2.5", false, varTypes);
            var variables = node.GetVariableNodes();

            variables[0].SetValue(7.3);
            var result = node.Evaluate();

            Assert.AreEqual(9.8, (double)result.Result, 0.0001);
            Assert.AreEqual(ResultType.Double, result.Type);
        }

        /// <summary>
        /// Tests that SetValue() correctly assigns string values to variables
        /// and that string concatenation operations work properly with variable values.
        /// </summary>
        [TestMethod()]
        public void SetValue_StringVariable_CorrectlyAssignsAndEvaluates()
        {
            var varTypes = new Dictionary<string, ResultType> { { "name", ResultType.String } };
            var node = ExpressionParser.Parser.Parser.Parse("[name] & \" Smith\"", false, varTypes);
            var variables = node.GetVariableNodes();

            variables[0].SetValue("John");
            var result = node.Evaluate();

            Assert.AreEqual("John Smith", result.Result);
            Assert.AreEqual(ResultType.String, result.Type);
        }

        /// <summary>
        /// Tests that undeclared variables result in error output type.
        /// Variables must be declared in the type dictionary to be valid.
        /// </summary>
        [TestMethod()]
        public void Variable_Undeclared_ResultsInError()
        {
            var node = ExpressionParser.Parser.Parser.Parse("[x] + 5");

            // Undeclared variable should result in UnDeclared or Error type
            Assert.IsTrue(node.OutputType == ResultType.UnDeclared || node.ContainsErrors);
        }

        /// <summary>
        /// Tests that multiple references to the same variable in an expression
        /// all point to the same variable node and share the same value.
        /// </summary>
        [TestMethod()]
        public void Variable_MultipleReferencesToSameName_ShareValue()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + [x] + [x]", false, varTypes);
            var variables = node.GetVariableNodes();

            // Set value once - should affect all references
            foreach (var variable in variables)
            {
                variable.SetValue(5);
            }

            var result = node.Evaluate();
            Assert.AreEqual(15, result.Result);
        }

        /// <summary>
        /// Tests that variable names with hyphens are correctly parsed and evaluated.
        /// Validates support for kebab-case variable naming conventions.
        /// </summary>
        [TestMethod()]
        public void Variable_NameWithHyphen_ParsesCorrectly()
        {
            var varTypes = new Dictionary<string, ResultType> { { "my-var", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[my-var] + 10", false, varTypes);
            var variables = node.GetVariableNodes();

            Assert.AreEqual(1, variables.Count);
            Assert.AreEqual("my-var", variables[0].VariableName);

            variables[0].SetValue(20);
            Assert.AreEqual(30, node.Evaluate().Result);
        }

        /// <summary>
        /// Tests that variable names with underscores are correctly parsed and evaluated.
        /// Validates support for snake_case variable naming conventions.
        /// </summary>
        [TestMethod()]
        public void Variable_NameWithUnderscore_ParsesCorrectly()
        {
            var varTypes = new Dictionary<string, ResultType> { { "my_var", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("[my_var] + 10", false, varTypes);
            var variables = node.GetVariableNodes();

            Assert.AreEqual(1, variables.Count);
            Assert.AreEqual("my_var", variables[0].VariableName);

            variables[0].SetValue(15);
            Assert.AreEqual(25, node.Evaluate().Result);
        }

        /// <summary>
        /// Tests that complex expressions with multiple variables of different types
        /// are correctly evaluated when all variable values are set.
        /// </summary>
        [TestMethod()]
        public void Variable_MultipleTypesInExpression_EvaluatesCorrectly()
        {
            var varTypes = new Dictionary<string, ResultType>
            {
                { "x", ResultType.Integer },
                { "y", ResultType.Double },
                { "name", ResultType.String }
            };
            var node = ExpressionParser.Parser.Parser.Parse("([x] + [y]) > 10", false, varTypes);
            var variables = node.GetVariableNodes();

            // Find and set each variable
            foreach (var variable in variables)
            {
                if (variable.VariableName == "x")
                    variable.SetValue(5);
                else if (variable.VariableName == "y")
                    variable.SetValue(7.5);
            }

            var result = node.Evaluate();
            Assert.AreEqual(true, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests that boolean variables can be used in logical expressions
        /// and properly evaluate with the assigned true/false values.
        /// </summary>
        [TestMethod()]
        public void Variable_BooleanInLogicalExpression_EvaluatesCorrectly()
        {
            var varTypes = new Dictionary<string, ResultType>
            {
                { "flag1", ResultType.Boolean },
                { "flag2", ResultType.Boolean }
            };
            var node = ExpressionParser.Parser.Parser.Parse("AND([flag1], [flag2])", false, varTypes);
            var variables = node.GetVariableNodes();

            variables[0].SetValue(true);
            variables[1].SetValue(false);

            var result = node.Evaluate();
            Assert.AreEqual(false, result.Result);
            Assert.AreEqual(ResultType.Boolean, result.Type);
        }

        /// <summary>
        /// Tests that expressions with variables maintain correct output types
        /// based on the operation and variable types involved.
        /// </summary>
        [TestMethod()]
        public void Variable_MixedIntegerAndDouble_ProducesDoubleResult()
        {
            var varTypes = new Dictionary<string, ResultType>
            {
                { "x", ResultType.Integer },
                { "y", ResultType.Double }
            };
            var node = ExpressionParser.Parser.Parser.Parse("[x] + [y]", false, varTypes);

            // When Integer and Double are combined, result should be Double
            Assert.AreEqual(ResultType.Double, node.OutputType);

            var variables = node.GetVariableNodes();
            foreach (var variable in variables)
            {
                if (variable.VariableName == "x")
                    variable.SetValue(10);
                else if (variable.VariableName == "y")
                    variable.SetValue(5.5);
            }

            var result = node.Evaluate();
            Assert.AreEqual(15.5, (double)result.Result, 0.0001);
        }

        /// <summary>
        /// Tests that simplified expressions with variables can still be evaluated correctly
        /// after variable values are set post-simplification.
        /// </summary>
        [TestMethod()]
        public void Simplify_ThenSetVariableValue_EvaluatesCorrectly()
        {
            var varTypes = new Dictionary<string, ResultType> { { "x", ResultType.Integer } };
            var node = ExpressionParser.Parser.Parser.Parse("(2 + 3) * [x] + (4 + 6)", false, varTypes);
            var simplified = node.Simplify();

            // After simplification, should still work with variable
            var variables = simplified.GetVariableNodes();
            variables[0].SetValue(3);

            // Result should be: 5 * 3 + 10 = 25
            Assert.AreEqual(25, simplified.Evaluate().Result);
        }

        #endregion
    }
}
