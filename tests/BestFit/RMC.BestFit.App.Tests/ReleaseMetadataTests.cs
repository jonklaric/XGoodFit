using Microsoft.VisualStudio.TestTools.UnitTesting;
using SoftwareUpdate;
using System;
using System.Diagnostics;
using System.Reflection;
using BestFitApplication = RMC_BestFit.App;

namespace RMC.BestFit.App.Tests
{
    /// <summary>
    /// Verifies the application release metadata and updater configuration remain synchronized.
    /// </summary>
    /// <remarks>
    /// XGoodFit port: XGoodFit releases under its own version (Version in Directory.Build.props, which
    /// the tests are built with too) and updates from its own releases (XGoodFitUpdateRepository), through
    /// Velopack, which checks packages itself. Projects still record upstream's version, 2.0.1
    /// (App.ProductVersion), which RMC-BestFit 2.0.1 writes, so that files stay compatible both ways. So
    /// the product metadata test is upstream's, and the other two check that the update options and the
    /// assembly's version attributes agree with XGoodFit's version and repository, as upstream's check
    /// RMC-BestFit's.
    /// </remarks>
    [TestClass]
    public class ReleaseMetadataTests
    {
        /// <summary>
        /// Verifies the shell displays the v2.0.1 product version and release month.
        /// </summary>
        [TestMethod]
        public void ProductMetadata_UsesFinalReleaseValues()
        {
            Assert.AreEqual("2.0.1", BestFitApplication.ProductVersion);
            Assert.AreEqual("October 2026", BestFitApplication.ProductVersionDate);
        }

        /// <summary>
        /// Verifies update checks use the assembly informational version and require checksums.
        /// </summary>
        [TestMethod]
        public void UpdateOptions_UseFinalReleaseAndRequireChecksum()
        {
            var options = BestFitApplication.CreateUpdateOptions();
            SemanticVersion version = SemanticVersion.Parse(XGoodFitVersion);
            string[] repository = new Uri(global::XGoodFit.Platform.ProductInfo.UpdateRepository).AbsolutePath.Trim('/').Split('/');

            string informationalVersion = typeof(BestFitApplication).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            Assert.AreEqual(SemanticVersion.Parse(informationalVersion).ToString(), options.CurrentVersion.ToString());
            Assert.IsTrue(options.RequireSha256Checksum);
            Assert.AreEqual(repository[0], options.GitHubOwner);
            Assert.AreEqual(repository[1], options.GitHubRepo);
            Assert.AreEqual("https://github.com/jonklaric/XGoodFit", global::XGoodFit.Platform.ProductInfo.UpdateRepository);
            Assert.AreEqual(version.IsPreRelease, options.IncludePreReleases);
        }

        /// <summary>
        /// Verifies all application assembly version attributes match the release convention.
        /// </summary>
        [TestMethod]
        public void AppAssemblyMetadata_UsesFinalReleaseValues()
        {
            Assembly assembly = typeof(BestFitApplication).Assembly;
            string informationalVersion = assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion;
            string fileVersion = FileVersionInfo.GetVersionInfo(assembly.Location).FileVersion!;
            SemanticVersion version = SemanticVersion.Parse(XGoodFitVersion);
            var numericVersion = new Version(version.Major, version.Minor, version.Patch, 0);

            Assert.AreEqual(XGoodFitVersion, informationalVersion.Split('+')[0]);
            Assert.AreEqual(numericVersion, assembly.GetName().Version);
            Assert.AreEqual(numericVersion.ToString(), fileVersion);
            Assert.AreEqual("XGoodFit", assembly.GetCustomAttribute<AssemblyTitleAttribute>()!.Title, "the About box and window title show the assembly title");
        }

        /// <summary>
        /// XGoodFit addition: the release version, Version in Directory.Build.props, which this test
        /// assembly is built with too (without the build metadata the informational version adds).
        /// </summary>
        private static string XGoodFitVersion =>
            typeof(ReleaseMetadataTests).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()!.InformationalVersion.Split('+')[0];

        /// <summary>
        /// XGoodFit addition: the release version is a three-part SemVer version, as upstream's 2.0.1 is.
        /// </summary>
        [TestMethod]
        public void ReleaseVersion_IsSemanticVersion()
        {
            Assert.IsTrue(SemanticVersion.TryParse(XGoodFitVersion, out _), XGoodFitVersion);
        }
    }
}
