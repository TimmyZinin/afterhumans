#!/usr/bin/env python3
"""Build the Afterhumans ambient + SFX pack from CC0 / public-domain sources.

Downloads freely-licensed source audio (Wikimedia Commons, OpenGameArt),
trims / seamless-loops / normalizes it, and lays it out under
Assets/_Project/Audio/{Ambient,SFX}/. Also (re)writes CREDITS.md.

Re-runnable and idempotent-ish: it overwrites outputs each run.

No login required for any source. Run from the repo root:
    python3 scripts/build_audio_assets.py
"""
import os
import subprocess
import tempfile
import urllib.request

ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
AUDIO = os.path.join(ROOT, "Assets", "_Project", "Audio")
AMBIENT = os.path.join(AUDIO, "Ambient")
SFX = os.path.join(AUDIO, "SFX")
CACHE = os.path.join(tempfile.gettempdir(), "ah_audio_src")
UA = "Mozilla/5.0 (afterhumans-audio/1.0; tim.zinin@gmail.com)"
MUSIC = os.path.join(AUDIO, "Music")

# name -> (url, license, author, source_page)
SOURCES = {
    "birds_garden.ogg": (
        "https://upload.wikimedia.org/wikipedia/commons/2/2f/Birds_chirping_in_a_garden.ogg",
        "CC0", "Akum20",
        "https://commons.wikimedia.org/wiki/File:Birds_chirping_in_a_garden.ogg"),
    "breeze_birds.ogg": (
        "https://upload.wikimedia.org/wikipedia/commons/0/0e/Gentle_breeze_and_birds_singing.ogg",
        "Public domain", "ezwa",
        "https://commons.wikimedia.org/wiki/File:Gentle_breeze_and_birds_singing.ogg"),
    "boiling_lentils.ogg": (
        "https://upload.wikimedia.org/wikipedia/commons/9/9c/Boiling_lentils.ogg",
        "Public domain", "hugh (freesound/Commons)",
        "https://commons.wikimedia.org/wiki/File:Boiling_lentils.ogg"),
    "kitchen_ambient.wav": (
        "https://upload.wikimedia.org/wikipedia/commons/8/83/Ambient_Kitchen_Sounds.wav",
        "CC0", "Wilfredor",
        "https://commons.wikimedia.org/wiki/File:Ambient_Kitchen_Sounds.wav"),
    "coffee_drip.ogg": (
        "https://upload.wikimedia.org/wikipedia/commons/b/bb/Drip_coffee_maker_dripping.ogg",
        "Public domain", "hugh",
        "https://commons.wikimedia.org/wiki/File:Drip_coffee_maker_dripping.ogg"),
    "door_creaks.wav": (
        "https://upload.wikimedia.org/wikipedia/commons/7/70/LL-Q1860_%28eng%29-Knites-Sound_recording._Door_creaks.wav",
        "CC0", "Knites",
        "https://commons.wikimedia.org/wiki/File:LL-Q1860_(eng)-Knites-Sound_recording._Door_creaks.wav"),
    "door_handle.ogg": (
        "https://upload.wikimedia.org/wikipedia/commons/7/74/Door_handle_creaking.ogg",
        "Public domain", "stephan (freesound/Commons)",
        "https://commons.wikimedia.org/wiki/File:Door_handle_creaking.ogg"),
    "sneeze_cc0.ogg": (
        "https://upload.wikimedia.org/wikipedia/commons/4/48/Sneeze.ogg",
        "CC0", "Neo139",
        "https://commons.wikimedia.org/wiki/File:Sneeze.ogg"),
    # OpenGameArt zips
    "creature.zip": (
        "https://opengameart.org/sites/default/files/80-CC0-creature-SFX_0.zip",
        "CC0", "rubberduck",
        "https://opengameart.org/content/80-cc0-creature-sfx"),
    "steps.zip": (
        "https://opengameart.org/sites/default/files/%5Bkdd%5DDifferentSteps_0.zip",
        "CC0", "TinyWorlds",
        "https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud"),
    # Music — Kevin MacLeod / incompetech.com, CC-BY 4.0 (attribution required)
    "Anamalie.mp3": (
        "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Anamalie.mp3",
        "CC-BY 4.0", "Kevin MacLeod (incompetech.com)",
        "https://incompetech.com/music/royalty-free/"),
    "Chill_Wave.mp3": (
        "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Chill%20Wave.mp3",
        "CC-BY 4.0", "Kevin MacLeod (incompetech.com)",
        "https://incompetech.com/music/royalty-free/"),
    "Long_Note_Two.mp3": (
        "https://incompetech.com/music/royalty-free/mp3-royaltyfree/Long%20Note%20Two.mp3",
        "CC-BY 4.0", "Kevin MacLeod (incompetech.com)",
        "https://incompetech.com/music/royalty-free/"),
}


