#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
arch="$(uname -m)"
rid=osx-arm64
[[ "$arch" == arm64 ]] || rid=osx-x64
out="$root/artifacts/macos-shared-settings"
mkdir -p "$out"
dotnet build "$root/tests/MacSettings/MacSettings.csproj" -c Release -o "$out/managed"
xcrun clang -mmacosx-version-min=14.0 -fobjc-arc -fmodules -Wall -Wextra -Werror \
  -framework Cocoa -framework ScreenSaver "$root/macos/NativeHost/SharedSettingsSmoke.m" -o "$out/shared-settings-smoke"
"$out/shared-settings-smoke" "$root/artifacts/macos-native/$rid/C64Screensaver.saver" "$(command -v dotnet)" "$out/managed/MacSettings.dll"
