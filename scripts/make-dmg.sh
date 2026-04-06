#!/usr/bin/env bash
# make-dmg.sh — package build/Afterhumans.app into a distributable .dmg
#
# Output: build/Послелюди.dmg
# Size: ~95 MB expected (compressed UDZO)
#
# Flow:
#   1. Verify the .app exists and is ad-hoc signed
#   2. Build a staging directory with .app + Applications symlink
#   3. hdiutil create compressed UDZO dmg
#   4. Print final size + sha256 for landing page integrity
#
# This script is idempotent: re-running overwrites the previous .dmg.

set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/.." && pwd)"
APP_PATH="$PROJECT_ROOT/build/Afterhumans.app"
DMG_NAME="Послелюди"
DMG_VOLUME_NAME="Послелюди"
DMG_OUTPUT="$PROJECT_ROOT/build/${DMG_NAME}.dmg"
STAGING_DIR="$PROJECT_ROOT/build/dmg_staging"

echo "==> Verifying .app at $APP_PATH"
if [ ! -d "$APP_PATH" ]; then
    echo "ERROR: $APP_PATH not found. Run Unity build first." >&2
    exit 1
fi

echo "==> Ad-hoc signature status:"
codesign -dvv "$APP_PATH" 2>&1 | head -5 || echo "  (not signed — Gatekeeper will require right-click → Открыть)"

echo "==> Clearing extended attributes (fixes Gatekeeper quarantine re-attach)"
xattr -cr "$APP_PATH" 2>/dev/null || true

echo "==> Preparing staging dir"
rm -rf "$STAGING_DIR"
mkdir -p "$STAGING_DIR"
cp -R "$APP_PATH" "$STAGING_DIR/"
ln -s /Applications "$STAGING_DIR/Applications"

echo "==> Removing previous DMG if any"
rm -f "$DMG_OUTPUT"

echo "==> Creating DMG (UDZO compressed) → $DMG_OUTPUT"
hdiutil create \
    -volname "$DMG_VOLUME_NAME" \
    -srcfolder "$STAGING_DIR" \
    -ov \
    -format UDZO \
    -fs HFS+ \
    "$DMG_OUTPUT"

echo "==> Cleaning staging dir"
rm -rf "$STAGING_DIR"

SIZE_BYTES=$(stat -f%z "$DMG_OUTPUT")
SIZE_MB=$(( SIZE_BYTES / 1024 / 1024 ))
SHA256=$(shasum -a 256 "$DMG_OUTPUT" | cut -d' ' -f1)

echo ""
echo "=========================================="
echo "DMG ready:"
echo "  path:   $DMG_OUTPUT"
echo "  size:   ${SIZE_MB} MB ($SIZE_BYTES bytes)"
echo "  sha256: $SHA256"
echo "=========================================="
