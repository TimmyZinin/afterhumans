# Post-Mortem Analysis Prompt — Послелюди / Afterhumans

## Для кого этот промпт

Ты — эксперт с двойной специализацией:
1. **Senior Game Developer** (10+ лет, shipped AAA titles) — знаешь pipeline, tools, production, visual quality gates
2. **AI-Assisted Game Development Specialist** — понимаешь возможности и ограничения разработки игр через LLM-агентов (Claude, Codex, Cursor), batchmode Unity, процедурную генерацию без визуального редактора

## Контекст проекта

**Послелюди / Afterhumans** — narrative walker (walking simulator) от первого лица, Unity 6 URP, macOS M1 8GB. Первая сцена «Ботаника» — стеклянная оранжерея с 5 NPC и собакой Kafka.

**Цель:** привести первую сцену до AAA-уровня (референсы: Sable, Firewatch, Journey, Dear Esther).

**Команда:** 1 человек (Тим, CMO / не разработчик игр) + Claude Code (Opus 4.6) как основной разработчик.

**Затраченные ресурсы:**
- 12+ спринтов кода (47 задач, 5 layers)
- Verification system: 23/23 criteria PASS
- ~6 сессий Claude Code (каждая ~200K токенов)
- Все Editor scripts, Runtime scripts, диалоги, документация — написаны Claude

## Что было создано (on paper — впечатляет)

```
Editor Scripts (9): BotanikaDresser, BotanikaEnvProps, BotanikaAtmosphere,
  BotanikaNpcPopulator, BotanikaLightingBaker, BotanikaSkyboxBuilder,
  VolumeProfileBuilder, BotanikaVerification, BuildScript

Runtime Scripts (16): SimpleFirstPersonController, PlayerInteraction,
  DialogueManager, DialogueUI, Interactable, BotanikaIntroDirector,
  NpcIdleBob, NpcFacing, BlinkingLight, InteractionPromptUI, SceneTheme,
  ThemeLoader, FootstepController, BotanikaAmbientAudio, DoorCueUI,
  ChapterIndicatorUI

Design Docs (6): ART_BIBLE.md, STORY.md, CHARACTERS.md, GDD.md,
  UNIVERSE.md, SCOPE.md

Verification: 23/23 automated criteria PASS
Ink Dialogues: 593 строки, 5 NPC knots
Assets: 1056 FBX, 2557 textures (Kenney packs + PolyHaven HDRI)
```

## Что получилось в реальности (по QA playtest)

**AAA Readiness Score: 1/10**

- Все поверхности — flat solid-color (ни одной текстуры)
- NPC неразличимы (одинаковые серые болванки)
- Kafka = набор мелких чёрных примитивов, не читается как собака
- Освещение плоское (нет теней, нет контраста, нет атмосферы)
- Игрок стабильно разворачивался в стену (cursor lock bug)
- Игрок проваливался сквозь пол (gravity + thin colliders)
- 50% запусков — камера после кинематика смотрит в стену
- Объекты неузнаваемые — диван, стол, книги сливаются в коричневую массу
- Graffiti отзеркалено
- Нет ощущения пространства, нет ощущения оранжереи

**Один скриншот стоит тысячи слов:** игрок видит коричневую стену вплотную, с кусочком красного текста "seg" в углу. Это всё.

## Документы для анализа

Прочитай ВСЕ перед ответом:

```
docs/SESSION_TRANSFER.md    — полный контекст: что сделано, все файлы
docs/BACKLOG_REMAINING.md   — бэклог багов
docs/PLAYTEST_REPORT.md     — QA playtest с оценками 1/10
docs/AAA_GAP_REPORT.md      — gap analysis: current vs target
docs/ART_BIBLE.md           — визуальное направление (то что ХОТЕЛИ)
docs/GDD.md                 — game design document
```

Также посмотри ключевые editor scripts:
```
Assets/_Project/Editor/BotanikaDresser.cs       — главный dresser
Assets/_Project/Editor/SceneEnricher.cs         — scene setup
Assets/_Project/Editor/LightingSetup.cs         — освещение
Assets/_Project/Editor/BotanikaAtmosphere.cs    — атмосфера
Assets/_Project/Editor/BotanikaNpcPopulator.cs  — NPC
Assets/_Project/Editor/BotanikaVerification.cs  — 23 criteria
```

