#!/bin/bash
# Source before signing. All operations stay inside a newly staged app bundle.
retrosaver_store_layout() {
  local app="$1" native_file name
  mkdir -p "$app/Contents/Frameworks" "$app/Contents/Resources/published"
  # Runtime crash-dump tooling is not part of the screensaver product.
  rm -f "$app/Contents/MacOS/createdump"
  for native_file in "$app/Contents/MacOS/"*.dylib; do
    [[ -f "$native_file" && ! -L "$native_file" ]] || continue
    name="$(basename "$native_file")"
    mv "$native_file" "$app/Contents/Frameworks/$name"
    ln -s "../Frameworks/$name" "$native_file"
  done
  # Preserve .NET's relative lookup paths without putting unsigned data in MacOS.
  for native_file in "$app/Contents/MacOS/"*; do
    [[ -f "$native_file" && ! -L "$native_file" ]] || continue
    if file "$native_file" | grep -q 'Mach-O'; then continue; fi
    name="$(basename "$native_file")"
    mv "$native_file" "$app/Contents/Resources/published/$name"
    ln -s "../Resources/published/$name" "$native_file"
  done
}
