#!/usr/bin/env python3
"""List previously generated Tripo tasks (account history) so we can reuse ready models
without spending new credit. Tries the documented list endpoints and prints id/type/status/prompt."""
import json, os, sys, urllib.request, urllib.error

API_BASE = "https://api.tripo3d.ai/v2/openapi"

def load_api_key():
    env_path = os.path.abspath(os.path.join(os.path.dirname(__file__), "..", ".env"))
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line.startswith("TRIPO_API_KEY="):
                    return line.split("=", 1)[1].strip().strip('"').strip("'")
    return os.environ.get("TRIPO_API_KEY")

def get(endpoint, key):
    url = f"{API_BASE}/{endpoint}"
    req = urllib.request.Request(url, headers={"Authorization": f"Bearer {key}"}, method="GET")
    try:
        with urllib.request.urlopen(req, timeout=60) as r:
            return r.status, json.loads(r.read().decode())
    except urllib.error.HTTPError as e:
        return e.code, e.read().decode()
    except Exception as e:
        return -1, str(e)

key = load_api_key()
if not key:
    print("NO KEY"); sys.exit(1)

# try several possible history/list endpoints
for ep in ["task", "tasks", "user/tasks", "task/list", "history", "user/balance", "balance"]:
    code, body = get(ep, key)
    print(f"\n===== GET /{ep} -> {code} =====")
    if isinstance(body, dict):
        print(json.dumps(body, ensure_ascii=False)[:3000])
    else:
        print(str(body)[:1500])
