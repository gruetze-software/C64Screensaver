#!/bin/bash
set -euo pipefail
[[ "$(uname -s)" == Darwin ]] || { echo "Run on macOS." >&2; exit 2; }
root="$(cd "$(dirname "$0")/.." && pwd)"
source "$root/macos/package-permissions.sh"
mkdir -p "$root/artifacts/package-permission-tests"
work="$(mktemp -d "$root/artifacts/package-permission-tests/case.XXXXXX")"
# Reproduce a private shell umask and a downloaded profile with owner-only access.
umask 077
app="$work/Permission Probe.app"
mkdir -p "$app/Contents/MacOS" "$app/Contents/Resources" "$app/Contents/_CodeSignature"
cp /usr/bin/true "$app/Contents/MacOS/probe"
cat > "$app/Contents/Info.plist" <<'PLIST'
<?xml version="1.0" encoding="UTF-8"?>
<plist version="1.0"><dict>
<key>CFBundleIdentifier</key><string>de.gruetze-software.PackagePermissionProbe</string>
<key>CFBundleExecutable</key><string>probe</string>
<key>CFBundlePackageType</key><string>APPL</string>
<key>CFBundleVersion</key><string>1</string>
<key>CFBundleShortVersionString</key><string>1.0</string>
</dict></plist>
PLIST
printf 'fixture' > "$app/Contents/embedded.provisionprofile"
printf 'fixture' > "$app/Contents/_CodeSignature/CodeResources"
printf 'fixture' > "$app/Contents/Resources/data.txt"
chmod 700 "$app/Contents/MacOS/probe"
ln -s ../Resources/data.txt "$app/Contents/MacOS/data-link"
if retrosaver_check_app_permissions "$app"; then echo "Restricted fixture was not rejected." >&2; exit 1; fi
productbuild --component "$app" /Applications "$work/restricted.pkg"
if retrosaver_check_package_permissions "$work/restricted.pkg" "$work/restricted-audit"; then
  echo "Restricted package was not rejected." >&2; exit 1
fi
retrosaver_normalize_permissions "$app"
retrosaver_check_app_permissions "$app"
[[ "$(stat -f %Lp "$app/Contents/MacOS/probe")" == 755 ]]
[[ "$(stat -f %Lp "$app/Contents/embedded.provisionprofile")" == 644 ]]
[[ "$(stat -f %Lp "$app/Contents/_CodeSignature/CodeResources")" == 644 ]]
[[ "$(stat -f %Lp "$app/Contents/Resources/data.txt")" == 644 ]]
[[ -L "$app/Contents/MacOS/data-link" ]]
productbuild --component "$app" /Applications "$work/readable.pkg"
retrosaver_check_package_permissions "$work/readable.pkg" "$work/readable-audit"
echo "PASS: private umask/profile/signature permissions, executable bits, data modes, symlinks and actual package BOM."
