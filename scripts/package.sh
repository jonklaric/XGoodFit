#!/usr/bin/env bash
# Builds XGoodFit's installable, self-updating packages with Velopack (decision D7; migration plan
# Phase 8 and §9). For each runtime it builds the HEC-DSS natives, publishes the application
# self-contained with them (not trimmed: the application relies on reflection), and packs it with
# Velopack's vpk tool (.config/dotnet-tools.json):
#   win-x64    Setup.exe, a portable zip
#   osx-*      an .app bundle (Info.plist with the .bestfit type, hardened runtime) as a .pkg
#              installer and a portable zip, signed ad hoc unless Developer ID signing is set up
#   linux-x64  an AppImage and a tarball, each with a desktop entry, the .bestfit MIME type and icons
# packaging/README.md describes the packages, the signing and the releases.
#
# Usage: scripts/package.sh [options] [rid...]
#   rid                  win-x64, osx-arm64, osx-x64 or linux-x64 (default: this machine's). Each OS
#                        packs its own runtimes; a Mac packs both of its.
#   --app <project>      the application to package (default: APP_PROJECT below)
#   --version <version>  the version, SemVer 2 (default: Version in Directory.Build.props)
#   --delta-from <dir>   a folder with the previous release's full package for each runtime, in
#                        <dir>/<rid> (as `dotnet vpk download github` leaves it), for delta packages
#   --release-notes <md> release notes (Markdown) for the packages
#   --skip-natives       don't build the HEC-DSS natives: use the ones in artifacts/native
# Output: artifacts/packages/<rid>/: Velopack's release files for channel <rid> (installers, packages,
#         releases.<rid>.json), and the tarball on Linux. Build files: artifacts/package-build/<rid>/.
# macOS signing, from the environment (without it the .app is signed ad hoc; packaging/README.md):
#   XGF_MAC_SIGN_APP_IDENTITY      "Developer ID Application: …": signs the .app
#   XGF_MAC_SIGN_INSTALL_IDENTITY  "Developer ID Installer: …": signs the .pkg
#   XGF_MAC_NOTARY_PROFILE         a notarytool keychain profile: notarizes and staples both
#   XGF_MAC_KEYCHAIN               the keychain that holds them
set -euo pipefail

# The application to package. RMC.BestFit.App isn't ported yet, so the theme gallery stands in for
# it; switch to src/BestFit/RMC.BestFit.App/RMC.BestFit.App.csproj when it is.
APP_PROJECT="tools/ThemeGallery/ThemeGallery.csproj"

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
start_dir="$(pwd)"

die() { echo "package.sh: $*" >&2; exit 1; }

