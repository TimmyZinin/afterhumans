#!/bin/bash
# Sprint A acceptance evidence collector. Run AFTER a fresh build is in Build/WebGL_play
# and the local server (serve_play.py, :8921) is up. Produces machine-checkable evidence
# for the judge panel: ambient-from-spawn, dialogue audio+subtitle near NPC, screenshots.
# Usage: bash scripts/verify_build_sprintA.sh [session_name]
# Output: evidence lines on stdout + screenshots in repo root (va_*.png).
set -uo pipefail
cd "$(dirname "$0")/.."
S="${1:-va}"
TS=$(date +%s)
EV=()

playwright-cli close -s="$S" >/dev/null 2>&1
playwright-cli open "http://localhost:8922/?v=$TS" -s="$S" >/dev/null 2>&1

# Patch the audio counter IMMEDIATELY after open — BEFORE Unity finishes loading.
# playOnAwake sources call start() while the AudioContext is still suspended (the click
# later only resume()s, no re-start), so patching after load races Unity and read 0.
playwright-cli eval "() => { window.__as=0; const P=AudioBufferSourceNode.prototype.start; AudioBufferSourceNode.prototype.start=function(){window.__as++; return P.apply(this,arguments);}; return 'p'; }" -s="$S" >/dev/null 2>&1

# wait for unity load
playwright-cli eval "() => new Promise(res => { let n=0; const t=setInterval(()=>{ n++; const bar=document.querySelector('#unity-loading-bar'); const hidden = !bar || getComputedStyle(bar).display==='none'; if(hidden && n>4){clearInterval(t);res('loaded@'+n);} if(n>90){clearInterval(t);res('timeout');} },1000); })" -s="$S" 2>&1 | grep -q loaded || { echo "EV load=FAIL"; exit 1; }
echo "EV load=OK"

# gesture (unlock AudioContext) + settle
playwright-cli click "canvas" -s="$S" >/dev/null 2>&1
playwright-cli eval "() => new Promise(r=>setTimeout(()=>r(1),4000))" -s="$S" >/dev/null 2>&1

# EVIDENCE 1: ambient/music from spawn (no movement, no E) — counter must be >0.
# Judges' finding: counters must be PER-SEGMENT deltas, not cumulative — reset after reading.
A0=$(playwright-cli eval "() => window.__as" -s="$S" 2>&1 | grep -oE '^[0-9]+|"[0-9]+"|Result[^0-9]*[0-9]+' | grep -oE '[0-9]+' | head -1)
echo "EV ambient_from_spawn_starts=${A0:-0}"
playwright-cli eval "() => { window.__as=0; return 0; }" -s="$S" >/dev/null 2>&1
playwright-cli screenshot --filename "va_spawn.png" -s="$S" >/dev/null 2>&1

# EVIDENCE 2: walk to Sasha (W ~3.5s), proximity dialogue: SEGMENT delta + subtitle visible
playwright-cli keydown w -s="$S" >/dev/null 2>&1
playwright-cli eval "() => new Promise(r=>setTimeout(()=>r(1),3500))" -s="$S" >/dev/null 2>&1
playwright-cli keyup w -s="$S" >/dev/null 2>&1
playwright-cli eval "() => new Promise(r=>setTimeout(()=>r(1),2500))" -s="$S" >/dev/null 2>&1
A1=$(playwright-cli eval "() => window.__as" -s="$S" 2>&1 | grep -oE '[0-9]+' | head -1)
echo "EV walk_and_near_npc_delta=${A1:-0}"
playwright-cli eval "() => { window.__as=0; return 0; }" -s="$S" >/dev/null 2>&1
playwright-cli screenshot --filename "va_npc_subtitle.png" -s="$S" >/dev/null 2>&1

# EVIDENCE 3: E-press fires dialogue too (SEGMENT delta)
playwright-cli press e -s="$S" >/dev/null 2>&1
playwright-cli eval "() => new Promise(r=>setTimeout(()=>r(1),1500))" -s="$S" >/dev/null 2>&1
A2=$(playwright-cli eval "() => window.__as" -s="$S" 2>&1 | grep -oE '[0-9]+' | head -1)
echo "EV after_E_delta=${A2:-0}"
playwright-cli screenshot --filename "va_after_e.png" -s="$S" >/dev/null 2>&1

# EVIDENCE 4: console errors (exclude known Shader spam)
LOG=$(ls -t .playwright-cli/console-*.log 2>/dev/null | head -1)
ERRS=$(grep -icE "exception|error" "$LOG" 2>/dev/null | head -1)
SHADER=$(grep -c "ERROR: Shader" "$LOG" 2>/dev/null | head -1)
echo "EV console_errors_total=${ERRS:-?} shader_known=${SHADER:-?}"

# EVIDENCE 5: subtitles present in console? (NpcDialogueHud has no console line — screenshot is the proof)
echo "EV screenshots=va_spawn.png,va_npc_subtitle.png,va_after_e.png"

playwright-cli close -s="$S" >/dev/null 2>&1
echo "EV done"
