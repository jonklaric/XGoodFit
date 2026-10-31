using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using RMC.BestFit.TestCommon;

namespace RMC.BestFit.App.Tests.CoreInfrastructure
{
    /// <summary>
    /// Protects the captured public and protected surface of the App assembly.
    /// </summary>
    /// <remarks>
    /// XGoodFit port of upstream's CoreInfrastructure/PublicApiCompatibilityTests.cs. The port's API can't equal
    /// upstream's baseline (PublicApiBaseline.txt): its views are Avalonia controls, its dependency properties Avalonia
    /// styled properties, and its classes implement Avalonia's interfaces where upstream's implemented WPF's. So the
    /// port guards its API twice, with the same snapshot (TestCommon's PublicApiContractSnapshot):
    /// <list type="bullet">
    /// <item><see cref="PublicApi_MatchesCapturedBaseline"/>: the port's API is its own baseline,
    /// PortPublicApiBaseline.txt, so that any change to it is noticed, as upstream's test notices changes to its
    /// own.</item>
    /// <item><see cref="PublicApi_KeepsUpstreamsApi_ExceptTheReviewedDifferences"/>: every line of upstream's baseline
    /// has its counterpart in the same type of the port's API, after the documented type maps and rewrites of
    /// PublicApiDifferences.txt, unless that file lists it as a reviewed difference, with its reason. An entry
    /// that nothing needs any more fails the test too, so the file stays the list of what changes for RMC-BestFit's
    /// API users.</item>
    /// </list>
    /// Upstream's baseline is embedded from upstream/ under its own name.
    /// </remarks>
    [TestClass]
    public class PublicApiCompatibilityTests
    {
        /// <summary>Upstream's baseline, embedded from upstream/RMC-BestFit/src/RMC.BestFit.App.Tests.</summary>
        private const string UpstreamBaseline = "RMC.BestFit.App.Tests.CoreInfrastructure.PublicApiBaseline.txt";

        /// <summary>XGoodFit port: the port's own baseline.</summary>
        private const string PortBaseline = "RMC.BestFit.App.Tests.CoreInfrastructure.PortPublicApiBaseline.txt";

        /// <summary>XGoodFit port: the reviewed type maps, rewrites and differences from upstream's API.</summary>
        private const string Differences = "RMC.BestFit.App.Tests.CoreInfrastructure.PublicApiDifferences.txt";

        /// <summary>
        /// Verifies that the App assembly matches its explicitly approved signature baseline.
        /// </summary>
        /// <remarks>XGoodFit port: the port's baseline, PortPublicApiBaseline.txt.</remarks>
        [TestMethod]
        public void PublicApi_MatchesCapturedBaseline()
        {
            string expected = ReadResource(PortBaseline);
            string actual = PublicApiContractSnapshot.Create(typeof(RMC_BestFit.TransformTypeItem).Assembly);

            if (!string.Equals(expected, actual, StringComparison.Ordinal))
            {
                Assert.Fail("The RMC.BestFit.App public/protected API changed. Preserve the baseline unless the change has explicit compatibility approval "
                    + "(then update CoreInfrastructure/PortPublicApiBaseline.txt, and PublicApiDifferences.txt where upstream's API is concerned)."
                    + Environment.NewLine + Difference(expected, actual));
            }
        }

        /// <summary>
        /// XGoodFit addition: verifies that the port keeps upstream's App API, but for the reviewed differences.
        /// </summary>
        [TestMethod]
        public void PublicApi_KeepsUpstreamsApi_ExceptTheReviewedDifferences()
        {
            Dictionary<string, List<string>> upstream = Parse(ReadResource(UpstreamBaseline));
            Dictionary<string, List<string>> port = Parse(PublicApiContractSnapshot.Create(typeof(RMC_BestFit.TransformTypeItem).Assembly));
            var reviewed = new ReviewedDifferences(ReadResource(Differences));
            Assert.IsTrue(upstream.Count > 100, "Upstream's baseline should be read.");

            var failures = new List<string>();
            foreach (KeyValuePair<string, List<string>> type in upstream)
            {
                string typeLine = reviewed.Map(type.Value[0]);
                if (!port.TryGetValue(type.Key, out List<string> portLines))
                {
                    if (!reviewed.IsDifference(type.Key, typeLine)) failures.Add(type.Key + ": the type is missing (" + typeLine + ")");
                    continue;
                }

                var portSet = new HashSet<string>(portLines, StringComparer.Ordinal);
                foreach (string line in type.Value)
                {
                    string mapped = reviewed.Map(line);
                    if (portSet.Contains(mapped)
                        || portSet.Contains(reviewed.Rewrite(type.Key, mapped) ?? string.Empty)
                        || reviewed.IsStyledProperty(mapped, portSet)
                        || reviewed.IsDifference(type.Key, mapped))
                    {
                        continue;
                    }

                    failures.Add(type.Key + ": " + mapped);
                }
            }

            Assert.AreEqual(0, failures.Count, "Upstream's App API has members without a counterpart in the port, and no reviewed difference "
                + "(CoreInfrastructure/PublicApiDifferences.txt):" + Environment.NewLine + string.Join(Environment.NewLine, failures));
            Assert.AreEqual(0, reviewed.Unused.Count, "Reviewed entries that no upstream member needs any more:"
                + Environment.NewLine + string.Join(Environment.NewLine, reviewed.Unused));
        }

