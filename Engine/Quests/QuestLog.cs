using Engine.Core;
using Engine.Entities;
using Engine.UI;

namespace Engine.Quests
{
    public class QuestLog
    {
        private readonly Player _player;

        public List<Quest> ActiveQuests { get; set; }
        public List<Quest> CompletedQuests { get; set; }

        public QuestLog(Player player)
        {
            _player = player;
            ActiveQuests = new List<Quest>();
            CompletedQuests = new List<Quest>();
        }

        public void AddQuest(Quest quest)
        {
            if (!ActiveQuests.Any(q => q.ID == quest.ID) && !CompletedQuests.Any(q => q.ID == quest.ID))
            {
                ActiveQuests.Add(quest);
                MessageSystem.AddMessage($"Получен новый квест: {quest.Name}");
            }
        }

        public void CompleteQuest(Quest quest, Player player)
        {
            if (ActiveQuests.Contains(quest))
            {
                quest.CompleteQuest(player);
                ActiveQuests.Remove(quest);
                CompletedQuests.Add(quest);
            }
        }

        public void DisplayQuestLog()
        {
            ScreenManager.PushScreen(new QuestLogScreen(_player));
        }
        private void ShowQuestDetails(Quest quest)
        {
            Console.Clear();
            Console.WriteLine($"======{quest.Name}======");
            Console.WriteLine($"{quest.Description}");

            // Добавляем информацию о квестодателе
            if (quest.QuestGiver != null)
            {
                Console.WriteLine($"\nКвестодатель: {quest.QuestGiver.Name}");
            }

            Console.WriteLine("\nЗадание:");
            foreach (var item in quest.QuestItems)
            {
                Console.WriteLine($"• {item.Details.Name}: {item.Quantity} шт.");
            }

            Console.WriteLine($"\nНаграда: {quest.RewardEXP} опыта, {quest.RewardGold} золота");

            if (quest.RewardItems.Count > 0)
            {
                Console.WriteLine("Предметы:");
                foreach (var item in quest.RewardItems)
                {
                    Console.WriteLine($"• {item.Details.Name} x{item.Quantity}");
                }
            }

            Console.WriteLine($"\nСтатус: {(quest.IsCompleted ? "Завершен" : "Активен")}");
            Console.WriteLine("\nНажмите любую клавишу чтобы вернуться...");
            Console.ReadKey();
        }
        // Остальные методы остаются без изменений...

        private class MenuOption
        {
            public string DisplayText { get; }
            public Action Action { get; }

            public MenuOption(string displayText, Action action)
            {
                DisplayText = displayText;
                Action = action;
            }
        }


    }
}