# Botanika — 4 Hero Shots

> **Документ:** BOTANIKA_HERO_SHOTS.md
> **Версия:** 1.0
> **Дата:** 2026-05-01
> **Статус:** Sprint 4 visual upgrade — Day 1
> **Связано:** ART_BIBLE.md (палитра, освещение), CHARACTERS.md (расположение NPC)

## Геометрия комнаты

Botanika — interior 12m × 8m × 3.2m (W × D × H). Origin (0, 0, 0) — центр пола.
- **Окно L (большое, восточное):** wall x=-6, центр окна (-6, 1.6, -1), height 1.8m
- **Окно R (меньшее, южное):** wall z=+4, центр (+2, 1.6, +4), height 1.4m
- **Дверь в коридор города:** wall z=-4, проём (+5, 0, -4), high 2.2m, wide 1.0m
- **Кресло пробуждения:** (-4.5, 0, +2.5), seat height 0.45m, faces (+x)
- **Кофе-угол Кирилла (плита+турка):** (+4.5, 0, +3)
- **Стол Милы:** (-1, 0, +2)
- **Диван Саши:** (0, 0, 0), centerline
- **Server rack Николая (corner):** (+5, 0, -3.2), стол Николая (+4, 0, -2.5)
- **Стас (ходит):** zone у двери, x=+3..+5, z=-3..-4

Ось Y up, units = метры. Все позиции достижимы через `Camera.main.transform.position = new Vector3(...)` в batchmode.

---

## Shot 1: Wake-up POV (первый кадр игры)

- **Camera position:** Vec3(-4.5, 0.95, 2.5)
- **Look-at target:** Vec3(-2.0, 1.05, 1.8)  // через комнату на espresso_old и edison_lamp на столе Милы
- **FOV:** 58 degrees
- **Key prop in focus:** espresso_old (старая медная эспрессо-машина на столе у плиты, видна по диагонали кадра справа), edison_lamp на столе Милы как foreground bokeh
- **Light setup:** Directional 3200K, 25° elevation, intensity 1.2, заходит через окно L (-6, 1.6, -1) — длинный косой луч режет комнату по диагонали и попадает прямо в кадр. Point light тёплый 2800K у edison_lamp (intensity 0.8, range 2.5m). Volumetric fog density 0.015, цвет #F5D8A3 — пылинки-мотты в луче. DoF: focus 3m, aperture 5.6 — edison_lamp soft в foreground, espresso_old и Саша на диване sharp в midground.
- **Composition:** камера на высоте сидящего (0.95m), угол слегка вверх (+5°). Луч солнца через окно L идёт сверху-слева вниз-вправо, режет кадр по правилу третей. Edison-лампа в нижнем-левом thirds — soft warm point. Espresso-машина на верхнем-правом thirds. Кафка в нижней четверти кадра в фокусе — лежит свёрнутая, голова поднята, смотрит на игрока.
- **Emotional beat (5 sec test):** "Я проснулся в чьём-то очень тёплом убежище, где время остановилось — и собака уже знает меня."

---

## Shot 2: Doorframe to corridor (Кафка как focal)

- **Camera position:** Vec3(0.5, 1.65, -0.5)
- **Look-at target:** Vec3(4.5, 1.0, -3.8)  // дверной проём в правой трети, Кафка mid-frame у проёма
- **FOV:** 62 degrees
- **Key prop in focus:** Кафка (чёрно-белый corgi-cardigan) стоит в средней части кадра на (3.0, 0, -2.0), смотрит в дверной проём — её силуэт читаем против холодного света коридора. Дверной фрейм — как natural vignette.
- **Light setup:** тёплый key 3200K через окно L бьёт сзади-слева камеры (rim на шерсти Кафки, на полу длинная тень). Холодный 5500K spill из коридора через дверь (intensity 0.6, range 3m) — single cool note справа кадра, контраст с warm interior. Volumetric fog тёплый density 0.02, цвет #F5D8A3 — dust motes в косом луче поперёк кадра между камерой и Кафкой.
- **Composition:** wide-ish (62° FOV). Левая треть — interior warm (стол Милы, edison_lamp blur). Средняя треть — Кафка mid-frame, в косом луче пыли. Правая треть — дверной проём как leading line к холодному коридору, Стас может быть размыт в фоне у двери. Eye-level стоящего человека (1.65m). Foreground floorboards дают depth.
- **Emotional beat (5 sec test):** "Собака смотрит туда, куда мы оба знаем что пойдём — но ещё пять секунд можно постоять в тёплом."

