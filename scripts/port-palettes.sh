#!/usr/bin/env bash
# Converts the WPF theme palettes (upstream/WPF-Framework/src/Themes/Resources/Colors/*.xaml) into
# Avalonia resource dictionaries (src/Framework/Themes/Resources/Colors/*.axaml). Re-run it whenever
# the vendored palettes change; Themes.Tests checks that every key matches upstream.
#
# The palettes only define colours and brushes, so two things change:
#   - the root XML namespace (WPF presentation -> Avalonia);
#   - gradient start and end points: WPF reads "0,1" as fractions of the element's bounds, while
#     Avalonia reads plain numbers as pixels, so they become percentages ("0%,100%").
set -euo pipefail

root="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
src="upstream/WPF-Framework/src/Themes/Resources/Colors"
dest="src/Framework/Themes/Resources/Colors"
mkdir -p "$root/$dest"

for name in LightColors DarkColors BlueColors; do
  {
    printf '<!-- Ported from %s/%s.xaml by scripts/port-palettes.sh. Do not edit by hand. -->\n' "$src" "$name"
    tr -d '\r' < "$root/$src/$name.xaml" | perl -pe '
      s{xmlns="http://schemas\.microsoft\.com/winfx/2006/xaml/presentation"}{xmlns="https://github.com/avaloniaui"};
      s{(StartPoint|EndPoint)="([0-9.]+),([0-9.]+)"}{sprintf("%s=\"%g%%,%g%%\"", $1, $2 * 100, $3 * 100)}ge;
    '
  } > "$root/$dest/$name.axaml"
  echo "$dest/$name.axaml"
done
