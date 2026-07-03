export const meta = {
  name: 'botanika-build-r-verify',
  description: 'Верификация Build R: походка не семенит / масштаб горшков vs собака / реализм не просел',
  phases: [
    { title: 'Judge', detail: '3 независимых судьи: gait / scale / realism' },
    { title: 'Synth', detail: 'сводный вердикт' },
  ],
}

const D = '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots'
const REF = '/Users/timofeyzinin/afterhumans/docs/concepts/refs_channel/ref_botanika.jpg'
const WALK = [1,2,3,4,5].map(i => `${D}/R_walk${i}.png`)
const SPAWN = `${D}/R_spawn.png`
const PREV = `${D}/P_spawn.png`

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
  { key: 'gait', prompt:
`Ты QA анимации четвероногих. Кадры ходьбы ВПЕРЁД из живого WebGL-билда (follow-камера сзади), ~0.4с между кадрами:
${WALK.join('\n')}
Заказчик жаловался: собака «топает-семенит» — мелкие частые шажки, выглядит неуверенно. Я снизил скорость и увеличил мах ног/длину шага.
ПРОВЕРЬ: шаг теперь КРУПНЫЙ и УВЕРЕННЫЙ (нога заметно выносится вперёд-назад, не мелкое семенение)? Меняется ли поза ног между кадрами естественно? pass=true если шаг читается как спокойная уверенная походка, НЕ семенит. score 1-10 натуральности шага. fix_hint если ещё семенит/не так.` },
  { key: 'scale', prompt:
`Ты арт-директор по композиции/масштабу. Заказчик: «собака выглядит несоразмерно — горшки/растения кажутся слишком большими». Я уменьшил высоту горшков (были 1.05-1.2 м при собаке 0.78 м → стали 0.62-0.78 м) и ближние монстеры (1.3-1.7→1.0-1.2 м).
Кадр СЕЙЧАС: ${SPAWN}
Кадр ДО (горшки были крупнее): ${PREV}
Референс (для ощущения масштаба): ${REF}
ПРОВЕРЬ: теперь собака читается как соразмерный/доминирующий герой рядом с растениями (горшки НЕ возвышаются над псом)? pass=true если масштаб собака↔горшки выглядит правильно/естественно. score 1-10. fix_hint если что-то ещё мелкое/крупное.` },
  { key: 'realism', prompt:
`Ты AAA арт-директор. Заказчик отметил, что реализм сцены «стал получше» — проверь, что мои правки (масштаб растений, скорость/шаг собаки) НЕ испортили общий вид.
Кадры: ${SPAWN} , ${WALK[1]} , ${WALK[3]}
Референс: ${REF}
ПРОВЕРЬ: золотой тёплый свет, пышная зелень, собака освещена и читается, нет нового брака (плавающие/клиппинг/пересвет/пустота). pass=true если общий реализм сохранён или лучше (не регресс). score 1-10. fix_hint главный следующий шаг к рефу.` },
]

const results = await parallel(checks.map(c => () =>
  agent(c.prompt + `\n\nОткрой все кадры через Read СВОИМИ ГЛАЗАМИ. Верни СТРОГО объект (check, pass, score, findings[], fix_hint). check="${c.key}". Не пиши пользователю.`,
    { label: `judge:${c.key}`, phase: 'Judge', schema: V, agentType: 'general-purpose' }
  ).then(v => v).catch(e => ({ check: c.key, pass: false, score: 0, findings: ['agent error: ' + String(e)], fix_hint: '' }))
))

const valid = results.filter(Boolean)
const passed = valid.filter(r => r.pass).map(r => r.check)
const failed = valid.filter(r => !r.pass)
log(`PASS: ${passed.join(', ') || 'none'} | FAIL: ${failed.map(r=>r.check).join(', ') || 'none'} | scores: ${valid.map(r=>r.check+'='+r.score).join(' ')}`)

phase('Synth')
return {
  allPass: failed.length === 0,
  scores: valid.map(r => ({ check: r.check, score: r.score, pass: r.pass })),
  results: valid,
}
