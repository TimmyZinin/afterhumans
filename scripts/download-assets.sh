#!/bin/bash
# Download free CC0 assets for Afterhumans.
# Run once after cloning repo to populate Assets/_Project/Vendor/.
# Vendor folder is in .gitignore — each developer downloads locally.
#
# Sources (all CC0 / free for commercial use):
#   - Kenney.nl    (furniture, nature, city)
#   - Poly Haven   (HDRI sunset for desert)
#
# Manual acquisition needed (not scriptable — requires login):
#   - Mixamo           — humanoid characters + animations (free Adobe account)
#   - Sketchfab CC0    — "Dog Corgi Animated" (Kafka)
#   - Quaternius       — "Ultimate Animated Animal Pack" (Kafka fallback), "Modular Sci-Fi MegaKit"
#   - Freesound.org    — SFX (free account)

set -e

VENDOR="$(cd "$(dirname "$0")/.." && pwd)/Assets/_Project/Vendor"
mkdir -p "$VENDOR/Kenney" "$VENDOR/PolyHaven"

echo "=== Asset download target: $VENDOR ==="
echo ""

download() {
    local url="$1"
    local out="$2"
    if [ -f "$out" ]; then
        echo "SKIP (exists): $(basename "$out")"
        return 0
    fi
    echo "DOWNLOAD: $(basename "$out")"
    curl -sL -o "$out" "$url" && echo "  OK ($(ls -lh "$out" | awk '{print $5}'))" || echo "  FAIL"
}

# Kenney packs (CC0)
download \
    "https://kenney.nl/media/pages/assets/furniture-kit/e56d2a9828-1677580847/kenney_furniture-kit.zip" \
    "$VENDOR/Kenney/furniture-kit.zip"

download \
    "https://kenney.nl/media/pages/assets/nature-kit/8334871c74-1677698939/kenney_nature-kit.zip" \
    "$VENDOR/Kenney/nature-kit.zip"

download \
    "https://kenney.nl/media/pages/assets/city-kit-commercial/16eb35d771-1753115042/kenney_city-kit-commercial_2.1.zip" \
    "$VENDOR/Kenney/city-kit-commercial.zip"

# Poly Haven HDRI (CC0) — desert sunset
download \
    "https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/2k/rogland_sunset_2k.hdr" \
    "$VENDOR/PolyHaven/rogland_sunset_2k.hdr"

# Poly Haven HDRI (CC0) — Botanika golden hour (BOT-A02)
# Matches STORY §3.1 "тихий оазис, afternoon sun". Alternative more dramatic:
# the_sky_is_on_fire_2k.hdr, venice_sunset_2k.hdr
download \
    "https://dl.polyhaven.org/file/ph-assets/HDRIs/hdr/2k/kloppenheim_06_puresky_2k.hdr" \
    "$VENDOR/PolyHaven/kloppenheim_06_puresky_2k.hdr"

# Extract Kenney zips
echo ""
echo "=== Extract Kenney packs ==="
for zip in "$VENDOR/Kenney/"*.zip; do
    [ -f "$zip" ] || continue
    name=$(basename "$zip" .zip)
    target="$VENDOR/Kenney/$name"
    if [ -d "$target" ]; then
        echo "SKIP extract (exists): $name"
        continue
    fi
    echo "EXTRACT: $name"
    unzip -q -o "$zip" -d "$target" 2>&1 | tail -3 || echo "  FAIL"
done

echo ""
echo "=== Manual acquisition reminder ==="
cat <<'EOF'
Some assets require manual download (login or browser):

1. Mixamo characters (humanoid NPCs):
   https://www.mixamo.com/#/?page=1&type=Character
   Required: 8 characters (Саша, Мила, Кирилл, Николай, Стас, Дмитрий, Анна, Смотрительница)
   Download format: FBX for Unity, with skin (T-pose)
   Animations: idle, talk, sit, walk (download separately and retarget)

2. Sketchfab CC0 "Dog Corgi Animated" (for Kafka):
   https://sketchfab.com/3d-models/dog-corgi-animated-5cc0075d0aa645c398c51316236ff156
   Download: FBX, includes animations

3. Quaternius "Ultimate Animated Animal Pack" (Kafka fallback):
   https://quaternius.com/packs/ultimateanimatedanimals.html
   Contains corgi + 10+ other animals with full animation sets

4. Quaternius "Modular Sci-Fi MegaKit" (City buildings):
   https://quaternius.itch.io/modular-sci-fi-megakit
   270+ modular pieces for futuristic architecture

5. Freesound.org SFX (for Денис's sound design work):
   https://freesound.org/
   Account: free
   See docs/DENIS_BRIEF.md for full SFX list

Place manual downloads into:
   Assets/_Project/Vendor/Mixamo/
   Assets/_Project/Vendor/Sketchfab/
   Assets/_Project/Vendor/Quaternius/
   (folders auto-created by Unity on first import, or mkdir manually)
EOF

echo ""
echo "=== DONE ==="
ls -la "$VENDOR/Kenney/" "$VENDOR/PolyHaven/" 2>/dev/null
