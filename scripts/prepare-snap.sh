#!/usr/bin/env bash
set -euo pipefail
root="$(cd "$(dirname "$0")/.." && pwd)"
arch="${1:-amd64}"
case "$arch" in amd64) rid=linux-x64 ;; arm64) rid=linux-arm64 ;; *) echo "Use amd64 or arm64" >&2; exit 2 ;; esac
output="$root/snap/local/publish"
if [[ -e "$output" ]]; then echo "Staging folder already exists: $output. Move it aside before preparing another build." >&2; exit 1; fi
dotnet publish "$root/C64Screensaver/C64Screensaver.csproj" -c Release -r "$rid" --self-contained true -p:DebugType=None -p:DebugSymbols=false -o "$output" --nologo
version="$(sed -n 's/.*<Version>\([^<]*\)<\/Version>.*/\1/p' "$root/C64Screensaver/C64Screensaver.csproj" | head -n1)"
[[ "$version" =~ ^[0-9]+\.[0-9]+\.[0-9]+$ ]]
printf '%s' "$version" > "$output/BUILD-VERSION"
printf '%s' "$arch" > "$output/BUILD-ARCH"
cp "$root/LICENSE" "$output/LICENSE"
cp "$root/C64Screensaver/Assets/Kickstart/CREDITS.md" "$output/KICKSTART-CREDITS.md"
chmod +x "$output/c64screensaver" "$root/snap/local/launcher/launch"
bad_files="$(find "$output" -type f \( -name '*.pdb' -o -name '*.dbg' -o -name '*.dylib' -o -name '*.scr' -o -name '*.icns' \))"
if [[ -n "$bad_files" ]]; then echo 'Unexpected platform/debug files in Snap payload' >&2; exit 1; fi
printf 'Prepared %s %s. Build on Ubuntu: snapcraft --platform %s\n' "$version" "$arch" "$arch"
