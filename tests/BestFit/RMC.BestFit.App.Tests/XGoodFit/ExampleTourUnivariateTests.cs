using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace RMC.BestFit.App.Tests.XGoodFit
{
    /// <summary>
    /// XGoodFit addition: the example tour (<see cref="ExampleTour"/>) of the Univariate Distribution Analysis (examples/4-univariate-distribution-analysis/1-univariate-analysis) examples: every element of
    /// each project, through the main window, changes nothing.
    /// </summary>
    [TestClass]
    [DoNotParallelize]
    public sealed class ExampleTourUnivariateTests
    {
        [TestMethod]
        [TestCategory(ExampleTour.Category)]
        [DataRow("arr-flike-examples")]
        [DataRow("bulletin-17c-bayesian-examples")]
        [DataRow("nsffa-brays-bayou-texas")]
        [DataRow("nsffa-oc-fisher-dam")]
        [DataRow("nsffa-synthetic-data")]
        [DataRow("sinnemahoning-move3-bayesian")]
        [DataRow("viglione-et-al-2013")]
        public void EveryElement_OpensItsDocumentAndProperties_AndChangesNothing(string example) =>
            HeadlessSession.Run(() => { ExampleTour.Run(example); });
    }
}
