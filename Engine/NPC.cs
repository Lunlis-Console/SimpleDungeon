namespace Engine
{
    public class NPC : IInteractable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Greeting { get; set; }
        public List<Quest> QuestsToGive { get; set; }
        public virtual List<InventoryItem> ItemsForSale { get; set; }
        public virtual int Gold { get; set; }
        public virtual int BuyPriceModifier => 100; // 100% по умолчанию
        public virtual int SellPriceModifier => 80; // 80% по умолчанию
        public ITrader Trader { get; set; } // Добавляем это свойство

        private readonly IWorldRepository _worldRepository;

        public NPC(int id, string name, string greeting = "", IWorldRepository worldRepository = null)
        {
            ID = id;
            Name = name;
            Greeting = greeting;
            QuestsToGive = new List<Quest>();
            _worldRepository = worldRepository;
        }

        // Новый метод для инициализации магазина
        public virtual void InitializeShop(Player player)
        {
            // Базовая реализация - может быть переопределена
            ItemsForSale = new List<InventoryItem>();
        }

        public virtual bool CanAfford(Item item, int quantity, Player player)
        {
            return player.Gold >= (item.Price * BuyPriceModifier / 100) * quantity;
        }

        public virtual string GetShopGreeting()
        {
            return $"{Name}: Что желаете приобрести?";
        }

        // В методе GetAvailableActions добавьте:
        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Поговорить", "Осмотреть" };

            // Добавляем опцию торговли если NPC является торговцем
            if (Trader != null)
            {
                actions.Add("Торговать");
            }

            // Добавляем опцию квестов если они есть
            if (QuestsToGive?.Count > 0)
            {
                actions.Add("Квесты");
            }

            return actions;
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

        // Новый метод Talk - просто начинает разговор
        public virtual void Talk(Player player)
        {
            DebugConsole.Log("DEBUG: NPC's Talk() started");

            // Создаем список опций с действиями
            var menuOptions = new List<MenuOption>();
            DebugConsole.Log("DEBUG: Create list of options");

            DebugConsole.Log("DEBUG: Try to check availabe quests");
            // Доступные квесты (еще не взятые)
            var availableQuests = QuestsToGive?
                .Where(q => !player.QuestLog.ActiveQuests.Contains(q) &&
                           !player.QuestLog.CompletedQuests.Contains(q))
                .ToList() ?? new List<Quest>();

            for (int i = 0; i < availableQuests.Count; i++)
            {
                DebugConsole.Log(availableQuests[i].Name);
            }



            foreach (var quest in availableQuests)
            {
                menuOptions.Add(new MenuOption($"Квест: {quest.Name}", () => OfferQuest(player, quest)));

            }

            // Активные квесты (уже взятые, но еще не завершенные)
            var activeQuests = QuestsToGive?
                .Where(q => player.QuestLog.ActiveQuests.Contains(q) &&
                           !player.QuestLog.CompletedQuests.Contains(q) &&
                           !q.CheckCompletion(player)) // Исключаем готовые к сдаче
                .ToList() ?? new List<Quest>();

            foreach (var quest in activeQuests)
            {
                menuOptions.Add(new MenuOption($"Квест: {quest.Name} ?", () => ShowQuestProgress(player, quest)));
            }

            // Квесты для сдачи (готовые к завершению)
            var completableQuests = player.QuestLog.ActiveQuests?
                .Where(q => q.CheckCompletion(player))
                .ToList() ?? new List<Quest>();

            foreach (var quest in completableQuests)
            {
                menuOptions.Add(new MenuOption($"Сдать: {quest.Name} ✓", () => CompleteQuest(player, quest)));
            }

            // Стандартные опции
            menuOptions.Add(new MenuOption("Уйти", () => { }));

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
        }
        protected virtual void ShowQuestProgress(Player player, Quest quest)
        {
            Console.Clear();
            Console.WriteLine($"======{quest.Name}======");
            Console.WriteLine($"{quest.Description}");

            // Добавляем информацию о квестодателе
            if (quest.QuestGiver != null)
            {
                Console.WriteLine($"\nКвестодатель: {quest.QuestGiver.Name}");
            }

            Console.WriteLine("\nПрогресс:");
            foreach (var questItem in quest.QuestItems)
            {
                var playerItem = player.Inventory.Items.Find(ii => ii.Details.ID == questItem.Details.ID);
                int currentQuantity = playerItem?.Quantity ?? 0;
                string status = currentQuantity >= questItem.Quantity ? "✓" : $"{currentQuantity}/{questItem.Quantity}";

                Console.WriteLine($"• {questItem.Details.Name}: {status}");
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

            Console.WriteLine("\nНажмите любую клавишу чтобы вернуться...");
            Console.ReadKey();
        }
        protected virtual void HaveConversation()
        {
            DebugConsole.Log($"Conversation with: {Name}");
            Console.Clear();
            Console.WriteLine($"{Name}: {Greeting}");
            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();
        }

        protected virtual void OfferQuest(Player player, Quest quest)
        {
            DebugConsole.Log($"Offering quest: {quest.Name}");

            // Проверяем, не взят ли уже этот квест у другого NPC
            if (player.QuestLog.ActiveQuests.Any(q => q.ID == quest.ID) ||
                player.QuestLog.CompletedQuests.Any(q => q.ID == quest.ID))
            {
                DebugConsole.Log($"Quest already taken or completed!");
                Console.WriteLine($"{Name}: Извини, но я слышал, ты уже взял это задание у кого-то другого.");
                Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
                Console.ReadKey();
                return;
            }

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
                $"======{quest.Name}======\n{quest.Description}\n\nЗадание:\n{GetQuestObjectives(quest)}\n\nНаграда: {quest.RewardEXP} опыта, {quest.RewardGold} золота\n{GetQuestRewards(quest)}",
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

        // В методе ExecuteAction добавьте:
        public void ExecuteAction(Player player, string action)
        {
            switch (action)
            {
                case "Поговорить":
                    Talk(player);
                    break;
                case "Осмотреть":
                    Examine(player);
                    break;
                case "Торговать":
                    if (Trader != null)
                    {
                        Trader.InitializeShop(player);
                        new TradeSystem(Trader, player).StartTrade();
                    }
                    break;
                case "Квесты":
                    ShowQuestMenu(player);
                    break;
            }
        }

        private void ShowQuestMenu(Player player)
        {
            // Логика меню квестов (перенести из старого Talk метода)
        }

        // Новый метод для осмотра NPC
        private void Examine(Player player)
        {
            bool isExamining = true;

            while (isExamining)
            {
                Console.Clear();
                Console.WriteLine($"============ ОСМОТР: {Name} ============");
                Console.WriteLine("Это житель деревни.");

                // Проверяем ВСЕ квесты, которые есть у NPC
                if (QuestsToGive?.Count > 0)
                {
                    // Доступные квесты (еще не взятые)
                    var availableQuests = QuestsToGive
                        .Where(q => !player.QuestLog.ActiveQuests.Contains(q) &&
                                   !player.QuestLog.CompletedQuests.Contains(q))
                        .ToList();

                    // Активные квесты (уже взятые, но еще не завершенные)
                    var activeQuests = QuestsToGive
                        .Where(q => player.QuestLog.ActiveQuests.Contains(q) &&
                                   !player.QuestLog.CompletedQuests.Contains(q))
                        .ToList();

                    // Завершенные квесты (уже сданные)
                    var completedQuests = QuestsToGive
                        .Where(q => player.QuestLog.CompletedQuests.Contains(q))
                        .ToList();

                    if (availableQuests.Count > 0)
                    {
                        Console.WriteLine("\nПохоже, у этого человека есть для вас дело.");
                        Console.WriteLine($"Доступные квесты: {availableQuests.Count}");
                        foreach (var quest in availableQuests)
                        {
                            Console.WriteLine($"  • {quest.Name}");
                        }
                    }

                    if (activeQuests.Count > 0)
                    {
                        Console.WriteLine("\nУ вас есть активные задания от этого человека.");
                        Console.WriteLine($"Активные квесты: {activeQuests.Count}");
                        foreach (var quest in activeQuests)
                        {
                            Console.WriteLine($"  • {quest.Name} ?");
                        }
                    }

                    if (completedQuests.Count > 0)
                    {
                        Console.WriteLine("\nВы уже выполнили задания этого человека.");
                        Console.WriteLine($"Завершенные квесты: {completedQuests.Count}");
                        foreach (var quest in completedQuests)
                        {
                            Console.WriteLine($"  • {quest.Name} ✓");
                        }
                    }
                }
                else
                {
                    Console.WriteLine("\nУ этого человека нет для вас заданий.");
                }

                Console.WriteLine("\n[Нажмите любую клавишу чтобы вернуться к выбору действия...]");
                Console.ReadKey();

                isExamining = false;
            }
        }

        protected virtual void OnExamine(Player player)
        {
            Examine(player); // Вызываем приватный метод
        }
    }
}