---

## Shot 3: Server rack corner (cool spot in warm room)

- **Camera position:** Vec3(2.5, 1.65, -1.8)
- **Look-at target:** Vec3(5.0, 1.3, -3.2)  // server_rack_retro угол, Николай за столом mid-ground
- **FOV:** 50 degrees
- **Key prop in focus:** server_rack_retro (retro-futuristic стойка с мигающими лампочками) в углу (+5, 0, -3.2), height 1.8m. Николай за столом сидит в profile slightly turned, бутылка виски и стакан на столе — second focal.
- **Light setup:** ОДИН cool 5500K spot light (intensity 1.6, range 4m, angle 45°) сверху над server_rack — единственный холодный источник в тёплой комнате. Тёплый 3200K ambient #F5D8A3 intensity 0.4 заполняет остальное. Warm point 2800K на столе Николая (intensity 0.5, range 2m) — на лице Николая warm fill, на серверной стойке cool key. Это даёт двухтоновое освещение лица: левая щека warm, правая cool. Bloom intensity 0.7 на server LEDs.
- **Composition:** правило третей — server_rack в правой трети вертикально, Николай в левой трети. Horizon line в верхней трети (низкий потолок видим). Camera slightly tilted (-3°) — рассказчик присматривается. 50° FOV — тесный, intimate, "что-то не так" feeling. Мигающие LEDs server stack создают точечные cool highlights — единственные cool tones в палитре сцены.
- **Emotional beat (5 sec test):** "В этом тёплом месте есть один холодный угол — и человек, который сидит к нему ближе всех, давно не пил воду."

---

## Shot 4: Window seat at sunset (silhouette + hand)

- **Camera position:** Vec3(-5.2, 1.55, -1.0)
- **Look-at target:** Vec3(-6.0, 1.6, -1.0)  // прямо в стекло окна L
- **FOV:** 55 degrees
- **Key prop in focus:** оконная рама (деревянная, faded #8B6F4E, переплёт крест) + рука игрока (right hand, открытая ладонь) прижата к стеклу в правой трети кадра на высоте Vec3(-5.7, 1.5, -0.7). За стеклом — закатное небо #E8A75C → #F2C084 gradient, силуэт далёкой растительности.
- **Light setup:** Directional 2800K (более warm чем дневной — закат), elevation 8° (ниже чем wake-up shot — солнце ушло), intensity 1.8 (HDR за стеклом), угол попадает прямо в камеру — backlit. Interior почти не подсвечен, только rim warm 3200K сзади камеры (intensity 0.3) — намёк на интерьер за спиной. Volumetric fog density 0.012, цвет #E8A75C. Bloom intensity 1.0 — солнце за окном пылает, переэкспонировано. Vignette 0.3 — концентрирует на руке. Tonemapping ACES.
- **Composition:** оконный переплёт делит кадр на 4 квадрата (золотое сечение в архитектуре окна). Рука в правом-нижнем квадрате, силуэтная, тёмная против warm sky. Левый-верхний и правый-верхний квадраты — закатное небо. Левый-нижний — нижний край подоконника + edison_lamp blurry в interior side. Eye-level почти стоя (1.55m — игрок чуть наклонился к окну). 55° FOV — не слишком wide, чтобы рука читалась.
- **Emotional beat (5 sec test):** "День заканчивается, я касаюсь границы между этим тёплым местом и тем, что снаружи — и пока что граница ещё стеклянная."

---

**Word count:** ~590 words (excluding code blocks and headers).
