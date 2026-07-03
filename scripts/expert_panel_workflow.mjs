export const meta = {
  name: 'botanika-expert-panel',
  description: 'Панель AAA-экспертов: как дотянуть сцену Botanika до референса/AAA + играбельный 3rd-person корги. Улучшать, НЕ ломать.',
  phases: [
    { title: 'Panel', detail: '6 экспертов независимо разбирают gap и дают конкретный план' },
    { title: 'Roadmap', detail: 'синтез в единый приоритезированный roadmap P0/P1/P2' },
  ],
}

const REF = '/Users/timofeyzinin/afterhumans/docs/concepts/refs_channel/ref_botanika.jpg'
const SHOT = '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots/46_cycleW_crop.png'

const CONTEXT = `
ПРОЕКТ: AAA-игра Afterhumans, сцена «Ботаника» — заброшенная викторианская оранжерея.
ДВИЖОК: Unity 6 (6000.0.72f1), URP. Цель сборки — WebGL (играется в браузере на GPU).
ПЕРСОНАЖ: 3rd-person корги «Кафка» (игрок водит собаку по сцене).

ЧТО УЖЕ СДЕЛАНО (НЕЛЬЗЯ ЛОМАТЬ — только улучшать/дорабатывать/добавлять):
- Процедурная архитектура: центральная бетонная колонна, ферменный остеклённый свод,
  стены, пол, остекление с переплётом, граффити WATCH OUT (реальная декаль).
- Реальные 3D-ассеты через пайплайн Gemini→Hunyuan3D: люди (на диване, читающая, мастер),
  книги, глобус, ящики, лабораторное стекло, лозы, папоротники, диван, столик, серверная, CRT.
- Персидский ковёр (текстура) с бахромой. Фото-задник «золотой лес» за стеклом (emissive).
- Свет: directional «закатный ключ», ambient, point-lights (CRT green, server, лампы), SSAO.
- Пост-обработка URP: ACES/Neutral tonemap, bloom, color grade, vignette, film grain, DoF.
- Корги: процедурный CorgiStateAnimator (дышит/сидит/нюхает/озирается) — РАБОТАЕТ в билде.
- Контроллеры в проекте (готовы, но не все подключены): SimpleFirstPersonController (FPS),
  KafkaDirectController (WASD двигает корги), KafkaFollowCamera (spring-arm 3rd-person),
  KafkaFollowSimple, NavMeshSurface-setup для блуждания. AI Navigation 2.0.12 установлен.

ТЕКУЩИЕ ПРОБЛЕМЫ (со слов заказчика и приёмки судей):
1. «Совсем не похоже на референс» — нет золотого часа/объёмной дымки/god-rays, плоский свет,
   небо за стеклом холодное, лес не читается сквозь стекло, хайлайты клиппят в белый,
   правый CRT белый вместо зелёного, точечные светильники на фермах как «гирлянда»-плейсхолдер,
   материалы читаются low-res (бетон-цилиндр, пол-каша, ковёр плоский), зелени мало.
2. «В игре нельзя ходить» — игрок видит дышащую собаку, но движение не работает.
3. Композиция кадра не геройская: корги мелкий в углу, нет читаемого DoF, есть letterbox.

ЖЁСТКИЕ ОГРАНИЧЕНИЯ:
- НЕ ломать уже готовое. Только инкрементальные улучшения/добавления.
- Headless-сборка на сервере БЕЗ GPU (llvmpipe) — правда света видна ТОЛЬКО в WebGL на GPU.
- URP (не HDRP) в текущем виде. Переход на HDRP — отдельное крупное решение, оценить ЦЕНУ.
- Бюджет WebGL (вес/перф) — реалистичный для браузера.

Открой и изучи СВОИМИ ГЛАЗАМИ через Read:
РЕФЕРЕНС (эталон, цель): ${REF}
ТЕКУЩИЙ GPU-РЕНДЕР сцены: ${SHOT}`

