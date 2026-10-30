# Native Mac performance: 1.2.3

The native host now creates a Metal device/command queue and offers it to the
NativeAOT renderer. Each session lazily creates its own serialized GPU context,
reuses a GPU surface and limits its resource cache to 64 MiB. CRT composition
uses the existing shader rather than a gamma approximation. BGRA readback stays
on the worker queue; AppKit receives an independently owned immutable frame.
If Metal is unavailable or its target/readback fails, that session uses the bounded
CPU renderer. Bloom extraction still uses the small CPU source surface.

Fullscreen rendering and session retirement now join the asynchronous queue used
by previews; stopping/detaching never waits for a render lock on AppKit. Stale
completions are rejected by generation. Amiga requests 60 fps, C64 30 fps; preview
fallback ticks at 30 fps. Requested rates are targets, not guaranteed measurements.

MetalSmoke.m measures Runner and Plasma CPU sessions before enabling Metal, then
measures real GPU sessions including readback. With a device present it fails if
Metal is not used or returns a black frame; without one it explicitly skips GPU
measurements. Hosted runners are not a Neo benchmark. OptionsSmoke also checks
fullscreen async submission, stop/start, stale results and option availability.

Measured on the macOS 26 hosted Apple Paravirtual device (960×540 output, default CRT):
Runner CPU 25.39 ms/frame vs Metal 9.35 ms/frame; Plasma CPU 28.99 vs Metal 8.94.
These include BGRA readback, exclude AppKit presentation and are not measurements
of the user's Neo. The real AppKit fullscreen flag passed at 1024×768 client size.

## Earlier measurements / CPU fallback history

# Native Mac performance

The prototype uses Skia CPU raster rendering, not a Metal/GPU render target.
Its CRT shader runs for every output pixel, including multiple source samples,
mask, scanline, curvature and bloom composition. The BASIC interpreter is a
small part of this workload. Bloom itself is calculated at the 384×288 or
320×200 source resolution, so turning down glow alone does not remove the
full-screen CRT shader cost.

The previous default allowed 1920×1080 CPU frames at a requested 30 fps.
Each immutable AppKit frame also copied approximately 7.91 MiB. Multiple
monitors/previews multiply those costs.

Build 5 adds an explicit native rendering quality control:
- Efficient (default): maximum 960×540, 1.98 MiB per frame.
- Balanced: maximum 1280×720, 3.52 MiB per frame.
- Detailed: maximum 1920×1080, 7.91 MiB per frame.

AppKit scales the result to the monitor. Full-screen coverage and BASIC logic
are unchanged. Lower budgets soften fine phosphor/scanline detail; Detailed is
available when the machine has enough CPU capacity. These are maximum
dimensions: small previews are not upscaled to the cap and the screen aspect
ratio is preserved.

The Efficient cap processes 75% fewer output pixels than Detailed. This is a
work budget reduction, not a promise of 75% lower measured CPU consumption.
Other changes reuse the color space across frames and coalesce duplicate
AppKit calls within one animation interval. Immutable frame copies are retained
deliberately: removing them without ownership guarantees would expose AppKit
to memory being overwritten by the next frame.

Reproduce the CPU-only measurement:

    dotnet run --project tests/NativePerformance -c Release

This warms each resolution and prints average elapsed milliseconds for twelve
frames, plus copy sizes. It excludes AppKit presentation and is a short
diagnostic, not an end-to-end smoothness/power benchmark. It runs in GitHub
Actions on Apple Silicon and Intel too; runner values do not represent a Neo.
Test lifecycle/rendering and quality persistence with the native options smoke
suite during the normal Mac build.

On the user's Mac, compare the same program, effects and monitor count in
Activity Monitor using Efficient versus Detailed. Check smoothness, CPU,
memory after several cycles and energy usage. A remaining architectural
improvement is rendering directly to a Metal/GPU surface; that requires a
different native presentation/Skia backend and is not implemented here.


## Amiga-Ausbau und begrenztes Software-Rendering (29. Oktober 2026)

