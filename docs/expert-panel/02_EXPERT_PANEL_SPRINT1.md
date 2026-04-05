# 02 · Expert Panel — Аналитический Спринт 1

> **Дата:** 2026-04-05 ~22:00
> **Фасилитатор:** Claude (skill-level, expert-panel skill invoked)
> **Статус:** СБОР МАТЕРИАЛА для Спринта 2 (разнос через все gamedev skills) и Спринта 3 (roadmap).
> **Не содержит:** финального плана. Это исходник для следующих спринтов.

---

## SCOPE

**Что:** Жёсткая gap analysis между изначальным заданием (docs/ART_BIBLE, docs/SCOPE, docs/GDD) и текущим состоянием Unity narrative walker «Послелюди / Afterhumans Episode 0».

**Constraints:**
- MacBook Pro M1 8GB RAM (жёсткий хардварный потолок)
- $0 budget (CC0/free assets only)
- Unity 6 LTS, URP 17.0.4 package установлен но не активирован
- Русский язык, TV-MA tone, 15-минутное прохождение, 11 NPC, Кафка-корги
- Референсы Art Bible: Sable, Death Stranding, Tchia, Journey, Disco Elysium, Observation, Firewatch

**Blast radius:** `business-critical` — это публичный проект на GitHub TimmyZinin/afterhumans, финальная цель DMG для друзей. Публикация «набора кубов» = reputation damage хуже отсутствия релиза.

**Decision type:** Stress-test существующего подхода + gap analysis + design recovery direction.

**Trigger фразы Тима (дословные):**
- *«пока это просто набор кубиков... больше похожи на Майнкрафт, а не на полноценную игру»*
- *«Это просто какая-то хуеплется»*
- *«Провести сравнение того говна высранного, которое было сейчас сделано, с тем, какие были изначально поставлены задачи»*

---

## PANEL

Шесть экспертов game industry с намеренно конфликтующими biases:

| Роль | Bias | Сигнатурный вопрос |
|---|---|---|
| **Art Director** (10+ лет AAA, стилизованные walkers) | Визуальное впечатление важнее технических деталей | *«Это впечатляет? Это хочется показать?»* |
| **Technical Artist** (Unity URP/HDRP pipeline expert) | Pipeline integrity — нельзя нарушать | *«Это повторяемо и поддерживаемо?»* |
| **Game Designer** (narrative walker specialist, Firewatch/Edith Finch) | Pacing и emotional beats важнее визуала | *«Это вызывает эмоции? Игрок понимает куда идти?»* |
| **Narrative Designer** (Ink/Twine, сценарий → интерактив) | Сценарий — это не текст, это scene-by-scene experience | *«Играется ли STORY.md в игре, или только в .md?»* |
| **Audio Director** (film+game sound, Hans Zimmer Dune, Disasterpeace) | Звук = 50% emotional impact | *«Что слышит игрок сейчас и что должен?»* |
| **Senior QA Lead** (ship gate, publishing) | Функциональная законченность прежде всего | *«Это можно показать плейтестеру не стыдясь?»* |

---

## INDIVIDUAL ANALYSIS

### 🎨 Art Director

**Assessment:** Проект визуально в состоянии Unity Asset Store прототипа 2015-го года. Kenney low-poly имеет право на жизнь как stylistic choice (Tchia делает похожее), но в Tchia это **осознанный low-poly look с custom shaders, PBR textures, dynamic sky, volumetric clouds**. Здесь — placeholder assets с solid color tint. Разница между «stylized low-poly game» и «Kenney prototype» = два слоя: (a) custom shader, (b) carefully composed lighting with post-FX. Оба слоя отсутствуют.

**Risks:**
- Выбор Firewatch/Journey reference + Kenney assets = **фундаментальный mismatch**. Firewatch использовал carefully hand-crafted terrain + volumetric fog + masterful color grading. Kenney furniture kit — generic props, они никогда не сложатся в Firewatch look даже с post-FX.
- Без custom materials каждая сцена читается как «scatter prefabs on a plane». Нет environmental storytelling, нет composition guides (leading lines, rule of thirds, depth).
- Current lighting = один Directional Light + ambient. Firewatch имеет 3-5 contextual lights per area + volumetric god rays + baked GI. Дистанция между текущим и нужным — космическая.

**Recommendation:**
- **Забрасываем Kenney furniture-kit + city-kit** полностью. Они prototype-grade и не дотянут.
- **Оставляем nature-kit частично** — trees/rocks/plants можно использовать как silhouettes если применить custom stylized shader.
- **Главная работа — environment art**: Unity Terrain tool для ландшафтов + Poly Haven PBR textures + Sketchfab CC0 props + custom handmade compositions через ProBuilder.
- Без Unity Terrain для Desert + Botanica exterior не будет никакого Firewatch look.

