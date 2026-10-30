using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Input Data (examples/2-input-data) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourInputDataTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("ghcn-peaks-over-threshold-example")]
        [DataRow("usgs-block-max-example")]
        [DataRow("usgs-peak-download-example")]
        [DataRow("usgs-peaks-over-threshold-example")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
