# Sprint 11: Research — Text-to-3D инструменты

## Задача
Найти инструмент для создания 3D моделей через промпт. Бесплатно. CLI/MCP. Game-ready.

## Сравнительная таблица

| Инструмент | Бесплатно | CLI | MCP | Качество | Game-ready FBX | Текстуры | M1 Mac | Управление из Claude |
|------------|-----------|-----|-----|----------|----------------|----------|--------|---------------------|
| **Blender MCP** | ДА | ДА (`blender -b --python`) | ДА (5+ реализаций) | ★★★★★ | ДА | ДА | ДА | ДА — через MCP |
| **Tripo3D** | ДА (300 credits/мес = ~12 моделей) | НЕТ | ДА (official tripo-mcp) | ★★★★★ | ДА (GLB/FBX) | ДА | ДА | ДА — через MCP |
| **Meshy.ai** | ДА (100 credits/мес) | НЕТ | ДА (meshy-ai-mcp-server) | ★★★★★ | ДА (GLB) | ДА | ДА | ДА — через MCP |
| **Point-E** (OpenAI) | ДА (open source) | ДА (Python) | НЕТ | ★★★★ | Частично (point cloud → mesh) | НЕТ | ДА | Через Bash |
| **Shap-E** (OpenAI) | ДА (open source) | ДА (Python) | НЕТ | ★★★★ | ДА (textured mesh) | ДА | ДА | Через Bash |
| **threestudio** | ДА (open source) | ДА (Python) | НЕТ | ★★★★★ | ДА (GLB/FBX) | ДА | ДА | Через Bash |
| **StableGen** (Blender) | ДА (open source) | Через Blender CLI | НЕТ | ★★★★★ | ДА | ДА | ДА | Через Blender CLI |
| **Modly** | ДА | НЕТ (GUI) | НЕТ | ★★★ | Частично | НЕТ | ДА | НЕТ |

## Детальный анализ TOP-3

### 1. Blender MCP — ЛУЧШИЙ ВЫБОР

**Что это:** MCP-сервер позволяющий Claude Code управлять Blender напрямую. Создавать объекты, применять материалы, экспортировать FBX — всё через промпт.

**Реализации на GitHub:**
- `poly-mcp/Blender-MCP-Server` — 50+ tools, auto dependency install, thread-safe
- `djeada/blender-mcp-server` — 22 tools, 6 namespaces
- `ahujasid/blender-mcp` — Claude-to-Blender прямой контроль

**Возможности:**
- Создание 3D объектов по текстовому описанию
- Применение PBR материалов
- UV mapping
- Export в FBX/GLTF (Unity-совместимо)
- Работает на M1 Mac (Blender native build)
- Полностью БЕСПЛАТНО

**Pipeline:** Claude Code → MCP → Blender → export FBX → Unity import

**Преимущество:** НЕТ лимитов. Бесконечная генерация. Полный контроль.

**Недостаток:** Blender нужно установить. Модели = процедурные (из примитивов), не AI-генерированные. Для AI-генерации нужен StableGen аддон.

### 2. Tripo3D MCP — ЛУЧШИЙ ДЛЯ AI-ГЕНЕРАЦИИ

**Что это:** Cloud AI сервис генерации 3D моделей из текста/изображений. Есть official MCP server.

**Free tier:** 300 credits/месяц ≈ 12 моделей
**Качество:** Высокое, game-ready
**Export:** GLB/FBX с текстурами
**MCP:** `VAST-AI-Research/tripo-mcp` (official)

**Pipeline:** Claude Code → MCP → Tripo API → download GLB → Unity import

**Преимущество:** AI-генерация по промпту. "Создай деревянный стеллаж с книгами" → готовая 3D модель.

**Недостаток:** 12 моделей/месяц бесплатно. Для нашей сцены (~20 объектов) нужно 2 месяца или платить.

### 3. Meshy.ai MCP — ЛУЧШИЙ ДЛЯ ПЕРСОНАЖЕЙ

**Что это:** AI сервис с фокусом на персонажах. Текстурирование, rigging, анимация.

**Free tier:** 100 credits/месяц
**MCP:** `pasie15/meshy-ai-mcp-server`
**Уникальное:** text-to-texture, remeshing, optimization, rigging

**Pipeline:** Claude Code → MCP → Meshy API → download GLB → Unity import

**Преимущество:** Полный pipeline для персонажей (генерация → текстуры → rig → animate).

**Недостаток:** 100 credits/месяц. CC BY 4.0 на free tier (не коммерческое).

## Anthropic / Claude Code экосистема

**Установленные скиллы:**
- `3d-artist` — принципы создания 3D ассетов, оптимизация, pipeline
- `3d-games` — рендеринг, шейдеры, физика, камеры
- `game-art` — визуальный стиль, ассет pipeline, анимация
- `game-development` — оркестратор, маршрутизация к суб-скиллам

**Возможности:** Скиллы дают ЗНАНИЯ (как создавать), но не ИНСТРУМЕНТЫ (чем создавать). Для создания моделей нужен внешний инструмент (Blender MCP / Tripo / Meshy).

**Unity ProceduralMesh API:** Можно создавать mesh через C# код (уже делаем — Kafka из 22 примитивов). Но для реалистичных персонажей недостаточно — получаются "кубики".

## РЕКОМЕНДАЦИЯ

### Основной инструмент: Blender MCP
**Почему:**
1. Полностью бесплатно, без лимитов
2. MCP — Claude Code управляет напрямую
3. CLI для batch-операций
4. FBX export → Unity
5. M1 Mac native
6. Можно комбинировать с AI аддонами (StableGen, Dream Textures)

### Дополнительный: Tripo3D MCP
**Почему:**
1. AI-генерация по промпту (то что Blender сам не даёт)
2. 12 моделей/месяц бесплатно
3. Для ключевых объектов (персонажи, собака) — создать через Tripo
4. Остальное — через Blender MCP процедурно

### Предлагаемый pipeline:

```
КЛЮЧЕВЫЕ ОБЪЕКТЫ (персонажи, собака, уникальная мебель):
  Claude Code → Tripo3D MCP → GLB → Unity import
  (12 моделей/месяц бесплатно)

МАССОВЫЕ ОБЪЕКТЫ (стены, полы, простая мебель, пропсы):
  Claude Code → Blender MCP → FBX → Unity import
  (безлимитно, процедурная генерация)

ТЕКСТУРЫ:
  ambientCG.com → curl download → Unity import
  (CC0, 2800+ PBR текстур бесплатно)
```

## Следующий шаг (Sprint 12)

1. Установить Blender (если не установлен)
2. Подключить Blender MCP server к Claude Code
3. Зарегистрировать Tripo3D API key (бесплатно)
4. Подключить Tripo MCP server
5. Тестовая генерация: создать один объект через каждый инструмент
6. Импортировать в Unity → проверить в сцене
