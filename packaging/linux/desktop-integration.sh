#!/bin/sh
# XGoodFit addition: registers XGoodFit with the desktop for the current user, or removes it again:
# its menu entry and icons, and the BestFit project file type (.bestfit), with XGoodFit among the
# applications that open it. It never makes XGoodFit the default for .bestfit files: the desktop's
# own settings decide that (mimeapps.list, which this doesn't write).
#
# Usage: desktop-integration.sh --install-desktop-entry|--remove-desktop-entry [--exec <program>]
#   --exec   the program the menu entry starts (default: the application in bin/ beside this
#            script). The AppImage gives its own path (AppRun).
# Files go to $XDG_DATA_HOME (default ~/.local/share): applications/, mime/packages/ and
# icons/hicolor/. Nothing needs root.
set -eu

here="$(cd "$(dirname "$0")" && pwd)"
app_id="@APP_ID@"
action="${1:-}"
exec_path="$here/bin/@EXECUTABLE@"
if [ "${2:-}" = "--exec" ] && [ -n "${3:-}" ]; then exec_path="$3"; fi
case "$exec_path" in /*) ;; *) exec_path="$(pwd)/$exec_path" ;; esac
data="${XDG_DATA_HOME:-$HOME/.local/share}"

refresh() {
  if [ -d "$data/mime" ] && command -v update-mime-database >/dev/null 2>&1; then update-mime-database "$data/mime"; fi
  if [ -d "$data/applications" ] && command -v update-desktop-database >/dev/null 2>&1; then
    update-desktop-database "$data/applications" || true
  fi
}

case "$action" in
  --install-desktop-entry)
    # The desktop entry quotes the path, inside which these characters would need escaping.
    case "$exec_path" in
      *[\"\`\$\\%]*) echo "The program's path can't contain \", \`, \$, \\ or %: $exec_path" >&2; exit 1 ;;
    esac
    escaped="$(printf '%s' "$exec_path" | sed 's/[&|]/\\&/g')"
    mkdir -p "$data/applications" "$data/mime/packages"
    sed "s|^Exec=.*|Exec=\"$escaped\" %f|" "$here/share/applications/$app_id.desktop" > "$data/applications/$app_id.desktop"
    cp "$here/share/mime/packages/$app_id.xml" "$data/mime/packages/$app_id.xml"
    for icon in "$here"/share/icons/hicolor/*/apps/"$app_id".png; do
      size="$(basename "$(dirname "$(dirname "$icon")")")"
      mkdir -p "$data/icons/hicolor/$size/apps"
      cp "$icon" "$data/icons/hicolor/$size/apps/$app_id.png"
    done
    refresh
    echo "Installed the desktop entry for $exec_path in $data"
    ;;
  --remove-desktop-entry)
    rm -f "$data/applications/$app_id.desktop" "$data/mime/packages/$app_id.xml" "$data"/icons/hicolor/*/apps/"$app_id".png
    refresh
    echo "Removed the desktop entry from $data"
    ;;
  *)
    sed -n '2,11p' "$0" >&2
    exit 2
    ;;
esac
