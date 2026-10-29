// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SvgAssert.cs" company="OxyPlot">
//   Copyright (c) 2014 OxyPlot contributors
// </copyright>
// <summary>
//   Provides methods to assert against the svg schema.
// </summary>
// --------------------------------------------------------------------------------------------------------------------

namespace OxyPlot.Tests
{
    using System.IO;
    using System.Xml.Linq;
    using NUnit.Framework;

    /// <summary>
    /// Provides methods to assert against the svg schema.
    /// </summary>
    public static class SvgAssert
    {
        /// <summary>
        /// Asserts that the specified file is a valid svg file by parsing its XML structure.
        /// </summary>
        /// <param name="path">The path to the svg file.</param>
        public static void IsValidFile(string path)
        {
            Assert.IsTrue(File.Exists(path), $"SVG file does not exist: {path}");
            var content = File.ReadAllText(path);
            Assert.IsNotEmpty(content, $"SVG file is empty: {path}");

            // Parse as valid XML
            XDocument doc = null;
            Assert.DoesNotThrow(() => doc = XDocument.Parse(content), $"SVG file is not valid XML: {path}");
            Assert.IsNotNull(doc?.Root, $"SVG file has no root element: {path}");
            Assert.IsTrue(doc.Root.Name.LocalName == "svg", $"SVG root element is '{doc.Root.Name.LocalName}', expected 'svg': {path}");
        }

        /// <summary>
        /// Asserts that the specified string is a valid svg document (including ?xml and !DOCTYPE).
        /// </summary>
        /// <param name="content">The svg document.</param>
        public static void IsValidDocument(string content)
        {
            Assert.IsNotNull(content, "SVG content is null.");
            Assert.IsNotEmpty(content, "SVG content is empty.");
            Assert.IsTrue(content.Contains("<svg"), "Content does not contain an <svg element.");
        }

        /// <summary>
        /// Asserts that the specified string is a valid svg element (<svg>...</svg>).
        /// </summary>
        /// <param name="content">The svg element.</param>
        public static void IsValidElement(string content)
        {
            Assert.IsNotNull(content, "SVG content is null.");
            Assert.IsNotEmpty(content, "SVG content is empty.");
            Assert.IsTrue(content.Contains("<svg"), "Content does not contain an <svg element.");
            Assert.IsTrue(content.Contains("</svg>"), "Content does not contain a closing </svg> tag.");
        }
    }
}
