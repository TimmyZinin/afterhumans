#!/usr/bin/env python3
"""Generate NPC voice-over lines for Afterhumans via OpenRouter openai/gpt-audio.

Reads a TSV of dialogue lines and produces one normalized .ogg (Vorbis) per line,
ready for Unity WebGL import.

Usage:
    python3 gen_npc_voices.py <lines.tsv> <out_dir> [--only line_id1,line_id2]
                                                     [--force] [--target-lufs -16]

TSV columns (tab-separated, header required):
    line_id  npc  knot  voice  speed  text  [direction]

  voice  — either a gpt-audio voice (echo/ash/onyx/verse/coral/...) OR a
           character alias that maps to one via VOICE_MAP.
  speed  — playback tempo multiplier (applied via ffmpeg atempo). 1.0 = as-is.
  direction (optional) — intonation instruction fed into the TTS system prompt.
           If absent, a per-NPC default tone is used.

Pipeline per line:
  gpt-audio (stream, pcm16 @24kHz mono)
    -> ffmpeg atempo (if speed != 1.0)
    -> two-pass EBU R128 loudnorm (target -16 LUFS, TP -1.5 dBTP)
    -> Vorbis .ogg

Idempotent: skips a line whose .ogg already exists unless --force.
"""
import sys
import os
import json
import base64
import subprocess
import tempfile
import time
import argparse

API_URL = "https://openrouter.ai/api/v1/chat/completions"
MODEL = "openai/gpt-audio"
SAMPLE_RATE = 24000  # gpt-audio pcm16 native rate

GPT_AUDIO_VOICES = {
    "alloy", "ash", "ballad", "coral", "echo", "fable",
    "onyx", "nova", "sage", "shimmer", "verse", "cedar", "marin",
}

# Character aliases used in the legacy lines.tsv -> gpt-audio voice.
VOICE_MAP = {
    "dmitri": "onyx",   # Sasha / Stas — male
    "denis": "onyx",    # Nikolai — deep, tired male
    "ruslan": "echo",   # Kirill — calm male
    "irina": "coral",   # Mila — female
}

# Per-NPC default intonation when the TSV has no `direction` column.
NPC_DIRECTION = {
    "sasha": "нервно, задумчиво, слегка растерянно, как человек, не спавший три ночи",
    "mila": "сосредоточенно, интеллигентно, с лёгкой досадой на себя",
    "kirill": "медленно, спокойно, отрешённо, будто полушёпотом",
    "nikolai": "устало, тяжело, с горькой мудростью, размеренно",
    "stas": "быстро, возбуждённо, сбивчиво, с паранойей в голосе",
}
DEFAULT_DIRECTION = "живой разговорной интонацией, естественно"


def load_key():
    path = os.path.expanduser("~/.secrets/zinin-chat-openrouter.env")
    with open(path) as f:
        for line in f:
            if line.startswith("OPENROUTER_API_KEY"):
                return line.split("=", 1)[1].strip()
    raise SystemExit("no OPENROUTER_API_KEY in " + path)


def resolve_voice(raw):
    v = (raw or "").strip().lower()
    if v in GPT_AUDIO_VOICES:
        return v
    if v in VOICE_MAP:
        return VOICE_MAP[v]
    return "onyx"


def build_system_prompt(direction):
    return (
        "Ты озвучиваешь персонажа компьютерной игры. Прочитай реплику "
        "пользователя ДОСЛОВНО на русском языке. Интонация: " + direction + ". "
        "Не добавляй, не убирай и не меняй слова, не комментируй, "
        "не здоровайся — только чистая речь персонажа."
    )


