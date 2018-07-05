using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CalculatorTest
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void AddTest()
        {
            CalculatorApp.Calculator calc = new CalculatorApp.Calculator();
            Assert.AreEqual(3, calc.Add(1, 2));
        }

        [TestMethod]
        public void SubTest()
        {
            CalculatorApp.Calculator calc = new CalculatorApp.Calculator();
            Assert.AreEqual(0, calc.Sub(2, 2));
        }

        [TestMethod]
        public void MultiplyTest()
        {
            CalculatorApp.Calculator calc = new CalculatorApp.Calculator();
            Assert.AreEqual(2, calc.Multiply(1, 2));
        }

        [TestMethod]
        public void DivideTest()
        {
            CalculatorApp.Calculator calc = new CalculatorApp.Calculator();
            Assert.AreEqual(1, calc.Divide(2, 2));
        }
    }
}
