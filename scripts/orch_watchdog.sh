#!/bin/bash
# Orchestration watchdog for the Afterhumans Claude Code session.
# Runs every 180s via launchd. Checks the orchestration heartbeat + infra; if the
# session is stuck (stale heartbeat) and the job isn't done, it (a) heals infra,
# (b) nudges THIS Claude session back to work via `claude --resume -p`, with a
# cooldown so it never spawns a pile of sessions. Self-unloads when done.
#
# Heartbeat contract: the Claude session writes ~/afterhumans/.orch_heartbeat
# (line1 = current step) on every major step. When the whole task is finished it
# touches ~/afterhumans/.orch_done and the watchdog stops.

set -u
SID="3e07940c-166a-4b1f-8a0f-015f57f74148"   # this session's id (transcript filename)
ROOT="$HOME/afterhumans"
HB="$ROOT/.orch_heartbeat"
DONE="$ROOT/.orch_done"
LAST_NUDGE="$ROOT/.orch_last_nudge"
LOG="$ROOT/scripts/watchdog.log"
PLIST="$HOME/Library/LaunchAgents/com.zinin.afterhumans-watchdog.plist"
STALE_SECS=120        # >2 min without a heartbeat update = stuck. Catches a malformed
                      # tool-call (turn ends, session goes idle, heartbeat stops) and a hung
                      # build. The shared .orch_last_nudge cooldown dedups when BOTH the
                      # launchd job and the nohup loop watch at once → never double-nudges.
NUDGE_COOLDOWN=240    # don't nudge more often than every 4 min
NOW=$(date +%s)
stamp() { date '+%Y-%m-%d %H:%M:%S'; }
log() { echo "[$(stamp)] $*" >> "$LOG"; }

log "tick"

# 0. Done? Stop watching.
if [ -f "$DONE" ]; then
  log "  .orch_done present -> unloading watchdog"
  /bin/launchctl unload "$PLIST" 2>/dev/null
  exit 0
fi

# 1. Heal local preview server (:8911) if it died.
if ! /usr/sbin/lsof -ti:8911 >/dev/null 2>&1; then
  if [ -d "$ROOT/webgl_local/WebGL" ] && [ -f /tmp/serve_webgl_mt.py ]; then
    log "  server :8911 DOWN -> restarting"
    ( cd "$ROOT/webgl_local/WebGL" && nohup /usr/bin/python3 /tmp/serve_webgl_mt.py >/tmp/serve8911mt.log 2>&1 & )
  fi
fi

# 2. Heartbeat freshness.
if [ ! -f "$HB" ]; then
  log "  no heartbeat file yet (session may be starting) — skip"
  exit 0
fi
HBTS=$(/usr/bin/stat -f %m "$HB" 2>/dev/null || echo "$NOW")
AGE=$(( NOW - HBTS ))
STEP=$(/usr/bin/head -1 "$HB" 2>/dev/null)
log "  heartbeat age=${AGE}s step='${STEP}'"

if [ "$AGE" -lt "$STALE_SECS" ]; then
  log "  fresh -> session alive, nothing to do"
  exit 0
fi

# 3. Stuck. Notify Tim (macOS banner) + cooldown-guarded resume-nudge.
/usr/bin/osascript -e "display notification \"Оркестрация застряла на: ${STEP}. Бужу сессию.\" with title \"Afterhumans watchdog\"" 2>/dev/null

LASTN=0; [ -f "$LAST_NUDGE" ] && LASTN=$(/bin/cat "$LAST_NUDGE" 2>/dev/null || echo 0)
if [ $(( NOW - LASTN )) -lt "$NUDGE_COOLDOWN" ]; then
  log "  STALE but within nudge cooldown ($(( NOW - LASTN ))s) — banner only, no resume"
  exit 0
fi
# PILE-UP GUARD: never launch a second resume while one is already running. This is
# what prevents the "extra claude session" / RAM blowup seen earlier. Both the launchd
# job and the nohup loop hit this same check + the shared cooldown file.
if pgrep -f "claude --resume $SID" >/dev/null 2>&1; then
  log "  STALE but a resume-nudge already running — skip (no pile-up)"
  exit 0
fi
echo "$NOW" > "$LAST_NUDGE"

CLAUDE_BIN="$HOME/.local/bin/claude"
[ -x "$CLAUDE_BIN" ] || CLAUDE_BIN="$(command -v claude 2>/dev/null)"
if [ -z "$CLAUDE_BIN" ] || { [ ! -x "$CLAUDE_BIN" ] && [ ! -f "$CLAUDE_BIN" ]; }; then
  log "  STALE: claude binary not found ($CLAUDE_BIN) — banner only"
  exit 0
fi

NUDGE="Watchdog: heartbeat протух (${AGE}s) на шаге '${STEP}'. Если ты застрял на битом tool-call / ждёшь / выпал — НЕ жди. Проверь фоновые билды afterhumans (iter*.done на контейнере unity-hub-activator), продолжи незавершённое, верифицируй в живом WebGL (CDP :9222, сервер :8911), доложи Тиму. Обновляй ~/afterhumans/.orch_heartbeat на каждом шаге. Когда вся задача готова и принята Тимом — touch ~/afterhumans/.orch_done."
log "  STALE >${STALE_SECS}s -> resume-nudge via claude --resume $SID"
# macOS has no \`timeout\`/\`gtimeout\`; use perl alarm to time-box the headless resume
# (self-kills after 600s so a hung resume can't linger / eat RAM).
( perl -e 'alarm shift; exec @ARGV' 600 "$CLAUDE_BIN" --resume "$SID" -p "$NUDGE" >>"$ROOT/scripts/watchdog_nudge.log" 2>&1 ; log "  nudge finished rc=$?" ) &
exit 0
