# Rust cross-platform core

Initial Rust implementation of the cross-platform package parser and CLI.

## Implemented

- IPA / TIPA:
  - Binary and XML `Info.plist`
  - Main bundle selection when multiple `.app` bundles exist
  - Package ID, app name, version name/code
  - `CFBundleIcons`, `CFBundleIcons~ipad`, legacy icon declarations
  - Ordinal case-insensitive ZIP entry lookup
  - Raw PNG/JPEG/WebP icon extraction
- APPX / APPXBUNDLE:
  - `AppxManifest.xml` identity, display name and version
  - Exact and highest-scale logo selection
  - Nested application package selection from app bundles
- HAP:
  - `module.json`
  - `resources.index` string and media ID maps
  - `$string:` label resolution
  - `$media:` and layered-image icon resolution
  - PNG/JPEG/WebP extraction
- ZIP-bomb limits:
  - Entry count
  - Per-entry declared size
  - Total declared uncompressed size

## Not implemented yet

- APK AXML / `resources.arsc` / VectorDrawable
- Apple CgBI PNG decrushing
- Icon decoding/resizing/conversion to PNG
- Platform shell adapters

## Build

```bash
cd rust
cargo build --release
```

## Use

```bash
package-icon inspect example.ipa
package-icon inspect example.hap
package-icon extract-icon example.hap icon.webp
```

`extract-icon` currently preserves the original image bytes and format. The output extension should match the `mediaType` printed to stderr.
