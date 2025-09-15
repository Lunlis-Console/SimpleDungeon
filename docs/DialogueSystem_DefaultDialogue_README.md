# Система диалогов с дефолтными диалогами и квестовой инъекцией

## Обзор

Реализована новая система диалогов в стиле Skyrim, которая позволяет NPC иметь дефолтные (idle) диалоги с возможностью перехода к квестовым диалогам через явные опции.

## Основные возможности

### 1. Дефолтные диалоги NPC
- Каждый NPC может иметь `DefaultDialogueId` - основной диалог, который открывается по умолчанию
- Поддержка обратной совместимости с `GreetingDialogueId`
- Диалоги содержат обычные темы: торговля, болтовня, общие вопросы

### 2. Квестовая инъекция
- Квестовые узлы динамически добавляются в диалог через `QuestDialogueManager.InjectQuestNodesForNPC`
- Квестовые опции появляются в дефолтном диалоге как отдельные пункты
- Поддержка автоматического переключения на квесты `ReadyToComplete`

### 3. Типизация узлов диалогов
- Поле `Type` для категоризации узлов: "greeting", "shop", "quest_offer", "quest_complete" и т.д.
- Поле `Tags` для дополнительной маркировки узлов
- Поле `Condition` в ответах для условного отображения опций

## Структура данных

### NPCData
```csharp
public class NPCData
{
    public int ID { get; set; }
    public string Name { get; set; }
    public string Greeting { get; set; }
    public string DefaultDialogueId { get; set; }  // Новое поле
    public string GreetingDialogueId { get; set; } // Для обратной совместимости
    // ... другие поля
}
```

### DialogueNodeData
```csharp
public class DialogueNodeData
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }              // Новое поле
    public List<string> Tags { get; set; }        // Новое поле
    public List<DialogueChoiceData> Choices { get; set; }
}
```

### DialogueChoiceData
```csharp
public class DialogueChoiceData
{
    public string Text { get; set; }
    public string NextNodeId { get; set; }
    public string Condition { get; set; }         // Новое поле
    // ... другие поля
}
```

## API

### QuestDialogueManager.InjectQuestNodesForNPC
```csharp
public string InjectQuestNodesForNPC(int npcId, DialogueDocument dialogue, bool autoOverrideStart = false)
```

**Параметры:**
- `npcId` - ID NPC
- `dialogue` - Диалоговый документ для модификации
- `autoOverrideStart` - Автоматически переключаться на квест ReadyToComplete

**Возвращает:**
- ID узла для принудительного старта (если `autoOverrideStart = true` и есть квест ReadyToComplete)
- `null` в остальных случаях

## Примеры использования

### Создание NPC с дефолтным диалогом
```json
{
  "ID": 1,
  "Name": "Торговец Гарольд",
  "DefaultDialogueId": "merchant_harold_default",
  "GreetingDialogueId": "old_greeting"
}
```

### Дефолтный диалог торговца
```json
{
  "Id": "merchant_harold_default",
  "Name": "Дефолтный диалог торговца",
  "Start": "greeting",
  "Nodes": [
    {
      "Id": "greeting",
      "Text": "Добро пожаловать в мой магазин! Чем могу помочь?",
      "Type": "greeting",
      "Tags": ["default", "main"],
      "Choices": [
        {
          "Text": "Посмотреть товары",
          "NextNodeId": "shop_menu"
        },
        {
          "Text": "Есть ли у вас задания?",
          "NextNodeId": "quests_menu",
          "Condition": "hasAvailableQuests"
        },
        {
          "Text": "До свидания",
          "NextNodeId": null
        }
      ]
    }
  ]
}
```

### Квест с диалоговыми узлами
```json
{
  "ID": 1,
  "Name": "Доставка посылки",
  "State": "Available",
  "DialogueNodes": {
    "Offer": "quest_delivery_offer",
    "InProgress": "quest_delivery_progress",
    "ReadyToComplete": "quest_delivery_complete",
    "Completed": "quest_delivery_completed"
  }
}
```

## Обратная совместимость

### Миграция с GreetingDialogueId
Если у NPC указан только `GreetingDialogueId`, система автоматически использует его как `DefaultDialogueId`:

```csharp
npc.DefaultDialogueId = !string.IsNullOrEmpty(npcData.DefaultDialogueId) 
    ? npcData.DefaultDialogueId 
    : npcData.GreetingDialogueId;
```

### Конвертация старых диалогов
Старые диалоги без полей `Type`, `Tags`, `Condition` продолжают работать. Новые поля инициализируются пустыми значениями.

## UI изменения

### DialogueScreen
- Группировка опций по типам (обычные и квестовые)
- Разные цвета для разных типов опций
- Динамические заголовки разделов

### JsonEditor
- Новые поля в формах редактирования NPC и диалогов
- Поддержка редактирования `Type`, `Tags`, `Condition`
- Выбор `DefaultDialogueId` для NPC

## Тестирование

Созданы тесты для проверки функциональности:

### QuestDialogueManagerTests
- Тест базовой инъекции квестовых узлов
- Тест автоматического переключения на ReadyToComplete
- Тест создания меню квестов

### DataLoadingTests
- Тест загрузки NPC с DefaultDialogueId
- Тест обратной совместимости
- Тест загрузки новых полей диалогов
- Тест конвертации данных

### Запуск тестов
```csharp
// Запуск всех тестов
DialogueSystemTestRunner.RunAllTests();

// Запуск только тестов загрузки данных
DialogueSystemTestRunner.RunDataLoadingTests();

// Запуск только тестов QuestDialogueManager
DialogueSystemTestRunner.RunQuestDialogueManagerTests();
```

## Примеры использования
```csharp
// Запуск примеров
DialogueSystemExamples.RunAllExamples();
```

## Миграция существующих данных

1. **NPC**: Добавить поле `DefaultDialogueId` (может быть равно `GreetingDialogueId`)
2. **Диалоги**: Добавить поля `Type`, `Tags` для узлов и `Condition` для ответов
3. **Квесты**: Убедиться, что `DialogueNodes` содержит все необходимые ID узлов

## Рекомендации по использованию

### Типы узлов
- `greeting` - приветствие, основной узел
- `shop` - торговля
- `service` - услуги
- `small_talk` - болтовня
- `quest_offer` - предложение квеста
- `quest_progress` - квест в процессе
- `quest_complete` - завершение квеста
- `quest_completed` - квест завершен
- `quests_menu` - меню квестов

### Теги
- `default` - дефолтные узлы
- `main` - основные узлы
- `quest` - квестовые узлы
- `service` - сервисные узлы
- `casual` - неформальные узлы

### Условия
- `hasAvailableQuests` - есть доступные квесты
- `hasActiveQuests` - есть активные квесты
- `questAvailable:quest_id` - конкретный квест доступен
- `questCompleted:quest_id` - конкретный квест завершен
- `hasItem:item_id` - есть предмет
- `level:min_level` - минимальный уровень

## Заключение

Новая система диалогов обеспечивает:
- ✅ Дефолтные диалоги для всех NPC
- ✅ Плавные переходы к квестовым диалогам
- ✅ Обратную совместимость
- ✅ Расширяемость через типы и теги
- ✅ Условную логику в диалогах
- ✅ Улучшенный UX с группировкой опций
