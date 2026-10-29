#!/usr/bin/env bash
# Fetches the pinned upstream sources listed in upstream.lock into upstream/<name>.
#
# Uses shallow, blob-filtered, sparse clones so only the folders the port needs are
# downloaded. RMC-BestFit's full history is over 1 GB, mostly verification evidence.
#
# Usage: scripts/fetch-upstream.sh [--with-examples]
#   --with-examples  also fetch the optional sample data used by the file-compatibility,
#                    parity and HEC-DSS suites (about 480 MB to download): RMC-BestFit's
#                    example projects and HEC's sample DSS files
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

# 'git sparse-checkout set --cone' needs git 2.35 or later.
read -r git_major git_minor <<< "$(git --version | sed -E 's/^git version ([0-9]+)\.([0-9]+).*/\1 \2/')"
if (( git_major < 2 || (git_major == 2 && git_minor < 35) )); then
  echo "fetch-upstream.sh needs git 2.35 or later; found $(git --version)." >&2
  exit 1
fi

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line%$'\r'}"
  [[ -z "${line//[[:space:]]/}" || "$line" =~ ^[[:space:]]*# ]] && continue
  read -r name url commit paths optional <<< "$line"
  dest="$root/upstream/$name"

  # Folders to check out; '*' means the whole tree and '-' means none.
  whole=false
  sparse=()
  case "$paths" in
    '*') whole=true ;;
    '-') ;;
    *) IFS=',' read -r -a sparse <<< "$paths" ;;
  esac
  if [[ "$with_optional" == true ]]; then
    case "${optional:--}" in
      '*') whole=true ;;
      '-') ;;
      *) IFS=',' read -r -a extra <<< "$optional"; sparse+=("${extra[@]}") ;;
    esac
  fi
  if [[ "$whole" == false && ${#sparse[@]} -eq 0 ]]; then
    echo "==> $name: optional, skipped (fetched with --with-examples)"
    continue
  fi
  echo "==> $name @ ${commit:0:7}"

  if [[ ! -d "$dest/.git" ]]; then
    git init -q "$dest"
    git -C "$dest" remote add origin "$url"
  fi
  git -C "$dest" config core.longpaths true

  if ! git -C "$dest" cat-file -e "$commit^{commit}" 2>/dev/null; then
    git -C "$dest" fetch -q --depth 1 --filter=blob:none origin "$commit"
  fi
  if [[ "$whole" == true ]]; then
    git -C "$dest" sparse-checkout disable
  else
    git -C "$dest" sparse-checkout set --cone "${sparse[@]}"
  fi
  git -C "$dest" -c advice.detachedHead=false checkout -q --detach "$commit"
done < "$root/upstream.lock"

# Lets the build detect a missing or out-of-date fetch (build/CheckUpstream.targets).
mkdir -p "$root/upstream"
cp "$root/upstream.lock" "$root/upstream/.fetched.lock"
echo "Upstream sources are in $root/upstream"
