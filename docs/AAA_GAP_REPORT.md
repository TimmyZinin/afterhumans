# AAA Gap Report — Ботаника Scene 1
> Дата: 2026-04-06
> Тестер: Claude Opus 4.6 (QA playtest + code review)
> Build: 126MB, Unity 6000.0.72f1, windowed 1280x720

## Резюме

Сцена **функционально работает** (23/23 verification, 0 crashes), но визуально находится на уровне **прототипа**. До AAA narrative walker (Sable/Firewatch/Journey уровень) — огромный gap.

Главная формула Art Bible: «свет важнее текстур, 100 объектов в правильном свете > 10000 в плоском». Сейчас 100+ объектов в **плоском свете** с **solid-color материалами**.

---

## КРИТИЧЕСКИЕ ПРОБЛЕМЫ (блокируют AAA)

### GAP-01: Kafka = 2 куба
- **Текущее:** `SceneEnricher.cs:503-557` — `CreateKafkaPlaceholder()` создаёт 2 куба (тёмный body 0.6x0.3x0.3 + белый chest 0.3x0.15x0.25)
- **AAA-целевое:** Узнаваемый корги с body/head/4 legs/tail/ears + idle bob + tail wag + follow player
- **Почему critical:** Kafka — эмоциональный якорь всей игры. Art Bible §7: "Кафка (корги-кардиган, чёрно-белая, quadruped rig) — главный вызов"
- **Fix:** Процедурный корги из 12-15 примитивов (capsules/spheres/boxes) с правильными пропорциями. Не нужен FBX — нужна форма.

### GAP-02: Камера после кинематика смотрит в skybox
- **Текущее:** `BotanikaIntroDirector.cs:76` сохраняет `_finalCamRot = playerCamera.transform.rotation` — начальная ротация камеры.
- **Проблема:** Player spawn rotation = `Quaternion.identity` (BotanikaDresser.cs:279), камера смотрит в +Z. Но сцена расположена так что +Z = в сторону стены/окна → skybox.
- **Fix:** Изменить `_finalCamRot` или player spawn rotation чтобы камера смотрела вниз на ~5-10° и в сторону интерьера (в сторону дивана Саши).

### GAP-03: Flat solid-color материалы
- **Текущее:** `LoadOrCreateLit()` создаёт URP/Lit с одним цветом. Пол, стены, мебель — одноцветные плоскости.
- **AAA-целевое:** Art Bible §10: PBR с roughness, metallic, normal maps. Дерево должно выглядеть как дерево. Ткань как ткань.
- **Fix:** Процедурные текстуры через код (checkerboard floor, wood grain через noise, fabric weave) или загрузка CC0 текстур.

---

## ВЫСОКИЙ ПРИОРИТЕТ

### GAP-04: NPC — серые болванки
- **Текущее:** `BotanikaNpcPopulator.cs` спаунит Kenney blocky-characters (character-a.fbx..character-e.fbx) с flat tint материалом.
- **AAA-целевое:** Каждый NPC визуально различим. Art Bible §7: разные позы, одежда, силуэты.
- **Fix:** Применить Kenney текстуры (texture-a..e.png из blocky-characters pack), дать каждому свой цвет.

### GAP-05: Освещение не соответствует Art Bible
- **Art Bible §4.1:** Temperature 3200K, intensity 1.2, angle 25°, shadows soft 0.6, ambient warm orange #F5D8A3, volumetric fog density 0.015
- **Текущее:** Lighting настроен через `LightingSetup.cs` — нужно верифицировать что параметры точно соответствуют Art Bible.
- **Fix:** Audit LightingSetup.cs, добавить volumetric fog (URP fog), light shafts через window geometry.

### GAP-06: Post-processing не выдрочен
- **Art Bible §5:** White Balance +15/-5, Saturation +10, Contrast +5, Shadows cool (6B7A85), Highlights warm (F5D8A3), DoF focus 3m aperture 5.6
- **Fix:** Audit VolumeProfileBuilder.cs, точно выставить все 9 overrides по Art Bible.

---

## СРЕДНИЙ ПРИОРИТЕТ

### GAP-07: Недостаточно environmental storytelling
- **Art Bible §12:** чашки, ноутбук, бутылка виски, блокнот, паяльник, шапочка из фольги
- **Текущее:** BotanikaEnvProps.cs добавляет server rack, graffiti, NPC stations — но мало мелких деталей
- **Fix:** Добавить больше мелких Kenney пропсов на столы и вокруг NPC.

### GAP-08: Пылинки и particles недостаточно выразительны
- **Art Bible §11:** ~50 частиц, медленные, следуют свету через окно. Пар от кофемашины.
- **Текущее:** BotanikaAtmosphere.cs создаёт dust particles — нужно верифицировать видимость и настройки.

### GAP-09: Vegetation слишком примитивная
- **Текущее:** Kenney nature-kit bushes/trees — очень low-poly, look like toys.
- **Fix:** Больше variation, scaling, rotation. Может дополнительные пропсы. Правильный материал листвы (не flat green).

### GAP-10: Graffiti "segfault == freedom" не верифицировано
- **BACKLOG BUG-03:** возможно отзеркалено. Фикс rotation применён, но не проверен в runtime.

---

## СПРИНТ-ПЛАН v2 (обновлённый после теста)

### Sprint 1: Camera Fix + Kafka Corgi [CRITICAL]
**Scope:** Две самые важные проблемы.
1. Fix camera orientation after cinematic (GAP-02)
2. Build procedural corgi from ~15 primitives (GAP-01)
3. Idle animation (subtle bob + tail wag)
4. Follow player behavior (already exists in KafkaFollowSimple.cs)
5. Build → Test → Screenshot

### Sprint 2: Materials & Lighting [HIGH]
**Scope:** Визуальная трансформация сцены.
1. Procedural textures: wood grain, tile pattern, fabric, glass
2. Lighting audit vs Art Bible §4.1
3. Volumetric fog
4. Color temperature correction
5. Build → Test

### Sprint 3: NPC Textures + Post-FX [HIGH]
**Scope:** Персонажи + финальная картинка.
1. Apply Kenney character textures
2. Per-NPC color differentiation
3. Post-FX audit vs Art Bible §5
4. White Balance, Shadows/Midtones/Highlights tune
5. Build → Test

### Sprint 4: Environment Details + Particles [MEDIUM]
**Scope:** Мир чувствуется живым.
1. Extra props: cups, laptop, whisky, soldering iron
2. Enhanced dust motes in light beams
3. Coffee steam particle near Kirill
4. Server rack LED improvement
5. Build → Test

### Sprint 5: Final Polish + Full Playthrough [MEDIUM]
**Scope:** Всё вместе создаёт опыт.
1. Vegetation variety
2. Graffiti verification
3. Camera cinematic fine-tune
4. Full gameplay test (dialogs, NPCs, gate)
5. Final screenshot comparison
