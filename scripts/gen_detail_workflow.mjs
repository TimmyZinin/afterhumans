export const meta = {
  name: 'botanika-gen-detail',
  description: 'Генерация чистых product-картинок ДЕТАЛЬНЫХ ассетов сцены Botanika (Gemini) + визуальная верификация/ретрай',
  phases: [
    { title: 'Generate', detail: 'по агенту на элемент: Gemini clean render + visual verify + retry' },
  ],
}

const AP = '/Users/timofeyzinin/afterhumans/art_pipeline'
const ELEMENTS = [
  { key: 'book_pile',   desc: 'a tall haphazard stack of about ten old worn hardcover books piled on top of each other, mixed faded muted colors (deep red, navy blue, olive green, tan, brown), some leather-bound with gold spine lettering, slightly dusty and vintage, a couple lying open.' },
  { key: 'old_globe',   desc: 'a vintage antique world globe on a turned wooden tripod floor stand, aged sepia-toned map, a brass meridian ring around it, slightly weathered.' },
  { key: 'wood_crate',  desc: 'a stack of three weathered wooden storage crates stacked on top of each other, vintage worn grey-brown planks, visible nails, empty, rustic.' },
  { key: 'lab_glass',   desc: 'a cluster of vintage laboratory glassware on a small dark metal stand: rounded flasks, a beaker, a coiled glass distillation condenser, slightly dusty, amber liquid in one flask.' },
  { key: 'npc_reading', desc: 'a young woman sitting cross-legged on the floor reading an open book held in her lap, wearing a cozy oversized knit sweater and jeans, hair in a loose bun, relaxed calm posture, full body visible head to feet.' },
]

const SCHEMA = {
  type: 'object',
  properties: {
    key: { type: 'string' }, path: { type: 'string' },
    ok: { type: 'boolean' }, note: { type: 'string' },
  },
  required: ['key', 'path', 'ok', 'note'],
}

phase('Generate')
const results = await parallel(ELEMENTS.map(el => () =>
  agent(
`Сгенерируй ЧИСТУЮ product-картинку 3D-ассета для последующего image-to-3D. Элемент: "${el.key}".

ШАГИ (через Bash):
1. Ключ: OR=$(grep -oiE "OPENROUTER_API_KEY=.*" ~/.secrets/zinin-chat-openrouter.env | head -1 | cut -d= -f2- | tr -d '"'"'"'"'"'"'"'"' ')
2. Промпт: "Product photo of ${el.desc} Isolated on a PURE WHITE seamless studio background. NO other objects, NO scenery, NO floor texture, NO people around it. Single subject centered, full subject visible in frame. Soft even studio lighting, photorealistic, 3/4 front view, square image."
3. Сгенерируй: python3 ${AP}/genimg.py "$OR" "<промпт>" "${AP}/gen/${el.key}.png" (печатает "OK <path> <bytes>")
4. ОБЯЗАТЕЛЬНО открой результат через Read (${AP}/gen/${el.key}.png) и ПРОВЕРЬ ГЛАЗАМИ:
   - ровно ОДИН субъект нужного типа, по центру, целиком в кадре
   - фон ЧИСТО БЕЛЫЙ (без сцены/пола/людей/комнаты)
   - нет дублей, текста-водяных знаков, рамок
5. Если картинка НЕ чистая — перегенерируй с усиленным промптом (добавь "absolutely plain seamless white background, isolated cutout, studio packshot, nothing else in frame"), до 3 попыток.
6. Верни {key:"${el.key}", path:"${AP}/gen/${el.key}.png", ok:true/false, note:"что на картинке / сколько попыток"}.

Не пиши пользователю — ответ ТОЛЬКО структурированный объект.`,
    { label: `gen:${el.key}`, phase: 'Generate', schema: SCHEMA }
  ).then(v => v).catch(() => ({ key: el.key, path: `${AP}/gen/${el.key}.png`, ok: false, note: 'agent error' }))
))

const ok = results.filter(Boolean).filter(r => r.ok)
log(`OK: ${ok.map(r => r.key).join(', ')} | failed: ${results.filter(Boolean).filter(r => !r.ok).map(r => r.key).join(', ') || 'none'}`)
return { results }
