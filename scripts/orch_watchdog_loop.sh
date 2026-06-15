#!/bin/bash
# Self-managed watchdog daemon (no launchctl needed). Runs the orchestration
# watchdog every 180s. Started via nohup so it survives the Claude turn. Stops
# itself when ~/afterhumans/.orch_done appears (orch_watchdog.sh handles that
# path too, but we also break the loop here).
ROOT="$HOME/afterhumans"
PIDFILE="$ROOT/scripts/watchdog_loop.pid"
echo $$ > "$PIDFILE"
while true; do
  /bin/bash "$ROOT/scripts/orch_watchdog.sh" 2>/dev/null
  [ -f "$ROOT/.orch_done" ] && { echo "[loop] .orch_done -> stop" >> "$ROOT/scripts/watchdog.log"; break; }
  sleep 90
done
rm -f "$PIDFILE"
