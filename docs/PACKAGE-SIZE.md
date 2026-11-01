# Release package size audit — 2026-11-01

Compared the public 1.2.3 ZIPs with separately rebuilt packages. Sizes below use
decimal MB (1 MB = 1,000,000 bytes). Trial packages are local artifacts, not
replacements for the published 1.2.3 assets.

| Package | Public ZIP | Lean ZIP | Reduction |
| --- | ---: | ---: | ---: |
| linux-arm64 | 57.70 MB | 54.79 MB | 5.0% |
| linux-x64 | 60.19 MB | 57.28 MB | 4.8% |
| win-arm64 | 86.88 MB | 57.37 MB | 34.0% |
| win-x64 | 89.41 MB | 59.55 MB | 33.4% |

## Findings and changes

- Windows shipped native libSkiaSharp.pdb and libHarfBuzzSharp.pdb, despite
  DebugType=None/DebugSymbols=false for the application. NuGet classifies them
  as native runtime assets. Together they added about 105 MB unpacked and
  26.6–27.0 MB compressed. A publish target now removes .pdb/.dbg entries when
  DebugSymbols=false; the release script rejects any remaining symbol files.
- Linux shipped no PDBs and no native Mac binaries. All ELF files in the lean
  archives match the advertised x64/ARM64 architecture.
- The Mac .icns icon was embedded in every application DLL by an Assets wildcard
  (733,026 source bytes). It is now excluded from Avalonia resources. The Mac
  builder still copies the original file separately into the .app bundle.
- Public ZIPs included website screenshots (including a redundant 1.29 MB C64
  screenshot) and Mac developer guides. A compact offline package README now
  replaces the full repository README; setup instructions, LICENSE, CHANGELOG
  and Kickstart artwork credits remain. Full screenshots/docs stay online.
- Avalonia.Native/Metal/Win32 assemblies in Linux are managed dependencies of
  the cross-platform desktop bootstrap, not macOS executable binaries. Their
  combined compressed size is small; they were retained along with all other
  runtime libraries. No manual framework-DLL deletion or trimming was applied.
- Most remaining space is the self-contained .NET runtime, graphics libraries
  and embedded game artwork. The packages still run without a separate .NET
  installation. Smaller framework-dependent downloads would change that promise.

## Validation

Release build: no warnings/errors. Existing BASIC, CRT, Amiga and companion smoke
checks passed after the resource exclusion. The final published Windows x64
executable starts with --help and exits 0. All four ZIPs pass CRC and SHA-256
checks; package contents contain no PDB/DBG, Mac native files, screenshots or
Mac developer documentation. Comparing old/new file lists confirms no runtime
binary was removed. Linux/ARM64 native execution was not tested on this Windows
machine. No public release assets have been replaced by these trial packages.

Reproduce from the repository root:

```powershell
pwsh scripts/release.ps1 -OutputDirectory artifacts/package-audit/optimized
```