**Open question:** Готов ли Тим принять что 80% текущих assets выкидываются? Это полная переделка, не polish pass.

---

### ⚙️ Technical Artist

**Assessment:** Технически проект в состоянии 2014-го года. Built-in Render Pipeline без post-FX в 2026-м — выбор который нельзя оправдать когда URP package уже установлен. Shader Graph package есть — не используется. Cinemachine есть — не используется. Visual Effect Graph есть — не используется. Timeline есть — не используется. **Это 50+ MB установленных пакетов которые не приносят ни одного пикселя результата.** Весь stack заточен на AAA-quality stylized pipeline, но мы отказались от него в пользу Standard shader + scatter placeholders.

**Risks:**
- Built-in pipeline **не поддерживает** URP Volume-based post-FX. Bloom, Tonemapping, Color Grading, Vignette, Depth of Field — ничего из этого нельзя просто «включить» на Built-in (legacy PPv2 есть, но deprecated и хуже URP Volume).
- Переключение на URP **в середине проекта** болезненно: все Standard materials становятся magenta до `Edit > Rendering > Materials > Convert Selected Built-in Materials to URP`. Это автомат, но возможны edge cases (custom shaders не конвертируются, materials с неизвестными keywords).
- Kenney FBX с `UnitScaleFactor: 100` + Unity FBX importer `useFileUnits: 1` дают непредсказуемый scale. Это **НЕ** решается дёрганьем `transform.localScale` — это решается установкой `globalScale: 0.01` в meta-файлах через `ModelImporter` API или `AssetPostprocessor` hook.
- Нет Volume Profile asset в проекте → нет per-scene post-FX настроек из ART_BIBLE §5.

**Recommendation:**
- **P0 блокер: активировать URP**. Без этого ни один следующий шаг из ART_BIBLE невозможен.
- Создать `Afterhumans URP Asset` + `Afterhumans URP Renderer` через `Assets > Create > Rendering > URP Asset`. Прописать в `GraphicsSettings.m_CustomRenderPipeline` и `QualitySettings`.
- Batch convert всех материалов через `UnityEditor.Rendering.Universal.URPConverter`.
- Создать 3 Volume Profile assets: `VP_Botanika.asset`, `VP_City.asset`, `VP_Desert.asset` с overrides по ART_BIBLE §5.
- `AssetPostprocessor` скрипт который автоматически ставит `globalScale = 0.01` для всего в `Assets/_Project/Vendor/Kenney/`.

**Open question:** Сколько минут времени M1 8GB потребует Edit > Convert Materials для 500+ FBX? Может зависнуть на 20-30 минут. План Б нужен (manual conversion per pack).

---

### 🎭 Game Designer

**Assessment:** Gameplay loop — walking + E press + текст. Это базовая формула narrative walker и **она работает даже с кубами**, при одном условии: **каждая встреча должна быть поставлена**. В Firewatch каждый walkie-talkie звонок — это **mise-en-scène**: точное время суток, точная погода, точка где ты стоишь, угол камеры. В текущем проекте NPC расставлены **случайно** в массиве кубов. Нет режиссуры, нет мизансцены, нет emotional beat'ов.

**Risks:**
- **Main issue**: игрок не понимает куда идти. Currently placeholder cube at (0, 1, 3), Player spawned at (0, 1.1, -12). Нет визуальной подсказки что это NPC. Firewatch решает через silhouette distinct от environment + hover effect + ambient voice line.
- **Kafka — самое важное в narrative** (эмоциональный крюк Episode 0 = Прогноз впервые замечает собаку). Текущий 2-cube placeholder стирает весь эмоциональный вес. Это equivalent удалению Delilah из Firewatch. **Без Kafka Episode 0 перестаёт быть тем эпизодом который описан в STORY.md.**
- **Pacing нарушено**: Ink story has 593 lines, rich dialogue, но Player может пропустить весь диалог нажав E → через stage trigger за 30 секунд. Нет force pause moments, нет camera locks на важных линиях.
- **Нет emotional beats**. Firewatch-стиль делает 3-5 major emotional moments за 15 минут. У нас нет ни одного: вход в Ботанику — серо, разговор с Сашей — кубики, встреча Анны — не реализовано, Кафка triggering memory — не реализовано.
- **NPC idle animations = 0**. В Firewatch даже Delilah есть анимированная силуэт на башне. Наши NPC стоят как статуи в buffer space.

