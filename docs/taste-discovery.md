# Taste Discovery — Kafka's Walk references

**Mode:** HITL Discovery
**Format:** 5 rounds × 3 games, feedback after each round
**Goal:** Calibrate Tim's vision for Kafka's Walk game

## Initial fix (before rounds)

**Already shown references:**
1. **Alto's Odyssey** — ✅ LIKED (music, aesthetic)
2. **Bruno Simon** — (tech reference only, not gameplay)
3. **A Short Hike** — ⚠️ PARTIAL LIKE (freedom + graphics, but "not quite it")

**Explicitly NOT wanted:**
- Arcade gameplay
- Coin collecting
- Score-chasing

**Wanted direction:**
- Open world walking with character (3rd person, camera follows)
- **Painterly realism** — realistic but "hand-painted" feel (like our v2 Kafka concept arts)
- Walking Dead (Telltale) style reference confirmed
- Very beautiful, observable spaces
- Meditative, contemplative
- WASD movement, dog walks through environment
- Character moves through atmosphere
- Art style = our generated concept art = target visual

---

## Round 1 feedback (2026-04-10)

| Game | Verdict | Notes |
|------|---------|-------|
| **Life is Strange** | ✅ LIKED visual, ❌ story | Анимация, визуализация, синематики, трейлер — точно то что хотим. Сюжет — другое. |
| **The Walking Dead Telltale** | (no feedback) | Тим не упомянул |
| **Tell Me Why** | ✅✅✅ ИДЕАЛ | Музыка, трейлер, персонажи, визуал, эстетика, подача, загадочность, **переключение камер** — точно то что хочет. |

### 🔑 КРИТИЧЕСКИЙ ИНСАЙТ ОТ ТИМА (меняет всю концепцию)

**Новая концепция: "Painterly Panel Game"**

- Игра = серия статичных **планов/сцен** (не open world)
- Каждый план = **painterly арт** (наши v2 концепт-арты = финальный рантайм визуал)
- **Кафка = единственный консистентный персонаж**, перемещается между планами
- NPC на арте **"оживают"** когда Кафка входит в кадр: двигаются, анимируются, взаимодействуют
- Когда Кафка выходит — у NPC своя жизнь (работают, сидят, звуки)
- **Разные камеры на разных сценах** — Кафка "заходит" в кадр
- Игра должна быть **маленькой** — "превью"
- НЕТ full 3D open world
- НЕТ необходимости создавать 3D объекты — **берём арты и оживляем персонажей прямо на них**

### Дополнительно упомянул

- **Journey** — нравится открытый мир, прогулка по пустыне. НЕ нравятся элементы аркады
- **Sword of the Sea** — открытый мир ok, эстетика и музыка нет
- **Heart Chain Kitty** — категорически нет

## Round 2 feedback (2026-04-10)

| Game | Verdict | Notes |
|------|---------|-------|
| **Kentucky Route Zero** | ⚠️ MIXED | Эстетика, звук, трейлер, живой мир — круто. НО слишком 2D, не попадает в референс. Элементы можно брать. |
| **Oxenfree** | ❌ не открылся (SSL) | — |
| **Night in the Woods** | ❌ пустой сайт | — |

### Доп. инсайт
- **Walking Dead Telltale S1 (Lee + Clementine)** — ✅✅ "ОЧЕНЬ нравится". Это эталон cel-shaded painterly cinematic narrative
- **3D, не 2D** — точно
- **Живой мир** — важно, оживающие NPC круто

## Round 3 feedback (2026-04-10)

| Game | Verdict | Notes |
|------|---------|-------|
| **Walking Dead S1** | (не прокомментировал) | — |
| **A Plague Tale: Innocence** | ✅✅✅✅ HOLY GRAIL | "Очень очень очень очень круто, мега погружение, ВАУ". Сомнение: "потянем ли качество?" |
| **Draugen** | (не прокомментировал) | — |

### Ключевой вывод

**A Plague Tale = визуальный эталон.** Painterly realism в 3D. НО студия Asobo 200+ человек, 5 лет. Нам нужен тот же стиль в реалистичном масштабе.

**Хорошая новость:** наши v2 концепт-арты уже имеют painterly realism A Plague Tale-level. Если использовать их как backgrounds + Кафка движется поверх + cinematic camera switches — визуально получим такой же уровень.

Round 4 — показать games ТОГО ЖЕ визуального стиля сделанные **малыми командами/одиночками** — доказать что это достижимо.

## Round 4 feedback (2026-04-10)

| Game | Verdict | Notes |
|------|---------|-------|
| **Season: A Letter to the Future** | ✅✅ LOVED | "Красивый вайб, элегантное оформление, чуть-чуть мультяшное, свободный мир, красиво, приятное ощущение". Прям близко к тому что хочется |
| **Omno** | ❌ | "Слишком по-игровому, слишком 3D-шно, не тот вайб". ВАЖНО: "игровой" 3D style — не то |
| **The Pathless** | ❌❌ | "Не нравится ничего — ни вайб, ни графика, ни музыка, ни трейлер" |