def request_tts(key, voice, direction, text, retries=4):
    """Stream one line from gpt-audio. Returns (pcm_bytes, transcript, usage)."""
    import requests  # local import so --help works without the dep

    payload = {
        "model": MODEL,
        "modalities": ["text", "audio"],
        "audio": {"voice": voice, "format": "pcm16"},
        "stream": True,
        "usage": {"include": True},
        "messages": [
            {"role": "system", "content": build_system_prompt(direction)},
            {"role": "user", "content": text},
        ],
    }
    last_err = None
    for attempt in range(1, retries + 1):
        try:
            resp = requests.post(
                API_URL,
                headers={"Authorization": f"Bearer {key}",
                         "Content-Type": "application/json"},
                json=payload, stream=True, timeout=300,
            )
            if resp.status_code >= 500 or resp.status_code == 429:
                last_err = f"HTTP {resp.status_code}: {resp.text[:200]}"
                wait = min(2 ** attempt, 30)
                print(f"    retry {attempt}/{retries} after {wait}s ({last_err})")
                time.sleep(wait)
                continue
            if resp.status_code != 200:
                raise SystemExit(f"HTTP {resp.status_code}: {resp.text[:400]}")

            audio_parts, transcript_parts, usage = [], [], None
            for raw in resp.iter_lines(decode_unicode=True):
                if not raw or not raw.startswith("data: "):
                    continue
                data = raw[6:]
                if data.strip() == "[DONE]":
                    break
                try:
                    obj = json.loads(data)
                except json.JSONDecodeError:
                    continue
                if "error" in obj:
                    last_err = json.dumps(obj["error"])[:300]
                    raise RuntimeError(last_err)
                if obj.get("usage"):
                    usage = obj["usage"]
                for ch in obj.get("choices", []):
                    delta = ch.get("delta", {}) or {}
                    au = delta.get("audio") or {}
                    if au.get("data"):
                        audio_parts.append(au["data"])
                    if au.get("transcript"):
                        transcript_parts.append(au["transcript"])
                    msg = ch.get("message", {}) or {}
                    mau = msg.get("audio") or {}
                    if mau.get("data"):
                        audio_parts.append(mau["data"])
                    if mau.get("transcript"):
                        transcript_parts.append(mau["transcript"])

            full_b64 = "".join(audio_parts)
            if not full_b64:
                last_err = "no audio in response"
                wait = min(2 ** attempt, 30)
                print(f"    retry {attempt}/{retries} after {wait}s (no audio)")
                time.sleep(wait)
                continue
            pad = len(full_b64) % 4
            if pad:
                full_b64 += "=" * (4 - pad)
            return base64.b64decode(full_b64), "".join(transcript_parts), usage
        except (RuntimeError,) as e:
            last_err = str(e)
            wait = min(2 ** attempt, 30)
            print(f"    retry {attempt}/{retries} after {wait}s ({last_err})")
            time.sleep(wait)
        except Exception as e:  # network hiccup
            last_err = str(e)
            wait = min(2 ** attempt, 30)
            print(f"    retry {attempt}/{retries} after {wait}s ({last_err})")
            time.sleep(wait)
    raise SystemExit(f"gpt-audio failed after {retries} retries: {last_err}")


def measure_loudnorm(pcm_path, atempo, target_lufs, tp, lra):
    af = []
    if abs(atempo - 1.0) > 1e-3:
        af.append(f"atempo={atempo:.4f}")
    af.append(f"loudnorm=I={target_lufs}:TP={tp}:LRA={lra}:print_format=json")
    cmd = ["ffmpeg", "-hide_banner", "-f", "s16le", "-ar", str(SAMPLE_RATE),
           "-ac", "1", "-i", pcm_path, "-af", ",".join(af), "-f", "null", "-"]
    out = subprocess.run(cmd, capture_output=True, text=True).stderr
    # loudnorm json is the last {...} block in stderr
    start = out.rfind("{")
    end = out.rfind("}")
    if start == -1 or end == -1:
        raise RuntimeError("could not parse loudnorm pass-1 json")
    return json.loads(out[start:end + 1])


