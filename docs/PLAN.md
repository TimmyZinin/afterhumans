# Dataland / Episode 0 — Project Plan

> **Статус:** Pre-production, Блок 1, Этап 0 (выбор названия)
> **Создан:** 2026-04-05
> **Последнее обновление:** 2026-04-05
> **Название проекта:** TBD (выбирается)
> **Финальный deliverable:** `.dmg` на лендинге `timzinin.com/<slug>/`, друзья с Mac скачивают и запускают

---

## Концепция в одном абзаце

Narrative walker от первого лица на Unity 6 LTS (URP), Mac standalone. Игрок начинает в **Ботанике** — последнем оазисе живого в мире, где люди downgraded себя в нейроинтерфейсы (по Harari, *Homo Deus*). Через стерильный футуристичный город игрок выходит в **пустыню** эстетики *Dune*. Финальный твист: настоящий мир — пустыня, город и Ботаника были предсказанием, которое не сбылось полностью. Тон диалогов — *Rick and Morty*, TV-MA, русский язык. Продолжительность демо — 10-15 минут прохождения. Три сцены: **Ботаника → Город → Пустыня**. Задумано как первый эпизод сериала.

## Технический стек

- **Engine:** Unity 6 LTS (6000.0.72f1)
- **Render pipeline:** URP (HDRP отвергнут из-за 8 GB RAM на MacBook Pro M1)
- **Target:** Standalone macOS (Apple Silicon)
- **Dialogue:** Ink (inkle) + ink-unity-integration
- **Assets:** Quixel Megascans (CC0 через Epic), Poly Haven (CC0), Kenney (CC0), Quaternius (CC0), Unity Starter Assets, Mixamo
- **Audio:** Freesound.org (CC0) + Suno (опционально)
- **Distribution:** ad-hoc signed `.dmg` → Contabo nginx → `timzinin.com/<slug>/`

## Схема разработки

```
PRE-PRODUCTION → PRODUCTION → POST-PRODUCTION
(планирование)   (разработка)  (упаковка + дистрибуция)
```

---

## БЛОК 1. PRE-PRODUCTION

Только документы и решения. Unity ещё не запущен.

### Этап 1. Вселенная и нарратив
- 3-4 варианта концепции вселенной
- Выбор одного или комбинирование
- Фиксация: где игрок, что видит, кто вокруг, смысл мира
- **Deliverable:** `docs/UNIVERSE.md`

### Этап 2. Сюжет Episode 0
- Что происходит за 10-15 минут прохождения
- Функции трёх локаций в сюжете
- Финальный момент и твист
- **Deliverable:** `docs/STORY.md`

### Этап 3. Персонажи и диалоги
- 5-7 NPC в Ботанике, 2-3 в городе, 1 в пустыне
- Архетипы, характеры, манера речи
- Полный скрипт в Ink-формате (русский, R&M-регистр)
- **Deliverable:** `docs/CHARACTERS.md` + `Assets/Dialogues/dataland.ink`

### Этап 4. Art Bible
- Стиль URP stylized-realistic, референсы (Sable, Tchia, Death Stranding)
- Цветовая палитра по локациям
- Освещение, камера, post-processing
- Аудио-направление
- **Deliverable:** `docs/ART_BIBLE.md`

### Этап 5. GDD (Game Design Document)
- Core loop, управление, камера
- Что игрок делает / не делает
- Прогрессия между сценами
- Fail states (нет)
- **Deliverable:** `docs/GDD.md`

### Этап 6. Technical plan
- Структура Unity-проекта
- Список ассет-библиотек и лицензий
- Build pipeline, signing, distribution
- **Deliverable:** `docs/TECH.md`

### Этап 7. Scope lock
- Что в Episode 0
- Cut list
- Fallback scope для разных временных порогов
- **Deliverable:** `docs/SCOPE.md`

**Выход из Блока 1:** папка `docs/` с 7 markdown-файлами + 1 `.ink`. Все решения приняты.

---

## БЛОК 2. PRODUCTION

### Этап 8. Environment setup
- Unity Hub + Unity 6 LTS + Mac Build Support + URP template
- Xcode Command Line Tools, Git LFS
- Пустой URP-проект, первый коммит, первый смоук-билд
- **Deliverable:** открывающийся проект + пустой билд

### Этап 9. Asset acquisition
- Quixel Megascans (desert biome, камни, текстуры)
- Poly Haven (HDRI sunset + desert)
- Kenney + Quaternius (пропы Ботаники, sci-fi город)
- Mixamo (7-10 персонажей + анимации)
- Starter Assets FirstPerson
- Ink Unity integration
- Freesound.org ambient + footsteps
- **Deliverable:** `Assets/Vendor/` наполнен, проект компилируется

### Этап 10. Walking skeleton (критический)
- FirstPerson controller работает
- 3 пустые сцены с маркерами, переходы
- Ink-диалоги, один тестовый диалог на сцену
- Финальный экран с твист-текстом
- Первый `.app` на Mac Тима
- **Deliverable:** проходимый серый билд 3 минуты от начала до конца
- **Чекпойнт:** если сюда не дошли — останавливаемся

### Этап 11. Ботаника визуально
- Теплица/оазис из ассетов
- 5 NPC, idle-анимации, все 5 диалогов
- Освещение, post-processing, эмбиент
- **Deliverable:** билд с живой Ботаникой

