# Technical Plan «Послелюди» / Episode 0

> **Документ:** TECH.md — технический стек, архитектура, build pipeline, deployment
> **Версия:** 1.0
> **Дата:** 2026-04-05
> **Статус:** финальный для Episode 0

---

## 1. Технический стек

### Engine
- **Unity 6 LTS** (6000.0.72f1 или выше — whatever актуально на 2026-04-05)
- **Render Pipeline:** URP (Universal Render Pipeline)
- **Scripting backend:** IL2CPP (для production билда), Mono (для dev)
- **API Compatibility:** .NET Standard 2.1

### Платформа
- **Primary target:** macOS Standalone, Apple Silicon (arm64)
- **Secondary target** (stretch): macOS Intel x86_64 (universal binary), если позволит время
- **Не таргетим:** Windows, Linux, WebGL, iOS, Android в Episode 0 (Episode 1+ — возможно)

### Unity модули обязательные
- **Mac Build Support** (Apple Silicon)
- **URP template** (из Unity Hub при создании проекта)
- **Universal RP package** (com.unity.render-pipelines.universal)
- **TextMeshPro** (com.unity.textmeshpro) — для субтитров
- **Cinemachine** (com.unity.cinemachine) — для cinematic camera moments
- **Input System** (com.unity.inputsystem) — современный ввод
- **AI Navigation** (com.unity.ai.navigation) — NavMeshAgent для Кафки
- **Visual Effect Graph** (com.unity.visualeffectgraph) — опционально, для VFX пустыни
- **Shader Graph** (com.unity.shadergraph) — для курсора и кастомных шейдеров

### Unity модули опциональные (stretch)
- **ProBuilder** (com.unity.probuilder) — для быстрого level design
- **Post Processing** (встроен в URP)
- **Timeline** (com.unity.timeline) — для финальных титров

### Сторонние пакеты (бесплатные)
- **Ink Unity Integration** (`inkle/ink-unity-integration`, MIT) — диалоги
- **Unity Starter Assets FirstPerson** (бесплатно, из Asset Store) — FPS контроллер

### НЕ используем
- DOTS / ECS (overkill)
- Addressables (для 15-мин игры не нужно)
- Netcode / Multiplayer
- Analytics SDK от Unity (своё логирование в простой json если нужно)
- Monetization SDK

---

## 2. Структура Unity-проекта

