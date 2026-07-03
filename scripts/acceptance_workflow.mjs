export const meta = {
  name: 'botanika-acceptance',
  description: 'Приёмка сцены Botanika vs референс — 5 агентов-судей @10/10 + синтез',
  phases: [
    { title: 'Review', detail: '5 независимых судей оценивают кадры vs реф' },
    { title: 'Synth', detail: 'консолидация блокеров, вердикт PASS только при всех 10/10' },
  ],
}

const REF = '/Users/timofeyzinin/afterhumans/docs/concepts/refs_channel/ref_botanika.jpg'
const SHOTS = [
  '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots/botanika_render_crop.png',
]

const SCORE_SCHEMA = {
  type: 'object',
  properties: {
    score: { type: 'integer', minimum: 1, maximum: 10 },
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    blockers: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          severity: { type: 'string', enum: ['CRITICAL', 'HIGH', 'MEDIUM', 'LOW'] },
          issue: { type: 'string' },
          fix: { type: 'string' },
        },
        required: ['severity', 'issue', 'fix'],
      },
    },
    good: { type: 'array', items: { type: 'string' } },
  },
  required: ['score', 'verdict', 'blockers', 'good'],
}

const COMMON = `
Открой и изучи СВОИМИ ГЛАЗАМИ через Read:
РЕФЕРЕНС (эталон, цель): ${REF}
ТЕКУЩАЯ СЦЕНА (реальный GPU-рендер WebGL-билда, hero-ракурс): ${SHOTS.join(' , ')}

Контекст: интерьер заброшенной викторианской оранжереи Botanika для AAA-игры Afterhumans
(WebGL, 3rd-person корги). Это НАСТОЯЩИЙ рендер игрового билда на GPU браузера — весь
свет работает (ambient, point lights, туман, bloom), пост-обработка активна (9 эффектов:
ACES, bloom, color grade, vignette, film grain, DoF). Сцена = процедурная архитектура
(центральная бетонная колонна, ферменный свод остекления, прозрачное стекло, лес снаружи)
+ реальные 2K PBR-материалы (пол — состаренное дерево, колонна — бетон, стены — штукатурка,
ковёр — ткань) + реальные 3D-ассеты растений + процедурные диван/столик/серверная/CRT/лозы.

ПОРОГ ПРИЁМКИ = 10/10. Это AAA. Ставь 10 ТОЛЬКО если кадр неотличимо-достоверно
соответствует уровню/настроению референса. 9 и ниже = FAIL, перечисли что мешает 10.
Будь предельно честен и придирчив, не завышай. Severity: CRITICAL/HIGH/MEDIUM/LOW.`

const ROLES = [
  { key: 'light', prompt: `Ты AAA lighting director. Оцени ТОЛЬКО свет/атмосферу: золотой час, направление/сила ключа, заполнение, тени, дымка/god-rays, тепло-холод, bloom/grade, эмиссия (CRT/LED). ${COMMON}` },
  { key: 'overall', prompt: `Ты AAA art director. Оцени общий арт: композиция, силуэт пространства, материалы, наполнение, узнаваемость кадра vs реф, что выглядит дёшево/битый артефакт. ${COMMON}` },
  { key: 'ref-fidelity', prompt: `Ты strict reference-fidelity judge. Сверь ПОЭЛЕМЕНТНО с рефом: центральная колонна, ферменный переплёт, стекло+сад, деревянный пол+ковёр, кожаный диван+столик с книгами, рабочие столы с зелёными CRT, стеллажи, серверная с LED, обилие растений/лоз, золотой час, обжитая заброшенность. Каждый отсутствующий/непохожий элемент = блокер. ${COMMON}` },
  { key: 'asset-quality', prompt: `Ты технический QA 3D-ассетов. Оцени КАЧЕСТВО моделей/материалов: реальная ли геометрия или примитив, текстуры на месте или белое/битое (magenta/радуга/z-fight), масштаб/посадка на пол, повторяемость, артефакты рендера. Дёшево/примитивно/битое = блокер. ${COMMON}` },
  { key: 'tim-proxy', prompt: `Ты цифровой двойник заказчика Тима Зинина: прямой, дерзкий, придирчив к деталям до занудства, мат ок. Ты сам выбрал этот реф. Дотягивает до него на 10/10 или нет? Что бесит, что не как на рефе. Не подлизывай. ${COMMON}` },
]

phase('Review')
const reviews = await parallel(ROLES.map(r => () =>
  agent(r.prompt, { label: `judge:${r.key}`, phase: 'Review', schema: SCORE_SCHEMA })
    .then(v => ({ role: r.key, ...v }))
))
const valid = reviews.filter(Boolean)
const scores = valid.map(v => `${v.role}=${v.score}(${v.verdict})`).join(' ')
const minScore = valid.length ? Math.min(...valid.map(v => v.score)) : 0
const allPass = valid.length === ROLES.length && valid.every(v => v.score >= 10)
log(`scores: ${scores} | min=${minScore} | allPass=${allPass}`)

phase('Synth')
const SYNTH_SCHEMA = {
  type: 'object',
  properties: {
    all_pass: { type: 'boolean' },
    min_score: { type: 'integer' },
    top_blockers: {
      type: 'array',
      items: {
        type: 'object',
        properties: {
          severity: { type: 'string' },
          issue: { type: 'string' },
          fix: { type: 'string' },
          raised_by: { type: 'array', items: { type: 'string' } },
        },
        required: ['severity', 'issue', 'fix'],
      },
    },
    next_actions: { type: 'array', items: { type: 'string' } },
    summary_ru: { type: 'string' },
  },
  required: ['all_pass', 'min_score', 'top_blockers', 'next_actions', 'summary_ru'],
}
const synth = await agent(
  `Ты ведущий приёмки. Вот 5 оценок судей (JSON):\n${JSON.stringify(valid, null, 1)}\n\n` +
  `Перепроверь, открыв кадры и реф своими глазами (${SHOTS.join(' , ')} vs ${REF}). ` +
  `Сведи блокеры (объедини дубли, отсортируй по severity/частоте упоминания), отсей галлюцинации ` +
  `(чего на кадрах НЕТ — выкинь). all_pass=true ТОЛЬКО если ВСЕ 5 судей >=10. Дай next_actions — ` +
  `конкретные правки в порядке приоритета для достижения 10/10. summary_ru — короткий вывод по-русски.`,
  { label: 'synthesis', phase: 'Synth', schema: SYNTH_SCHEMA })

return { scores, minScore, allPass, reviews: valid, synth }