**Recommendation:**
- **Pacing first, visuals second**. Неважно если всё кубы — важно чтобы Ink knot Anna memory actually triggered когда Kafka approaches. Это 20-30 минут кода но даёт весь emotional weight.
- **NPC silhouettes must be recognizable**. Даже если cubes — добавить name plate «Саша» над каждым (world-space text), добавить sound cue при approach, добавить particle indicator (light glow). Firewatch без этого не был бы Firewatch.
- **Camera режиссура**: Cinemachine virtual cameras в каждой ключевой точке. Первый взгляд на Сашу — VCam с dolly in. Первый выход из Ботаники — VCam с slow pan. Это **обязательное** для narrative walker, без этого игра = просто ходьба + текст.
- **Kafka presence**: даже placeholder должен реагировать на игрока — звуки (даже bark.wav один), движение за игроком с variable distance (уже есть в KafkaFollowSimple, но без визуала не читается).
- **Dialogue pacing**: force 1.5s pause между lines при typewriter, force player press key to advance, показывать speaker name сверху реплики.

**Open question:** Тим сказал полный 15-min scope. Реалистично это 30-40 часов работы. Предложение: **Phase 1 = Ботаника AAA (10 часов)**, **Phase 2 = City + Desert (15-20 часов)**. Это сохраняет полный scope, но через gates.

---

### 📖 Narrative Designer

**Assessment:** Это самая болезненная часть. `docs/STORY.md` — 326 строк детального scene-by-scene сценария с **mise-en-scène** каждой сцены (первый кадр, первое действие, story beat, camera moment, audio cue для каждого из 11 NPC). **В игре ни один из этих beats не реализован.** STORY.md описывает Episode 0 как фильм: крупный план луча через стеклянную крышу → pan вниз → Кафка поднимает голову → листок бумаги → tutorial overlay → первый взгляд на Ботанику в ширину. Текущая реализация: Player спавнится где-то в кубическом зале с цветными блоками, никакого кинематического входа, никакого первого листка, никакого Кафка-reveal.

**Script-to-Game gap по секциям STORY.md:**

| STORY.md секция | Что должно быть | Что реально |
|---|---|---|
| **1.1 Пробуждение (0:00-0:40)** | Крупный план solar ray → pan down → Кафка поднимает голову → листок `note` knot → tutorial overlay | ❌ Ничего. Player spawn, walk. Нет `note` knot trigger, нет Кафка reveal |
| **1.2 Свобода в Ботанике (0:40-6:00)** | 5 NPC с distinct positions + scripted idle animations + specific dialogue beats | ⚠️ Только Саша cube в (0,1,3). Мила/Кирилл/Николай/Стас **не существуют в сценах** как NPC. Ink knots написаны, но NPC нет |
| **1.3 Ключевая экспозиция Николая** | Николай сидит за столом с бутылкой, поворачивается когда подходишь, даёт gate-opening monologue | ❌ Николая нет вообще. Ink gate `met_nikolai → door_to_city_open` никогда не trigger |
| **2.1 Переход в город** | Дверь открывается после Николая, выход на улицу, первый взгляд slow pan | ⚠️ SceneExitTrigger есть + Ink gate прописан, но Николая нет → gate невозможно пройти кроме cheat |
| **2.2 Встреча с Анной** | Анна сидит у фонтана, Кафка приближается → trigger Anna memory knot → emotional peak | ❌ Анны нет. `anna_memory` knot в Ink есть но не trigger. Emotional center разрушен |
| **2.3 Дмитрий** | Downgraded-human walks in loop, даёт короткий dialogue | ⚠️ Placeholder_NPC_Dmitriy cube at (0,1,8), Ink knot правильный (dmitriy), но без animation/idle loop |
| **3.1 Пустыня walk** | Медитативная ходьба 3-4 минуты, песчаные dunes, ветер, Кафка идёт рядом | ⚠️ Cubes + cliffs в пустоте. Нет duration control, player может убежать за 30 сек |
| **3.2 Broken server** | Руина серверной стойки в песке, glitch sound на подходе, Кафка growls | ⚠️ Broken_Server_Monument создан в DesertDresser но без звука/glitch VFX. Кафка не реагирует |
| **3.3 Финальный курсор** | Мигающий `> _` на сером поле, 5 опций ввода, выбор → credits | ⚠️ Placeholder_Cursor cube с knot `cursor`, но визуала `> _` blinking нет, 5 опций не wire в UI |
| **Titres + sting** | После финала — чёрный экран, финальный текст, credits, `Продолжение следует.` | ✅ Единственная реализованная секция STORY.md (CreditsSequence работает) |

