export const meta = {
  name: 'corgi-gait-acceptance',
  description: 'СПЕЦ-приёмка походки корги (G3): спец-судья анимации + независимый биомех-верификатор по кадровым последовательностям из живого WebGL-билда',
  phases: [
    { title: 'Judge', detail: 'спец-судья анимации + биомех-верификатор смотрят последовательности кадров' },
    { title: 'Verdict', detail: 'сводный вердикт PASS/FAIL по натуральности походки' },
  ],
}

const D = '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots'
// Sequences captured from the LIVE WebGL build via CDP (behind 3rd-person camera).
const FWD  = [1,2,3,4,5].map(i => `${D}/gaitK_fwd${i}.png`)
const BACK = [1,2,3,4,5].map(i => `${D}/gaitK_back${i}.png`)
const IDLE = [1,2].map(i => `${D}/gaitK_idle${i}.png`)
const FWD_Z  = [1,2,3,4,5].map(i => `${D}/gaitK_fwd${i}_z.png`)   // dog-cropped zoom
const BACK_Z = [1,2,3,4,5].map(i => `${D}/gaitK_back${i}_z.png`)

const V = {
  type: 'object',
  properties: {
    score: { type: 'integer', minimum: 1, maximum: 10 },
    verdict: { type: 'string', enum: ['PASS', 'FAIL'] },
    legs_move: { type: 'boolean', description: 'меняется ли поза ног между кадрами' },
    forward_pushoff: { type: 'boolean', description: 'есть ли мах ногой вперёд-назад (stride/push-off), а не топот на месте' },
    backward_ok: { type: 'boolean', description: 'назад НЕ лунная походка (ноги крутятся в верную сторону)' },
    foot_plant: { type: 'boolean', description: 'лапы ставятся (не скользят/не плывут)' },
    findings: { type: 'array', items: { type: 'string' } },
    fix_hint: { type: 'string' },
  },
  required: ['score', 'verdict', 'legs_move', 'forward_pushoff', 'backward_ok', 'foot_plant', 'findings', 'fix_hint'],
}

const BIOMECH = `
БИОМЕХАНИКА ШАГА СОБАКИ (эталон для оценки):
- Собака идёт 4-тактным боковым (lateral-sequence) шагом: задняя-левая → передняя-левая →
  задняя-правая → передняя-правая. В каждый момент опора минимум на 2-3 лапы (duty>50%).
- ГЛАВНОЕ отличие настоящего шага от "топота на месте": бедро/плечо машет ВПЕРЁД-НАЗАД
  (sagittal sweep). Опорная лапа стоит на земле и УХОДИТ НАЗАД относительно тела (отталкивание),
  потом поднимается (сгиб колена) и проносится ВПЕРЁД по воздуху. Если лапы только дёргаются
  вверх-вниз без переноса вперёд-назад — это БРАК (Тим: "топает лапами, не отталкивается").
- При движении НАЗАД цикл идёт в ОБРАТНУЮ сторону. Брак "лунная походка" = тело едет назад,
  а ноги шагают как вперёд (рассинхрон). Лапы при ходьбе назад должны загребать вперёд.`

phase('Judge')
const judges = [
  { key: 'gait-acceptance', prompt:
`Ты СПЕЦИАЛЬНЫЙ судья-приёмщик анимации четвероногих для AAA-игры (твоя единственная задача — походка корги, ничего больше).
${BIOMECH}

Перед тобой кадры из ЖИВОГО WebGL-билда (камера 3rd-person, сзади собаки). Это РЕАЛЬНЫЙ GPU-рендер.

ХОДЬБА ВПЕРЁД (W), последовательность ~0.18с между кадрами — общий план + зум на собаку:
${FWD.join('\n')}
ЗУМ: ${FWD_Z.join('\n')}

ХОДЬБА НАЗАД (S):
${BACK.join('\n')}
ЗУМ: ${BACK_Z.join('\n')}

СТОИТ (idle): ${IDLE.join('\n')}

Изучи СВОИМИ ГЛАЗАМИ через Read. Оцени:
1) legs_move — реально ли меняется поза ног между кадрами (а не застывшая/скользящая собака);
2) forward_pushoff — виден ли мах ногами вперёд-назад (stride с отталкиванием), а не топот на месте;
3) backward_ok — при ходьбе назад НЕ выглядит лунной походкой;
4) foot_plant — лапы выглядят поставленными, а не плывущими.
score 1-10: насколько походка натуральна и по-собачьи. PASS только если score>=7 И forward_pushoff И backward_ok И legs_move. Будь честен и придирчив.` },
  { key: 'biomech-verify', prompt:
`Ты независимый биомеханик-верификатор (перепроверяешь спец-судью, не доверяй ему слепо).
${BIOMECH}

Кадры (живой GPU WebGL-билд, камера сзади):
ВПЕРЁД зум: ${FWD_Z.join('\n')}
НАЗАД зум: ${BACK_Z.join('\n')}
ВПЕРЁД общий: ${FWD.join('\n')}
СТОИТ: ${IDLE.join('\n')}

Открой через Read и въедливо сравни позы ног МЕЖДУ кадрами последовательности. Конкретно ищи:
- меняется ли угол бёдер/плеч (мах вперёд-назад) — или ноги только вверх-вниз/неподвижны;
- есть ли фазовый сдвиг между 4 ногами (диагонали/боковая последовательность), или все ноги синхронно;
- при ходьбе назад — направление прокрутки ног.
Верни ту же структуру. Не завышай: если по кадрам нельзя подтвердить мах — ставь соответствующий флаг false и объясни.` },
]

const results = await parallel(judges.map(j => () =>
  agent(j.prompt + `\n\nВерни СТРОГО структуру (score, verdict, legs_move, forward_pushoff, backward_ok, foot_plant, findings[], fix_hint). Не пиши пользователю.`,
    { label: `gait:${j.key}`, phase: 'Judge', schema: V, agentType: 'general-purpose' }
  ).then(v => ({ judge: j.key, ...v })).catch(e => ({ judge: j.key, score: 0, verdict: 'FAIL', legs_move: false, forward_pushoff: false, backward_ok: false, foot_plant: false, findings: ['agent error: ' + String(e)], fix_hint: '' }))
))

const valid = results.filter(Boolean)
const minScore = valid.length ? Math.min(...valid.map(r => r.score)) : 0
const pushoff = valid.every(r => r.forward_pushoff)
const backOk = valid.every(r => r.backward_ok)
const legsMove = valid.every(r => r.legs_move)
const allPass = valid.length === judges.length && valid.every(r => r.verdict === 'PASS')
log(`gait: min=${minScore} pushoff=${pushoff} backOk=${backOk} legsMove=${legsMove} allPass=${allPass}`)

phase('Verdict')
return {
  allPass, minScore,
  forward_pushoff: pushoff, backward_ok: backOk, legs_move: legsMove,
  judges: valid,
  blockers: valid.flatMap(r => r.findings.map(f => ({ judge: r.judge, finding: f }))),
  fixes: valid.map(r => ({ judge: r.judge, fix: r.fix_hint })),
}
