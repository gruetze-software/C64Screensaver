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