const ITEM_SCHEMA = {
  type: 'object',
  properties: {
    title: { type: 'string' },
    why: { type: 'string', description: 'почему это двигает к AAA/референсу' },
    how: { type: 'string', description: 'КОНКРЕТНО как сделать в Unity URP (методы/настройки/ассеты)' },
    bucket: { type: 'string', enum: ['URP_NOW', 'ASSET_WORK', 'HDRP_OR_ARCH', 'GAMEPLAY'] },
    impact: { type: 'string', enum: ['HIGH', 'MEDIUM', 'LOW'] },
    effort: { type: 'string', enum: ['S', 'M', 'L'] },
    breaks_existing: { type: 'boolean', description: 'есть ли риск сломать готовое' },
  },
  required: ['title', 'why', 'how', 'bucket', 'impact', 'effort', 'breaks_existing'],
}

const PANEL_SCHEMA = {
  type: 'object',
  properties: {
    role: { type: 'string' },
    verdict_oneliner: { type: 'string', description: 'честная одна строка: главный разрыв с рефом по твоей теме' },
    items: { type: 'array', items: ITEM_SCHEMA },
  },
  required: ['role', 'verdict_oneliner', 'items'],
}

const EXPERTS = [
  { key: 'lighting', role: 'AAA Lighting / Volumetrics TD',
    brief: `Ты AAA lighting director + волюметрик-TD. Тема: золотой час, объёмная дымка, god-rays, тонмаппинг, экспозиция, эмиссия. КАК получить киношный golden-hour volumetric look в URP БЕЗ HDRP (light shafts/raymarch-quad, height-fog, fog-density, baked GI/lightmaps, reflection probes, правильный roll-off хайлайтов, мотивированный bloom). Дай конкретные шаги и где это URP умеет, а где нужен кастомный render-feature/HDRP.` },
  { key: 'environment', role: 'Environment Art Lead',
    brief: `Ты environment art lead AAA. Тема: что делает кадр «обжитой заброшенной оранжереей» vs greybox — плотность вегетации (плющ оплетает фермы/колонну, мох, прорастание), wear/grime, set-dressing, силуэт, story-в-материалах. Что добавить/уплотнить тем же Gemini→Hunyuan3D пайплайном, что заменить. Конкретные ассеты и расстановка.` },
  { key: 'techart', role: 'Technical Artist / Shaders',
    brief: `Ты technical artist (шейдеры/материалы URP). Тема: стекло (translucent/refraction/Fresnel, грязь/потёки, чтобы читался лес снаружи), декали, trim-sheets, тайлинг пола (убрать повтор, detail-normals, AO у контактов), бетон колонны (roughness/normal вариативность), фикс CRT (зелёная эмиссия, не белый клиппинг), фикс «гирлянды» на фермах. Конкретные shader graph / material настройки.` },
  { key: 'artdirector', role: 'AAA Art Director / Reference Fidelity',
    brief: `Ты AAA art director, ты выбрал этот реф. Тема: композиция, камера, цветосценарий, что именно заставляет кадр ЧИТАТЬСЯ как реф (колонна-ось, диагонали ферм, корги-герой на переднем плане крупно, DoF, золотой пул света). Приоритизируй: что даст максимум «похожести» за минимум усилий. Будь честен где это арт-продакшн, а не пара тумблеров.` },
  { key: 'gameplay', role: 'Game Feel / Experience Designer',
    brief: `Ты geme-feel/experience designer. Тема: ИГРАБЕЛЬНОСТЬ 3rd-person корги. Сейчас «ходить нельзя». Как сделать ТЕСТИРУЕМЫЙ игровой экспириенс: WASD водит корги, spring-arm follow-камера, скорость/повороты/инерция, как уживить движение с CorgiStateAnimator (дыхание/idle не должно драться с ходьбой), что игрок должен ЧУВСТВОВАТЬ и какие 3-5 micro-интеракций в сцене дают «игру», а не walking-sim. Учти WebGL (canvas focus/pointer-lock).` },
  { key: 'webgl', role: 'Unity WebGL Build Engineer',
    brief: `Ты Unity WebGL build/perf инженер. Тема: почему «нельзя ходить» в WebGL (canvas keyboard-focus, pointer-lock user-gesture, activeInputHandler=2 Both, CharacterController grounding/спавн), как это надёжно чинится. Плюс перф-бюджет WebGL для всего, что предлагают другие эксперты (волюметрика/доп.геометрия/тени) — что потянет браузер, что нет, где LOD/baking. Конкретные настройки Player/Quality/URP-asset.` },
]

