#!/bin/bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
source "$root/macos/disk-image.sh"
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
saver="$root/artifacts/macos-native/$rid/C64Screensaver.saver"
[[ -d "$saver" ]] || { echo "Build the native screensaver first." >&2; exit 1; }
ditto "$saver" "$app/Contents/Resources/C64Screensaver.saver"
while IFS= read -r -d '' native_file; do
  if file "$native_file" | grep -q 'Mach-O'; then codesign --force --sign - "$native_file"; fi
done < <(find "$app/Contents/MacOS" -type f -print0)
codesign --force --sign - "$app"
codesign --verify --deep --strict "$app"
"$app/Contents/MacOS/c64screensaver" --help
payload="$(mktemp -d "$out/dmg-payload.XXXXXX")"
ditto "$app" "$payload/Gruetze RetroSaver.app"
ln -s /Applications "$payload/Applications"
cp "$root/macos/INSTALL-DE.txt" "$payload/Lies mich.txt"
dmg="$out/Gruetze-RetroSaver-$version-macos-$arch.dmg"
retrosaver_create_dmg "$payload" "$dmg"
# Inspect the actual image, not just the source staging folder.
mount="$(mktemp -d "$out/mount.XXXXXX")"
retrosaver_attach_dmg "$dmg" "$mount" readonly
trap '/usr/sbin/diskutil eject "$mount" >/dev/null || true' EXIT
codesign --verify --deep --strict "$mount/Gruetze RetroSaver.app"
test -f "$mount/Gruetze RetroSaver.app/Contents/Resources/C64Screensaver.saver/Contents/Info.plist"
"$mount/Gruetze RetroSaver.app/Contents/MacOS/c64screensaver" --help
/usr/sbin/diskutil eject "$mount"
trap - EXIT
echo "Developer test app (ad-hoc signed, not notarized): $app"