        /// <summary>XGoodFit port: reads an embedded text resource, with LF line ends.</summary>
        /// <param name="name">The resource's logical name.</param>
        /// <returns>The text.</returns>
        private static string ReadResource(string name)
        {
            Assembly testAssembly = typeof(PublicApiCompatibilityTests).Assembly;
            using Stream stream = testAssembly.GetManifestResourceStream(name);
            Assert.IsNotNull(stream, "The embedded " + name + " is missing.");
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd().Replace("\r\n", "\n", StringComparison.Ordinal);
        }

        /// <summary>
        /// XGoodFit port: a snapshot's lines by type (a type's own line first, then its members, unindented).
        /// </summary>
        /// <param name="snapshot">A PublicApiContractSnapshot.</param>
        /// <returns>The lines of each type, by the type's name.</returns>
        private static Dictionary<string, List<string>> Parse(string snapshot)
        {
            var types = new Dictionary<string, List<string>>(StringComparer.Ordinal);
            List<string> current = null;
            foreach (string line in snapshot.Split('\n').Where(line => line.Length > 0))
            {
                if (line.StartsWith("TYPE ", StringComparison.Ordinal))
                {
                    current = new List<string> { line };
                    types.Add(line.Split(' ')[2], current);
                }
                else
                {
                    Assert.IsNotNull(current, "A member line comes before any type: " + line);
                    current.Add(line.Trim());
                }
            }

            return types;
        }

        /// <summary>XGoodFit port: the lines one snapshot has and the other hasn't, for a failure's message.</summary>
        /// <param name="expected">The baseline.</param>
        /// <param name="actual">The current snapshot.</param>
        /// <returns>The differing lines, at most 60 of each.</returns>
        private static string Difference(string expected, string actual)
        {
            var expectedLines = new HashSet<string>(expected.Split('\n'), StringComparer.Ordinal);
            var actualLines = new HashSet<string>(actual.Split('\n'), StringComparer.Ordinal);
            IEnumerable<string> removed = expected.Split('\n').Where(line => !actualLines.Contains(line)).Take(60).Select(line => "- " + line);
            IEnumerable<string> added = actual.Split('\n').Where(line => !expectedLines.Contains(line)).Take(60).Select(line => "+ " + line);
            string lines = string.Join(Environment.NewLine, removed.Concat(added));
            return lines.Length > 0 ? lines : "(The same lines, in another order.)";
        }

        /// <summary>
        /// XGoodFit port: the reviewed type maps, rewrites and differences from upstream's API (PublicApiDifferences.txt),
        /// and which of them were used.
        /// </summary>
        private sealed class ReviewedDifferences
        {
            /// <summary>A dependency property field, after the maps.</summary>
            private static readonly Regex DependencyPropertyField = new Regex(@"^FIELD public System\.Windows\.DependencyProperty (\w+)Property$", RegexOptions.CultureInvariant);

            private readonly List<(string Entry, Regex Pattern, string Replacement)> _maps = new List<(string, Regex, string)>();
            private readonly List<(string Entry, string Type, string Upstream, string Port)> _rewrites = new List<(string, string, string, string)>();
            private readonly List<(string Entry, string Type, string Upstream)> _differences = new List<(string, string, string)>();
            private readonly string _dependencyPropertyRule;
            private readonly HashSet<string> _used = new HashSet<string>(StringComparer.Ordinal);

