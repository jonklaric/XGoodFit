using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Time Series Data (examples/1-time-series-data) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourTimeSeriesDataTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("abom-download-example")]
        [DataRow("chmn-download-example")]
        [DataRow("ghcn-download-example")]
        [DataRow("hec-dss-import-example")]
        [DataRow("manual-entry-example")]
        [DataRow("usgs-download-example")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
