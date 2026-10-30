using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Bulletin 17C Analysis (examples/4-univariate-distribution-analysis/2-bulletin-17C-analysis) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourB17CTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("blakely-mountain-dam-b17c")]
        [DataRow("blakely-mountain-dam-b17c-metric")]
        [DataRow("bulletin-17c-examples")]
        [DataRow("sinnemahoning-move3-b17c")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