            /// <summary>Reads the reviewed entries.</summary>
            /// <param name="text">PublicApiDifferences.txt.</param>
            public ReviewedDifferences(string text)
            {
                foreach (string entry in text.Split('\n').Select(line => line.Trim()).Where(line => line.Length > 0 && !line.StartsWith('#')))
                {
                    string[] fields = entry.Split(" | ");
                    Assert.IsTrue(fields[^1].Trim().Length > 0, "A reviewed entry needs its reason: " + entry);
                    switch (fields[0])
                    {
                        case "MAP" when fields.Length == 4:
                            // A whole type name, not part of a longer one.
                            _maps.Add((entry, new Regex(@"(?<![\w.+`])" + Regex.Escape(fields[1]) + @"(?![\w.+`])", RegexOptions.CultureInvariant), fields[2]));
                            break;
                        case "LINE" when fields.Length == 5:
                            _rewrites.Add((entry, fields[1], fields[2], fields[3]));
                            break;
                        case "RULE" when fields.Length == 3 && fields[1] == "DependencyProperty":
                            _dependencyPropertyRule = entry;
                            break;
                        case "DIFF" when fields.Length == 4:
                            _differences.Add((entry, fields[1], fields[2]));
                            break;
                        default:
                            Assert.Fail("An entry of an unknown form: " + entry);
                            break;
                    }
                }
            }

            /// <summary>The entries nothing used.</summary>
            public List<string> Unused => _maps.Select(map => map.Entry)
                .Concat(_rewrites.Select(rewrite => rewrite.Entry))
                .Concat(_differences.Select(difference => difference.Entry))
                .Concat(_dependencyPropertyRule == null ? Enumerable.Empty<string>() : new[] { _dependencyPropertyRule })
                .Where(entry => !_used.Contains(entry)).ToList();

            /// <summary>An upstream line with the type maps applied.</summary>
            /// <param name="line">The line.</param>
            /// <returns>The line in the port's types.</returns>
            public string Map(string line)
            {
                foreach ((string entry, Regex pattern, string replacement) in _maps)
                {
                    if (!pattern.IsMatch(line)) continue;
                    line = pattern.Replace(line, replacement);
                    _used.Add(entry);
                }

                return line;
            }

            /// <summary>The port's form of a member that has one (LINE), or null.</summary>
            /// <param name="type">The member's type.</param>
            /// <param name="line">The member's line, mapped.</param>
            /// <returns>The port's line, or null.</returns>
            public string Rewrite(string type, string line)
            {
                foreach ((string entry, string forType, string upstream, string port) in _rewrites)
                {
                    if ((forType == "*" || forType == type) && upstream == line)
                    {
                        _used.Add(entry);
                        return port;
                    }
                }

                return null;
            }

            /// <summary>
            /// Whether a dependency property field's counterpart is the port's: the styled property of the same name, of the
            /// type of the class's property (RULE DependencyProperty).
            /// </summary>
            /// <param name="line">The upstream member's line, mapped.</param>
            /// <param name="port">The lines of the port's type.</param>
            /// <returns>Whether the port has it.</returns>
            public bool IsStyledProperty(string line, HashSet<string> port)
            {
                Match field = DependencyPropertyField.Match(line);
                if (!field.Success || _dependencyPropertyRule == null) return false;
                string prefix = "PROPERTY ", name = " " + field.Groups[1].Value + " {";
                string property = port.SingleOrDefault(member => member.StartsWith(prefix, StringComparison.Ordinal) && member.Contains(name, StringComparison.Ordinal)
                    && member.IndexOf(name, StringComparison.Ordinal) == member.IndexOf(' ', prefix.Length));
                if (property == null) return false;
                string propertyType = property.Substring(prefix.Length, property.IndexOf(' ', prefix.Length) - prefix.Length);
                if (!port.Contains("FIELD public Avalonia.StyledProperty<" + propertyType + "> " + field.Groups[1].Value + "Property")) return false;
                _used.Add(_dependencyPropertyRule);
                return true;
            }

            /// <summary>Whether a line is a reviewed difference (DIFF).</summary>
            /// <param name="type">The member's type.</param>
            /// <param name="line">The member's line, mapped.</param>
            /// <returns>Whether it is.</returns>
            public bool IsDifference(string type, string line)
            {
                foreach ((string entry, string forType, string upstream) in _differences)
                {
                    if ((forType == "*" || forType == type) && upstream == line)
                    {
                        _used.Add(entry);
                        return true;
                    }
                }

                return false;
            }
        }
    }
}
