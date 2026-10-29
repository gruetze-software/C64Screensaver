#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
arch="$(uname -m)"
if [[ $# -gt 0 ]]; then arch="$1"; fi
case "$arch" in
  arm64) rid=osx-arm64 ;;
  x86_64|x64) arch=x86_64; rid=osx-x64 ;;
  *) echo "Use arm64 or x86_64" >&2; exit 1 ;;
esac
[[ "$(uname -s)" == Darwin ]] || { echo "Build on macOS." >&2; exit 1; }
version="$(sed -n 's:.*<Version>\([^<]*\)</Version>.*:\1:p' "$root/C64Screensaver/C64Screensaver.csproj" | head -1)"
out="$root/artifacts/macos-companion/$rid"
app="$out/Gruetze RetroSaver.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources"
dotnet publish "$root/C64Screensaver/C64Screensaver.csproj" -c Release -r "$rid" --self-contained true \
  -p:PublishSingleFile=true -p:DebugType=None -p:DebugSymbols=false -o "$app/Contents/MacOS"
cp "$root/macos/Companion-Info.plist" "$app/Contents/Info.plist"
plutil -replace CFBundleShortVersionString -string "$version" "$app/Contents/Info.plist"
plutil -replace CFBundleVersion -string "$version" "$app/Contents/Info.plist"
cp "$root/C64Screensaver/Assets/app-icon.icns" "$app/Contents/Resources/"
while IFS= read -r -d '' native_file; do
  if file "$native_file" | grep -q 'Mach-O'; then codesign --force --sign - "$native_file"; fi
done < <(find "$app/Contents/MacOS" -type f -print0)
codesign --force --sign - "$app"
codesign --verify --deep --strict "$app"
"$app/Contents/MacOS/c64screensaver" --help
(cd "$out" && ditto -c -k --keepParent "Gruetze RetroSaver.app" "Gruetze-RetroSaver-$version-macos-$arch-test.zip")
echo "Developer test app (ad-hoc signed, not notarized): $app"
