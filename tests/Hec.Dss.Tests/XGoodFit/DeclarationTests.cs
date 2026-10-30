#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Hec.Dss.Tests
{
  /// <summary>
  /// XGoodFit addition: compares every native declaration in the ported DssNative.cs with the HEC-DSS
  /// header it's built from (upstream/hec-dss/heclib/hecdss/hecdss.h, 7-JA-7), parameter for parameter
  /// (docs/migration-plan.md, R18). Bumping hec-dss without updating DssNative.cs fails here.
  /// </summary>
  [TestClass]
  public class DeclarationTests
  {
    /// <summary>Declared by wrapper 2.0.3 but not exported by 7-JA-7. Nothing RMC-BestFit uses calls them.</summary>
    private static readonly string[] NotIn7JA7 =
    {
      "hec_dss_convertToVersion7",          // DSS 6 conversion, dropped with DSS 6 support
      "hec_dss_deprecated_ifltab",          // not exported by 7-IU-6 either
      "hec_dss_deprecated_ifltab_print",
    };

    /// <summary>Upstream declares an int return for this void function; nothing reads it (see DssNative.cs).</summary>
    private const string VoidDeclaredAsInt = "hec_dss_julianToYearMonthDay";

    private static IEnumerable<MethodInfo> NativeDeclarations => typeof(DssNative)
      .GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic)
      .Where(method => (method.Attributes & MethodAttributes.PinvokeImpl) != 0);

    /// <summary>The native function a declaration calls (hec_dss_open's is declared under another name).</summary>
    private static string EntryPoint(MethodInfo method) => method.GetCustomAttribute<DllImportAttribute>()?.EntryPoint ?? method.Name;

    /// <summary>Every declaration matches hecdss.h: return type, parameter count and each parameter's type.</summary>
    [TestMethod]
    public void EveryDeclarationMatchesTheHeader()
    {
      IReadOnlyDictionary<string, CFunction> header = ReadHeader();
      var problems = new List<string>();
      int compared = 0;
      foreach (MethodInfo method in NativeDeclarations.OrderBy(EntryPoint, StringComparer.Ordinal))
      {
        string name = EntryPoint(method);
        bool known = header.TryGetValue(name, out CFunction? function);
        if (NotIn7JA7.Contains(name))
        {
          if (known)
            problems.Add(name + " is in hecdss.h again: check its declaration and remove it from NotIn7JA7");
          continue;
        }
        if (!known)
        {
          problems.Add(name + " isn't declared in hecdss.h");
          continue;
        }
        compared++;

        string expectedReturn = name == VoidDeclaredAsInt ? "int" : function!.ReturnType;
        if (!ReturnMatches(method.ReturnType, expectedReturn))
          problems.Add($"{name} returns {method.ReturnType.Name}; hecdss.h: {function!.ReturnType}");
        ParameterInfo[] parameters = method.GetParameters();
        if (parameters.Length != function!.ParameterTypes.Count)
        {
          problems.Add($"{name} has {parameters.Length} parameters; hecdss.h: {function.ParameterTypes.Count} ({string.Join(", ", function.ParameterTypes)})");
          continue;
        }
        for (int i = 0; i < parameters.Length; i++)
        {
          if (!ParameterMatches(parameters[i].ParameterType, function.ParameterTypes[i]))
            problems.Add($"{name} parameter {i + 1} ({parameters[i].Name}) is {parameters[i].ParameterType.Name}; hecdss.h: {function.ParameterTypes[i]}");
        }
      }

      Assert.AreEqual(0, problems.Count, Environment.NewLine + string.Join(Environment.NewLine, problems));
      // Wrapper 2.0.3's 31 declarations, less the 3 7-JA-7 doesn't have, plus XGoodFit's hec_dss_api_version.
      Assert.AreEqual(29, compared);
    }

    /// <summary>The library exports everything declared, except the functions 7-JA-7 no longer has.</summary>
    [TestMethod]
    public void TheLibraryExportsEveryDeclaredFunction()
    {
      IntPtr library = NativeLibraryTests.LoadBundledLibrary();
      foreach (MethodInfo method in NativeDeclarations)
      {
        string name = EntryPoint(method);
        bool exported = NativeLibrary.TryGetExport(library, name, out _);
        Assert.AreEqual(!NotIn7JA7.Contains(name), exported, name);
      }
    }

    /// <summary>
    /// Upstream's signatures of the six functions 7-JA-7 changed are still there, so every other wrapper
    /// file compiles unchanged, and each forwards to a native declaration with 7-JA-7's parameters.
    /// </summary>
    [TestMethod]
    [DataRow("hec_dss_tsRetrieve", 18, 20)]
    [DataRow("hec_dss_tsStoreRegular", 11, 13)]
    [DataRow("hec_dss_tsStoreIregular", 12, 14)]
    [DataRow("hec_dss_pdRetrieve", 18, 20)]
    [DataRow("hec_dss_pdStore", 14, 15)]
    [DataRow("hec_dss_gridStore", 29, 29)]
    public void ChangedFunctionsKeepUpstreamsSignature(string name, int upstreamParameters, int nativeParameters)
    {
      MethodInfo[] overloads = typeof(DssNative).GetMethods(BindingFlags.Static | BindingFlags.Public)
        .Where(method => method.Name == name).ToArray();
      Assert.AreEqual(2, overloads.Length, name);
      MethodInfo native = overloads.Single(method => (method.Attributes & MethodAttributes.PinvokeImpl) != 0);
      MethodInfo upstream = overloads.Single(method => method != native);
      Assert.AreEqual(nativeParameters, native.GetParameters().Length, name);
      Assert.AreEqual(upstreamParameters, upstream.GetParameters().Length, name);
    }

    private sealed record CFunction(string ReturnType, IReadOnlyList<string> ParameterTypes);

    /// <summary>Reads the HECDSS_API prototypes of hecdss.h, with types normalized ("const char*", "int*", "dss_file**").</summary>
    private static IReadOnlyDictionary<string, CFunction> ReadHeader()
    {
      string path = typeof(DeclarationTests).Assembly.GetCustomAttributes<AssemblyMetadataAttribute>().Single(a => a.Key == "HecDssHeader").Value!;
      string text = File.ReadAllText(path);
      text = Regex.Replace(text, @"/\*.*?\*/", " ", RegexOptions.Singleline);
      text = Regex.Replace(text, @"//[^\n]*", " ");

      var functions = new Dictionary<string, CFunction>(StringComparer.Ordinal);
      foreach (Match match in Regex.Matches(text, @"HECDSS_API\s+(?<return>[\w\s\*]+?)\s*\b(?<name>hec_dss_\w+)\s*\((?<parameters>[^)]*)\)\s*;"))
      {
        string parameters = match.Groups["parameters"].Value.Trim();
        List<string> types = parameters.Length == 0 || parameters == "void"
          ? new List<string>()
          : parameters.Split(',').Select(parameter => NormalizeType(Regex.Replace(parameter.Trim(), @"\w+$", ""))).ToList();
        functions.Add(match.Groups["name"].Value, new CFunction(NormalizeType(match.Groups["return"].Value), types));
      }
      Assert.IsTrue(functions.Count >= 30, "hecdss.h wasn't read: found " + functions.Count + " functions in " + path);
      return functions;
    }

    /// <summary>"const  char *" becomes "const char*"; const is dropped from values ("const int" is "int").</summary>
    private static string NormalizeType(string type)
    {
      string compact = Regex.Replace(type.Trim(), @"\s*\*", "*");
      compact = Regex.Replace(compact, @"\s+", " ");
      return compact.EndsWith("*", StringComparison.Ordinal) ? compact : Regex.Replace(compact, @"^const ", "");
    }

    private static bool ReturnMatches(Type type, string c)
    {
      if (type == typeof(void)) return c == "void";
      if (type == typeof(int)) return c == "int";
      if (type == typeof(IntPtr)) return c.EndsWith("*", StringComparison.Ordinal);
      return false;
    }

    private static bool ParameterMatches(Type type, string c)
    {
      if (type.IsByRef)
      {
        Type element = type.GetElementType()!;
        return (element == typeof(IntPtr) && c == "dss_file**") || c == CPointerTo(element);
      }
      if (type == typeof(IntPtr)) return c == "dss_file*";
      if (type == typeof(string)) return c == "const char*";
      if (type == typeof(byte[])) return c == "char*" || c == "const char*";
      if (type.IsArray) return c == CPointerTo(type.GetElementType()!);
      return c == CName(type);
    }

    private static string CPointerTo(Type element) => CName(element) + "*";

    private static string CName(Type type)
    {
      if (type == typeof(int)) return "int";
      if (type == typeof(double)) return "double";
      if (type == typeof(float)) return "float";
      return "(" + type.Name + ")";
    }
  }
}
