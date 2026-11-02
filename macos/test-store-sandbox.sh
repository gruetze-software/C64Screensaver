#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
arch="${1:-$(uname -m)}"
case "$arch" in arm64) rid=osx-arm64 ;; x86_64|x64) rid=osx-x64 ;; *) exit 2 ;; esac
out="$root/artifacts/store-sandbox-probe/$rid"
app="$out/Gruetze RetroSaver Sandbox Probe.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
dotnet publish "$root/tests/StoreSandbox/StoreSandbox.csproj" -c Release -r "$rid" --self-contained true \
  -p:MacAppStore=true -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "$app/Contents/MacOS"
cp "$root/macos/Companion-Info.plist" "$app/Contents/Info.plist"
plutil -replace CFBundleIdentifier -string de.gruetze-software.RetroSaver.SandboxProbe "$app/Contents/Info.plist"
plutil -replace CFBundleExecutable -string StoreSandbox "$app/Contents/Info.plist"
cp "$root/C64Screensaver/Assets/app-icon.icns" "$app/Contents/Resources/"
while IFS= read -r -d '' native_file; do
  if file "$native_file" | grep -q 'Mach-O'; then codesign --force --sign - "$native_file"; fi
done < <(find "$app/Contents/MacOS" -type f -print0)
codesign --force --options runtime --entitlements "$root/macos/Store.entitlements" --sign - "$app"
codesign --verify --deep --strict "$app"
log="$out/probe.log"
open -n -W --stdout "$log" --stderr "$log" "$app"
cat "$log"
grep -q '^PASS: actual Store companion' "$log"
