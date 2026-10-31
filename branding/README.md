# Branding

XGoodFit's logo and application icon. They are original artwork, under the repository's MIT licence.
RMC-BestFit's icon (two overlapping probability densities, shaded blue to red) inspired them; nothing
is traced from it. The X shows that XGoodFit is a separate, cross-platform port.

`tools/Branding` draws everything here in code: it renders each drawing with SkiaSharp, and writes the
same geometry out as SVG. To change the logo, edit `tools/Branding/Logos.cs`, then run

    dotnet run --project tools/Branding

which rewrites this folder.

## What's here

| File | What it is |
|---|---|
| `xgoodfit.svg` | The logo, 256 × 256 units: concept C |
| `XGoodFit.ico` | The application icon: PNG images of 16, 24, 32, 48, 64, 128 and 256 px (`XGoodFitIcon` in `Directory.Build.props`) |
| `xgoodfit-1024.png` | The icon at 1024 px, for macOS's 512 px @2x (`XGoodFitLargeIcon`) |
| `concepts/` | The three concepts, each as an SVG and a 512 px PNG |
| `previews/<concept>.png`, `previews/xgoodfit.png` | Each concept, and the icon as `XGoodFit.ico` holds it, at 256, 64, 32 and 16 px (and 16 px magnified 4 times), on a light and a dark background, beside RMC-BestFit's icon |
| `previews/xgoodfit-panels.png` | The icon at 16, 24, 32 and 48 px, beside RMC-BestFit's, on the colours of the Windows taskbar, the macOS Dock, and GNOME's and KDE Plasma's panels, light and dark |

## The concepts

| | Concept | The X |
|---|---|---|
| A | Crossed tails (`a-crossed-tails`) | Two skewed densities, mirrored: their long tails cross into the X. On a transparent background, as RMC-BestFit's icon is. |
| B | X behind the curves (`b-x-behind`) | A bold X stands behind RMC-BestFit's pair of densities, on a dark tile. |
| C | **Exceedance X** (`c-exceedance-x`), the logo | A distribution's non-exceedance curve (its CDF, rising) and its exceedance curve (falling) cross into the X, over RMC-BestFit's pair of densities, on a light tile. |

C is the logo for these reasons:
- Its X is the most legible of the three: a dark X on a white tile, even at 16 px.
- It keeps the most of RMC-BestFit's icon (the lower density on the left, the taller one on the right,
  the blue-to-red gradient), yet nobody would take it for RMC-BestFit's: a white tile with a bold X.
- Its X means something: exceedance probability is what flood-frequency analysis estimates.
- The tile carries its own background, so the icon looks the same on light and dark taskbars, Docks
  and panels. A rounded square is also the shape macOS expects of an application icon.

Colours: the densities run from blue `#2F6BEA` through violet `#7C4DEB` to rose `#E83A62`. The X is
ink `#1E1B4B`. The tile shades from white to `#ECEAFB`, with a faint ink edge.

## How the sizes are made

- From 48 px up, every size is the same drawing (`xgoodfit.svg`'s), rendered at that size.
- Below 48 px, the drawing is made bolder so it stays legible:
  - the X's strokes are at least 2 px wide;
  - the gap where they cross is at least 1 px wide, and at 16 px there's none, so the X stays whole;
  - the tile's margin is a whole number of pixels;
  - the tile's edge is at least 0.375 px wide.
- `XGoodFit.ico` holds the sizes as PNG images, which Windows (Vista and later), Avalonia and
  `tools/AppIcons` all read. `xgoodfit-1024.png` is the same drawing at 1024 px.
- `scripts/package.sh` runs `tools/AppIcons` to make macOS's `.icns` (16 to 256 px from the `.ico`,
  512 and 1024 px from `xgoodfit-1024.png`) and the Linux PNGs (16 to 256 px). Windows uses the
  `.ico` as it is. [packaging/README.md](../packaging/README.md) has the details.
