# Передача контекста — Afterhumans Session Transfer

> Дата: 2026-04-06 ~10:00
> От: Claude Opus 4.6 (основная сессия, 12 спринтов)
> Кому: новая сессия Claude Code с Computer Use

---

## Что это за проект

**Послелюди / Afterhumans** — narrative walker от первого лица на Unity 6 URP. Серийный проект, Episode 0. Мир Homo Deus + Dune + Ботаника. R&M-диалоги. macOS Apple Silicon standalone.

- **Repo:** `~/afterhumans/` (GitHub: TimmyZinin/afterhumans, public)
- **Engine:** Unity 6 LTS (6000.0.72f1), URP 17.0.4
- **Target:** macOS M1 8GB, Universal Binary, ad-hoc codesign
- **Язык игры:** русский
- **Длительность демо:** 10-15 минут, 3 сцены (Ботаника → Город → Пустыня)
- **Текущий scope:** ТОЛЬКО первая сцена Ботаника (выдрочить до блеска)

## Текущее состояние

### Что сделано (12 спринтов, 47 задач в 5 layers)

| Layer | Задачи | Статус |
|---|---|---|
| **F Foundation** (10) | URP activation, Kenney scale fix, AudioMixer, Input System, SceneTheme SO, BoxCollider helper, Perf profiler, Interactable cache, Debug HUD gate, Static batching | 10/10 DONE |
| **A Art Direction** (9) | VolumeProfile post-FX, HDRI skybox, env props (server rack+LEDs, graffiti, NPC stations), glass ceiling+dust particles, accent lights, window glass overlays, palette from SceneTheme, baked GI lightmap | 9/9 DONE |
| **N NPC** (9) | Kenney blocky-characters, procedural idle animation, 5 NPC spawn, hover prompts, speaker name prefix, typewriter 22cps+skip, Nikolai facing | 8/9 DONE (N04 Kafka mesh deferred) |
| **S Narrative** (11) | Wake-up cinematic, first-look pan, note interactable, 5 knots wire, door cue UI, ambient audio, footsteps, chapter indicator | 8/11 DONE (S10/S11 P2) |
| **T Test Harness** (10) | BotanikaVerification master 23/23 criteria PASS | DONE via T01 |
| **QA Bug Fixes** (4) | Escape cursor, focus loss, camera skybox, Metal fullscreen | 4/4 DONE, mm-review APPROVED |

### Verification: 23/23 PASS

Все 23 AAA criteria проходят через `BotanikaVerification.RunAll`:
```bash
Unity -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BotanikaVerification.RunAll
```

### Последний билд

- **Коммит:** `7570a3a` (pushed to GitHub)
- **Build:** `~/afterhumans/build/Afterhumans.app` — 125MB Universal Binary
- **Запуск:** `open ~/afterhumans/build/Afterhumans.app`
- **Windowed mode:** 1280x720 default (для совместимости с screen capture)

## Что НЕ работает — результаты QA playtest

Первый QA playtest (`docs/TEST_REPORT.md`) выявил что **игра запускается, но полный gameplay loop не протестирован:**

### Протестировано и работает
- Игра запускается без crash
- Кинематик играет (камера двигается 18 секунд)
- WASD двигает игрока
- Tutorial overlay появляется
- Тёплая палитра видна
- Player.log чистый (0 ошибок)

### НЕ протестировано (нужна твоя работа)
- Диалоги с NPC (E press → dialogue panel → typewriter → choices)
- Hover prompts "[E] говорить" при подходе к NPC
- Все 5 NPC (Саша/Мила/Кирилл/Николай/Стас) доступны и отвечают
- Gate cue "Дверь открыта" после Николая
- Переход в Scene_City через дверь
- Визуальные детали (пылинки, graffiti, server rack, Kafka)
- Камера теперь смотрит в комнату после cinematic (fix применён, не проверен)

## Что делать дальше

### Приоритет 1: Полный playtest через Computer Use
1. Запустить свежий билд
2. Дождаться конца кинематика (18с)
3. Проверить камера смотрит в комнату (баг 3 фикс)
4. WASD → дойти до Саши → E → проверить диалог
5. Обойти всех 5 NPC
6. Проверить door cue после Николая
7. Записать новый TEST_REPORT

### Приоритет 2: Исправление найденных багов
- Каждый баг фиксить, пересобирать (`BuildScript.BuildMacOS`), перетестировать
- Протокол: /self-adversarial → fix → build → test → /mm-review

### Приоритет 3: Доработки визуала
Если gameplay loop работает, улучшать визуал:
- Kafka: заменить 2-cube placeholder на нормальную модель
- NPC: сейчас Kenney blocky characters — низкополигональные но различимые
- Аудио: сейчас процедурные placeholder (sine tones) — заменить на CC0 (Suno AI для музыки, Freesound для SFX)
- Graffiti: может быть отзеркалено (был баг в предыдущих скриншотах)

### Приоритет 4: Оставшиеся задачи
- N04: Kafka improved mesh
- S10: Nikolai audio sting (P2)
- S11: Stas-Kafka feeding event (P2)
- Runtime performance validation (FPS >= 40 на M1 8GB)

## Протокол работы (установлен Тимом)

1. **Перед каждым спринтом:** `/self-adversarial` — adversarial critique подхода
2. **После каждого спринта:** `/mm-review` — MiniMax M2.5 даёт однозначный APPROVED/NEEDS_CHANGES
3. **Агенты запрещены** для этой задачи (только Claude + skills)
4. **10 gamedev skills** используются: 3d-games, game-art, game-audio, game-design, game-development, ui-ux-pro-max, frontend-design, scroll-experience, theme-factory, 3d-web-experience
5. **Коммиты:** после каждого спринта, push в GitHub
6. **Язык:** общение на русском, код на английском

