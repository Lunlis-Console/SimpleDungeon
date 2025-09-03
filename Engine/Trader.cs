using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Trader : NPC
    {
        public List<InventoryItem> ItemsForSale { get; set; }
        public int Gold { get; set; } = 1000;


        public Trader(int id, string name, string greeting, List<InventoryItem> itemsForSale = null) :
            base(id, name, greeting)
        {
            ItemsForSale = itemsForSale ?? new List<InventoryItem>();
        }

        //Trader теперь будет использовать базовую реализацию NPC, но с дополнительными опциями
        public override void Talk(Player player)
        {
            DebugConsole.Log($"=== TRADER TALK START ===");
            DebugConsole.Log($"Trader: {Name}, ID: {ID}");
            DebugConsole.Log($"QuestsToGive is null: {QuestsToGive == null}");
            DebugConsole.Log($"Quests count: {QuestsToGive?.Count ?? 0}");

            if (QuestsToGive != null)
            {
                foreach (var quest in QuestsToGive)
                {
                    DebugConsole.Log($"- {quest.Name} (ID: {quest.ID}, Completed: {quest.IsCompleted})");

                    // Проверяем условия видимости
                    bool isActive = player.QuestLog.ActiveQuests.Contains(quest);
                    bool isCompleted = player.QuestLog.CompletedQuests.Contains(quest);
                    bool canComplete = quest.CheckCompletion(player);

                    DebugConsole.Log($"  Active: {isActive}, Completed: {isCompleted}, CanComplete: {canComplete}");
                }
            }

            // Создаем список опций с действиями
            var menuOptions = new List<MenuOption>();

            // Торговля - добавляем как первую опцию
            menuOptions.Add(new MenuOption("Торговать", () => StartTrade(player)));

            // Доступные квесты
            var availableQuests = QuestsToGive?
                .Where(q => !player.QuestLog.ActiveQuests.Contains(q) &&
                           !player.QuestLog.CompletedQuests.Contains(q))
                .ToList() ?? new List<Quest>();

            DebugConsole.Log($"Available quests: {availableQuests.Count}");
            foreach (var quest in availableQuests)
            {
                DebugConsole.Log($"Available: {quest.Name}");
                menuOptions.Add(new MenuOption($"Квест: {quest.Name}", () => OfferQuest(player, quest)));
            }

            // Активные квесты (уже взятые, но еще не завершенные)
            var activeQuests = QuestsToGive?
                .Where(q => player.QuestLog.ActiveQuests.Contains(q) &&
                           !player.QuestLog.CompletedQuests.Contains(q) &&
                           !q.CheckCompletion(player)) // Исключаем готовые к сдаче
                .ToList() ?? new List<Quest>();

            DebugConsole.Log($"Active quests: {activeQuests.Count}");
            foreach (var quest in activeQuests)
            {
                DebugConsole.Log($"Active: {quest.Name}");
                menuOptions.Add(new MenuOption($"Квест: {quest.Name} ?", () => ShowQuestProgress(player, quest)));
            }

            // Квесты для сдачи (готовые к завершению)
            var completableQuests = player.QuestLog.ActiveQuests?
                .Where(q => q.CheckCompletion(player))
                .ToList() ?? new List<Quest>();

            DebugConsole.Log($"Completable quests: {completableQuests.Count}");
            foreach (var quest in completableQuests)
            {
                DebugConsole.Log($"Completable: {quest.Name}");
                menuOptions.Add(new MenuOption($"Сдать: {quest.Name} ✓", () => CompleteQuest(player, quest)));
            }

            // Стандартные опции
            menuOptions.Add(new MenuOption("Поговорить", () => HaveConversation()));
            menuOptions.Add(new MenuOption("Уйти", () => { }));

            DebugConsole.Log($"Total menu options: {menuOptions.Count}");

            // Используем MenuSystem для выбора
            var selectedOption = MenuSystem.SelectFromList(
                menuOptions,
                opt => opt.DisplayText,
                $"======{Name}======\n{Greeting}\nЗолото: {player.Gold}",
                "Клавиши 'W' 'S' для выбора, 'E' для подтверждения"
            );

            if (selectedOption != null)
            {
                selectedOption.Action(); // Вызываем действие выбранной опции
            }
        }
        // Добавляем метод для обычного разговора
        protected override void HaveConversation()
        {
            Console.Clear();
            Console.WriteLine($"{Name}: {Greeting}");
            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();
        }

        // Остальной код класса Trader остается без изменений
        public void StartTrade(Player player)
        {
            bool trading = true;
            string systemMessage = "";
            bool viewingTraderItems = true;

            while (trading)
            {
                Console.Clear();

                if (!string.IsNullOrEmpty(systemMessage))
                {
                    Console.WriteLine($"СИСТЕМА: {systemMessage}");
                    systemMessage = "";
                }

                Console.WriteLine($"======{Name}======");
                Console.WriteLine($"{Greeting}");
                Console.WriteLine($"\nВаше золото: {player.Gold}");
                Console.WriteLine($"Золото торговца: {Gold}\n");

                if (viewingTraderItems)
                {
                    Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======");
                    DisplayTraderItems(ItemsForSale);

                    Console.WriteLine("========ВАШИ ПРЕДМЕТЫ========");
                    DisplayPlayerItems(player.Inventory.Items, false);
                }
                else
                {
                    Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======");
                    DisplayTraderItems(ItemsForSale, false);

                    Console.WriteLine("========ВАШИ ПРЕДМЕТЫ========");
                    DisplayPlayerItems(player.Inventory.Items);
                }

                Console.WriteLine("'W' и 'S' - Переключить панель торговли");
                Console.WriteLine("'E' - купить/продать 'Q' - уйти");
                Console.WriteLine(">");

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.S:
                        viewingTraderItems = !viewingTraderItems;
                        break;
                    case ConsoleKey.E:
                        if (viewingTraderItems)
                        {
                            systemMessage = BuySelectedItem(player);
                        }
                        else
                        {
                            systemMessage = SellSelectedItem(player);
                        }
                        break;
                    case ConsoleKey.Q:
                        trading = false;
                        Console.Clear();
                        Console.WriteLine($"{Name}: Заходи еще!");
                        break;

                    default:
                        systemMessage = "Используйте клавиши для навигации, E для действия, Q для выхода";
                        break;
                }
            }
        }
        //public void StartTrade(Player player)
        //{
        //    bool trading = true;
        //    string systemMessage = "";
        //    bool viewingTraderItems = true;
            
        //    while (trading)
        //    {
        //        Console.Clear();

        //        if(!string.IsNullOrEmpty(systemMessage))
        //        {
        //            Console.WriteLine($"СИСТЕМА: {systemMessage}");
        //            systemMessage = "";
        //        }

        //        Console.WriteLine($"======{Name}======");
        //        Console.WriteLine($"{Greeting}");
        //        Console.WriteLine($"\nВаше золото: {player.Gold}");
        //        Console.WriteLine($"Золото торговца: {Gold}\n");
                
        //        if(viewingTraderItems)
        //        {
        //            Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======");
        //            DisplayTraderItems(ItemsForSale);

        //            Console.WriteLine("========ВАШИ ПРЕДМЕТЫ========");
        //            DisplayPlayerItems(player.Inventory.Items, false);
        //        }
        //        else
        //        {
        //            Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======");
        //            DisplayTraderItems(ItemsForSale, false);

        //            Console.WriteLine("========ВАШИ ПРЕДМЕТЫ========");
        //            DisplayPlayerItems(player.Inventory.Items);
        //        }

        //        Console.WriteLine("'W' и 'S' - Переключить панель торговли");
        //        Console.WriteLine("'E' - купить/продать 'Q' - уйти");
        //        Console.WriteLine(">");

        //        var key = Console.ReadKey(true).Key;

        //        switch(key)
        //        {
        //            case ConsoleKey.W:
        //            case ConsoleKey.S:
        //                viewingTraderItems = !viewingTraderItems;
        //                break;
        //            case ConsoleKey.E:
        //                if(viewingTraderItems)
        //                {
        //                    systemMessage = BuySelectedItem(player);
        //                }
        //                else
        //                {
        //                    systemMessage = SellSelectedItem(player);
        //                }
        //                break;
        //            case ConsoleKey.Q:
        //                trading = false;
        //                Console.Clear();
        //                Console.WriteLine($"{Name}: Заходи еще!");
        //                break;

        //            default:
        //                systemMessage = "Используйте клавиши для навигации, E для действия, Q для выхода";
        //                break;
        //        }
        //    }
        //}
        private void DisplayTraderItems(List<InventoryItem> items, bool highlight = true)
        {
            if(items.Count == 0)
            {
                Console.WriteLine("     Товаров нет.");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (highlight)
                    Console.ForegroundColor = ConsoleColor.Yellow;

                Console.WriteLine($"    {i + 1}. {items[i].Details.Name} x{items[i].Quantity} - {items[i].Details.Price} золота");
                Console.ResetColor();
            }
        }
        private void DisplayPlayerItems(List<InventoryItem> items, bool highlight = true)
        {
            if (items.Count == 0)
            {
                Console.WriteLine("     Предметов нет.");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (highlight)
                    Console.ForegroundColor = ConsoleColor.Yellow;

                int sellPrice = (int)(items[i].Details.Price * 0.8);
                Console.WriteLine($"    {i + 1}. {items[i].Details.Name} x{items[i].Quantity} - {items[i].Details.Price} золота");
                Console.ResetColor();
            }
        }
        private string BuySelectedItem(Player player)
        {
            if (ItemsForSale.Count == 0)
                return "У торговца нет товаров!";

            // Используем MenuSystem для выбора товара
            var selectedItem = MenuSystem.SelectFromList(
                ItemsForSale,
                item => $"{item.Details.Name} x{item.Quantity} - {item.Details.Price} золота",
                "Выберите товар для покупки",
                "Клавиши 'W' 'S' для выбора, 'E' для покупки, 'Q' для отмены"
            );

            if (selectedItem == null) return "";

            // Используем MenuSystem для выбора количества
            int quantity = SelectQuantity(selectedItem, "покупки", true);
            if (quantity == 0) return "";

            return BuyItem(player, selectedItem, quantity);
        }
        private int SelectQuantity(InventoryItem item, string action, bool isBuying)
        {
            int quantity = 1;
            ConsoleKey key;

            do
            {
                Console.Clear();

                int pricePerItem = isBuying ? item.Details.Price : (int)(item.Details.Price * 0.8);
                int totalPrice = pricePerItem * quantity;

                Console.WriteLine($"====== {item.Details.Name} ======");
                Console.WriteLine($"Тип: {GetItemTypeName(item.Details.Type)}");
                Console.WriteLine($"Доступно: {item.Quantity} шт.");
                Console.WriteLine($"Цена за шт.: {pricePerItem} золota");
                Console.WriteLine($"Количество: {quantity} шт.");
                Console.WriteLine($"Общая стоимость: {totalPrice} золота");
                Console.WriteLine($"\n'W' - увеличить, 'S' - уменьшить");
                Console.WriteLine("E - подтвердить, Q - отмена");

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        if (quantity < item.Quantity)
                            quantity++;
                        break;
                    case ConsoleKey.S:
                        if (quantity > 1)
                            quantity--;
                        break;
                    case ConsoleKey.Q:
                        return 0;
                }

            } while (key != ConsoleKey.E);

            return quantity;
        }
        // Вспомогательный класс для меню опций
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
        private string SellSelectedItem(Player player)
        {
            if (player.Inventory.Items.Count == 0)
                return "У вас нет предметов для продажи!";

            int selectedIndex = SelectItemIndex(player.Inventory.Items, "Выберите предмет для продажи");

            if (selectedIndex == -1)
                return "";

            InventoryItem itemToSell = player.Inventory.Items[selectedIndex];

            int quantity = AskForQuantity(itemToSell, "продажи", false);
            if (quantity == 0)
                return "";

            return SellItem(player, itemToSell, quantity);
        }
        private int AskForQuantity(InventoryItem item, string action, bool isBuying)
        {
            int quantity = 1;
            ConsoleKey key;

            do
            {
                Console.Clear();

                string itemName = item.Details.Name;
                int availableQuantity = item.Quantity;
                int pricePerItem = isBuying ? item.Details.Price : (int)(item.Details.Price * 0.8);
                int totalPrice = pricePerItem * quantity;

                Console.WriteLine($"====== {itemName} ======");
                Console.WriteLine($"Тип: {GetItemTypeName(item.Details.Type)}");

                if (item.Details is Equipment equipment)
                {
                    Console.WriteLine($"АТК: +{equipment.AttackBonus} | ЗЩТ: +{equipment.DefenceBonus}");
                }
                else if (item.Details is HealingItem healingItem)
                {
                    Console.WriteLine($"Восстановление: +{healingItem.AmountToHeal} ОЗ");
                }

                Console.WriteLine($"Доступно: {availableQuantity} шт.");
                Console.WriteLine($"Цена за шт.: {pricePerItem} золота");
                Console.WriteLine($"Количество: {quantity} шт.");
                Console.WriteLine($"Общая стоимость: {totalPrice} золота");
                Console.WriteLine($"\n'W' и 'S' для изменения количества");
                Console.WriteLine("I - информация о предмете");
                Console.WriteLine("E - подтвердить, Q - отмена");

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        if (quantity < availableQuantity)
                            quantity++;
                        break;
                    case ConsoleKey.S:
                        if (quantity > 1)
                            quantity--;
                        break;
                    case ConsoleKey.I:
                        item.Details.Read();
                        break;
                    case ConsoleKey.Q:
                        return 0;
                }

            } while (key != ConsoleKey.E);

            return quantity;
        }
        private string GetItemTypeName(ItemType type)
        {
            return type switch
            {
                ItemType.OneHandedWeapon => "Одноручное оружие",
                ItemType.TwoHandedWeapon => "Двуручное оружие",
                ItemType.OffHand => "Вспомогательное",
                ItemType.Helmet => "Шлем",
                ItemType.Armor => "Броня",
                ItemType.Gloves => "Перчатки",
                ItemType.Boots => "Ботинки",
                ItemType.Amulet => "Амулет",
                ItemType.Ring => "Кольцо",
                ItemType.Consumable => "Расходник",
                ItemType.Stuff => "Прочее",
                _ => "Неизвестно"
            };
        }
        private int SelectItemIndex(List<InventoryItem> items, string title)
        {
            if (items.Count == 0)
                return -1;

            if (items.Count == 1)
                return 0;

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                Console.WriteLine(title);
                Console.WriteLine("Клавиши 'W' 'S' для выбора, 'E' для подтверждения, 'Q' для отмены");
                Console.WriteLine();

                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(">");
                    }
                    else
                    {
                        Console.Write(" ");
                    }

                    if (items == ItemsForSale)
                    {
                        Console.WriteLine($"{items[i].Details.Name} x{items[i].Quantity} - {items[i].Details.Price} золота");
                    }
                    else
                    {
                        int sellPrice = (int)(items[i].Details.Price * 0.8);
                        Console.WriteLine($"{items[i].Details.Name} x{items[i].Quantity} - {sellPrice} золота");
                    }

                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        selectedIndex = (selectedIndex - 1 + items.Count) % items.Count;
                        break;
                    case ConsoleKey.S:
                        selectedIndex = (selectedIndex + 1) % items.Count;
                        break;
                    case ConsoleKey.Q:
                        return -1;
                }

            } while (key != ConsoleKey.E);

            return selectedIndex;
        }
        private string BuyItem(Player player, InventoryItem traderItem, int quantity)
        {
            if (traderItem.Quantity < quantity)
            {
                return $"{traderItem.Details.Name} закончился!";
            }

            int totalPrice = traderItem.Details.Price * quantity;

            if (player.Gold >= totalPrice)
            {
                player.Gold -= totalPrice;
                Gold += totalPrice;

                for (int i = 0; i < quantity; i++)
                {
                    player.AddItemToInventory(traderItem.Details);
                }

                
                traderItem.Quantity -= quantity;

                if(traderItem.Quantity <= 0)
                {
                    ItemsForSale.Remove(traderItem);
                }

                return $"Вы купили {traderItem.Details.Name} за {traderItem.Details.Price} золота.";
            }
            else
            {
                return $"У вас недостаточно золота!";
            }
        }
        private string SellItem(Player player, InventoryItem playerItem, int quantity)
        {
            if (playerItem.Quantity < quantity)
            {
                return $"У вас недостаточно {playerItem.Details.Name}!";
            }

            if(playerItem.Details.Price <= 0)
            {
                return $"СИСТЕМА: Торговец не покупает {playerItem.Details.Name}";
            }

            int sellPricePerItem = (int)(playerItem.Details.Price * 0.8);
            if (sellPricePerItem <= 0)
            {
                sellPricePerItem = 1;
            }

            int totalPrice = sellPricePerItem * quantity;

            if (Gold < totalPrice)
            {
                return $"У торговца недостаточно золота! Нужно: {totalPrice}, есть: {Gold}";
            }

            player.Gold += totalPrice;
            Gold -= totalPrice;

            playerItem.Quantity -= quantity;
            if (playerItem.Quantity <= 0)
            {
                player.Inventory.RemoveItem(playerItem);
            }

            var traderItem = ItemsForSale.FirstOrDefault(ii => ii.Details.ID == playerItem.Details.ID);
            if(traderItem != null)
            {
                traderItem.Quantity += quantity;
            }
            else
            {
                ItemsForSale.Add(new InventoryItem(playerItem.Details, 1));
            }

            return $"Вы продали {quantity} шт. {playerItem.Details.Name} за {totalPrice} золота.";
        }

        public override List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Поговорить", "Осмотреть", "Торговать" };
            return actions;
        }

        public override void ExecuteAction(Player player, string action)
        {
            switch (action)
            {
                case "Поговорить":
                    HaveConversation(); // Простой разговор
                    break;
                case "Осмотреть":
                    OnExamine(player); // Осмотр
                    break;
                case "Торговать":
                    StartTrade(player); // Торговля
                    break;
                default:
                    MessageSystem.AddMessage("Неизвестное действие.");
                    break;
            }
        }

    }
}
