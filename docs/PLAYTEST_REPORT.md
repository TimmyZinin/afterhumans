# Playtest Report — Ботаника 2026-04-06

## Среда
- macOS Darwin 25.3.0, Apple M1 8GB
- Build: 125 MB, Unity 6000.0.72f1 URP, windowed 1920x1080 (960x568 Retina)
- Тестер: Claude Opus 4.6 + Computer Use + CLI screencapture

## AAA Readiness Score: 1/10

Это не игра. Это набор расставленных примитивов в коробке из flat-color стен. Ни один элемент не соответствует Art Bible. Sable, Firewatch, Journey не выпустили бы даже internal alpha с таким качеством визуала.

## 30-Second Test: FAIL

**Первые 30 секунд:** Игрок видит коричневую стену вплотную, затем мелькает комната (если повезёт с cursor lock), потом опять стену. Нет ощущения места. Нет ощущения «тёплый оазис». Есть ощущение «Unity tutorial scene without textures». Игрок не понимает ГДЕ он, ЧТО это за место, и хочет ВЫЙТИ, а не остаться.

**Art Bible ожидание:** «Как будто ты пришёл в чьё-то очень тёплое лаборатория-квартиру в час до заката.»
**Реальность:** Как будто ты застрял в текстурированной коробке из одного цвета.

---

## Оценка по категориям

### Visual Quality (game-art): 1/10

