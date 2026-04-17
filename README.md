# Послелюди / Afterhumans

> Narrative walker от первого лица. Episode 0 — пилот будущего сериала.
> **Deadline:** 2026-04-12 · **Target:** Mac + Windows standalone · **License:** TBD (PWYW on itch.io)

## О проекте

**Послелюди** — narrative walker на Unity 6 URP. Игрок проходит через три локации за 10-15 минут:

1. **Ботаника** — последний оазис живого в мире, где всё оптимизировано огромным алгоритмом *Прогноз*. 5 NPC: философ-LLM-болтун Саша, девушка с манифестом Мила, мистик с грибами Кирилл, сбежавший data-жрец Николай, параноик Стас. С игроком — **Кафка**, чёрно-белая корги-кардиган, 6 лет.
2. **Город** — стерильная финальная форма Прогноза. Downgraded-humans говорят data-фразами из двух слов. Эмоциональный пик: **Кафка тыкается в колено Анны**, Анна впервые за годы вспоминает свою сестринскую собаку Белку.
3. **Пустыня** — Dune-style закат, Кафка идёт рядом, в центре мигающий ASCII-курсор `> _`. Игрок вводит одно слово. 5 вариантов → 5 финальных текстов. Episode 0 заканчивается.

Диалоги в тоне *Rick and Morty* TV-MA, русский язык. Философия через абсурд. Цель: narrative experience такого уровня, чтобы игроки писали *«эта игра заставила меня заплакать из-за собаки»*.

## Технический стек

- **Engine:** Unity 6 LTS (`6000.0.72f1`)
- **Render pipeline:** URP 17.0.3
- **Target:** Standalone macOS (Apple Silicon), Windows (для playtest)
- **Dialogue:** Ink (inkle) + `com.inkle.ink-unity-integration`
- **Input:** Unity Input System (new)
- **Pathfinding:** AI Navigation (для Кафки follow)
- **Assets:** Kenney (CC0, furniture/nature/city kits), Poly Haven (CC0, HDRI), Mixamo (humanoid NPCs), Sketchfab CC0 / Quaternius (Kafka dog model)
- **Audio:** Suno AI (ambient), Freesound.org CC0 (SFX), Edge TTS + ElevenLabs (voice-over)
- **Distribution:** ad-hoc signed `.dmg` / `.zip` через `timzinin.com/afterhumans/`

## Статус

Pre-production завершён (Блок 1). **Production в процессе (Блок 2, Этап 8 — Unity install)**.

### Meadow Sandbox (отдельный полигон)

Параллельно с Episode 0 есть sandbox-сцена `Scene_MeadowForest_Greybox.unity` — полянка 80×80м с лесом, где Кафка бегает под прямым управлением WASD. Используется для прототипирования окружения и будущей интеграции пака **Stylized Nature MegaKit** (Quaternius). Не попадает в main Episode 0 build. Подробно: [`docs/MEADOW_SANDBOX.md`](docs/MEADOW_SANDBOX.md).

Быстрый запуск: в Unity меню `Afterhumans → Meadow → Bootstrap Sandbox Scene`, затем `Afterhumans → Greybox → Build Meadow Forest`.

## Структура репо

