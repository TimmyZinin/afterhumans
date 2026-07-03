export const meta = {
  name: 'botanika-gameplay-qa',
  description: 'Агентная проверка живого WebGL-билда Botanika: камера 3rd-person, анимация ходьбы, чистота сцены, look vs реф',
  phases: [
    { title: 'QA', detail: '4 QA-агента независимо проверяют кадры из живого билда' },
    { title: 'Verdict', detail: 'сводный вердикт PASS/FAIL по геймплею' },
  ],
}

const D = '/Users/timofeyzinin/afterhumans/docs/m1_greybox_shots'
const REF = '/Users/timofeyzinin/afterhumans/docs/concepts/refs_channel/ref_botanika.jpg'
const F = {
  idle:  `${D}/qa_idle.png`,
  walk1: `${D}/qa_walk1.png`,
  walk2: `${D}/qa_walk2.png`,
  turnL: `${D}/qa_turnL.png`,
  turnR: `${D}/qa_turnR.png`,
  hero:  `${D}/qa_hero.png`,
}

const V = {
  type: 'object',
  properties: {
    check: { type: 'string' },
    pass: { type: 'boolean' },
    confidence: { type: 'string', enum: ['high', 'medium', 'low'] },
    findings: { type: 'array', items: { type: 'string' } },
    fix_hint: { type: 'string' },
  },
  required: ['check', 'pass', 'confidence', 'findings', 'fix_hint'],
}

phase('QA')
const checks = [
  { key: 'camera', prompt:
`Ты QA геймплейной камеры 3rd-person. Это кадры из ЖИВОГО WebGL-билда игры (управляешь корги-собакой).
Открой через Read:
- idle (стоит): ${F.idle}
- после поворота ВЛЕВО (клавиша A): ${F.turnL}
- после поворота ВПРАВО (клавиша D): ${F.turnR}
ПРОВЕРЬ: камера должна быть СЗАДИ собаки (видно спину/хвост/затылок), НЕ морду/лицо. При поворотах камера должна оставаться за спиной (а не показывать морду). Заказчик жаловался: "вижу морду собаки куда ни повернусь" — это БАГ. pass=true ТОЛЬКО если во всех 3 кадрах видно собаку преимущественно со спины/сверху-сзади, морда НЕ направлена в камеру. Если видно морду/глаза/грудь спереди — pass=false. fix_hint: что крутить (FreeLook X/recenter/facing).` },
  { key: 'animation', prompt:
`Ты QA анимации персонажа. Два кадра сняты во время ХОДЬБЫ с интервалом ~0.4с:
- walk1: ${F.walk1}
- walk2: ${F.walk2}
- (для сравнения, стоит на месте): ${F.idle}
ПРОВЕРЬ: при ходьбе НОГИ собаки должны менять позу между walk1 и walk2 (шаг: одна нога вперёд/другая назад, разное положение лап) — это значит walk-цикл играет. Если ноги в обоих кадрах в ОДИНАКОВОЙ позе/прямые/неподвижные = собака "скользит" (БАГ, заказчик жаловался). pass=true если видно смену позы ног между кадрами или явную походку. Оцени и естественность (natural/janky). fix_hint про анимацию/риг.` },
  { key: 'cleanliness', prompt:
`Ты QA чистоты сцены. Кадры из билда:
- ${F.idle}
- ${F.hero}
- ${F.walk1}
ПРОВЕРЬ на СЫРЫЕ артефакты/баги, которые портят сцену:
1) безголовые/половинчатые человеческие фигуры (NPC без головы) — критичный баг;
2) жёлто-чёрная "разметочная лента"/hazard-полоса на полу (placeholder);
3) плавающие/битые объекты, magenta/радужные текстуры, z-fighting;
4) фигуры, провалившиеся в мебель.
pass=true если НИ ОДНОГО из этих артефактов не видно. Перечисли что нашёл. ВАЖНО: пустые места где НЕТ людей — это НЕ баг (NPC временно скрыты намеренно), не считай отсутствие людей багом.` },
  { key: 'look', prompt:
`Ты AAA арт-директор. Сравни общий LOOK билда с референсом.
- РЕФЕРЕНС (цель): ${REF}
- ТЕКУЩИЙ кадр билда: ${F.hero}
Контекст: заброшенная викторианская оранжерея, золотой час, 3rd-person корги. Это РАБОЧИЙ прогресс (играбельный билд), НЕ финал.
Оцени ЧЕСТНО прогресс к рефу по: золотой свет/глубина (туман), отсутствие пересветов в белый, тёплый тон, наполнение/композиция. pass=true если кадр уверенно движется к настроению рефа (тёплый золотой, читается глубина, нет белых клиппингов), даже если до фотореала далеко. Перечисли 2-3 главных оставшихся разрыва (для roadmap). fix_hint — самый важный следующий шаг.` },
]

const results = await parallel(checks.map(c => () =>
  agent(c.prompt + `\n\nВерни СТРОГО структурный объект (check, pass, confidence, findings[], fix_hint). check="${c.key}". Не пиши пользователю.`,
    { label: `qa:${c.key}`, phase: 'QA', schema: V, agentType: 'general-purpose' }
  ).then(v => v).catch(() => ({ check: c.key, pass: false, confidence: 'low', findings: ['agent error'], fix_hint: '' }))
))

const valid = results.filter(Boolean)
const passed = valid.filter(r => r.pass).map(r => r.check)
const failed = valid.filter(r => !r.pass)
log(`QA PASS: ${passed.join(', ') || 'none'} | FAIL: ${failed.map(r => r.check).join(', ') || 'none'}`)

phase('Verdict')
return {
  allPass: failed.length === 0,
  passed,
  failed: failed.map(r => ({ check: r.check, findings: r.findings, fix_hint: r.fix_hint })),
  full: valid,
}
