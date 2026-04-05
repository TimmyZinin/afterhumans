# Бриф для Дениса — саунд-дизайн и озвучка Episode 0

> **Кому:** Денис Говорунов (@dgovorunov)
> **От:** Тим Зинин + Claude Code
> **Проект:** Послелюди / Afterhumans — narrative walker, Unity 6 URP, macOS + Windows standalone
> **Дата старта:** 2026-04-05
> **Дедлайн:** неделя (~2026-04-12), ship Episode 0 на `timzinin.com/afterhumans/`
> **Формат:** этот файл — source of truth, живёт в `github.com/TimmyZinin/afterhumans/blob/main/docs/DENIS_BRIEF.md`

---

## Денис, привет!

Мы с Тимом собираем **narrative walker** — 3D игра от первого лица, 10-15 минут прохождения, три сцены (**Ботаника** → **Город** → **Пустыня**), финальный момент у мигающего ASCII-курсора. Вся концепция, сюжет, персонажи и диалоги уже написаны и лежат в репо (детали ниже). Unity Editor сейчас качается на Mac Тима.

**Твоя задача — саунд: всё что звучит в игре, от ambient до голосов NPC.** Это критически важная часть опыта. Сейчас у нас 0 звуков. С хорошим саундом это будет работа уровня Sable/Firewatch. Без саунда — тишина.

**Мы хотим что-то, что заставит игроков охуеть через неделю.** Amount денег не главное, главное — крутой опыт, который люди будут шарить.

---

## Структура проекта и где что лежит

**Source of truth: репо** https://github.com/TimmyZinin/afterhumans (public). Клонируй или просто читай через web:

```
afterhumans/
├── docs/
│   ├── UNIVERSE.md        ← мир «Баг в алгоритме»
│   ├── STORY.md           ← сюжет сцена за сценой
│   ├── CHARACTERS.md      ← 12 персонажей с описанием
│   ├── ART_BIBLE.md       ← визуальное направление, аудио раздел 8
│   ├── GDD.md             ← геймплей
│   ├── TECH.md            ← технические детали
│   ├── SCOPE.md           ← scope lock
│   ├── PLAN.md            ← roadmap разработки
│   └── DENIS_BRIEF.md     ← этот файл
├── Assets/
│   ├── Dialogues/dataland.ink   ← полный скрипт диалогов
│   └── _Project/
│       ├── Scripts/        ← C# код (C++ мне не надо)
│       └── Audio/          ← СЮДА ТЫ КЛАДЁШЬ ВСЁ АУДИО
│           ├── Music/      ← 3 ambient track'а
│           ├── SFX/        ← SFX
│           └── VO/         ← озвучка NPC
└── README.md
```

**Порядок чтения перед началом:**
1. **`UNIVERSE.md`** (5 минут) — что за мир, что такое Прогноз, три локации, Кафка-корги
2. **`CHARACTERS.md`** (10 минут) — 12 персонажей: Саша, Мила, Кирилл, Николай, Стас, Дмитрий, Анна, Смотрительница, Ребёнок, Кафка, Сервер, Курсор. У каждого **речевой портрет** — как он говорит, тон, темп.
3. **`Assets/Dialogues/dataland.ink`** (15 минут) — реальные реплики всех NPC. Это то, что тебе нужно озвучить.
4. **`ART_BIBLE.md` раздел 8 «Аудио»** (5 минут) — аудио-направление, референсы, жанры.
5. **`STORY.md`** (10 минут) — опционально, если хочешь понять где какая сцена в последовательности.

---

## Твои задачи (по приоритету)

### 🔴 TIER 1 — КРИТИЧНО (без этого не шипнем)

#### Задача A — 3 Ambient Music Tracks

Один трек на каждую локацию. Каждый 4-6 минут, looping без швов.