### Этап 12. Город визуально
- Модульная улица, холодная палитра
- 2-3 NPC города со своими диалогами
- Переход к пустыне видно на горизонте
- **Deliverable:** билд с Ботаникой + городом

### Этап 13. Пустыня + финальный момент
- Megascans desert biome, барханы
- HDRI закат, лёгкий fog
- Финальный объект + твист-текст
- Dune-style эмбиент
- **Deliverable:** полный билд трёх сцен

### Этап 14. Audio pass
- Шаги по разным поверхностям
- Ambient loops по локациям
- UI-звуки диалогов
- Музыкальные переходы
- **Deliverable:** озвученный билд

**Выход из Блока 2:** играбельный Episode 0, 10-15 минут, визуал + аудио.

---

## БЛОК 3. POST-PRODUCTION

### Этап 15. Polish pass
- Главное меню (Начать / Выйти / Credits)
- Subtitle styling
- Fade-transitions
- Баг-фиксы
- Credits screen
- **Deliverable:** полированный билд

### Этап 16. Build & signing
- Финальный Unity build → `<Project>.app`
- Ad-hoc codesign (без Apple Developer ID)
- Инструкция "правый клик → Открыть" для Gatekeeper
- **Deliverable:** подписанный запускаемый `.app`

### Этап 17. DMG-упаковка
- `hdiutil create` → красивый `.dmg`
- Background-картинка с инструкцией install
- Applications-симлинк для drag'n'drop
- **Deliverable:** `<Project>-Episode-0.dmg`

### Этап 18. Хостинг на Contabo
- Заливка `.dmg` на Contabo VPS 30
- nginx location с правильными MIME
- Rate-limiting
- **Deliverable:** `https://timzinin.com/<slug>/download/<Project>.dmg`

### Этап 19. Лендинг
- Одностраничник: тизер, описание, скриншоты, кнопка Download
- Инструкция установки для друзей с Mac
- Contact для фидбэка (TG)
- Deploy на GitHub Pages
- **Deliverable:** `https://timzinin.com/<slug>/`

### Этап 20. End-to-end verify
- Заход на лендинг "со стороны"
- Скачивание, распаковка, установка, запуск, прохождение
- Откат если что-то сломано
- **Deliverable:** подтверждённый рабочий flow

### Этап 21. Notify + архивация
- TG-уведомление со ссылкой
- Запись в `memory/logs/<project>.md`
- Обновление `session_handoff.md`
- Черновик Episode 1
- **Deliverable:** замкнутый цикл, готовность к Episode 1

---

## Deliverable матрица

| # | Этап | Deliverable |
|---|---|---|
| 1 | Вселенная | `UNIVERSE.md` |
| 2 | Сюжет | `STORY.md` |
| 3 | Персонажи | `CHARACTERS.md` + `dataland.ink` |
| 4 | Art bible | `ART_BIBLE.md` |
| 5 | GDD | `GDD.md` |
| 6 | Tech plan | `TECH.md` |
| 7 | Scope lock | `SCOPE.md` |
| 8 | Env ready | smoke build |
| 9 | Ассеты | `Assets/Vendor/` |
| 10 | Walking skeleton | grey playable `.app` |
| 11 | Ботаника | билд с Ботаникой |
| 12 | Город | билд с городом |
| 13 | Пустыня | полный билд |
| 14 | Audio | озвученный билд |
| 15 | Polish | полированный билд |
| 16 | Signed | ad-hoc signed `.app` |
| 17 | DMG | `.dmg` файл |
| 18 | Hosting | download URL |
| 19 | Лендинг | `timzinin.com/<slug>/` |
| 20 | Verify | подтверждённый flow |
| 21 | Notify | сообщение в TG |

## Временной контур

- **Блок 1:** ~2 часа
- **Блок 2:** ~7-9 часов
- **Блок 3:** ~1.5-2 часа
- **Итого:** 10-13 часов

## Роли

**Тим:**
- Решения на Этапах 1-7
- Один раз кликает Unity Hub installer (Этап 8)
- Запускает билды на чекпойнтах (6 раз за день)
- Финальный тест на лендинге

**Claude:**
- Пишет все `docs/*.md` и `.ink`
- Устанавливает Unity через bash
- Скачивает и импортирует ассеты
- Пишет Unity C# код, сцены, prefabs
- Билдит через Unity batchmode CLI
- Упаковывает DMG
- Деплоит на Contabo + nginx
- Собирает лендинг
- Verify end-to-end

## Fallback-план

- Если к 20:00 на Этапах 11-12 → режем до одной сцены (Ботаника + fade-to-black + твист)
- Если к 22:00 → режем polish, шипим без главного меню
- Если что-то критически сломалось → минимальный scope "Ботаника + финальный текст"

## Риски

1. **Unity на M1 8GB тормозит** — митигация: закрывать Chrome/Safari во время работы Editor
2. **Quixel Bridge требует GUI** — один раз клик от Тима
3. **Ink integration может глючить** — fallback на самописную простую систему
4. **Билд не стартует на Mac Тима** — митигация: билдим и тестим уже на Этапе 10
5. **Gatekeeper блокирует unsigned** — инструкция "правый клик → Открыть" в лендинге

---

## Следующий шаг

**Этап 1: Вселенная.** Предложить 3-4 варианта концепции, выбрать, написать `UNIVERSE.md`.