**Материалы — КРИТИЧЕСКАЯ ПРОБЛЕМА #1:**
- ВСЕ поверхности = flat solid color URP/Lit material. Ни одной текстуры.
- Пол: один коричневый цвет (#8B6F4E). Нет grain, нет tile pattern, нет normal map.
- Стены: один коричневый цвет (#BFA07A). Абсолютно плоские. Нет штукатурки, трещин, depth.
- Мебель: solid wood color. Диван = коричневый куб. Стол = коричневый куб. Книги = красноватый куб.
- Разницы между полом и стеной почти нет — один brownish tone без контраста.

**Art Bible §10:** «URP Lit Shader — PBR с roughness, metallic, normal maps. Дерево должно выглядеть как дерево.»
**Реальность:** Ни один материал не имеет roughness/metallic/normal variation. Всё — плоский albedo.

**Объекты — неузнаваемые:**
- Kenney furniture-kit модели ОЧЕНЬ низкополигональные и без текстур выглядят как геометрические примитивы
- Диван не читается как диван — просто коричневый блок
- Стол, стулья, стеллажи — всё сливается в одну массу из-за одинакового цвета
- Растения (nature-kit) — зелёные треугольники, не читаются как растения

**NPC — серые болванки:**
- Kenney blocky-characters без текстур
- Все одного цвета (default URP/Lit grey или tinted)
- Невозможно отличить Сашу от Милы от Кирилла
- Нет ощущения что это люди — скорее маннекены

**Kafka — узнаваемость 3/10:**
- 22-part procedural corgi из примитивов. Силуэт с ушами ВИДЕН — это плюс
- Но на расстоянии >2m — набор мелких тёмных форм, не корги
- Хвост не виден, ноги не различимы
- Tail wag и breathing анимация не видны на скриншотах (слишком subtle)

### Lighting & Atmosphere (3d-games): 2/10

**Главная проблема — НЕТ АТМОСФЕРЫ:**
- Sun intensity 2.5 + ambient 2.0 = всё залито ровным светом. Нет контраста.
- Нет теней, нет light/shadow interplay. Всё плоско освещено.
- Art Bible: «гипер-выраженное освещение» — реальность: плоское равномерное.
- Volumetric fog (Art Bible: density 0.015, тёплый цвет) — не виден. Нет пыльного воздуха.
- Пылинки (BotanikaAtmosphere: 120 particles) — не замечены ни на одном скриншоте.
- Accent lights (3 точки: кофе-стол, Николай, Кирилл) — одна видна (оранжевый glow справа), остальные незаметны.
- Glass ceiling at y=3.2 — не видна. Нет ощущения стеклянной крыши.
- Camera background: solid amber (0.85, 0.65, 0.40) — при виде через окна вместо неба видна плоская оранжевая заливка. Выглядит дешевле чем skybox.

**Art Bible §4.1:** «Temperature 3200K, sun angle 25°, soft shadows 0.6, ambient warm orange, volumetric fog 0.015.»
**Реальность:** Нет soft shadows (не видны). Нет volumetric. Flat ambient заливка.

### Game Design & Pacing (game-design): 3/10

**Кинематик (18с):** Технически работает — камера двигается по beats. Но:
- Beat 1 (ceiling rays): не видны лучи, только brown wall
- Beat 2 (pan to Kafka): Kafka видна как тёмный blob, не как собака
- Beat 4 (note): записка не видна
- Beat 5 (wide shot): слишком high angle, пол однородно коричневый, объекты мелкие
- Beat 6 (settle to FPS): в 50% случаев cursor lock delta разворачивает камеру в стену

**Cursor Lock Bug:**
- При переходе от кинематика к gameplay, macOS cursor lock отправляет mouse delta burst
- Freeze timer 1.5с помогает удержать body rotation, но после expire pitch сбрасывается с 15° до ~0°
- Результат: в ~50% запусков игрок смотрит в стену, в ~50% — в комнату

**Движение:**
- WASD работает (confirmed: hold_key W двигает игрока)
- Walk speed 2.0 m/s — ощущается нормально
- Head bob — не замечен на скриншотах
- Strafe (A/D) работает

**NPC Interaction:**
- Prompt "[E] говорить" ПОЯВЛЯЕТСЯ (confirmed: текст виден у NPC Стаса)
- E press через Computer Use НЕ достигает игры (CU display unavailable → key events идут в Terminal)
- Тест диалогов невозможен через автоматические инструменты
- Нужен ручной тест

**Player position bug:**
- Игрок стабильно оказывается у южной стены (z=-5.0) вместо центра комнаты
- Причина: Stas NPC at (0, 0, -3.5) с CapsuleCollider выталкивает игрока at spawn (0, 0.02, -4.0)
- Fallback: cursor lock delta поворачивает игрока → W идёт к стене

### UI/UX (ui-ux): 4/10

**Interaction prompt:**
- "[E] говорить" — виден, жёлтый текст, worldspace. Но шрифт мелковат.
- Не пульсирует (или пульсация слишком subtle для скриншотов)

**Chapter title "I. Ботаника":**
- Виден на одном скриншоте кинематика — белый текст на тёмном фоне
- Читаемый, стилистически нормальный

**Dialogue panel:**
- НЕ ТЕСТИРОВАНО (E key events не доходят до игры)

**Tutorial overlay:**
- В предыдущих тестах виден ("WASD — ходить", etc.)
- Исчезает после 5 секунд — корректно

**Graffiti "segfault == freedom":**
- Видна часть текста "seg" / "se" на стене. Красный цвет.
- **ОТЗЕРКАЛЕНО** — текст читается справа налево. BUG-03 из бэклога.

### Kafka (game-art + game-design): 2/10

**Visual:**
- Силуэт с ушами — единственный положительный момент. На фоне пола различим как "что-то маленькое с ушами".
- Не узнаваем как корги на расстоянии >2m. Мелкие детали (нос, глаза, брови, лапы) не видны.
- Чёрный цвет корги сливается с тенями.

**Animation:**
- Breathing/tail wag/ear twitch — scripted, но не видимы на скриншотах (слишком subtle, маленький объект).

**Follow behavior:**
- KafkaFollowSimple — код есть, но на скриншотах Kafka стоит на месте (не подтверждено движение за игроком из-за ограничений тестирования).

**Emotional weight: 0/10**
- Art Bible: «Kafka — эмоциональный якорь всей игры. Quadruped rig, idle/sit/walk/bark/sniff/tail_wag.»
- Реальность: набор из 22 примитивов, не вызывает НИКАКИХ эмоций. Это props, не companion.

### NPC & Dialogue (game-design + ui-ux): NOT TESTED (2/10 visual)

**Visual:**
- 5 NPC в сцене (confirmed by BotanikaNpcPopulator logs)
- Все используют Kenney blocky-characters без текстур
- НЕРАЗЛИЧИМЫ друг от друга
- Interaction prompts видны (yellow "[E] говорить")
- NpcIdleBob animation — не заметна

**Dialogue:**
- DialogueUI subscribed to events (confirmed by log)
- DialogueManager wired with dataland.ink (confirmed)
- Actual dialogue flow — NOT TESTED (E key не доходит)

---

## Критические баги

### BUG-01: Cursor lock delta разворачивает игрока в стену
- **Severity:** CRITICAL — разрушает first impression
- **Шаг:** кинематик заканчивается → control handoff
- **Ожидание:** Игрок смотрит вперёд в комнату (pitch 15°, yaw 0°)
- **Реальность:** В ~50% запусков cursor lock delta разворачивает body на 90-180°, pitch сбрасывается к 0°. Игрок видит стену вплотную.
- **Данные:** pos=(0.94, 0.18, -5.03), pitch=0.125 (вместо 15)
- **Файл:** SimpleFirstPersonController.cs — freeze timer недостаточен, mouse delta после expire сбрасывает pitch

### BUG-02: Игрок выталкивается к стене при spawn
- **Severity:** HIGH
- **Шаг:** запуск → player spawn at (0, 0.02, -4.0)
- **Ожидание:** Игрок стоит свободно в комнате
- **Реальность:** NPC Stas at (0, 0, -3.5) с CapsuleCollider выталкивает игрока на 0.3m в сторону + cursor lock delta толкает к южной стене
- **Файл:** BotanikaDresser.cs — player spawn, BotanikaNpcPopulator.cs — Stas position

### BUG-03: Graffiti отзеркалено
- **Severity:** MEDIUM
- **Шаг:** посмотреть на стену с граффити
- **Ожидание:** "segfault == freedom" читается слева направо
- **Реальность:** текст отзеркалирован (видно "seg" справа налево)
- **Файл:** BotanikaEnvProps.cs или BotanikaAtmosphere.cs — rotation quad

### BUG-04: Camera background solid color выглядит хуже skybox
- **Severity:** MEDIUM
- **Шаг:** посмотреть в окно
- **Ожидание:** вид неба / заката
- **Реальность:** плоская оранжевая заливка. Дешевле чем HDRI skybox.
- **Файл:** LightingSetup.cs — camera.clearFlags = SolidColor

---

## Top 5 Visual Impact Fixes

1. **ТЕКСТУРЫ НА МАТЕРИАЛЫ** — procedural noise/pattern или CC0 textures на пол (tile grid), стены (plaster), мебель (wood grain), ткань (fabric). Это единственный фикс который трансформирует сцену из "прототипа" в "игру". Impact: +3 к visual score.

2. **ОСВЕЩЕНИЕ С КОНТРАСТОМ** — убрать flat ambient fill. Добавить выраженные shadows, light/dark zones, god rays через окна. Один сильный directional + мягкий ambient + акцентные spots. Impact: +2.

3. **SKYBOX ОБРАТНО** — вернуть HDRI skybox вместо solid color. Решение: снизить exposure skybox в VolumeProfile чтобы не засвечивал интерьер, но за окнами было красивое небо.

4. **CURSOR LOCK FIX** — гарантировать что после кинематика камера ВСЕГДА смотрит в правильном направлении. Заморозить body rotation + pitch на 2-3 секунды ПОЛНОСТЬЮ (без потребления mouse delta).

5. **KAFKA SCALE + CONTRAST** — увеличить Kafka в 1.5-2x, добавить white/bright элементы для контраста с тёмным полом. Сейчас корги = тёмный blob на тёмном фоне.

---

## Sprint Backlog (приоритезированный)

### Sprint 2: World Fundamentals (BLOCKING)
1. Fix cursor lock — полная заморозка yaw + pitch на 3 секунды после cinematic, body rotation = identity
2. Move player spawn away from Stas — z=-4.5 или move Stas to z=-2.5
3. Вернуть HDRI skybox с пониженной exposure
4. Graffiti orientation fix

### Sprint 3: Materials Revolution
1. Procedural floor texture (tile grid pattern через code)
2. Procedural wall texture (plaster noise)  
3. Wood grain на мебель (noise-based)
4. Fabric material на диван/кресла
5. Glass material на окна (прозрачность + отражение)

### Sprint 4: Lighting & Atmosphere
1. Sun: angle по Art Bible (25°, -45°), shadows VISIBLE
2. Kill flat ambient — lower to 0.5, let sun/accent do the work
3. Accent lights: STRONGER, create pools of light
4. Volumetric fog или haze material visible
5. Dust particles — bigger, brighter, more visible

### Sprint 5: Kafka & NPC
1. Kafka scale 2x, brighter white markings, contrast fix
2. NPC textures from Kenney pack (texture-a..e.png)
3. Per-NPC color differentiation (shirt colors)
4. NPC scale/position verify (all at correct height)
5. Test E interaction (manual playtest required)

### Sprint 6: Post-FX & Polish
1. VolumeProfile audit vs Art Bible §5
2. Bloom tune — more pronounced on lights/windows
3. Film grain visible
4. DoF — focus distance 3m, noticeable background blur
5. Vignette — subtle edge darkening

### Sprint 7: Environment Details
1. More props: cups, laptop, whisky bottle, soldering iron, foil hat
2. Bookcase with visible books
3. Coffee machine near Kirill
4. Server rack LED more visible
5. Plants — more variety, better colors

### Sprint 8: Final Polish & Verification
1. Full manual playtest (keyboard + mouse)
2. All 5 NPC dialogues
3. Gate mechanic (Nikolai → door)
4. Kafka follow behavior
5. Final screenshot comparison with Art Bible
