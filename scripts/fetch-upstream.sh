#!/usr/bin/env bash
# Fetches the pinned upstream sources listed in upstream.lock into upstream/<name>.
#
# Uses shallow, blob-filtered, sparse clones so only the folders the port needs are
# downloaded. RMC-BestFit's full history is over 1 GB, mostly verification evidence.
#
# Usage: scripts/fetch-upstream.sh [--with-examples]
#   --with-examples  also fetch each repo's optional paths (RMC-BestFit's ~455 MB of
#                    example projects, used by the file-compatibility and parity suites)
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
with_optional=false
for arg in "$@"; do
  case "$arg" in
    --with-examples) with_optional=true ;;
    -h|--help) sed -n '2,10p' "${BASH_SOURCE[0]}"; exit 0 ;;
    *) echo "Unknown option: $arg" >&2; exit 2 ;;
  esac
done

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line%$'\r'}"
  [[ -z "${line//[[:space:]]/}" || "$line" =~ ^[[:space:]]*# ]] && continue
  read -r name url commit paths optional <<< "$line"
  dest="$root/upstream/$name"
  echo "==> $name @ ${commit:0:7}"

  if [[ ! -d "$dest/.git" ]]; then
    git init -q "$dest"
    git -C "$dest" remote add origin "$url"
  fi
  git -C "$dest" config core.longpaths true

  sparse=()
  if [[ "$paths" != "*" ]]; then
    IFS=',' read -r -a sparse <<< "$paths"
  fi
  if [[ "$with_optional" == true && -n "${optional:-}" && "$optional" != "-" ]]; then
    IFS=',' read -r -a extra <<< "$optional"
    sparse+=("${extra[@]}")
  fi

  if ! git -C "$dest" cat-file -e "$commit^{commit}" 2>/dev/null; then
    git -C "$dest" fetch -q --depth 1 --filter=blob:none origin "$commit"
  fi
  if ((${#sparse[@]})); then
    git -C "$dest" sparse-checkout set --cone "${sparse[@]}"
  else
    git -C "$dest" sparse-checkout disable
  fi
  git -C "$dest" -c advice.detachedHead=false checkout -q --detach "$commit"
done < "$root/upstream.lock"

echo "Upstream sources are in $root/upstream"
