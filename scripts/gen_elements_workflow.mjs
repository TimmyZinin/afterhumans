export const meta = {
  name: 'botanika-gen-elements',
  description: 'Генерация чистых product-картинок элементов сцены Botanika (Gemini) с визуальной верификацией',
  phases: [
    { title: 'Generate', detail: 'по агенту на элемент: Gemini clean render + visual verify + retry' },
  ],
}

const AP = '/Users/timofeyzinin/afterhumans/art_pipeline'
const ELEMENTS = [
  { key: 'coffee_table', desc: 'a single low rectangular industrial coffee table: thin dark wrought-iron frame legs and a weathered reclaimed-wood plank top. Empty top. Vintage, slightly rusted.' },
  { key: 'fern',         desc: 'a single large lush Boston fern houseplant in a weathered terracotta pot, many arching green fronds, full bushy plant.' },
  { key: 'potted_plant', desc: 'a single potted leafy houseplant (snake plant / sansevieria with tall upright green leaves) in a terracotta pot.' },
  { key: 'crt_monitor',  desc: 'a single vintage late-1980s CRT computer monitor, beige-grey plastic casing, chunky, dark blank screen with a faint green phosphor glow.' },
  { key: 'server_rack',  desc: 'a single tall black 42U server rack cabinet packed with rack-mount servers, dozens of tiny green and red LED status lights on the front, dark metal, front 3/4 view.' },
  { key: 'bookshelf',    desc: 'a single tall weathered wooden bookshelf fully packed with rows of old worn hardcover books of mixed colors, front view.' },
  { key: 'hanging_vine', desc: 'a single hanging trailing pothos / english-ivy plant with long draping green tendrils and leaves cascading downward, as if hanging from above.' },
]

const SCHEMA = {
  type: 'object',
  properties: {
    key: { type: 'string' },
    path: { type: 'string' },
    ok: { type: 'boolean' },
    note: { type: 'string' },
  },
  required: ['key', 'path', 'ok', 'note'],
}

phase('Generate')
const results = await parallel(ELEMENTS.map(el => () =>
  agent(
`Сгенерируй ЧИСТУЮ product-картинку 3D-ассета для последующего image-to-3D. Элемент: "${el.key}".

ШАГИ (выполни через Bash):
1. Ключ: OR=$(grep -oiE "OPENROUTER_API_KEY=.*" ~/.secrets/zinin-chat-openrouter.env | head -1 | cut -d= -f2- | tr -d '"'"'"'"'"'"'"'"' ')
2. Промпт строй так: "Product photo of ${el.desc} Isolated on a PURE WHITE seamless studio background. NO people, NO other objects, NO plants around it, NO floor texture, NO scenery. Single object centered, full object visible in frame. Soft even studio lighting, photorealistic, 3/4 front view, square image."
3. Сгенерируй: python3 ${AP}/genimg.py "$OR" "<промпт>" "${AP}/gen/${el.key}_clean.png"
   (скрипт печатает "OK <path> <bytes>" при успехе, или ошибку)
4. ОБЯЗАТЕЛЬНО открой результат через Read (${AP}/gen/${el.key}_clean.png) и ПРОВЕРЬ ГЛАЗАМИ:
   - ровно ОДИН объект нужного типа, по центру, целиком в кадре
   - фон ЧИСТО БЕЛЫЙ (без травы/деревьев/людей/комнаты/пола)
   - нет дублей, нет текста, нет рамок
5. Если картинка НЕ чистая (есть фон/сцена/люди/несколько объектов/обрезан) — перегенерируй с усиленным промптом (добавь "absolutely plain white background, isolated cutout, studio packshot, nothing else in frame"), до 3 попыток суммарно.
6. Верни {key:"${el.key}", path:"${AP}/gen/${el.key}_clean.png", ok:true/false, note:"что на картинке / сколько попыток"}.

Не пиши ничего пользователю — твой ответ это ТОЛЬКО структурированный объект.`,
    { label: `gen:${el.key}`, phase: 'Generate', schema: SCHEMA }
  ).then(v => v).catch(() => ({ key: el.key, path: `${AP}/gen/${el.key}_clean.png`, ok: false, note: 'agent error' }))
))

const ok = results.filter(Boolean).filter(r => r.ok)
log(`generated OK: ${ok.map(r => r.key).join(', ')} | failed: ${results.filter(Boolean).filter(r => !r.ok).map(r => r.key).join(', ') || 'none'}`)
return { results }