def encode_ogg(pcm_path, ogg_path, atempo, target_lufs, tp, lra, m):
    # This ffmpeg build ships only the broken native `vorbis` encoder, so we
    # normalize to WAV with ffmpeg and hand the WAV to oggenc (vorbis-tools).
    af = []
    if abs(atempo - 1.0) > 1e-3:
        af.append(f"atempo={atempo:.4f}")
    af.append(
        "loudnorm=I={I}:TP={tp}:LRA={lra}:measured_I={mi}:measured_TP={mtp}:"
        "measured_LRA={mlra}:measured_thresh={mth}:offset={off}:linear=true".format(
            I=target_lufs, tp=tp, lra=lra,
            mi=m["input_i"], mtp=m["input_tp"], mlra=m["input_lra"],
            mth=m["input_thresh"], off=m["target_offset"],
        )
    )
    # loudnorm's TP target leaks on short/tonal clips and lossy Vorbis adds
    # inter-sample overshoot, so hard-cap sample peak at -2 dBFS (0.794) to
    # guarantee true peak stays under -1 dBTP after encoding.
    af.append("alimiter=limit=0.794:level=false")
    with tempfile.NamedTemporaryFile(suffix=".wav", delete=False) as tf:
        wav_path = tf.name
    try:
        cmd = ["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
               "-f", "s16le", "-ar", str(SAMPLE_RATE), "-ac", "1", "-i", pcm_path,
               "-af", ",".join(af), "-ar", str(SAMPLE_RATE), "-ac", "1",
               "-c:a", "pcm_s16le", wav_path]
        r = subprocess.run(cmd, capture_output=True, text=True)
        if r.returncode != 0 or not os.path.exists(wav_path):
            raise RuntimeError("ffmpeg normalize failed: " + r.stderr[-400:])
        r2 = subprocess.run(["oggenc", "-Q", "-q", "5", "-o", ogg_path, wav_path],
                            capture_output=True, text=True)
        if r2.returncode != 0 or not os.path.exists(ogg_path):
            raise RuntimeError("oggenc failed: " + r2.stderr[-400:])
    finally:
        if os.path.exists(wav_path):
            os.unlink(wav_path)


def process_line(key, row, out_dir, force, target_lufs, tp, lra):
    line_id = row["line_id"]
    ogg_path = os.path.join(out_dir, line_id + ".ogg")
    if os.path.exists(ogg_path) and os.path.getsize(ogg_path) > 0 and not force:
        print(f"[skip] {line_id} (exists)")
        return None

    voice = resolve_voice(row.get("voice"))
    direction = (row.get("direction") or "").strip() or \
        NPC_DIRECTION.get(row.get("npc", "").strip(), DEFAULT_DIRECTION)
    try:
        speed = float(row.get("speed", "1.0") or "1.0")
    except ValueError:
        speed = 1.0
    text = row["text"].strip()

    print(f"[gen ] {line_id}  voice={voice}  speed={speed}  \"{text[:48]}...\"")
    pcm, transcript, usage = request_tts(key, voice, direction, text)

    with tempfile.NamedTemporaryFile(suffix=".pcm", delete=False) as tf:
        tf.write(pcm)
        pcm_path = tf.name
    try:
        m = measure_loudnorm(pcm_path, speed, target_lufs, tp, lra)
        encode_ogg(pcm_path, ogg_path, speed, target_lufs, tp, lra, m)
    finally:
        os.unlink(pcm_path)

    print(f"       -> {ogg_path}  ({os.path.getsize(ogg_path)} bytes)")
    return {"line_id": line_id, "usage": usage, "transcript": transcript,
            "voice": voice}


WHISPER_CLI = "whisper-cli"
WHISPER_MODEL = os.path.expanduser("~/whisper-models/ggml-small.bin")


def _norm_words(s):
    import re
    s = s.lower().replace("ё", "е")
    s = re.sub(r"[^\w\s]", " ", s, flags=re.UNICODE)
    return [w for w in s.split() if w]


def _wer(ref, hyp):
    r, h = _norm_words(ref), _norm_words(hyp)
    if not r:
        return 0.0 if not h else 1.0
    # Levenshtein distance over words
    prev = list(range(len(h) + 1))
    for i, rw in enumerate(r, 1):
        cur = [i]
        for j, hw in enumerate(h, 1):
            cur.append(min(prev[j] + 1, cur[j - 1] + 1,
                           prev[j - 1] + (rw != hw)))
        prev = cur
    return prev[-1] / len(r)


def stt(ogg_path):
    r = subprocess.run(
        [WHISPER_CLI, "-m", WHISPER_MODEL, "-l", "ru", "-nt", ogg_path],
        capture_output=True, text=True)
    return " ".join(r.stdout.split()).strip()