**Story beat coverage: 1/10 секций реализовано** (credits only).

**Risks:**
- **STORY.md написан как сценарий фильма**, но в игре никто его не ставит. Это как снять фильм по lambda.py без декораций, актёров и камеры.
- **Эмоциональное ядро Episode 0 = Kafka + Anna memory beat**. Ни одно не реализовано. Без этого Episode 0 перестаёт быть тем эпизодом который описан в STORY.md.
- **Ink story coverage**: написано 593 строки, в игре игрок реально видит ~5% (только sasha → sasha_first первые 3-4 реплики при текущей reachability).
- **Нет «tutorial overlay»** из 1.1. Игрок не знает controls. Firewatch показывает 10-секундную подсказку на вход — у нас ничего.

**Recommendation:**
- **Scene-by-scene rebuild** с STORY.md в руке как **shot list**. Каждая сцена = отдельный sprint с acceptance criteria «каждый story beat из STORY.md §N реализован».
- **Ink coverage script**: автоматический walker по всем knots проверяет что каждый achievable через legal player actions (не cheat).
- **Cinematic first moment ОБЯЗАТЕЛЕН** — без scene 1.1 Пробуждения Episode 0 теряет входной hook. Timeline/Cinemachine intro 40 seconds.
- **NPC placement буквально из STORY.md** — Саша на диване, Мила у стола с ноутбуком, Кирилл у кухни с туркой, Николай в дальнем углу за столом, Стас у двери в город. Не «кубики в ряд», а режиссура из описания.
- **Tutorial overlay** в сцене 1.1: `WASD — ходить, Мышь — смотреть, E — говорить, Shift — быстрее` на 10 секунд после `note` knot.

**Open question:** Мы пишем игру по STORY.md или STORY.md — это art bible которая игрой игнорируется? Если первое — каждый sprint gate = «story beat N реализован полностью».

---

### 🎵 Audio Director

**Assessment:** Аудио в проекте — 0 файлов. Acceptable для прототипа, но **критично плохо для narrative walker**. В Firewatch 40% emotional impact = голос Delilah + ambient forest. В Journey почти нет диалога — всё звук + music (композитор Austin Wintory получил Grammy nomination). **Narrative walker без аудио = библиотека с броской обложкой и пустыми страницами.** Мы откладываем аудио «до Дениса» — но это 50% игры. Денис должен иметь техническую основу уже сейчас: AudioMixer asset, AudioSources в сценах, event triggers в Ink, placeholder tracks для тестирования.

**Что должно быть по ART_BIBLE §8 + STORY.md audio cues:**

| Элемент | Требование | Статус |
|---|---|---|
| **Ботаника ambient music** | chill electronic, Nils Frahm/Emancipator/Bonobo, 4-6 min loop, Am/Em, Rhodes + warm pad | ❌ Нет |
| **Ботаника SFX** | кофемашина, кашель Саши, шуршание страниц (Мила), кипение турки (Кирилл), звон стакана (Николай), паяльник (Стас), серверный гул, Кафка paws on wood | ❌ Ничего из 8 |
| **City ambient drone** | Tim Hecker/Stars of the Lid, sustained D, 4-5 min loop, glitch micro-sounds | ❌ Нет |
| **City SFX** | footsteps на каменной плитке, фонтан (тихий), редкие data-glitches каждые 30 сек | ❌ Ничего |
| **Desert cinematic ambient** | Hans Zimmer Dune style, 50 BPM, low brass drone + female wordless choir + muted drum каждые 10-15 сек, 5-7 min loop | ❌ Нет |
| **Desert SFX** | постоянный ветер, footsteps на песке, eerie глитчи каждые 15-20 сек, Кафка paws on sand, growl у сервера | ❌ Ничего |
| **Credits одна длинная synth note Dm** ~40 сек | ❌ Нет |
| **UI sounds** (dialogue advance, menu hover, scene transition whoosh) | ❌ Нет |
| **AudioMixer asset** с tracks Music/SFX/VO/UI + ducking при диалоге | ❌ Скрипт `AudioMixerController.cs` есть, но **AudioMixer.mixer asset не создан** |

**Audio coverage: 0/15 элементов реализовано.**

