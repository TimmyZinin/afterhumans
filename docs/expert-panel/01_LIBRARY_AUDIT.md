# 01 · Аудит скачанных библиотек и Unity packages

> **Дата:** 2026-04-05 ~21:50
> **Аналитик:** Claude (skill-level)
> **Цель:** зафиксировать что **реально** есть в проекте, чтобы Expert Panel и план восстановления работали с фактами, а не догадками.

---

## 1. Ассеты на диске (Assets/_Project/Vendor/)

**Total:** 113 MB на диске.

### Kenney (CC0 low-poly) — 510 FBX моделей

| Pack | FBX count | Содержимое | Стилистический статус |
|---|---|---|---|
| **kenney/furniture-kit** | 140 | Stylized low-poly мебель: кровати, диваны, столы, шкафы, лампы, книги, посуда, **walls+floor+doorway tiles для строительства помещений**, ceilingFan, rugDoormat | ⚠️ Не соответствует ART_BIBLE (требует Quixel Megascans + URP PBR) |
| **kenney/city-kit-commercial** | 41 | Здания, небоскрёбы, low-detail buildings, parasol, awnings | ⚠️ Low-poly — не соответствует референсам (Observation, Blade Runner 2049) |
| **kenney/nature-kit** | 329 | Cliffs, rocks, trees (palm/oak/pine варианты), cacti, bushes, grass, flowers, mushrooms, **campfire_stones**, bridges, fences, ground_paths | ⚠️ Хорошо подходит для **stylized** варианта, но не для полуреалистичного Firewatch/Journey |

**Всего 510 FBX моделей Kenney.** Все импортированы Unity (meta-файлы есть), но имеют **UnitScaleFactor 100** в FBX header — это причина почему объекты кажутся неправильного размера при `Vector3.one` scale (требуется компенсация через importSettings или scale 0.01).

**Материалы Kenney:** НЕТ текстур. Только per-material Kd (diffuse colour) values в .mtl файлах (OBJ format), которые Unity FBX importer **не читает**. Поэтому всё импортируется как magenta missing-material.

### Poly Haven (CC0 PBR) — 1 HDRI

| Файл | Тип | Размер | Назначение |
|---|---|---|---|
| `rogland_sunset_2k.hdr` | Equirectangular HDRI | 2K (~4 MB) | Закатное небо для пустыни (соответствует ART_BIBLE «Sunset HDRI») |

**Gap:** По ART_BIBLE нужны **минимум 3 HDRI** (по одной на каждую сцену) + PBR texture sets для ground/walls/wood/metal. Сейчас: **1 HDRI, 0 texture sets.**

### Всё остальное

**НИЧЕГО больше не скачано.** НЕТ:
- Mixamo humanoids (Art Bible требует 11 NPC)
- Quaternius models
- Sketchfab CC0 dog (для Кафки)
- Quixel Megascans textures
- Музыка и SFX (всё отложено до Дениса)
- Custom shaders

---

## 2. Unity Packages (из `Packages/manifest.json`)

### Активные пакеты

| Package | Версия | Статус использования |
|---|---|---|
| `com.unity.render-pipelines.universal` | **17.0.4** | ⚠️ **Установлен, но НЕ активирован** (GraphicsSettings.m_CustomRenderPipeline = null → проект фактически на Built-in Render Pipeline) |
| `com.unity.shadergraph` | 17.0.4 | Не используется |
| `com.unity.postprocessing` | **3.5.4** (PPv2 legacy) | Не используется |
| `com.unity.visualeffectgraph` | 17.0.4 | Не используется |
| `com.inkle.ink-unity-integration` | `git#upm` | ✅ используется: dataland.ink → dataland.json через Ink.Compiler API |
| `com.unity.ai.navigation` | 2.0.12 | Не используется (Kafka через KafkaFollowSimple без NavMesh) |
| `com.unity.cinemachine` | 2.10.7 | Не используется (Art Bible просит cinematic camera moments) |
| `com.unity.probuilder` | 6.0.9 | Не используется |
| `com.unity.timeline` | 1.8.10 | Не используется |
| `com.unity.inputsystem` | 1.19.0 | Установлен, **но контроллер на legacy Input.GetAxisRaw** |

### Ключевое открытие

**URP установлен, но не активен.** Это критичный факт:
1. GraphicsSettings.asset имеет `m_CustomRenderPipeline: {fileID: 0}` = Built-in
2. Вся наша работа идёт через Standard shader (Built-in)
3. Post-processing (PPv2) есть в проекте, но Volume stack не настроен
4. Shader Graph не используется хотя установлен
5. Cinemachine не используется
6. Visual Effect Graph не используется
7. Timeline не используется

**Мы импортировали URP, но фактически ведём разработку на Built-in.** Это объясняет почему нельзя применить пожелания ART_BIBLE (Bloom, Color Adjustments, Volumetric Fog, Shader Graph cursor) — они в URP.

---

## 3. Скрипты написанные (26 C# файлов)

