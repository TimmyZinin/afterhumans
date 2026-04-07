# Session Status — 2026-04-07 (Afterhumans v2)

## Обзор

Две длинные сессии Claude Code (6 апр + 7 апр) по разработке первой сцены «Ботаника» для narrative walker «Послелюди». Начали с нуля (v2), прошли 12 спринтов, достигли функциональной сцены с Kenney-стилем + Tripo3D модель собаки.

## Текущее состояние сцены

**Работает:**
- Комната 12×10m, стены 9.6m, панорамные окна, потолок с люстрой
- 5 NPC с диалогами (Ink, 593 строки), "[E] говорить" промпты
- Тёплое освещение 3200K, HDRI skybox, post-FX (Bloom, ACES, SSAO, DoF, Vignette)
- Kenney FBX мебель + растения с процедурными PBR текстурами
- Пылинки, server LED, storytelling props
- Ходьба WASD + мышь, невидимая стена у двери
- Kafka (Tripo3D модель корги) — ЕСТЬ но СЛОМАНА

**Не работает (Kafka):**
- ❌ Утоплена в пол (origin не в ногах)
- ❌ Пропорции нарушены (Blender decimate исказил ноги)
- ❌ Анимация ходьбы не играет (AnimatorController назначен но clips не воспроизводятся)
- ❌ Не смотрит на игрока мордой

## Завершённые спринты

| Sprint | Название | Статус | Commit |
|--------|---------|--------|--------|
| 1 | Greybox — комната, стены, ходьба | ✅ PASS | 0b15e50 |
| 2 | Gameplay — NPC, диалоги, Kafka follow | ✅ PASS | ebd1a0e |
| 3 | Lighting — свет, skybox, post-FX | ✅ PASS | c428eb1 |
| 4 | Art — текстуры, NPC skins | ✅ PASS | 59072d9 |
| 5 | Polish — высокие стены, окна, FBX мебель | ✅ PASS | 90a7678 |
| 6 | URP Revolution — SSAO, soft shadows, HDR | ✅ PASS | 176c025 |
| 7 | Geometry — Kenney FBX мебель + растения | ✅ PASS | 59a154e |
| 8 | Materials — normal maps, emissive, glass | ✅ PASS | 9e04fc5 |
| 9 | Atmosphere — пылинки, пропсы, LED | ✅ частично | cdf02fc |
| 10 | Polish — post-FX max, debug HUD off | ✅ частично | 2f5d2b9 |
| 11 | Research — Blender + 15 инструментов | ✅ DONE | 7d003b4 |
| 12 | Kafka Corgi — интеграция Tripo модели | ❌ BROKEN | uncommitted |

## Kafka Corgi — текущая проблема

### Что сделано:
1. Tripo3D модель сгенерирована Тимом через web UI (детальный промпт Welsh Corgi Cardigan)
2. Скачан FBX из Tripo (42MB, 698K verts, 2 animation clips)
3. Blender decimate → 8K faces, export FBX
4. KafkaAnimator.controller создан с Walk state
5. SetupKafka() загружает модель, назначает AnimatorController
6. KafkaFollowSimple.cs переписан (кэш Animator, face player)

### Что сломано:
- **Origin:** Blender fix сдвинул origin но не точно в ноги (min_z=0.14 вместо 0)
- **Decimate:** исказил пропорции (ноги растянулись)
- **Animation:** AnimatorController назначается но clips не воспроизводятся в runtime
- **Scale:** менялся 6 раз (1x, 3x, 4x, 1.3x, 1x, 1x) — неясно какой правильный

### Root cause:
Blender decimate ratio 0.0057 (698K → 8K) слишком агрессивный — теряет форму модели. Нужно: или меньший decimate (20-30K faces), или использовать оригинальную модель и decimate в Unity через Mesh.SetTriangles LOD.

### Файлы модели:
```
Assets/_Project/Models/kafka_corgi.fbx          — 1.5MB (decimated, origin broken)
Assets/_Project/Models/kafka_corgi.glb          — 979KB (decimated earlier version)
Assets/_Project/Models/kafka_tripo/kafka_animated.fbx — 42MB (ORIGINAL from Tripo, full quality)
Assets/_Project/Models/kafka_tripo/*.fbm/       — 4 JPEG текстуры (basecolor, normal, metallic, roughness)
Assets/_Project/Models/KafkaAnimator.controller — AnimatorController с Walk state
```

### Рекомендация для следующей сессии:
1. Использовать оригинальный `kafka_animated.fbx` (42MB) напрямую БЕЗ Blender decimate
2. Unity сам оптимизирует mesh при import (ModelImporter settings)
3. Или decimate с ratio 0.02-0.03 (20-30K faces) вместо 0.005 (8K)
4. Origin fix: в Blender перед decimate, не после
5. AnimatorController: проверить что clip name совпадает с тем что в FBX

## Архитектура кода