**Risks:**
- **Кафка без звуковой persona** = безликий 2-cube. Когда у Kafka есть `bark.wav`, `whine.wav`, `paws-wood.wav`, `paws-sand.wav`, `growl.wav` — она становится персонажем **даже без финального mesh**.
- **Desert без ветра** = пустая песочница. В Journey именно звук ветра делает пустыню одиноко-эпической. Один 2-минутный loop ветра из freesound.org = 80% эффекта.
- **Footstep variety** — в Firewatch footsteps меняются на 5 поверхностях (grass/dirt/wood/rock/water). У нас ноль.
- **Отсутствие AudioMixer** = когда Денис пришлёт tracks, некуда их воткнуть. Нужна **инфраструктура сейчас**, финальные треки когда-нибудь.

**Recommendation (не ждать Дениса для инфраструктуры):**
- Создать `AudioMixer.mixer` asset с 4 groups: `Master → Music, SFX, VO, UI`. Expose volume params для Settings меню.
- Добавить `AudioSource` с `playOnAwake + loop` в каждую FPS сцену для ambient.
- Создать `FootstepController.cs` на Player который trigger'ит random footstep через raycast ground material detection.
- Скачать **freesound.org CC0 placeholder tracks** (curl-able):
  - `wind_desert_loop.ogg` (5 min loop)
  - `greenhouse_ambient.ogg` (lo-fi chill)
  - `city_silence_drone.ogg` (ambient drone)
  - `kafka_bark.wav`, `kafka_paws_wood.wav`, `kafka_paws_sand.wav`, `kafka_growl.wav`
  - `ui_click.wav`, `dialogue_advance.wav`
  - `door_open.wav`, `fountain.wav`, `coffee_machine.wav`, `paper_rustle.wav`, `coughing.wav`, `glass_clink.wav`
- Это **placeholder audio** которое Денис позже заменит на custom. Но проект **начинает звучать уже сейчас**.

**Suno AI промпты для Дениса (когда подключится):**
- Botanika: *«cinematic chill ambient, Rhodes piano, warm pad synth, no drums, 60 BPM, A minor, cosy evening oasis feel, 4 minutes loop, in the style of Emancipator»*
- City: *«minimal ambient drone, sustained D note, very slow, glitchy micro-textures, Tim Hecker style, 5 minutes loop, no melody»*
- Desert: *«cinematic desert ambient in style of Hans Zimmer Dune 2021, slow 50 BPM, wordless female choir, low brass drone, muted percussion every 12 seconds, D minor modal, 6 minutes loop»*
- Credits: *«single sustained synth note D minor, slow fade in 40 seconds, no rhythm, meditative»*

**Open question:** Готов ли Тим **сегодня** curl placeholder audio tracks из freesound.org, или ждём Дениса на всё? По моему опыту — **infrastructure сегодня, финальное audio когда Денис**. Без infrastructure игра никогда не зазвучит.

---

### 🛡️ Senior QA Lead (Ship Gate)

**Assessment:** Проект в состоянии **рабочего прототипа**, не альфа и не бета. Если бы нужно было показать publisher — это internal pitch demo максимум. Нельзя показать external playtester — они закроют через 30 секунд.

**Blockers for showing to playtester (SEV1, ship-stoppers):**
1. **Player spawned inside furniture** в Botanika → игрок видит сплошную розовую стену → не понимает что видит → закрывает
2. **NPC кубы идентичны** кроме цвета → игрок не понимает кто разговаривает → нарратив разрушен
3. **Kafka невидима** (2-cube placeholder) → эмоциональный центр Episode 0 отсутствует
4. **Pipeline magenta materials** возможны при любом изменении scene → проект фрагилен
5. **Нет interaction prompt** («нажми E чтобы говорить») → игроки не знают о механике
6. **Нет проверки dialogue flow** end-to-end → может быть что Ink knot не резольвится на реальных переходах
7. **Camera pitch clamping работает но yaw free** → игрок может себя случайно развернуть на 360° и потерять direction
8. **Player может упасть с края Boundary_Walls** если их renderer disabled (a colliders kept — но не проверено)
9. **No save system** → выход из игры = потеря прогресса (GameStateManager есть но не wired)

**Risks для ship:**
- Если загрузим .app друзьям Тима как есть → **reputation damage** серьёзнее чем у отсутствующего релиза. «Тим сделал игру» → 30 секунд → «Тим делает мусор». Это хуже чем «Тим пока не сделал игру».
- DMG size 42 MB скромно — это хорошо, но пустой контент не оправдывает даже эти 42 MB.
- Нет crash reporting / analytics → если что-то упадёт у friend-а на другом Mac, никто не узнает.

