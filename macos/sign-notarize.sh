#!/bin/bash
set -euo pipefail
if [[ $# -ne 3 ]]; then
  echo "Usage: bash macos/sign-notarize.sh /absolute/C64Screensaver.saver 'Developer ID Application: Name (TEAMID)' keychain-profile" >&2
  exit 2
fi
[[ "$(uname -s)" == Darwin ]] || { echo "Run on macOS." >&2; exit 2; }
source_bundle="$1"
identity="$2"
profile="$3"
[[ -d "$source_bundle" && "$source_bundle" == *.saver ]] || { echo "Expected a .saver bundle." >&2; exit 2; }
[[ "$identity" == "Developer ID Application: "* ]] || { echo "Use a Developer ID Application identity." >&2; exit 2; }
root="$(cd "$(dirname "$0")/.." && pwd)"
mkdir -p "$root/artifacts/macos-signed"
work="$(mktemp -d "$root/artifacts/macos-signed/distribution.XXXXXX")"
mkdir "$work/payload"
bundle="$work/payload/C64Screensaver.saver"
ditto "$source_bundle" "$bundle"
# Sign all nested native libraries and then the enclosing plug-in with one identity.
codesign --force --timestamp --options runtime --sign "$identity" "$bundle/Contents/Frameworks/libSkiaSharp.dylib"
codesign --force --timestamp --options runtime --sign "$identity" "$bundle/Contents/Frameworks/C64Screensaver.NativeCore.dylib"
codesign --force --timestamp --options runtime --sign "$identity" "$bundle"
codesign --verify --deep --strict --verbose=2 "$bundle"
dmg="$work/C64Screensaver.dmg"
hdiutil create -volname C64Screensaver -srcfolder "$work/payload" -format UDZO "$dmg"
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
