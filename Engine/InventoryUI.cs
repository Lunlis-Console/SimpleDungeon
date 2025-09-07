namespace Engine
{
    public static class InventoryUI
    {

        public static void RenderInventory(InventoryRenderData inventoryData)
        {
            var renderer = GameServices.BufferedRenderer;
            renderer.BeginFrame();

            int windowWidth = Console.WindowWidth;
            int dividerPosition = windowWidth / 2;

            // Заголовок
            renderer.Write(0, 0, "=========СУМКА=========", ConsoleColor.Yellow);

            // Левая колонка - предметы
            int maxLeftItems = Math.Min(inventoryData.Items.Count, Console.WindowHeight - 15);
            int y = 2;

            for (int i = 0; i < maxLeftItems; i++)
            {
                string displayText = GetItemDisplayText(inventoryData.Items[i]);
                if (displayText.Length > dividerPosition - 3)
                    displayText = displayText.Substring(0, dividerPosition - 6) + "...";

                if (i == inventoryData.SelectedIndex)
                {
                    renderer.Write(0, y, "> ", ConsoleColor.Green);
                    renderer.Write(2, y, displayText, ConsoleColor.Green);
                }
                else
                {
                    renderer.Write(2, y, displayText, ConsoleColor.White);
                }
                y++;
            }

            // Правая колонка - экипировка и статистика
            string[] rightContent = {
                "======ЭКИПИРОВКА======",
                $"Оружие: {(inventoryData.MainHand?.Name ?? "Пусто")}",
                $"Шлем: {(inventoryData.Helmet?.Name ?? "Пусто")}",
                $"Броня: {(inventoryData.Armor?.Name ?? "Пусто")}",
                $"Перчатки: {(inventoryData.Gloves?.Name ?? "Пусто")}",
                $"Ботинки: {(inventoryData.Boots?.Name ?? "Пусто")}",
                $"Амулет: {(inventoryData.Amulet?.Name ?? "Пусто")}",
                $"Кольцо 1: {(inventoryData.Ring1?.Name ?? "Пусто")}",
                $"Кольцо 2: {(inventoryData.Ring2?.Name ?? "Пусто")}",
                "",
                "======ПАРАМЕТРЫ======",
                $"Здоровье: {inventoryData.CurrentHP}/{inventoryData.TotalMaximumHP}",
                $"Атака: {inventoryData.Attack}",
                $"Защита: {inventoryData.Defence}",
                $"Ловкость: {inventoryData.Agility}",
                $"Золото: {inventoryData.Gold}",
                $"Уровень: {inventoryData.Level}",
                $"Опыт: {inventoryData.CurrentEXP}/{inventoryData.MaximumEXP}"
            };

            // Отрисовываем правую колонку
            for (int i = 0; i < rightContent.Length && i < Console.WindowHeight - 1; i++)
            {
                renderer.Write(dividerPosition + 1, i + 1, rightContent[i], ConsoleColor.White);
            }

            // Управление
            string controls = "W/S - Выбор │ E - Действие │ Q - Назад";
            int controlsX = (windowWidth - controls.Length) / 2;
            renderer.Write(controlsX, Console.WindowHeight - 2, controls, ConsoleColor.DarkGray);

            renderer.EndFrame();
        }

        private static string GetItemDisplayText(object item)
        {
            return item switch
            {
                InventoryItem invItem => $"{invItem.Details.Name} x{invItem.Quantity}",
                EquipmentSlotItem eqItem => $"[Надето] {eqItem.Equipment.Name}",
                _ => item?.ToString() ?? "Неизвестный предмет"
            };
        }
        public static void ShowItemContextMenu(Player player, InventoryItem inventoryItem)
        {
            // Вместо старого контекстного меню, просто переходим на экран действий
            ScreenManager.PushScreen(new InventoryItemActionScreen(player, inventoryItem));
        }
        public static string SelectActionFromList(List<string> actions, string title)
        {
            if (actions == null || actions.Count == 0)
                return null;

            if (actions.Count == 1)
                return actions[0];

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();

                //MessageSystem.DisplayMessages();

                Console.WriteLine($"{title}");
                Console.WriteLine("Клавиши 'W' 'S' для выбора, 'E' для подтверждения, 'Q' для выхода");
                Console.WriteLine();

                for (int i = 0; i < actions.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(">");
                    }
                    else
                    {
                        Console.Write("  ");
                    }

                    Console.WriteLine(actions[i]);
                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        selectedIndex = (selectedIndex - 1 + actions.Count) % actions.Count;
                        break;
                    case ConsoleKey.S:
                        selectedIndex = (selectedIndex + 1) % actions.Count;
                        break;
                    case ConsoleKey.Q:
                        return null;
                }

            } while (key != ConsoleKey.E);

            return actions[selectedIndex];
        }

        public static object SelectItemFromCombinedList(List<object> items, Player player)
        {
            if (items == null || items.Count == 0)
                return null;

            int selectedIndex = 0;
            bool needsRedraw = true;

            while (true)
            {
                if (needsRedraw)
                {
                    var inventoryData = new InventoryRenderData
                    {
                        Items = items,
                        SelectedIndex = selectedIndex,
                        Player = player,
                        Title = "Выберите предмет"
                    };

                    InventoryUI.RenderInventory(inventoryData);
                    needsRedraw = false;
                }

                var key = InputHandler.WaitForKey(
                    ConsoleKey.W, ConsoleKey.UpArrow,
                    ConsoleKey.S, ConsoleKey.DownArrow,
                    ConsoleKey.Q, ConsoleKey.Escape,
                    ConsoleKey.E, ConsoleKey.Enter,
                    ConsoleKey.Spacebar);

                switch (key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        needsRedraw = true;
                        break;

                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                        needsRedraw = true;
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        // При выходе устанавливаем флаг перерисовки
                        GameServices.BufferedRenderer.SetNeedsFullRedraw();
                        return null;

                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        // При выборе предмета также устанавливаем флаг перерисовки
                        GameServices.BufferedRenderer.SetNeedsFullRedraw();
                        return items[selectedIndex];
                }
            }
        }
        public class EquipmentSlotItem
        {
            public string SlotName { get; set; }
            public Equipment Equipment { get; }

            public EquipmentSlotItem(Equipment equipment)
            {
                Equipment = equipment;
            }

            public override string ToString()
            {
                return $"{Equipment.Name}";
            }
        }

        // В класс InventoryUI добавляем этот метод
        public static List<object> PrepareInventoryItems(Player player)
        {
            var allItems = new List<object>();

            // Добавляем экипированные предметы
            if (player.Inventory.Helmet != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Helmet) { SlotName = "Шлем" });

            if (player.Inventory.Armor != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Armor) { SlotName = "Броня" });

            if (player.Inventory.Gloves != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Gloves) { SlotName = "Перчатки" });

            if (player.Inventory.Boots != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Boots) { SlotName = "Ботинки" });

            if (player.Inventory.MainHand != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.MainHand) { SlotName = "Оружие" });

            if (player.Inventory.OffHand != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.OffHand) { SlotName = "Щит" });

            if (player.Inventory.Amulet != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Amulet) { SlotName = "Амулет" });

            if (player.Inventory.Ring1 != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Ring1) { SlotName = "Кольцо 1" });

            if (player.Inventory.Ring2 != null)
                allItems.Add(new EquipmentSlotItem(player.Inventory.Ring2) { SlotName = "Кольцо 2" });

            // Добавляем предметы из инвентаря, исключая экипированные
            foreach (var item in player.Inventory.Items.ToList())
            {
                if (!IsItemEquipped(player, item.Details))
                {
                    allItems.Add(item);
                }
            }

            return allItems;
        }
        private static bool IsItemEquipped(Player player, Item item)
        {
            if (item is Equipment equipment)
            {
                return player.Inventory.Helmet == equipment ||
                       player.Inventory.Armor == equipment ||
                       player.Inventory.Gloves == equipment ||
                       player.Inventory.Boots == equipment ||
                       player.Inventory.MainHand == equipment ||
                       player.Inventory.OffHand == equipment ||
                       player.Inventory.Amulet == equipment ||
                       player.Inventory.Ring1 == equipment ||
                       player.Inventory.Ring2 == equipment;
            }
            return false;
        }
    }
}