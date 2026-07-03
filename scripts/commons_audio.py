#!/usr/bin/env python3
"""Search Wikimedia Commons for freely-licensed audio (no login).

Usage:
  commons_audio.py survey "<term>" [limit]      # list candidates + license
  commons_audio.py get "File:Name.ogg" <out.ogg> [--lufs -20] [--mono]

Prefers CC0 / Public Domain / CC-BY over CC-BY-SA. Prints license so the
caller records attribution in CREDITS.md.
"""
import sys
import json
import subprocess
import urllib.parse
import urllib.request

UA = "afterhumans-audio/1.0 (tim.zinin@gmail.com)"
API = "https://commons.wikimedia.org/w/api.php"

LICENSE_RANK = {
    "cc0": 0, "public domain": 0, "pd": 0, "no restrictions": 0,
    "cc by 4.0": 1, "cc by 3.0": 1, "cc by 2.5": 1, "cc by 2.0": 1,
    "cc by-sa 4.0": 3, "cc by-sa 3.0": 3, "cc by-sa 2.5": 3, "cc by-sa 2.0": 3,
}


def rank(lic):
    l = (lic or "").strip().lower()
    return LICENSE_RANK.get(l, 2)


def api(params):
    params = {**params, "format": "json"}
    url = API + "?" + urllib.parse.urlencode(params)
    req = urllib.request.Request(url, headers={"User-Agent": UA})
    return json.load(urllib.request.urlopen(req, timeout=30))


def survey(term, limit=8):
    data = api({
        "action": "query", "generator": "search",
        "gsrsearch": f"filetype:audio {term}", "gsrnamespace": "6",
        "gsrlimit": str(limit), "prop": "imageinfo",
        "iiprop": "url|mime|size|extmetadata",
    })
    pages = (data.get("query", {}) or {}).get("pages", {})
    rows = []
    for p in pages.values():
        ii = (p.get("imageinfo") or [{}])[0]
        em = ii.get("extmetadata", {}) or {}
        lic = (em.get("LicenseShortName", {}) or {}).get("value", "?")
        artist = (em.get("Artist", {}) or {}).get("value", "?")
        # strip html from artist
        import re
        artist = re.sub("<[^>]+>", "", artist).strip()
        dur = (em.get("Duration", {}) or {}).get("value", "?")
        rows.append({
            "title": p["title"], "url": ii.get("url"), "mime": ii.get("mime"),
            "license": lic, "artist": artist[:40], "duration": dur,
        })
    rows.sort(key=lambda r: rank(r["license"]))
    for r in rows:
        print(f"[{rank(r['license'])}] {r['license']:14} {r['mime']:16} "
              f"{r['title']}")
        print(f"      {r['url']}")
        print(f"      by: {r['artist']}  dur: {r['duration']}")


def get(title, out, lufs=-20.0, mono=False):
    data = api({
        "action": "query", "titles": title, "prop": "imageinfo",
        "iiprop": "url|mime|extmetadata",
    })
    p = list(data["query"]["pages"].values())[0]
    ii = p["imageinfo"][0]
    src_url = ii["url"]
    em = ii.get("extmetadata", {})
    lic = (em.get("LicenseShortName", {}) or {}).get("value", "?")
    import re
    artist = re.sub("<[^>]+>", "",
                    (em.get("Artist", {}) or {}).get("value", "?")).strip()
    # download
    raw = "/tmp/_commons_raw"
    req = urllib.request.Request(src_url, headers={"User-Agent": UA})
    with urllib.request.urlopen(req, timeout=120) as r, open(raw, "wb") as f:
        f.write(r.read())
    # normalize -> wav -> ogg
    af = f"loudnorm=I={lufs}:TP=-1.5:LRA=11,alimiter=limit=0.794:level=false"
    ac = ["-ac", "1"] if mono else []
    wav = "/tmp/_commons_norm.wav"
    subprocess.run(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
                    "-i", raw, "-af", af, "-ar", "44100", *ac,
                    "-c:a", "pcm_s16le", wav], check=True)
    subprocess.run(["oggenc", "-Q", "-q", "5", "-o", out, wav], check=True)
    print(f"WROTE {out}")
    print(f"SRC {src_url}")
    print(f"LICENSE {lic} | BY {artist}")


if __name__ == "__main__":
    if sys.argv[1] == "survey":
        survey(sys.argv[2], int(sys.argv[3]) if len(sys.argv) > 3 else 8)
    elif sys.argv[1] == "get":
        kw = {}
        args = sys.argv[4:]
        if "--lufs" in args:
            kw["lufs"] = float(args[args.index("--lufs") + 1])
        kw["mono"] = "--mono" in args
        get(sys.argv[2], sys.argv[3], **kw)
