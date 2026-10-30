# Plot-settings fixtures

The XML that RMC-BestFit's WPF OxyPlot fork (`upstream/WPF-Framework/src/OxyPlot/OxyPlot.Wpf`) writes
for the tests' plots, through `PlotSerializer.ToXElement`. `SerializationTests` checks that the Avalonia
port writes exactly the same XML for the same plots, and reads it back unchanged. RMC-BestFit saves this
XML in `.bestfit` project files, which both apps open.

| File | Plot (`TestPlots.cs`) |
|---|---|
| `FrequencyPlot.wpf.xml` | `FrequencyPlot()`: a frequency plot like BestFit's, with a normal probability axis, a fitted line, observed points, a hidden series and annotations |
| `EveryElement.wpf.xml` | `EveryElement()`: one of every axis (15), series (21) and annotation (9) type the serializers read back, with colours and other settings changed from their defaults |

## Regenerating them

The fixtures come from WPF itself, so they're made on Windows. `tools/OxyPlotFixtures.Wpf` compiles
`TestPlots.cs` against the WPF fork (with `WPF` defined; the file's aliases give WPF's and Avalonia's
types the same names) and writes the XML. From the repository root:

```sh
dotnet run -c Release --project tools/OxyPlotFixtures.Wpf -- tests/Framework/OxyPlot.Wpf.Tests/Fixtures
```

Regenerate them when `TestPlots.cs` changes, or when a new upstream version changes the serializers,
and commit them with that change. The tool isn't in `XGoodFit.slnx`, as it only builds on Windows.
