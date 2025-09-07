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
        public DialogueSystem.DialogueNode GreetingDialogue { get; set; }

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
            if (GreetingDialogue != null)
            {
                // Создаем и показываем экран диалога
                var dialogueScreen = new DialogueScreen(this, GreetingDialogue);
                ScreenManager.PushScreen(dialogueScreen);
            }
            else
            {
                // Fallback к старой системе
                HaveConversation(player);
            }
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Поговорить" };

            // Автоматически добавляем торговлю если NPC - торговец
            if (Trader != null)
            {
                actions.Add("Торговля");
            }

            // Автоматически добавляем квесты если они доступны
            if (HasAvailableQuests(player) || HasCompletableQuests(player))
            {
                actions.Add("Задания");
            }

            actions.Add("Осмотреть");
            return actions;
        }
        private bool HasAvailableQuests(Player player)
        {
            return QuestsToGive?.Any(q =>
                !player.QuestLog.ActiveQuests.Contains(q) &&
                !player.QuestLog.CompletedQuests.Contains(q)) ?? false;
        }

        private bool HasCompletableQuests(Player player)
        {
            return QuestsToGive?.Any(q =>
                player.QuestLog.ActiveQuests.Contains(q) &&
                q.CheckCompletion(player)) ?? false;
        }

        protected virtual void HaveConversation(Player player)
        {
            Console.Clear();
            Console.WriteLine($"╔{new string('═', Name.Length + 4)}╗");
            Console.WriteLine($"║  {Name}  ║");
            Console.WriteLine($"╚{new string('═', Name.Length + 4)}╝");
            Console.WriteLine();
            Console.WriteLine(Greeting);
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            Console.ReadKey(true);
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
        public virtual void Examine(Player player)
        {
            Console.Clear();
            Console.WriteLine($"============ ОСМОТР: {Name} ============");
            Console.WriteLine("Это житель деревни.");

            // Информация о квестах
            if (QuestsToGive?.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Возможные задания:");

                foreach (var quest in QuestsToGive)
                {
                    string status = "❓"; // Доступен
                    if (player.QuestLog.ActiveQuests.Contains(quest))
                        status = quest.CheckCompletion(player) ? "✅" : "⏳";
                    else if (player.QuestLog.CompletedQuests.Contains(quest))
                        status = "✔️";

                    Console.WriteLine($" {status} {quest.Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            Console.ReadKey(true);
        }
        protected virtual void OnExamine(Player player)
        {
            Examine(player); // Вызываем приватный метод
        }
    }
}