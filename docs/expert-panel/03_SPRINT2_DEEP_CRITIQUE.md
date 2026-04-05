# 03 · Sprint 2 — Deep Critique через 10 gamedev оптик

> **Дата:** 2026-04-05 ~22:30
> **Аналитик:** Claude (skill-level, 10 skills loaded)
> **Вход:** Sprint 1 (docs/expert-panel/01_LIBRARY_AUDIT + 02_EXPERT_PANEL_SPRINT1)
> **Выход:** Жёсткая per-lens критика → input для Sprint 3 (Plan Mode roadmap + scene-by-scene backlog)

---

## Предисловие: как читать этот документ

Каждая оптика — отдельный раздел с тремя частями:
1. **Принципы skill-а** (что skill считает важным)
2. **Применение к Afterhumans** (что это значит для нашего проекта)
3. **Findings** — P0 (блокер ship), P1 (важно), P2 (nice), **с конкретными ссылками на STORY.md беаты и сцены**

В конце — **сводная матрица** (строки = scenes, колонки = skills) и **топ-10 действий** для Sprint 3 backlog.

---

## Оптика 1: `3d-games` — Rendering Pipeline, Shaders, Physics, Cameras

### Принципы
- Frustum culling, occlusion culling, LOD, batching — основа performance
- Custom shaders только для: spec FX, stylized look, performance, unique identity
- Simple colliders, complex visuals
- Camera feel: smooth lerp, collision avoidance, FOV changes for speed
- Real-time shadows дорогие — bake когда возможно
- *«3D is about illusion. Create the impression of detail, not the detail itself»*

### Применение к Afterhumans
У нас **510 Kenney FBX** с MeshCollider на каждом (`SceneEnricher` автоматически добавляет MeshCollider если нет Collider). Это **anti-pattern** из skill-а: *«Mesh colliders everywhere»*. Должны быть simple box/capsule на мебели.

Нет LOD groups ни на одном prop. Нет occlusion culling (Unity не автоматически включает для Built-in). Нет static batching. Нет baked lighting. Shadows real-time от одного Directional Light.

Camera = `SimpleFirstPersonController` без lerp smoothing, без FOV changes для sprint, без head-bob variation per terrain. ART_BIBLE §6 требует **contextual head bob** (Ботаника medium, Город minimal, Пустыня strong+staggers) — не реализовано.

### Findings

**P0:**
- **[3D-1] MeshCollider на каждом Kenney prop** — `SceneEnricher`/`BotanikaDresser`/`CityDresser`/`DesertDresser` делают `instance.AddComponent<MeshCollider>(); mc.sharedMesh = mf.sharedMesh; mc.convex = false;`. 500+ mesh colliders = performance killer на M1 8GB. Скрипт должен генерировать **BoxCollider** с bounds из mesh.
- **[3D-2] Нет FOV zoom в финале** — ART_BIBLE §6 требует при подходе к Курсору FOV медленно сужается с 65° до 50°. Не реализовано. Camera остаётся 65° весь путь.

**P1:**
- **[3D-3] Нет LOD groups** — Kenney trees/rocks/buildings без LOD0/1/2. На M1 8GB с 50+ деревьями в Desert будет просадка. Нужен `LODGroupBuilder` post-processor.
- **[3D-4] Нет static batching** — все объекты dynamically instantiated через `PrefabUtility.InstantiatePrefab`. Unity static batching требует GameObjects marked Static в Inspector. В Dresser нужно `GameObjectUtility.SetStaticEditorFlags(inst, StaticEditorFlags.BatchingStatic)`.
- **[3D-5] Camera smoothing отсутствует** — Player.rotation меняется instantly от Mouse X/Y. Skill требует lerp. Нужно `Quaternion.Slerp(currentRotation, targetRotation, smoothing * Time.deltaTime)`.
- **[3D-6] Head bob не contextual** — одинаковый bobFrequency/bobAmplitude в `SimpleFirstPersonController` для всех сцен. Должен быть per-scene override через `LightingSetup`-style preset.

**P2:**
- **[3D-7] No occlusion culling** — включить через `Window > Rendering > Occlusion Culling > Bake` (занимает 5-15 минут на M1).
- **[3D-8] No baked GI** — все lighting real-time. Один Lightmap bake per scene дал бы 2x визуальное улучшение.
- **[3D-9] No FOV change для sprint** — skill требует FOV++ при Shift. Subtle но важно для «feel».

---

## Оптика 2: `game-art` — Visual Style, Asset Pipeline, Animation

### Принципы
- *«Define and follow style guide»* — не смешивать random стили
- Silhouette readability — test at gameplay distance
- Focus detail on player area, less in background
- Animation 12 principles (squash/stretch, anticipation, staging, follow-through...)
- Idle breathing: 4-8 frames. Walk: 6-12. Run: 4-8.
- *«Art serves gameplay. If it doesn't help the player, it's decoration.»*

### Применение к Afterhumans
ART_BIBLE определяет **один** style guide: *«stylized-realistic... что-то между Sable и Death Stranding»*. Current state: **Kenney low-poly с solid color tints** — это **третий стиль** который в ART_BIBLE отсутствует. Мы нарушили главный принцип skill-а: «define style → follow it».

