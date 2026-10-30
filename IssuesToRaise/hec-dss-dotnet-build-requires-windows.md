# Hec.Dss only builds on Windows: the pre-build step uses cmd syntax, PowerShell and 7-Zip, and the package has no macOS native

**Target:** [HydrologicEngineeringCenter/hec-dss-dotnet](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet), `dotnet/Hec.Dss/Hec.Dss.csproj`, `download-hecdss.ps1`, `pre-build-clean.ps1`. Found in 2.0.3 at commit [`f8f576f`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/tree/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd), still `main` on 2026-10-30.

**Type:** build · **Severity:** medium

**Verification:** verified by reading the project. The `Exec` command text, run with `sh` (which MSBuild uses for `Exec` on Linux and macOS), fails with `syntax error near unexpected token '('`. The `readme.md` case mismatch was read, not built on a case-sensitive file system.

## Summary

- **The pre-build target** runs `if "$(CleanSolution)" == "true" ( … ) else ( … )`, which is `cmd.exe` syntax. On Linux and macOS MSBuild runs `Exec` through `/bin/sh`, so `dotnet build` fails (MSB3073) before compiling anything. Even with `pwsh` installed, the scripts call `powershell` and `cmd /r 7z e`.
- **The natives** are downloaded as prebuilt 7-IU-6 binaries for win-x64 and linux-x64 from HEC's Nexus, not built from a tagged hec-dss source. There's no macOS (osx-arm64 or osx-x64) native, so the package can't run on macOS.
- **The readme:** `<PackageReadmeFile>readme.md</PackageReadmeFile>` and `<None Include="readme.md" …>` refer to a file named `Readme.md`. On a case-sensitive file system (Linux) the item matches nothing, and packing fails with NuGet's missing-readme error (NU5039).
- **The build needs the network:** every build on a clean checkout downloads binaries.

## Details and how to reproduce

On Linux: `dotnet build dotnet/Hec.Dss/Hec.Dss.csproj` gives `error MSB3073: The command "if "" == "true" ( …" exited with code 2`.

## Why this is an issue and not intended behaviour

The project targets `net9.0` and packs `runtimes/linux-x64/native`, so it's meant to be used off Windows, but it can only be built on Windows. hec-dss itself builds and tests with CMake on Windows, Linux and macOS in HEC's CI.

## Where in the source

- [`dotnet/Hec.Dss/Hec.Dss.csproj`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/Hec.Dss.csproj): the `PreBuild` target (`Exec Command="if &quot;$(CleanSolution)&quot; == &quot;true&quot; ( … powershell -ExecutionPolicy Unrestricted -File download-hecdss.ps1 … )"`), the `native-lib\win\hecdss.dll` and `native-lib\linux\libhecdss.so` items, and `PackageReadmeFile`.
- [`dotnet/Hec.Dss/download-hecdss.ps1`](https://github.com/HydrologicEngineeringCenter/hec-dss-dotnet/blob/f8f576ff98b524a4a6b5a2f17735adf5f144b9fd/dotnet/Hec.Dss/download-hecdss.ps1): `$win_version = "7-IU-6"`, `Invoke-WebRequest`, `cmd /r 7z e`.

## Suggested fix

- Replace the `Exec` with MSBuild tasks that work everywhere: `DownloadFile` and `Unzip` from `Microsoft.Build.Tasks`, guarded by `Condition="!Exists(...)"`. Or move the download out of the build into a documented script or CI step.
- Better, build the natives from a pinned hec-dss release with CMake in CI for win-x64, linux-x64, osx-arm64 and osx-x64, and pack them. hec-dss `main` already packages its natives with CPack (#361).
- Rename the readme reference to `Readme.md`, or rename the file.

## How XGoodFit handles it

XGoodFit builds hec-dss 7-JA-7 from source for all four runtimes ([`native/build-hecdss.sh`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/native/build-hecdss.sh)) and compiles the wrapper's sources in its own project without the pre-build step ([`src/Hec.Dss/Hec.Dss.csproj`](https://github.com/jonklaric/XGoodFit/blob/fcb3902c2937662ed5f1b3869e0ea019e88e3c63/src/Hec.Dss/Hec.Dss.csproj)).