```
~/afterhumans/
├── docs/                    ← markdown документы (UNIVERSE, STORY, etc)
├── Assets/                  ← сюда идёт всё содержимое Unity проекта
│   ├── _Project/            ← свой код и ассеты проекта (префикс _ чтобы был сверху)
│   │   ├── Scripts/
│   │   │   ├── Player/
│   │   │   │   ├── PlayerController.cs
│   │   │   │   └── PlayerInteraction.cs
│   │   │   ├── Dialogue/
│   │   │   │   ├── DialogueManager.cs
│   │   │   │   ├── DialogueUI.cs
│   │   │   │   └── Interactable.cs
│   │   │   ├── Kafka/
│   │   │   │   ├── KafkaFollow.cs
│   │   │   │   ├── KafkaReactions.cs
│   │   │   │   └── KafkaSpecialEvents.cs
│   │   │   ├── Scenes/
│   │   │   │   ├── SceneTransition.cs
│   │   │   │   └── DoorToCity.cs
│   │   │   ├── Cursor/
│   │   │   │   └── CursorFinale.cs
│   │   │   ├── Audio/
│   │   │   │   └── AmbientController.cs
│   │   │   └── UI/
│   │   │       ├── MainMenu.cs
│   │   │       └── SettingsMenu.cs
│   │   ├── Scenes/
│   │   │   ├── Scene_MainMenu.unity
│   │   │   ├── Scene_Botanika.unity
│   │   │   ├── Scene_City.unity
│   │   │   ├── Scene_Desert.unity
│   │   │   └── Scene_Credits.unity
│   │   ├── Prefabs/
│   │   │   ├── Player.prefab
│   │   │   ├── Kafka.prefab
│   │   │   ├── NPC_template.prefab
│   │   │   ├── DialogueUI.prefab
│   │   │   └── Cursor.prefab
│   │   ├── Materials/
│   │   │   └── (custom URP materials)
│   │   ├── Shaders/
│   │   │   ├── Cursor.shadergraph
│   │   │   └── Glitch.shadergraph
│   │   ├── Audio/
│   │   │   ├── Music/
│   │   │   │   ├── botanika_ambient.ogg
│   │   │   │   ├── city_drone.ogg
│   │   │   │   └── desert_cinematic.ogg
│   │   │   └── SFX/
│   │   │       ├── footsteps_*
│   │   │       ├── kafka_*
│   │   │       ├── ui_*
│   │   │       └── cursor_*
│   │   ├── Dialogues/
│   │   │   ├── dataland.ink          ← source
│   │   │   └── dataland.json         ← auto-generated compiled
│   │   ├── UI/
│   │   │   ├── Fonts/
│   │   │   └── Sprites/
│   │   └── Settings/
│   │       ├── URP_Pipeline_Asset.asset
│   │       ├── VolumeProfile_Botanika.asset
│   │       ├── VolumeProfile_City.asset
│   │       └── VolumeProfile_Desert.asset
│   │
│   └── Vendor/               ← сторонние ассеты, НЕ свой код
│       ├── Megascans/        ← Quixel ассеты
│       ├── Kenney/           ← Kenney паки
│       ├── Quaternius/       ← Quaternius паки
│       ├── PolyHaven/        ← HDRI и текстуры
│       ├── Mixamo/           ← персонажи и анимации
│       ├── StarterAssets/    ← Unity Starter Assets FirstPerson
│       └── Ink/              ← ink-unity-integration
│
├── unity-project/            ← ПУСТОЙ в начале. После Блока 2 здесь будет Unity project structure (ProjectSettings, Packages, Library etc)
│                              (Assets/ живёт на одном уровне выше для удобства git)
├── build/                    ← .app и .dmg финальные билды (в .gitignore, только для локального)
├── .gitignore                ← Unity-specific ignore
└── README.md                 ← overview
```

**ВАЖНО о git layout:**
- Unity project по стандарту имеет `Assets/`, `Packages/`, `ProjectSettings/`, `Library/`, `Temp/`, и т.д. в ОДНОЙ директории — корень проекта.
- Но мы держим `Assets/` и `docs/` в корне репо `afterhumans`. Unity создаст свои служебные папки рядом.
- `unity-project/` в начальной структуре — это заготовка, реально Unity откроется с корневой директорией `~/afterhumans/`. Служебные папки `Library/`, `Temp/`, `Logs/` будут в `.gitignore`.

---

## 3. Архитектура кода

### Основные managers (singleton'ы)

#### `DialogueManager.cs`
```csharp
// Singleton, DontDestroyOnLoad
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance;
    public Story story; // Ink story object
    
    public void Load(TextAsset inkJson) { /* загрузить .json */ }
    public void StartKnot(string knotName) { /* story.ChoosePathString(knotName) */ }
    public void ContinueStory() { /* story.Continue() */ }
    public void ChooseChoice(int index) { /* story.ChooseChoiceIndex(index) */ }
    public bool GetVariable(string name) { /* return (bool)story.variablesState[name] */ }
    public void SetVariable(string name, object value) { /* story.variablesState[name] = value */ }
}
```

#### `GameStateManager.cs`
```csharp
// Управляет глобальным состоянием, Save/Load
public class GameStateManager : MonoBehaviour
{
    public static GameStateManager Instance;
    public string currentScene;
    public int cursorInput; // 0 = empty, 1-5 = endings
    
    public void SaveGame() { /* JSON в persistentDataPath */ }
    public void LoadGame() { /* восстановление */ }
    public void ResetGame() { /* начать заново */ }
}
```

#### `SceneTransitionManager.cs`
```csharp
// Плавные переходы с fade
public class SceneTransitionManager : MonoBehaviour
{
    public static SceneTransitionManager Instance;
    public Image fadeOverlay; // полноэкранный UI Image
    
    public IEnumerator FadeToScene(string sceneName) { /* fade to black + load + fade from black */ }
}
```

### Player

#### `PlayerController.cs`
- Обёртка над Unity Starter Assets FirstPerson
- Настройки скорости, чувствительности, head bob
- Передача ввода из Input System