def verify_line(row, out_dir, threshold):
    line_id = row["line_id"]
    ogg_path = os.path.join(out_dir, line_id + ".ogg")
    if not os.path.exists(ogg_path):
        return {"line_id": line_id, "status": "MISSING", "wer": None}
    hyp = stt(ogg_path)
    wer = _wer(row["text"], hyp)
    # loudness
    j = subprocess.run(
        ["ffmpeg", "-hide_banner", "-i", ogg_path,
         "-af", "loudnorm=print_format=json", "-f", "null", "-"],
        capture_output=True, text=True).stderr
    lufs = tp = None
    s, e = j.rfind("{"), j.rfind("}")
    if s != -1 and e != -1:
        try:
            d = json.loads(j[s:e + 1])
            lufs, tp = float(d["input_i"]), float(d["input_tp"])
        except Exception:
            pass
    status = "OK" if wer <= threshold else "WER_HIGH"
    if lufs is not None and not (-20.0 <= lufs <= -14.0):
        status = "LOUDNESS_OUT"
    if tp is not None and tp > -1.0:
        status = "PEAK_HOT"
    return {"line_id": line_id, "status": status, "wer": wer,
            "lufs": lufs, "tp": tp, "hyp": hyp, "ref": row["text"]}


def read_tsv(path):
    with open(path, encoding="utf-8") as f:
        header = f.readline().rstrip("\n").split("\t")
        rows = []
        for line in f:
            if not line.strip():
                continue
            parts = line.rstrip("\n").split("\t")
            parts += [""] * (len(header) - len(parts))
            rows.append(dict(zip(header, parts)))
    return rows


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("tsv")
    ap.add_argument("out_dir")
    ap.add_argument("--only", default=None,
                    help="comma-separated line_ids to generate")
    ap.add_argument("--force", action="store_true")
    ap.add_argument("--target-lufs", type=float, default=-16.0)
    ap.add_argument("--tp", type=float, default=-1.5)
    ap.add_argument("--lra", type=float, default=11.0)
    ap.add_argument("--verify", action="store_true",
                    help="after generation, reverse-STT each line and report "
                         "WER + loudness")
    ap.add_argument("--verify-only", action="store_true",
                    help="skip generation, only verify existing .ogg files")
    ap.add_argument("--wer-threshold", type=float, default=0.15)
    args = ap.parse_args()

    os.makedirs(args.out_dir, exist_ok=True)
    rows = read_tsv(args.tsv)
    if args.only:
        want = set(args.only.split(","))
        rows = [r for r in rows if r["line_id"] in want]

    results = []
    if not args.verify_only:
        key = load_key()
        for row in rows:
            r = process_line(key, row, args.out_dir, args.force,
                             args.target_lufs, args.tp, args.lra)
            if r:
                results.append(r)

    if args.verify or args.verify_only:
        print("\n=== VERIFY (reverse-STT WER + loudness) ===")
        flagged = []
        for row in rows:
            v = verify_line(row, args.out_dir, args.wer_threshold)
            if v["wer"] is None:
                print(f"  {v['line_id']}: {v['status']}")
                flagged.append(v)
                continue
            wer_pct = f"{v['wer']*100:.0f}%"
            loud = (f"I={v['lufs']:.1f} TP={v['tp']:.2f}"
                    if v.get("lufs") is not None else "loud=?")
            print(f"  [{v['status']:11}] {v['line_id']}: WER={wer_pct:>4}  {loud}")
            if v["status"] != "OK":
                print(f"        ref: {v['ref']}")
                print(f"        stt: {v['hyp']}")
                flagged.append(v)
        print(f"\nverified: {len(rows)}  flagged: {len(flagged)}")
        if flagged:
            print("flagged line_ids: " + ",".join(f["line_id"] for f in flagged))

    # cost summary
    total_cost = 0.0
    print("\n=== USAGE / COST ===")
    for r in results:
        u = r.get("usage") or {}
        cost = u.get("cost")
        if cost is not None:
            total_cost += cost
        print(f"  {r['line_id']}: {json.dumps(u, ensure_ascii=False)}")
    if results:
        print(f"\ngenerated: {len(results)} lines")
        if total_cost:
            print(f"total cost: ${total_cost:.5f}  "
                  f"avg/line: ${total_cost/len(results):.5f}  "
                  f"projected 55 lines: ${total_cost/len(results)*55:.4f}")
    else:
        print("  (nothing generated)")


if __name__ == "__main__":
    main()
