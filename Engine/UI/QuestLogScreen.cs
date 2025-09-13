using Engine.Entities;
using Engine.Quests;

namespace Engine.UI
{
    public class QuestLogScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;
        private bool _showActiveQuests = true;

        public QuestLogScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;
        }

        public override void Render()
        {
            ClearScreen();

            RenderHeader("ЖУРНАЛ ЗАДАНИЙ");
            RenderQuestTypesToggle();
            RenderQuestsList();
            RenderSelectedQuestInfo();
            RenderFooter("W/S - выбор │ A/D - переключить тип │ E - детали │ Q - назад");
        }

        private void RenderQuestTypesToggle()
        {
            int y = 4;
            string activeText = _showActiveQuests ? "[АКТИВНЫЕ]" : "АКТИВНЫЕ";
            string completedText = !_showActiveQuests ? "[ЗАВЕРШЕННЫЕ]" : "ЗАВЕРШЕННЫЕ";

            _renderer.Write(2, y, $"{activeText} | {completedText}", ConsoleColor.Yellow);
        }

        private void RenderQuestsList()
        {
            int y = 6;
            var quests = _showActiveQuests ?
                _player.QuestLog.ActiveQuests :
                _player.QuestLog.CompletedQuests;

            if (quests.Count == 0)
            {
                string message = _showActiveQuests ?
                    "Нет активных заданий." :
                    "Нет завершенных заданий.";
                _renderer.Write(2, y, message, ConsoleColor.DarkGray);
                return;
            }

            for (int i = 0; i < quests.Count; i++)
            {
                var quest = quests[i];
                bool isSelected = i == _selectedIndex;
                string statusSymbol = _showActiveQuests ?
                    quest.CheckCompletion(_player) ? " ✓" : " ?" :
                    " ✓";

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
            var quests = _showActiveQuests ?
                _player.QuestLog.ActiveQuests :
                _player.QuestLog.CompletedQuests;

            if (quests.Count == 0 || _selectedIndex >= quests.Count)
                return;

            var selectedQuest = quests[_selectedIndex];
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 6;

            _renderer.Write(rightColumn, y, $"=== {selectedQuest.Name} ===", ConsoleColor.Yellow);
            y += 2;

            // Описание
            var descriptionLines = WrapText(selectedQuest.Description, Console.WindowWidth / 2 - 4);
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

            // Задание
            _renderer.Write(rightColumn, y, "ЗАДАНИЕ:", ConsoleColor.Cyan);
            y++;

            foreach (var questItem in selectedQuest.QuestItems)
            {
                var playerItem = _player.Inventory.Items.Find(ii => ii.Details.ID == questItem.Details.ID);
                int currentQuantity = playerItem?.Quantity ?? 0;
                string status = currentQuantity >= questItem.Quantity ? "✓" : $"{currentQuantity}/{questItem.Quantity}";

                _renderer.Write(rightColumn, y, $"• {questItem.Details.Name}: {status}");
                y++;
            }
            y++;

            // Награда
            _renderer.Write(rightColumn, y, "НАГРАДА:", ConsoleColor.Cyan);
            y++;
            _renderer.Write(rightColumn, y, $"{selectedQuest.RewardEXP} опыта, {selectedQuest.RewardGold} золота");
            y++;

            if (selectedQuest.RewardItems.Count > 0)
            {
                _renderer.Write(rightColumn, y, "Предметы:");
                y++;
                foreach (var item in selectedQuest.RewardItems)
                {
                    _renderer.Write(rightColumn + 2, y, $"• {item.Details.Name} x{item.Quantity}");
                    y++;
                }
            }

            // Статус для активных квестов
            if (_showActiveQuests)
            {
                y++;
                bool isComplete = selectedQuest.CheckCompletion(_player);
                string status = isComplete ? "ГОТОВО К СДАЧЕ!" : "В ПРОЦЕССЕ";
                ConsoleColor color = isComplete ? ConsoleColor.Green : ConsoleColor.Yellow;

                _renderer.Write(rightColumn, y, $"Статус: {status}", color);
            }
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            var quests = _showActiveQuests ?
                _player.QuestLog.ActiveQuests :
                _player.QuestLog.CompletedQuests;

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

                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    ToggleQuestType(false);
                    break;

                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    ToggleQuestType(true);
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

        private void ToggleQuestType(bool showActive)
        {
            _showActiveQuests = showActive;
            _selectedIndex = 0;
        }

        private void ShowQuestDetails(Quest quest)
        {
            // Можно реализовать всплывающее окно с детальной информацией
            //ScreenManager.PushScreen(new QuestDetailScreen(_player, quest));
        }
    }
}