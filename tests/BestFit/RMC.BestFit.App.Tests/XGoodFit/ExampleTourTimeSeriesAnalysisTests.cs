using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Time Series Analysis (examples/7-time-series-analysis) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourTimeSeriesAnalysisTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("classic-time-series-examples")]
        [DataRow("synthetic-time-series-examples")]
        [DataRow("time-series-regression-example")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