def sh(cmd):
    r = subprocess.run(cmd, capture_output=True, text=True)
    if r.returncode != 0:
        raise RuntimeError(f"cmd failed: {' '.join(cmd)}\n{r.stderr[-500:]}")
    return r


def dur(path):
    r = sh(["ffprobe", "-v", "error", "-show_entries", "format=duration",
            "-of", "csv=p=0", path])
    return float(r.stdout.strip())


def fetch():
    os.makedirs(CACHE, exist_ok=True)
    for name, (url, *_ ) in SOURCES.items():
        dst = os.path.join(CACHE, name)
        if os.path.exists(dst) and os.path.getsize(dst) > 0:
            continue
        print("fetch", name)
        req = urllib.request.Request(url, headers={"User-Agent": UA})
        with urllib.request.urlopen(req, timeout=180) as r, open(dst, "wb") as f:
            f.write(r.read())
    # unzip OGA packs
    for z, sub in (("creature.zip", "creature"), ("steps.zip", "steps")):
        d = os.path.join(CACHE, sub)
        os.makedirs(d, exist_ok=True)
        sh(["unzip", "-o", "-q", os.path.join(CACHE, z), "-d", d])


def _norm_af(lufs):
    return (f"loudnorm=I={lufs}:TP=-1.5:LRA=11,"
            "alimiter=limit=0.794:level=false")


def encode(inp, out, lufs, mono, pre=""):
    """Normalize + encode to ogg (ffmpeg->wav->oggenc; native vorbis is broken)."""
    chain = (pre + "," if pre else "") + _norm_af(lufs)
    ac = ["-ac", "1"] if mono else ["-ac", "2"]
    wav = os.path.join(CACHE, "_tmp.wav")
    sh(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", inp,
        "-af", chain, "-ar", "44100", *ac, "-c:a", "pcm_s16le", wav])
    sh(["oggenc", "-Q", "-q", "5", "-o", out, wav])
    print("  ->", os.path.relpath(out, ROOT), f"({dur(out):.1f}s)")


def seamless(inp, out, x, lufs, mono, trim=None):
    """Seamless loop: crossfade tail into head so end==start (both at t=x)."""
    d = dur(inp)
    if trim:
        d = min(d, trim)
    fc = (
        f"[0:a]atrim=0:{d},asetpts=PTS-STARTPTS[t];"
        f"[t]asplit[h][m];"
        f"[h]atrim=0:{x},asetpts=PTS-STARTPTS[head];"
        f"[m]atrim={x}:{d},asetpts=PTS-STARTPTS[main];"
        f"[main][head]acrossfade=d={x}:c1=tri:c2=tri,"
        f"{_norm_af(lufs)}[out]"
    )
    ac = ["-ac", "1"] if mono else ["-ac", "2"]
    wav = os.path.join(CACHE, "_tmp.wav")
    sh(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", inp,
        "-filter_complex", fc, "-map", "[out]", "-ar", "44100", *ac,
        "-c:a", "pcm_s16le", wav])
    sh(["oggenc", "-Q", "-q", "5", "-o", out, wav])
    print("  ->", os.path.relpath(out, ROOT), f"({dur(out):.1f}s, seamless)")


def greenhouse():
    """Birds bed (150s) + breeze layered underneath -> seamless loop."""
    birds = os.path.join(CACHE, "birds_garden.ogg")
    breeze = os.path.join(CACHE, "breeze_birds.ogg")
    out = os.path.join(AMBIENT, "greenhouse_ambient_loop.ogg")
    L = 150.0
    x = 0.6
    # breeze is 32s -> tile with aloop to cover L, drop 6 dB so birds lead.
    fc = (
        f"[0:a]atrim=0:{L},asetpts=PTS-STARTPTS[birds];"
        f"[1:a]aloop=loop=-1:size=2147483647,atrim=0:{L},"
        f"asetpts=PTS-STARTPTS,volume=-7dB[wind];"
        f"[birds][wind]amix=inputs=2:duration=first:normalize=0[mix];"
        f"[mix]asplit[h][m];"
        f"[h]atrim=0:{x},asetpts=PTS-STARTPTS[head];"
        f"[m]atrim={x}:{L},asetpts=PTS-STARTPTS[main];"
        f"[main][head]acrossfade=d={x}:c1=tri:c2=tri,{_norm_af(-28)}[out]"
    )
    wav = os.path.join(CACHE, "_gh.wav")
    sh(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y",
        "-i", birds, "-i", breeze, "-filter_complex", fc, "-map", "[out]",
        "-ar", "44100", "-ac", "2", "-c:a", "pcm_s16le", wav])
    sh(["oggenc", "-Q", "-q", "5", "-o", out, wav])
    print("  ->", os.path.relpath(out, ROOT),
          f"({dur(out):.1f}s, birds+breeze seamless)")