**Silhouette readability**: placeholder NPC cubes (0.7×1.8×0.7) имеют **идентичные silhouettes** между собой, отличаются только цветом. На distance 5+ метров они сливаются в «ряд кубов». Distance от spawn (0,1,-12) до Sasha (0,1,3) = 15 метров. На этом distance игрок видит **один коричневый куб** без hint что это NPC.

**Animation 12 principles — реализовано 0 из 12.** NPC статичны (нет idle breathing). Kafka double-cube не дышит. Player walk не имеет anticipation/follow-through. Dialogue trigger не имеет squash/stretch feedback.

**Naming convention** из skill: `[type]_[object]_[variant]_[state].[ext]`. Наши Kenney файлы: `loungeDesignSofa.fbx`, `cactus_tall.fbx` — **не наши**, так Kenney экспортировал. Но наши runtime instances в scene hierarchy назыаются `Placeholder_NPC_Sasha`, `KafkaBody`, `KafkaChest` — нарушает convention (должно быть `npc_sasha_idle` / `kafka_body_base`).

### Findings

**P0:**
- **[ART-1] Три конфликтующих стиля в одном проекте**: (a) Kenney low-poly (512 FBX), (b) Firewatch полуреалистичный (целевой), (c) Minecraft cube placeholders (NPC + Kafka). Пользователь видит frankenstein. **Решение: выбрать ОДИН стиль и беспощадно resize всё под него**. По выбору Тима = Firewatch = необходимо выбросить Kenney furniture/city и оставить только nature-kit для silhouette vegetation.
- **[ART-2] NPC silhouette failure** — 5 placeholder cubes в Ботанике идентичны по форме. Игрок не распознаёт *«Это Саша»* vs *«Это Николай»* vs *«Это Мила»*. Это блокер navigation → narrative разрушен. **Решение**: Quaternius CC0 humans с **отчётливыми силуэтами** (разная одежда, позы, роста).
- **[ART-3] Animation coverage 0/12 принципов**. Ни одна 12-principle анимация не применена. **Решение**: Quaternius character packs идут с **baked idle/walk/talk анимациями**, Unity Mecanim берёт их из коробки.

**P1:**
- **[ART-4] Detail distribution backwards** — background (Kenney trees в Desert) = больше polygons чем foreground (placeholder NPC cubes). Skill говорит «focus detail on player area». У нас обратное.
- **[ART-5] Нет color palette enforcement**. ART_BIBLE §3 определяет 3 палитры по локациям с hex codes. Мой `BotanikaDresser` создаёт `Tint_Wood`, `Tint_Upholstery` с **приблизительными** color values не соответствующими hex palette ART_BIBLE. Должен быть `Assets/_Project/Art/Palettes/Botanika.asset` ScriptableObject → все materials читают оттуда.
- **[ART-6] Naming convention нарушена** — scene instances `Placeholder_NPC_*`, `KafkaBody`, `Boundary_Walls`, `Scene_Exit_Trigger` — mix стилей. Skill требует `[type]_[object]_[variant]_[state]`.

**P2:**
- **[ART-7] Нет concept art фазы** — мы прыгнули в Unity без blockout, без greybox thumbnails. Skill требует *«Concept → Blockout → Production»*.
- **[ART-8] Silhouette test at gameplay distance не проводили** — нужен script который рендерит каждый prop из player POV на 5m/10m/20m и проверяет что он recognizable.

---

## Оптика 3: `game-audio` — Sound Design, Music, Adaptive Audio

### Принципы
- Audio category system: Music, SFX, Ambient, UI, Voice — каждая с priority hierarchy
- Priority: Voice > Player SFX > Enemy SFX > Music > Ambient
- 3D spatialization: player footsteps НЕ 3D, NPC footsteps ДА 3D, music НЕ 3D
- Distance behavior: near full → medium rolloff → far low-pass → max silent
- Mix hierarchy: Voice 0dB ref, Player SFX -3-6dB, Music -6-12dB, Ambient -12-18dB
- Ducking: Voice играет → Music/Ambient -6-9dB
- *«50% of the game experience is audio. A muted game loses half its soul.»*
- *«Skip audio in prototype? No. Placeholder audio matters.»*

### Применение к Afterhumans
**Coverage 0/15** (из Sprint 1 Audio Director). Narrative walker без аудио = библиотека с обложкой и пустыми страницами. Anti-pattern skill: *«Skip audio in prototype»* — именно это мы сделали.

Skill-level архитектура требует:
1. **AudioMixer.mixer** asset с группами Master→Music/SFX/VO/UI + expose volume params
2. **AudioSource** с `playOnAwake+loop` в каждой scene (Ambient layer)
3. **3D AudioSource** per NPC (talking cue, breathing) + per interactable (кофемашина, паяльник)
4. **FootstepController** на Player, raycast ground material → trigger footstep sound из array
5. **Ink event triggers** для music stingers на key knots (Nikolai exposition, Anna memory, Cursor finale)

### Findings

