#!/bin/bash
set -euo pipefail
# Builds a separate candidate; never edits the installed app or uploads anything.
# test: Developer ID / ad-hoc sandbox test. store: distribution-signed .pkg.
root="$(cd "$(dirname "$0")/.." && pwd)"
[[ "$(uname -s)" == Darwin ]] || { echo "Run on macOS." >&2; exit 2; }
mode="${1:-}"
arch="${2:-$(uname -m)}"
identity="${3:--}"
profile="${4:-}"
installer="${5:-}"
case "$arch" in arm64) rid=osx-arm64 ;; x86_64|x64) arch=x86_64; rid=osx-x64 ;; *) echo "Use arm64 or x86_64." >&2; exit 2 ;; esac
case "$mode" in
  test) [[ "$identity" == - || "$identity" == "Developer ID Application: "* ]] || { echo "Test with - or a Developer ID Application identity." >&2; exit 2; } ;;
  store)
    [[ "$identity" == "Apple Distribution: "* || "$identity" == "3rd Party Mac Developer Application: "* ]] || { echo "Use an App Store application signing identity." >&2; exit 2; }
    [[ -f "$profile" && "$installer" == "3rd Party Mac Developer Installer: "* ]] || { echo "Provide the Mac App Store profile and Mac Installer Distribution identity." >&2; exit 2; } ;;
  *) echo "Usage: bash macos/build-store.sh test arm64 [identity] OR store arm64 'Apple Distribution: ...' /path/profile.provisionprofile '3rd Party Mac Developer Installer: ...'" >&2; exit 2 ;;
esac
out="$root/artifacts/macos-store/$rid"
mkdir -p "$out"
work="$(mktemp -d "$out/$mode.XXXXXX")"
entitlements="$work/Store.entitlements"
cp "$root/macos/Store.entitlements" "$entitlements"
if [[ "$mode" == store ]]; then
  security cms -D -i "$profile" > "$work/profile.plist"
  team="$(/usr/libexec/PlistBuddy -c 'Print :TeamIdentifier:0' "$work/profile.plist")"
  appid="$(/usr/libexec/PlistBuddy -c 'Print :Entitlements:com.apple.application-identifier' "$work/profile.plist")"
  [[ "$appid" == "$team.de.gruetze-software.RetroSaver" ]] || { echo "Profile does not match this app." >&2; exit 2; }
  [[ "$identity" == *"($team)" ]] || { echo "Identity and profile teams do not match." >&2; exit 2; }
  [[ "$installer" == *"($team)" ]] || { echo "Installer and profile teams do not match." >&2; exit 2; }
  /usr/libexec/PlistBuddy -c "Add :com.apple.application-identifier string $appid" "$entitlements"
  /usr/libexec/PlistBuddy -c "Add :com.apple.developer.team-identifier string $team" "$entitlements"
fi
bash "$root/macos/build-native.sh" "$arch"
RETROSAVER_MAC_APP_STORE=1 bash "$root/macos/build-companion.sh" "$arch" --app-only
app="$work/Gruetze RetroSaver.app"
ditto "$root/artifacts/macos-companion/$rid/Gruetze RetroSaver.app" "$app"
# Place loadable code at Apple's standard nested-code location.
mkdir -p "$app/Contents/PlugIns"
mv "$app/Contents/Resources/C64Screensaver.saver" "$app/Contents/PlugIns/"
bundle="$app/Contents/PlugIns/C64Screensaver.saver"
plutil -replace NSHumanReadableCopyright -string 'Copyright © 2026 Christoph Schaefer / Grütze-Software' "$app/Contents/Info.plist"
if [[ "$mode" == store ]]; then cp "$profile" "$app/Contents/embedded.provisionprofile"; fi
while IFS= read -r -d '' native_file; do
  if [[ "$native_file" != "$app/Contents/MacOS/c64screensaver" ]] && file "$native_file" | grep -q 'Mach-O'; then
    codesign --force --options runtime --sign "$identity" "$native_file"
  fi
done < <(find "$app/Contents/MacOS" "$bundle/Contents/Frameworks" -type f -print0)
codesign --force --options runtime --sign "$identity" "$bundle"
codesign --force --options runtime --entitlements "$entitlements" --sign "$identity" "$app"
codesign --verify --deep --strict --verbose=2 "$app"
codesign -d --entitlements :- "$app" > "$work/signed-entitlements.plist"
[[ "$(/usr/libexec/PlistBuddy -c 'Print :com.apple.security.app-sandbox' "$work/signed-entitlements.plist")" == true ]]
if [[ "$mode" == test ]]; then
  echo "Sandbox test app: $app"
  echo "Open it, connect the settings folder, save, quit and reopen. Test the installed system screensaver as well."
  echo "This app is NOT an App Store upload."
else
  version="$(plutil -extract CFBundleShortVersionString raw -o - "$app/Contents/Info.plist")"
  pkg="$work/Gruetze-RetroSaver-$version-$arch.pkg"
  productbuild --component "$app" /Applications --sign "$installer" "$pkg"
  pkgutil --check-signature "$pkg"
  shasum -a 256 "$pkg" > "$work/SHA256SUMS.txt"
  echo "App Store candidate: $pkg"
  echo "Validate with Transporter. A valid signature does not establish App Review acceptance."
fi
