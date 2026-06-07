# Assets

## App icon

`Snipdeck.ico` is the multi-size app icon (16/20/24/32/48/64/128/256, the 256
entry PNG-compressed), rendered from `designs/snipdeck-icon.svg`. It is embedded
in the exe (`<ApplicationIcon>`), set on the window (`AppWindow.SetIcon`), used
by the tray icon and passed to `vpk pack --icon` for the installer.

To regenerate after the SVG changes (the glyph is 430×507, so each render is
height-fitted and centred on a square page):

```sh
for s in 16 20 24 32 48 64 128 256; do
  left=$(awk "BEGIN{printf \"%.2f\", ($s-430/507*$s)/2}")
  rsvg-convert -h $s --page-width $s --page-height $s --left $left --top 0 \
    designs/snipdeck-icon.svg -o glyph-$s.png
done
icotool -c -o src/Snipdeck.App/Assets/Snipdeck.ico \
  glyph-16.png glyph-20.png glyph-24.png glyph-32.png glyph-48.png \
  glyph-64.png glyph-128.png -r glyph-256.png
```

## Home hero images (theme-specific)

The Home page hero banner uses a theme-specific image:

- `HomeHeroLight.png` — shown in the Light theme
- `HomeHeroDark.png` — shown in the Dark (and High Contrast) theme

Drop both here (≈2400×1000, wide) and the header swaps them with the selected
theme. Keep the **upper-left area light/clear** in the light image (and suitably
contrasted in the dark image) — the title text uses the theme foreground colour
and sits top-left. The image's lower edge fades into the header background.

Until the files are present, the header shows its background colour. See the
Firefly prompt in the project history for generating them.