**P0:**
- **[AUDIO-1] AudioMixer.mixer asset не создан** — скрипт `AudioMixerController.cs` существует, но assets/Audio/AfterhumansMixer.mixer нет. Без этого settings menu sliders некуда wire. **Инфраструктурный блокер.**
- **[AUDIO-2] 0 AudioSources в сценах** — нет ambient loop ни в Ботанике, ни в Городе, ни в Пустыне. Игрок играет в полной тишине. Это не нарративная тишина — это bug.
- **[AUDIO-3] Kafka без звуковой persona** — 2-cube visual + 0 audio cues. Персонаж равен «фоновый куб». Добавление 3-4 CC0 sounds (bark, whine, paws) + KafkaFollowSimple triggering их = 80% персонажа без mesh.

**P1:**
- **[AUDIO-4] Нет FootstepController** — одна из двух самых важных систем для narrative walker. Firewatch делает footsteps с 8 surface variants. У нас 0.
- **[AUDIO-5] Нет 3D spatialization на NPC** — даже когда добавим audio, если AudioSource `spatialBlend=0` (2D), игрок не поймёт направление. Должно быть `spatialBlend=1` (3D) с rolloff curve.
- **[AUDIO-6] Нет Ink-triggered music stingers**. Key story moments (Nikolai exposition, Anna memory, approach server, approach cursor) должны иметь audio cue. Нужна интеграция `DialogueManager` events → `AudioStinger.Play(id)`.
- **[AUDIO-7] Нет ducking логики** — когда диалог начинается music должен duck -6dB. Требует AudioMixer snapshots.

**P2:**
- **[AUDIO-8] Нет variation pool** для footsteps — skill требует 3-5 variations per sound чтобы избежать repetition fatigue. Даже если мы загрузим 1 footstep.wav — нужно 4.
- **[AUDIO-9] Нет SFX tail layering** — skill рекомендует Attack+Body+Tail+Sweetener для каждого SFX. Для placeholder OK single file, но для финала нужен layered design.

---

## Оптика 4: `game-design` — Core Loop, GDD, Player Psychology

### Принципы
- **30-second test**: ACTION → FEEDBACK → REWARD → REPEAT, fun within 30 seconds
- GDD sections: Pitch, Core Loop, Mechanics, Progression, Art, Audio
- Player motivation types: Achiever, Explorer, Socializer, Killer
- Flow state: not too hard, not too easy
- Pacing: early wins → rest beats → meaningful choices
- *«Fun is discovered through iteration, not designed on paper.»*

### Применение к Afterhumans
Narrative walker не имеет «fun» в классическом смысле — но имеет **engagement loop**. Для Firewatch/Edith Finch loop = «walk → notice detail → interact → get emotional payload → continue». Это 30-second test адаптированный.

Current 30-second test для Afterhumans:
1. **ACTION**: player walks forward
2. **FEEDBACK**: scenery slowly changes
3. **REWARD**: ... empty. Нет первого beat-а за 30 секунд. Нет note на подлокотнике (STORY 1.1). Нет Kafka wake-up reveal. Нет first-look shot.
4. **REPEAT**: никогда не начинается потому что game-design loop не закрыт

**Verdict**: игра **проваливает 30-second test**. В первые 30 секунд нет ни одной FEEDBACK/REWARD beats. Player загружается в комнату, ходит, не понимает что делать → quit.

Player motivation для narrative walker = **Explorer** primary, **Socializer** secondary. Explorer требует «что-то найти». У нас ничего не спрятано, ничего не рассеяно по сцене. Socializer требует «с кем-то поговорить» — NPC не читаются как персонажи (см. ART-2).

### Findings

**P0:**
- **[DES-1] 30-second test FAILS** — первые 30 секунд игры не содержат ни одного FEEDBACK/REWARD момента. Должен быть: note knot (0:05) → pick up → tutorial overlay (0:10) → Kafka head lift (0:15) → first look at Ботаника (0:20) → understand goal (0:30).
- **[DES-2] Нет "early win"** — skill требует hook quickly. У нас hook через Саша dialogue который player может пропустить через exit trigger. Требуется **forced moment**: note knot нельзя пропустить (scripted camera lock first 15 seconds).
- **[DES-3] Explorer motivation не удовлетворена** — в Ботанике по сценарию 5 NPC в разных местах + note + серверная стойка в углу + граффити `segfault == freedom` + книги на стеллажах. **Реализовано 1 NPC, 0 окружения.** Нечего explore.

**P1:**
- **[DES-4] Progression не очевидна** — игрок не понимает arc «Ботаника → Город → Пустыня». Нет visual cue что за doorway = new chapter. Нужен subtle progress indicator (scene counter, chapter title fade-in).
- **[DES-5] Flow state нарушено** — текущая версия слишком пустая (boredom) **одновременно** с слишком запутанной (player не знает куда идти = frustration). Это двойной провал.
- **[DES-6] Rest beats отсутствуют** — ART_BIBLE прописан contextual head bob, pacing, тишина. Current: одинаковый шаг везде. Нет rest beats после intense moments (нет intense moments).

**P2:**
- **[DES-7] No meaningful choices до финала** — Ink story имеет 593 строки с разветвлениями, но эти выборы не связаны с visible game state change. Player выбирает *«я тоже не знаю где я»* vs *«кто ты такой»* но следующий play через ту же Ботанику идентичен.
- **[DES-8] No GDD living update** — docs/GDD.md написан один раз и не обновлялся. Skill требует «keep it living».

---

## Оптика 5: `game-development` (orchestrator) — Performance Budget, AI, Patterns

