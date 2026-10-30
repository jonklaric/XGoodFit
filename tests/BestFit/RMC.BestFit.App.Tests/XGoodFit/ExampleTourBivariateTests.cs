using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Bivariate Distribution and Coincident Frequency Analysis (examples/5-bivariate-distribution-analysis) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourBivariateTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("bivariate-distribution-examples")]
        [DataRow("sum-two-normals")]
        [DataRow("waimea-river-stage-frequency")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
