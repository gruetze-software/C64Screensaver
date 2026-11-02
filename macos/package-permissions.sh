#!/bin/bash
# Only call on a staged application, never on installed apps or user settings.
retrosaver_normalize_permissions() {
  local app="$1"
  [[ -d "$app/Contents" && "$app" == *.app ]] || return 1
  # find does not follow the runtime symlinks into unrelated directories.
  find "$app" -type d -exec chmod 755 {} + || return 1
  find "$app" -type f -exec chmod a+rX,u+w,go-w {} + || return 1
}

retrosaver_check_app_permissions() {
  local app="$1" path bad=0
  while IFS= read -r -d '' path; do
    echo "App entry is not readable/traversable by other users: $path" >&2
    bad=1
  done < <(find "$app" \( -type f ! -perm -0044 -o -type d ! -perm -0055 \) -print0)
  [[ "$bad" == 0 ]]
}

retrosaver_check_package_permissions() {
  local pkg="$1" inspection="$2" bom name mode remainder bits kind count=0 entries=0
  # Inspect the package's recorded modes, not just the source folder.
  pkgutil --expand "$pkg" "$inspection" || return 1
  while IFS= read -r -d '' bom; do
    count=$((count + 1))
    lsbom "$bom" > "$inspection/bom-$count.txt" || return 1
    while IFS=$'\t' read -r name mode remainder; do
      [[ "$mode" =~ ^[0-7]+$ ]] || { echo "Invalid package mode: $name $mode" >&2; return 1; }
      bits=$((8#$mode))
      kind=$((bits & 0170000))
      if [[ "$kind" == $((0100000)) ]]; then
        entries=$((entries + 1))
        (( (bits & 0044) == 0044 )) || { echo "Package file is not readable by other users: $name ($mode)" >&2; return 1; }
      elif [[ "$kind" == $((0040000)) ]]; then
        (( (bits & 0055) == 0055 )) || { echo "Package directory is not traversable by other users: $name ($mode)" >&2; return 1; }
      fi
    done < "$inspection/bom-$count.txt"
  done < <(find "$inspection" -type f -name Bom -print0)
  (( count > 0 && entries > 0 )) || { echo "No package file permissions could be inspected." >&2; return 1; }
  echo "PASS: package file and directory permissions ($entries files)."
}