### Принципы
- **Performance budget 60 FPS = 16.67ms/frame**: Input 1 / Physics 3 / AI 2 / Logic 4 / Render 5 / Buffer 1.67
- Pattern selection: State Machine (3-5 states) → ECS (thousands of entities)
- Input abstraction: *«jump»* → Space/Gamepad A/Touch tap (not raw keys)
- Optimization priority: Algorithm → Batching → Pooling → LOD → Culling
- AI selection: FSM (simple) → BT (medium) → GOAP (high) → Utility (high)
- Anti-patterns: update everything every frame, create objects in hot loops, cache nothing, optimize without profiling, mix input with logic

### Применение к Afterhumans
**Input abstraction нарушена**: `SimpleFirstPersonController.HandleMovement()` использует `Input.GetAxisRaw("Horizontal")`, `Input.GetKey(KeyCode.LeftShift)`, `Input.GetKeyDown(KeyCode.Escape)`. **Это raw keys, не abstracted actions.** Unity `InputSystem` package 1.19 установлен но не используется. Skill требует Input Actions asset + action map "Gameplay/UI/Dialogue".

**Pattern selection нарушена**: Kafka использует `KafkaFollowSimple.cs` с прямой `Vector3.MoveTowards` — это не State Machine. Должно быть: FSM с states = `Idle`, `Follow`, `CatchingUp`, `Stopped`, `Sniffing`, `Barking`. Каждое состояние транзиционирует по условиям (distance, time, trigger). Current код — single-state if/else.

**Performance budget нарушено потенциально**:
- **Rendering**: 500+ mesh colliders + 0 static batching + real-time shadows от 1 Directional + 4 point lights в Botanika (ceiling lamps) = много draw calls. На M1 8GB потенциально провал <30 FPS.
- **Logic**: `PlayerInteraction.Update()` делает `FindObjectsByType<Interactable>(FindObjectsSortMode.None)` каждый кадр. **Anti-pattern**: *«update everything every frame»* + *«create objects in hot loops»* (FindObjectsByType allocates array). Должно быть **cached list** refreshed on scene load.

### Findings

**P0:**
- **[GDV-1] FindObjectsByType каждый кадр** в `PlayerInteraction.Update` → GC pressure + CPU spike. Скрипт должен cachить список Interactables в OnEnable/scene load, обновлять только при spawn/destroy.
- **[GDV-2] Нет Input Actions asset** — весь input через legacy Input Manager. Skill и Unity industry standard = Input System. Требует `Assets/_Project/Input/AfterhumansInput.inputactions` с action maps Gameplay (Move/Look/Interact/Sprint/Pause) + Dialogue (Continue/SkipChoice/Cancel) + UI (Navigate/Submit/Cancel).

**P1:**
- **[GDV-3] Kafka без FSM** — `KafkaFollowSimple` = одно состояние. Для real persona требует FSM: Idle→Follow→Sniff→Bark→Growl transitions.
- **[GDV-4] NPC без FSM** — все NPC статичны. Skill: FSM для 3-5 states (Idle→Talking→LookAround→ReactToKafka).
- **[GDV-5] Нет object pooling** — footsteps, dialogue choice buttons, particle effects будут instantiate-destroy в runtime. Без pool → GC spikes на M1 8GB. Нужен `ObjectPool<T>` utility class.
- **[GDV-6] Нет static batching** (см. также 3D-4).

**P2:**
- **[GDV-7] Нет profiling baseline** — skill говорит «profile first». Мы не знаем current FPS, draw calls, memory в Botanika. Нужен `PerformanceReporter.cs` editor tool.
- **[GDV-8] Нет event system для cross-systems** — `DialogueManager.OnDialogueLine`/`OnDialogueChoices`/`OnDialogueEnd` работают как observer pattern (good), но другие системы не имеют event bus. Skill рекомендует observer/events для cross-system communication.

---

## Оптика 6: `ui-ux-pro-max` — UI/UX Intelligence, Dialogue/Menu/HUD

### Принципы
- **Accessibility CRITICAL**: color contrast 4.5:1, focus states, alt text, keyboard nav
- **Touch targets**: minimum 44×44 px
- **Performance**: image optimization, reduced-motion, content jumping prevention
- **Layout**: viewport meta, readable font ≥16px, no horizontal scroll
- **Typography**: line-height 1.5-1.75, line-length 65-75 chars
- **Animation**: 150-300ms micro-interactions, transform/opacity (not width/height)
- **Style**: match product type, consistency across pages

### Применение к Afterhumans
UI в проекте:
1. **Main Menu** — `MenuDresser` создаёт Canvas с title «ПОСЛЕЛЮДИ» (fontSize 120), subtitle 32, Start/Continue/Quit buttons 80px tall
2. **Dialogue UI** — `DialogueUI` + `DialogueManager` создают bottom panel 35% height с line text 28pt + choices container
3. **Credits** — `CreditsDresser` создаёт 3 canvas groups (finalText, credits, sting)
4. **Player HUD** — `PlayerInteraction` имеет OnGUI debug HUD в левом верхнем углу (developer mode, должен быть отключён в production)

### Findings