## Вопросы для анализа

### A. ДИАГНОСТИКА: Почему 12 спринтов и 47 задач дали результат 1/10?

1. **Process failure:** Где в пайплайне «спринт → код → verification → build» теряется визуальное качество? Почему 23/23 verification PASS но результат убогий?

2. **Architecture failure:** Правильно ли строить сцену через 9 Editor scripts (procedural generation) без визуального предпросмотра? Или это антипаттерн для визуально-интенсивных задач?

3. **Asset pipeline failure:** Kenney low-poly assets без текстур — это жизнеспособный путь к AAA? Или нужны другие ассеты? Какие?

4. **Lighting failure:** Почему настроенное по Art Bible освещение не дало результата? Проблема в параметрах, в pipeline (baked vs realtime), в URP ограничениях, или в отсутствии визуального тюнинга?

5. **Iteration failure:** Почему фундаментальные баги (провал сквозь пол, камера в стену, cursor lock) прожили 12 спринтов? Что в процессе не ловит такие проблемы?

6. **AI-development failure:** Какие СПЕЦИФИЧЕСКИЕ ограничения разработки через Claude Code привели к этому результату? Что Claude Code НЕ МОЖЕТ делать, что human developer делает автоматически?

### B. ROOT CAUSE: Что конкретно пошло не так?

Для каждого root cause дай:
- Описание проблемы
- Почему она возникла именно в AI-assisted pipeline
- Как бы human developer избежал этой проблемы
- Конкретный fix для нашего проекта

### C. МЕТОДОЛОГИЯ: Как правильно разрабатывать Unity-игру через AI?

1. **Правильный pipeline:** опиши step-by-step workflow, который даст визуально качественный результат через Claude Code + Unity batchmode. Что делать в batchmode, что — в Unity Editor вручную?

2. **Visual feedback loop:** как организовать итерацию визуала когда разработчик — LLM без глаз? Какие инструменты, скрипты, автоматические проверки нужны?

3. **Asset strategy:** какие ассеты использовать для narrative walker на M1 8GB + URP? Kenney? Unity Asset Store? Megascans? Sketchfab CC0? Mixamo? Procedural? Конкретные паки и ссылки.

4. **Quality gates:** какие ВИЗУАЛЬНЫЕ (не технические) criteria нужно проверять автоматически? Screenshot comparison? Perceptual metrics? Цветовой анализ?

5. **Scope control:** 47 задач — это слишком много для первой итерации? Каким должен быть scope первого визуального milestone? Что делать ПЕРВЫМ, что — последним?

### D. ПЛАН СПАСЕНИЯ: Конкретный roadmap до AAA

Учитывая:
- Текущий код (9 editor scripts + 16 runtime scripts + 593 строки ink)
- Текущие ассеты (Kenney + PolyHaven HDRI)
- Инструмент: Claude Code (no Unity Editor GUI access)
- Hardware: M1 8GB, URP
- Цель: AAA narrative walker первая сцена

Дай конкретный пронумерованный план действий (не абстрактные советы):
- Что удалить / переписать
- Что добавить
- Какие ассеты скачать (конкретные URL или Asset Store names)
- Какой порядок действий (что ПЕРВОЕ)
- Сколько спринтов реалистично нужно
- Какие этапы делать в batchmode, какие — вручную в Unity Editor

### E. HONEST ASSESSMENT

1. Реалистично ли вообще достичь AAA-уровня визуала через Claude Code + batchmode без работы в Unity Editor? Или это фундаментально невозможный подход?

2. Если нужен компромисс — какой уровень визуала реально достижим через AI-only pipeline? Как он выглядит?

3. Какие минимальные ручные действия в Unity Editor дадут максимальный скачок качества?

## Формат ответа

Структурируй ответ по секциям A-E. В каждой секции:
- **Вердикт** (1-2 предложения, прямо)
- **Анализ** (подробно, с примерами из нашего кода)
- **Fix** (конкретные действия, файлы, команды)

Будь ЖЁСТКИМ и ЧЕСТНЫМ. Не «в целом неплохой подход, нужно доработать» — а «вот конкретная архитектурная ошибка, вот почему она катастрофическая, вот как исправить». Тим предпочитает brutal honesty.

Общий объём ответа: 3000-5000 слов. Это стратегический документ, не чеклист.
