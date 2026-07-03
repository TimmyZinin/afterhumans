# Afterhumans — Audio Credits

Все ассеты — CC0 / Public Domain / CC-BY (без copyleft-обязательств для игры). Голоса NPC генерируются TTS (см. ниже), не входят в этот список.

Собран `scripts/build_audio_assets.py` из источников ниже. **Содержимое не проверено на слух** (среда без аудиовыхода) — перед релизом прослушать.


## Ambient

| Файл | Источник | Автор | Лицензия |
|---|---|---|---|
| greenhouse_ambient_loop.ogg | birds_garden + breeze_birds (mix) | Akum20 / ezwa | CC0 / PD |
| greenhouse_birds_loop.ogg | Birds chirping in a garden | Akum20 | **CC0** |
| kitchen_ambience_loop.ogg | Ambient Kitchen Sounds | Wilfredor | **CC0** |

## SFX

| Файл | Источник | Автор | Лицензия |
|---|---|---|---|
| kitchen_boiling_loop.ogg | Boiling lentils | hugh | Public domain |
| coffee_drip_loop.ogg | Drip coffee maker dripping | hugh | Public domain |
| door_creak_01.ogg | Door creaks (Knites) | Knites | **CC0** |
| door_creak_02.ogg | Door handle creaking | stephan | Public domain |
| dog_bark_01/02.ogg | 80 CC0 creature SFX (barking) | rubberduck | **CC0** |
| dog_sniff_01.ogg | 80 CC0 creature SFX (nose) | rubberduck | **CC0** |
| dog_sniff_02.ogg | 80 CC0 creature SFX (grunt) | rubberduck | **CC0** |
| dog_breath_01.ogg | 80 CC0 creature SFX (breath) | rubberduck | **CC0** |
| dog_growl_01.ogg | 80 CC0 creature SFX (grunt) | rubberduck | **CC0** |
| dog_yawn_01_APPROX.ogg | derived from breath.ogg (slowed/pitched) | rubberduck | **CC0** (derived) |
| dog_paw_wood_01..06.ogg | Different Steps (wood01-03, +pitch variants) | TinyWorlds | **CC0** |
| Dog/dog_sneeze_01.ogg | Sneeze (human, pitched +5 semitones) | Neo139 | **CC0** |
| Dog/dog_sneeze_02.ogg | Sneeze (human, pitched +8 semitones) | Neo139 | **CC0** |

## Music (Kevin MacLeod, CC-BY 4.0 — attribution REQUIRED)

Атрибуция обязательна в титрах, напр.: *«Anamalie» by Kevin MacLeod (incompetech.com), Licensed under Creative Commons: By Attribution 4.0.*

| Файл | Трек | Назначение | Лицензия |
|---|---|---|---|
| botanika_music_anamalie_loop.ogg | Anamalie | Ботаника (тёплый ambient) | CC-BY 4.0 |
| botanika_music_chillwave_loop.ogg | Chill Wave | Ботаника (альт) | CC-BY 4.0 |
| city_music_longnote_loop.ogg | Long Note Two | Город (ambient drone) | CC-BY 4.0 |

**Iron_Deadbolt.mp3** (уже лежал в Music/) — 13.7с, не подходит под фон 4-6 мин из ART_BIBLE; имя/длина не соответствуют «chill ambient warm». Оставлен, но как фон Ботаники не годится — заменён треками выше. Источник/лицензия Iron_Deadbolt неизвестны (не документированы) — проверить перед релизом.


## Source pages

- Akum20 — CC0 — https://commons.wikimedia.org/wiki/File:Birds_chirping_in_a_garden.ogg
- ezwa — Public domain — https://commons.wikimedia.org/wiki/File:Gentle_breeze_and_birds_singing.ogg
- hugh (freesound/Commons) — Public domain — https://commons.wikimedia.org/wiki/File:Boiling_lentils.ogg
- Wilfredor — CC0 — https://commons.wikimedia.org/wiki/File:Ambient_Kitchen_Sounds.wav
- hugh — Public domain — https://commons.wikimedia.org/wiki/File:Drip_coffee_maker_dripping.ogg
- Knites — CC0 — https://commons.wikimedia.org/wiki/File:LL-Q1860_(eng)-Knites-Sound_recording._Door_creaks.wav
- stephan (freesound/Commons) — Public domain — https://commons.wikimedia.org/wiki/File:Door_handle_creaking.ogg
- Neo139 — CC0 — https://commons.wikimedia.org/wiki/File:Sneeze.ogg
- rubberduck — CC0 — https://opengameart.org/content/80-cc0-creature-sfx
- TinyWorlds — CC0 — https://opengameart.org/content/different-steps-on-wood-stone-leaves-gravel-and-mud
- Kevin MacLeod (incompetech.com) — CC-BY 4.0 — https://incompetech.com/music/royalty-free/

## MISSING / нужно доснять или заменить перед релизом

- **Настоящий собачий зевок (yawn)** — CC0-записи не нашлось на Commons/OpenGameArt. Сейчас `dog_yawn_01_APPROX.ogg` — производная от breath.ogg (замедлена и понижена). Приемлемо как заглушка, но это не реальный зевок. Кандидаты: freesound.org (нужен логин).
- **Реалистичный собачий фоли** (bark/sniff/breath) сейчас из пака rubberduck — записаны голосом человека с фильтрами, стилизованные, не хай-фай. Для стилизованной игры ок; для реализма — freesound.
- **Хвост-виляние (tail wag / fabric brush)** и **server hum / glitch** (Город/Пустыня) — не входили в этот заход, не собраны.
- **Звук содержимого не верифицирован на слух** — среда без аудиовыхода. Проверить перед интеграцией в Unity.
- **Музыка выбрана по метаданным/названию, не по прослушке** — BPM/тональность (ART_BIBLE: 60-70 BPM, Am/Em) не измерены. Anamalie/Chill Wave заявлены как ambient/chill, но подтвердить на слух. Для Пустыни (Hans Zimmer Dune style) трека нет — ART_BIBLE предлагает генерацию через Suno.

## TTS-голоса NPC

Генерируются `scripts/gen_npc_voices.py` через OpenRouter `openai/gpt-audio` (проприетарная модель, лицензия — условия OpenAI/OpenRouter, не CC0). Это озвучка реплик, а не библиотечные ассеты. См. `_tts_probe/` для проб.