#### `PlayerInteraction.cs`
- Raycast из camera forward на дистанцию 2m
- Поиск объектов с компонентом `Interactable`
- Показ prompt UI
- По E → `Interactable.Interact()`

### NPC & Interaction

#### `Interactable.cs`
- MonoBehaviour на каждом NPC, записке, Кафке, курсоре, двери
- Поля: knotName, promptText, interactRadius, oneTime
- Метод `Interact()` → вызывает `DialogueManager.Instance.StartKnot(knotName)`

#### `NpcAnimator.cs`
- На каждом NPC с Mixamo-анимациями
- Простой state machine: Idle / Talking / Custom (для Анны: Sad, для Стаса: Nervous)
- Переключение по событиям Dialogue Manager

### Kafka

#### `KafkaFollow.cs` (см. Раздел 7)
#### `KafkaReactions.cs` (см. Раздел 7)
#### `KafkaSpecialEvents.cs` (см. Раздел 7)

### Cursor finale

#### `CursorFinale.cs`
- Обнаруживает подход игрока
- Triggers zoom-in FOV effect
- Показывает финальные 5 вариантов через DialogueUI
- После выбора → загружает Scene_Credits

### Audio

#### `AmbientController.cs`
- Attached к AudioSource на сцене
- Crossfade между треками при переходах
- Reverb zones для разных частей Ботаники (опционально)

---

## 4. Input System

Используем **new Input System** (com.unity.inputsystem), не legacy.

### Actions asset `PlayerInput.inputactions`

| Action | Binding |
|---|---|
| Move | WASD (2D Vector) |
| Look | Mouse Delta (2D) |
| Interact | E |
| Sprint | Shift |
| SkipText | Space or E |
| SelectChoice1-5 | 1-5 digits |
| Pause | Escape |

### Action Maps
- `Player` — во время свободного движения
- `Dialogue` — когда открыт диалог
- `Menu` — когда открыто меню

Переключение через `PlayerInput.SwitchCurrentActionMap("Dialogue")` при старте диалога и обратно по окончании.

---

## 5. Build pipeline

### Dev билды (в процессе разработки)
- Unity Editor → File → Build Settings → Target: macOS
- Architecture: Apple Silicon
- Scripting Backend: **Mono** (быстрый билд)
- Development Build: ✅ enabled
- Compression: None (не ждём)
- Размер папки билда: ~300-500 MB (с ассетами)

### Production билд (финальный перед релизом)
- Scripting Backend: **IL2CPP** (быстрее, меньше размером)
- Development Build: ❌ disabled
- Compression: LZ4HC (максимальная компрессия)
- Strip engine code: enabled
- Managed stripping level: Medium
- Размер ожидаемый: 200-400 MB

### Сборочный скрипт (можно автоматизировать)
```bash
# build.sh
UNITY_APP="/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity"
PROJECT_PATH="$HOME/afterhumans"
BUILD_PATH="$HOME/afterhumans/build/Afterhumans.app"

"$UNITY_APP" \
  -batchmode \
  -nographics \
  -quit \
  -projectPath "$PROJECT_PATH" \
  -buildTarget StandaloneOSX \
  -executeMethod BuildScript.BuildMacOS \
  -customBuildPath "$BUILD_PATH" \
  -logFile "$HOME/afterhumans/build/build.log"
```

`BuildScript.BuildMacOS` — статический метод в C# в `Assets/_Project/Editor/BuildScript.cs`, который настраивает BuildPlayerOptions и вызывает BuildPipeline.BuildPlayer.

---

## 6. Code signing & DMG packaging

### Ad-hoc codesign (без Apple Developer ID)
```bash
codesign --sign - --deep --force --options runtime "build/Afterhumans.app"
```

Это создаёт **ad-hoc подпись** (без сертификата Apple). macOS всё ещё предупредит при первом запуске ("app from unidentified developer"), но:
1. Пользователь может правый клик → Open → Open в диалоге
2. После первого открытия приложение будет запускаться свободно

В лендинге добавим инструкцию для friends: "правый клик → Open → Open в диалоге".

### DMG packaging
```bash
# Простой способ через hdiutil
hdiutil create \
  -volname "Afterhumans Episode 0" \
  -srcfolder "build/Afterhumans.app" \
  -ov -format UDZO \
  "build/Afterhumans-Episode-0.dmg"
```