**P0:**
- **[UX-1] Debug HUD активен в production build** — `PlayerInteraction.showDebugHud = true` по default. Shipping build имеет ярко-жёлтый дебаг-текст поверх кадра. Блокер ship.
- **[UX-2] Dialogue UI нет speaker name** — `DialogueUI.HandleLine` показывает текст реплики без префикса «Саша:», «Николай:». Player теряется кто говорит. Skill-level: обязательный element.
- **[UX-3] Touch targets buttons < 44px на мобиле** (не наш случай — Mac standalone) но на desktop кнопки Start/Quit высотой 80px — норм. Однако **continue button скрыт** если нет save, что ломает visual hierarchy — должны быть 2 кнопки (Start/Quit) с правильным alignment когда Continue отсутствует.

**P1:**
- **[UX-4] Color contrast не проверен**. Dialogue text Color.white на RGBA(0,0,0,0.7) = ~4:1 contrast. Skill требует 4.5:1 (WCAG AA). Нужно либо поднять alpha до 0.85 либо добавить text stroke/outline.
- **[UX-5] Font size dialogue 28pt** на 1920×1080 reference — OK для desktop. Но line length не ограничен (анchor 0.1-0.9 = ~1500px wide). Skill: 65-75 characters max. Русский text с fontSize 28 на 1500px = ~90 chars per line → eye strain. Ограничить до 900-1000px wide.
- **[UX-6] Typewriter effect 30 chars/sec** = too fast для narrative walker. Firewatch/Edith Finch = 20-25 chars/sec + можно скипать. Нужен tunable + skip на press.
- **[UX-7] Нет interaction prompt** (*«говорить [E]»*) — уже в Sprint 1 findings. Skill: feedback critical для touch/interaction.
- **[UX-8] Нет focus states** на menu buttons — игрок с gamepad/keyboard не видит selected button.
- **[UX-9] Нет Settings menu** — skill требует options for all (volume, sensitivity). Main Menu только Start/Continue/Quit.

**P2:**
- **[UX-10] Нет reduced-motion** — пользователь с motion sickness не может отключить head bob.
- **[UX-11] Нет loading screen** между сценами — только fade. Skill: skeleton screens or spinners.
- **[UX-12] Animation timing не consistent** — skill 150-300ms. SceneTransition fade 0.8s (слишком медленно), typewriter speed хаотичный. Нужен `UIAnimationTiming.cs` константы.

---

## Оптика 7: `frontend-design` — Design Quality, Tim's Calibrated Taste

### Принципы (из Tim's calibration profile)
- **Typography weight 7/7 MAX**: bold, heavy, assertive type always
- **Approach 2/7 utilitarian**: function drives form, no decoration
- **Mood 6-7/7 playful**: serious structure + playful surface
- **Color 6/7 colorful**: rich multi-color, not monochrome
- **Era 6/7 futuristic**: contemporary, not retro
- **Corners 6/7 rounded**: soft radii
- **Animation 6/7 animated**: meaningful motion
- **Structure 6/7 asymmetric**: grid-breaking, dynamic
- **Typography style 6/7 toward serif**: display serif headlines
- **Density 6/7 spacious**: generous whitespace

**Anti-patterns**: glassmorphism, retro, generic SaaS UI, stamp/badge design, data viz dashboards.

### Применение к Afterhumans
Это оптика для **меню, credits, HUD, landing page** — все "frontend"-элементы игры. Game world сам по себе не frontend-design.

Current MenuDresser:
- Title «ПОСЛЕЛЮДИ» fontSize 120, bold ✅ (matches Typography weight 7/7)
- Subtitle «Episode 0 — Баг в алгоритме» fontSize 32, italic ✅
- Background solid orange ⚠️ (colour 6/7 OK, но **solid не asymmetric**, нарушает structure 6/7)
- Buttons solid dark brown — ⚠️ **не playful**, utilitarian только
- Font = LiberationSans default — ❌ **не display serif** для headline, нарушает Typography style 6/7
- Layout strict grid center — ❌ нарушает structure 6/7 asymmetric

Credits UI — simple text fades, чёрный → белый. **Нет playful mood**, нет colour variation. Нарушает Mood 6-7/7 playful.

### Findings

**P0:**
- **[FE-1] Main Menu layout нарушает Tim's profile**: strict center grid, no asymmetry, no playful elements. Title + subtitle + vertical buttons stack. Нужна **poster-style** композиция: title большой off-center, subtitle слева снизу, buttons правый нижний угол, grain overlay, asymmetric whitespace.
- **[FE-2] Typography стандартный** — LiberationSans Sans-Serif везде. Tim's profile 6/7 toward serif для headlines. Нужен display serif для «ПОСЛЕЛЮДИ» (Tangerine, Playfair Display, или custom Cyrillic serif). Body — геометричный sans (Inter, GeistSans).

**P1:**
- **[FE-3] Цветовая схема monotone per scene**. Main Menu solid orange. Credits pure black/white. Tim's profile 6/7 colorful. Нужна richer palette с 3+ accents per screen.
- **[FE-4] Нет grain overlay / texture** — Tim обожает это (reference_premium_web_design). UI screens выглядят flat. Нужен SVG noise overlay или Unity UI texture on background.
- **[FE-5] Credits text layout boring** — все центрировано, statically. Должна быть asymmetric composition: большой финальный текст off-center, credits список slide-in from left, sting cursor `> _` pulse animation.

