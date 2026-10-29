#!/usr/bin/env bash
# Downloads the sample data pinned in sample-data.lock into sample-data/<name> (git-ignored):
# RMC-BestFit's example projects and HEC's sample DSS files, about 480 MB in total. Only the
# compatibility and HEC-DSS test suites need it; building XGoodFit doesn't.
#
# Uses shallow, blob-filtered, sparse clones, so re-running only fetches what changed.
#
# Usage: scripts/fetch-sample-data.sh [name...]   (default: every entry in sample-data.lock)
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
case "${1:-}" in
  -h|--help) sed -n '2,8p' "${BASH_SOURCE[0]}"; exit 0 ;;
esac
wanted=" $* "

# 'git sparse-checkout set --cone' needs git 2.35 or later.
read -r git_major git_minor <<< "$(git --version | sed -E 's/^git version ([0-9]+)\.([0-9]+).*/\1 \2/')"
if (( git_major < 2 || (git_major == 2 && git_minor < 35) )); then
  echo "fetch-sample-data.sh needs git 2.35 or later; found $(git --version)." >&2
  exit 1
fi

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line%$'\r'}"
  [[ -z "${line//[[:space:]]/}" || "$line" =~ ^[[:space:]]*# ]] && continue
  read -r name url commit folders <<< "$line"
  [[ $# -gt 0 && "$wanted" != *" $name "* ]] && continue
  dest="$root/sample-data/$name"
  echo "==> $name @ ${commit:0:7}"

  if [[ ! -d "$dest/.git" ]]; then
    git init -q "$dest"
    git -C "$dest" remote add origin "$url"
  fi
  git -C "$dest" config core.longpaths true

  if ! git -C "$dest" cat-file -e "$commit^{commit}" 2>/dev/null; then
    git -C "$dest" fetch -q --depth 1 --filter=blob:none origin "$commit"
  fi
  if [[ "$folders" == "*" ]]; then
    git -C "$dest" sparse-checkout disable
  else
    IFS=',' read -r -a dirs <<< "$folders"
    git -C "$dest" sparse-checkout set --cone "${dirs[@]}"
  fi
  git -C "$dest" -c advice.detachedHead=false checkout -q --detach "$commit"
done < "$root/sample-data.lock"

echo "Sample data is in $root/sample-data"