### Editor scripts (активные):
```
Assets/_Project/Editor/BotanikaBuilder.cs       — ГЛАВНЫЙ: 10 методов Sprint1..Sprint9 + Sprint8_Materials
Assets/_Project/Editor/UrpQualitySetup.cs       — URP quality flags + SSAO + light cookie
Assets/_Project/Editor/ProceduralTextures.cs    — Процедурные текстуры (tile, plaster, wood, fabric + normals)
Assets/_Project/Editor/BuildScript.cs           — Build .app (1920x1080 windowed)
Assets/_Project/Editor/ColliderHelper.cs        — BoxCollider helper
Assets/_Project/Editor/KenneyAssetPostprocessor.cs — Scale fix для Kenney FBX
```

### Runtime scripts (активные):
```
Scripts/Player/SimpleFirstPersonController.cs   — WASD + mouse + gravity (v2, чистый)
Scripts/Player/PlayerInteraction.cs             — E press → NPC dialogue
Scripts/Dialogue/DialogueManager.cs             — Ink story singleton
Scripts/Dialogue/DialogueUI.cs                  — Canvas UI, typewriter
Scripts/Dialogue/Interactable.cs                — NPC trigger, static cache
Scripts/Kafka/KafkaFollowSimple.cs              — Follow player + Animator control
Scripts/Kafka/KafkaIdleAnimation.cs             — Procedural idle (breathing, tail, ears)
Scripts/Art/NpcIdleBob.cs                       — NPC idle animation
Scripts/Art/BlinkingLight.cs                    — Server rack LED
Scripts/Art/InteractionPromptUI.cs              — "[E] говорить" worldspace
Scripts/UI/DoorCueUI.cs                         — "Дверь открыта" UI
```

### Archived (v1, не компилируются):
```
Assets/_Project/Editor/_v1_archive/             — 10 .cs.bak файлов
```

## Установленные инструменты

- **Blender 5.1.0** — `brew install --cask blender`, CLI works
- **Blender MCP** — `claude mcp add blender uvx blender-mcp` (в .claude.json)
- **Tripo3D API** — ключ в `.env` (TRIPO_API_KEY), баланс пополнен Тимом

## Ключевые документы

| Документ | Содержание |
|----------|-----------|
| `docs/ART_BIBLE.md` | Палитры, освещение, post-FX, референсы |
| `docs/STORY.md` | Сценарий 3 актов, NPC диалоги |
| `docs/CHARACTERS.md` | 5 NPC + Kafka: характер, реплики, §10 Kafka детали |
| `docs/GDD.md` | Механики, управление |
| `docs/TOOLS_RESEARCH.md` | 15 text-to-3D инструментов, Blender MCP, Tripo |
| `docs/KAFKA_MODEL_PLAN.md` | Breed standard, пропорции, окрас, pipeline |
| `docs/PLAYTEST_REPORT.md` | QA playtest v1 (1/10) |
| `docs/AAA_GAP_REPORT.md` | Gap analysis current vs target |
| `docs/POST_MORTEM_PROMPT.md` | Анализ почему v1 провалился |
| `docs/ACTION_PLAN.md` | Метод: один цикл = код→build→feedback |

## Git tags для отката

```
v2-sprint6-stable   — Sprint 6 URP (всё работает, Kenney стиль)
v2-sprint7-stable   — Sprint 7 Geometry (FBX мебель)
v2-sprint8-stable   — Sprint 8 Materials (PBR textures)
v2-sprint11-stable  — Sprint 11 Research (перед Kafka)
```

## Batchmode команды

```bash
UNITY="$HOME/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity"

# Полная пересборка сцены (все спринты):
for S in Sprint1_Greybox Sprint2_Gameplay Sprint3_Lighting Sprint4_Art Sprint5_Polish Sprint8_Materials Sprint9_Atmosphere; do
  $UNITY -batchmode -quit -projectPath ~/afterhumans -executeMethod Afterhumans.EditorTools.BotanikaBuilder.$S -logFile /tmp/$S.log 2>&1
done
$UNITY -batchmode -quit -projectPath ~/afterhumans -executeMethod Afterhumans.EditorTools.UrpQualitySetup.EnableDesktopQuality -logFile /tmp/urp.log 2>&1
$UNITY -batchmode -quit -projectPath ~/afterhumans -executeMethod Afterhumans.EditorTools.BuildScript.BuildMacOS -logFile /tmp/build.log 2>&1

# Запуск:
open ~/afterhumans/build/Afterhumans.app
```

## Следующие шаги (для новой сессии)

1. **Kafka fix** — использовать оригинальный FBX без агрессивного decimate, fix origin, проверить анимацию
2. **Kafka orientation** — должна смотреть на игрока мордой
3. **After Kafka** — commit + push + обновить все docs
4. **Далее** — замена Kenney объектов на реалистичные (Tripo3D API, баланс пополнен)

## Tripo3D API

- **Ключ:** в `~/afterhumans/.env` (TRIPO_API_KEY)
- **Баланс:** пополнен Тимом 7 апр
- **Endpoint:** `https://api.tripo3d.ai/v2/openapi/`
- **Docs:** https://platform.tripo3d.ai/docs/quick-start

## Feedback от Тима (записан в память)

- ВСЕГДА планировать перед реализацией (memory/feedback_plan_before_implement.md)
- Не перегружать сессию большими файлами (>20MB)
- Kenney стиль = Minecraft, не реализм — нужна смена ассетов
- Собака — главный эмоциональный якорь, должна быть реалистичной
