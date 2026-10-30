#!/bin/bash
# Shared by developer packaging and Developer ID distribution; Bash 3.2 compatible.
# diskutil image syntax changed in macOS 26 (positional source instead of -source).
retrosaver_attach_dmg() {
  if [[ "$3" == readonly ]]; then
    /usr/sbin/diskutil image attach -readOnly -mountOptions nobrowse -mountPoint "$2" "$1"
  else
    /usr/sbin/diskutil image attach -mountOptions nobrowse -mountPoint "$2" "$1"
  fi
}

retrosaver_create_dmg() (
  set -euo pipefail
  [[ $# -eq 2 && -d "$1" ]] || { echo "Expected source folder and destination DMG." >&2; exit 2; }
  local source destination folder work raw candidate mount mounted size major
  source="$(cd "$1" && pwd)"
  folder="$(cd "$(dirname "$2")" && pwd)"
  destination="$folder/$(basename "$2")"
  work="$(mktemp -d "$folder/.diskimage.XXXXXX")"
  raw="$work/payload.dmg"
  candidate="$work/distribution.dmg"
  mount="$work/mount"
  mounted=false
  cleanup() {
    # Do not remove an image that could not be ejected; retain it for diagnosis.
    if [[ "$mounted" == true ]]; then
      /usr/sbin/diskutil eject "$mount" >/dev/null || return 1
    fi
    rm -f "$raw" "$candidate"
    rmdir "$mount" 2>/dev/null || true
    rmdir "$work" 2>/dev/null || true
  }
  trap cleanup EXIT
  mkdir "$mount"
  # Reserve 50% plus 64 MiB beyond the payload for APFS metadata and copy growth.
  # du does not follow the payload's /Applications shortcut.
  size="$(du -sk "$source" | awk '{print int($1 * 1.5) + 65536}')"
  /usr/sbin/diskutil image create blank -format RAW -size "$size"k -volumeName "Gruetze RetroSaver" "$raw"
  retrosaver_attach_dmg "$raw" "$mount" writable
  mounted=true
  ditto "$source" "$mount"
  chmod 755 "$mount"
  /usr/sbin/diskutil eject "$mount"
  mounted=false
  major="$(sw_vers -productVersion | cut -d. -f1)"
  if (( major >= 26 )); then
    /usr/sbin/diskutil image create from --format UDZO "$raw" "$candidate"
  else
    /usr/sbin/diskutil image create from -format UDZO -source "$raw" "$candidate"
  fi
  # Publish only a successfully created image, replacing an earlier build atomically.
  mv -f "$candidate" "$destination"
)
