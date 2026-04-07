# План: Kafka — Welsh Corgi Cardigan 3D модель

## Источник правды
- `docs/CHARACTERS.md` §10: порода, окрас, характер, реакции
- `docs/ART_BIBLE.md` §7: анимации, rigging requirements

## Референсы

### Порода: Welsh Corgi Cardigan (НЕ Pembroke!)
- **AKC Breed Standard:** [Official Standard](https://images.akc.org/pdf/breeds/standards/Cardigan_Welsh_Corgi.pdf)
- **Dimensions.com:** [Corgi Measurements](https://www.dimensions.com/element/cardigan-welsh-corgi)
- **Key difference от Pembroke:** Cardigan имеет ПОЛНЫЙ ХВОСТ (Pembroke — купированный)

### Пропорции (breed standard)
| Параметр | Значение |
|----------|----------|
| Высота в холке | 26.5-31.5 см (10.5-12.5 inches) |
| Вес | 11-17 кг (25-38 lbs) |
| Длина/высота ratio | **1.8:1** |
| Морда/череп | 3:5 |
| Хвост | полный, лисий, низко посажен |
| Уши | большие, стоячие, закруглённые на концах |
| Ноги | короткие, слегка изогнутые |
| Грудь | глубокая, опущена ниже локтей |

### Окрас (из CHARACTERS.md)
> «Чёрно-белый (глубокий чёрный + яркий белый, без пятен рыжего)»

Распределение цветов Welsh Corgi Cardigan black & white:
- **Чёрный:** спина, бока, верх головы, уши, верхняя часть хвоста
- **Белый:** грудь/манишка, нижняя часть морды, блейз (полоска между глаз), лапы, кончик хвоста, живот
- **Нос:** чёрный, влажный блеск
- **Глаза:** тёмно-карие

### Существующие 3D модели (референсы качества)
- [BlenderKit: Corgi low poly](https://www.blenderkit.com/get-blenderkit/d72dfdca-04c1-4619-99e0-9bda4a67300b/) — FREE, low-poly стиль
- [Studio Ochi: Low Poly Corgi](https://studioochi.com/product/3d-low-poly-corgi-dog/) — 580 vertices, 599 faces
- [BlendSwap: Low Poly Corgi](https://www.blendswap.com/blend/15391) — FREE
- [FlippedNormals: Corgi Animated](https://flippednormals.com/product/corgi-animated-377) — с анимациями (платный, для референса)
- [ArtStation: Corgi Walk Cycle](https://www.artstation.com/artwork/xzklYE) — анимация референс

---

## Спецификация модели

### Стиль
**Stylized-realistic** (Art Bible): между Sable и Death Stranding. Не фотореализм, не cartoon. Узнаваемая корги с упрощённой детализацией и выраженным силуэтом.

### Технические параметры
| Параметр | Значение | Обоснование |
|----------|----------|-------------|
| Poly count | 800-1500 triangles | Game-ready для M1 8GB, достаточно для узнаваемости |
| Высота модели | ~0.30m (30cm) | Breed standard |
| Длина модели | ~0.55m (55cm) | Ratio 1.8:1 |
| Текстуры | Vertex color или 512x512 | Для Kenney-совместимого стиля |
| Формат | FBX (Unity) | Из Blender через export script |

### Детальная анатомия

#### Тело
- Длинный торс (ratio 1.8:1)
- Глубокая грудь (ниже локтей)
- Слегка сужается к задней части
- Smooth shading для органичного вида
- Спина ровная, не провисает

#### Голова
- Пропорция морда:череп = 3:5
- Широкая между ушами
- Морда сужается к носу, но не заострённая
- Чёткий стоп (переход лоб→нос)

#### Уши
- БОЛЬШИЕ (signature corgi feature!)
- Стоячие, слегка закруглённые на концах
- Расставлены широко
- Угол ~15° наружу от вертикали

#### Глаза
- Тёмно-карие
- Среднего размера
- Миндалевидные
- Расставлены широко

#### Нос
- Чёрный
- Влажный (высокий smoothness/specular)
- Небольшой, не выдающийся

#### Морда/рот
- Белая нижняя часть (маркинг)
- Блейз (белая полоска) между глаз вверх на лоб
- Щёки слегка округлые

#### Лапы (4 шт)
- КОРОТКИЕ (defining feature!)
- Передние слегка изогнуты наружу
- Задние прямее
- Лапы компактные, округлые
- Белые (маркинг)

#### Хвост
- **ПОЛНЫЙ** (Cardigan, не Pembroke!)
- Лисий тип (пушистый)
- Низко посажен
- В покое — свободно опущен
- При движении — поднят до уровня спины
- Белый кончик

#### Шерсть
- В low-poly: через геометрию (не particles)
- Средней длины, плотная
- Лёгкий пушок на груди и задней части ног
- Для stylized: достаточно smooth shading + vertex color

### Распределение окраса

```
ВИД СВЕРХУ:
          ██ уши ██
        ████████████
       ██████████████  ← чёрный верх
      ████████████████
     ██████████████████
      ████████████████
        ██████████████
          ████ хвост (чёрный + белый кончик)

ВИД СПЕРЕДИ:
        ██ ██         ← уши (чёрные)
       ████████       ← лоб (чёрный)
      ███░░███        ← блейз (белая полоска)
      ██ ●● ██        ← глаза (тёмные)
       ░░██░░         ← морда (белая + чёрный нос)
       ░░░░░░         ← грудь/манишка (белая)
      ██░░░░██        ← бока (чёрные) + грудь (белая)
      ░░░░░░░░        ← лапы (белые)
```

---

## Анимации (план)

### Приоритет 1 (для Ботаники)
| Анимация | Описание | Frames |
|----------|----------|--------|
| **idle** | Стоит, слегка дышит, иногда поворачивает голову | loop 60f |
| **walk** | Бег/ходьба за игроком | loop 24f |
| **tail_wag** | Хвост виляет (overlay) | loop 12f |

### Приоритет 2 (для полного gameplay)
| Анимация | Описание |
|----------|----------|
| sit | Садится |
| bark | Короткий лай |
| sniff | Нюхает землю |
| lie_down | Ложится |

### Реализация анимаций
Два варианта:
1. **Процедурные** (как сейчас — KafkaIdleAnimation.cs): breathing, tail wag, ear twitch через код
2. **Blender armature** → export с анимациями → Unity Animator Controller

Для текущей итерации: процедурные (уже работают). Blender armature = следующий спринт.

---

## Pipeline создания

### Шаг 1: Blender модель
Использовать скилл `3d-artist` + Blender CLI:
```
Claude Code (скилл 3d-artist) → Blender Python script → модель
```
- Модель из mesh primitives (cube → subdivide → sculpt)
- Vertex colors для окраса (чёрный/белый)
- Smooth shading
- ~1000 triangles target

### Шаг 2: Масштаб
- Высота 0.30m в Blender (реальный масштаб корги)
- Export scale 1.0 → Unity import
- В сцене: модель на полу рядом с игроком

### Шаг 3: Export FBX
```bash
blender -b kafka.blend --python export_fbx.py
```
- FBX с embedded materials
- Axis: -Z forward, Y up (Unity convention)

### Шаг 4: Unity интеграция
- Import FBX в `Assets/_Project/Models/kafka_corgi.fbx`
- BotanikaBuilder.SetupKafka() → PlaceFbx с правильным scale
- KafkaFollowSimple.cs — уже работает
- KafkaIdleAnimation.cs — уже работает

### Шаг 5: Тестирование
- Тим запускает билд
- Вопросы: похожа на корги? правильный размер? бегает за мной?

---

## Проблемы текущей модели (v1)

1. **ОГРОМНЫЙ размер** — scale не настроен, собака больше игрока
2. **Примитивная геометрия** — сферы и цилиндры, не smooth mesh
3. **Нет vertex colors** — весь mesh одного материала, а не чёрно-белый
4. **Rotation** — Euler(-90,0,0) для Blender→Unity, но может быть неточно

## Фиксы для следующей итерации

1. **Scale:** модель 0.30m высотой в Blender, scale 1.0 при export
2. **Mesh:** single mesh с subdivision вместо joined primitives
3. **Окрас:** vertex painting (чёрный + белый) или два материала
4. **Пропорции:** ratio 1.8:1, короткие ноги, большие уши, полный хвост
5. **Smooth shading:** proper normals, не faceted

---

## Финальный чеклист перед реализацией

- [ ] Модель ~1000 triangles
- [ ] Высота 0.30m
- [ ] Ratio длина/высота 1.8:1
- [ ] Чёрно-белый окрас (vertex color или 2 материала)
- [ ] Большие уши (стоячие, закруглённые)
- [ ] Полный хвост (Cardigan!)
- [ ] Короткие ноги
- [ ] Глубокая грудь
- [ ] Блейз (белая полоска на морде)
- [ ] Белые лапы, грудь, кончик хвоста
- [ ] Smooth shading
- [ ] FBX export → Unity import работает
- [ ] Правильный масштаб в сцене
- [ ] KafkaFollowSimple работает
- [ ] Бегает за игроком

Sources:
- [AKC Cardigan Welsh Corgi Standard](https://images.akc.org/pdf/breeds/standards/Cardigan_Welsh_Corgi.pdf)
- [Dimensions.com: Cardigan Welsh Corgi](https://www.dimensions.com/element/cardigan-welsh-corgi)
- [BlenderKit: Corgi Low Poly](https://www.blenderkit.com/get-blenderkit/d72dfdca-04c1-4619-99e0-9bda4a67300b/)
- [Studio Ochi: Low Poly Corgi (580 verts)](https://studioochi.com/product/3d-low-poly-corgi-dog/)
- [BlendSwap: Low Poly Corgi](https://www.blendswap.com/blend/15391)
- [FlippedNormals: Corgi Animated](https://flippednormals.com/product/corgi-animated-377)
- [ArtStation: Corgi Walk Cycle](https://www.artstation.com/artwork/xzklYE)
- [Blender Studio: Project DogWalk Animations](https://studio.blender.org/blog/animations-for-dogwalk/)
- [Blender Low Poly Dog Tutorial](https://astropad.com/blog/how-to-model-a-low-poly-dog-in-blender-3-0/)
