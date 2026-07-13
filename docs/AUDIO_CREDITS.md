# Audio Credits — docs/ (сводный, для релизных титров)

> Полный технический реестр всех аудио-ассетов (ambient/SFX/старая музыка) —
> `Assets/_Project/Audio/CREDITS.md`. Этот файл — блок по новым ассетам Спринта E
> (сцена «Город», R6), дописывается, не затирает предыдущее содержимое.

## Спринт E — саундтрек «Город» + городской эмбиент (2026-07-11, R6)

Источники — только CC0 / CC-BY, лицензия проверена по первоисточнику перед скачиванием.
Кандидаты лежат в `audio_e/` (вне Unity-проекта, для приёмки G1); после ACCEPT финальный
трек копируется в `Assets/_Project/Audio/Music/` и таблица переносится в
`Assets/_Project/Audio/CREDITS.md`.

| Файл | Трек | Автор | Лицензия | Источник |
|---|---|---|---|---|
| candidate1.ogg | Loss | Kevin MacLeod (incompetech.com) | CC-BY 4.0 — атрибуция обязательна | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Loss.mp3 |
| candidate2.ogg | Bittersweet | Kevin MacLeod (incompetech.com) | CC-BY 4.0 — атрибуция обязательна | https://incompetech.com/music/royalty-free/mp3-royaltyfree/Bittersweet.mp3 |
| candidate3.ogg | String Impromptu Number 1 | Kevin MacLeod (incompetech.com) | CC-BY 4.0 — атрибуция обязательна | https://incompetech.com/music/royalty-free/mp3-royaltyfree/String%20Impromptu%20Number%201.mp3 |
| city_ambient.ogg | wind1 | Luke.RUSTLTD (opengameart.org/users/lukerustltd) | CC0 (Public Domain) | https://opengameart.org/content/wind1 (файл: .../sites/default/files/wind1.wav) |

**Атрибуция для титров игры (CC-BY 4.0, обязательна независимо от того, какой из трёх
кандидатов выберет панель):**

> «{Track Title}» by Kevin MacLeod (incompetech.com), Licensed under Creative Commons:
> By Attribution 4.0. https://creativecommons.org/licenses/by/4.0/

Пример для рекомендованного трека: *«Bittersweet» by Kevin MacLeod (incompetech.com),
Licensed under Creative Commons: By Attribution 4.0.*

`city_ambient.ogg` (CC0) атрибуции не требует, но по практике проекта источник
документируется здесь же.

**Не проверено на слух** — среда сборки без аудиовыхода (как и весь остальной звук проекта,
см. `Assets/_Project/Audio/CREDITS.md`). Выбор основан на 2-pass LUFS-измерении, визуальном
анализе спектрограммы/waveform (ffmpeg `showspectrumpic`/`showwavespic`) и метаданных
исходного файла (genre/album). Прослушать перед финальным ACCEPT.

Полный разбор кандидатов, сравнение и обоснование рекомендации — `audio_e/AUDIO_CANDIDATES.md`.