## Как пересобрать игру

```bash
UNITY="$HOME/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity"

# 1. Пересобрать сцену (все layers)
$UNITY -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BotanikaDresser.Dress

# 2. Verification 23/23
$UNITY -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BotanikaVerification.RunAll

# 3. Build .app
$UNITY -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BuildScript.BuildMacOS

# 4. Запуск
open ~/afterhumans/build/Afterhumans.app
```

## Ключевые файлы

### Editor scripts (пересборка сцены)
| Файл | Что делает |
|---|---|
| `Editor/BotanikaDresser.cs` | Главный dresser — greenhouse shell + вызывает все sub-layers |
| `Editor/BotanikaEnvProps.cs` | 7 diegetic prop groups (server rack, graffiti, NPC stations) |
| `Editor/BotanikaAtmosphere.cs` | Glass ceiling + dust particles + accent lights + window overlays |
| `Editor/BotanikaNpcPopulator.cs` | 5 NPC spawn + note + door cue + chapter + intro + audio |
| `Editor/BotanikaLightingBaker.cs` | Baked GI lightmap |
| `Editor/BotanikaSkyboxBuilder.cs` | HDRI → Skybox material → SceneTheme wire |
| `Editor/VolumeProfileBuilder.cs` | Post-FX VolumeProfile (Bloom/ACES/Grain/DoF etc) |
| `Editor/BotanikaVerification.cs` | 23 criteria AAA verification |
| `Editor/BuildScript.cs` | macOS .app build |

### Runtime scripts
| Файл | Что делает |
|---|---|
| `Scripts/Player/SimpleFirstPersonController.cs` | WASD + mouse look + cursor lock |
| `Scripts/Player/PlayerInteraction.cs` | E press → closest Interactable → dialogue |
| `Scripts/Dialogue/DialogueManager.cs` | Ink story singleton, knot management |
| `Scripts/Dialogue/DialogueUI.cs` | Canvas UI, typewriter, speaker name, choices |
| `Scripts/Dialogue/Interactable.cs` | NPC/note trigger, static cache |
| `Scripts/Scenes/BotanikaIntroDirector.cs` | 18s wake-up cinematic coroutine |
| `Scripts/Art/NpcIdleBob.cs` | Procedural idle animation |
| `Scripts/Art/NpcFacing.cs` | Nikolai turn-to-player on interact |
| `Scripts/Art/BlinkingLight.cs` | Server rack LED oscillator |
| `Scripts/Art/InteractionPromptUI.cs` | Worldspace "[E] говорить" hover |
| `Scripts/Art/SceneTheme.cs` | Data-driven palette ScriptableObject |
| `Scripts/Art/ThemeLoader.cs` | Runtime theme applier |
| `Scripts/Audio/FootstepController.cs` | Procedural footstep sounds |
| `Scripts/Audio/BotanikaAmbientAudio.cs` | Ambient drone loop |
| `Scripts/UI/DoorCueUI.cs` | "Дверь открыта" subtitle after Николай |
| `Scripts/UI/ChapterIndicatorUI.cs` | "I. Ботаника" fade-in title |

### Документы
| Файл | Что внутри |
|---|---|
| `docs/ART_BIBLE.md` | Визуальное направление, палитры, освещение, post-FX |
| `docs/STORY.md` | Сценарий, 3 акта, NPC диалоги |
| `docs/CHARACTERS.md` | 5 NPC + Kafka описания |
| `docs/GDD.md` | Game Design Document |
| `docs/UNIVERSE.md` | Лор мира |
| `docs/TEST_REPORT.md` | Первый QA playtest результат |
| `docs/QA_PLAYTEST_PROMPT.md` | Промпт для QA тестирования через Computer Use |
| `Assets/Dialogues/dataland.ink` | 593 строки Ink диалогов (5 NPC knots) |

### Assets
| Путь | Что |
|---|---|
| `Assets/_Project/Vendor/Kenney/` | 510+ FBX (furniture, nature, city, blocky-characters) — gitignored, скачать через `scripts/download-assets.sh` |
| `Assets/_Project/Vendor/PolyHaven/` | HDRI kloppenheim_06_puresky_2k.hdr — gitignored, скачать через download-assets.sh |
| `Assets/_Project/Art/Themes/Botanika.asset` | SceneTheme с палитрой ART_BIBLE §3.1 |
| `Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika.asset` | Post-FX stack (9 overrides) |
| `Assets/_Project/Settings/Lighting_Botanika.lighting` | Baked GI settings |
| `Assets/_Project/Materials/Tints/` | URP/Lit tinted materials |
| `Assets/_Project/Materials/Skyboxes/` | Skybox + Glass + DustMote materials |

## Сценарий Ботаники (STORY.md §3)

1. **Wake-up (0-18с):** Камера pan по оранжерее. Кафка рядом. Записка на столе.
2. **Записка:** E → читает 4 строки ("Ты проснулся. Кафка рядом.")
3. **Tutorial:** WASD/Mouse/E/Shift overlay 5 секунд
4. **Саша (диван):** философ, 60 строк, сидит на диване
5. **Мила (стол):** манифест, 61 строка, за столом с ноутбуком
6. **Кирилл (кухня):** грибы+кофе, 66 строк, у плиты
7. **Николай (угол):** data-жрец, 88 строк, поворачивается к игроку. После диалога → `door_to_city_open = true`
8. **Стас (у двери):** параноик, 69 строк, ходит туда-сюда
9. **Дверь:** после Николая → "Дверь открыта" → переход в Scene_City

## Железо Тима

- MacBook Pro 13" M1 2020, 8 GB RAM
- macOS Tahoe 26.3.1
- Unity Editor: `~/Applications/Unity/Hub/Editor/6000.0.72f1/`
