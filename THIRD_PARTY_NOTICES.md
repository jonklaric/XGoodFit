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
| [OxyPlot](https://github.com/oxyplot/oxyplot) (USACE-RMC fork, inside WPF-Framework) | Charting core, used unchanged; the fork's WPF layer ported to Avalonia in `src/Framework/OxyPlot` | [MIT](upstream/WPF-Framework/src/OxyPlot/LICENSE) |
| [oxyplot-avalonia](https://github.com/oxyplot/oxyplot-avalonia) (OxyPlot contributors) | Reference for porting OxyPlot's WPF layer to Avalonia (`src/Framework/OxyPlot`): its approach to the plot view, tracker, render context and exporters is adapted there. Not vendored | MIT (Copyright (c) 2014 OxyPlot contributors) |
| [AvalonDock](https://github.com/xceedsoftware/wpftoolkit) (inside WPF-Framework) | Not used by XGoodFit; vendored as part of WPF-Framework, as the reference for the docking UI's look | [Ms-PL](upstream/WPF-Framework/src/AvalonDock/LICENSE) |
| [HEC-DSS](https://github.com/HydrologicEngineeringCenter/hec-dss) (USACE Hydrologic Engineering Center) | Native library for reading DSS files, built from source. Vendored in `upstream/hec-dss` | [MIT](upstream/hec-dss/LICENSE.md) |
| [Hec.Dss .NET wrapper](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) (USACE Hydrologic Engineering Center) | .NET API over the HEC-DSS library, built from source. Vendored in `upstream/hec-dss-dotnet` | No licence file in the repository; treated as MIT, like HEC-DSS, pending confirmation from HEC |
| [zlib](https://github.com/madler/zlib) | Compression inside the HEC-DSS library. Vendored in `upstream/zlib` | [zlib](upstream/zlib/LICENSE) |
| [System.Data.SQLite](https://system.data.sqlite.org) and [SourceGear.sqlite3](https://github.com/ericsink/SQLitePCL.raw) | Reading and writing `.bestfit` project files (NuGet) | Public domain |
| [Avalonia](https://github.com/AvaloniaUI/Avalonia) | The cross-platform UI framework (NuGet) | MIT |
| [Dock](https://github.com/wieslawsoltes/Dock) (Dock.Avalonia.AvalonDock and the packages it uses) | Docking in the main window, with AvalonDock's API and layout XML (NuGet) | MIT |
| [ReactiveUI](https://github.com/reactiveui/ReactiveUI) | Used by Dock's AvalonDock adapter (NuGet) | MIT |
| [ProDataGrid](https://github.com/wieslawsoltes/ProDataGrid) | The data grids (NuGet) | MIT |
| [SkiaSharp](https://github.com/mono/SkiaSharp) | Avalonia's renderer; also gives the glyph outlines of the plots' PDF and SVG exports (NuGet, through Avalonia.Skia) | MIT |
| [Velopack](https://github.com/velopack/velopack) (Velopack Ltd) | Installers, packages and updates: the `Velopack` library (NuGet) and the `vpk` tool, which adds Velopack's launcher and updater (`Update.exe`, `UpdateMac`, `UpdateNix`) to every package | MIT |
| [AppImage type2-runtime](https://github.com/AppImage/type2-runtime) | The runtime at the start of the Linux AppImage, which `vpk` adds. It has [libfuse](https://github.com/libfuse/libfuse) (LGPL-2.1) and [squashfuse](https://github.com/vasi/squashfuse) (BSD-2-Clause) linked in statically | MIT |
| [Selawik](https://github.com/microsoft/Selawik) (Microsoft) | Substitute for Segoe UI where it isn't installed (macOS, Linux). Vendored in `upstream/Selawik` and embedded in the app | [OFL-1.1](upstream/Selawik/LICENSE.txt) |
