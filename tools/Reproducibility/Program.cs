using System.Globalization;
using Reproducibility;

// The numerical reproducibility tool (docs/reproducibility.md).
//
//   Reproducibility list <examples> [--only <text>]...
//       Lists the analyses of the example projects under <examples>, and whether each project holds it as estimated.
//   Reproducibility run <examples> --out <file.json> [--only <text>]... [--limit <minutes>]
//       Estimates again every analysis the projects hold as estimated, and writes the saved and the new results.
//   Reproducibility compare <file.json or folder>... --report <report.md> [--baseline <rid>]
//       Compares the runs of several machines; exits with 1 if any analysis is unexplained.
//
// --only keeps the analyses whose "<example path> | <analysis name>" contains the text (ignoring case); give it several
// times to keep several. --limit cancels an analysis's rerun after that many minutes (default 10), and lists it as
// skipped.

Console.Out.NewLine = "\n";
CultureInfo.DefaultThreadCurrentCulture = CultureInfo.DefaultThreadCurrentUICulture = CultureInfo.InvariantCulture;

if (args.Length < 2 || args[0] is "-h" or "--help")
    return Usage();

try
{
    string command = args[0];
    var positional = new List<string>();
    var only = new List<string>();
    string? output = null, report = null, baseline = null;
    double limit = 10;
    for (int i = 1; i < args.Length; i++)
    {
        switch (args[i])
        {
            case "--only": only.Add(Next(ref i)); break;
            case "--out": output = Next(ref i); break;
            case "--report": report = Next(ref i); break;
            case "--baseline": baseline = Next(ref i); break;
            case "--limit": limit = double.Parse(Next(ref i), CultureInfo.InvariantCulture); break;
            default:
                if (args[i].StartsWith("--", StringComparison.Ordinal)) throw new ArgumentException($"Unknown option {args[i]}.");
                positional.Add(args[i]);
                break;
        }
    }

    switch (command)
    {
        case "list" when positional.Count == 1:
        {
            using var headless = new Headless();
            return Runner.List(positional[0], only.ToArray(), headless);
        }
        case "run" when positional.Count == 1 && output != null:
        {
            using var headless = new Headless();
            return Runner.Run(positional[0], output, only.ToArray(), TimeSpan.FromMinutes(limit), headless, Runner.XGoodFit);
        }
        case "compare" when positional.Count > 0 && report != null:
            Report compared = Report.Read(positional, baseline);
            string markdown = compared.Write();
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
            File.WriteAllText(report, markdown);
            Console.WriteLine(markdown);
            return compared.HasUnexplained ? 1 : 0;
        default:
            return Usage();
    }
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 2;
}

string Next(ref int i) => ++i < args.Length ? args[i] : throw new ArgumentException($"{args[i - 1]} needs a value.");

static int Usage()
{
    Console.Error.WriteLine("Usage:");
    Console.Error.WriteLine("  Reproducibility list <examples> [--only <text>]...");
    Console.Error.WriteLine("  Reproducibility run <examples> --out <file.json> [--only <text>]... [--limit <minutes>]");
    Console.Error.WriteLine("  Reproducibility compare <file.json or folder>... --report <report.md> [--baseline <rid>]");
    return 2;
}