def paw_variants():
    """3 wood steps -> 6 paw variants (3 raw + 3 pitch/tempo-shifted)."""
    steps = os.path.join(CACHE, "steps")
    variants = [
        ("wood01.ogg", "dog_paw_wood_01.ogg", ""),
        ("wood02.ogg", "dog_paw_wood_02.ogg", ""),
        ("wood03.ogg", "dog_paw_wood_03.ogg", ""),
        ("wood01.ogg", "dog_paw_wood_04.ogg", "asetrate=44100*1.12,aresample=44100"),
        ("wood02.ogg", "dog_paw_wood_05.ogg", "asetrate=44100*0.9,aresample=44100"),
        ("wood03.ogg", "dog_paw_wood_06.ogg", "asetrate=44100*1.06,aresample=44100"),
    ]
    for src, out, pre in variants:
        encode(os.path.join(steps, src), os.path.join(SFX, out), -20, True, pre)


def build_music(inp, out, lufs=-20.0, x=1.5):
    """Seamless-loop a full track and encode to ~128kbps ogg for a music bed."""
    d = dur(inp)
    fc = (
        f"[0:a]atrim=0:{d},asetpts=PTS-STARTPTS[t];[t]asplit[h][m];"
        f"[h]atrim=0:{x},asetpts=PTS-STARTPTS[head];"
        f"[m]atrim={x}:{d},asetpts=PTS-STARTPTS[main];"
        f"[main][head]acrossfade=d={x}:c1=tri:c2=tri,{_norm_af(lufs)}[out]"
    )
    wav = os.path.join(CACHE, "_mus.wav")
    sh(["ffmpeg", "-hide_banner", "-loglevel", "error", "-y", "-i", inp,
        "-filter_complex", fc, "-map", "[out]", "-ar", "44100", "-ac", "2",
        "-c:a", "pcm_s16le", wav])
    sh(["oggenc", "-Q", "-q", "4", "-o", out, wav])
    print("  ->", os.path.relpath(out, ROOT), f"({dur(out):.1f}s)")