**P2:**
- **[FE-6] Нет brand mark** — нет логотипа «ПОСЛЕЛЮДИ» или иконки в меню/credits. Tim's profile Tier 1 loves brand identity systems.
- **[FE-7] Нет Easter eggs в UI** — Tim's profile «playful 6-7/7». Например secret `segfault == freedom` граффити при долгом hover над title.

---

## Оптика 8: `scroll-experience` — Cinematic Pacing (applied to camera work)

### Принципы (adapted to games)
- Scroll = narrative device, not navigation
- Moments of delight through progression
- Cinematic when needed, subtle otherwise
- Balance performance + visual impact
- Progress indicators, sticky sections, reveals

### Применение к Afterhumans
Narrative walker — это **walking scroll**. Player движется по 3 локациям как reader по long-form article. Камера = scroll position. Beats = scroll reveals. Transitions = sticky sections.

**Cinematic moments** из ART_BIBLE §6:
1. Пробуждение в кресле (первый кадр)
2. Первый выход из Ботаники (door reveal)
3. Первый взгляд на Город (slow pan)
4. Первый взгляд на Пустыню (pan справа налево по горизонту)
5. Приближение к Курсору (FOV zoom 65°→50°)

**Реализовано: 0 из 5.**

### Findings

**P0:**
- **[SCROLL-1] 5 cinematic moments не реализованы**. Это **критические narrative reveals** — без них Episode 0 читается как walking simulator без режиссуры. Требует Cinemachine package (уже установлен 2.10.7) + Timeline (1.8.10) для scripted moments.
- **[SCROLL-2] Нет progression feedback** — player не видит где находится в 15-минутном arc. Firewatch subtle показывает day 1/2/3 overlay. У нас 0. Нужен subtle chapter indicator на scene load (fade in/out).

**P1:**
- **[SCROLL-3] Transitions bетtween scenes слишком резкие** — fade 0.8s → LoadScene → fade back. Без continuity. Нужен **overlap transition**: player нажимает E на door → Cinemachine dolly-in → fade через 1.5s → new scene → Cinemachine dolly-out с новой позиции. Это «sticky section» pattern из scroll skill.
- **[SCROLL-4] Нет parallax в Desert** — dunes должны двигаться медленнее чем foreground cacti когда player ходит. Это не true parallax в 3D — это **layered depth** через far plane + depth cue. Нужно Desert background layer (distant mountains) не движущийся с player.

**P2:**
- **[SCROLL-5] Нет reveal animation на key props** — когда player впервые видит Кафку (Scene 1.1 wake-up), нужен reveal (Kafka head lift anim). Когда впервые видит Cursor — blink animation. Skill: «moments of delight».

---

## Оптика 9: `theme-factory` — Art Direction Consistency

### Принципы
- Cohesive palette + complementary fonts + distinct visual identity per theme
- Apply consistently across artifacts
- 10 themes available: Ocean Depths, Sunset Boulevard, Forest Canopy, Modern Minimalist, Golden Hour, Arctic Frost, Desert Rose, Tech Innovation, Botanical Garden, [10th]

### Применение к Afterhumans
У нас **3 themes** (одна per locale) прописаны в ART_BIBLE §3:
- **Ботаника**: `Golden Hour` theme (warm amber, temperature 3200K)
- **Город**: `Arctic Frost` theme (cool blue-grey, temperature 7500K)
- **Пустыня**: `Sunset Boulevard` или `Desert Rose` (orange sunset, temperature 2400K)

Current implementation — 3 `LightingSetup.Preset` + тинт materials per scene. **Примерно соответствует**, но не **enforced systematically**:
- Main Menu использует orange (похоже на Desert theme) — но Main Menu не в Desert scene, это нарушает theme consistency
- Credits — pure black/white — нет theme вообще
- Dialogue UI — одинаковый стиль во всех сценах → нарушает contextual theming

### Findings

**P0:**
- **[THEME-1] Главная проблема — нет theme data-driven architecture**. Должно быть:
  ```
  Assets/_Project/Art/Themes/
      Botanika.asset     (ScriptableObject SceneTheme)
      City.asset
      Desert.asset
      MainMenu.asset     (borrows Desert или свой)
      Credits.asset
  ```
  Каждый `SceneTheme.asset` содержит: primary/secondary/tertiary colours, fontAsset, ambient intensity, fog params, post-FX profile reference. Все системы (DialogueUI, Lighting, Material generators) читают active theme при scene load.

**P1:**
- **[THEME-2] Dialogue UI не themed per scene** — одинаковый panel color (0,0,0,0.7) во всех сценах. Должен быть warm brown в Ботанике, cold blue-grey в Городе, warm orange в Пустыне.
- **[THEME-3] Main Menu тема не connected к Episode theme** — просто orange solid. Должна быть Desert sunset theme match (подготавливает игрока к финалу визуально).
- **[THEME-4] Credits тема black/white vs STORY final moment** — должна использовать theme финального cursor input (5 variants — каждый свой цвет? один canonical тёплый?).

**P2:**
- **[THEME-5] Нет theme preview tool** — Dresser'ы используют hardcoded colors. Нужен Editor window `Window > Afterhumans > Theme Preview` который показывает всю палитру перед bake.

---

