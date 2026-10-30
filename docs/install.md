# Installing XGoodFit

XGoodFit's packages are on the [releases page](https://github.com/jonklaric/XGoodFit/releases). Each
release has packages for Windows, macOS (Apple silicon and Intel) and Linux; installed copies update
themselves from the same page. The packages include everything XGoodFit needs, including .NET.

> [!NOTE]
> Until the RMC-BestFit application itself is ported, the packages contain XGoodFit's theme gallery,
> which stands in for it. They install, update and open `.bestfit` files as the application will.

`.bestfit` files are shared with RMC-BestFit. XGoodFit adds itself as another application that can
open them; where RMC-BestFit is installed, it stays the one that opens them on a double-click.

## Windows

1. Download `io.github.jonklaric.xgoodfit-win-x64-Setup.exe` and run it. It installs XGoodFit for
   you (no administrator needed) in `%LocalAppData%\io.github.jonklaric.xgoodfit`, adds it to the
   Start menu and the desktop, and starts it.
2. XGoodFit isn't signed with a code-signing certificate yet, so Windows SmartScreen says "Windows
   protected your PC". Click **More info**, then **Run anyway**.

To open a `.bestfit` file with XGoodFit, right-click it and choose **Open with**, then **XGoodFit**.
To make XGoodFit the default, choose **Choose another app**, then XGoodFit, and tick **Always use
this app**. Where RMC-BestFit isn't installed, Windows asks which application to use the first time.

**Portable:** `io.github.jonklaric.xgoodfit-win-x64-Portable.zip` runs from any folder: unzip it and
run `XGoodFit.exe`. It updates itself in that folder, but doesn't add itself to "Open with".

**Uninstall:** Settings, Apps, Installed apps, XGoodFit, Uninstall.

## macOS

1. Download the package for your Mac: `…-osx-arm64-…` for Apple silicon (M1 and later), or
   `…-osx-x64-…` for Intel.
2. Either run `io.github.jonklaric.xgoodfit-osx-<arch>-Setup.pkg`, which installs `XGoodFit.app` in
   Applications, or unzip `io.github.jonklaric.xgoodfit-osx-<arch>-Portable.zip` and drag
   `XGoodFit.app` to Applications.

Releases are meant to be signed and notarized by Apple. A package that isn't (for example a test
build) is blocked when you first open it: open **System Settings**, **Privacy & Security**, and
click **Open Anyway** next to the message about XGoodFit. An unsigned `.pkg` is blocked the same
way. Only do this for packages from this repository.

To open a `.bestfit` file with XGoodFit, double-click it (if no other application opens them), or
right-click it and choose **Open With**, then **XGoodFit**. **Get Info**, **Open with**,
**Change All…** makes XGoodFit the default for every `.bestfit` file.

**Uninstall:** drag `XGoodFit.app` from Applications to the Bin (Trash).

## Linux

**AppImage:** download `io.github.jonklaric.xgoodfit-linux-x64.AppImage`, put it where you'll keep
it (for example `~/Applications`), make it executable and run it:

```bash
chmod +x io.github.jonklaric.xgoodfit-linux-x64.AppImage
./io.github.jonklaric.xgoodfit-linux-x64.AppImage
```

AppImages need FUSE (`fusermount`), which desktop distributions include; without it, run it with
`--appimage-extract-and-run`. The AppImage updates itself in place. To add XGoodFit to the
applications menu and register it for `.bestfit` files, run it once with `--install-desktop-entry`
(and `--remove-desktop-entry` to remove that again). Tools such as AppImageLauncher or Gear Lever do
the same.

**Tarball:** unpack `XGoodFit-<version>-linux-x64.tar.gz` and run the program in its `bin` folder.
`./desktop-integration.sh --install-desktop-entry` adds it to the menu and registers it for
`.bestfit` files. The tarball doesn't update itself: download a new one.

Once registered, XGoodFit opens "BestFit project" files: double-click one if no other application
opens them, or choose XGoodFit with **Open With**. Registering never makes XGoodFit the default
when another application is: your file manager's settings decide that.

**Uninstall:** run `--remove-desktop-entry` (AppImage) or `desktop-integration.sh
--remove-desktop-entry` (tarball), then delete the file or folder.

## Updates

An installed XGoodFit checks this repository's releases for a newer version and installs it when
you agree. The Windows installer, the portable zip, the macOS `.app` and the AppImage update
themselves; the Linux tarball doesn't.

## Where XGoodFit keeps your settings

| | Settings, recent files and window layout | Update downloads |
|---|---|---|
| Windows | `%AppData%\XGoodFit\settings` | `%LocalAppData%\io.github.jonklaric.xgoodfit\packages` |
| macOS | `~/Library/Application Support/XGoodFit/settings` | `~/Library/Caches/velopack/io.github.jonklaric.xgoodfit` |
| Linux | `$XDG_CONFIG_HOME/XGoodFit/settings` (`~/.config/XGoodFit/settings` by default) | `/var/tmp/velopack/io.github.jonklaric.xgoodfit` |

Uninstalling leaves the settings; delete the folder to remove them too.