def main():
    os.makedirs(AMBIENT, exist_ok=True)
    os.makedirs(SFX, exist_ok=True)
    os.makedirs(MUSIC, exist_ok=True)
    fetch()
    C = CACHE
    cr = os.path.join(C, "creature")

    print("\n# Ambient")
    greenhouse()
    seamless(os.path.join(C, "birds_garden.ogg"),
             os.path.join(AMBIENT, "greenhouse_birds_loop.ogg"),
             0.6, -28, False, trim=120)
    seamless(os.path.join(C, "kitchen_ambient.wav"),
             os.path.join(AMBIENT, "kitchen_ambience_loop.ogg"),
             0.6, -28, False, trim=45)

    print("\n# SFX — kitchen")
    seamless(os.path.join(C, "boiling_lentils.ogg"),
             os.path.join(SFX, "kitchen_boiling_loop.ogg"), 0.3, -20, True)
    seamless(os.path.join(C, "coffee_drip.ogg"),
             os.path.join(SFX, "coffee_drip_loop.ogg"), 0.4, -20, True, trim=20)

    print("\n# SFX — doors")
    encode(os.path.join(C, "door_creaks.wav"),
           os.path.join(SFX, "door_creak_01.ogg"), -20, True)
    encode(os.path.join(C, "door_handle.ogg"),
           os.path.join(SFX, "door_creak_02.ogg"), -20, True, "atrim=0:4")

    print("\n# SFX — dog")
    encode(os.path.join(cr, "barking_01.ogg"),
           os.path.join(SFX, "dog_bark_01.ogg"), -20, True)
    encode(os.path.join(cr, "barking_02.ogg"),
           os.path.join(SFX, "dog_bark_02.ogg"), -20, True)
    encode(os.path.join(cr, "nose.ogg"),
           os.path.join(SFX, "dog_sniff_01.ogg"), -20, True)
    encode(os.path.join(cr, "grunt_02.ogg"),
           os.path.join(SFX, "dog_sniff_02.ogg"), -20, True)
    encode(os.path.join(cr, "breath.ogg"),
           os.path.join(SFX, "dog_breath_01.ogg"), -20, True)
    encode(os.path.join(cr, "grunt_03.ogg"),
           os.path.join(SFX, "dog_growl_01.ogg"), -20, True)
    # yawn: no true CC0 yawn found -> approximate by slowing+lowering a breath.
    encode(os.path.join(cr, "breath.ogg"),
           os.path.join(SFX, "dog_yawn_01_APPROX.ogg"), -20, True,
           "asetrate=44100*0.72,aresample=44100,atempo=0.9")

    print("\n# SFX — paws")
    paw_variants()

    print("\n# SFX — dog sneeze (Dog/ subfolder, code matches 'sneeze')")
    dog_dir = os.path.join(SFX, "Dog")
    os.makedirs(dog_dir, exist_ok=True)
    sneeze = os.path.join(C, "sneeze_cc0.ogg")
    # human sneeze pitched up -> cute small-dog "pfchh"; two pitch levels.
    encode(sneeze, os.path.join(dog_dir, "dog_sneeze_01.ogg"), -20, True,
           "asetrate=44100*1.3348,aresample=44100")   # +5 semitones
    encode(sneeze, os.path.join(dog_dir, "dog_sneeze_02.ogg"), -20, True,
           "asetrate=44100*1.5874,aresample=44100")   # +8 semitones (tinier)

    print("\n# Music (Kevin MacLeod, CC-BY 4.0)")
    build_music(os.path.join(C, "Anamalie.mp3"),
                os.path.join(MUSIC, "botanika_music_anamalie_loop.ogg"))
    build_music(os.path.join(C, "Chill_Wave.mp3"),
                os.path.join(MUSIC, "botanika_music_chillwave_loop.ogg"))
    build_music(os.path.join(C, "Long_Note_Two.mp3"),
                os.path.join(MUSIC, "city_music_longnote_loop.ogg"))

    write_credits()


