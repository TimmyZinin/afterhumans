# Оставшийся бэклог — Botanika Phase 1.5

## Немедленно (блокирует playability)

### BUG-01: Полный gameplay loop не подтверждён
- **Что:** Диалоги, hover prompts, gate cue, scene transition — всё реализовано в коде но ни разу не протестировано в runtime
- **Как проверить:** Computer Use → запустить игру → пройти весь сценарий по шагам (docs/QA_PLAYTEST_PROMPT.md)
- **Файлы:** PlayerInteraction.cs, DialogueUI.cs, DialogueManager.cs, Interactable.cs, DoorCueUI.cs, SceneExitTrigger.cs

### BUG-02: Камера после кинематика (фикс не проверен)
- **Что:** Применён фикс Euler(5°,0,0) для ориентации камеры в комнату. Не проверено в runtime.
- **Файл:** BotanikaIntroDirector.cs:74-76

### BUG-03: Graffiti возможно отзеркалено
- **Что:** На ранних скриншотах "segfault == freedom" читалось задом наперёд. Rotation фикс применён (Euler 0,180,0 для north wall) но не проверен в runtime.
- **Файл:** BotanikaAtmosphere.cs (BuildWindowGlassOverlays) + BotanikaEnvProps.cs (BuildGraffiti)

## После подтверждения gameplay loop

### VISUAL-01: Kafka — заменить 2-cube placeholder
- **Текущее:** 2 куба (тёмный body + белый chest) с KafkaFollowSimple
- **Целевое:** 8-cube corgi (body/head/4 legs/tail/ears) или CC0 dog mesh
- **Файлы:** SceneEnricher.cs:503-557 (CreateKafkaPlaceholder)

### VISUAL-02: NPC — текстуры Kenney blocky-characters
- **Текущее:** character-a..e.fbx без текстур (default URP/Lit)
- **Целевое:** применить texture-a..e.png из Kenney pack для различимости
- **Файлы:** BotanikaNpcPopulator.cs (SpawnNpc)

### AUDIO-01: Заменить процедурное аудио на CC0
- **Текущее:** sine-wave ambient drone + noise footsteps (ProceduralAudioGenerator)
- **Целевое:** Suno AI lofi track + Freesound CC0 footsteps/ambient SFX
- **Файлы:** BotanikaAmbientAudio.cs, FootstepController.cs, ProceduralAudioGenerator.cs

### AUDIO-02: Kafka звуки
- **Что:** bark/sniff/paws при движении и idle
- **Файл:** KafkaFollowSimple.cs + новый KafkaAudio.cs

### STORY-01: Nikolai audio sting (P2)
- **Что:** При начале диалога с Николаем — music duck + low drone
- **Файл:** новый AudioStinger.cs

### STORY-02: Stas-Kafka event (P2)
- **Что:** Однократное событие когда Kafka подходит к Стасу
- **Файл:** новый Timeline или coroutine

## Performance validation

### PERF-01: Runtime FPS baseline
- **Как:** Development build → PerformanceReporter → PerformanceBaseline.txt
- **Критерий:** avg FPS >= 40 на M1 8GB
- **Файл:** BuildScript.cs (добавить BuildMacOSDebug), PerformanceReporter.cs

## Протокол для каждого спринта

```
1. /self-adversarial — критика подхода перед началом
2. Implement — написать код
3. Build — пересобрать сцену + .app
4. Test — Computer Use playtest
5. Commit — git add + commit
6. /mm-review — MiniMax M2.5 должен дать APPROVED
7. Push — git push origin main
```

mm-review запускается так:
```bash
# В текущей сессии Claude Code:
/mm-review Sprint N — описание что проверять
```

Скилл `/mm-review` уже настроен в `~/.claude/skills/mm-review/`.
