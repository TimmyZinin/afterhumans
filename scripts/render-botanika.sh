#!/usr/bin/env bash
# render-botanika.sh — full headless build+render pipeline for the Botanika scene.
# Runs ON the Contabo host (orchestrates the unity-hub-activator container).
# Usage (from Mac):
#   scp Assets/_Project/Editor/Botanika*.cs root@185.202.239.165:/opt/afterhumans-build/
#   scp scripts/render-botanika.sh root@185.202.239.165:/opt/afterhumans-build/
#   ssh root@185.202.239.165 'bash /opt/afterhumans-build/render-botanika.sh'
#   scp 'root@185.202.239.165:/opt/afterhumans-build/shots/*.png' docs/m1_greybox_shots/
set -euo pipefail

C=unity-hub-activator
UNITY=/root/Unity/Hub/Editor/6000.0.72f1/Editor/Unity
PROJ=/root/afterhumans
EDITOR_DIR=$PROJ/Assets/_Project/Editor
OUT=/tmp/afterhumans_visual_review
HOST_SHOTS=/opt/afterhumans-build/shots

echo "=== [1/4] copy .cs into container ==="
for f in BotanikaBuilder.cs BotanikaCameraProbe.cs; do
  if [ -f /opt/afterhumans-build/$f ]; then
    docker cp /opt/afterhumans-build/$f $C:$EDITOR_DIR/$f
    echo "  copied $f"
  fi
done

echo "=== [2/4] BUILD (greybox+art+light) — nographics ==="
docker exec $C bash -lc "
  $UNITY -batchmode -nographics -quit -projectPath $PROJ \
    -executeMethod Afterhumans.EditorTools.BotanikaBuilder.BuildFull \
    -logFile - 2>&1 | tail -40
  echo \"BUILD_EXIT=\${PIPESTATUS[0]}\"
"

echo "=== [3/4] RENDER (CaptureLit) — xvfb + glcore ==="
docker exec $C bash -lc "
  rm -f $OUT/1*_lit_*.png 2>/dev/null || true
  xvfb-run -a -s '-screen 0 1920x1080x24' \
    $UNITY -batchmode -quit -force-glcore -projectPath $PROJ \
    -executeMethod Afterhumans.EditorTools.BotanikaCameraProbe.CaptureLit \
    -logFile - 2>&1 | tail -30
  echo \"RENDER_EXIT=\${PIPESTATUS[0]}\"
  ls -la $OUT/1*_lit_*.png 2>&1
"

echo "=== [4/4] pull screenshots to host ==="
mkdir -p $HOST_SHOTS
for n in 10_lit_forward 11_lit_hero 12_lit_mid; do
  docker cp $C:$OUT/$n.png $HOST_SHOTS/$n.png 2>&1 && echo "  pulled $n.png" || echo "  MISSING $n.png"
done
ls -la $HOST_SHOTS/
echo "=== DONE ==="