| # | Локация | Жанр | Референсы | Длина | Ключ |
|---|---|---|---|---|---|
| 1 | **Ботаника** | Chill electronic ambient, lo-fi, тёплый | Nils Frahm, Tycho, Bonobo (early), Emancipator | 4-6 мин | Am или Em |
| 2 | **Город** | Ambient drone, почти тишина | Tim Hecker (minimal), Stars of the Lid, Eluvium | 4-5 мин | D drone |
| 3 | **Пустыня** | Cinematic, low brass drone + wordless female voices | Hans Zimmer *Dune 2021*: «Ripples in the Sand», «Visions of Chani», Jóhann Jóhannsson | 5-7 мин | Dm |

**Чем генерить:**
- 🟢 **Первый выбор: [Suno AI](https://suno.com/)** — text-to-music AI, бесплатный tier (10 generations/day), идеально для cinematic ambient. Промпт пример для пустыни: *«cinematic desert ambient in style of Hans Zimmer Dune, slow 50 bpm, wordless female choir, low brass drone, spacious, 3 minutes, no drums, minor key»*
- 🟡 **Второй выбор: [Udio](https://www.udio.com/)** — похоже на Suno, альтернативный tier
- 🟢 **Бесплатный CC0 готовый контент:**
  - [Free Music Archive](https://freemusicarchive.org/)
  - [Incompetech (Kevin MacLeod)](https://incompetech.com/)
  - [Pixabay Music](https://pixabay.com/music/) — CC0
- 🟢 **Самый artisanal путь:** запиши сам через любой бесплатный DAW (GarageBand on Mac, Cakewalk on Windows, Reaper trial). Pad synth + reverb = 80% треку.

#### Задача B — SFX Pack (~30 звуков)

**Ботаника:**
- `botanika_ambient_bed.ogg` — фоновая атмосфера (слои: растения шелестят, вода капает, хобот кофемашины)
- `coffee_machine_brewing.ogg` — кофемашина варит
- `coffee_machine_steam.ogg` — пар
- `turkish_coffee_bubbling.ogg` — турка Кирилла кипит
- `server_rack_hum.ogg` — серверная стойка гудит
- `paper_rustle.ogg` — Мила переворачивает страницы
- `sasha_coughing.ogg` — Саша кашляет (тихо, болен)
- `stas_soldering.ogg` — Стас паяет
- `door_creak.ogg` — дверь Ботаники в город

**Город:**
- `city_drone.ogg` — фоновый drone
- `fountain_quiet.ogg` — фонтан (приглушённый)
- `footsteps_stone.ogg` — шаги по плитке (4-6 вариантов для рандомизации)
- `distant_glitch.ogg` — редкий далёкий глитч (раз в 30 секунд в ambient)

**Пустыня:**
- `desert_wind_loop.ogg` — ветер
- `footsteps_sand.ogg` — шаги по песку (4-6 вариантов)
- `sand_shift.ogg` — песок сыпется
- `server_glitch_quiet.ogg` — глитч разрушенного сервера (тихий)
- `server_glitch_loud.ogg` — глитч громче (при E press)

**Кафка (корги):**
- `kafka_walk_wood.ogg` — Кафка идёт по дереву (Ботаника)
- `kafka_walk_stone.ogg` — идёт по камню (Город)
- `kafka_walk_sand.ogg` — идёт по песку (Пустыня)
- `kafka_bark_short.ogg` — короткий лай
- `kafka_growl_low.ogg` — тихое рычание (у сервера в пустыне)
- `kafka_sniff.ogg` — нюхает
- `kafka_tail_wag.ogg` — виляет хвостом (почти неслышно)
- `kafka_snort.ogg` — фыркает

**Финал:**
- `cursor_blink.ogg` — мигающий курсор (очень тихий pulse, loop)
- `cursor_submit.ogg` — звук когда игрок выбирает input
- `credits_sustain.ogg` — долгий синтезаторный аккорд для титров (~40 сек)

**UI:**
- `ui_dialogue_open.ogg` — открытие диалогового окна
- `ui_choice_hover.ogg` — наведение на выбор реплики
- `ui_choice_select.ogg` — выбор реплики
- `ui_transition.ogg` — переход между сценами (лёгкий whoosh)

**Чем генерить:**
- 🟢 **[Freesound.org](https://freesound.org/)** — огромная CC0 библиотека, нужен только аккаунт (бесплатный). Ищешь нужное, скачиваешь, редактируешь в Audacity.
- 🟢 **[Zapsplat](https://www.zapsplat.com/)** — требует free аккаунт, много SFX
- 🟢 **[Pixabay Sound Effects](https://pixabay.com/sound-effects/)** — CC0
- 🟢 **[BBC Sound Effects Library](https://sound-effects.bbcrewind.co.uk/)** — бесплатно для non-commercial; для нашего случая (бесплатная инди-игра) скорее всего ок
- 🟢 **Запись сама** — телефон + Audacity. Шаги, бумага, кашель, собака (если у тебя или друзей есть корги). Личные записи — самое аутентичное.
- 🟡 **ElFoundry / audio-tech** — AI SFX generators, некоторые с free tier

#### Задача C — Озвучка персонажей (Voice Over)

**Вариант MINIMUM (2-3 часа работы):**

Озвучь только **ключевые моменты** через free TTS:

1. **Записка на подлокотнике** (читается голосом — опционально):
   > *«Если ты это читаешь, значит Прогноз тебя потерял. Добро пожаловать в Ботанику. Не торопись. Тут ещё работает кофемашина.»*

2. **Николай — ключевой монолог про Прогноз** (~90 секунд текста, в `dataland.ink` knot `nikolai_monologue`). Это сердце игры. Если что-то одно озвучиваем — это.

3. **Анна — её воспоминание про Белку** (~60 секунд, knot `anna_memory`). Эмоциональный пик.

4. **Финальные тексты у Курсора** (5 вариантов × ~15 секунд = 75 секунд) — knot `ending_*`.

**Вариант FULL (~10 часов работы, если время):**

Озвучь всех 11 NPC + narrator для финальных текстов. Характер каждого персонажа в `CHARACTERS.md` раздел «Речевой портрет».

**Чем озвучивать (только бесплатное первым делом):**

1. 🟢 **[Microsoft Edge TTS](https://github.com/rany2/edge-tts)** — open-source обёртка над Azure neural voices. **Бесплатно, без лимитов, качество reference-уровня.** Русские голоса: `ru-RU-DmitryNeural` (мужской), `ru-RU-SvetlanaNeural` (женский). Установка: `pip install edge-tts`. Использование:
   ```bash
   edge-tts --voice "ru-RU-DmitryNeural" --text "Твой текст" --write-media output.mp3
   ```
2. 🟢 **[Piper TTS](https://github.com/rhasspy/piper)** — локальный neural TTS, офлайн, open source. Русские голоса есть. Устанавливается через brew/apt/pip.
3. 🟢 **[Coqui TTS](https://github.com/coqui-ai/TTS)** — open-source, multi-language, voice cloning. Сложнее в установке.
4. 🟢 **[Google Cloud TTS Free tier](https://cloud.google.com/text-to-speech)** — 1 млн chars/месяц бесплатно, WaveNet voices.
5. 🟢 **Crowdsource через сообщество** — если у тебя есть друзья с приятными голосами (особенно для Кирилла — низкий, медленный, бородатый), попроси их записать 5-10 реплик. Телефон + тихая комната + Audacity. **Это самый характерный путь.**
6. 🟡 **Сам запиши** — если твой голос подходит для кого-то (Стас — быстрый нервный, или Саша — бормочущий философ)
7. 🔶 **ElevenLabs** — Тим подтвердил доступ, API key отправлен тебе **лично в Telegram DM** (не в git — из соображений безопасности). Модель для русского: `eleven_multilingual_v2`. Voice recommendations для 8 NPC тоже в Telegram сообщении. Используй для финальных версий тех реплик, где edge-tts не вытягивает характер. **Первый проход — edge-tts бесплатно, финальная полировка — ElevenLabs.**

### 🟡 TIER 2 — Сильное усиление (если время после Tier 1)

#### Задача D — Трейлер / промо-видео

30-60 секунд видео для лендинга `timzinin.com/afterhumans/`. Понадобится **после того как Mac-билд будет готов**.

Что делаешь:
1. Я присылаю тебе через Telegram .dmg билд
2. Устанавливаешь на Mac (подожди — **у тебя Windows!**). Альтернатива: я пришлю **Windows .exe** билд (Unity собирает Windows на Mac host без проблем).
3. Записываешь геймплей через OBS (бесплатно, обе OS)
4. Монтируешь в DaVinci Resolve (бесплатно) или CapCut
5. Вкладываешь свой ambient track (из задачи A) как подложку
6. Цветокоррекция в закатных тонах
7. Добавляешь texts на ключевых моментах

Референс: [трейлеры Sable, Firewatch, What Remains of Edith Finch](https://youtu.be/qGwIEFKzdvo) — смотри, понимай tempo.

**Формат:** 1080p MP4, H.264, ~50 MB.

#### Задача E — Windows playtest

Когда я соберу Windows-билд, я отправлю тебе через Dropbox/Drive. Ты:
1. Устанавливаешь на свой Windows PC
2. Проходишь от начала до конца
3. Записываешь структурированный фидбэк:
   - FPS в каждой сцене
   - Crashes / visual glitches
   - Audio issues (если что-то пропало / обрезано)
   - Gameplay issues (что-то непонятно / заблокировано)
4. Отправляешь мне в Telegram, я фикшу

### 🟢 TIER 3 — Nice to have (полная коллаборация)

- **Marketing posts drafts**: Twitter thread, Reddit r/indiegames, dev.to статья «Как мы сделали игру за 7 дней»
- **Discord server setup** для будущего комьюнити Afterhumans
- **Landing page frontend**: если умеешь React/Next.js — premium лендинг вместо моего базового HTML
- **Мемы** в стиле Rick and Morty для viral promo

---

## Рабочий процесс и синхронизация

### Как общаемся

- **Основной канал: Telegram** — @dgovorunov ↔ @timofeyzinin
- **Я (Claude Code) могу читать и писать Telegram от имени Тима** через Telethon MCP. Тим может мне сказать «напиши Денису X» или «посмотри, что написал Денис» — я выполню.
- **Технические вопросы / баги / status updates** — пиши в Telegram в DM с Тимом, или в репо issues на GitHub

### Как передаёшь файлы

Есть три варианта, выбери удобный:

**Вариант 1 — через git (рекомендую если ты знаком с git)**
1. Форкни или клонируй `github.com/TimmyZinin/afterhumans`
2. Создай бранч `feature/audio-<твоёимя>`
3. Клади файлы в `Assets/_Project/Audio/Music/`, `Assets/_Project/Audio/SFX/`, `Assets/_Project/Audio/VO/`
4. Коммить, пуш, открой PR в main
5. Я мерджу + интегрирую в Unity

**Вариант 2 — через Google Drive / Dropbox (если git лень)**
1. Создай папку `Afterhumans_Audio_By_Denis`
2. Загрузи туда mp3/ogg/wav файлы
3. Дай мне ссылку в Telegram
4. Я скачиваю + коммичу в репо за тебя

**Вариант 3 — через Telegram**
1. Пакуешь файлы в zip
2. Шлёшь в DM мне/Тиму
3. Я разбираю и коммичу

### Формат именования файлов

```
{location}_{category}_{name}_{version}.{ext}
```

Примеры:
- `botanika_music_ambient_v1.ogg`
- `city_sfx_fountain_v1.wav`
- `desert_sfx_wind_loop_v2.ogg`
- `kafka_sfx_bark_short_v1.ogg`
- `vo_nikolai_monologue_v1.mp3`

**Предпочтительные форматы:**
- Music: `.ogg` (Vorbis) — Unity любит
- SFX: `.wav` (PCM 44.1kHz stereo) — лучшее качество
- VO: `.mp3` 192 kbps — компромисс размер/качество

### Milestone timeline

| День | Твой milestone |
|---|---|
| **День 1-2 (2026-04-05/06)** | Прочитать документы + прислать 1-2 first draft audio (можно SFX или начальный ambient Ботаники) — проверка регистра |
| **День 3** | TIER 1A + 1B — все 3 ambient + основной SFX пак |
| **День 4** | TIER 1C — озвучка минимум (Николай, Анна, финал) |
| **День 5** | Integration + playtest первого билда |
| **День 6-7** | Trailer + Windows playtest + polish |
| **День 7 (2026-04-12)** | **SHIP** |

---

## Технические требования

### Качество

- **Music:** 44.1 kHz / 16-bit / stereo, OGG Vorbis quality 6+ (192 kbps)
- **SFX:** 44.1 kHz / 16-bit / mono для point sources, stereo для ambient
- **VO:** 48 kHz / 16-bit / mono, normalized to -6 dB peak, remove silence at start/end, noise reduction applied

### Громкость

- **Music: baseline -18 LUFS** (чтобы не заглушало голоса)
- **SFX: peak -6 dB** (не clipping)
- **VO: baseline -14 LUFS** (разборчиво поверх music)

Unity Audio Mixer потом балансирует финально, так что не парься до идеала — мы поднимем/опустим при интеграции.

### Loop points

Для looping music и ambient — обеспечь seamless loop (нет щелчков на стыке). Audacity умеет crossfade первых/последних 100ms.

---

## Сердце проекта — что ты должен почувствовать

Игра про человека, который просыпается в странном месте (Ботаника — последний оазис живого в мире, где всё управляется алгоритмом под названием Прогноз). Там 5 странных персонажей, его собака Кафка (чёрно-белая корги-кардиган, 6 лет), которая всегда была рядом. Через холодный стерильный Город, где люди говорят data-фразами из двух слов, он выходит в пустыню. В центре пустыни — мигающий ASCII-курсор `> _`. Он вводит одно слово. Игра заканчивается.

**Настроение:**
- **Ботаника** = тёплое укрытие, прелое и живое, кофе в 4 утра, философия, смех
- **Город** = холодная, стерильная тишина, красота без души, жуткое спокойствие
- **Пустыня** = закат не двигается, вы идёте вдвоём с собакой через бесконечность, в конце одно слово решает всё

**Tone of dialogues** — Rick and Morty TV-MA: мат инструментально, алкоголь/наркотики/секс обсуждается openly, но всё это всегда поверх серьёзной мысли. Николай — тихий пьяный гений, Саша — бормочущий философ, Кирилл — серьёзный мистик с грибами, Стас — параноик, Мила — страстная упрямая. Кафка — просто рядом.

**Главный эмоциональный момент:** Кафка подходит к Анне (downgraded-human в городе), тычется носом в её колено, Анна впервые за годы вспоминает что у её сестры была такая же собака, и плачет от воспоминания. Музыка в этот момент — почти тишина, один нежный акцент.

Мы хотим чтобы игроки написали в отзывах *«эта игра заставила меня заплакать из-за собаки»*. Твой саунд делает 50% этого эффекта.

---

## Контакт

- **Тим**: @timofeyzinin (Telegram), `tim.zinin@gmail.com`
- **Claude Code** (я): пиши Тиму, он мне передаст, или я читаю Telegram Dialog между вами через Telethon
- **Repo**: https://github.com/TimmyZinin/afterhumans
- **Issues / bugs**: пиши в Telegram или открывай GitHub issue

---

**Погнали. Через неделю все охуеют от того, что мы сделали.**
