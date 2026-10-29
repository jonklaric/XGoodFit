# Vendored upstream source code

XGoodFit builds entirely from this repository. The folders here are verbatim copies of the
open-source projects it is built from, at the commits pinned in [`upstream.lock`](../upstream.lock).
`scripts/vendor-upstream.sh` copies them and checks that every file is byte-identical to the upstream
commit.

Nothing here is edited by hand. XGoodFit's own code lives outside `upstream/`, and the ported UI
lives in `src/`. So every upstream file can be compared with its port, and each new upstream release
can be vendored again and its changes ported one by one ([migration plan §10](../docs/migration-plan.md#10-tracking-upstream)).

| Folder | Original repository | Commit | Version | Licence | What was copied |
|---|---|---|---|---|---|
| [`RMC-BestFit`](RMC-BestFit) | [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) | [`bfce2dd`](https://github.com/USACE-RMC/RMC-BestFit/tree/bfce2ddbebb8d75e6cf6e21c4fef8ecfb5058269) | 2.0.1 | [0BSD](RMC-BestFit/LICENSE) | `src`, `docs`, `tools/PlotReferenceExporter` and the root files |
| [`WPF-Framework`](WPF-Framework) | [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) | [`7f897e1`](https://github.com/USACE-RMC/WPF-Framework/tree/7f897e107db4a25cca29127ce9d564898c5fde93) | 1.0.5 | [0BSD](WPF-Framework/LICENSE); it includes OxyPlot (MIT) and AvalonDock (Ms-PL), see its [THIRD_PARTY_LICENSES.md](WPF-Framework/THIRD_PARTY_LICENSES.md) | Everything |
| [`Numerics`](Numerics) | [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics) | [`7e8e8d1`](https://github.com/USACE-RMC/Numerics/tree/v2.2.0) (tag `v2.2.0`) | 2.2.0 | [0BSD](Numerics/LICENSE) | `Numerics`, `Test_Numerics`, `docs` and the root files, except the instructions for AI coding agents (`AGENTS.md`, `CLAUDE.md`) |
| [`hec-dss`](hec-dss) | [HydrologicEngineeringCenter/hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss) | [`1da5976`](https://github.com/HydrologicEngineeringCenter/hec-dss/tree/7-JA-7) (tag `7-JA-7`) | 7-JA-7 | [MIT](hec-dss/LICENSE.md) | `deps`, `heclib`, `test` and the root files, except a prebuilt tool (`DssVersion.exe`) and the Visual Studio solution and project files of HEC's old Windows build, which CMake replaces |
| [`hec-dss-dotnet`](hec-dss-dotnet) | [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) | [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd) | 2.0.3 | None stated. Treated as MIT, like HEC-DSS, pending confirmation from HEC | `dotnet/Hec.Dss`, `dotnet/DotNetTests` and the root files |
| [`zlib`](zlib) | [madler/zlib](https://github.com/madler/zlib) | [`51b7f2a`](https://github.com/madler/zlib/tree/v1.3.1) (tag `v1.3.1`) | 1.3.1 | [zlib](zlib/LICENSE) | Everything except the optional `contrib` extras |

Each folder keeps its upstream licence file, and the USACE projects keep their `CITATION.cff`.
Please cite the original projects when you use or describe this work:

- **RMC-BestFit:** Smith, C. H., et al. *RMC-BestFit: Bayesian Estimation and Fitting Software for
  Flood Frequency Analysis* (version 2.0.1). U.S. Army Corps of Engineers, Risk Management Center.
  See [`RMC-BestFit/CITATION.cff`](RMC-BestFit/CITATION.cff).
- **WPF Framework:** Smith, C. H., et al. *WPF Framework* (version 1.0.5). U.S. Army Corps of
  Engineers, Risk Management Center. See [`WPF-Framework/CITATION.cff`](WPF-Framework/CITATION.cff).
- **Numerics:** Smith, C. H., et al. *Numerics: A .NET Library for Numerical Computing, Statistical
  Analysis, and Risk Assessment* (version 2.2.0). U.S. Army Corps of Engineers, Risk Management
  Center. See [`Numerics/CITATION.cff`](Numerics/CITATION.cff).
- **HEC-DSS:** U.S. Army Corps of Engineers, Hydrologic Engineering Center,
  <https://www.hec.usace.army.mil/software/hec-dss/>.

## Notes on the choice of versions

- **Numerics 2.2.0** is the exact source of the `RMC.Numerics` 2.2.0 NuGet package that RMC-BestFit
  2.0.1 uses; the package records this commit. XGoodFit builds it from source instead of downloading
  the package (`Directory.Build.rsp` sets RMC-BestFit's own `UseLocalRmcNumerics` switch).
- **HEC-DSS 7-JA-7** is the newest release. It builds with CMake and needs no Fortran. The .NET
  wrapper (2.0.3) was written for the older 7-IU-6 library, and five of its native declarations no
  longer match, so XGoodFit will compile the wrapper with a corrected copy of that one file. See the
  Phase 0 findings in the [migration plan](../docs/migration-plan.md).

## Not vendored

- **Sample data** isn't code and isn't needed to build: RMC-BestFit's 455 MB of example projects and
  HEC's sample DSS files, which have no licence. `scripts/fetch-sample-data.sh` downloads them into
  `sample-data/` for the test suites, at the commits pinned in [`sample-data.lock`](../sample-data.lock).
- **Third-party libraries published on NuGet**, such as System.Data.SQLite and the test frameworks,
  are restored by `dotnet build` at the versions pinned in the project files.

## Files in this folder

`Directory.Build.props`, `Directory.Build.targets`, `Directory.Packages.props` and `.editorconfig`
are XGoodFit's, not upstream's. They contain no settings, so XGoodFit's own build and editor settings
never reach upstream code. `.gitattributes` keeps one zlib file byte-identical.
