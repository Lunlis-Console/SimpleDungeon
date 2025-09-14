# Интеграция расширенной системы квестов

## Что было создано

### 1. Новые классы и компоненты

#### Основные модели квестов:
- `EnhancedQuest` - расширенная модель квеста с поддержкой различных условий
- `QuestCondition` (базовый класс) - абстрактный класс для условий квестов
- `CollectItemsCondition` - условие сбора предметов
- `KillMonstersCondition` - условие убийства монстров
- `VisitLocationCondition` - условие посещения локации
- `TalkToNPCCondition` - условие разговора с NPC
- `ReachLevelCondition` - условие достижения уровня

#### Система управления:
- `QuestManager` - менеджер для загрузки и управления квестами
- `QuestDialogueManager` - менеджер для интеграции квестов с диалогами
- `QuestConditionConverter` - конвертер для JSON сериализации условий

#### UI компоненты:
- `EnhancedQuestLogScreen` - обновленный экран журнала квестов
- `EditEnhancedQuestForm` - форма редактирования квестов в JsonEditor
- `EditQuestConditionForm` - форма редактирования условий квестов
- `EditRewardItemForm` - форма редактирования предметов-наград
- `SelectQuestForm` - форма выбора квестов

### 2. Файлы данных

- `SimpleDungeon/Data/quests.json` - пример файла с квестами
- `docs/QuestSystem_README.md` - подробная документация
- `docs/QuestSystem_Integration.md` - инструкции по интеграции

## Интеграция с игрой

### 1. Обновление GameServices

В `GameServices.cs` добавлен `QuestManager`:

```csharp
public static QuestManager QuestManager
{
    get
    {
        if (_questManager == null && CurrentPlayer != null)
        {
            _questManager = new QuestManager(WorldRepository, CurrentPlayer.QuestLog);
            string questsPath = Path.Combine("Data", "quests.json");
            if (File.Exists(questsPath))
            {
                _questManager.LoadQuests(questsPath);
            }
        }
        return _questManager;
    }
    set => _questManager = value;
}
```

### 2. Обновление QuestLog

В `QuestLog.cs` добавлена поддержка новой системы:

```csharp
// Новая система квестов
public List<EnhancedQuest> EnhancedActiveQuests { get; set; }
public List<EnhancedQuest> EnhancedCompletedQuests { get; set; }
public List<EnhancedQuest> AvailableQuests { get; set; }
```

### 3. Интеграция с событиями игры

Для автоматического обновления прогресса квестов добавьте вызовы в соответствующие места:

#### При убийстве монстра:
```csharp
GameServices.QuestManager?.OnMonsterKilled(killedMonster, player);
```

#### При разговоре с NPC:
```csharp
GameServices.QuestManager?.OnNPCTalked(npc, player);
```

#### При смене локации:
```csharp
GameServices.QuestManager?.OnLocationChanged(player);
```

#### При получении предмета:
```csharp
GameServices.QuestManager?.OnItemObtained(item, player);
```

### 4. Интеграция с диалогами

В системе диалогов добавьте обработку действий квестов:

```csharp
// В DialogueSystem или аналогичном классе
var questManager = GameServices.QuestManager;
if (questManager != null)
{
    foreach (var action in response.Actions)
    {
        if (action.Type == "StartQuest" || action.Type == "CompleteQuest")
        {
            questManager.ProcessQuestAction(action, player);
        }
    }
}
```

## Использование JsonEditor

### 1. Создание квестов

1. Запустите JsonEditor
2. Перейдите к разделу "Квесты"
3. Нажмите "Добавить квест"
4. Заполните информацию на всех вкладках:
   - **Основное**: ID, название, описание, квестодатель
   - **Условия**: добавьте необходимые условия (убить мобов, собрать предметы, etc.)
   - **Награды**: укажите опыт, золото и предметы
   - **Диалоги**: укажите ID узлов диалогов для разных состояний
   - **Предварительные условия**: добавьте зависимости от других квестов

### 2. Создание диалогов

В диалогах NPC используйте действия:
- `StartQuest` с параметром ID квеста для начала квеста
- `CompleteQuest` с параметром ID квеста для завершения квеста

## Примеры квестов

### Простой квест "Убить крыс"
```json
{
  "id": 1,
  "name": "Убить крыс",
  "description": "Убей 3 крыс в подвале",
  "questGiverId": 1,
  "conditions": [
    {
      "id": 1,
      "description": "Убить крыс",
      "requiredAmount": 3,
      "monsterID": 1
    }
  ],
  "rewards": {
    "experience": 100,
    "gold": 50
  }
}
```

### Сложный квест с несколькими условиями
```json
{
  "id": 2,
  "name": "Герой деревни",
  "description": "Выполни все задания для защиты деревни",
  "questGiverId": 2,
  "conditions": [
    {
      "id": 1,
      "description": "Убить 5 гоблинов",
      "requiredAmount": 5,
      "monsterID": 2
    },
    {
      "id": 2,
      "description": "Собрать 10 лечебных трав",
      "requiredAmount": 10,
      "itemID": 3
    },
    {
      "id": 3,
      "description": "Посетить пещеру дракона",
      "requiredAmount": 1,
      "locationID": 5
    }
  ],
  "rewards": {
    "experience": 500,
    "gold": 200,
    "items": [
      {
        "itemId": 4,
        "quantity": 1
      }
    ]
  },
  "prerequisites": [1]
}
```

## Преимущества новой системы

1. **Гибкость**: Множество типов условий квестов
2. **Автоматизация**: Автоматическое отслеживание прогресса
3. **Динамические диалоги**: NPC реагируют на состояние квестов
4. **Визуальный редактор**: Удобное создание квестов
5. **Расширяемость**: Легко добавлять новые типы условий
6. **Совместимость**: Работает параллельно со старой системой

## Следующие шаги

1. **Тестирование**: Протестируйте систему с различными типами квестов
2. **Интеграция событий**: Добавьте вызовы обновления прогресса в нужные места
3. **Создание квестов**: Используйте JsonEditor для создания интересных квестов
4. **Расширение**: При необходимости добавьте новые типы условий

Система готова к использованию и может быть легко расширена для добавления новых возможностей!
