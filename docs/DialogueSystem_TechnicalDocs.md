# 🔧 Техническая документация: Система диалогов

## 🏗️ Архитектура

### Основные компоненты

1. **DialogueModels.cs** - модели данных диалогов
2. **QuestDialogueManager.cs** - менеджер квестовой инъекции
3. **JsonWorldRepository.cs** - загрузка и конвертация данных
4. **NPC.cs** - логика NPC и диалогов
5. **DialogueScreen.cs** - UI отображения диалогов
6. **JsonEditor** - редактор диалогов

### Поток данных

```
JSON Data → JsonWorldRepository → DialogueDocument → QuestDialogueManager → UI
```

## 📊 Модели данных

### DialogueDocument
```csharp
public class DialogueDocument
{
    public string Id { get; set; }
    public string Name { get; set; }
    public string Start { get; set; }
    public List<DialogueNode> Nodes { get; set; }
}
```

### DialogueNode
```csharp
public class DialogueNode
{
    public string Id { get; set; }
    public string Text { get; set; }
    public string Type { get; set; }        // Новое поле
    public List<string> Tags { get; set; }  // Новое поле
    public List<Response> Responses { get; set; }
}
```

### Response
```csharp
public class Response
{
    public string Text { get; set; }
    public string Target { get; set; }
    public string Condition { get; set; }   // Новое поле
    public List<DialogueAction> Actions { get; set; }
}
```

## 🔄 API

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

**Алгоритм:**
1. Находит стартовый узел диалога
2. Получает квесты для NPC (доступные, активные, завершенные)
3. Добавляет квестовые узлы в диалог
4. Добавляет ссылки на квестовые узлы в стартовый узел
5. Создает меню квестов при необходимости
6. Возвращает ID для принудительного старта

### Вспомогательные методы

```csharp
private void AddQuestOfferNode(DialogueDocument dialogue, EnhancedQuest quest)
private void AddQuestInProgressNode(DialogueDocument dialogue, EnhancedQuest quest)
private void AddQuestCompleteNode(DialogueDocument dialogue, EnhancedQuest quest)
private void AddQuestCompletedNode(DialogueDocument dialogue, EnhancedQuest quest)
private void AddQuestPointerToStart(DialogueDocument dialogue, string questNodeId, string choiceText)
private void CreateQuestMenuNode(DialogueDocument dialogue)
```

## 🔧 Конвертация данных

### JsonWorldRepository.ConvertToDialogueDocument

```csharp
private DialogueDocument ConvertToDialogueDocument(DialogueData data)
```

**Процесс:**
1. Создает DialogueDocument из DialogueData
2. Конвертирует DialogueNodeData в DialogueNode
3. Конвертирует DialogueChoiceData в Response
4. Заполняет новые поля: Type, Tags, Condition

### Обратная совместимость

```csharp
// NPC
npc.DefaultDialogueId = !string.IsNullOrEmpty(npcData.DefaultDialogueId) 
    ? npcData.DefaultDialogueId 
    : npcData.GreetingDialogueId;

// DialogueNode
Type = nodeData.Type ?? "",
Tags = nodeData.Tags ?? new List<string>(),

// Response
Condition = choice.Condition ?? "",
```

## 🎨 UI компоненты

### DialogueScreen

**Группировка опций:**
```csharp
var questOptions = availableOptions.Where(o => IsQuestRelatedOption(o.Text)).ToList();
var regularOptions = availableOptions.Where(o => !IsQuestRelatedOption(o.Text)).ToList();
```

**Определение типа узла:**
```csharp
private string GetNodeTypeHeader()
{
    if (_currentNode?.Type == "quest_offer") return "ПРЕДЛОЖЕНИЕ КВЕСТА";
    if (_currentNode?.Type == "quest_complete") return "ЗАВЕРШЕНИЕ КВЕСТА";
    if (_currentNode?.Type == "shop") return "ТОРГОВЛЯ";
    return "ДИАЛОГ";
}
```

### JsonEditor