phase('Panel')
const panels = await parallel(EXPERTS.map(e => () =>
  agent(
`${e.brief}

${CONTEXT}

Дай ЧЕСТНЫЙ разбор ТОЛЬКО по своей теме. Не общие слова — конкретные шаги выполнимые в Unity 6 URP.
Каждый пункт промаркируй bucket (URP_NOW=чинится в движке сейчас / ASSET_WORK=нужны ассеты/текстуры /
HDRP_OR_ARCH=нужен HDRP или крупное арх-решение / GAMEPLAY=играбельность) + impact + effort(S/M/L) +
breaks_existing. Помни ЖЁСТКОЕ правило: НЕ ломать готовое, только улучшать/добавлять.
Верни СТРОГО структурный объект (role, verdict_oneliner, items[]). Не пиши пользователю.`,
    { label: `expert:${e.key}`, phase: 'Panel', schema: PANEL_SCHEMA, agentType: 'general-purpose' }
  ).then(v => v).catch(() => ({ role: e.role, verdict_oneliner: 'agent error', items: [] }))
))

const valid = panels.filter(Boolean)
const allItems = valid.flatMap(p => (p.items || []).map(it => ({ ...it, role: p.role })))
log(`Эксперты: ${valid.map(p => p.role.split('/')[0].trim()).join(' · ')} | пунктов всего: ${allItems.length}`)

phase('Roadmap')
const ROADMAP_SCHEMA = {
  type: 'object',
  properties: {
    headline: { type: 'string', description: 'честный вердикт: насколько далеко до AAA/референса и что главное' },
    p0_now: { type: 'array', items: { type: 'object', properties: {
      title: { type: 'string' }, how: { type: 'string' }, why_first: { type: 'string' } },
      required: ['title', 'how', 'why_first'] }, description: 'URP_NOW + GAMEPLAY: чинится сейчас, max impact, не ломает' },
    p1_assets: { type: 'array', items: { type: 'object', properties: {
      title: { type: 'string' }, how: { type: 'string' } }, required: ['title', 'how'] },
      description: 'ASSET_WORK: ещё ассеты/текстуры/wear тем же пайплайном' },
    p2_strategic: { type: 'array', items: { type: 'object', properties: {
      title: { type: 'string' }, tradeoff: { type: 'string' } }, required: ['title', 'tradeoff'] },
      description: 'HDRP_OR_ARCH: крупные решения с ценой/трейдоффом — для решения Тима' },
    gameplay_plan: { type: 'string', description: 'как сделать играбельный тестируемый 3rd-person корги (конкретно)' },
    honest_gap: { type: 'string', description: 'честно: что в URP без авторского арт-продакшна недостижимо и почему' },
  },
  required: ['headline', 'p0_now', 'p1_assets', 'p2_strategic', 'gameplay_plan', 'honest_gap'],
}

const roadmap = await agent(
`Ты ведущий продюсер/арт-директор AAA. Перед тобой разборы 6 экспертов по сцене Botanika.
Сведи их в ОДИН приоритезированный roadmap. Дедуплицируй, разреши конфликты, отсортируй по impact/effort.
ЖЁСТКОЕ правило проекта: НЕ ломать готовое — только улучшать/дорабатывать/добавлять. Будь предельно честен:
где пара настроек даст результат, а где нужен месяц арт-продакшна или HDRP.

Разборы экспертов (JSON):
${JSON.stringify(valid, null, 1).slice(0, 60000)}

Верни СТРОГО структурный объект (headline, p0_now[], p1_assets[], p2_strategic[], gameplay_plan, honest_gap).`,
  { label: 'synth:roadmap', phase: 'Roadmap', schema: ROADMAP_SCHEMA, agentType: 'general-purpose' }
)

return { roadmap, panels: valid }