Die Software-CRT-Pipeline rechnet jetzt unabhängig von der Monitorgröße in einem
begrenzten Zwischenbild und skaliert das fertige Ergebnis. Die Qualitätsstufen
verwenden maximal 640×360 (Efficient), 960×540 (Balanced) bzw. 1280×720 (Detailed).
Das tatsächliche Seitenverhältnis bleibt erhalten. Native Host-Ausgabepuffer und
Monitordarstellung behalten ihre bisherige Größe. GPU-Rendering in Avalonia
bleibt in voller Auflösung. Feine Masken und Scanlines werden bei niedrigeren
Software-Stufen sichtbar weicher; höhere Qualität braucht mehr Rechenzeit.

Lokale Windows-CPU-Rastermessung mit tests/NativePerformance, gleiche
CRT-Voreinstellungen, drei Aufwärmbilder und zwölf Messbilder:

| Ausgabe | Vorher ms/Bild | Jetzt, Efficient ms/Bild |
| --- | ---: | ---: |
| 960×540 | 39,17 | 21,66 |
| 1280×720 | 60,88 | 21,54 |
| 1920×1080 | 512,68 | 24,91 |

Diese Stichprobe enthält die Shaderarbeit und CPU-Ausgabe, aber weder AppKit-
Präsentation noch eine Messung auf dem Neo. Sie garantiert keine bestimmte
Bildrate. Laufende Vorschaufenster beeinflussen die lokale Rechnerlast.

Die Amiga-Quellbilder verwenden eine wiederverwendete Fläche, vorbereitete
Schriftspalten und vorberechnete Kugelbilder. Gemessene reine Szenen-Zeiten
liegen lokal bei ungefähr 0,35–0,48 ms/Bild; die verwalteten Allokationen sinken
von rund 2074 Byte/Bild beim alten Copper-Prototyp auf ca. 104–208 Byte/Bild.
Das ist wesentlich weniger als der Aufwand des Software-CRT-Effekts.

Die Begleit-App verwendet den Darstellungstakt statt eines unabhängigen
30-Hz-Timers, begrenzt auf maximal 60 Aktualisierungen pro Sekunde. Während
eigener Vollbildvorschauen und modaler Dialoge pausiert ihre Hintergrundvorschau.
Die Vollbildanzeige vermeidet ebenfalls unnötige Aktualisierungen oberhalb 60 Hz.
Animationen verwenden weiterhin die verstrichene Zeit statt fester Schrittweiten.


Die optionale Laufwerksreflexion liegt nach dem CRT-Pass auf dem Glas. Sie verwendet
eine einmal vorberechnete 96×48-Grafik und nur einen kleinen Bildaufruf pro aktivem
Bild. Ruhige Phasen überspringen das Zeichnen vollständig; kein zusätzlicher
Vollbild-Shader oder Blur-Pass ist nötig. Sie ist nur im Kickstart-Start aktiv.

## Jump-and-run artwork revision 2

The new panorama, terrain atlas and character sheet are embedded resources.
The renderer prepares a 768×256 panorama, small platform surfaces and eight
40×44 character frames once, before the demo starts. Sprite bounds use one
bulk pixel read instead of millions of individual native pixel calls.
Other Amiga demos do not load the runner artwork until it is selected.

Per-frame work is a few cached image draws plus the existing fixed-step physics.
Local measured source rendering is about 0.40 ms/frame with 104 managed bytes
per frame, excluding CRT effects. The three-minute autoplay/30-vs-60-Hz tests
continue to pass. These are local measurements, not a Neo performance claim.

## Second parallax layer and text visibility

A second transparent 768×256 woodland texture is cached once. Far scenery moves
at 0.18 of camera speed, middle woodland at 0.46, and terrain at full speed.
Both backgrounds use mirrored joins. Local source rendering with both layers
and scrolling text measured about 0.51 ms/frame, excluding CRT.
Disabling the scroller skips its draw calls entirely; text and speed are preserved.
