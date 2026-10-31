# Packaging

XGoodFit's installers and updates use [Velopack](https://github.com/velopack/velopack) (MIT) on every
OS, with this repository's GitHub Releases as the update feed (decision D7, migration plan §9 and
Phase 8). `scripts/package.sh` builds the packages and `scripts/check-package.sh` checks them;
`.github/workflows/package.yml` does both on each OS and drafts releases. For installing them, see
[docs/install.md](../docs/install.md).

The packages hold XGoodFit, the port of RMC-BestFit's desktop application
(`src/BestFit/RMC.BestFit.App`), which `APP_PROJECT` in `scripts/package.sh` names; see
[The application](#the-application).

## What each runtime gets

Each runtime is a Velopack *channel* of its own (`win-x64`, `osx-arm64`, `osx-x64`, `linux-x64`), so
a copy updates only from its own runtime's packages. `scripts/package.sh <rid>` writes them to
`artifacts/packages/<rid>/` (`io.github.jonklaric.xgoodfit` is the app ID):

| Runtime | Packages | |
|---|---|---|
| `win-x64` | `…-win-x64-Setup.exe` | Installs for the user (no administrator) in `%LocalAppData%\io.github.jonklaric.xgoodfit`, with Start menu and desktop shortcuts, and registers `.bestfit` |
| | `…-win-x64-Portable.zip` | Runs from any folder (`XGoodFit.exe`) and updates itself there; registers nothing |
| `osx-arm64`, `osx-x64` | `…-osx-<arch>-Setup.pkg` | Installs `XGoodFit.app` in `/Applications` (or the user's own Applications folder) |
| | `…-osx-<arch>-Portable.zip` | `XGoodFit.app` itself |
| `linux-x64` | `…-linux-x64.AppImage` | Runs as it is and updates itself; `--install-desktop-entry` registers it |
| | `XGoodFit-<version>-linux-x64.tar.gz` | The same files, unpacked (`bin/`, `share/`, `desktop-integration.sh`); doesn't update itself |
| every runtime | `…-<version>-<rid>-full.nupkg`, `-delta.nupkg`, `releases.<rid>.json`, `assets.<rid>.json`, `RELEASES-<rid>` | Velopack's release files: the update feed reads these |

Every package has:
- the application published self-contained (no .NET install needed) and untrimmed, since the
  application relies on reflection;
- the HEC-DSS native library for its runtime (`runtimes/<rid>/native`, built by
  `native/build-hecdss.sh`);
- Velopack's updater beside the application, which `PlatformStartup` uses (see below).

## Building them

Each OS builds its own packages: macOS's need macOS's `codesign` and `pkgbuild`, and the natives are
built on the OS they're for. You need the .NET 10 SDK, bash (Git Bash on Windows; macOS's
`/bin/bash` 3.2 is enough), and CMake with a C compiler for the natives
([native/README.md](../native/README.md)). On Linux, Velopack also needs `mksquashfs`
(`squashfs-tools`); `desktop-file-validate` (`desktop-file-utils`) is used if it's there.

```bash
scripts/package.sh                         # this machine's runtime
scripts/package.sh osx-arm64 osx-x64       # on a Mac, both architectures
scripts/check-package.sh osx-arm64 osx-x64 # check them (no install, no window)
```

`vpk` comes from `.config/dotnet-tools.json` (`dotnet tool restore`), pinned to the same version
as the Velopack package (`VelopackVersion` in `Directory.Packages.props`); the script refuses to
pack with different versions, so update both together. `scripts/package.sh --help` lists the
options: another version, release notes, delta packages, and skipping the natives' build.

`check-package.sh` unpacks each package and checks its contents (the HEC-DSS library; Info.plist and
the signature on macOS; the desktop entry, MIME type and icons on Linux), then runs the packaged
application headlessly (its `capture` mode) with its home and temporary folders in
`artifacts/package-check`, so it neither opens a window nor touches the user's folders.

## The application

`APP_PROJECT` in `scripts/package.sh` names the project to package,
`src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj`. What the packages rely on:

1. `PlatformStartup.BuildVelopackApp(args)?.Run();` is the first line of `Main` (XGoodFit.Platform).
   `vpk` refuses a Windows application whose own code doesn't call `VelopackApp…Run()`.
2. The application references `src/Hec.Dss` (through `RMC.BestFit.UI`), which ships the HEC-DSS
   natives.
3. Its headless mode, which `check-package.sh`'s `run_headless` runs: `XGoodFit capture <folder>
   [project]` renders the main window, its Project and Help menus, the project explorer's context
   menu on the analyses and the About window in each theme to `<folder>/xgoodfit-<view>-<theme>.png`,
   then prints `HEC-DSS native library: loaded` (or why it isn't). The shell's settings, layout,
   recent files and log go to a temporary folder; given a project, it opens a copy of it. It's also
   a quick way to look at a project in every theme.

The executable's name is the project's `AssemblyName`; Windows also gets a launcher named after the
product (`XGoodFit.exe`), which shortcuts and the file association use.

## Identity

`Directory.Build.props` holds the branding the packages use (migration plan §1.1):

| Property | Used for |
|---|---|
| `XGoodFitProductName` (`XGoodFit`) | Package title, shortcuts, Windows launcher, `XGoodFit.app`, settings folder |
| `XGoodFitAppId` (`io.github.jonklaric.xgoodfit`) | Velopack pack ID, macOS bundle ID, Windows install folder, `.bestfit`'s ProgID and UTI, Linux desktop entry and icon names |
| `Version` | Package version (SemVer 2); a release tag must be `v` + it |
| `XGoodFitUpdateRepository` | The GitHub repository whose releases are the update feed |
| `XGoodFitIcon` (`branding/XGoodFit.ico`) | The icon, an `.ico` with sizes up to 256 px: Windows uses it as it is; `tools/AppIcons` renders the macOS `.icns` and the Linux PNGs from it |
| `XGoodFitLargeIcon` (`branding/xgoodfit-1024.png`) | The icon at 1024 px, for the `.icns`'s 512 and 1024 px sizes, which an `.ico` can't hold (optional: without it the `.icns` stops at 256 px) |

The icon is XGoodFit's own, drawn by `tools/Branding`: see [branding/README.md](../branding/README.md).
So each OS gets it this way:

| OS | Where the icon comes from |
|---|---|
| Windows | `XGoodFit.ico` as it is: the executable's icon (`ApplicationIcon`), which its shortcuts show, and `Setup.exe`'s (`vpk --icon`) |
| macOS | `app.icns`, which `tools/AppIcons` makes: 16 to 256 px from `XGoodFit.ico`, 512 and 1024 px from `xgoodfit-1024.png`; Velopack puts it in the bundle (`XGoodFit.icns`, `CFBundleIconFile`) |
| Linux | `tools/AppIcons`' PNGs from `XGoodFit.ico` (16 to 256 px), in the AppImage's and the tarball's `share/icons/hicolor/<size>/apps/io.github.jonklaric.xgoodfit.png`, and the 256 px one as the AppImage's own icon |

The application shows it too (`XGoodFitIcon` is an Avalonia resource of `RMC.BestFit.App` and
`RMC.BestFit.UI`): as the main window's icon (the project's image, as upstream's) and in the About box.

## `.bestfit` files

`.bestfit` files are shared with RMC-BestFit, so XGoodFit registers as an extra handler and never
takes over the default:
- **Windows:** the installer's hooks (`WindowsFileAssociation`) add a per-user ProgID,
  `io.github.jonklaric.xgoodfit.bestfit`, to `.bestfit`'s `OpenWithProgids` ("Open with"), and name
  the launcher under `Applications\XGoodFit.exe` ("Choose another app"). `.bestfit`'s own default
  value, which RMC-BestFit's installer sets, is never written. Uninstalling removes them.
- **macOS:** `packaging/macos/Info.plist` exports the type `io.github.jonklaric.xgoodfit.bestfit`
  (`public.data`) and opens it with the rank `Alternate`, so an application that declares itself
  the owner or default handler keeps that role.
- **Linux:** `packaging/linux/mime.xml` defines `application/x-bestfit` (`*.bestfit`, a kind of
  SQLite database), and the desktop entry lists it. `desktop-integration.sh` installs them for the
  user and never writes `mimeapps.list`, which sets defaults.

The running application gets the files to open from `FileOpenRequests`: the command line on Windows
and Linux, Finder's activations on macOS.

## Signing

- **Windows** packages are unsigned (decision D5), so SmartScreen warns about an unknown publisher
  (R15; [docs/install.md](../docs/install.md) shows users the "More info, Run anyway" step). If the
  project gets a free certificate from the SignPath Foundation, `vpk pack` signs with
  `--signParams` or `--signTemplate`.
- **macOS** bundles always have the hardened runtime and .NET's entitlements
  (`packaging/macos/XGoodFit.entitlements`: `allow-jit`, `allow-unsigned-executable-memory`,
  `allow-dyld-environment-variables`, `disable-library-validation`, the set Microsoft lists for
  .NET apps). Without a Developer ID they're signed ad hoc, which runs on the Mac that built them
  but which Gatekeeper blocks when downloaded. With these variables set, `package.sh` signs with the
  Developer ID and notarizes:

  | Variable | Value |
  |---|---|
  | `XGF_MAC_SIGN_APP_IDENTITY` | `Developer ID Application: <name> (<team ID>)` |
  | `XGF_MAC_SIGN_INSTALL_IDENTITY` | `Developer ID Installer: <name> (<team ID>)`, for the `.pkg` |
  | `XGF_MAC_NOTARY_PROFILE` | A profile stored with `xcrun notarytool store-credentials` |
  | `XGF_MAC_KEYCHAIN` | The keychain that holds them, if not the default |
- **Linux** AppImages and tarballs aren't signed.

## Continuous integration

`.github/workflows/package.yml` runs by hand (Actions, Package, Run workflow) and on version tags.
On each OS's runner it builds the natives, packs and checks the packages (macOS makes both
architectures), and uploads them as artifacts (`xgoodfit-windows-x64`, `xgoodfit-macos`,
`xgoodfit-linux-x64`). It first downloads the previous release's full packages, so each release
also has delta packages. Linux packages are built on the oldest Ubuntu runner, so the HEC-DSS library
asks for as old a glibc as possible.

On a tag `vX.Y.Z` (which must equal `Version`), a last job uploads every runtime's Velopack files to
one **draft** GitHub release (`vpk upload github --merge`), and attaches the tarball. Review it and
publish it: published releases are the update feed. A version with a pre-release part (such as
`0.2.0-beta.1`) is drafted as a GitHub pre-release; copies running a pre-release version update to
pre-releases too, others only to full releases. If a job fails, delete the draft before running it
again.

### Secrets

Add these in the repository's settings (Secrets and variables, Actions). Without
`APPLE_DEVELOPER_ID_CERTIFICATE`, macOS builds are signed ad hoc; without the notary three, they're
signed but not notarized.

| Secret | Value |
|---|---|
| `APPLE_DEVELOPER_ID_CERTIFICATE` | The Developer ID certificates and private keys, exported from Keychain Access as one `.p12` and base64-encoded (`base64 -i developer-id.p12`): "Developer ID Application" for the app, and "Developer ID Installer" for the `.pkg` |
| `APPLE_DEVELOPER_ID_CERTIFICATE_PASSWORD` | The password the `.p12` was exported with |
| `APPLE_ID` | The Apple ID of the developer account, for notarization |
| `APPLE_TEAM_ID` | The team's 10-character ID (developer.apple.com, Membership) |
| `APPLE_APP_SPECIFIC_PASSWORD` | An app-specific password for that Apple ID (account.apple.com, Sign-In and Security) |

The workflow imports the certificates into a temporary keychain, which it deletes at the end.

## Updates in the application

`XGoodFit.Platform`'s `VelopackUpdateService` implements upstream SoftwareUpdate's `IUpdateService`
over Velopack's `UpdateManager` and the GitHub feed. FrameworkUI's main window drives the interface
("Check for Updates…", download with progress, install and restart), so the ported `App` only
changes the service it creates, in place of RMC-BestFit's `GitHubUpdateService`:

```csharp
var updateService = new XGoodFit.Platform.VelopackUpdateService(XGoodFit.Platform.VelopackUpdateService.CreateOptions());
mainWindow.UpdateService = updateService.IsInstalled ? updateService : null;
```

A copy that wasn't installed from a package (a development build, the tarball) can't update itself;
leaving the service out hides the menu item, and otherwise a check says so.
