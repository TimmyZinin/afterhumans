# POSTPRODUCTION — отложенные этапы

**Статус:** отложено по решению Тима 2026-04-05 ~21:40.
**Причина:** прежде чем думать о хостинге и публикации, нужно определить общее направление игры через аудит + expert panel + план доработок. Публиковать «набор кубов» не имеет смысла.

Вернёмся к этим этапам **после** того как визуал Episode 0 достигнет выбранного Art Direction (атмосферный полуреалистичный, референсы Firewatch + Journey).

---

## Этап 18 — Хостинг DMG на Contabo VPS 30

**Что делать когда вернёмся:**
- Патчить `/etc/nginx/sites-available/timzinin.com` — добавить `location /afterhumans/` до GitHub Pages fallback
- `alias /var/www/afterhumans/` для статики
- Отдельный `location = /afterhumans/Послелюди.dmg` с `Content-Disposition: attachment; filename="Afterhumans.dmg"` (Latin fallback filename для браузеров)
- `nginx -t && systemctl reload nginx`
- Проверка через `curl -sI https://timzinin.com/afterhumans/`

**Что уже готово:**
- `scripts/make-dmg.sh` — packaging через `hdiutil create -format UDZO`. Работает, проверено: ~42 MB, sha256 `7853629d2f1aa86e195accb882448d4aa5f118e6e4d9b77f3969e12ce81c71c8`
- Директория `/var/www/afterhumans/` на Contabo 185.202.239.165 создана, файлы текущей версии уже залиты через `scp`, но **nginx location не патчился** — URL ещё не публичный, безопасно обновить позже

**Заготовка nginx patch:** `/tmp/afterhumans-nginx-patch.py` (локально). Идемпотентный, делает backup.

---

## Этап 19 — Landing `timzinin.com/afterhumans/`

**Что делать когда вернёмся:**
- Пересобрать HTML с актуальными скриншотами (сцены полуреалистичные Firewatch-style, не Kenney Minecraft)
- Hero desert закат + «Начать прохождение» CTA + 3 скриншота сцен + install инструкция + Gatekeeper warning + GitHub link
- Использовать `reference_premium_web_design` (Awwwards-уровень, grain, OKLCH, fluid typography)
- Скопировать через scp → `/var/www/afterhumans/index.html`

**Что уже готово:**
- Черновик HTML в `/tmp/afterhumans-landing/index.html` (~11 КБ) — Firewatch-стиль, OKLCH palette warm, grain overlay, sticky CTA. **Использовать только как референс** — визуальное содержимое устарело после switch art direction.

---

## Этап 20 — End-to-end verify

**Что делать когда вернёмся:**
- `curl -sI` на landing URL + Playwright WebFetch render check
- Автоматическое скачивание DMG через curl
- `hdiutil attach` + копирование `.app` + `xattr -cr` + `open` + ручной walkthrough от Меню до Финала
- Проверка Ink gate (не пропускает City без Николая)
- Проверка DialogueUI показа на каждом из 5 NPC
- Финальный `tg-communicator notify` с download link

**Критерии PASS:**
1. Landing URL → HTTP 200, рендерится, кнопка Download кликабельна
2. DMG скачивается, открывается, .app запускается, Main Menu показан
3. Полный проход `MainMenu → Botanika → City → Desert → Credits` без крашей
4. Диалоги с 5 NPC работают, выборы прописываются в Ink state
5. Final cursor input сохраняется в GameState и влияет на credits text

---

## Когда возвращаемся

Возвращаемся когда:
- Визуал достигает выбранного Art Direction (3 сцены смотрятся как атмосферный полуреалистичный walker)
- Персонажи видны и интерактивны (не placeholder cubes)
- Кафка имеет 3D mesh (хотя бы Quaternius corgi или аналог)
- Ink gate работает (можно не пропустить City без Николая)
- Dialogue UI показывает все 5 персонажей корректно
- Expert Panel одобрила финальный look

**Триггер пересмотра постпродакшена:** Тим явно говорит «пора шипать» после визуальной проверки.
