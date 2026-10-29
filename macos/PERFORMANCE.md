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
