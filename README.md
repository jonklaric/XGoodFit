# XGoodFit

XGoodFit is an unofficial, community-maintained, cross-platform port of
[RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), the U.S. Army Corps of Engineers Risk
Management Center's desktop application for Bayesian flood-frequency analysis and distribution
fitting.

The goal is the same application, looking and behaving the same, running natively on
**Windows, macOS and Linux**, and reading and writing the same `.bestfit` project files.

> [!NOTE]
> XGoodFit is in early development and is not usable yet. The groundwork is in place and the UI port
> has begun with the VS2013 theme (Light, Blue and Dark). The [migration plan](docs/migration-plan.md)
> describes the approach and roadmap.

## Approach

- RMC-BestFit's statistical engine (the `RMC.BestFit` model library) and `RMC.Numerics` are used
  unchanged.
- The Windows-only WPF desktop layers are ported file for file to [Avalonia](https://avaloniaui.net),
  keeping the original structure, logic and look.
- The upstream source code is vendored in [`upstream/`](upstream), copied byte for byte from the
  commits pinned in [`upstream.lock`](upstream.lock). Each upstream release can be vendored again
  and its changes ported one by one.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and git. Everything
needed to build is in this repository:

```bash
git clone https://github.com/jonklaric/XGoodFit.git
cd XGoodFit
dotnet build XGoodFit.slnx

# See the ported theme so far: palettes and controls, with a Light/Blue/Dark switcher.
dotnet run --project tools/ThemeGallery
```

To check that this machine gives the same results as the other platforms, you also need bash (on
Windows, Git Bash, which comes with Git for Windows) and git 2.35 or later:

```bash
# Download the sample data (about 480 MB): BestFit's example projects and HEC's DSS files.
scripts/fetch-sample-data.sh

# Run upstream's portable test suites.
scripts/run-upstream-tests.sh

# Check that every example project opens and reads correctly.
dotnet run --project tools/ProjectProbe/ProjectProbe.csproj -- sample-data/RMC-BestFit/examples
```

## Where the source code comes from

All the code needed to build XGoodFit is in this repository. Code that comes from other projects is
in [`upstream/`](upstream), with [a table](upstream/README.md) that cites each original repository,
commit, version and licence.

| Code | Original repository | In this repository |
|---|---|---|
| XGoodFit's own code: the Avalonia port (from Phase 1), tools and scripts | This repository | `src/`, `tools/`, `scripts/` |
| RMC-BestFit: the model library, and the WPF code being ported | [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) | `upstream/RMC-BestFit` |
| WPF-Framework: the database and expression libraries, the OxyPlot fork, and the WPF code being ported | [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) | `upstream/WPF-Framework` |
| RMC.Numerics: the numerical library under the model | [USACE-RMC/Numerics](https://github.com/USACE-RMC/Numerics) | `upstream/Numerics` |
| HEC-DSS: the C library, the zlib it builds with, and the `Hec.Dss` .NET wrapper | [hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), [zlib](https://github.com/madler/zlib), [hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet) | `upstream/hec-dss`, `upstream/zlib`, `upstream/hec-dss-dotnet` |
| Third-party libraries published on NuGet, such as SQLite | Their own repositories | Not stored: `dotnet build` restores them at pinned versions |
| Sample data for the test suites | [RMC-BestFit examples](https://github.com/USACE-RMC/RMC-BestFit/tree/main/examples), [dss-test-data](https://github.com/HydrologicEngineeringCenter/dss-test-data) | Not stored: `scripts/fetch-sample-data.sh` downloads it |

Upstream code is never edited. Portable upstream code compiles unmodified. The one planned exception
is the file of native declarations in HEC's .NET wrapper: it no longer matches the HEC-DSS library,
so a corrected copy will live in `src/`. The Windows-only WPF code is ported into `src/` file for
file.

## Relationship to RMC-BestFit

XGoodFit is not affiliated with or endorsed by the U.S. Army Corps of Engineers. RMC-BestFit is
developed by USACE-RMC and released under the Zero-Clause BSD licence. Please report problems with
the original application to [its repository](https://github.com/USACE-RMC/RMC-BestFit/issues).

If you use XGoodFit in your work, please cite the original software: RMC-BestFit, and the Numerics
library it is built on. [`upstream/README.md`](upstream/README.md) gives the citations, and each
project's `CITATION.cff` is included.

## Licence

XGoodFit is released under the [MIT licence](LICENSE). Third-party components are listed in
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
