export const meta = {
  name: 'corgi-gait-T-judge',
  description: 'Приёмка походки v3 (Build T): убрана ли фаза полёта (feet_planted), пропало ли семенение, уверенный ли шаг — по чистой боковой последовательности кадров',
  phases: [
    { title: 'Judge', detail: '2 спец-судьи: аниматор + биомех-верификатор смотрят боковую последовательность' },
    { title: 'Verdict', detail: 'сводный PASS/FAIL' },
  ],
}

const D = '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots'
// Build T3 (lateral-sequence phasing + duty 0.80 + lower lift/bob). Clean side sequence, dog walking L→R.
const SEQ = ['01','02','03','04','05','06','07','08'].map(n => `${D}/svW_${n}_z.png`)
const FULL = ['03','06'].map(n => `${D}/svW_${n}.png`)   // full frame for ground-plane context

const BIOMECH = `
БИОМЕХАНИКА ШАГА СОБАКИ (эталон):
- 4-тактный боковой шаг. duty factor каждой ноги ~0.7 → в ЛЮБОЙ момент на земле минимум 2-3 лапы из 4.
  Значит фазы ПОЛЁТА (когда ВСЕ лапы в воздухе одновременно) у шага НЕТ — полёт бывает только у бега/галопа.
- Опорная лапа стоит на полу и уходит НАЗАД относительно тела (отталкивание); затем колено сгибается,
  лапа поднимается и проносится ВПЕРЁД по воздуху, ставится снова. Бедро/плечо машет вперёд-назад.
- ВАЖНО про тень: свет направленный под углом → тень падает чуть В СТОРОНУ/ВПЕРЁД от лапы, а не строго под ней.
  Поэтому «зазор до тени» сам по себе НЕ доказывает отрыв. Смотри, достаёт ли НИЗ опорной лапы до ПЛОСКОСТИ ПОЛА
  (клетчатые квадраты на глубине собаки), а не до тени.
`

const V = {
  type: 'object',
  properties: {
    score: { type: 'integer', minimum: 1, maximum: 10 },
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    confident_stride: { type: 'boolean', description: 'шаг крупный/уверенный, нога заметно выносится вперёд-назад' },
    mincing_gone: { type: 'boolean', description: 'НЕ семенит (нет мелких частых топающих шажков на месте)' },
    feet_planted: { type: 'boolean', description: 'опорные лапы достают до плоскости пола; НЕТ фазы полёта (хотя бы 2 лапы всегда внизу)' },
    legs_cycle_natural: { type: 'boolean', description: 'поза ног естественно меняется между кадрами (сгиб колена на проносе, вынос)' },
    findings: { type: 'array', items: { type: 'string' } },
    fix_hint: { type: 'string', description: 'если что-то не так — КОНКРЕТНО что крутить (амплитуда/duty/strideLength/IK clamp/bob)' },
  },
  required: ['score', 'verdict', 'confident_stride', 'mincing_gone', 'feet_planted', 'legs_cycle_natural', 'findings', 'fix_hint'],
}

phase('Judge')
const judges = [
  { key: 'animator', role:
`Ты СТАРШИЙ аниматор-QA четвероногих в AAA-студии.` },
  { key: 'biomech', role:
`Ты биомеханик-верификатор походки животных, придирчивый скептик. По умолчанию ищешь брак.` },
]

const ctx = `${BIOMECH}
ИСТОРИЯ: заказчик жаловался — собака «топает-семенит» (мелкие частые шажки, неуверенно). Я переделал процедурную походку v3:
увеличил мах бедра (37°), длину шага под мах (no-slip), duty factor 0.72 (чтобы НЕ было фазы полёта — всегда ≥2 лапы внизу),
убавил подпрыгивание корпуса, расширил IK-вилку чтобы опорная лапа доставала до пола на полном выносе.

КАДРЫ (боковая камера, собака идёт слева-направо, ~0.4-0.6с между кадрами), зум на собаку:
${SEQ.join('\n')}
Полный кадр для контекста плоскости пола:
${FULL.join('\n')}

ЗАДАЧА: оцени походку. Главный вопрос этой итерации — УБРАНА ли фаза полёта (предыдущий судья нашёл «лапы плывут над землёй в фазе выноса, читается как прыжок-бег, а не шаг»). Проверь по ПЛОСКОСТИ ПОЛА (не по тени, см. примечание про угол света).`

const results = await parallel(judges.map(j => () =>
  agent(`${j.role}\n\n${ctx}\n\nОткрой ВСЕ кадры через Read СВОИМИ ГЛАЗАМИ, просмотри как покадровую анимацию. Верни СТРОГО объект схемы. Не пиши пользователю.`,
    { label: `judge:${j.key}`, phase: 'Judge', schema: V, agentType: 'general-purpose' }
  ).then(v => ({ who: j.key, ...v })).catch(e => ({ who: j.key, score: 0, verdict: 'FAIL', confident_stride: false, mincing_gone: false, feet_planted: false, legs_cycle_natural: false, findings: ['agent error: ' + String(e)], fix_hint: '' }))
))

const valid = results.filter(Boolean)
log('VERDICTS: ' + valid.map(r => `${r.who}=${r.verdict}(${r.score}) planted=${r.feet_planted} minc_gone=${r.mincing_gone} conf=${r.confident_stride}`).join(' | '))

phase('Verdict')
const allPass = valid.every(r => r.verdict === 'PASS')
const plantedAll = valid.every(r => r.feet_planted)
const mincingGoneAll = valid.every(r => r.mincing_gone)
const confidentAll = valid.every(r => r.confident_stride)
return {
  allPass,
  feet_planted: plantedAll,
  mincing_gone: mincingGoneAll,
  confident_stride: confidentAll,
  avgScore: Math.round(valid.reduce((a, r) => a + r.score, 0) / valid.length * 10) / 10,
  judges: valid,
}
