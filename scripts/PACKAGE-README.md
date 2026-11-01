# Gruetze RetroSaver @VERSION@

Package: @RUNTIME@. C64 BASIC and Amiga demos with configurable CRT effects.
The .NET runtime is included; no separate .NET installation is required.
Extract the entire ZIP and keep its files together.

## Start

Windows: open `c64screensaver.exe`. To start the animation directly, use
`c64screensaver.exe --fullscreen`.

Linux: run `chmod +x c64screensaver`, then `./c64screensaver`.
Start the animation with `./c64screensaver --fullscreen`.
A graphical desktop and the native libraries required by Avalonia/Skia are needed.
Use `--x11` or `--wayland` to select the backend. Wayland currently uses one display.

The executable retains its historical name for compatibility.
Normal launch opens the companion. Select C64 or Amiga, edit playback/text,
open Monitor effects to adjust the CRT and save your settings.
Fullscreen exits on mouse movement, click or keypress after a short launch grace
period. Escape closes previews; F toggles frame statistics.
Additional commands: `--settings`, `--windowed`, `--about`.

## Set up as a screensaver

Windows: right-click `c64screensaver.scr` for installation/configuration.
Keep it beside the executable and every included dependency; do not copy it alone
into a system folder. Standard `/s`, `/c` and `/p HWND` switches are supported.

Linux: automatic activation depends on the desktop environment.
See [the setup guide](docs/SCREEN-SAVER-SETUP.md). The application does not install
an idle helper or login autostart entry and does not replace the password lock.

Mac binaries are distributed separately and are not part of this package.

## Features and support

C64: ghost-typed BASIC, corrections, maze, company card and stars.
Amiga: historical Kickstart startup images, autoplay Jump & run, Plasma and
editable sine-wave scrolling text. Shared CRT presets, glow/spread, scanlines,
mask, curvature, softness and optional CRT power-off/on transitions.

- [Full documentation and screenshots](https://github.com/gruetze-software/C64Screensaver#readme)
- [Release downloads and checksums](https://github.com/gruetze-software/C64Screensaver/releases/tag/v@VERSION@)
- [Report a problem](https://github.com/gruetze-software/C64Screensaver/issues)
- [Grütze-Software](https://www.gruetze-software.de)

MIT license; see [LICENSE](LICENSE) and [CHANGELOG.md](CHANGELOG.md).
Historical artwork has separate [Kickstart credits](docs/KICKSTART-CREDITS.md).
Independent project, not affiliated with Commodore.
