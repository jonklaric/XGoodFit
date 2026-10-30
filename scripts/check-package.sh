#!/usr/bin/env bash
# Checks the packages scripts/package.sh made, without installing them: their structure (the HEC-DSS
# library; the desktop entry, MIME type and icons; Info.plist and the code signature) and that the
# packaged application starts and renders. It runs the application's headless mode, so no window
# opens, with its home and temporary folders in a work folder, so the user's own aren't touched.
#
# Usage: scripts/check-package.sh [rid...]   (default: this machine's runtime)
#   An osx-x64 package on Apple silicon runs under Rosetta 2 (or isn't run, without it). On Linux,
#   bwrap (if it works) also keeps /var/tmp, where Velopack keeps an AppImage's packages, in the work
#   folder.
# Work folder: artifacts/package-check/<rid>/, deleted first.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

die() { echo "check-package.sh: $*" >&2; exit 1; }
ok() { echo "  ok: $*"; }

case "$(uname -s)" in
  Linux) host_os=linux ;;
  Darwin) host_os=osx ;;
  MINGW*|MSYS*|CYGWIN*) host_os=win ;;
  *) die "unsupported OS: $(uname -s)" ;;
esac
case "$(uname -m)" in
  x86_64|amd64) host_arch=x64 ;;
  arm64|aarch64) host_arch=arm64 ;;
  *) die "unsupported architecture: $(uname -m)" ;;
esac
rids="$*"
[ -n "$rids" ] || rids="$host_os-$host_arch"

native_path() {
  if [ "$host_os" = win ]; then cygpath -w "$1"; else printf '%s\n' "$1"; fi
}

# The same branding scripts/package.sh packs with.
property() { dotnet msbuild src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj -getProperty:"$1" | tr -d '\r'; }
app_id="$(property XGoodFitAppId)"
product="$(property XGoodFitProductName)"
version="$(property Version)"

# Runs the packaged application headlessly: its capture mode, which renders the main window, its
# menus and the About window in each theme to PNG files and reports whether the HEC-DSS library loads.
# Arguments: the command that starts the packaged application.
run_headless() {
  local capture="$work/capture" log="$work/run.log"
  mkdir -p "$capture" "$work/home" "$work/tmp"
  # A home and temporary folder of the check's own: Velopack logs and keeps packages there
  # (~/Library on macOS), and the application keeps its settings there.
  if ! HOME="$work/home" TMPDIR="$work/tmp" XDG_CONFIG_HOME="$work/home/.config" \
      XDG_DATA_HOME="$work/home/.local/share" XDG_CACHE_HOME="$work/home/.cache" \
      "$@" capture "$(native_path "$capture")" > "$log" 2>&1; then
    cat "$log" >&2
    die "the packaged application failed"
  fi
  local count
  count="$(find "$capture" -name '*.png' | wc -l | tr -d ' ')"
  [ "$count" -gt 0 ] || { cat "$log" >&2; die "the packaged application rendered nothing"; }
  ok "the packaged application rendered $count captures"
  grep -q 'HEC-DSS native library: loaded' "$log" || { cat "$log" >&2; die "the packaged application didn't load the HEC-DSS library"; }
  ok "the packaged application loaded the HEC-DSS library"
}

check_windows() {
  local zip="$out/$app_id-$rid-Portable.zip"
  [ -f "$out/$app_id-$rid-Setup.exe" ] || die "no Setup.exe"
  ok "$app_id-$rid-Setup.exe ($(du -h "$out/$app_id-$rid-Setup.exe" | cut -f1))"
  [ -f "$zip" ] || die "no portable zip"
  unzip -q "$zip" -d "$work/portable"
  [ -f "$work/portable/$product.exe" ] && [ -f "$work/portable/Update.exe" ] && [ -f "$work/portable/current/sq.version" ] ||
    die "the portable zip lacks Velopack's launcher, updater or manifest"
  [ -f "$work/portable/current/runtimes/$rid/native/hecdss.dll" ] || die "no hecdss.dll in the portable zip"
  ok "portable zip: $product.exe, Update.exe, current/ with hecdss.dll"

  local exe
  exe="$(sed -n 's:.*<mainExe>\(.*\)</mainExe>.*:\1:p' "$work/portable/current/sq.version")"
  run_headless "$work/portable/current/$exe"
  # Velopack logs to %LocalAppData%\velopack, which the check can't move: remove its log again.
  local logs
  logs="$(cygpath -u "$LOCALAPPDATA")/velopack"
  rm -f "$logs/velopack_$app_id.log"
  rmdir "$logs" 2>/dev/null || true
}

