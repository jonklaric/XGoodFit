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

You need the [.NET 10 SDK](https://dotnet.microsoft.com/download/dotnet/10.0) and git. On Windows,
run the script from Git Bash.

```bash
# Fetch the pinned upstream sources. Omit --with-examples to skip the 455 MB of example projects.
scripts/fetch-upstream.sh --with-examples

# Check that every example project opens and reads correctly on this machine.
dotnet run --project tools/ProjectProbe/ProjectProbe.csproj -- upstream/RMC-BestFit/examples
```

## Relationship to RMC-BestFit

XGoodFit is not affiliated with or endorsed by the U.S. Army Corps of Engineers. RMC-BestFit is
developed by USACE-RMC and released under the Zero-Clause BSD licence. Please report problems with
the original application to [its repository](https://github.com/USACE-RMC/RMC-BestFit/issues).

## Licence

XGoodFit is released under the [MIT licence](LICENSE). Third-party components are listed in
[THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md).
