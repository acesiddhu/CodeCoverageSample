using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Module1;

namespace Module1Test1
{
    [TestClass]
    public class Module1Test2UnitTest1
    {
        [TestMethod]
        public void Module1Test2UnitTest1_TestMethod1()
        {
            var module1Class2 = new Module1Class2();
            Assert.AreEqual(1, module1Class2.Module1Class2Method1(4));
        }
    }
}