# Options' paths are relative to the folder the script runs from; the script works in the repository.
absolute() {
  case "$1" in
    /*|[A-Za-z]:*) printf '%s\n' "$1" ;;
    *) printf '%s\n' "$start_dir/$1" ;;
  esac
}

app="$APP_PROJECT"
version=""
delta_from=""
release_notes=""
skip_natives=0
rids=""
while [ $# -gt 0 ]; do
  case "$1" in
    -h|--help) sed -n '2,27p' "${BASH_SOURCE[0]}"; exit 0 ;;
    --app) app="$(absolute "${2:?--app needs a project}")"; shift ;;
    --version) version="${2:?--version needs a version}"; shift ;;
    --delta-from) delta_from="$(absolute "${2:?--delta-from needs a folder}")"; shift ;;
    --release-notes) release_notes="$(absolute "${2:?--release-notes needs a file}")"; shift ;;
    --skip-natives) skip_natives=1 ;;
    -*) die "unknown option: $1" ;;
    *) rids="$rids $1" ;;
  esac
  shift
done

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
[ -n "$rids" ] || rids="$host_os-$host_arch"
for rid in $rids; do
  case "$rid" in
    win-x64|osx-arm64|osx-x64|linux-x64) ;;
    *) die "unsupported runtime: $rid (win-x64, osx-arm64, osx-x64 or linux-x64)" ;;
  esac
  # Each OS's tools make its own packages (macOS's codesign and pkgbuild, for instance), and the
  # natives are built for the OS they run on.
  [ "${rid%%-*}" = "$host_os" ] || die "$rid packages are made on ${rid%%-*} (this is $host_os)."
done
cd "$root"
[ -f "$app" ] || die "no application project at $app"
[ -z "$release_notes" ] || [ -f "$release_notes" ] || die "no release notes at $release_notes"

# Paths handed to Windows programs (dotnet.exe) must be Windows paths.
native_path() {
  if [ "$host_os" = win ]; then cygpath -m "$1"; else printf '%s\n' "$1"; fi
}

# Reads properties of the application project (with Directory.Build.props) into prop_<name>.
read_properties() {
  local json name value args=""
  for name in "$@"; do args="$args -getProperty:$name"; done
  # $args holds no spaces besides the separators, so word splitting is wanted.
  json="$(dotnet msbuild "$(native_path "$app")" $args | tr -d '\r')"
  for name in "$@"; do
    value="$(printf '%s\n' "$json" | sed -n "s/^ *\"$name\": \"\(.*\)\",\{0,1\}\$/\1/p" | sed -e 's/\\"/"/g' -e 's/\\\\/\\/g')"
    eval "prop_$name=\$value"
  done
}

echo "==> Reading $app"
read_properties Version AssemblyName XGoodFitProductName XGoodFitAppId XGoodFitIcon Company Copyright VelopackVersion
version="${version:-$prop_Version}"
product="$prop_XGoodFitProductName"
app_id="$prop_XGoodFitAppId"
assembly_name="$prop_AssemblyName"
icon="$prop_XGoodFitIcon"
[ "$host_os" != win ] || icon="$(cygpath -u "$icon")"
for value in "$version" "$product" "$app_id" "$assembly_name" "$icon"; do
  [ -n "$value" ] || die "a branding property is empty: check Directory.Build.props and $app"
done
[ -f "$icon" ] || die "no icon at $icon (XGoodFitIcon)"
case "$product$prop_Copyright$prop_Company" in *[\<\>\&\|\\]*) die "the product name, copyright and company can't contain < > & | or \\" ;; esac
# MAJOR.MINOR.PATCH, for macOS's bundle versions.
short_version="$(printf '%s\n' "$version" | sed -n 's/^\([0-9][0-9]*\.[0-9][0-9]*\.[0-9][0-9]*\).*$/\1/p')"
[ -n "$short_version" ] || die "the version must be SemVer 2 (MAJOR.MINOR.PATCH[-PRERELEASE]): $version"

# vpk must be the Velopack version the application is built with (Directory.Packages.props).
dotnet tool restore >/dev/null
vpk_version="$(tr -d '\r' < .config/dotnet-tools.json | sed -n '/"vpk"/,/}/s/^ *"version": *"\([^"]*\)".*$/\1/p')"
[ "$vpk_version" = "$prop_VelopackVersion" ] ||
  die "vpk $vpk_version (.config/dotnet-tools.json) isn't Velopack $prop_VelopackVersion (Directory.Packages.props): pin them together"
echo "$product $version ($app_id), $(basename "$app" .csproj) as $assembly_name, Velopack $vpk_version"

# vpk pack, with the options every OS's packages have, then the OS's own. Each runtime is a channel
# of its own, so a copy updates from its own runtime's packages.
vpk_pack() {
  set -- --packId "$app_id" --packVersion "$version" --packTitle "$product" --packAuthors "$prop_Company" \
    --channel "$rid" --runtime "$rid" --outputDir "$(native_path "$out")" "$@"
  [ -z "$release_notes" ] || set -- "$@" --releaseNotes "$(native_path "$release_notes")"
  dotnet vpk pack "$@" --skip-updates
}

sed_escape() { printf '%s' "$1" | sed -e 's/[&|\\]/\\&/g'; }

# Fills in a template from packaging/: its @NAME@ placeholders, from the variables set for the runtime.
fill_template() {
  tr -d '\r' < "$1" | sed \
    -e "s|@NAME@|$(sed_escape "$product")|g" \
    -e "s|@APP_ID@|$(sed_escape "$app_id")|g" \
    -e "s|@EXECUTABLE@|$(sed_escape "$assembly_name")|g" \
    -e "s|@EXEC@|$(sed_escape "${template_exec:-}")|g" \
    -e "s|@VERSION@|$(sed_escape "$version")|g" \
    -e "s|@SHORT_VERSION@|$(sed_escape "$short_version")|g" \
    -e "s|@ICON@|$(sed_escape "${template_icon:-}")|g" \
    -e "s|@COPYRIGHT@|$(sed_escape "$prop_Copyright")|g" \
    -e "s|@MINIMUM_MACOS@|$(sed_escape "${template_minimum_macos:-}")|g" > "$2"
  if grep -q '@[A-Z_][A-Z_]*@' "$2"; then die "$1 has a placeholder package.sh doesn't fill in"; fi
}

# The icons macOS and Linux need, rendered from the .ico (tools/AppIcons): app.icns and <size>.png.
icons=""
render_icons() {
  [ -z "$icons" ] || return 0
  icons="$root/artifacts/package-build/icons"
  rm -rf "$icons"
  dotnet run --project tools/AppIcons/AppIcons.csproj -c Release -- "$(native_path "$icon")" "$(native_path "$icons")"
}

# The newest minimum macOS version among the application's binaries: the bundle's minimum.
minimum_macos() {
  local file found newest=""
  for file in "$1"/*; do
    [ -f "$file" ] || continue
    found="$(otool -l "$file" 2>/dev/null | awk '
      $1 == "cmd" { cmd = $2 }
      cmd == "LC_BUILD_VERSION" && $1 == "minos" { print $2; exit }
      cmd == "LC_VERSION_MIN_MACOSX" && $1 == "version" { print $2; exit }')" || true
    [ -n "$found" ] || continue
    newest="$(printf '%s\n%s\n' "$newest" "$found" | sed '/^$/d' | sort -t. -k1,1n -k2,2n -k3,3n | tail -n 1)"
  done
  [ -n "$newest" ] || die "no macOS binaries in $1"
  printf '%s\n' "$newest"
}

pack_windows() {
  local exe="$assembly_name.exe"
  vpk_pack --packDir "$(native_path "$publish")" --mainExe "$exe" --icon "$(native_path "$icon")"
}

pack_macos() {
  local exe="$assembly_name" plist="$stage/Info.plist"
  render_icons
  # Velopack copies the icon into the bundle's Resources under its file name.
  cp "$icons/app.icns" "$stage/$product.icns"
  template_icon="$product.icns"
  template_minimum_macos="$(minimum_macos "$publish")"
  fill_template packaging/macos/Info.plist "$plist"
  plutil -lint "$plist" >/dev/null || die "$plist isn't a valid property list"

  # Signed with the Developer ID when it's set up, else ad hoc ("-"); either way with the hardened
  # runtime and .NET's entitlements (packaging/macos/XGoodFit.entitlements).
  local identity="${XGF_MAC_SIGN_APP_IDENTITY:-}"
  set -- --signAppIdentity "${identity:--}" --signEntitlements "$root/packaging/macos/XGoodFit.entitlements"
  [ -z "${XGF_MAC_SIGN_INSTALL_IDENTITY:-}" ] || set -- "$@" --signInstallIdentity "$XGF_MAC_SIGN_INSTALL_IDENTITY"
  [ -z "${XGF_MAC_NOTARY_PROFILE:-}" ] || set -- "$@" --notaryProfile "$XGF_MAC_NOTARY_PROFILE"
  [ -z "${XGF_MAC_KEYCHAIN:-}" ] || set -- "$@" --keychain "$XGF_MAC_KEYCHAIN"
  if [ -n "$identity" ]; then echo "Signing with $identity"; else echo "Signing ad hoc (no Developer ID set up)"; fi
  if [ -n "$identity" ] && [ -z "${XGF_MAC_NOTARY_PROFILE:-}" ]; then echo "Not notarizing (XGF_MAC_NOTARY_PROFILE isn't set)"; fi

  vpk_pack --packDir "$publish" --mainExe "$exe" --icon "$stage/$product.icns" --plist "$plist" "$@"
}

pack_linux() {
  local exe="$assembly_name" appdir="$stage/$product.AppDir" size
  render_icons

  # The AppImage's folder: AppRun and the desktop entry at the top, as the AppImage format has them,
  # the application in usr/bin (where Velopack adds its updater), and the desktop files in usr/share.
  mkdir -p "$appdir/usr/bin" "$appdir/usr/share/applications" "$appdir/usr/share/mime/packages"
  cp -R "$publish/." "$appdir/usr/bin/"
  find "$appdir/usr/bin" -name '*.pdb' -exec rm -f {} +
  fill_template packaging/linux/AppRun "$appdir/AppRun"
  template_exec="$exe"
  fill_template packaging/linux/app.desktop "$appdir/usr/share/applications/$app_id.desktop"
  cp "$appdir/usr/share/applications/$app_id.desktop" "$appdir/$app_id.desktop"
  fill_template packaging/linux/mime.xml "$appdir/usr/share/mime/packages/$app_id.xml"
  fill_template packaging/linux/desktop-integration.sh "$appdir/usr/desktop-integration.sh"
  chmod +x "$appdir/AppRun" "$appdir/usr/desktop-integration.sh" "$appdir/usr/bin/$exe"
  for size in 16 24 32 48 64 128 256; do
    [ -f "$icons/$size.png" ] || continue
    mkdir -p "$appdir/usr/share/icons/hicolor/${size}x$size/apps"
    cp "$icons/$size.png" "$appdir/usr/share/icons/hicolor/${size}x$size/apps/$app_id.png"
  done
  cp "$icons/256.png" "$appdir/$app_id.png"
  cp "$icons/256.png" "$appdir/.DirIcon"
  if command -v desktop-file-validate >/dev/null 2>&1; then desktop-file-validate "$appdir/$app_id.desktop"; fi

  vpk_pack --packDir "$appdir" --mainExe "$exe"

  # The tarball: the same files, with the application in bin/ and desktop-integration.sh at the top.
  local name="$product-$version-$rid" tree="$stage/tarball"
  mkdir -p "$tree/$name"
  cp -R "$appdir/usr/bin" "$tree/$name/bin"
  cp -R "$appdir/usr/share" "$tree/$name/share"
  cp "$appdir/usr/desktop-integration.sh" "$tree/$name/desktop-integration.sh"
  # Velopack's updater updates the AppImage only.
  rm -f "$tree/$name/bin/UpdateNix" "$tree/$name/bin/sq.version"
  tar -C "$tree" -czf "$out/$name.tar.gz" "$name"
}

if [ "$skip_natives" = 0 ]; then
  echo "==> HEC-DSS natives"
  # $rids holds no spaces besides the separators, so word splitting is wanted.
  bash native/build-hecdss.sh $rids
fi

for rid in $rids; do
  os="${rid%%-*}"
  stage="$root/artifacts/package-build/$rid"
  out="$root/artifacts/packages/$rid"
  publish="$stage/publish"
  template_exec="" template_icon="" template_minimum_macos=""
  echo "==> $rid"
  rm -rf "$stage" "$out"
  mkdir -p "$stage" "$out"

  case "$os" in
    win) library=hecdss.dll ;;
    osx) library=libhecdss.dylib ;;
    *) library=libhecdss.so ;;
  esac
  native="artifacts/native/runtimes/$rid/native/$library"
  [ -f "$native" ] || die "no HEC-DSS native library for $rid ($native): run native/build-hecdss.sh $rid"
  # Only this runtime's library: src/Hec.Dss ships every one under HecDssNativeRoot.
  mkdir -p "$stage/natives/runtimes/$rid/native"
  cp "$native" "$stage/natives/runtimes/$rid/native/"

  dotnet publish "$(native_path "$app")" -c Release -r "$rid" --self-contained -p:PublishTrimmed=false \
    -p:PublishDocumentationFiles=false -p:Version="$version" -p:HecDssNativeRoot="$(native_path "$stage/natives")/" \
    -o "$(native_path "$publish")"
  [ -f "$publish/runtimes/$rid/native/$library" ] || die "the published application has no HEC-DSS library: does it reference src/Hec.Dss?"

  # vpk makes a delta package from the newest earlier full package in its output folder.
  if [ -n "$delta_from" ]; then
    if ls "$delta_from/$rid"/*.nupkg >/dev/null 2>&1; then
      cp "$delta_from/$rid"/*.nupkg "$out/"
    else
      echo "No earlier package in $delta_from/$rid: no delta package."
    fi
  fi

  case "$os" in
    win) pack_windows ;;
    osx) pack_macos ;;
    linux) pack_linux ;;
  esac
  echo "Packages for $rid in $out:"
  ls -l "$out"
done
