using DSSUnitTests;
using Hec.Dss;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DotNetTests
{
   [TestClass]
   public class Conversions
   {

      [TestMethod]
      public void ConvertDSS6ToDSS7()
      {
         var fn6 = TestUtility.GetCopyForTesting("sample6.dss");
         var fn7 = TestUtility.GetSimpleTempFileName(".dss");
         int status = DssWriter.ConvertFromDSS6ToDSS7(fn6, fn7);
         Console.WriteLine(  "Status = "+status);
        }
   }
}
