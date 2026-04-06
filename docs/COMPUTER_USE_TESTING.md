# Computer Use тестирование Afterhumans

## Запуск игры

```bash
# Убить предыдущий инстанс если есть
pkill -f "Послелюди" 2>/dev/null; sleep 1

# Запустить
open ~/afterhumans/build/Afterhumans.app

# Подождать загрузку
sleep 3
```

Затем `request_access` к приложению "Послелюди".

## Ограничения (из первого playtest)

1. **Cursor lock** — Unity захватывает мышь. Mouse look через Computer Use невозможен. Но WASD и E работают через `hold_key`/`key`.
2. **Fullscreen** — билд теперь windowed 1280x720 (фикс Sprint 13). Screencapture должен работать.
3. **Кинематик 18с** — первые 18 секунд камера автоматическая. Не трогать клавиши. Делать screenshot каждые 5с.

## Тест-план

### Фаза 1: Кинематик (0-20с)
```
wait 3       # app loading
screenshot   # должна быть видна сцена
wait 5
screenshot   # камера должна переместиться
wait 5
screenshot   # другой ракурс
wait 8
screenshot   # конец кинематика, видна комната
```

### Фаза 2: Движение (20-40с)
```
hold_key "w" 2    # вперёд 2 секунды
screenshot         # должна быть другая позиция
hold_key "a" 1    # влево
screenshot
hold_key "d" 2    # вправо
screenshot
hold_key "w" 3    # вперёд к дивану (Саша)
screenshot         # должен появиться "[E] говорить"
```

### Фаза 3: Диалог (40-90с)
```
key "e"            # открыть диалог
wait 1
screenshot         # dialogue panel должен быть видн
key "e"            # skip typewriter / advance
wait 1
screenshot         # следующая строка
key "e" repeat:10  # прокликать диалог до конца
wait 1
screenshot         # dialogue panel скрылся
```

### Фаза 4: Другие NPC
Повторить движение + E для каждого NPC. Их позиции:
- Саша: прямо по ходу от спавна (0, 0, 4)
- Мила: слева (-3.5, 0, 2)
- Кирилл: справа (3.5, 0, 2)
- Николай: дальний левый угол (-4.5, 0, 4.5)
- Стас: позади у двери (0, 0, -3.5)

### Фаза 5: Gate check
После диалога с Николаем должен появиться текст "Дверь открыта. Ты можешь идти." внизу экрана. Screenshot.

## Как пересобрать после фикса

```bash
UNITY="$HOME/Applications/Unity/Hub/Editor/6000.0.72f1/Unity.app/Contents/MacOS/Unity"

# Пересобрать сцену
$UNITY -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BotanikaDresser.Dress \
  -logFile /tmp/dress.log 2>&1
grep "DONE" /tmp/dress.log

# Проверить 23/23
$UNITY -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BotanikaVerification.RunAll \
  -logFile /tmp/verify.log 2>&1
grep "passed" /tmp/verify.log

# Собрать .app
$UNITY -batchmode -quit -projectPath ~/afterhumans \
  -executeMethod Afterhumans.EditorTools.BuildScript.BuildMacOS \
  -logFile /tmp/build.log 2>&1
grep "SUCCEEDED" /tmp/build.log

# Запустить
pkill -f "Послелюди" 2>/dev/null; sleep 1
open ~/afterhumans/build/Afterhumans.app
```

## Формат бага

```
### BUG-XX: Название
- **Severity:** CRITICAL / HIGH / MEDIUM / LOW
- **Шаг:** что делал
- **Ожидание:** что должно было произойти (из STORY.md / ART_BIBLE.md)
- **Реальность:** что произошло (+ screenshot)
- **Файл:** какой скрипт чинить
```

## Как запустить mm-review

После фикса и коммита:
```
/mm-review Sprint N — описание фиксов
```
MiniMax M2.5 проверит код и даст APPROVED или NEEDS_CHANGES.