## Оптика 10: `3d-web-experience` — Three.js patterns (transferable to Unity)

### Принципы
- Balance visual impact + performance
- Make 3D accessible к users who've never touched 3D app
- Moments of wonder without sacrificing usability
- Model integration workflows (glTF standard)
- Interactive 3D scenes

### Применение к Afterhumans
Skill про Three.js/R3F — прямо не наш stack, но **patterns transferable**:
- **glTF standard** > FBX для современных проектов. Quaternius предлагает glTF versions. URP supports glTF import through UnityGLTF package.
- **3D accessibility**: players никогда не игравшие в narrative walker не знают «press E to interact». Skill: always provide hints.
- **Moments of wonder**: small magical moments — particle on footstep, light god rays through glass roof, bird fly across sky. Firewatch делал this well.

### Findings

**P1:**
- **[3DWEB-1] Нет magical moments** — в Ботанике ART_BIBLE §4.1 прописан *«warm volumetric fog in sun rays through windows»*. Это один конкретный *«moment of wonder»*. У нас 0 таких moments. Нужно Timeline sequence на enter Botanika = sun ray visible for 3 seconds.
- **[3DWEB-2] Нет onboarding для narrative walker conventions** — player с опытом FPS ожидает crosshair, shooting. Narrative walker не имеет этих conventions. Skill: *«make accessible to users who never played this genre»*. Нужен 10-second intro overlay в Scene 1.1.

**P2:**
- **[3DWEB-3] Kenney FBX вместо glTF** — современный стандарт glTF, но Kenney только FBX. Не фатально, но для новых assets (Quaternius, Sketchfab) предпочитать glTF.
- **[3DWEB-4] No interactive wonder в финале** — Cursor finale в STORY.md = blinking `> _` + 5 options. Skill-wise это «interactive 3D moment». Нужен custom Shader Graph cursor с pulse + particle effect.

---

## 📊 СВОДНАЯ МАТРИЦА: findings per scene × skill

| Skill | Botanika | City | Desert | Main Menu | Credits |
|---|---|---|---|---|---|
| **3d-games** | 3D-1, 3D-3, 3D-5 | 3D-1, 3D-3 | 3D-1, 3D-2, 3D-3, 3D-8 | — | — |
| **game-art** | ART-1, ART-2, ART-3, ART-4, ART-5 | ART-1, ART-2 | ART-1, ART-4, ART-5 | ART-6 | ART-5 |
| **game-audio** | AUDIO-1, 2, 4, 5, 6 | AUDIO-1, 2, 4, 6 | AUDIO-1, 2, 3, 4, 6 | AUDIO-1 | AUDIO-1 |
| **game-design** | DES-1, DES-2, DES-3 | DES-3, DES-4 | DES-4, DES-5 | — | — |
| **game-development** | GDV-1, 3, 4 | GDV-1, 4 | GDV-1, 3 | — | — |
| **ui-ux-pro-max** | UX-1, 2, 5, 6, 7 | UX-1, 2, 6, 7 | UX-1, 2, 6 | UX-3, 8, 9 | UX-11 |
| **frontend-design** | — | — | — | FE-1, 2, 3, 4 | FE-5 |
| **scroll-experience** | SCROLL-1, 5 | SCROLL-1, 3 | SCROLL-1, 2, 3, 4, 5 | — | — |
| **theme-factory** | THEME-1, 2 | THEME-1, 2 | THEME-1, 2 | THEME-3 | THEME-4 |
| **3d-web-experience** | 3DWEB-1, 2 | 3DWEB-2 | 3DWEB-2, 4 | — | — |

**Всего finding IDs: 68** (нумерация уникальная в пределах skill).

---

## 🎯 ТОП-20 КОРНЕВЫХ ДЕЙСТВИЙ (для Sprint 3 backlog)

Эти действия покрывают 80% findings cross-skill. Подготовка для Sprint 3 Plan Mode backlog.

### Foundation layer (должны быть сделаны первыми, блокируют всё)

1. **[FOUND-1] Активировать URP** — создать URPAsset + RendererAsset + патч GraphicsSettings + batch convert materials. Blocks всё визуальное.
2. **[FOUND-2] AssetPostprocessor для Kenney FBX scale** — автоматический `globalScale = 0.01` через ModelImporter hook. Blocks asset placement.
3. **[FOUND-3] AudioMixer.mixer asset** + 4 groups + expose volume params. Blocks all audio.
4. **[FOUND-4] Input System migration** — Input Actions asset + PlayerInput component + update `SimpleFirstPersonController` и `PlayerInteraction`. Blocks input polish.
5. **[FOUND-5] SceneTheme ScriptableObject architecture** — 5 theme assets + ThemeLoader at scene start. Blocks all visual consistency.
6. **[FOUND-6] BoxCollider вместо MeshCollider** во всех Dresser'ах. Blocks performance.

### Botanika Phase 1 (scene-specific backlog)

