export const meta = {
  name: 'botanika-game-final',
  description: 'Финальная приёмка ИГРЫ (follow-cam, что видит Тим): КАМЕРА (3rd-person, не в упор/не top-down), походка плавная, композиция+свет+NPC vs реф. ACCEPT или вернуть на доделку.',
  phases: [
    { title: 'Judge', detail: '3 судьи: camera + walk + art-composition' },
    { title: 'Verdict', detail: 'сводный ACCEPT/REVISE' },
  ],
}

// Свежие кадры текущего билда кладём СЮДА перед запуском судьи.
const D = '/Users/timofeyzinin/afterhumans/docs/judge_shots'
const REF = '/Users/timofeyzinin/afterhumans/docs/concepts/refs_channel/ref_botanika.jpg'
const SPAWN = `${D}/spawn.png`
const WALK = ['1','2','3','4'].map(n => `${D}/walk${n}.png`)
const LOOK = [`${D}/look1.png`, `${D}/look2.png`] // ракурсы осмотра (камера повёрнута)

const V = {
  type: 'object',
  properties: {
    check: { type: 'string' },
    pass: { type: 'boolean' },
    score: { type: 'integer', minimum: 1, maximum: 10 },
    findings: { type: 'array', items: { type: 'string' } },
    fix_hint: { type: 'string' },
  },
  required: ['check', 'pass', 'score', 'findings', 'fix_hint'],
}

phase('Judge')
const checks = [
  { key: 'camera', prompt:
`Ты — QA по камере 3rd-person экшена. Кадры из ЖИВОЙ игры (follow-камера, ровно что видит игрок): спавн ${SPAWN} ; в движении ${WALK.join(' , ')} ; осмотр ${LOOK.join(' , ')}.
Заказчик ЖЁСТКО жаловался ДВАЖДЫ: «камера в упор к собаке» и «камера сверху/top-down, не видно сцену — видно только спину/макушку пса».
ПРОВЕРЬ СТРОГО: (1) камера ПОЗАДИ собаки на разумной дистанции (видно собаку ЦЕЛИКОМ сбоку-сзади + пространство ВПЕРЁД), НЕ вплотную к спине; (2) угол взгляда примерно ГОРИЗОНТАЛЬНЫЙ (чуть сверху ок), НЕ отвесно-вниз на макушку (top-down = БРАК); (3) в кадре читается СЦЕНА впереди/вокруг (пол, зелень, мебель, глубина), а не только собака в упор. pass=true ТОЛЬКО если это нормальный играбельный 3rd-person с обзором сцены. score 1-10 (top-down/в упор = ≤4). fix_hint: что менять (высота камеры / дистанция / угол наклона / YAxis).` },
  { key: 'walk', prompt:
`Ты QA gameplay. Кадры из живой игры, собака идёт вперёд по W (~0.5с между кадрами): ${WALK.join(' , ')}.
Контакт лап с полом уже подтверждён телеметрией — про парение не спрашиваю.
ПРОВЕРЬ: (1) собака движется ВПЕРЁД носом (НЕ задом-наперёд!); (2) плавно, шаг уверенный, не семенит/не дёргается. pass=true если идёт носом вперёд и плавно. score 1-10. fix_hint если задом/рвано/семенит.` },
  { key: 'composition', prompt:
`Ты AAA арт-директор. Спавн ${SPAWN} ; движение ${WALK[0]} , ${WALK[2]} ; осмотр ${LOOK.join(' , ')}. Референс: ${REF}.
ПРОВЕРЬ vs реф: (1) тёплый ровный закатный свет БЕЗ выжженных в белый пятен и БЕЗ чёрных провалов; (2) пышная зелень, богатая сцена (оранжерея: колонна, стекло, мебель, ковёр); (3) NPC — РЕАЛЬНЫЕ люди с головами (НЕ капсулы-снеговики), стоят/сидят у мебели; (4) масштаб собака↔окружение естественный; (5) нет брака (плавающие объекты, клиппинг в белый). pass=true если близко к рефу по настроению и без брака. score 1-10. fix_hint: главный следующий шаг к рефу.` },
]

const results = await parallel(checks.map(c => () =>
  agent(c.prompt + `\n\nОткрой ВСЕ кадры через Read СВОИМИ ГЛАЗАМИ. Будь СТРОГ — лучше вернуть на доделку, чем пропустить брак. Верни СТРОГО объект (check="${c.key}", pass, score, findings[], fix_hint). Не пиши пользователю.`,
    { label: `judge:${c.key}`, phase: 'Judge', schema: V, agentType: 'general-purpose' }
  ).then(v => v).catch(e => ({ check: c.key, pass: false, score: 0, findings: ['agent error: ' + String(e)], fix_hint: '' }))
))

const valid = results.filter(Boolean)
log('RESULT: ' + valid.map(r => `${r.check}=${r.pass?'PASS':'FAIL'}(${r.score})`).join(' | '))

phase('Verdict')
const allPass = valid.every(r => r.pass)
return {
  verdict: allPass ? 'ACCEPT' : 'REVISE',
  allPass,
  results: valid,
  fixes: valid.filter(r => !r.pass).map(r => `[${r.check}] ${r.fix_hint}`),
}
