export const meta = {
  name: 'botanika-density-judge',
  description: 'Стало ли гуще/богаче зеленью vs прошлый билд, БЕЗ возврата гигантских горшков и без потери масштаба собаки',
  phases: [{ title: 'Judge', detail: 'before/after density + scale preserved' }, { title: 'Verdict', detail: 'свод' }],
}

const D = '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots'
const REF = '/Users/timofeyzinin/afterhumans/docs/concepts/refs_channel/ref_botanika.jpg'
const BEFORE = `${D}/heroT_spawn2.png`         // оригинал до добавления зелени
const AFTER  = `${D}/hero5_spawn.png`          // после: почвопокровка + deep-fill + ВЕРТИКАЛЬНЫЙ плющ на стёклах
const AFTER_WALK = ['2','4','6'].map(n => `${D}/hero5_walk${n}.png`)
const BEFORE_WALK = `${D}/heroT_walk5.png`     // самый пустой кадр «до» (у стекла)

const V = {
  type: 'object',
  properties: {
    pass: { type: 'boolean' },
    denser: { type: 'boolean', description: 'стало гуще/богаче зеленью чем ДО' },
    scale_preserved: { type: 'boolean', description: 'собака осталась соразмерным героем (горшки/растения НЕ вернулись к гигантизму)' },
    score: { type: 'integer', minimum: 1, maximum: 10 },
    findings: { type: 'array', items: { type: 'string' } },
    fix_hint: { type: 'string' },
  },
  required: ['pass', 'denser', 'scale_preserved', 'score', 'findings', 'fix_hint'],
}

phase('Judge')
const prompt =
`Ты AAA арт-директор по композиции теплицы. Я добавил НИЖНИЙ ярус почвопокровной зелени + заполнил дальний конец зала (у стеклянной стены), НЕ увеличивая горшки.
ДО (спавн): ${BEFORE}
ПОСЛЕ (спавн): ${AFTER}
ДО — самый пустой кадр (у стекла): ${BEFORE_WALK}
ПОСЛЕ — в движении (включая дальний конец у стекла): ${AFTER_WALK.join('\n')}
Референс настроения/плотности: ${REF}

ПРОВЕРЬ:
1) denser — стало ли ГУЩЕ/богаче зеленью чем ДО (особенно: нижний ярус у пола вокруг пути собаки + дальний конец у стекла больше НЕ голый пол)?
2) scale_preserved — собака ОСТАЛАСЬ соразмерным героем? Горшки/растения НЕ вернулись к гигантизму (это была исходная жалоба — не должно повториться)?
3) Нет нового брака (плавающие растения, клиппинг в собаку, дубли).
pass=true если denser И scale_preserved И без брака. score 1-10 общей насыщенности vs реф. fix_hint главный следующий шаг.`

const results = await parallel([0,1].map(k => () =>
  agent(prompt + `\n\nОткрой ВСЕ кадры через Read СВОИМИ ГЛАЗАМИ (сравни ДО vs ПОСЛЕ). Верни СТРОГО объект схемы. Ты судья #${k+1}. Не пиши пользователю.`,
    { label: `density:${k+1}`, phase: 'Judge', schema: V, agentType: 'general-purpose' }
  ).then(v => v).catch(e => ({ pass:false, denser:false, scale_preserved:false, score:0, findings:['err '+String(e)], fix_hint:'' }))
))

const valid = results.filter(Boolean)
log('DENSITY: ' + valid.map(r=>`#${r.score} denser=${r.denser} scale=${r.scale_preserved} pass=${r.pass}`).join(' | '))

phase('Verdict')
return {
  allPass: valid.every(r=>r.pass),
  denserBoth: valid.every(r=>r.denser),
  scaleBoth: valid.every(r=>r.scale_preserved),
  avgScore: Math.round(valid.reduce((a,r)=>a+r.score,0)/valid.length*10)/10,
  judges: valid,
}
