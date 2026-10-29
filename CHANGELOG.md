# Changelog

## 1.2.1 — 2026-10-29

- Distribute one Mac DMG with the native screensaver embedded inside the companion.
- Install or update the system screensaver using a button in the companion.
- Use a shared per-user settings file with atomic writes; migrate existing desktop/native
  preferences when no shared file exists. Retain the last good settings on malformed saves.
- Automatically reload native playback after external saves and refresh the companion
  when returning from native options. Preserve settings not exposed by the native dialog.
- Add cross-process Mac integration tests and mounted-DMG signature/content checks.
- Extend Developer ID signing/notarization to the complete app and embedded screensaver.

## 1.2.0 — 2026-10-29

- Introduce the Gruetze RetroSaver companion for C64 and Amiga.
- Keep only Jump & run and Plasma in the Amiga selection; alternate both or repeat one.
  Retired saved demo selections fall back to alternating. C64 BASIC programs are unchanged.
- Add layered woodland parallax, animated explorer, platform physics and crystals.
- Add optional editable scrolling text, historical Kickstart captures and disk reflection.
- Improve preview pacing, cache drawing resources and bound CPU CRT rendering costs.
- Add native macOS C64/Amiga selection and Amiga startup, demo and scroller options.
- Build native screensaver and companion test bundles for Apple Silicon and Intel.
  Preferences remain separate; Developer ID distribution and Store submission are separate steps.

## Development history before 1.2.0

# Gruetze RetroSaver development

- Add a separately scrolling transparent woodland layer for stronger parallax depth.
- Add a saved Show scrolling text option for Amiga demos with instant preview;
  hidden text retains its content and settings.

- Rework jump-and-run art with project-owned generated pixel assets: layered
  woodland panorama, shaded mossy rock platforms, six running/two jumping poses
  and faceted collectible crystals. Textures are prepared once at game resolution.

- Add an autoplay pixel jump-and-run to the Amiga demos and playlist, with
  fixed-step jump physics, platform collisions, crystal pickups and parallax hills.

- Optional red disk-activity reflection on the CRT glass during Kickstart startup,
  with short irregular seek bursts and quiet pauses.

- Replace the freehand Kickstart approximations with historical static/animated
  screen captures, preserving original layout, text, palette and frame timing.

- Amiga: three independently drawn Kickstart-style startup motifs, four demos,
  configurable playlist/runtime and continuous scrolling text across demo changes.
- Persistent Amiga drawing buffers and sprite atlases; bounded software CRT
  resolution with Efficient/Balanced/Detailed quality, retaining full GPU detail.
- Display-synchronized companion preview, paused while dialogs/fullscreen previews
  are active; larger preview with a separate scrolling editor panel.

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
