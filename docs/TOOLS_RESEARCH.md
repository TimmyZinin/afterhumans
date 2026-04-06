# Sprint 11: Research — Text-to-3D инструменты (расширенное)

## Задача
Найти лучший инструмент для создания 3D моделей через промпт. Бесплатно. CLI/MCP. Game-ready. Глубокое исследование Blender + AI, аналоги, use cases, best practices.

---

## ПОЛНАЯ СРАВНИТЕЛЬНАЯ ТАБЛИЦА (15 инструментов)

| # | Инструмент | Бесплатно | CLI | MCP | Качество | FBX/GLB | M1 Mac | Управление из Claude | Best for |
|---|------------|-----------|-----|-----|----------|---------|--------|---------------------|----------|
| 1 | **Blender MCP** | ДА, безлимит | ДА | ДА (5+ repos) | ★★★★★ | ДА | ДА | ДА | Процедурные модели, полный контроль |
| 2 | **Tripo3D** | 300 cr/мес (~12) | НЕТ | ДА (official) | ★★★★★ | ДА | ДА | ДА | AI text-to-3D быстро |
| 3 | **Meshy.ai** | 100 cr/мес | НЕТ | ДА (2 repos) | ★★★★★ | ДА | ДА | ДА | Персонажи + rigging |
| 4 | **TRELLIS 2.0** (Microsoft) | ДА, open source | ДА | ДА (trellis_mcp) | ★★★★★ | ДА | ДА | ДА | SOTA качество, CVPR'25 |
| 5 | **Sloyd.ai** | ДА, безлимит | SDK | НЕТ | ★★★★★ | ДА | ДА | Через API | Game assets, Unity plugin |
| 6 | **Hyper3D Rodin** | ДА ($0 start) | НЕТ | НЕТ | ★★★★★ | ДА | ДА | Через API | Коммерческие права |
| 7 | **CRM** | ДА, open source | ДА | НЕТ | ★★★★ | ДА | ДА | Через Bash | Image→3D за 10 сек |
| 8 | **LGM** | ДА, open source | ДА | НЕТ | ★★★★ | Частично | ДА | Через Bash | Fastest (5 сек) |
| 9 | **Point-E** (OpenAI) | ДА, open source | ДА | НЕТ | ★★★ | Частично | ДА | Через Bash | Быстрый прототип |
| 10 | **Shap-E** (OpenAI) | ДА, open source | ДА | НЕТ | ★★★★ | ДА | ДА | Через Bash | Text + Image input |
| 11 | **threestudio** | ДА, open source | ДА | НЕТ | ★★★★★ | ДА | ДА | Через Bash | Multi-framework unified |
| 12 | **StableGen** (Blender) | ДА, open source | Через Blender | НЕТ | ★★★★★ | ДА | ДА | Через Blender MCP | AI текстуры в Blender |
| 13 | **Meta AssetGen 2.0** | ДА (Horizon) | Скоро | НЕТ | ★★★★★ | ДА | ДА | НЕТ пока | PBR materials из коробки |
| 14 | **Neural4D** | Freemium | НЕТ | НЕТ | ★★★★ | ДА | ДА | Через API | 3D printing + game |
| 15 | **Modly** | ДА | НЕТ (GUI) | НЕТ | ★★★ | Частично | ДА | НЕТ | Offline, без интернета |

---

## BLENDER — ГЛУБОКОЕ ИССЛЕДОВАНИЕ

### Почему Blender — лучший выбор

1. **Безлимитно бесплатный** — никаких credits, quotas, подписок
2. **5+ MCP серверов** — Claude Code управляет напрямую
3. **CLI автоматизация** — `blender -b --python script.py`
4. **Native M1** — ARM64 сборка, Metal GPU
5. **FBX export** — прямой путь в Unity
6. **AI аддоны** — Dream Textures, StableGen, 3D-Agent, Tripo plugin
7. **Огромная экосистема** — 20+ лет, миллионы пользователей

### MCP серверы для Blender (TOP-3)

| Repo | Stars | Tools | Особенности |
|------|-------|-------|-------------|
| `ahujasid/blender-mcp` | 13.7K | Core | Основной, Claude Desktop совместим |
| `poly-mcp/Blender-MCP-Server` | ~1K | 50+ | Thread-safe, auto dependencies |
| `djeada/blender-mcp-server` | ~500 | 22 (6 namespaces) | Самый документированный |

### Установка Blender MCP для Claude Code

```bash
# 1. Установить Blender
brew install --cask blender

# 2. Добавить MCP в Claude Code
claude mcp add blender uvx blender-mcp

# 3. В Blender: установить addon
# Edit → Preferences → Add-ons → Install → addon.py
# Enable "MCP Blender Bridge"
# Side panel (N) → Blender MCP → Start MCP Server
```

