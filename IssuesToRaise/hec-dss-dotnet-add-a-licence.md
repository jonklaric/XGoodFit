# Please add a licence to hec-dss-dotnet

**Target:** [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), repository root and `dotnet/Hec.Dss/Hec.Dss.csproj`. Checked at commit [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd) (2.0.3), still `main` on 2026-10-30.

**Type:** licence · **Severity:** medium (blocks redistribution decisions downstream)

**Verification:** verified by reading the repository. The root holds only `.gitignore`, `.gitmodules` and `dotnet/`. No file in the repository is a licence, no source file has a licence header, the project declares no `PackageLicenseExpression` or `PackageLicenseFile`, and GitHub shows no licence for the repository.

## Summary

The C library this wrapper calls, [hec-dss](https://github.com/HydrologicEngineeringCenter/hec-dss), is MIT-licensed ([`LICENSE.md`](https://github.com/HydrologicEngineeringCenter/hec-dss/blob/1da59764e509b31fe790133acc757cab8a6b846c/LICENSE.md)). The .NET wrapper states no licence at all, and without one, copyright law reserves all rights by default. Applications that ship `Hec.Dss.dll` can't confirm they're allowed to, for example USACE's [RMC-BestFit](https://github.com/USACE-RMC/RMC-BestFit) and the open-source community port XGoodFit. The NuGet package `Hec.Dss` carries no licence either, since the project file declares none.

## Details and how to reproduce

Look for a licence in the repository, its project file or on GitHub: there's none.

## Why this is an issue and not intended behaviour

- Downstream projects need a clear basis for redistributing the assembly and for building it from source. The C library it wraps is published under MIT, so a licence was probably simply never added.
- The repository's [`.gitmodules`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/.gitmodules) also references HEC's `dss-test-data`, which has no licence either. It's only used by tests, but a sentence on its terms would help too.

## Where in the source

- The repository root: [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd) (only `.gitignore`, `.gitmodules` and `dotnet/`).
- [`dotnet/Hec.Dss/Hec.Dss.csproj`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/Hec.Dss.csproj): package metadata (`Authors`, `PackageDescription`, `PackageVersion` 2.0.3), with no licence property.

## Suggested fix

Please add a `LICENSE` file (MIT, matching hec-dss, would be the natural choice), and declare it in the package:

```xml
<PropertyGroup>
  <PackageLicenseExpression>MIT</PackageLicenseExpression>
</PropertyGroup>
```

If the code is a U.S. Government work and in the public domain, a statement saying so (as some USACE repositories have) would serve the same purpose.

## How XGoodFit handles it

XGoodFit vendors the wrapper's source and, pending an answer from HEC, assumes MIT like the C library. It records this in [`upstream/README.md`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/upstream/README.md) ("None stated. Treated as MIT, like HEC-DSS, pending confirmation from HEC") and in [`docs/migration-plan.md`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/docs/migration-plan.md#L470-L472) (risk R17).
