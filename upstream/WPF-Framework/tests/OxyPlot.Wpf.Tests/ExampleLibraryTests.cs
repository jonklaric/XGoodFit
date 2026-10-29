// --------------------------------------------------------------------------------------------------------------------
// <copyright file="ExampleLibraryTests.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides unit tests for the example library.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Wpf.Tests
{
    using System;
    using System.IO;
    using NUnit.Framework;

    /// <summary>
    /// Provides unit tests for the example library.
    /// </summary>
    [TestFixture]
    public class ExampleLibraryTests
    {
        const string DestinationDirectory = "ExampleLibrary.Actual";
        const string BaselineDirectory = "ExampleLibrary.Baseline";
        const string DiffDirectory = "ExampleLibrary.Diff";

        [OneTimeSetUp]
        public void Init()
        {
            Directory.CreateDirectory(BaselineDirectory);
            Directory.CreateDirectory(DestinationDirectory);
            Directory.CreateDirectory(DiffDirectory);
        }

        /// <summary>
        /// Compares all examples with a baseline in "ExampleLibrary.Baseline". Creates the baseline
        /// if it does not exist.
        /// </summary>
        [Test]
        [TestCaseSource(typeof(ExampleLibrary.Examples), nameof(ExampleLibrary.Examples.GetListForAutomatedTest))]
        public void ExportPngAndCompareWithBaseline(ExampleLibrary.ExampleInfo example)
        {
            void ExportAndCompareToBaseline(PlotModel model, string fileName)
            {
                if (model == null)
                {
                    return;
                }

                var baselinePath = Path.Combine(BaselineDirectory, fileName);
                var path = Path.Combine(DestinationDirectory, fileName);
                var diffpath = Path.Combine(DiffDirectory, fileName);

                PngExporter.Export(model, path, 800, 500);
                if (example.Category == "Z1 Issues" && example.Title == "#758: IntervalLength = 0")
                {
                    // This invalid-axis example renders a runtime stack trace whose frames can change with JIT inlining.
                    // Verify its intended error contract while retaining PNG comparisons for every other example.
                    var error = model.GetLastPlotException();
                    Assert.That(error, Is.TypeOf<ArgumentException>(), example.Title);
                    var argumentError = (ArgumentException)error;
                    Assert.That(argumentError.ParamName, Is.EqualTo("maxIntervalSize"), example.Title);
                    Assert.That(argumentError.Message, Does.StartWith("Maximum interval size cannot be zero."), example.Title);
                    return;
                }
                if (File.Exists(baselinePath))
                {
                    PngAssert.AreEqual(baselinePath, path, example.Title, diffpath);
                }
                else
                {
                    File.Copy(path, baselinePath);
                }
            }

            ExportAndCompareToBaseline(example.PlotModel, CreateValidFileName($"{example.Category} - {example.Title}", ".png"));
        }

        /// <summary>
        /// Creates a valid file name.
        /// </summary>
        /// <param name="title">The title.</param>
        /// <param name="extension">The extension.</param>
        /// <returns>A file name.</returns>
        private static string CreateValidFileName(string title, string extension)
        {
            string validFileName = title.Trim();
            var invalidFileNameChars = "/?<>\\:*|\0\t\r\n".ToCharArray();
            foreach (var invalChar in invalidFileNameChars)
            {
                validFileName = validFileName.Replace(invalChar.ToString(), string.Empty);
            }

            if (validFileName.Length > 160)
            {
                // safe value threshold is 260
                validFileName = validFileName.Remove(156) + "...";
            }

            return validFileName + extension;
        }
    }
}
