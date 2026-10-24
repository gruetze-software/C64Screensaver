#!/bin/bash
set -euo pipefail
# Run on a Mac with .NET 10 SDK and Xcode command-line tools.
root="$(cd "$(dirname "$0")/.." && pwd)"
arch="$(uname -m)"
if [[ $# -gt 0 ]]; then arch="$1"; fi
case "$arch" in
  arm64) rid=osx-arm64 ;;
  x86_64|x64) arch=x86_64; rid=osx-x64 ;;
  *) echo "Use arm64 or x86_64" >&2; exit 1 ;;
esac
[[ "$(uname -s)" == Darwin ]] || { echo "Build on macOS." >&2; exit 1; }
out="$root/artifacts/macos-native/$rid"
publish="$out/core"
bundle="$out/C64Screensaver.saver"
dotnet publish "$root/macos/NativeCore/C64Screensaver.NativeCore.csproj" \
  -c Release -r "$rid" -p:PublishAot=true -o "$publish"
xcrun clang -arch "$arch" -mmacosx-version-min=14.0 -Wall -Wextra -Werror \
  "$root/macos/NativeHost/CoreSmoke.c" -o "$out/core-smoke"
"$out/core-smoke" "$publish/C64Screensaver.NativeCore.dylib" "$publish/libSkiaSharp.dylib"
mkdir -p "$bundle/Contents/MacOS" "$bundle/Contents/Frameworks" "$bundle/Contents/Resources"
cp "$publish/C64Screensaver.NativeCore.dylib" "$bundle/Contents/Frameworks/"
cp "$publish/libSkiaSharp.dylib" "$bundle/Contents/Frameworks/"
cp "$root/macos/NativeHost/Info.plist" "$bundle/Contents/"
cp "$root/C64Screensaver/Assets/app-icon.icns" "$bundle/Contents/Resources/"
xcrun clang -arch "$arch" -mmacosx-version-min=14.0 -fobjc-arc -fmodules \
  -Wall -Wextra -Wno-unused-parameter -bundle \
  -framework Cocoa -framework ScreenSaver -framework QuartzCore \
  "$root/macos/NativeHost/C64SaverView.m" -o "$bundle/Contents/MacOS/C64Screensaver"
plutil -lint "$bundle/Contents/Info.plist"
# Ad-hoc signing is for local tests, not notarized distribution.
codesign --force --sign - "$bundle/Contents/Frameworks/libSkiaSharp.dylib"
codesign --force --sign - "$bundle/Contents/Frameworks/C64Screensaver.NativeCore.dylib"
codesign --force --sign - "$bundle"
codesign --verify --deep --strict "$bundle"
echo "Experimental bundle: $bundle"
echo "Inspect dependencies: otool -L \"$bundle/Contents/Frameworks/C64Screensaver.NativeCore.dylib\""
