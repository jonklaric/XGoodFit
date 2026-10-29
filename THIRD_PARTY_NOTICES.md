# Third-party notices

XGoodFit builds on the following projects. Each project's licence applies to its code. Source code
from other projects is vendored, unmodified, in [`upstream/`](upstream), with its licence files.
[`upstream/README.md`](upstream/README.md) cites each original repository and commit. Entries are
added as dependencies are adopted.

| Project | Use in XGoodFit | Licence |
|---|---|---|
| [RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) (USACE Risk Management Center) | Model library used unchanged; desktop UI ported to Avalonia. Vendored in `upstream/RMC-BestFit` | [0BSD](upstream/RMC-BestFit/LICENSE) |
| [WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) (USACE Risk Management Center) | Portable libraries used unchanged; UI framework ported to Avalonia. Vendored in `upstream/WPF-Framework` | [0BSD](upstream/WPF-Framework/LICENSE) |
| [RMC.Numerics](https://github.com/USACE-RMC/Numerics) (USACE Risk Management Center) | Numerical library used unchanged, built from source. Vendored in `upstream/Numerics` | [0BSD](upstream/Numerics/LICENSE) |
| [OxyPlot](https://github.com/oxyplot/oxyplot) (USACE-RMC fork, inside WPF-Framework) | Charting core | [MIT](upstream/WPF-Framework/src/OxyPlot/LICENSE) |
| [AvalonDock](https://github.com/xceedsoftware/wpftoolkit) (inside WPF-Framework) | Not used by XGoodFit; vendored as part of WPF-Framework, as the reference for the docking UI's look | [Ms-PL](upstream/WPF-Framework/src/AvalonDock/LICENSE) |
| [HEC-DSS](https://github.com/HydrologicEngineeringCenter/hec-dss) (USACE Hydrologic Engineering Center) | Native library for reading DSS files, built from source. Vendored in `upstream/hec-dss` | [MIT](upstream/hec-dss/LICENSE.md) |
| [Hec.Dss .NET wrapper](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) (USACE Hydrologic Engineering Center) | .NET API over the HEC-DSS library, built from source. Vendored in `upstream/hec-dss-dotnet` | No licence file in the repository; treated as MIT, like HEC-DSS, pending confirmation from HEC |
| [zlib](https://github.com/madler/zlib) | Compression inside the HEC-DSS library. Vendored in `upstream/zlib` | [zlib](upstream/zlib/LICENSE) |
| [System.Data.SQLite](https://system.data.sqlite.org) and [SourceGear.sqlite3](https://github.com/ericsink/SQLitePCL.raw) | Reading and writing `.bestfit` project files (NuGet) | Public domain |
