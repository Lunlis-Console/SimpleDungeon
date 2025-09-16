# Изменения в позиционировании MessageSystem в диалогах

## Что было изменено

### 1. BaseScreen.cs
- Добавлен новый метод `RenderOverlay(int startY)` для динамического позиционирования MessageSystem
- Добавлен метод `GetDefaultMessageSystemPosition()` для получения позиции по умолчанию
- Обновлен основной метод `RenderOverlay()` для использования новой функциональности

### 2. DialogueScreen.cs
- Переопределен метод `RenderOverlay()` для позиционирования MessageSystem ниже текста НПС
- Добавлен метод `CalculateMessageSystemPosition()` для вычисления оптимальной позиции
- В конструкторе добавлен вызов `MessageSystem.EnterDialogueMode()`
- В методе `CloseDialogue()` добавлен вызов `MessageSystem.ExitDialogueMode()`

### 3. MessageSystem.cs
- Добавлены статические поля для отслеживания режима диалога:
  - `_isInDialogueMode` - флаг режима диалога
  - `_savedPosition` - сохраненная позиция (для будущего использования)
- Добавлены методы:
  - `EnterDialogueMode()` - вход в режим диалога
  - `ExitDialogueMode()` - выход из режима диалога
  - `IsInDialogueMode` - свойство для проверки режима

## Как это работает

1. **При входе в диалог**: `DialogueScreen` вызывает `MessageSystem.EnterDialogueMode()`
2. **Во время диалога**: `DialogueScreen.RenderOverlay()` вычисляет позицию ниже текста НПС и всех опций ответов
3. **При выходе из диалога**: `DialogueScreen.CloseDialogue()` вызывает `MessageSystem.ExitDialogueMode()`
4. **В других экранах**: MessageSystem отображается в стандартной позиции (под заголовком)

## Результат

- MessageSystem больше не перекрывает текст диалога НПС
- Позиция MessageSystem динамически адаптируется к содержимому диалога
- При выходе из диалога MessageSystem возвращается к стандартной позиции
- Изменения не влияют на другие экраны игры