**Выпадающие списки:**
```csharp
// Type
_nodeType.Items.AddRange(new string[] { 
    "", "greeting", "shop", "service", "small_talk", 
    "quest_offer", "quest_progress", "quest_complete", 
    "quest_completed", "quests_menu" 
});

// Condition
_txtCondition.Items.AddRange(new string[] { 
    "", "hasAvailableQuests", "hasActiveQuests", 
    "questAvailable:quest_id", "questCompleted:quest_id", 
    "hasItem:item_id", "level:min_level", "flag:flag_name" 
});
```

## 🧪 Тестирование

### QuestDialogueManagerTests

```csharp
public static void TestBasicQuestInjection()
public static void TestAutoOverrideForReadyToComplete()
public static void TestQuestMenuCreation()
```

### DataLoadingTests

```csharp
public static void TestNPCDefaultDialogueId()
public static void TestBackwardCompatibility()
public static void TestDialogueNodeFields()
public static void TestDialogueNodeConversion()
```

### Запуск тестов

```csharp
DialogueSystemTestRunner.RunAllTests();
```

## 🔍 Отладка

### Логирование

```csharp
DebugConsole.Log($"InjectQuestNodesForNPC returned forced node: '{forcedNodeId}'");
DebugConsole.Log($"Using forced start node: {forcedNodeId}");
DebugConsole.Log($"Using default start node: {defaultNodeId}");
```

### Проверка состояния

```csharp
// Проверка наличия квестовых узлов
var hasQuestNodes = dialogue.Nodes.Any(n => n.Type?.StartsWith("quest_") == true);

// Проверка условий
if (quest.State == QuestState.ReadyToComplete && autoOverrideStart)
{
    forcedStartNode = quest.DialogueNodes.ReadyToComplete;
}
```

## ⚡ Производительность

### Оптимизации

1. **Кэширование диалогов** - диалоги загружаются один раз
2. **Ленивая загрузка** - квестовые узлы добавляются только при необходимости
3. **Индексация узлов** - быстрый поиск узлов по ID

### Рекомендации

- Используйте уникальные ID для всех узлов
- Избегайте циклических ссылок
- Ограничивайте количество узлов в одном диалоге (< 50)
- Используйте условия для оптимизации отображения

## 🚨 Обработка ошибок

### Валидация данных

```csharp
if (string.IsNullOrEmpty(dialogue.Start))
    throw new ArgumentException("Dialogue start node is required");

if (!dialogue.Nodes.Any(n => n.Id == dialogue.Start))
    throw new ArgumentException($"Start node '{dialogue.Start}' not found");
```

### Fallback механизмы

```csharp
// Fallback для стартового узла
var startNodeId = dialogue.Start ?? (dialogue.Nodes.Count > 0 ? dialogue.Nodes[0].Id : "default_start");

// Fallback для DefaultDialogueId
var effectiveDialogueId = !string.IsNullOrEmpty(npcData.DefaultDialogueId) 
    ? npcData.DefaultDialogueId 
    : npcData.GreetingDialogueId;
```

## 📈 Расширение системы

### Добавление новых типов узлов

1. Добавьте тип в enum или список
2. Обновите UI для отображения
3. Добавьте логику обработки в QuestDialogueManager
4. Обновите документацию

### Добавление новых условий

1. Добавьте условие в список в JsonEditor
2. Реализуйте проверку в игровой логике
3. Добавьте тесты
4. Обновите документацию

### Добавление новых действий

1. Добавьте действие в DialogueAction enum
2. Реализуйте выполнение в DialogueSystem
3. Добавьте UI для выбора параметров
4. Обновите тесты

## 🔗 Интеграция

### С существующими системами

- **QuestSystem** - автоматическая интеграция через QuestLog
- **InventorySystem** - проверка предметов в условиях
- **PlayerSystem** - проверка уровня и флагов
- **WorldState** - проверка глобальных флагов

### API для внешних модулей

```csharp
// Получение диалога для NPC
var dialogue = npc.GetAppropriateDialogueNode(player);

// Инъекция квестовых узлов
var forcedStart = questDialogueManager.InjectQuestNodesForNPC(npcId, dialogue, autoOverrideStart);

// Проверка условий
var canShowOption = CheckCondition(condition, player, worldState);
```

---

**Техническая документация готова! 🚀**
