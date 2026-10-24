# Native macOS screensaver — experimental

This branch prototypes an installable ScreenSaverView bundle. It is separate from the
released Avalonia application and is not part of release 1.1.

## Architecture

The Objective-C view in NativeHost is loaded by Apple's screensaver host. A self-contained
.NET 10 NativeAOT shared library supplies the same BASIC interpreter, ghost typing,
program rotation, C64 character rendering and CRT/glow renderer as the desktop application.
Avalonia is not initialized inside Apple's host. Each view owns an independent session.
The C ABI validates sizes and handles, contains managed exceptions and writes BGRA frames
to caller-owned memory. Frames passed to AppKit are copied so their lifetime is independent
of the next render. NativeAOT/Skia modules remain loaded for the lifetime of the process:
NativeAOT library unloading is unsupported.

The prototype rasterizes at up to 1920×1080 and requests 30 fps. This is a performance cap,
not a guarantee; CPU usage, Retina scaling and energy use need real Mac measurements.
Its options include program selection, duration after RUN, 4:3/stretch, border visibility,
strong glow and the three company texts. Preferences use a separate NSUserDefaults suite;
the desktop application's JSON settings are not imported. The native power transition
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
