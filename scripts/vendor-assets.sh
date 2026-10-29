#!/usr/bin/env bash
# Downloads the release assets pinned in upstream-assets.lock into upstream/<name> and stages them.
# Every download must match its pinned SHA-256 before anything is extracted. Run it after changing
# upstream-assets.lock, then review and commit. (Source code in git is vendored by vendor-upstream.sh.)
#
# Usage: scripts/vendor-assets.sh
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

sha256() {
  if command -v sha256sum > /dev/null; then sha256sum "$1" | cut -d ' ' -f 1; else shasum -a 256 "$1" | cut -d ' ' -f 1; fi
}

work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT
started=" "

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line%$'\r'}"
  [[ -z "${line//[[:space:]]/}" || "$line" =~ ^[[:space:]]*# ]] && continue
  read -r name url sha files <<< "$line"
  dest="upstream/$name"

  # Each folder is rebuilt from scratch the first time it appears in the lock.
  if [[ "$started" != *" $name "* ]]; then
    rm -rf "$dest"
    mkdir -p "$dest"
    started+="$name "
  fi

  download="$work/$(basename "$url")"
  echo "==> $name: $(basename "$url")"
  curl -fsSL -o "$download" "$url"
  actual="$(sha256 "$download")"
  if [[ "$actual" != "$sha" ]]; then
    echo "    ERROR: SHA-256 mismatch for $url (got $actual)" >&2
    exit 1
  fi

  if [[ "$files" == "-" ]]; then
    cp "$download" "$dest/"
  else
    IFS=',' read -r -a members <<< "$files"
    unzip -q -o -j "$download" "${members[@]}" -d "$dest"
  fi
done < "$root/upstream-assets.lock"

for name in $started; do
  git add -A -f -- "upstream/$name"
  echo "    upstream/$name: $(git ls-files -- "upstream/$name" | wc -l | tr -d ' ') files"
done
echo "Staged. Review with 'git diff --cached --stat -- upstream', then commit."