### Key insight от Round 4
**Painterly realism + slight elegant stylization = taget.** НЕ чисто реалистично (boring) и НЕ чисто stylized 3D (Omno — "слишком по-игровому"). Нужна та промежуточная зона элегантного 3D с painterly цветокоррекцией — как Season, A Plague Tale, Tell Me Why, Life is Strange.

### Это кардинально упрощает техническую реализацию

- Один HTML + canvas (или HTML+DOM+CSS animations)
- 3 арта как background (уже есть)
- Кафка как 2D sprite sheet или rigged 2D character (generate from our 3D model)
- NPC как cutout layers с micro-animations (breathing, gestures, dialogue bubbles)
- WASD/click → Кафка ходит по плану
- Enter zone → NPC reacts
- Exit → crossfade next scene
- **Three.js не нужен.** Можно даже просто canvas + sprite animation

## Round 2 feedback

## Round 3 feedback

## Round 4 feedback

## Round 5 feedback (2026-04-10)

| Game | Verdict | Notes |
|------|---------|-------|
| **Firewatch** | ⚠️ OK но overkill | Открытый мир круто, эстетика классная, НО overengineering. FIRST PERSON НЕ ПОДХОДИТ — у нас от 3rd person должна собака |
| **What Remains of Edith Finch** | ❌ | First person — не наш случай. Отголоски стилистики есть, но не то |
| **Virginia** | ✅✅✅ КРУТЕЙШАЯ эстетика + вайб | Но ГЛАВНОЕ: **сквозная narrative через все сцены (детектив, расследование)** — это то чего нам не хватало |

### 🔑🔑🔑 ГЛАВНЫЙ ИНСАЙТ — NARRATIVE HOOK

**Проблема:** До сих пор у нас 3 сцены были разобщены — Ботаника, Город, Пустыня без сквозного смысла.

**Решение от Тима:** Сквозная история **"Кафка ищет хозяина"**

#### Сюжетный арк

1. **Ботаника (start)** — Кафка оставлена/потеряна, живёт у странных людей в оранжерее. Они её кормят, гладят, но она не их. У неё есть ощущение — надо идти искать. Её хозяин где-то потерялся. Может быть — она видит что-то (запах, фото, ошейник) что запускает поиск.

2. **Город (middle)** — Прогноз/машина "украла" хозяина. В Городе downgraded-humans — люди превращённые в оптимизированные версии себя. Кафка проходит через них. Анна у фонтана может намекнуть — "у моего... был такой же пёс. Но я перестала помнить". Намёки что хозяин Кафки был когда-то здесь.

3. **Пустыня (final)** — Машина/сервер где "души" потерянных людей. Кафка находит там хозяина — **подключённого к машине**. Она не говорит, не думает — она **лает**. Её лай каким-то образом **освобождает** хозяина. Финальный кадр: хозяин поднимает её (но мы НЕ видим его лица — только руки, силуэт). Катарсис.

**Ключевое:** мы НЕ видим лицо хозяина — это работает на несколько уровней:
- Universal (каждый игрок видит своё воспоминание)
- Тайна (тема остаётся открытой)
- Эмоциональный удар без риска плохого face model

### Финальные параметры ВКУСА (синтез после 5 раундов)

**Визуальный стиль:** Painterly realism + slight elegant stylization (не "игровое 3D", не cartoon, не фотореализм — промежуточная зона как Tell Me Why / Life is Strange / A Plague Tale / Season / Virginia)

**Техника:** Cinematic camera switches между сценами (Virginia). НЕ open world. Каждая сцена = отдельная "мини-игра" с фиксированной/cinematic камерой.

**Camera:** 3rd person, следует за собакой. Плавная, атмосферная. В ключевые моменты — cinematic cuts на wide shots или close-ups.

**Narrative:** Сквозная история про поиск хозяина. 3 акта через 3 локации.

**Mechanics:** Ходьба (WASD) + приближение к NPC (auto-trigger) + финальный лай (пробел в последней сцене).

**Scope:** Маленькая игра, ~10-15 минут прохождения, "превью" уровня.

**Что НЕ:** First person, combat, сборка вещей, сложное 3D, open world большой, аркада, Minecraft-look, чисто мультяшное, cel-shaded без painterly base.

**Отсылки к играм для реализации:**
- **Virginia** — camera switching principle (ГЛАВНОЕ)
- **Season: A Letter to the Future** — вайб, painterly realism, медитативность
- **Tell Me Why** — overall aesthetics, dialogue triggers, сюжетный тон
- **Life is Strange** — анимация, cinematic moments
- **A Plague Tale** — painterly 3D environments (визуальный потолок)
- **Walking Dead Telltale S1** — cinematic story delivery, emotional moments

---

## Final synthesis
