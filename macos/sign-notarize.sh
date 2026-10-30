#!/bin/bash
set -euo pipefail
if [[ $# -ne 3 ]]; then
  echo "Usage: bash macos/sign-notarize.sh '/absolute/Gruetze RetroSaver.app' 'Developer ID Application: Name (TEAMID)' keychain-profile" >&2
  exit 2
fi
[[ "$(uname -s)" == Darwin ]] || { echo "Run on macOS." >&2; exit 2; }
source_bundle="$1"
identity="$2"
profile="$3"
[[ -d "$source_bundle" && ( "$source_bundle" == *.saver || "$source_bundle" == *.app ) ]] || { echo "Expected a .app or .saver bundle." >&2; exit 2; }
[[ "$identity" == "Developer ID Application: "* ]] || { echo "Use a Developer ID Application identity." >&2; exit 2; }
root="$(cd "$(dirname "$0")/.." && pwd)"
source "$root/macos/disk-image.sh"
mkdir -p "$root/artifacts/macos-signed"
work="$(mktemp -d "$root/artifacts/macos-signed/distribution.XXXXXX")"
mkdir "$work/payload"
if [[ "$source_bundle" == *.app ]]; then
  app="$work/payload/Gruetze RetroSaver.app"
  ditto "$source_bundle" "$app"
  bundle="$app/Contents/Resources/C64Screensaver.saver"
  [[ -d "$bundle" ]] || { echo "The app must contain its native screensaver." >&2; exit 2; }
  while IFS= read -r -d '' native_file; do
    if file "$native_file" | grep -q 'Mach-O'; then
      codesign --force --timestamp --options runtime --sign "$identity" "$native_file"
    fi
  done < <(find "$app/Contents/MacOS" -type f -print0)
else
  bundle="$work/payload/C64Screensaver.saver"
  ditto "$source_bundle" "$bundle"
fi
codesign --force --timestamp --options runtime --sign "$identity" "$bundle/Contents/Frameworks/libSkiaSharp.dylib"
codesign --force --timestamp --options runtime --sign "$identity" "$bundle/Contents/Frameworks/C64Screensaver.NativeCore.dylib"
codesign --force --timestamp --options runtime --sign "$identity" "$bundle"
codesign --verify --deep --strict --verbose=2 "$bundle"
if [[ "$source_bundle" == *.app ]]; then
  # .NET 10 uses MAP_JIT; NativeAOT in the .saver needs no JIT entitlement.
  codesign --force --timestamp --options runtime --entitlements "$root/macos/Companion.entitlements" --sign "$identity" "$app"
  codesign --verify --deep --strict --verbose=2 "$app"
  "$app/Contents/MacOS/c64screensaver" --help
  ln -s /Applications "$work/payload/Applications"
  cp "$root/macos/INSTALL.txt" "$work/payload/Read me.txt"
  # Replace the developer-build note in the signed distribution's instructions.
  sed -i '' '/Developer test build:/,$d' "$work/payload/Read me.txt"
  echo "Developer ID signed distribution. www.gruetze-software.de" >> "$work/payload/Read me.txt"
fi
dmg="$work/Gruetze-RetroSaver.dmg"
retrosaver_create_dmg "$work/payload" "$dmg"
codesign --timestamp --sign "$identity" "$dmg"
# Credentials have already been stored interactively in the Mac login keychain.
# This command uploads the signed disk image to Apple's notary service.
xcrun notarytool submit "$dmg" --keychain-profile "$profile" --wait --output-format json > "$work/notary-result.json"
status="$(plutil -extract status raw -o - "$work/notary-result.json")"
[[ "$status" == Accepted ]] || { cat "$work/notary-result.json"; echo "Not accepted. Fetch the notary log before distributing." >&2; exit 1; }
# Staple the supported DMG container rather than assuming .saver supports stapling.
xcrun stapler staple "$dmg"
xcrun stapler validate "$dmg"
spctl --assess --type open --context context:primary-signature --verbose=2 "$dmg"
shasum -a 256 "$dmg" > "$work/SHA256SUMS.txt"
echo "Notarized distribution: $dmg"
echo "Test installation from this DMG on a separate Mac before publishing."