**Recommendation:**
- **Не шипить пока не пройдён internal playthrough gate**: Тим сам проходит игру от Main Menu до Credits, все 5 диалогов работают, все 3 exit triggers работают, все 5 NPC видимы и distinguishable, Kafka следует и влияет на Anna memory moment.
- **Закрыть все SEV1** прежде чем думать о визуале. Игрок должен сначала **понимать что происходит**, потом требовать красоты.
- **Dev console mode**: Alt+D показывает текущую Ink state, scene name, player pos, Kafka state. Без этого отладка вслепую.
- **Build smoke test** при каждом commit: скрипт запускает .app, делает 5 screenshots (menu, 3 scenes, credits), если все 5 не magenta/black → PASS.
- **Automated Ink coverage**: прогнать Ink story через все branch выборы (script), проверить что каждый finalText variant ends gracefully.

**Open question:** Что важнее — функциональная законченность (игра полностью проходима даже если кубы) или визуальная attractive demo (1 сцена с AAA look)? По моему опыту в publishing, функциональная законченность всегда важнее.

---

## ⚖️ PANEL CONFLICTS

### Conflict 1 — Переключаемся на URP сейчас?

| Позиция | Эксперт | Аргумент |
|---|---|---|
| **ДА** | Art Director | Без URP post-FX невозможен Firewatch look. Точка |
| **ДА** | Technical Artist | С предупреждением: конвертация 500+ materials болезненна, риск magenta + 20-30 мин на M1 8GB |
| **Нейтрально** | Game Designer | Геймдизайн не зависит от pipeline, но признаёт что Cinemachine лучше в URP |
| **СОМНЕВАЕТСЯ** | QA Lead | Переключение в середине проекта = introducing bugs. Лучше добить функционал на Built-in |

**Resolution (Priority Ladder: Safety → Correctness → Simplicity):**
Переключение на URP **сейчас** даёт Correctness (соответствие ART_BIBLE) и Simplicity (один pipeline для всего). Делать в конце = накопить technical debt.
**Вердикт: ДА, переключаемся. Technical Artist делает batch convert с backup snapshot сцен. QA Lead получает smoke test требование до и после.**

---

### Conflict 2 — Kenney vs Megascans + Mixamo

| Позиция | Эксперт | Аргумент |
|---|---|---|
| **Выкинуть furniture+city** | Art Director | Не match ART_BIBLE. Silhouette только через nature-kit |
| **Выкинуть, но осторожно** | Technical Artist | Согласен с Art Dir, добавляет Megascans LOD performance risk |
| **ПРОТИВ** | Game Designer | «Sunk cost, 2 недели работы. Функциональность важнее» |
| **ПРОТИВ** | QA Lead | «Текущие Kenney работают. Megascans + Mixamo = новые bugs» |

**Resolution:**
Sunk cost — это fallacy. Kenney furniture не match ART_BIBLE и портит визуал.
**Вердикт: КОМПРОМИСС.**
- `kenney/nature-kit` **keep** — для silhouette vegetation и дистантных rocks
- `kenney/furniture-kit` **OUT** — заменяем ProBuilder custom geometry + Megascans textures + 3D Sketchfab CC0 props
- `kenney/city-kit-commercial` **OUT** — заменяем Megascans urban scan assets + custom Unity Terrain для улицы
- Characters — **Quaternius Ultimate Modular Characters** (CC0, no login) для Phase 1, Mixamo как upgrade в Phase 2

---

### Conflict 3 — Scope (Full 15-min vs MVP 1 сцена)

| Позиция | Эксперт | Аргумент |
|---|---|---|
| **1 сцена AAA** | Art Director | «1 сцена в полном качестве >> 3 сцены half-baked» |
| **1 сцена** | Technical Artist | «Меньше материалов = меньше рисков конверсии» |
| **3 сцены** | Game Designer | «Это сериал. Без всех трёх локаций не работает установка вселенной» |
| **3 сцены по шагам** | Narrative Designer | «Все 3 нужны по сюжету, но делаем по одной. Scene-by-scene sprint pattern, не параллельно» |
| **1 сцена** | QA Lead | «1 сцена проще тестировать, меньше SEV1» |
| **3 сцены по шагам** | Audio Director | «Аудио делается per-scene, не массово. Последовательность = возможность Денису» |

**Resolution (ОБНОВЛЕНО после явного указания Тима):**
Тим сказал «**Full 15 минут**» + уточнил: «**оптимизируем сцены последовательно, сначала первую, потом вторую, потом третью, не одновременно. Отдельный бэклог на каждую локацию + проверка бесшовного перехода + UX плавность**».

**Вердикт: 3 PHASES последовательно, каждая со своим sprint-бэклогом + transition gates.**

