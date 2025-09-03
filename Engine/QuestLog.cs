using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class QuestLog
    {
        public List<Quest> ActiveQuests { get; set; }
        public List<Quest> CompletedQuests { get; set; }

        public QuestLog()
        {
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

            if (ActiveQuests.Count == 0 && CompletedQuests.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("=========== ЖУРНАЛ КВЕСТОВ ===========");
                Console.WriteLine("У вас нет активных или завершенных квестов.");
                Console.WriteLine("\nНажмите любую клавишу чтобы вернуться...");
                Console.ReadKey();
                return;
            }

            var menuOptions = new List<MenuOption>();

            // Активные квесты
            foreach (var quest in ActiveQuests)
            {
                menuOptions.Add(new MenuOption($"АКТИВНО: {quest.Name}", () => ShowQuestDetails(quest)));
            }

            // Завершенные квесты
            foreach (var quest in CompletedQuests)
            {
                menuOptions.Add(new MenuOption($"ЗАВЕРШЕНО: {quest.Name} ✓", () => ShowQuestDetails(quest)));
            }

            menuOptions.Add(new MenuOption("Назад", () => { }));

            var selected = MenuSystem.SelectFromList(
                menuOptions,
                opt => opt.DisplayText,
                "=========== ЖУРНАЛ КВЕСТОВ ===========",
                "Клавиши 'W' 'S' для выбора, 'E' для просмотра, 'Q' для выхода"
            );

            selected?.Action();
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