**Опционально (если время позволит):** красивый DMG с background-картинкой и Applications-симлинком через `create-dmg` (brew install create-dmg).

```bash
create-dmg \
  --volname "Afterhumans Episode 0" \
  --background "assets/dmg-background.png" \
  --window-pos 200 120 --window-size 600 400 \
  --icon-size 100 \
  --icon "Afterhumans.app" 150 190 \
  --hide-extension "Afterhumans.app" \
  --app-drop-link 450 190 \
  "build/Afterhumans-Episode-0.dmg" \
  "build/Afterhumans.app"
```

### Размер финального DMG
- Ожидаемый: 200-500 MB (зависит от Megascans)
- Максимум: 1 GB (не превышать — друзьям качать долго)

---

## 7. Hosting

### Лендинг
- **URL:** `https://timzinin.com/afterhumans/`
- **Хостинг:** GitHub Pages (бесплатно, через `TimmyZinin/afterhumans` репо)
- **Файлы:** `docs/landing/index.html` + css + изображения + скриншоты
- **Deploy:** GitHub Actions workflow при push в main

### DMG hosting
- **GitHub Pages НЕ ПОДХОДИТ** для файлов >100 MB
- **Contabo VPS 30** через nginx — отдельный location для раздачи файлов
- **URL:** `https://timzinin.com/afterhumans/download/Afterhumans-Episode-0.dmg`
- **Путь на сервере:** `/var/www/afterhumans/downloads/`
- **nginx config:**
  ```nginx
  location /afterhumans/download/ {
    alias /var/www/afterhumans/downloads/;
    add_header Content-Disposition "attachment";
    limit_rate 10m;  # ограничение скорости для экономии трафика (10 MB/s на клиента)
  }
  ```
