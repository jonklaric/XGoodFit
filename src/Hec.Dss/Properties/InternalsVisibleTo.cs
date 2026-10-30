using System.Runtime.CompilerServices;

// XGoodFit addition: XGoodFit's tests of the wrapper (tests/Hec.Dss.Tests) compile HEC's own wrapper
// tests, which call DssNative directly, as upstream's DotNetTests project does through the
// InternalsVisibleTo in DssReader.cs.
[assembly: InternalsVisibleTo("Hec.Dss.Tests")]
