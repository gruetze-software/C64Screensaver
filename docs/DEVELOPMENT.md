# Build, test and package Gruetze RetroSaver

Run the commands below from the repository root.

## Build and test

Requires the .NET 10 SDK. Package versions are fixed in the project file.

```sh
dotnet build C64Screensaver.sln -c Release
dotnet run --project tests/Smoke -c Release
```

The smoke suite checks BASIC execution, ghost typing, rotation, settings,
proportions, CRT transitions, live previews and editor layout. It also regenerates
README dialog screenshots using isolated settings, without resetting personal
preferences. Additional Amiga and native bridge tests are in `tests/Amiga` and
`tests/NativeBridge`. Mac CI checks the native options, lifecycle, Metal rendering,
real fullscreen and packaged DMG on Apple Silicon and Intel.

To deliberately reset personal settings and capture defaults, use the optional
`--release-screenshots` smoke argument. Previous settings are backed up in
`artifacts/user-settings-before-release.json`.

## Prepare release packages

```powershell
pwsh scripts/release.ps1
```

Builds and tests Release, then creates Windows/Linux ZIPs and SHA-256 checksums
in `artifacts/release/<version>`. Download packages include a short offline guide and
credits; screenshots and Mac developer instructions stay in the repository.
Publish output excludes dependency debug symbols as well as application symbols.
Use `-OutputDirectory artifacts/package-audit/optimized` for separate trial packages.
For one platform use
`pwsh scripts/publish.ps1 -Runtime win-x64`. Public release targets are win-x64,
win-arm64, linux-x64 and linux-arm64. Publishing locally does not upload a release.

See the [changelog](../CHANGELOG.md), [CRT research](CRT-RESEARCH.md) and
[Mac performance notes](../macos/PERFORMANCE.md).
