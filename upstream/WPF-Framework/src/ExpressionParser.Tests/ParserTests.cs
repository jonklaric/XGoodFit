using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ExpressionParser.Tests
{
    /// <summary>
    /// Contains unit tests that validate expression parsing, evaluation,
    /// and functional correctness of the ExpressionParser library.
    /// </summary>
    [TestClass()]
    public class ParserTests
    {
        /// <summary>
        /// Tests addition and subtraction expressions, including invalid input handling.
        /// </summary>
        [TestMethod()]
        public void AddSubtractTests()
        {
            Assert.AreEqual(30, Parser.Parser.Parse("10 + 20").Evaluate().Result);
            Assert.AreEqual(-10, Parser.Parser.Parse("10 - 20").Evaluate().Result);
            Assert.AreEqual(90, Parser.Parser.Parse("10 + 20 - 40 + 100").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("1 + 1 1").Evaluate().Type);
        }

        /// <summary>
        /// Tests behavior of parenthesis in expressions including malformed and nested forms.
        /// </summary>
        [TestMethod()]
        public void ParenthesisTests()
        {
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("2(3+5)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("(3+5)2").Evaluate().Type);
            Assert.AreEqual(16, Parser.Parser.Parse("2*(3+5)").Evaluate().Result);
            Assert.AreEqual(3, Parser.Parser.Parse("1-(1-3+5)*(2-1)+5").Evaluate().Result);
        }

        /// <summary>
        /// Tests evaluation of comparison operators and invalid comparisons.
        /// </summary>
        [TestMethod()]
        public void ComparisonTests()
        {
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("3>cat").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse(">5").Evaluate().Type);
            Assert.AreEqual(false, Parser.Parser.Parse("3>5").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("3>=5").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("3>=3").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("3>3").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("3=5").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("3<5").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("3<>5").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("3!=5").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("5<>5").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("5!=5").Evaluate().Result);

        }

        /// <summary>
        /// Tests ROUND, ROUNDUP, and ROUNDDOWN functions across a range of precision values.
        /// </summary>
        [TestMethod()]
        public void RoundingTests()
        {
            Assert.AreEqual(30, Parser.Parser.Parse("=ROUND(31.1543,-1)").Evaluate().Result);
            Assert.AreEqual(31, Parser.Parser.Parse("=ROUND(31.1543,0)").Evaluate().Result);
            Assert.AreEqual(31.2d, Parser.Parser.Parse("=ROUND(31.1543,1)").Evaluate().Result);

            Assert.AreEqual(40, Parser.Parser.Parse("=ROUNDUP(31.1231,-1)").Evaluate().Result);
            Assert.AreEqual(32, Parser.Parser.Parse("=ROUNDUP(31.1231,0)").Evaluate().Result);
            Assert.AreEqual(31.2d, Parser.Parser.Parse("=ROUNDUP(31.1231,1)").Evaluate().Result);

            Assert.AreEqual(30, Parser.Parser.Parse("=ROUNDDOWN(31.1231,-1)").Evaluate().Result);
            Assert.AreEqual(31, Parser.Parser.Parse("=ROUNDDOWN(31.1231,0)").Evaluate().Result);
            Assert.AreEqual(31.1d, Parser.Parser.Parse("=ROUNDDOWN(31.1231,1)").Evaluate().Result);
        }

        /// <summary>
        /// Tests random number functions including deterministic seed-based results.
        /// </summary>
        [TestMethod()]
        public void RandomTests()
        {
            Assert.AreEqual(ResultType.Double, Parser.Parser.Parse("RAND()").Evaluate().Type);
            Assert.AreEqual(new Random(1).NextDouble(), Parser.Parser.Parse("RAND(1)").Evaluate().Result);
            Assert.AreEqual(ResultType.Integer, Parser.Parser.Parse("RANDINT()").Evaluate().Type);
            Assert.AreEqual(new Random(1).Next(), Parser.Parser.Parse("RANDINT(1)").Evaluate().Result);
            //
            Assert.AreEqual(ResultType.Double, Parser.Parser.Parse("RANDBETWEEN(2.3,14)").Evaluate().Type);
            Assert.AreEqual(-4 + (96 - (-4)) * new Random(1).NextDouble(), Parser.Parser.Parse("RANDBETWEEN(-4,96,1)").Evaluate().Result);
            Assert.AreEqual(ResultType.Integer, Parser.Parser.Parse("RANDINTBETWEEN(1,5)").Evaluate().Type);
            Assert.AreEqual(new Random(1).Next(4, 18), Parser.Parser.Parse("RANDINTBETWEEN(4,18,1)").Evaluate().Result);
        }

        /// <summary>
        /// Tests logical functions AND and OR, including error handling for invalid inputs.
        /// </summary>
        [TestMethod()]
        public void AndOrTests()
        {
            Assert.AreEqual(false, Parser.Parser.Parse("AND(3>5,1<5)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("OR(3>5,1<5)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("AND(3<5,1<5,1-1=0,3^2=9,3/3=1)").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("OR(3>5,1<5,4-2)").Evaluate().Type);
        }

        /// <summary>
        /// Tests the conditional IF function, including various conditions and error handling.
        /// </summary>
        [TestMethod()]
        public void IFTests()
        {
            Assert.AreEqual(2, Parser.Parser.Parse("IF(3>5,1,2)").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("If(3>5,True,3)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("If(3>5,,3)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("If(3>5,True,)").Evaluate().Type);
            Assert.AreEqual(3, Parser.Parser.Parse("If(3<5,1-(1-3+5)*(2-1)+5,8)").Evaluate().Result);
            Assert.AreEqual(new Random(1).NextDouble(), Parser.Parser.Parse("If(AND(3<5,1<5,1-1=0,3^2=9,3/3=1),RAND(1),4-2)").Evaluate().Result);
            Assert.AreEqual(2, Parser.Parser.Parse("If(AND(3<5, 1<5, 1-1=1, 3^2=9, 3/3=1), RAND(1), 4-2)").Evaluate().Result);
        }

        /// <summary>
        /// Tests the behavior of the INCREMENT function, including error handling for invalid inputs.
        /// </summary>
        [TestMethod()]
        public void IncrementTests()
        {
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("INCREMENT(1, 2, 3)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("INCREMENT()").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("INCREMENT('1', 2)").Evaluate().Type);
            IParserNode parseNode = Parser.Parser.Parse("INCREMENT(1, 2)");
            for (int i = 1; i <= 5; i += 2)
                Assert.AreEqual(i, parseNode.Evaluate().Result);
            //
            parseNode = Parser.Parser.Parse("INCREMENT(5)");
            for (int i = 5; i <= 10; i++)
                Assert.AreEqual(i, parseNode.Evaluate().Result);
        }

        /// <summary>
        /// Tests string manipulation functions including concatenation, substring, and length checks.
        /// </summary>
        [TestMethod()]
        public void StringTests()
        {
            Assert.AreEqual("rabbit goat drizzle.", Parser.Parser.Parse('"' + "rabbit goat drizzle." + '"').Evaluate().Result);
            Assert.AreEqual("rabbit goat drizzle.", Parser.Parser.Parse("'rabbit goat drizzle.'").Evaluate().Result);
            Assert.AreEqual("rabbit " + "\r" + " goat drizzle.", Parser.Parser.Parse("'rabbit ' & " + "\r" + " & ' goat drizzle.'").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("rabbit goat drizzle.").Evaluate().Type);
            Assert.AreEqual(true, Parser.Parser.Parse("CONTAINS('rabbit goat drizzle.','goat')").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("CONTAINS('rabbit goat drizzle.','y')").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("CONTAINS(3*4,12)").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("CONTAINS(,12)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("CONTAINS(,)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("CONTAINS()").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("CONTAINS(,12,'1')").Evaluate().Type);
            Assert.AreEqual(20, Parser.Parser.Parse("LEN('rabbit goat drizzle.')").Evaluate().Result);
            Assert.AreEqual(20, Parser.Parser.Parse("LENGTH('rabbit goat drizzle.')").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEN('rabbit goat drizzle.')=20").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("LEN('rabbit goat drizzle.')>20").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("LEN('rabbit goat drizzle.')<20").Evaluate().Result);
            Assert.AreEqual(2, Parser.Parser.Parse("LEN('rabbit goat drizzle.')-18").Evaluate().Result);
            Assert.AreEqual("rab", Parser.Parser.Parse("LEFT('rabbit goat drizzle.',3)").Evaluate().Result);
            Assert.AreEqual("le.", Parser.Parser.Parse("RIGHT('rabbit goat drizzle.',3)").Evaluate().Result);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("LEFT('rabbit goat drizzle.',3,1)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("LEFT('rabbit goat drizzle.')").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("LEFT()").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("RIGHT('rabbit goat drizzle.',3,1)").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("RIGHT('rabbit goat drizzle.')").Evaluate().Type);
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("RIGHT()").Evaluate().Type);
            Assert.AreEqual("rabbit goat drizzle.", Parser.Parser.Parse("CONCATENATE('rabbit ', goat, ' ', 'drizzle.')").Evaluate().Result);
            Assert.AreEqual("rabbit goat drizzle.", Parser.Parser.Parse("'rabbit ' & goat & ' ' & 'drizzle.'").Evaluate().Result);
            Assert.AreEqual("rabbit goat drizzle.", Parser.Parser.Parse("CONCATENATE('rabbit ' & goat, ' ', 'drizzle.')").Evaluate().Result);
            Assert.AreEqual(3, Parser.Parser.Parse("INSTRING(abcdef,def)").Evaluate().Result);
            Assert.AreEqual(3, Parser.Parser.Parse("IndexOf(abcdef,def)").Evaluate().Result);
            Assert.AreEqual("rabb", Parser.Parser.Parse("SUBSTRING('rabbit goat drizzle.', 0, 4)").Evaluate().Result);
        }

        /// <summary>
        /// Tests variable handling in expressions, including setting and evaluating variable inputs.
        /// </summary>
        [TestMethod()]
        public void VariablesTests()
        {
            var variableTypes = new Dictionary<string, ResultType>() { { "var", ResultType.Integer }, { "var2", ResultType.Integer } };
            IParserNode parseNode = Parser.Parser.Parse("[var]*[var2] + 2 * 8 - ([var] + [var] * 2^([var2]*8-1))", default, variableTypes);
            var varNodes = parseNode.GetVariableNodes();
            // simple way to define values
            int var1;
            int var2 = 2;
            for (int i = 0; i <= 3; i++)
            {
                var1 = i;
                foreach (var @var in varNodes)
                {
                    if (@var.VariableName == "var")
                        @var.SetValue(var1);
                    if (@var.VariableName == "var2")
                        @var.SetValue(var2);
                }
                Assert.AreEqual(var1 * var2 + 2 * 8 - (var1 + var1 * Math.Pow(2d, var2 * 8 - 1)), parseNode.Evaluate().Result);
            }
            // Another way to define values
            string[] varNames = variableTypes.Keys.ToArray();
            int[] varVals = new int[] { 0, 2 };
            var nodeToValueIndices = new int[varNodes.Count];
            for (int i = 0; i < varNodes.Count; i++)
                nodeToValueIndices[i] = Array.IndexOf(varNames, varNodes[i].VariableName);
            for (int i = 0; i <= 3; i++)
            {
                varVals[0] = i;
                for (int j = 0; j < nodeToValueIndices.Length; j++)
                    varNodes[j].SetValue(varVals[nodeToValueIndices[j]]);
                Assert.AreEqual(varVals[0] * varVals[1] + 2 * 8 - (varVals[0] + varVals[0] * Math.Pow(2d, varVals[1] * 8 - 1)), parseNode.Evaluate().Result);
            }
            // Fail due to incorrect input types
            variableTypes = new Dictionary<string, ResultType>() { { "var", ResultType.Integer }, { "var2", ResultType.String } };
            parseNode = Parser.Parser.Parse("[var]*[var2] + 2 * 8 - ([var] + [var] * 2^([var2]*8-1))", default, variableTypes);
            Assert.AreEqual(ResultType.Error, parseNode.Evaluate().Type);

        }

        /// <summary>
        /// Tests type conversion functions including error handling for invalid conversions.
        /// </summary>
        [TestMethod()]
        public void ConverterTests()
        {
            Assert.AreEqual(ResultType.Error, Parser.Parser.Parse("tostring(3)>=3").Evaluate().Type);
            Assert.AreEqual(true, Parser.Parser.Parse("TOINTEGER(3)>=3").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("int(3)>=3").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("cint(3)>=3").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("TOdouble(3)>=3").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("dbl(3)>=3").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("cdbl(3)>=3").Evaluate().Result);
            Assert.AreEqual("True", Parser.Parser.Parse("tostring(3>=3)").Evaluate().Result);
            Assert.AreEqual("True", Parser.Parser.Parse("str(3>=3)").Evaluate().Result);
            Assert.AreEqual("True", Parser.Parser.Parse("cstr(3>=3)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("cbool('True')").Evaluate().Result);
        }

        /// <summary>
        /// Tests complex logical combinations and application-specific parsing behavior.
        /// </summary>
        [TestMethod()]
        public void ConsequenceToolsTests()
        {
            Assert.AreEqual(true, Parser.Parser.Parse("contains(" + '"' + "ASDF" + '"' + ", A)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("Fi & sh=Fish").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("and(rand()<1, rand()>0)").Evaluate().Result);
            Assert.AreEqual(new Random(2245).NextDouble(), Parser.Parser.Parse("rand(2245)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("IF((OR(1=1, 2=2)), True, False)").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("IF((OR(1!=1, 2!=2)), True, False)").Evaluate().Result);
            Assert.AreEqual("True", Parser.Parser.Parse("IF((OR(1+1!=1, 2!=2)), " + '"' + "True" + '"' + ", " + '"' + "False" + '"' + ")").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("If((Or(LEN('Hello') = 1, 2!=2)), True, False)").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("If((Or(2!=2,LEN('Hello')=1)),True,False)").Evaluate().Result);
            Assert.AreEqual("True", Parser.Parser.Parse("If('Hello'=" + '"' + "Hello" + '"' + "," + '"' + "True" + '"' + "," + '"' + "False" + '"' + ")").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("If(And(1=1,2=1),True,False)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("If(And(a=a,b=b),True,False)").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("If(2^3>=(5+5),True,False)").Evaluate().Result);
            Assert.AreEqual(5, Parser.Parser.Parse("LEN('hello')").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("If(2^(6-1)>=(1+5*5),True,False)").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("And(abe=a,b=b)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(a=a,b=b)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(Or(q=q,w=w),a=a)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(And(Or(q=q,w=w),a=a),Or(1=1,5=5))").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(And(Or(q=q,w=w),a=a),Or(1+1=2,5-1=4))").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(And(Or(q=q,w=w),a=a),Or((LEN('Hi'))=2,(5-1)=4))").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(And(Or((LEN('Hi'))=2,(LEN('Hi'))!=2),a=a),Or((LEN('Hi'))=2,(5-1)=4))").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(2^2>=4,1=1)").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("abc=bd").Evaluate().Result);
            Assert.AreEqual(6d, Parser.Parser.Parse("(2^(6-1)-(1+5*5))").Evaluate().Result);
            Assert.AreEqual(64d, Parser.Parser.Parse("((2+1*2)^(6-3))").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("Hello=" + '"' + "Bye" + '"' + "").Evaluate().Result);
            Assert.AreEqual("lo wo", Parser.Parser.Parse("RIGHT(LEFT('hello world',8),5)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEN(RIGHT(hello,2))=2").Evaluate().Result);
            Assert.AreEqual(5, Parser.Parser.Parse("LEN(k1234)").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("And(2!=2,'ac'='ac')").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("And(ac=ac,aBc=ac)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(ac=ac,aBc!=ac)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEN(RIGHT(wrong,2))=LEN(RIGHT(wrong,2))").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEN(hello)=LEN(hello)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("Hello!='Bye'").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("And(abe!=a,b=b)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("If(LEFT(hello,2)='he',True,False)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("If(a!=b,True,False)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("If(And(abe!=a,b=b),True,False)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEFT(abc,1)=LEFT(ad,1)").Evaluate().Result);
            Assert.AreEqual("lo", Parser.Parser.Parse("RIGHT(hello,2)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("'str'=LEFT(string,3)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEFT(string,3)='str'").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("LEFT(string,3)=LEFT(string,3)").Evaluate().Result);
            Assert.AreEqual("ing", Parser.Parser.Parse("RIGHT(string,3)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("ing=RIGHT(string,3)").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("st=st").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("steak!=steel").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("well!='wall'").Evaluate().Result);
            Assert.AreEqual(true, Parser.Parser.Parse("123!=200-10").Evaluate().Result);
            Assert.AreEqual(false, Parser.Parser.Parse("3*5!=3*(2+3)").Evaluate().Result);

        }

    }
}