def write_credits():
    path = os.path.join(AUDIO, "CREDITS.md")
    lines = []
    lines.append("# Afterhumans — Audio Credits\n")
    lines.append("Все ассеты — CC0 / Public Domain / CC-BY (без copyleft-обязательств "
                 "для игры). Голоса NPC генерируются TTS (см. ниже), не входят в этот "
                 "список.\n")
    lines.append("Собран `scripts/build_audio_assets.py` из источников ниже. "
                 "**Содержимое не проверено на слух** (среда без аудиовыхода) — "
                 "перед релизом прослушать.\n")

    lines.append("\n## Ambient\n")
    lines.append("| Файл | Источник | Автор | Лицензия |")
    lines.append("|---|---|---|---|")
    lines.append("| greenhouse_ambient_loop.ogg | birds_garden + breeze_birds (mix) | Akum20 / ezwa | CC0 / PD |")
    lines.append("| greenhouse_birds_loop.ogg | Birds chirping in a garden | Akum20 | **CC0** |")
    lines.append("| kitchen_ambience_loop.ogg | Ambient Kitchen Sounds | Wilfredor | **CC0** |")

    lines.append("\n## SFX\n")
    lines.append("| Файл | Источник | Автор | Лицензия |")
    lines.append("|---|---|---|---|")
    lines.append("| kitchen_boiling_loop.ogg | Boiling lentils | hugh | Public domain |")
    lines.append("| coffee_drip_loop.ogg | Drip coffee maker dripping | hugh | Public domain |")
    lines.append("| door_creak_01.ogg | Door creaks (Knites) | Knites | **CC0** |")
    lines.append("| door_creak_02.ogg | Door handle creaking | stephan | Public domain |")
    lines.append("| dog_bark_01/02.ogg | 80 CC0 creature SFX (barking) | rubberduck | **CC0** |")
    lines.append("| dog_sniff_01.ogg | 80 CC0 creature SFX (nose) | rubberduck | **CC0** |")
    lines.append("| dog_sniff_02.ogg | 80 CC0 creature SFX (grunt) | rubberduck | **CC0** |")
    lines.append("| dog_breath_01.ogg | 80 CC0 creature SFX (breath) | rubberduck | **CC0** |")
    lines.append("| dog_growl_01.ogg | 80 CC0 creature SFX (grunt) | rubberduck | **CC0** |")
    lines.append("| dog_yawn_01_APPROX.ogg | derived from breath.ogg (slowed/pitched) | rubberduck | **CC0** (derived) |")
    lines.append("| dog_paw_wood_01..06.ogg | Different Steps (wood01-03, +pitch variants) | TinyWorlds | **CC0** |")
    lines.append("| Dog/dog_sneeze_01.ogg | Sneeze (human, pitched +5 semitones) | Neo139 | **CC0** |")
    lines.append("| Dog/dog_sneeze_02.ogg | Sneeze (human, pitched +8 semitones) | Neo139 | **CC0** |")

    lines.append("\n## Music (Kevin MacLeod, CC-BY 4.0 — attribution REQUIRED)\n")
    lines.append("Атрибуция обязательна в титрах, напр.: "
                 "*«Anamalie» by Kevin MacLeod (incompetech.com), "
                 "Licensed under Creative Commons: By Attribution 4.0.*\n")
    lines.append("| Файл | Трек | Назначение | Лицензия |")
    lines.append("|---|---|---|---|")
    lines.append("| botanika_music_anamalie_loop.ogg | Anamalie | Ботаника (тёплый ambient) | CC-BY 4.0 |")
    lines.append("| botanika_music_chillwave_loop.ogg | Chill Wave | Ботаника (альт) | CC-BY 4.0 |")
    lines.append("| city_music_longnote_loop.ogg | Long Note Two | Город (ambient drone) | CC-BY 4.0 |")
    lines.append("\n**Iron_Deadbolt.mp3** (уже лежал в Music/) — 13.7с, не подходит "
                 "под фон 4-6 мин из ART_BIBLE; имя/длина не соответствуют "
                 "«chill ambient warm». Оставлен, но как фон Ботаники не годится — "
                 "заменён треками выше. Источник/лицензия Iron_Deadbolt неизвестны "
                 "(не документированы) — проверить перед релизом.\n")

    lines.append("\n## Source pages\n")
    seen = set()
    for name, (url, lic, author, page) in SOURCES.items():
        if page in seen:
            continue
        seen.add(page)
        lines.append(f"- {author} — {lic} — {page}")

    lines.append("\n## MISSING / нужно доснять или заменить перед релизом\n")
    lines.append("- **Настоящий собачий зевок (yawn)** — CC0-записи не нашлось на "
                 "Commons/OpenGameArt. Сейчас `dog_yawn_01_APPROX.ogg` — производная "
                 "от breath.ogg (замедлена и понижена). Приемлемо как заглушка, но "
                 "это не реальный зевок. Кандидаты: freesound.org (нужен логин).")
    lines.append("- **Реалистичный собачий фоли** (bark/sniff/breath) сейчас из пака "
                 "rubberduck — записаны голосом человека с фильтрами, стилизованные, "
                 "не хай-фай. Для стилизованной игры ок; для реализма — freesound.")
    lines.append("- **Хвост-виляние (tail wag / fabric brush)** и **server hum / "
                 "glitch** (Город/Пустыня) — не входили в этот заход, не собраны.")
    lines.append("- **Звук содержимого не верифицирован на слух** — среда без "
                 "аудиовыхода. Проверить перед интеграцией в Unity.")
    lines.append("- **Музыка выбрана по метаданным/названию, не по прослушке** — "
                 "BPM/тональность (ART_BIBLE: 60-70 BPM, Am/Em) не измерены. "
                 "Anamalie/Chill Wave заявлены как ambient/chill, но подтвердить "
                 "на слух. Для Пустыни (Hans Zimmer Dune style) трека нет — "
                 "ART_BIBLE предлагает генерацию через Suno.")

    lines.append("\n## TTS-голоса NPC\n")
    lines.append("Генерируются `scripts/gen_npc_voices.py` через OpenRouter "
                 "`openai/gpt-audio` (проприетарная модель, лицензия — условия "
                 "OpenAI/OpenRouter, не CC0). Это озвучка реплик, а не библиотечные "
                 "ассеты. См. `_tts_probe/` для проб.")
    lines.append("")

    with open(path, "w", encoding="utf-8") as f:
        f.write("\n".join(lines))
    print("\nwrote", os.path.relpath(path, ROOT))


if __name__ == "__main__":
    main()
