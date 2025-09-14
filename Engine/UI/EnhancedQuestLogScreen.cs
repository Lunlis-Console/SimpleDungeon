using Engine.Entities;
using Engine.Quests;
using Engine.Core;

namespace Engine.UI
{
    public class EnhancedQuestLogScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;
        private QuestTab _currentTab = QuestTab.Active;

        private enum QuestTab
        {
            Active,
            Available,
            Completed
        }

        public EnhancedQuestLogScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;
        }

        public override void Render()
        {
            ClearScreen();

            RenderHeader("ЖУРНАЛ ЗАДАНИЙ");
            RenderQuestTabs();
            RenderQuestsList();
            RenderSelectedQuestInfo();
            RenderFooter("W/S - выбор │ Tab - переключить вкладку │ E - детали │ Q - назад");
        }

        private void RenderQuestTabs()
        {
            int y = 4;
            int x = 2;

            string activeText = _currentTab == QuestTab.Active ? "[АКТИВНЫЕ]" : "АКТИВНЫЕ";
            string availableText = _currentTab == QuestTab.Available ? "[ДОСТУПНЫЕ]" : "ДОСТУПНЫЕ";
            string completedText = _currentTab == QuestTab.Completed ? "[ЗАВЕРШЕННЫЕ]" : "ЗАВЕРШЕННЫЕ";

            _renderer.Write(x, y, $"{activeText} | {availableText} | {completedText}", ConsoleColor.Yellow);
        }

        private void RenderQuestsList()
        {
            int y = 6;
            var quests = GetCurrentQuests();

            if (quests.Count == 0)
            {
                string message = GetEmptyMessage();
                _renderer.Write(2, y, message, ConsoleColor.DarkGray);
                return;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                bool isSelected = i == _selectedIndex;
                string statusSymbol = GetStatusSymbol(quest);

                string displayText = $"{quest.Name}{statusSymbol}";

                if (isSelected)
                {
                    _renderer.Write(2, y, "► ");
                    _renderer.Write(4, y, displayText, ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, y, displayText);
                }
                y++;
            }
        }

        private void RenderSelectedQuestInfo()
        {
            var quests = GetCurrentQuests();

            if (quests.Count == 0 || _selectedIndex >= quests.Count)
                return;

            var selectedQuest = quests[_selectedIndex];
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 6;

            _renderer.Write(rightColumn, y, $"=== {selectedQuest.Name} ===", ConsoleColor.Yellow);
            y += 2;

            // Описание
            var description = !string.IsNullOrEmpty(selectedQuest.DetailedDescription) 
                ? selectedQuest.DetailedDescription 
                : selectedQuest.Description;
            
            var descriptionLines = WrapText(description, Console.WindowWidth / 2 - 4);
            foreach (var line in descriptionLines)
            {
                _renderer.Write(rightColumn, y, line);
                y++;
            }
            y++;

            // Квестодатель
            if (selectedQuest.QuestGiver != null)
            {
                _renderer.Write(rightColumn, y, $"От: {selectedQuest.QuestGiver.Name}", ConsoleColor.Cyan);
                y++;
            }
            y++;

            // Условия квеста
            var runtimeConditions = selectedQuest.GetRuntimeConditions();
            if (runtimeConditions.Count > 0)
            {
                _renderer.Write(rightColumn, y, "УСЛОВИЯ:", ConsoleColor.Cyan);
                y++;

                foreach (var condition in runtimeConditions)
                {
                    string progressText = condition.GetProgressText();
                    ConsoleColor color = condition.IsCompleted ? ConsoleColor.Green : ConsoleColor.White;

                    _renderer.Write(rightColumn, y, $"• {progressText}", color);
                    y++;
                }
                y++;
            }

            // Награды
            if (selectedQuest.Rewards != null)
            {
                _renderer.Write(rightColumn, y, "НАГРАДЫ:", ConsoleColor.Cyan);
                y++;
                
                if (selectedQuest.Rewards.Experience > 0)
                    _renderer.Write(rightColumn, y, $"• {selectedQuest.Rewards.Experience} опыта");
                y++;
                
                if (selectedQuest.Rewards.Gold > 0)
                    _renderer.Write(rightColumn, y, $"• {selectedQuest.Rewards.Gold} золота");
                y++;

                if (selectedQuest.Rewards.Items.Count > 0)
                {
                    _renderer.Write(rightColumn, y, "Предметы:");
                    y++;
                    foreach (var item in selectedQuest.Rewards.Items)
                    {
                        if (item.ItemDetails != null)
                        {
                            _renderer.Write(rightColumn + 2, y, $"• {item.ItemDetails.Name} x{item.Quantity}");
                            y++;
                        }
                    }
                }
            }

            // Статус
            y++;
            string status = GetQuestStatus(selectedQuest);
            ConsoleColor statusColor = GetQuestStatusColor(selectedQuest);
            _renderer.Write(rightColumn, y, $"Статус: {status}", statusColor);

            // Процент выполнения
            if (_currentTab == QuestTab.Active)
            {
                y++;
                int progressPercentage = selectedQuest.GetProgressPercentage();
                _renderer.Write(rightColumn, y, $"Прогресс: {progressPercentage}%", ConsoleColor.Cyan);
            }
        }

        private List<EnhancedQuest> GetCurrentQuests()
        {
            return _currentTab switch
            {
                QuestTab.Active => _player.QuestLog.ActiveQuests,
                QuestTab.Available => _player.QuestLog.AvailableQuests,
                QuestTab.Completed => _player.QuestLog.CompletedQuests,
                _ => new List<EnhancedQuest>()
            };
        }

        private string GetEmptyMessage()
        {
            return _currentTab switch
            {
                QuestTab.Active => "Нет активных заданий.",
                QuestTab.Available => "Нет доступных заданий.",
                QuestTab.Completed => "Нет завершенных заданий.",
                _ => ""
            };
        }

        private string GetStatusSymbol(EnhancedQuest quest)
        {
            return _currentTab switch
            {
                QuestTab.Active => quest.State switch
                {
                    QuestState.ReadyToComplete => " ✓",
                    QuestState.InProgress => " ?",
                    _ => " ?"
                },
                QuestTab.Available => " !",
                QuestTab.Completed => " ✓",
                _ => ""
            };
        }

        private string GetQuestStatus(EnhancedQuest quest)
        {
            return quest.State switch
            {
                QuestState.NotStarted => "Не начат",
                QuestState.InProgress => "В процессе",
                QuestState.ReadyToComplete => "Готов к завершению",
                QuestState.Completed => "Завершен",
                QuestState.Failed => "Провален",
                _ => "Неизвестно"
            };
        }

        private ConsoleColor GetQuestStatusColor(EnhancedQuest quest)
        {
            return quest.State switch
            {
                QuestState.NotStarted => ConsoleColor.Gray,
                QuestState.InProgress => ConsoleColor.Yellow,
                QuestState.ReadyToComplete => ConsoleColor.Green,
                QuestState.Completed => ConsoleColor.Cyan,
                QuestState.Failed => ConsoleColor.Red,
                _ => ConsoleColor.White
            };
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            HandleCommonInput(keyInfo);

            var quests = GetCurrentQuests();

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(quests.Count - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.Tab:
                    SwitchTab();
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    if (quests.Count > 0)
                        ShowQuestDetails(quests[_selectedIndex]);
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void SwitchTab()
        {
            _currentTab = _currentTab switch
            {
                QuestTab.Active => QuestTab.Available,
                QuestTab.Available => QuestTab.Completed,
                QuestTab.Completed => QuestTab.Active,
                _ => QuestTab.Active
            };
            _selectedIndex = 0;
        }

        private void ShowQuestDetails(EnhancedQuest quest)
        {
            // Можно реализовать всплывающее окно с детальной информацией
            //ScreenManager.PushScreen(new EnhancedQuestDetailScreen(_player, quest));
        }
    }
}
