# Gruetze RetroSaver development

- Shared .NET 10 / Avalonia 12 companion with C64/Amiga selection, live preview,
  app/company logos and access to existing monitor settings.
- Original Amiga Copper bars and editable sine scroller; text up to 4096
  characters with independent speed and wave-height controls.
- Normal launch and settings entry points open the companion; explicit
  fullscreen and Windows screensaver/preview arguments remain supported.
- Shared Amiga rendering is available to the native macOS core.
- Future public release preparation targets free Windows/Linux packages.
  Mac development builds remain possible; Store integration is pending.

# Changelog

## 1.1.0 — 2026-10-24

- External screensaver setup guidance; no built-in idle monitoring or autostart.
- Manual-launch Snap packaging draft; not yet built or Store-published.

- Stronger smooth CRT bloom with independent glow spread and a Glowing CRT preset.

## 1.0.0 — 2026-10-24

First release.

- C64 blue startup, blinking cursor, slow ghost typing, visible typo corrections and RUN.
- Real BASIC subset interpreter with maze, editable company card and twinkling stars.
- Timed program rotation and repeated code entry.
- Adjustable CRT effects, presets and live previews.
- Optional CRT power-off/on transition.
- Independent 4:3/stretch proportions and hidden C64 border.
- Windows screensaver switches and fullscreen desktop mode for Windows, Linux and macOS.
- C64 product icon and Grütze-Software About dialog.

### Platform status

Windows has been checked locally. Linux and macOS are cross-published and still
need native runtime testing. Native Wayland currently uses one display.
macOS is delivered as an unsigned fullscreen application, without a native
.saver bundle. Packages are unsigned and not notarized.