### Best Practices (из реальных проектов)

1. **Разбивать сложные задачи на шаги** — "создай стол" лучше чем "создай комнату с мебелью"
2. **Указывать "game-ready"** — Claude оптимизирует topology
3. **Промпт = описание + ограничения** — "low-poly sofa, max 2000 triangles, PBR materials"
4. **Timeout на больших сценах** — первая команда может зависнуть, повторить
5. **Human review для артистичных решений** — Claude хорош в execution, не в art direction

### Use Cases (реальные примеры)

| Проект | Что создали | Качество | Время |
|--------|------------|----------|-------|
| Архитектура | Высотное здание из промпта | Высокое | ~10 мин |
| Game сцена | Dungeon с драконом | Среднее-высокое | ~15 мин |
| Персонажи | Low-poly фигурки | Среднее | ~5 мин |
| Окружение | Пляж с пальмами + HDRI | Высокое | ~20 мин |
| Мебель | Стол, стул, диван | Высокое | ~3 мин каждый |

### Pipeline: Blender MCP → Unity

```
Claude Code промпт: "Создай деревянный стеллаж с книгами, low-poly, game-ready"
    ↓
Blender MCP → создаёт модель в Blender
    ↓
Blender CLI: blender -b model.blend --python export_fbx.py
    ↓
FBX файл → Assets/_Project/Models/bookcase.fbx
    ↓
Unity batchmode: BotanikaBuilder.PlaceFbx("bookcase.fbx", position)
```

### Скиллы для работы с Blender

| Скилл | Источник | Описание |
|-------|---------|----------|
| **Blender Toolkit** | mcpmarket.com | PBR materials, mesh modifiers, Blender 4.0+ |
| **3D-Agent** | 3d-agent.com | Multi-agent system для Blender |
| **BlenderGPT** | blendergpt.org | GPT-powered, Python code preview |

**Рекомендация:** установить `blender-mcp` как MCP сервер в Claude Code config.

---

## TRELLIS 2.0 — НОВАЯ НАХОДКА (SOTA)

**Что:** Microsoft Research, CVPR'25 Spotlight. State-of-the-art text-to-3D.
**GitHub:** `microsoft/TRELLIS.2`
**Есть MCP:** `FishWoWater/trellis_mcp`
**Есть Blender plugin:** `FishWoWater/trellis_blender`
**Качество:** Лучшее из доступных (4B параметров)
**Бесплатно:** Да, open source
**Export:** GLB, OBJ, FBX, PLY

**Уникальное:** Генерирует 3D из текста/изображения с production-quality topology. Лучше Tripo/Meshy по качеству mesh.

---

## SLOYD.AI — UNITY-NATIVE

**Что:** AI генератор game-ready моделей с Unity plugin.
**Бесплатно:** Да, безлимитная генерация
**Unity plugin:** Да, прямой импорт
**SDK:** Python, JavaScript
**Export:** GLB, FBX, OBJ

**Уникальное:** Единственный с нативным Unity plugin. Parametric API для кастомизации.

---

## РЕКОМЕНДАЦИЯ (обновлённая)

### Tier 1 — Устанавливаем обязательно:
1. **Blender** (`brew install --cask blender`) — основной инструмент
2. **Blender MCP** (`claude mcp add blender`) — управление из Claude Code

### Tier 2 — Подключаем для AI-генерации:
3. **Tripo3D MCP** — 12 AI-моделей/месяц бесплатно
4. **TRELLIS 2.0 MCP** — SOTA качество, open source, безлимитно

### Tier 3 — Текстуры:
5. **ambientCG.com** — 2800+ CC0 PBR текстур через curl

### Pipeline (финальный):

```
ПЕРСОНАЖИ: Tripo3D/TRELLIS → GLB → Blender (доработка) → FBX → Unity
МЕБЕЛЬ: Claude Code → Blender MCP (процедурно) → FBX → Unity
ПРОПСЫ: Claude Code → Blender MCP → FBX → Unity
ТЕКСТУРЫ: ambientCG.com → curl → Unity import
УНИКАЛЬНЫЕ: TRELLIS 2.0 (text→3D SOTA) → GLB → Unity
```

---

## Следующий шаг (Sprint 12)

1. `brew install --cask blender`
2. Подключить Blender MCP к Claude Code
3. Зарегистрировать Tripo3D API (бесплатно)
4. Тестовая генерация: один объект через Blender MCP
5. Тестовая генерация: один объект через Tripo3D
6. Import обоих в Unity → сравнить в сцене