```
afterhumans/
├── docs/                              ← Design documents (all written)
│   ├── PLAN.md                        ← 21-этапный roadmap
│   ├── UNIVERSE.md                    ← мир «Баг в алгоритме»
│   ├── STORY.md                       ← сюжет сцена за сценой
│   ├── CHARACTERS.md                  ← 12 персонажей
│   ├── ART_BIBLE.md                   ← визуал + аудио direction
│   ├── GDD.md                         ← game design document
│   ├── TECH.md                        ← технический план
│   ├── SCOPE.md                       ← scope lock
│   └── DENIS_BRIEF.md                 ← задачи для саунд-дизайнера
├── Assets/
│   ├── Dialogues/
│   │   └── dataland.ink               ← полный скрипт диалогов
│   └── _Project/
│       ├── Scripts/                   ← 14 C# files
│       │   ├── Dialogue/              ← Ink integration + UI + interactables
│       │   ├── Player/                ← PlayerInteraction raycast
│       │   ├── Kafka/                 ← Kafka follow + reactions + special events
│       │   ├── Scenes/                ← fade transitions + door gates
│       │   ├── Cursor/                ← final cursor zoom-in
│       │   ├── UI/                    ← main menu + credits sequence
│       │   ├── Audio/                 ← mixer controller
│       │   ├── Managers/              ← save/load state
│       │   └── Editor/BuildScript.cs  ← Mac Apple Silicon batchmode build
│       ├── Settings/                  ← URP Volume Profile specs (md)
│       ├── Audio/
│       │   ├── Music/                 ← для Дениса (empty)
│       │   ├── SFX/                   ← для Дениса (empty)
│       │   └── VO/                    ← для Дениса (empty)
│       └── Vendor/                    ← gitignored, download via scripts/download-assets.sh
│           ├── Kenney/
│           └── PolyHaven/
├── Packages/
│   └── manifest.json                  ← Unity packages (URP + Ink + Input + ...)
└── scripts/
    ├── create-unity-project.sh        ← Unity batchmode init после Editor install
    └── download-assets.sh             ← скачивает бесплатные CC0 ассеты
```

## Collaborators

- **[Тим Зинин](https://github.com/TimmyZinin)** — идея, концепция, сюжет, диалоги, Unity integration, Mac build, deployment
- **Денис Говорунов** — саунд-дизайн, озвучка, Windows playtest, trailer. Бриф: [`docs/DENIS_BRIEF.md`](docs/DENIS_BRIEF.md)
- **Claude Code** (Opus 4.6, 1M context) — AI engineer, реализация, координация

## Quick Start для разработчика

```bash
# 1. Клонировать репо
git clone https://github.com/TimmyZinin/afterhumans.git
cd afterhumans

# 2. Скачать бесплатные CC0 ассеты (Kenney + Poly Haven)
bash scripts/download-assets.sh

# 3. Установить Unity 6000.0.72f1 через Unity Hub
#    - Модуль: Mac Build Support (Apple Silicon)
#    - Для Windows билда добавить: Windows Build Support (Mono)

# 4. Открыть проект в Unity Hub
#    Unity автоматически импортирует Ink package + остальные зависимости
#    dataland.ink компилируется в dataland.json на import

# 5. Первый smoke build
open ~/afterhumans
# Unity Editor → File → Build Profiles → Mac Apple Silicon → Build
# Или через CLI: bash scripts/create-unity-project.sh
```

## Roadmap

| Блок | Этапы | Deliverable |
|---|---|---|
| **0. Research** | Contabo Unity + Mac resources inventory | ✅ |
| **1. Pre-production** | 7 документов + Ink скрипт | ✅ |
| **2. Production** | Unity install → assets → 3 сцены → аудио → polish | 🟡 в процессе (этап 8) |
| **3. Post-production** | build → sign → DMG → hosting → landing → verify → notify | ⏳ |

Полный план: [`docs/PLAN.md`](docs/PLAN.md)

## Ключевые файлы для чтения (в порядке приоритета)

1. [`docs/UNIVERSE.md`](docs/UNIVERSE.md) — мир «Баг в алгоритме», Прогноз как корреляционная функция
2. [`docs/STORY.md`](docs/STORY.md) — сюжет сцена за сценой, 15 key story beats
3. [`docs/CHARACTERS.md`](docs/CHARACTERS.md) — 12 персонажей с речевыми портретами
4. [`Assets/Dialogues/dataland.ink`](Assets/Dialogues/dataland.ink) — полный скрипт диалогов (15 knots)
5. [`docs/DENIS_BRIEF.md`](docs/DENIS_BRIEF.md) — задачи для саунд-дизайнера Дениса
6. [`docs/SCOPE.md`](docs/SCOPE.md) — что в Episode 0, что cut, fallback ladders

## License

Source code: MIT (TBD)
Story, dialogues, characters: © Тим Зинин, 2026
Assets: каждый под своей лицензией (Kenney/Poly Haven — CC0, Mixamo — Adobe free use, Unity assets — standard Unity EULA)

---

**Через неделю все охуеют от того, что мы сделали.**
