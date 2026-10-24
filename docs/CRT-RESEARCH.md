# CRT research and implementation

The previous effect clipped the outline and overlaid stripes. Its image was not
being optically curved; global flicker and frequent BASIC border POKEs made it
restless. The replacement processes one complete image in a Skia runtime shader.

## References

- [VICE machine-specific video settings](https://vice-emu.sourceforge.io/vice_7.html):
  separate horizontal blur, scanline shading, palette, gamma and display filtering.
- [MAME HLSL documentation](https://docs.mamedev.org/advanced/hlsl.html): independently adjustable mask strength, image distortion, scanlines, vignette and bloom; settings depend on the monitor.
- [Libretro CRT shader overview](https://docs.libretro.com/shader/crt/):
  scanlines, phosphor masks, blending, optional barrel distortion and bloom.
- [CRT-Geom source and exposed parameters](https://github.com/libretro/glsl-shaders/blob/master/crt/shaders/crt-geom.glsl):
  independently controllable curvature, mask, scanline weight and gamma.
- [CRT-Easymode source and exposed parameters](https://github.com/libretro/slang-shaders/blob/master/crt/shaders/crt-easymode.slang):
  a flat CRT alternative with independent mask, beam/scanline and sharpness controls.
- [Avalonia custom rendering](https://docs.avaloniaui.net/docs/graphics-animation/custom-rendering):
  custom Skia rendering via ICustomDrawOperation and ISkiaSharpApiLeaseFeature.
- [Commodore Programmer's Reference: TI](https://www.devili.iki.fi/Computers/Commodore/C64/Programmers_Reference/Chapter_2/page_089.html):
  the interval timer counts 60 jiffies per second.

These are design references. The SKSL shader here is independently written;
no emulator shader source was imported.

## Controls and defaults

Each effect is adjustable from 0 to 100; CRT can also be disabled entirely.

| Control | Default | What it changes |
| --- | ---: | --- |
| Curvature | 18 | Inverse coordinate mapping bows the complete picture, including text. |
| Scanline strength | 22 | Source-row beam modulation; the small thumbnail illustrates it with enlarged spacing. |
| Phosphor mask | 8 | RGB grille anchored to output-pixel spacing. |
| Horizontal softness | 12 | Blends nearest and bilinear source sampling. |
| Phosphor glow | 12 | Small neighbour-based luminous halo. |
| Darkened corners | 15 | Smooth vignette toward the edges. |
| Brightness flicker | 0 | Optional low-amplitude brightness variation. |

Presets: Clean pixels, Subtle CRT, Classic monitor and Custom. Settings has a
live preview that can show the startup screen or running program. Changes are
saved only on Save. Cancel leaves the saved settings untouched.

This is a stylised CRT rendering pipeline, not a PAL signal decoder or physical
phosphor-persistence simulation. The glow is spatial, not a history-buffer trail.
If the runtime shader cannot compile, rendering falls back to the unfiltered
image. The smoke check requires successful shader compilation.

## Calm BASIC animation

The maze keeps its diagonal lines. Its visible listing uses T=TI+240 and
IF TI<T THEN 30, changing the border only after roughly four seconds.
The timer uses elapsed wall time, so animation speed does not multiply border
flash frequency. Settings may also disable border changes.

The second program prints GRUETZE-SOFTWARE, WWW.GRUETZE-SOFTWARE.DE and a short
slogan in green, cyan and light blue. It adds another card every four seconds,
eventually scrolling the screen. It is typed with the same pauses and corrected
typos as the maze. Select either program or alternate them after each run.

## Borderless display mode

The optional Fill entire monitor mode passes only the 320×200 character area
to the renderer and independently scales width and height to the available
screen. It disables coordinate curvature and the edge fade to avoid black
corners. The scene's palette brightness changes gradually even with CRT effects
disabled. All four rendered edges and a change at every pixel across two
1280×720 captures are checked by the smoke suite.

Company text is generated from Settings rather than hardcoded. Input is
normalised to the supported C64 glyph set before it is used in BASIC literals.
No PRG import is included.

## Settings layout and timed playback

Effect sliders retain at least 52 logical pixels of height; the Fluent thumb
must fit fully inside that height. A rendered minimum-size-window check verifies
the allocated height and thumb geometry. Playback has its own tab.

Program duration is configurable from 15 to 1800 seconds and starts only after
RUN. All-program mode rotates maze, company card and the BASIC starfield; each
transition resets the machine, palette, cursor and ghost typing. Single-program
mode repeats that same sequence with its selected listing. The smoke suite
checks all three transitions, wraparound, single-program restart and persistence.

## Preview correction and power transitions

The earlier source-row cutoff made scanlines and the mask vanish in the small
thumbnail. The mask now follows physical output-pixel spacing independently of
source scale. The small thumbnail explicitly enlarges its scanline spacing
instead of hiding the effect. Its caption explains that approximation. The
separate enlarged preview uses the same normal spacing as the screensaver.

All seven sliders are exercised in a real Settings window. Each is changed from
0 to 100, and pixel differences are measured in the preview region; unsaved
changes remain isolated from persisted settings. A second image comparison
verifies that the open enlarged preview also updates live.

The optional CRT power effect is a presentation transition, separate from BASIC
execution. Its timeline is: black 0–0.10 s, white 0.10–0.14 s, vertical collapse
0.14–0.46 s, line contraction 0.46–0.67 s, fading point 0.67–0.86 s, black until
1.02 s, and power-on reveal until 1.42 s. The next machine is prepared during
the black interval, and its ghost typing waits until the reveal ends.
A manual test button works even when Running program is unchecked.

For a later, more elaborate CRT style, useful references are
[CRT-Royale](https://docs.libretro.com/shader/crt_royale/) for mask choices,
beam properties, bloom and diffusion, and
[Mega Bezel](https://github.com/HyperspaceMadness/Mega_Bezel) for glass and bezel
reflections. Those complete shader chains have not been integrated here.
## Broad phosphor glow

The development renderer extracts luminance-gated light into a small offscreen image, applies a Gaussian blur and combines it after scanline/mask modulation.
Strength and spread are independent. This is spatial only, without flashing
or temporal persistence. Glowing CRT keeps flicker at zero. Dark-pixel image
comparisons verify that the halo extends beyond bright glyph pixels.

[cool-retro-term](https://github.com/Swordfish90/cool-retro-term) is a visual
reference supplied by the user; none of its QML or shader code is incorporated.
