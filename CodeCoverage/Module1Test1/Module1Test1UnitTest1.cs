using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module1;

namespace Module1Test1
{
    [TestClass]
    public class Module1Test1UnitTest1
    {
        [TestMethod]
        public void Module1Test1UnitTest1_TestMethod1()
        {
            var module1Class1 = new Module1Class1();
            Assert.AreEqual(1, module1Class1.Module1Class1Method1());
        }
    }
}
