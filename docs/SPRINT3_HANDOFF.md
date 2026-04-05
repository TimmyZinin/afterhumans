# Sprint 3 Handoff — Botanika AAA Phase 1

> **Дата:** 2026-04-06 ~00:45
> **Статус:** Foundation layer 10/10 + Art A01 10/10 DONE + 2 mm-review passes applied
> **План:** `~/.claude/plans/rosy-bouncing-quasar.md` (992 строки, 47 задач)
> **Готовность к компактированию:** ✅

---

## 🎯 Контекст одной строкой

Unity 6 narrative walker «Послелюди / Afterhumans» Episode 0, фаза 1 — полировка **первой сцены Botanika** до Firewatch/Journey AAA-уровня. Phase 2 (City) и Phase 3 (Desert) отложены. Автономная работа Claude + skills (агенты запрещены).

---

## 📦 Что сделано к этой точке

### Аналитическая фаза (3 sprints, 4 docs)

| Документ | Содержание |
|---|---|
| `docs/expert-panel/01_LIBRARY_AUDIT.md` | 510 Kenney FBX, 1 Poly Haven HDRI, URP installed but inactive, 14/15 ART_BIBLE gap |
| `docs/expert-panel/02_EXPERT_PANEL_SPRINT1.md` | 6 экспертов (Art/Tech/Game/Narrative/Audio/QA), Script-to-Game coverage 1/10, Audio 0/15, 3 panel conflicts resolved |
| `docs/expert-panel/03_SPRINT2_DEEP_CRITIQUE.md` | 68 findings через 10 gamedev skill lenses + сводная матрица + топ-20 корневых действий |
| `~/.claude/plans/rosy-bouncing-quasar.md` | Approved Plan Mode roadmap, 47 tasks в 5 layers (F/A/N/S/T), 23 DONE criteria, AAA auto-test harness |

### Execution фаза (6 git commits)

```
f93f1dc polish(foundation): mm-review per-task audit → push all 11 tasks toward 10/10
85ff86a fix(foundation): mm-review Sprint 3 — 1 CRITICAL + 5 HIGH findings fixed
d025670 feat(art): BOT-A01 VP_Botanika/City/Desert Volume Profiles + F02 scale fix
56148fc feat(foundation): BOT-F04..F10 — Input Actions, ColliderHelper, SceneTheme, Perf, Cache, HUD gate, Static flags
af98ace feat(foundation): BOT-F01 URP activation + F02 Kenney scale fix + F03 AudioRouter
```

### Tasks score card (после 2 mm-review passes)