check_macos() {
  local zip="$out/$app_id-$rid-Portable.zip" app plist exe
  [ -f "$out/$app_id-$rid-Setup.pkg" ] || die "no .pkg"
  pkgutil --expand-full "$out/$app_id-$rid-Setup.pkg" "$work/pkg"
  ls -d "$work"/pkg/*.pkg/Payload/"$product.app" >/dev/null || die "the .pkg doesn't install $product.app"
  ok "$app_id-$rid-Setup.pkg installs $product.app ($(pkgutil --check-signature "$out/$app_id-$rid-Setup.pkg" | sed -n 's/^ *Status: //p'))"

  [ -f "$zip" ] || die "no portable zip"
  ditto -x -k "$zip" "$work/portable"
  app="$work/portable/$product.app"
  plist="$app/Contents/Info.plist"
  [ -d "$app" ] || die "the portable zip has no $product.app"
  [ "$(plutil -extract CFBundleIdentifier raw "$plist")" = "$app_id" ] || die "CFBundleIdentifier isn't $app_id"
  [ "$(plutil -extract CFBundleDocumentTypes.0.LSItemContentTypes.0 raw "$plist")" = "$app_id.bestfit" ] || die "no .bestfit document type"
  [ "$(plutil -extract CFBundleDocumentTypes.0.LSHandlerRank raw "$plist")" = "Alternate" ] || die ".bestfit isn't an Alternate handler's"
  [ "$(plutil -extract UTExportedTypeDeclarations.0.UTTypeTagSpecification.public\\.filename-extension.0 raw "$plist")" = "bestfit" ] ||
    die "no exported .bestfit type"
  exe="$(plutil -extract CFBundleExecutable raw "$plist")"
  [ -f "$app/Contents/Resources/$(plutil -extract CFBundleIconFile raw "$plist")" ] || die "the icon is missing"
  ok "Info.plist: $app_id, .bestfit (exported, Alternate), icon, minimum macOS $(plutil -extract LSMinimumSystemVersion raw "$plist")"
  [ -f "$app/Contents/MacOS/runtimes/$rid/native/libhecdss.dylib" ] || die "no libhecdss.dylib in the bundle"
  [ -x "$app/Contents/MacOS/UpdateMac" ] || die "no Velopack updater in the bundle"

  codesign --verify --deep --strict "$app" || die "the bundle's signature doesn't verify"
  local signature
  signature="$(codesign -dv "$app" 2>&1)"
  # (grep reads all the input here: with grep -q, a producer cut off by the early exit fails the pipeline.)
  printf '%s\n' "$signature" | grep 'flags=.*runtime' >/dev/null || die "the bundle isn't signed with the hardened runtime"
  codesign -d --entitlements - "$app" 2>/dev/null | grep 'com.apple.security.cs.allow-jit' >/dev/null ||
    die "the bundle lacks the allow-jit entitlement"
  codesign -dv "$app/Contents/MacOS/runtimes/$rid/native/libhecdss.dylib" 2>&1 | grep 'flags=.*runtime' >/dev/null ||
    die "libhecdss.dylib isn't signed with the hardened runtime"
  ok "signature verifies (deep, strict): $(printf '%s\n' "$signature" | grep -E '^(Signature|flags|Authority|TeamIdentifier)' | head -3 | tr '\n' ' ')"

  if [ "$rid" = osx-x64 ] && [ "$host_arch" = arm64 ]; then
    if arch -x86_64 /usr/bin/true 2>/dev/null; then
      run_headless arch -x86_64 "$app/Contents/MacOS/$exe"
    else
      echo "  skipped: running the osx-x64 application needs Rosetta 2, which this Mac doesn't have"
    fi
  else
    run_headless "$app/Contents/MacOS/$exe"
  fi
}

check_linux() {
  local appimage="$out/$app_id-$rid.AppImage" tarball="$out/$product-$version-$rid.tar.gz" dir exe
  [ -x "$appimage" ] || die "no AppImage"
  (cd "$work" && "$appimage" --appimage-extract >/dev/null)
  dir="$work/squashfs-root"
  [ -x "$dir/AppRun" ] && [ -f "$dir/$app_id.desktop" ] && [ -f "$dir/$app_id.png" ] || die "the AppImage lacks AppRun, its desktop entry or its icon"
  [ -x "$dir/usr/bin/UpdateNix" ] && [ -f "$dir/usr/bin/sq.version" ] || die "the AppImage lacks Velopack's updater or manifest"
  [ -f "$dir/usr/bin/runtimes/$rid/native/libhecdss.so" ] || die "no libhecdss.so in the AppImage"
  grep -q '^MimeType=application/x-bestfit;$' "$dir/$app_id.desktop" || die "the desktop entry doesn't open .bestfit files"
  if command -v desktop-file-validate >/dev/null 2>&1; then
    desktop-file-validate "$dir/$app_id.desktop"
    desktop-file-validate "$dir/usr/share/applications/$app_id.desktop"
  fi
  grep -q '<glob pattern="\*.bestfit"/>' "$dir/usr/share/mime/packages/$app_id.xml" || die "no .bestfit MIME type"
  ok "AppImage: AppRun, desktop entry (application/x-bestfit), MIME type, $(ls "$dir"/usr/share/icons/hicolor | wc -l | tr -d ' ') icon sizes, UpdateNix, libhecdss.so"

  # The desktop entry, installed in the work folder as the AppImage installs it for a user.
  local data="$work/xdg"
  XDG_DATA_HOME="$data" APPIMAGE="$appimage" "$dir/AppRun" --install-desktop-entry >/dev/null
  grep -q "^Exec=\"$appimage\" %f\$" "$data/applications/$app_id.desktop" || die "the installed desktop entry doesn't start the AppImage"
  [ -f "$data/mime/packages/$app_id.xml" ] && [ -f "$data/icons/hicolor/256x256/apps/$app_id.png" ] || die "the MIME type or icons weren't installed"
  if [ -f "$data/mime/globs2" ]; then
    grep -q ':application/x-bestfit:\*\.bestfit' "$data/mime/globs2" || die "update-mime-database didn't register *.bestfit"
  fi
  if command -v gio >/dev/null 2>&1; then
    XDG_DATA_HOME="$data" gio mime application/x-bestfit | grep "$app_id.desktop" >/dev/null ||
      die "gio doesn't list $product for .bestfit files"
  fi
  XDG_DATA_HOME="$data" "$dir/AppRun" --remove-desktop-entry >/dev/null
  [ ! -f "$data/applications/$app_id.desktop" ] || die "--remove-desktop-entry left the desktop entry"
  ok "--install-desktop-entry registers the AppImage for .bestfit files; --remove-desktop-entry removes it"

  # The AppImage itself, extracted and run by its own runtime (no FUSE needed).
  local sandbox=""
  if command -v bwrap >/dev/null 2>&1 && bwrap --bind / / --dev /dev --proc /proc true 2>/dev/null; then
    mkdir -p "$work/var-tmp" "$work/tmp"
    sandbox="bwrap --bind / / --bind $work/var-tmp /var/tmp --bind $work/tmp /tmp --dev /dev --proc /proc --"
  fi
  # $sandbox holds no spaces besides the separators (the work folder has none), so word splitting is wanted.
  run_headless $sandbox "$appimage" --appimage-extract-and-run
  [ -z "$sandbox" ] || [ -d "$work/var-tmp/velopack/$app_id" ] || die "Velopack didn't find the AppImage installed"

  [ -f "$tarball" ] || die "no tarball"
  tar -C "$work" -xzf "$tarball"
  dir="$work/$product-$version-$rid"
  exe="$(sed -n 's/^Exec=\([^ ]*\).*/\1/p' "$dir/share/applications/$app_id.desktop")"
  [ -x "$dir/bin/$exe" ] && [ -x "$dir/desktop-integration.sh" ] && [ -f "$dir/share/mime/packages/$app_id.xml" ] ||
    die "the tarball lacks the application, desktop-integration.sh or the MIME type"
  [ -f "$dir/bin/runtimes/$rid/native/libhecdss.so" ] || die "no libhecdss.so in the tarball"
  ok "tarball: bin/$exe, share/ (desktop entry, MIME type, icons), desktop-integration.sh"
  work="$work/tarball-run"
  run_headless "$dir/bin/$exe"
}

for rid in $rids; do
  out="$root/artifacts/packages/$rid"
  work="$root/artifacts/package-check/$rid"
  [ -d "$out" ] || die "no packages for $rid in $out: run scripts/package.sh $rid"
  echo "==> $rid ($out)"
  rm -rf "$work"
  mkdir -p "$work"
  case "${rid%%-*}" in
    win) check_windows ;;
    osx) check_macos ;;
    linux) check_linux ;;
    *) die "unsupported runtime: $rid" ;;
  esac
done
echo "Packages check out: $rids"