7. **[BOT-1] Scene 1.1 cinematic wake-up**: Timeline sequence 40s, Kafka head lift, note knot, tutorial overlay, first look pan. Source: STORY §3.1.
8. **[BOT-2] 5 Quaternius NPC с distinct silhouettes** — Саша (sitting), Мила (desk), Кирилл (kitchen), Николай (corner), Стас (walking at door). Plus idle anims.
9. **[BOT-3] Kafka Quaternius CC0 corgi mesh** + FSM (Idle/Follow/Sniff/Bark) + sound cues. Без этого Episode 0 = без эмоционального ядра.
10. **[BOT-4] Volume Profile Botanika** с ACES tonemap, Bloom 0.5, warm color grading, vignette 0.2, film grain 0.15, depth of field f/5.6 focus 3m.
11. **[BOT-5] Dialogue UI speaker name + typewriter 22 chars/sec + skip-on-press + theme-color panel** warm brown per Ботаника theme.
12. **[BOT-6] Interaction prompt «говорить [E]»** world-space floating text над NPC когда в radius.

### Audio Track (параллельно Botanika phase)

13. **[AUD-1] FootstepController.cs** + surface raycast + 4 footstep pool variants per surface type.
14. **[AUD-2] Placeholder tracks curl из freesound.org CC0**: wind_desert, greenhouse_ambient, city_drone, kafka_bark, kafka_paws_wood/sand, ui_click, dialogue_advance, coffee_machine, paper_rustle, coughing.
15. **[AUD-3] AudioSource prefabs** per scene ambient + 3D per NPC + ducking snapshots.

### Transition verification (после Botanika done)

16. **[TRANS-1] Smoke test harness** — automated scene walk from MainMenu to Credits через все 3 FPS scenes + dialogue + transitions. Output: 5 screenshots + PASS/FAIL log.
17. **[TRANS-2] Ink coverage walker** — script проходит все Ink knots, verifies каждый achievable через legal player input.
18. **[TRANS-3] Seamless bridge** — Kafka follow state + Ink story state carry-over через SceneTransition.

### Phase 2+3 (наследуют Phase 1 pipeline)

19. **[CITY-1/DES-1] Replicate Phase 1 pipeline** для City и Desert (Volume profiles, NPC Quaternius, Cinemachine cinematic moments, audio tracks).
20. **[FINALE-1] Cursor finale custom Shader Graph** — blinking `> _` с pulse + 5-option input UI + wire в GameStateManager.cursorInput → CreditsSequence.

---

## 🔑 CROSS-SKILL ИНСАЙТЫ

### Что повторяется по всем оптикам
1. **URP activation — блокер всех визуальных findings** (3D, game-art, game-audio indirectly through cinematic, theme, scroll, frontend). 6/10 skills explicitly требуют URP.
2. **NPC characters — блокер narrative** (game-art ART-2, game-design DES-1, scroll SCROLL-1). Без distinct NPC silhouettes и idle animations история не читается.
3. **Audio infrastructure — блокер 50% experience** (game-audio весь + game-design DES-5 rest beats + scroll SCROLL-1 cinematic moments).
4. **Cinemachine — блокер режиссуры** (3d-games camera feel + scroll cinematic + 3d-web-experience wonder moments). Package установлен, не использован.
5. **Theme data-driven — блокер consistency** (game-art ART-5 + frontend-design FE-3 + theme-factory THEME-1 + ui-ux UX-4).

### Skill-level выводы
- **«50% of experience is audio»** (game-audio) — у нас 0%. Это наш самый большой blind spot.
- **«30-second test»** (game-design) — FAILS. Первый beat должен случиться в первые 30 секунд.
- **«Art serves gameplay»** (game-art) — Kenney furniture art не serves gameplay, потому что gameplay = narrative discovery, а Kenney cubes не communicate characters.
- **«Mesh colliders everywhere = anti-pattern»** (3d-games) — у нас именно это, 500+ mesh colliders.
- **«Silhouette readability at gameplay distance»** (game-art) — наши NPC не passable test.

---

## NEXT STEP → Sprint 3

Sprint 2 закончен. Материал готов для Sprint 3 Plan Mode.

**Sprint 3 задача:**
1. Войти в **Plan Mode** (`Shift+Tab` или `/plan`)
2. На основе Sprint 1 + Sprint 2 создать 7 markdown файлов:
   - `04_RECOVERY_ROADMAP.md` — high-level phases + gates
   - `05_BACKLOG_FOUNDATION.md` — Foundation layer (6 tasks, blocks всё)
   - `06_BACKLOG_BOTANIKA.md` — Phase 1 scene backlog
   - `07_BACKLOG_CITY.md` — Phase 2 scene backlog
   - `08_BACKLOG_DESERT.md` — Phase 3 scene backlog
   - `09_BACKLOG_AUDIO.md` — audio track параллельно
   - `10_BACKLOG_TRANSITIONS_AND_TESTS.md` — bridges + smoke harness + Ink coverage
3. Каждый task имеет:
   - ID (BOT-1, CITY-3, AUDIO-5 и т.д.)
   - Finding references (e.g. *«addresses ART-2, DES-1, SCROLL-1»*)
   - Acceptance criteria (*«player видит Сашу с distinct silhouette на 10м distance»*)
   - Agent allocation (`unity-game-developer`, `3d-artist`, `code-reviewer`)
   - Dependency chain (FOUND-1 blocks BOT-4, etc.)
4. ExitPlanMode после approval от Тима → начинаем execution.

**Статус этого документа:** ✅ DONE. Зафиксировано 68 findings через 10 skill lenses, 20 корневых действий, 5 cross-skill insights.