| Task | Final score | Notes |
|---|---|---|
| **F01** URP activation | **10/10** | `UrpActivation.Activate()` → auto-runs `ConvertMaterialsToUrp()`, 490 materials Standard→URP/Lit converted |
| **F02** Kenney FBX scale | **10/10** | `globalScale=0.2, useFileScale=false` empirically tuned; verified via `ScaleDiagnostics.cs` (sofa 2.24m, coffee table 0.80m, lamp 1.72m) |
| **F03** AudioMixer infra | **10/10** | Runtime `AudioRouter.cs` singleton + `AudioRouterConfig.asset` fallback (native AudioMixer deferred — Unity C# API не создаёт .mixer programmatically) |
| **F04** Input System migration | **9/10 hybrid** | `AfterhumansInput.inputactions` + `AfterhumansInputWrapper.cs` lazy loader. Code migration `SimpleFirstPersonController`+`PlayerInteraction` explicitly deferred до BOT-S01 Timeline phase. Legacy Input работает через `activeInputHandler = Both` |
| **F05** ColliderHelper | **10/10** | BoxCollider bounds-based, exception list, wired в 3 dressers, mm-review size math fix |
| **F06** SceneTheme SO | **10/10** | 5 theme assets с палитрами ART_BIBLE §3, ThemeLoader runtime, Camera.main race fix, `ClearActive()` static cleanup |
| **F07** PerfReporter | **10/10** | DEVELOPMENT_BUILD only, reusable StringBuilder, writes `PerformanceBaseline.txt` |
| **F08** Interactable cache | **10/10** | `Interactable.All` static list + `OnEnable`/`OnDisable`/`OnDestroy`, sqrMagnitude вместо Distance |
| **F09** Debug HUD gate | **10/10** | `showDebugHud = false` default + `Afterhumans/Debug/Toggle PlayerInteraction HUD` QA menu |
| **F10** Static flags | **10/10** | `MarkStaticProp()` wired в dressers + `Afterhumans/Debug/Verify Static Flags` menu |
| **A01** VolumeProfiles | **10/10** | VP_Botanika (9 overrides), VP_City (8), VP_Desert (8), wired в SceneTheme.postFxProfile, `SafeClearProfile()` via `VolumeProfile.Remove<T>()` |

**Среднее: 9.9/10** (10 tasks at 10, F04 explicitly hybrid 9).

### Визуальный результат

Screenshot `/tmp/afterhumans_debug/mm_fix_verify.png` показывает Scene_Botanika:
- Sofa 2.24m в центре (orange, правильный scale)
- Bookcases + side tables + coffee table
- Floor lamp 1.7m высотой
- 4 ceiling lamps
- Зелёные bushes + красные/фиолетовые цветы
- Blue sky через оконные cutouts
- Warm URP ambient + baked shadows

Это **уровень Tchia/Crash Bandicoot 4** low-poly + post-FX (target был Firewatch/Journey — нужно ещё density + NPC + god rays + baked GI для финала).

---

## 📋 Что осталось в плане (36 задач)

### Art layer (8 из 9 остались)
- **A02** Poly Haven warm interior HDRI для Botanika
- **A03** Env props (server rack + graffiti + laptop + bottle + turka + foil hat)
- **A04** Volumetric god rays через стеклянную крышу
- **A05** Grain overlay verification
- **A06** 3200K lighting refined + accent point lights (coffee, Nikolai, Kirill, cool spot на serverrack)
- **A07** Glass shader для окон
- **A08** Color palette SceneTheme enforcement в BotanikaDresser
- **A09** Baked GI lightmap

### NPC layer (9 задач)
- **N01** Quaternius Ultimate Modular Characters download
- **N02** Mixamo idle animations для 5 NPC (требует Adobe OAuth 1×)
- **N03** 5 NPC spawn в Scene_Botanika с правильными позициями
- **N04** Kafka CC0 corgi mesh + Animator
- **N05** Worldspace prompts «[E] говорить»
- **N06** Dialogue UI speaker name prefix
- **N07** Typewriter 22 cps + skip on E
- **N08** Nikolai turn-to-player on interact
- **N09** Kafka 3D sound cues (bark, paws, sniff)

### Narrative/Story layer (11 задач)
- **S01** Scene 1.1 cinematic wake-up Timeline + Cinemachine (L, 6h)
- **S02** First-look pan над Ботаникой
- **S03** `note` knot wire + paper quad
- **S04** Wire все 5 Ink knots verification
- **S05** `door_to_city_open` visual cue
- **S06** Ambient audio loop + 3D coffee drip SFX
- **S07** Footstep controller player
- **S08** Chapter indicator UI fade
- **S09** 30-second test verification (bot-t прогон)
- **S10** Nikolai exposition audio sting
- **S11** Stas Kafka feeding event (P2 optional)

### Automated AAA Test Harness layer (10 задач)
- **T01-T10** BotanikaVerification, URP checker, PlayMode tests, screenshot harness, histogram palette check, magenta detection, silhouette test, motion monitor, perf automation, master scorecard → 23/23 criteria PASS

---

## 🛑 Новые правила sprint protocol (зафиксированы Тимом)

### Перед КАЖДЫМ спринтом — `/self-adversarial`

**Причина:** у нас **нет Codex** в этой сессии. `/self-adversarial` skill — fallback где Claude сам играет жёсткого критика. Задаёт вопросы: *«что сломается?», «какие edge cases пропущены?», «правильно ли архитектурно?», «где trade-offs?»*

**Порядок:**
1. Прочитать task из plan (BOT-X)
2. Прочитать `/self-adversarial` skill принципы
3. **Применить 10-пунктный adversarial checklist** к upcoming task
4. Зафиксировать warnings/gotchas в комментарии к task (или рядом)
5. Скорректировать approach если найдены risks
6. Только ТОГДА начать IMPLEMENT

### После КАЖДОГО спринта — `/mm-review`

**Причина:** повторить то что сделали с Foundation layer — поймать regressions, architecture smells, missing edge cases. Оценка per-task **10/10**. Если < 10 — fix до того как двигаться дальше.

**Порядок:**
1. Закончить IMPLEMENT + TEST + VERIFY + DEPLOY (commit)
2. Запустить `/mm-review` на свежие commits
3. Прочитать verdict per finding (CRITICAL → HIGH → MEDIUM → LOW)
4. **CRITICAL+HIGH — блокеры следующего спринта**. MEDIUM/LOW можно batch в polish pass
5. Fix всё CRITICAL+HIGH
6. Re-run `/mm-review` или spot-check fixes
7. Update task score card в handoff
8. Continue only когда target score (10/10 или explicit hybrid) достигнут

### Extended Sprint Protocol для Afterhumans

Заменяет стандартный 8-step протокол из `~/.claude/rules/sprint-protocol.md`:

```
0. /self-adversarial  (NEW — перед каждым спринтом, Codex fallback)
1. PLAN               (из rosy-bouncing-quasar.md, read task spec)
2. HITL               (skip для стандартных, иначе short notify)
3. IMPLEMENT          (only Claude + skills, NO agents)
4. TEST               (build + screenshot + log verification)
5. VERIFY             (все acceptance criteria из plan)
6. SEC                (git add selective, не git add .)
7. DEPLOY             (git commit, not push — Тим пушит)
8. /mm-review         (NEW — mandatory audit после каждого спринта)
9. Fix <10/10         (NEW — блокер следующего спринта)
10. NOTIFY            (short summary, без excessive detail)
```

---

## 🎓 Обязательные skills для execution

### Gamedev core (используются continuously)

| Skill | Когда применять | Ключевые принципы из Sprint 2 |
|---|---|---|
| **3d-games** | Любая task с rendering/shaders/cameras/physics | LOD strategy, batching, frustum culling, no mesh colliders everywhere, «3D is illusion» |
| **game-art** | Asset pipeline, palette, naming, silhouette | «1 unit = 1 meter industry standard», silhouette readability test, define + follow style guide, «art serves gameplay» |
| **game-audio** | Любая task со звуком — mixer, footsteps, cues | Category hierarchy (voice reference, SFX slightly below, music mid, ambient low), 3D spatialization, ducking снапшоты, «50% of experience is audio» |
| **game-design** | Core loop, pacing, emotional beats, NPC design | 30-second test, early wins, flow state, rest beats, reward schedules |
| **game-development** | Orchestrator — always loaded | Performance budget 60FPS=16.67ms, State Machine default, input abstraction (actions not raw keys), profile first |
| **ui-ux-pro-max** | Dialogue UI, menu, HUD, prompts, settings | Color contrast 4.5:1, touch targets 44px, animation 150-300ms, reduced-motion, focus states |
| **frontend-design** | Main menu, credits, any UI screens | Tim calibrated profile: Typography 7/7 MAX, Approach 2/7 utilitarian, Mood 6-7 playful, Color 6/7 colorful, Structure 6/7 asymmetric, Density 6/7 spacious |
| **scroll-experience** | Cinematic pacing, camera work, transitions | «Scroll = narrative device», moments of delight, sticky sections, parallax depth layers |
| **theme-factory** | Data-driven consistency across scenes | Cohesive palette + typography + sound per locale |
| **3d-web-experience** | Interaction patterns (transferable to Unity) | Moments of wonder, accessibility для genre newcomers, glTF standard |

### Process skills (quality gates)

| Skill | Когда применять |
|---|---|
| **`/self-adversarial`** | **BEFORE каждого спринта** — adversarial critique плана задачи |
| **`/mm-review`** | **AFTER каждого спринта** — per-task audit с 10/10 scoring |
| **`/expert-panel`** | Для major decisions (pipeline switch, asset source choice, scope pivot) |
| **`/combo-review`** | Для ship-critical code (deploy, payments, auth) — не наш случай сейчас |

### Что НЕ использовать

- ❌ **Любые агенты** (`Agent` tool) — Тим явно запретил 2026-04-05 для этой задачи
- ❌ Post-production skills (`/deploy-project`, `/verify`, `/wiki`) — отложены в `docs/POSTPRODUCTION.md`
- ❌ Marketing skills (`/launch-strategy`, `/copywriting`) — post-release
- ❌ `/mm` slave tasks — это не delegation, это hands-on execution

---

## 🔧 Созданные / модифицированные файлы

### Новые (23 файла)
- `Assets/_Project/Editor/UrpActivation.cs` — URP pipeline activation + material converter
- `Assets/_Project/Editor/KenneyAssetPostprocessor.cs` — scale 0.2 + disable cameras/lights/anims
- `Assets/_Project/Editor/AudioMixerSetup.cs` — AudioRouter config verification/creation
- `Assets/_Project/Editor/ColliderHelper.cs` — BoxCollider helper + MarkStaticProp + VerifyStaticFlags
- `Assets/_Project/Editor/SceneThemeBuilder.cs` — 5 theme assets creation
- `Assets/_Project/Editor/VolumeProfileBuilder.cs` — 3 URP Volume Profiles creation
- `Assets/_Project/Editor/ScaleDiagnostics.cs` — runtime bounds measurement tool
- `Assets/_Project/Editor/DebugMenuToggles.cs` — QA toggle menu items
- `Assets/_Project/Scripts/Art/SceneTheme.cs` — ScriptableObject с palette/lighting/fog/audio
- `Assets/_Project/Scripts/Art/ThemeLoader.cs` — runtime component применяющий theme
- `Assets/_Project/Scripts/Audio/AudioRouterConfig.cs` — volume hierarchy ScriptableObject
- `Assets/_Project/Scripts/Audio/AudioRouter.cs` — runtime singleton applying ducking
- `Assets/_Project/Scripts/Debug/PerformanceReporter.cs` — FPS/memory/DC baseline
- `Assets/_Project/Input/AfterhumansInput.inputactions` — Input Actions asset
- `Assets/_Project/Input/AfterhumansInput.cs` — C# wrapper (namespace `Afterhumans.InputSystems`)
- `Assets/_Project/Settings/URP/Afterhumans_URP_Asset.asset`
- `Assets/_Project/Settings/URP/Afterhumans_URP_Renderer.asset`
- `Assets/_Project/Settings/URP/VolumeProfiles/VP_Botanika.asset`
- `Assets/_Project/Settings/URP/VolumeProfiles/VP_City.asset`
- `Assets/_Project/Settings/URP/VolumeProfiles/VP_Desert.asset`
- `Assets/_Project/Art/Themes/Botanika.asset`, `City.asset`, `Desert.asset`, `MainMenu.asset`, `Credits.asset`
- `Assets/_Project/Audio/AudioRouterConfig.asset`
- `docs/SPRINT3_HANDOFF.md` (this file)

### Modified
- `Assets/_Project/Editor/SceneEnricher.cs` — added themeName field, CreateThemeLoader method
- `Assets/_Project/Editor/BotanikaDresser.cs` — URP/Lit shader, ColliderHelper integration
- `Assets/_Project/Editor/CityDresser.cs` — same
- `Assets/_Project/Editor/DesertDresser.cs` — same
- `Assets/_Project/Scripts/Dialogue/Interactable.cs` — static All list
- `Assets/_Project/Scripts/Player/PlayerInteraction.cs` — cached iteration, sqrMagnitude, HUD off
- `ProjectSettings/GraphicsSettings.asset` — URP pipeline reference
- `ProjectSettings/QualitySettings.asset` — URP per quality level
- 510 Kenney FBX `.meta` files — globalScale 0.2
- 490 Material `.mat` files — Standard → URP/Lit shader

---

## 🎛 Ключевые команды для continuation

### Run all foundation setup (idempotent)

```bash
cd ~/afterhumans

# Step 1: URP pipeline (runs ConvertMaterialsToUrp automatically)
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.UrpActivation.Activate \
  -logFile -

# Step 2: Force reimport Kenney at correct scale
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.KenneyAssetPostprocessor.ReimportKenney \
  -logFile -

# Step 3: Build scene themes
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.SceneThemeBuilder.BuildAll \
  -logFile -

# Step 4: Build Volume Profiles
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.VolumeProfileBuilder.BuildAll \
  -logFile -

# Step 5: Enrich scenes (wires ThemeLoader + existing SceneTransition + Exit triggers)
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.SceneEnricher.EnrichAllScenes \
  -logFile -

# Step 6: Dress Botanika with props
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BotanikaDresser.Dress \
  -logFile -

# Step 7: Build macOS .app
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -buildTarget StandaloneOSX \
  -executeMethod Afterhumans.EditorTools.BuildScript.BuildMacOS \
  -logFile -

# Step 8: Launch + screenshot for visual verify
open ~/afterhumans/build/Afterhumans.app
sleep 4
osascript -e 'tell application "Послелюди" to activate'
screencapture -x /tmp/afterhumans_debug/verify.png
```

### Diagnostics

```bash
# Verify URP active
grep "m_CustomRenderPipeline" ~/afterhumans/ProjectSettings/GraphicsSettings.asset
# Expect: m_CustomRenderPipeline: {fileID: 11400000, guid: ..., type: 2}

# Verify Kenney scale
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.ScaleDiagnostics.VerifyKenneyScale \
  -logFile -
# Expect: sofa ~2m, table ~0.8m, lamp ~1.7m

# Verify static flags count
~/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity \
  -batchmode -nographics -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.ColliderHelper.VerifyStaticFlags \
  -logFile -
```

---

## 🚀 Immediate next step post-compact

1. **Resume session** (`claude --resume`) чтобы получить свежий контекст.
2. **Прочитать** этот файл + `memory/session_handoff.md` + `~/.claude/plans/rosy-bouncing-quasar.md`.
3. **Обновить task #60**, #61 статусы если нужно (should be completed).
4. **Запустить `/self-adversarial`** для BOT-A02 (Poly Haven HDRI) — adversarial critique перед implement.
5. **Execute BOT-A02** — download CC0 HDRI, create Skybox material, wire в Botanika theme.
6. **Запустить `/mm-review`** после A02 commit — audit decision + code review.
7. **Fix findings <10/10** if any.
8. **Continue BOT-A03** через тот же цикл.
9. **Periodic `claude --resume`** для keeping контекст свежим (каждые ~3-5 спринтов).

---

## ⚠️ Known gaps / technical debt (deferred, documented)

| Gap | Severity | Why deferred | When to fix |
|---|---|---|---|
| **F04 full Input System migration** | Low | Legacy Input работает через Both mode, migration не на critical path | BOT-S01 Timeline phase (cinematic wake-up требует action-based input) |
| **Native AudioMixer asset** | Low | Unity C# API не позволяет создать .mixer programmatically. AudioRouter fallback достаточен для placeholder audio | После Дениса когда native mixer нужен для production audio |
| **MEDIUM/LOW findings from mm-review pass 1** | Low | UrpActivation metallic copy, BotanikaDresser array allocation, OnGUI style alloc | Phase 1 cleanup pass перед ship |
| **A04 god rays shader performance** | Low | Может быть дорогой на M1 8GB, fallback particles + light cookies готов | BOT-A04 execution с perf check |
| **Mixamo OAuth для N02** | Low | Требует Тим 1-минутный Adobe login или fallback на Quaternius Ultimate Animated | BOT-N02 когда дойдём |
| **Sketchfab CC0 corgi для N04** | Medium | 3-tier fallback готов в plan (Sketchfab → Quaternius Animals → improved placeholder) | BOT-N04 |

---

## 📊 Progress metrics

- **Закрыто в Sprint 3:** 11 tasks (F01-F10 + A01)
- **Остаётся в плане:** 36 tasks (A02-A09 + N01-N09 + S01-S11 + T01-T10)
- **Commits:** 6 (4 execution + 2 mm-review polish)
- **Lines of code added:** ~2500 C#
- **Files created:** 23
- **Files modified:** 15+ scripts + 510 meta + 490 materials
- **Build size:** 99 MB Universal Binary macOS .app
- **Average task score:** 9.9/10
- **Review passes:** 2 × `/mm-review` (initial + polish)

---

**Status:** READY FOR COMPACT. Все критичные точки задокументированы, git committed, handoff trinity обновлён (этот файл + session_handoff.md + project_afterhumans.md).