- **SSL:** уже настроен (Let's Encrypt на `timzinin.com`)

### Upload на Contabo
```bash
scp build/Afterhumans-Episode-0.dmg root@185.202.239.165:/var/www/afterhumans/downloads/
ssh root@185.202.239.165 'chmod 644 /var/www/afterhumans/downloads/Afterhumans-Episode-0.dmg'
```

### Альтернатива (fallback)
Если Contabo недоступен или DMG >500 MB — можно положить на Google Drive / Dropbox / MEGA и поставить ссылку на лендинге. Но предпочтительно Contabo, потому что это наш контроль и мы знаем что работает.

---

## 8. Лендинг `timzinin.com/afterhumans/`

### Структура
```html
<html>
  <head>
    <title>Послелюди / Afterhumans — Episode 0</title>
    <meta og tags />
  </head>
  <body>
    <!-- Hero -->
    <section class="hero">
      <h1>Послелюди</h1>
      <h2>Episode 0</h2>
      <p>Narrative walker от первого лица. 10-15 минут. Для Mac. Бесплатно.</p>
      <a href="download/Afterhumans-Episode-0.dmg" class="download-btn">Скачать (XXX MB)</a>
    </section>
    
    <!-- Trailer video (если будет) -->
    <section class="trailer">
      <video src="trailer.mp4" controls></video>
    </section>
    
    <!-- Описание -->
    <section class="about">
      <p>Мир управляется алгоритмом. Ботаника — баг. Ты — первый тест-кейс.</p>
      <p>С тобой — Кафка, чёрно-белая корги-кардиган, 6 лет.</p>
    </section>
    
    <!-- Скриншоты -->
    <section class="screenshots">
      <img src="screens/01.jpg">
      <img src="screens/02.jpg">
      <img src="screens/03.jpg">
    </section>
    
    <!-- Установка -->
    <section class="install">
      <h3>Как установить</h3>
      <ol>
        <li>Скачай .dmg</li>
        <li>Открой, перетащи приложение в Applications</li>
        <li>При первом запуске: правый клик → Открыть → Открыть в диалоге</li>
        <li>Играй</li>
      </ol>
    </section>
    
    <!-- Контакт -->
    <section class="contact">
      <p>Фидбэк и баги: <a href="https://t.me/timofeyzinin">@timofeyzinin</a></p>
    </section>
  </body>
</html>
```

### Стиль лендинга
Dune-inspired: тёплые оранжевые тона, крупный шрифт, минимум элементов. Главное — кнопка *Скачать*.

---

## 9. Логирование и диагностика

По правилам CLAUDE.md — каждый продукт должен иметь логирование.

### In-game logs
- Simple console logs через `Debug.Log` в development
- В production билде — отключены по умолчанию
- При необходимости — `Application.logMessageReceived` event → запись в файл `Application.persistentDataPath/game.log`

### Что логируем
- Переходы между сценами
- Ошибки (exception)
- Starts/completions диалогов
- Финальный выбор у Курсора (для аналитики какой вариант популярнее)

### Что НЕ логируем
- Персональные данные (их нет, игра offline)
- Трекинг действий игрока (нет смысла для offline-игры)

### Простая аналитика (stretch)
Если время позволит — endpoint на Contabo принимает POST от игры с финальным выбором. Игра шлёт один HTTP request в финале: `POST https://timzinin.com/afterhumans/api/ending { "ending": "unknown" }`. Это даст знать какой финал выбирают чаще. Полностью анонимно.

---

## 10. Версионирование

### Semantic versioning
- **Episode 0 v0.1.0** — первый публичный релиз (today)
- **Episode 0 v0.1.1** — багфикс hotfix (если нужен)
- **Episode 0 v0.2.0** — post-release патч с доп. polish
- **Episode 1 v0.3.0** — следующий эпизод

### Git tags
```bash
git tag -a v0.1.0 -m "Episode 0 first public release"
git push --tags
```

### Build versioning
В `PlayerSettings`:
- Version: `0.1.0`
- Build: инкрементируется при каждом билде (автоскрипт)
- Bundle Version: `0.1.0` (macOS CFBundleShortVersionString)

---

## 11. Continuous workflow

### Разработка в Блоке 2
```
утро:   Unity Editor открыт, работаем над сценой X
полдень: коммит прогресса, git push
вечер:  билд → локальный тест → коммит билда в release branch
ночь:   если готово — деплой на Contabo + TG notify Тиму
```

### Каждый тестовый билд:
1. `Cmd+B` в Unity (или через скрипт)
2. Запуск `.app` локально на Mac Тима
3. Прохождение ключевых сцен (Ботаника → Город → Пустыня → Курсор)
4. Фидбэк Тима или самопроверка Claude
5. Итерация

---

## 12. Риски и митигация

| Риск | Вероятность | Митигация |
|---|---|---|
| Unity крашится на M1 8GB | Средняя | Закрывать фоновые приложения, minimal lighting bakes, очистка Library при проблемах |
| Ink integration не компилируется | Низкая | Fallback — самописная диалоговая система на C# (~4 часа работы) |
| Кафка-ассет не находится бесплатно | Средняя | Fallback — обычная low-poly dog модель, перекраска в шейдере |
| Билд слишком большой (>1 GB) | Средняя | Texture compression, LOD, cut из Megascans избыточного, убрать неиспользуемые шейдеры |
| Ad-hoc sign не пропускает Gatekeeper | Низкая | Инструкция "правый клик → Open" в лендинге |
| Contabo nginx не раздаёт .dmg | Низкая | Проверено в прошлых проектах, должно работать сходу |
| Time overrun | Высокая | Fallback scopes в SCOPE.md |

---

## 13. Dev environment checklist

Перед началом Блока 2 (установка Unity) проверить:
- [x] Mac M1 2020, 8 GB RAM
- [x] macOS Tahoe 26.3.1 — совместим с Unity 6 LTS
- [ ] Свободное место на диске: нужно >80 GB (Unity + Megascans + проект)
- [ ] Xcode Command Line Tools: проверить `xcode-select --install`
- [ ] Homebrew: установить если нет (для git-lfs, create-dmg)
- [ ] git-lfs: `brew install git-lfs`
- [ ] Unity Hub: скачать с unity.com
- [ ] Unity 6 LTS + Mac Build Support: через Hub
- [ ] Бесплатный Epic account для Quixel Megascans
- [ ] Адрес почты для Mixamo (бесплатно)

---

**Статус документа:** финальный для Episode 0.

**Следующий документ:** `SCOPE.md` (Этап 7).
