# XGoodFit

XGoodFit is an unofficial, community-maintained, cross-platform port of
[RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit), the U.S. Army Corps of Engineers Risk
Management Center's desktop application for Bayesian flood-frequency analysis and distribution
fitting.

The goal is the same application, looking and behaving the same, running natively on
**Windows, macOS and Linux**, and reading and writing the same `.bestfit` project files.

> [!NOTE]
> XGoodFit is in early development (Phase 0: groundwork and de-risking) and is not usable yet.
> The [migration plan](docs/migration-plan.md) describes the approach and roadmap.

## Approach

- RMC-BestFit's statistical engine (the `RMC.BestFit` model library) and `RMC.Numerics` are used
  unchanged.
- The Windows-only WPF desktop layers are ported file for file to [Avalonia](https://avaloniaui.net),
  keeping the original structure, logic and look.
- Upstream sources are pinned in [`upstream.lock`](upstream.lock) and fetched on demand, so each
  upstream release can be re-ported change by change.

## Building

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0), git 2.35 or later,
and bash. On Windows, use Git Bash, which comes with Git for Windows.

```bash
git clone https://github.com/jonklaric/XGoodFit.git
cd XGoodFit

# Download the pinned upstream sources into upstream/ (about 22 MB).
scripts/fetch-upstream.sh

dotnet build XGoodFit.slnx
```

If the upstream sources are missing or out of date, the build stops and says so. This happens, for
example, after a pull that changed `upstream.lock`. Re-run `scripts/fetch-upstream.sh` to fix it.

To check that this machine gives the same results as the other platforms:

```bash
# Also download the sample data (about 480 MB): BestFit's example projects and HEC's DSS files.
scripts/fetch-upstream.sh --with-examples

# Run upstream's portable test suites.
scripts/run-upstream-tests.sh

# Check that every example project opens and reads correctly.
dotnet run --project tools/ProjectProbe/ProjectProbe.csproj -- upstream/RMC-BestFit/examples
```

## Where the source code comes from

XGoodFit's own code lives in this repository. Everything else is downloaded at pinned versions, so
every build uses exactly the same sources.

| Code | Comes from | How a build gets it |
|---|---|---|
| XGoodFit's own code: the Avalonia port (from Phase 1), tools and scripts | This repository | `git clone` |
| RMC-BestFit: the model library, and the WPF code being ported | [USACE-RMC/RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) | `scripts/fetch-upstream.sh`, at the commit pinned in [`upstream.lock`](upstream.lock) |
| WPF-Framework: the database and expression libraries, the OxyPlot fork, and the WPF code being ported | [USACE-RMC/WPF-Framework](https://github.com/USACE-RMC/WPF-Framework) | Same |
| HEC-DSS: the C library, the zlib it builds with, the `Hec.Dss` .NET wrapper, and HEC's sample DSS files | [hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), [zlib](https://github.com/madler/zlib), [hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), [dss-test-data](https://github.com/HydrologicEngineeringCenter/dss-test-data) | Same |
| RMC.Numerics and third-party libraries such as SQLite | Their own repositories | NuGet packages, restored by `dotnet build` |

The fetched sources go into `upstream/`, which is git-ignored and never edited. Portable upstream
code compiles unmodified. The one planned exception is the file of native declarations in HEC's
.NET wrapper: it no longer matches the HEC-DSS library, so a corrected copy will live here. The
Windows-only WPF code is ported into this repository file for file.

## Relationship to RMC-BestFit

XGoodFit is not affiliated with or endorsed by the U.S. Army Corps of Engineers. RMC-BestFit is
developed by USACE-RMC and released under the Zero-Clause BSD licence. Please report problems with
the original application to [its repository](https://github.com/USACE-RMC/RMC-BestFit/issues).

## Licence

XGoodFit is released under the [MIT licence](LICENSE). Third-party components are listed in
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