| Phase | Scene | Acceptance gate перед Phase N+1 |
|---|---|---|
| **Phase 1** | Botanika AAA (pipeline proof) | Все 10 story beats из STORY.md §3 реализованы, визуал = Firewatch/Tchia уровень, 5 NPC с distinct silhouettes, Kafka follows + reacts, audio infrastructure создана + placeholder tracks loaded |
| **Transition 1** | Botanika → City bridge | Bесшовный fade + Kafka carries over + saved Ink state. UX smooth: нет очевидных loading artefacts, gatekeeper ink var honored |
| **Phase 2** | City (наследует pipeline Phase 1) | Все story beats STORY.md §4 (Anna memory, Dmitriy loop, Keeper gate), + визуал одного уровня качества с Botanika |
| **Transition 2** | City → Desert bridge | Те же критерии, Kafka карта сохраняется |
| **Phase 3** | Desert + Cursor finale | Story beats §5, визуал, финальный input flow 5 options → credits text variant |
| **Transition 3** | Desert → Credits | Credits получают правильный variant из GameState.cursorInput |
| **Phase 4** | Full smoke pass | End-to-end playthrough MainMenu→Credits без ручного touching |

**Каждая фаза — отдельный бэклог** с тасками. Разделение по фазам мешает scope creep и позволяет вернуть уроки Phase 1 в Phase 2.

---

## CONVERGED FINDINGS (для Спринтов 2-3)

Эта секция — **не финальный план**. Это материал который Sprint 2 (разнос через gamedev skills) и Sprint 3 (roadmap) будут использовать как основу.

### Найденные P0 блокеры (все согласны)
1. URP активен только как package, не как pipeline → magenta everything на любой попытке post-FX
2. **Script-to-Game gap = 1/10 story beats** (только credits реализован) — игра не отражает сценарий
3. **4 из 5 Ботаника NPC не существуют** в сценах (Мила/Кирилл/Николай/Стас — только в Ink)
4. Kafka = 2 cubes = emotional core Episode 0 missing
5. Anna memory beat (эмоциональный пик Episode 0) не реализован
6. **Cinematic wake-up scene 1.1 отсутствует** — нет tutorial overlay, нет `note` листка, нет Kafka reveal
7. Player spawn UX сломан (upon scene load видно непонятно что)
8. No Cinemachine / camera direction → walker без режиссуры
9. **0 audio files, 0 AudioMixer asset, 0 AudioSource в сценах**

### Найденные P1 блокеры
1. Kenney FBX scale chaos (UnitScaleFactor 100)
2. No Volume Profiles per scene (post-FX настройки из ART_BIBLE §5 не применены)
3. No interaction prompts ("нажми E")
4. No idle animations на NPC
5. Dialogue pacing: нет force pauses, нет speaker name display, нет skip protection
6. Save/Load не wired (GameStateManager существует, не used)
7. No footstep variety (один или ноль sounds вместо 5 surfaces)
8. No tutorial overlay в scene 1.1
9. Ink coverage: игрок реально видит ~5% из 593-строчного story

### Найденные P2 проблемы
1. Build Botanika first (dev), должен быть MainMenu first (prod)
2. No dev console for debugging
3. No automated smoke test pipeline
4. Ink coverage не автоматизирована (нет walker по всем knots)
5. No Ink gate verification end-to-end (met_nikolai → door_to_city_open path)
6. Camera pitch clamping работает но yaw free — игрок может себя развернуть
7. Нет save/restore state при переходе между сценами (DialogueManager singleton carry-over OK, но Kafka state/Ink story state не wired в GameStateManager)
8. No Settings menu (volume sliders wire в AudioMixer params — отсутствует)

### Согласованные фундаментальные решения
1. **URP → активируем** (Conflict 1 resolution)
2. **Kenney selective keep, furniture/city out, nature in, ProBuilder+Megascans+Quaternius replace** (Conflict 2 resolution)
3. **Scope 2-phase** (Conflict 3 resolution)

### Несогласованные open items (ждут Тима)
1. **OAuth для Mixamo**: Тим готов на 1-минутный login один раз? (Иначе Quaternius only для всех NPC)
2. **Time budget 3 phases**: ~30-40 часов автономной работы scene-by-scene. OK?
3. **Throwing away Kenney furniture/city**: принципиально окей с переписыванием?
4. **Placeholder audio сегодня**: я curl CC0 tracks из freesound.org или ждём Дениса на всё?
5. **Если Phase 1 Botanika не достигает Firewatch look** → что делаем? Fallback plan?
6. **Cinematic intro 40 seconds в сцене 1.1** — готов инвестировать в это или скипаем?

