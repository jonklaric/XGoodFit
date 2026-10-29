# Third-party notices

XGoodFit builds on the following projects. Each project's licence applies to its code.
Entries are added as dependencies are adopted.

| Project | Use in XGoodFit | Licence |
|---|---|---|
| [RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) (USACE Risk Management Center) | Model library used unchanged; desktop UI ported to Avalonia | 0BSD |
| [WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) (USACE Risk Management Center) | Portable libraries used unchanged; UI framework ported to Avalonia | 0BSD |
| [RMC.Numerics](https://github.com/USACE-RMC/Numerics) (USACE Risk Management Center) | Numerical library used unchanged (NuGet) | 0BSD |
| [OxyPlot](https://github.com/oxyplot/oxyplot) (USACE-RMC fork, via WPF-Framework) | Charting core | MIT |
| [System.Data.SQLite](https://system.data.sqlite.org) and [SourceGear.sqlite3](https://github.com/ericsink/SQLitePCL.raw) | Reading and writing `.bestfit` project files | Public domain |
| [HEC-DSS](https://github.com/HydrologicEngineeringCenter/hec-dss) (USACE Hydrologic Engineering Center) | Native library for reading DSS files, built from source | MIT |
| [Hec.Dss .NET wrapper](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) (USACE Hydrologic Engineering Center) | .NET API over the HEC-DSS library, built from source | No licence file in the repository; treated as MIT, like HEC-DSS, pending confirmation from HEC |
| [zlib](https://github.com/madler/zlib) | Compression inside the HEC-DSS library | zlib |
