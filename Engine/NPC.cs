namespace Engine
{
    public class NPC
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Greeting { get; set; }
        public List<Quest> QuestsToGive { get; set; }

        public NPC(int id, string name, string greeting = "")
        {
            ID = id;
            Name = name;
            Greeting = greeting;
            QuestsToGive = new List<Quest>();
        }

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

        public virtual void Talk(Player player)
        {
            bool interacting = true;

            while (interacting)
            {
                // Создаем список опций с действиями
                var menuOptions = new List<MenuOption>();

                // Доступные квесты
                var availableQuests = QuestsToGive?
                    .Where(q => !player.QuestLog.ActiveQuests.Contains(q) &&
                               !player.QuestLog.CompletedQuests.Contains(q))
                    .ToList() ?? new List<Quest>();

                foreach (var quest in availableQuests)
                {
                    menuOptions.Add(new MenuOption($"Квест: {quest.Name}", () => OfferQuest(player, quest)));
                }

                // Квесты для сдачи
                var completableQuests = player.QuestLog.ActiveQuests?
                    .Where(q => q.CheckCompletion(player))
                    .ToList() ?? new List<Quest>();

                foreach (var quest in completableQuests)
                {
                    menuOptions.Add(new MenuOption($"Сдать: {quest.Name} ✓", () => CompleteQuest(player, quest)));
                }

                // Стандартные опции
                menuOptions.Add(new MenuOption("Поговорить", () => HaveConversation()));
                menuOptions.Add(new MenuOption("Уйти", () => { interacting = false; }));

                // Используем MenuSystem для выбора
                var selectedOption = MenuSystem.SelectFromList(
                    menuOptions,
                    opt => opt.DisplayText,
                    $"======{Name}======\n{Greeting}",
                    "Клавиши 'W' 'S' для выбора, 'E' для подтверждения"
                );

                if (selectedOption != null)
                {
                    selectedOption.Action(); // Вызываем действие выбранной опции
                }
                else
                {
                    interacting = false;
                }
            }
        }

        protected virtual void HaveConversation()
        {
            Console.Clear();
            Console.WriteLine($"{Name}: {Greeting}");
            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();
        }

        protected virtual void OfferQuest(Player player, Quest quest)
        {
            var menuOptions = new List<MenuOption>
    {
        new MenuOption("Принять квест", () =>
        {
            player.QuestLog.AddQuest(quest);
            Console.WriteLine($"Квест '{quest.Name}' принят!");
        }),
        new MenuOption("Отказаться", () =>
        {
            Console.WriteLine("Может быть в другой раз...");
        })
    };

            // Показываем описание квеста и меню выбора
            var selectedOption = MenuSystem.SelectFromList(
                menuOptions,
                opt => opt.DisplayText,
                $"======{quest.Name}======\n{quest.Description}\n\nЗадание:\n{GetQuestObjectives(quest)}\n\nНаграда: {quest.RewardEXP} опыта, {quest.RewardGold} золota\n{GetQuestRewards(quest)}",
                "Клавиши 'W' 'S' для выбора, 'E' для подтверждения"
            );

            selectedOption?.Action();
            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();
        }

        // Вспомогательные методы для форматирования
        private string GetQuestObjectives(Quest quest)
        {
            return string.Join("\n", quest.QuestItems.Select(item => $"• {item.Details.Name}: {item.Quantity} шт."));
        }

        private string GetQuestRewards(Quest quest)
        {
            if (quest.RewardItems.Count == 0) return "";
            return "Предметы:\n" + string.Join("\n", quest.RewardItems.Select(item => $"• {item.Details.Name} x{item.Quantity}"));
        }

        protected virtual void CompleteQuest(Player player, Quest quest)
        {
            Console.Clear();
            Console.WriteLine($"Поздравляю! Ты выполнил квест '{quest.Name}'!");
            Console.WriteLine("\nНаграда:");
            Console.WriteLine($"• {quest.RewardEXP} опыта");
            Console.WriteLine($"• {quest.RewardGold} золота");

            foreach (var item in quest.RewardItems)
            {
                Console.WriteLine($"• {item.Details.Name} x{item.Quantity}");
            }

            // Используем MenuSystem для подтверждения
            bool acceptReward = MenuSystem.ConfirmAction("Получить награду?");

            if (acceptReward)
            {
                player.QuestLog.CompleteQuest(quest, player);
                Console.WriteLine("Награда получена!");
                player.CheckLevelUp();
            }

            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();
        }

        public void AddQuest(Quest quest)
        {
            if (quest == null)
            {
                MessageSystem.AddMessage("Ошибка: попытка добавить null квест");
                return;
            }

            if (QuestsToGive == null)
            {
                QuestsToGive = new List<Quest>();
            }

            if (!QuestsToGive.Contains(quest))
            {
                QuestsToGive.Add(quest);
            }
        }

    }
}