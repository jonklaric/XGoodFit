#!/usr/bin/env bash
# Copies the upstream source code pinned in upstream.lock into upstream/<name>, byte for byte,
# and stages the result. Run it after changing a pin in upstream.lock, then review the staged
# diff, port the upstream changes (docs/migration-plan.md §10) and commit.
#
# Usage: scripts/vendor-upstream.sh [name...]   (default: every entry in upstream.lock)
#
# Stops if git would store any vendored file differently from its upstream blob, for example by
# normalising line endings. Fix that with an attribute in upstream/.gitattributes, or exclude the
# file in upstream.lock.
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$root"

# 'git sparse-checkout set --cone' needs git 2.35 or later.
read -r git_major git_minor <<< "$(git --version | sed -E 's/^git version ([0-9]+)\.([0-9]+).*/\1 \2/')"
if (( git_major < 2 || (git_major == 2 && git_minor < 35) )); then
  echo "vendor-upstream.sh needs git 2.35 or later; found $(git --version)." >&2
  exit 1
fi

wanted=" $* "
work="$(mktemp -d)"
trap 'rm -rf "$work"' EXIT

while IFS= read -r line || [[ -n "$line" ]]; do
  line="${line%$'\r'}"
  [[ -z "${line//[[:space:]]/}" || "$line" =~ ^[[:space:]]*# ]] && continue
  read -r name url commit folders excluded <<< "$line"
  [[ $# -gt 0 && "$wanted" != *" $name "* ]] && continue
  echo "==> $name @ ${commit:0:7}"

  # Check out the pinned commit with raw blob contents: no line-ending conversion, keyword
  # expansion or filters, whatever the upstream .gitattributes say.
  src="$work/$name"
  git init -q "$src"
  git -C "$src" remote add origin "$url"
  git -C "$src" config core.autocrlf false
  git -C "$src" config core.longpaths true
  mkdir -p "$src/.git/info"
  printf '* -text -ident -filter\n' > "$src/.git/info/attributes"
  git -C "$src" fetch -q --depth 1 --filter=blob:none origin "$commit"
  if [[ "$folders" != "*" ]]; then
    IFS=',' read -r -a dirs <<< "$folders"
    git -C "$src" sparse-checkout set --cone "${dirs[@]}"
  fi
  git -C "$src" -c advice.detachedHead=false checkout -q --detach "$commit"
  # Remember which files upstream marks executable: copies made on Windows lose the bit.
  git -C "$src" -c core.quotePath=false ls-tree -r "$commit" | awk -F'\t' '$1 ~ /^100755 / { print $2 }' > "$work/$name.exec"
  rm -rf "$src/.git"

  if [[ "$excluded" != "-" ]]; then
    IFS=',' read -r -a patterns <<< "$excluded"
    for pattern in "${patterns[@]}"; do
      find "$src" -path "$src/$pattern" -prune -print | while IFS= read -r path; do rm -rf "$path"; done
    done
  fi

  # Replace the vendored copy and stage it, including files an upstream .gitignore would hide.
  dest="upstream/$name"
  rm -rf "$dest"
  mkdir -p "$dest"
  cp -R "$src/." "$dest/"
  git -c core.safecrlf=false add -A -f -- "$dest"
  while IFS= read -r path; do
    if [[ -e "$dest/$path" ]]; then git update-index --chmod=+x -- "$dest/$path"; fi
  done < "$work/$name.exec"

  # Every staged blob must hash the same as the raw upstream file.
  git -c core.quotePath=false ls-files -s -- "$dest" | awk -F'\t' '{ split($1, a, " "); print a[2] " " $2 }' | LC_ALL=C sort > "$work/$name.staged"
  git -c core.quotePath=false ls-files -- "$dest" > "$work/$name.files"
  git hash-object --no-filters --stdin-paths < "$work/$name.files" > "$work/$name.hashes"
  paste -d ' ' "$work/$name.hashes" "$work/$name.files" | LC_ALL=C sort > "$work/$name.raw"
  if ! diff -q "$work/$name.raw" "$work/$name.staged" > /dev/null; then
    echo "    ERROR: git would change these files from their upstream bytes:" >&2
    diff "$work/$name.raw" "$work/$name.staged" | sed -n 's/^< [0-9a-f]* /      /p' >&2
    exit 1
  fi

  # Re-create the working copy from the index, so line endings match a fresh clone.
  rm -rf "$dest"
  git checkout -- "$dest"
  count=$(wc -l < "$work/$name.files" | tr -d ' ')
  echo "    $count files, byte-identical to upstream"
done < "$root/upstream.lock"

echo "Staged. Review with 'git diff --cached --stat -- upstream', then commit."
