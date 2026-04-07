#!/usr/bin/env python3
"""
Tripo3D API client: generate 3D model from text prompt.
Supports static and animated models.

Usage:
  python3 scripts/tripo_generate.py --prompt "wooden chair" --name chair_01
  python3 scripts/tripo_generate.py --prompt "gray cat" --name cat --animated --animation-preset walk
"""
import argparse
import json
import os
import sys
import time
import urllib.request
import urllib.error

API_BASE = "https://api.tripo3d.ai/v2/openapi"


def load_api_key():
    """Load TRIPO_API_KEY from .env file."""
    env_path = os.path.join(os.path.dirname(__file__), "..", ".env")
    env_path = os.path.abspath(env_path)
    if os.path.exists(env_path):
        with open(env_path) as f:
            for line in f:
                line = line.strip()
                if line.startswith("TRIPO_API_KEY="):
                    return line.split("=", 1)[1].strip().strip('"').strip("'")
    # Fallback to environment variable
    key = os.environ.get("TRIPO_API_KEY")
    if not key:
        print("ERROR: TRIPO_API_KEY not found in .env or environment")
        sys.exit(1)
    return key


def api_request(method, endpoint, api_key, data=None):
    """Make API request to Tripo3D."""
    url = f"{API_BASE}/{endpoint}"
    headers = {
        "Authorization": f"Bearer {api_key}",
        "Content-Type": "application/json",
    }
    body = json.dumps(data).encode() if data else None
    req = urllib.request.Request(url, data=body, headers=headers, method=method)

    try:
        with urllib.request.urlopen(req, timeout=60) as resp:
            return json.loads(resp.read().decode())
    except urllib.error.HTTPError as e:
        error_body = e.read().decode() if e.fp else ""
        print(f"API Error {e.code}: {error_body}")
        sys.exit(1)


def create_model_task(api_key, prompt):
    """Create text-to-model generation task."""
    data = {
        "type": "text_to_model",
        "prompt": prompt,
        "model_version": "v2.0-20240919",
        "face_limit": 100000,
        "texture": True,
        "pbr": True,
    }
    print(f"[1] Creating model task: '{prompt[:60]}...'")
    result = api_request("POST", "task", api_key, data)
    task_id = result["data"]["task_id"]
    print(f"    Task ID: {task_id}")
    return task_id


def create_animation_task(api_key, model_task_id, preset):
    """Create animation task for an existing model."""
    data = {
        "type": "animate_model",
        "original_model_task_id": model_task_id,
        "animation": {
            "mode": "preset",
            "preset": preset,
        },
    }
    print(f"[3] Creating animation task: preset='{preset}'")
    result = api_request("POST", "task", api_key, data)
    task_id = result["data"]["task_id"]
    print(f"    Task ID: {task_id}")
    return task_id


def poll_task(api_key, task_id, label=""):
    """Poll task until completion."""
    start = time.time()
    interval = 5
    while True:
        result = api_request("GET", f"task/{task_id}", api_key)
        status = result["data"]["status"]
        elapsed = time.time() - start
        print(f"    [{label}] {elapsed:.0f}s — status: {status}")

        if status == "success":
            return result["data"]
        elif status == "failed":
            print(f"ERROR: Task failed: {json.dumps(result, indent=2)}")
            sys.exit(1)

        # Adaptive polling: faster at start, slower later
        if elapsed < 30:
            interval = 5
        elif elapsed < 120:
            interval = 10
        else:
            interval = 15

        if elapsed > 600:
            print("ERROR: Task timed out (>10 min)")
            sys.exit(1)

        time.sleep(interval)


def download_model(api_key, task_id, output_path, fmt="fbx"):
    """Download generated model file."""
    url = f"{API_BASE}/task/{task_id}/download?type={fmt}"
    headers = {"Authorization": f"Bearer {api_key}"}
    req = urllib.request.Request(url, headers=headers)

    print(f"[D] Downloading {fmt.upper()}...")
    try:
        with urllib.request.urlopen(req, timeout=120) as resp:
            data = resp.read()
            os.makedirs(os.path.dirname(output_path), exist_ok=True)
            with open(output_path, "wb") as f:
                f.write(data)
            size_mb = len(data) / 1024 / 1024
            print(f"    Saved: {output_path} ({size_mb:.1f} MB)")
            return output_path
    except urllib.error.HTTPError as e:
        print(f"Download error {e.code}: {e.read().decode()}")
        sys.exit(1)


def main():
    parser = argparse.ArgumentParser(description="Tripo3D model generator")
    parser.add_argument("--prompt", required=True, help="Text description")
    parser.add_argument("--name", required=True, help="Model name (no spaces)")
    parser.add_argument("--animated", action="store_true", help="Generate with animation")
    parser.add_argument("--animation-preset", default="walk", choices=["walk", "run", "idle"])
    parser.add_argument("--output", default="Assets/_Project/Models/Generated/", help="Output directory")
    parser.add_argument("--format", default="fbx", choices=["fbx", "glb"])
    args = parser.parse_args()

    api_key = load_api_key()
    output_dir = os.path.join(os.path.dirname(__file__), "..", args.output)
    output_dir = os.path.abspath(output_dir)

    # Step 1: Generate model
    model_task_id = create_model_task(api_key, args.prompt)

    # Step 2: Poll model generation
    print("[2] Waiting for model generation...")
    model_data = poll_task(api_key, model_task_id, "model")

    if args.animated:
        # Step 3: Create animation
        anim_task_id = create_animation_task(api_key, model_task_id, args.animation_preset)

        # Step 4: Poll animation
        print("[4] Waiting for animation...")
        anim_data = poll_task(api_key, anim_task_id, "anim")

        # Download animated model
        download_task_id = anim_task_id
    else:
        download_task_id = model_task_id

    # Step 5: Download
    ext = args.format
    output_path = os.path.join(output_dir, f"{args.name}_raw.{ext}")
    download_model(api_key, download_task_id, output_path, ext)

    print(f"\n=== DONE ===")
    print(f"Model: {output_path}")
    print(f"Animated: {args.animated}")
    if args.animated:
        print(f"Animation: {args.animation_preset}")
    print(f"Next: process_model.py --input {output_path}")


if __name__ == "__main__":
    main()
