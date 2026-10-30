using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Point Process, Mixture and Composite Analysis (examples/4-univariate-distribution-analysis/3-5) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourOtherUnivariateTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("point-process-examples")]
        [DataRow("mixture-distribution-examples")]
        [DataRow("mixed-population-examples")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
