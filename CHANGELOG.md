# Changelog

## 1.1.0-preview.1 — in development

- Stronger smooth CRT bloom with independent glow spread and a Glowing CRT preset.
- Opt-in Linux idle helper, login autostart, wait time and desktop diagnostics.
- GNOME X11/Wayland and Cinnamon/MATE/Xfce X11 adapters, pending native desktop validation.
- Lock and presentation-inhibitor checks; pause activation when information is unavailable.
- Strict Snap packaging draft and documented inhibitor-access blocker; not Store-ready.

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