### Editor scripts (Assets/_Project/Editor/)
- `BuildScript.cs` — build macOS .app
- `ProjectSetup.cs` — scene creation + ForceInkCompile + SetSceneFirst helpers
- `SceneEnricher.cs` — populates FPS scenes с Player/walls/NPC/Dialogue/Kafka/ExitTrigger
- `BotanikaDresser.cs`, `CityDresser.cs`, `DesertDresser.cs` — prop dressers
- `CreditsDresser.cs`, `MenuDresser.cs` — UI scene dressers
- `LightingSetup.cs` — fog/ambient/sun/camera per preset

### Runtime scripts (Assets/_Project/Scripts/)
- **Player:** `SimpleFirstPersonController.cs` (legacy Input), `PlayerInteraction.cs` (distance-based)
- **Dialogue:** `DialogueManager.cs` (Ink Story singleton), `DialogueUI.cs` (Canvas + typewriter), `Interactable.cs`, `InteractionPrompt.cs`
- **Scenes:** `SceneTransition.cs` (fade+load), `SceneExitTrigger.cs` (Ink-gated box trigger), `DoorToCity.cs`
- **Kafka:** `KafkaFollow.cs` (NavMesh, не используется), `KafkaFollowSimple.cs` (distance-based, используется), `KafkaReactions.cs`
- **UI:** `MainMenuController.cs`, `CreditsSequence.cs`
- **Managers:** `GameStateManager.cs`
- **Finale:** `CursorFinale.cs`
- **Audio:** `AudioMixerController.cs` (не используется — аудио отложено)

### Гигиена кода
- Все 26 скриптов компилируются без ошибок
- Ink integration работает (dataland.json 43569 байт скомпилирован из 39661 байт .ink)
- 5 сцен связаны через SceneExitTrigger
- Build успешно производит 97 MB Universal Binary macOS .app

---

## 4. Чего **нет** из ART_BIBLE

По документу `docs/ART_BIBLE.md` требуется (цитата): **«URP + Post-processing по палитре»**, **«Mixamo NPC, Quixel Megascans, Sketchfab CC0 dog, Poly Haven HDRI, Cinemachine camera moments»**, **«Custom Shader Graph для курсора»**.

| Требование ART_BIBLE | Реализовано? | Комментарий |
|---|---|---|
| URP активен | ❌ | Package установлен, GraphicsSettings не патчен |
| Post-processing Volume stack | ❌ | Package установлен, ни один Volume не создан |
| Mixamo NPC (11 персонажей) | ❌ | 0 скачано, используются placeholder cubes |
| Quixel Megascans textures | ❌ | 0 скачано, используются solid color tints |
| Sketchfab CC0 dog (Кафка) | ❌ | 0 скачано, placeholder 2-cube stub |
| Poly Haven HDRI (3 штуки) | ⚠️ Частично | 1 из 3 (только закат, нет Botanika/City) |
| Cinemachine camera cinematic | ❌ | Package установлен, не используется |
| Shader Graph cursor | ❌ | Package установлен, не используется |
| ACES tonemapping | ❌ | Нет Volume, нет настройки |
| Film Grain, Vignette, Bloom | ❌ | Те же причины |
| Volumetric Fog (URP) | ❌ | Используется Built-in fog вместо URP volumetric |
| Depth of Field | ❌ | Не применён |
| Chromatic Aberration (для City) | ❌ | Не применён |
| Аудио (ambient, SFX, footsteps) | ❌ | Отложено, ждёт Дениса |
| Mixamo NPC animations (idle/talking) | ❌ | Не применено |

**Сводка:** **14 из 15** требований ART_BIBLE **не реализованы**. Реализован только `docs/ART_BIBLE.md` на бумаге + частично (HDRI × 1).

---

## 5. Выводы аудита

1. **Мы не соответствуем ART_BIBLE ни по одному из основных пунктов** визуала.
2. **Kenney low-poly — это prototype-grade assets**, не финальные. Их использование для Episode 0 = выбор стиля, которого НЕТ в Art Bible.
3. **URP package в проекте, но проект работает на Built-in.** Это критичный блокер — все pipeline-dependent фичи (bloom, color grading, volumetric fog, shader graph) недоступны на Built-in.
4. **Скрипты и логика** написаны добротно (26 файлов, 0 ошибок компиляции, Ink integration работает, scene transitions работают). **Не соответствует не код — соответствует asset pipeline и визуал.**
5. **Ресурсы на диске**: 113 MB Kenney (полезно, но не по Art Bible), 4 MB Poly Haven HDRI. **Критично не хватает: Megascans textures, Mixamo humanoids, CC0 corgi model, 2 HDRI.**

**Главный вопрос для Expert Panel:** продолжить на Kenney/Built-in и переписать ART_BIBLE под реальность, или **выполнить рестарт asset pipeline**: активировать URP, скачать Megascans/Mixamo/Sketchfab, применить всё это через агентов?

Спойлер: правильный ответ — **рестарт**. Детали в следующем документе.