---

## СЛЕДУЮЩИЕ ШАГИ

### Аналитический Спринт 2 — Разнос через gamedev skills

**Задача:** Использовать **все доступные в сессии skills** которые касаются геймдева и применить их как отдельные lenses критики к current state. Каждый skill = отдельная оптика. Результат — разгромная критика **с STORY.md в руке** как shot list.

| Skill | Оптика | Что критикуем |
|---|---|---|
| `3d-games` | Rendering pipeline, shaders, cameras, physics | Built-in vs URP, shader strategy, LOD, culling, camera work |
| `game-art` | Visual style, asset pipeline, animation workflow | Kenney selection, texture pipeline, animation state machines |
| `game-audio` | Sound design, ambient, music | AudioMixer архитектура, 3D spatial audio, music progression, SFX catalog |
| `game-design` | GDD, core loop, player psychology, progression | 30-second test, emotional beats, player agency, story coverage |
| `game-development` | Orchestrator — common anti-patterns, performance budget 60 FPS, AI selection | Общие паттерны, NPC AI (FSM vs scripted), performance budget для M1 8GB |
| `ui-ux-pro-max` | UI/UX intelligence — dialogue UI, menu, HUD, prompts | Dialogue panel layout, menu hierarchy, HUD information density, interaction prompts |
| `frontend-design` | Design quality, composition, visual hierarchy | Menu composition, credits layout, typographic hierarchy |
| `scroll-experience` | Cinematic pacing (applies to camera work, not just web) | Cinematic transitions между сценами, scroll-style reveals, pacing control |
| `theme-factory` | Art direction consistency | Visual consistency между тремя локациями, палитра coherence |
| `3d-web-experience` | Three.js / 3D interaction patterns | Interaction patterns, spatial UI, camera behavior под narrative walker |

**Output Sprint 2:** `03_SPRINT2_DEEP_CRITIQUE.md` — жёсткий разнос по каждой оптике, **с конкретными shot-by-shot сравнениями** из STORY.md, с priority findings P0/P1/P2 **per scene**.

### Аналитический Спринт 3 — Roadmap + Backlog scene-by-scene (Plan Mode)

**Задача:** На основе Спринт 1 + Спринт 2 написать детальный recovery plan **с отдельным бэклогом per scene** по требованию Тима:
- Переход в **Plan Mode** для утверждения
- Structured roadmap с 4 phases: Botanika → Transition 1 → City → Transition 2 → Desert → Transition 3 → Full Smoke
- **Отдельный бэклог per scene + per transition** разбитый на conventional 8-step sprints (PLAN → IMPLEMENT → TEST → SEC → DEPLOY → VERIFY → NOTIFY)
- Agent allocation (unity-game-developer, 3d-artist, code-reviewer, general-purpose) per task
- **Story beat coverage** для каждой scene phase — каждый task связан с конкретным beat из STORY.md
- **Audio task track** параллельно (AudioMixer setup → placeholder tracks → Denis final tracks)
- **Transition verification tasks** между фазами: bесшовность, UX smoothness, state carry-over
- Time estimates в токенах, не часах (правило Тима)
- Exit из Plan Mode = approved roadmap готовый к работе

**Output Sprint 3:**
- `04_RECOVERY_ROADMAP.md` — высокоуровневый план phases + gates
- `05_BACKLOG_BOTANIKA.md` — бэклог для Phase 1
- `06_BACKLOG_CITY.md` — бэклог для Phase 2
- `07_BACKLOG_DESERT.md` — бэклог для Phase 3
- `08_BACKLOG_TRANSITIONS.md` — бэклог для bridge sprints между сценами
- `09_BACKLOG_AUDIO.md` — audio track параллельно
- `10_BACKLOG_TEST_INFRA.md` — smoke tests, Ink coverage, dev console

### Реализация (после Sprint 3 approval)

**Не в Sprint 1.** Выполнение начинается только когда Тим approved roadmap.

---

## STATUS FOOTER

- **Sprint 1 (этот документ):** ✅ DONE — материал собран
- **Sprint 2 (разнос через skills):** ⏳ Next — ждёт старта
- **Sprint 3 (roadmap в Plan Mode):** ⏳ После Sprint 2
- **Implementation phase:** ⏳ После Sprint 3 approval

**Место встречи:** этот файл (`docs/expert-panel/02_EXPERT_PANEL_SPRINT1.md`) и следующие 03/04/05 в той же папке.
