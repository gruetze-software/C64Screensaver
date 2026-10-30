# Gruetze RetroSaver 1.2.1 on macOS

DMG creation uses diskutil image create blank/from; mounting and ejecting also
use diskutil. A temporary APFS image preserves the volume name and payload
permissions, then converts to compressed read-only UDZO. macOS 15 uses its older
-source syntax; macOS 26 and later use a positional source. Failed builds retain
the previous output. Both developer and Developer ID packaging share this path.

Build one integrated distribution on a Mac:

    bash macos/build-native.sh arm64
    bash macos/build-companion.sh arm64
    bash macos/test-shared-settings.sh

Use x86_64 for Intel. The single DMG is under artifacts/macos-companion/osx-*.
Its app contains C64Screensaver.saver. Drag the app into Applications, open it and
click Install / update system screensaver; macOS handles the installation confirmation.
Select Gruetze RetroSaver (older installations may show C64Screensaver) in Screen Saver settings.

Both interfaces use /Users/Shared/GruetzeRetroSaver-<uid>/settings.json, where uid
is the login user's numeric UID. The directory is restricted to its owner; the name
is independent of the screensaver host's remapped home directory. Writes replace the
file atomically. Native playback checks once per second and retains its last good
settings on corrupt input; the companion reloads external saves on activation.
The first valid legacy source migrates only if the shared file is absent. The old
native suite remains a fallback cache; Windows/Linux keep their existing JSON path.
There is no daemon or idle monitor. App Store sandbox/review is still a separate task.

Developer DMGs are ad-hoc signed and not notarized. To sign/notarize the complete app,
use sign-notarize.sh with Gruetze RetroSaver.app, not just the embedded .saver.
See [the Developer ID guide](APPLE-DEVELOPER-DE.md).
CI checks cross-process settings exchange, live reload, corrupt-file fallback,
mounted-DMG content and signatures on Apple Silicon and Intel. It does not replace
checking Apple's actual screensaver host and Gatekeeper on each macOS release.

## Architecture

The Objective-C view in NativeHost is loaded by Apple's screensaver host. A self-contained
.NET 10 NativeAOT shared library supplies the same BASIC interpreter, ghost typing,
program rotation, C64 character rendering and CRT/glow renderer as the desktop application.
Avalonia is not initialized inside Apple's host. Each view owns an independent session.
The C ABI validates sizes and handles, contains managed exceptions and writes BGRA frames
to caller-owned memory. Frames passed to AppKit are copied so their lifetime is independent
of the next render. NativeAOT/Skia modules remain loaded for the lifetime of the process:
NativeAOT library unloading is unsupported.

The native host requests 30 fps fullscreen and 20 fps in the small preview. This is a performance cap,
not a guarantee; CPU usage, Retina scaling and energy use need real Mac measurements.
Its options include program selection, duration after RUN, 4:3/stretch, border visibility,
phosphor mask, glow, glow spread, scanline strength and the three company texts. An explicit fill-entire-screen option removes both letterboxing and the C64 border. Preferences use the common per-user JSON file; a local suite caches native fallback values. The native power transition
currently uses a Skia counterpart of the desktop effect.

## Build on a Mac

Requires .NET 10 SDK and Xcode command-line tools. macOS 14 is the declared experimental
deployment target, not a tested compatibility promise.

    bash macos/build-native.sh

For another architecture use arm64 or x86_64 as the first argument. Build each slice
separately; this script does not produce a universal bundle. The host and NativeAOT library
must have matching architectures. Outputs are under artifacts/macos-native/osx-*/.
The script applies ad-hoc signatures for local testing. Public distribution still needs
Developer ID signing, notarization and compatibility validation.

GitHub Actions builds both architectures and retains zipped bundles as workflow artifacts.
A successful build proves compilation and signing checks, not system integration.

## Try the bundle

1. Download the matching experimental workflow artifact, unzip it, and double-click
   C64Screensaver.saver to install for the current user.
2. Select it in macOS System Settings → Screen Saver. Open Options, save a distinct company
   name, reopen the options and check persistence.
3. Test both the small preview and full-screen preview. Check the boot screen is upright,
   READY appears correctly and slow typing reaches RUN before any animation.
4. Test all three programs, switching after 15 seconds of runtime, glow, 4:3/stretch and
   hidden borders. Check both preview and full-screen settings.
5. Repeatedly start/stop, close/reopen Settings, resize previews and test all attached
   monitors. Monitor CPU/memory and check Console for C64Screensaver messages.
6. Check normal password-protected screen locking still works. The module does not provide
   a separate lock or any inactivity monitor.

The local ad-hoc bundle may be blocked by macOS installation/security policy. Do not turn
off Gatekeeper; use a locally built bundle or a properly signed/notarized distribution.
To remove the prototype, delete only ~/Library/Screen Savers/C64Screensaver.saver using
Finder. Restart the screensaver host/Settings after rebuilding because the native runtime
cannot be unloaded and an existing process may retain the old library.

## Validation status

The Windows managed bridge tests cover ABI version, malformed JSON, invalid buffer sizes,
stride padding, real frame output, typing through RUN, independent views and disposal.
Existing desktop smoke tests exercise BASIC, settings and CRT rendering. Actual installation,
screen locking, lifecycle callbacks, preview orientation and performance require a Mac and
are not yet confirmed by this prototype.

    dotnet run --project tests/NativeBridge -c Release
    dotnet run --project tests/Smoke -c Release

## Primary references

- [Apple ScreenSaverView](https://developer.apple.com/documentation/screensaver/screensaverview)
- [Microsoft NativeAOT libraries](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/libraries)
- [Microsoft NativeAOT interop](https://learn.microsoft.com/en-us/dotnet/core/deploying/native-aot/interop)

This prototype uses the public ScreenSaver framework, not undocumented wallpaper extension
points. No stable release or main-branch integration is implied.

Both architectures have compiled and passed bundle signature verification in GitHub Actions.
The build additionally runs CoreSmoke.c against the actual NativeAOT exports and Skia dylib.

Experimental build 3 repairs numeric boolean settings from build 2 automatically. Native tests cover nonblack frames after saving, view reattachment, stop/start, slider persistence and borderless full-screen. After replacing an installed bundle, restart the Mac to clear the screensaver host cache.
