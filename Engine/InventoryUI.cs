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
                "======СТАТИСТИКА======",
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
                EquipmentSlotItem eqItem => $"[Экипировано] {eqItem.SlotName}: {eqItem.Equipment.Name}",
                _ => item?.ToString() ?? "Неизвестный предмет"
            };
        }
        public static void ShowItemContextMenu(Player player, InventoryItem selectedItem)
        {
            if (selectedItem == null) return;

            List<string> actions = new List<string>();

            // Добавляем доступные действия в зависимости от типа предмета
            if (selectedItem.Details.Type == ItemType.Consumable)
            {
                actions.Add("Использовать");
            }

            if (selectedItem.Details is Equipment)
            {
                actions.Add("Надеть");
            }

            actions.Add("Осмотреть");
            actions.Add("Выбросить");
            actions.Add("Назад");

            // Показываем меню и обрабатываем выбор
            string selectedAction = SelectActionFromListOverlay(actions, $"Предмет: {selectedItem.Details.Name} x{selectedItem.Quantity}");

            if (selectedAction == null || selectedAction == "Назад")
            {
                return;
            }

            // Обрабатываем выбранное действие
            switch (selectedAction)
            {
                case "Использовать":
                    player.UseItemToHeal(selectedItem);
                    break;

                case "Надеть":
                    player.EquipItem(selectedItem);
                    break;

                case "Осмотреть":
                    selectedItem.Details.Read();
                    // После осмотра снова показываем меню
                    ShowItemContextMenu(player, selectedItem);
                    break;

                case "Выбросить":
                    HandleItemDiscard(player, selectedItem);
                    // Если предмет не полностью выброшен, показываем меню снова
                    if (selectedItem.Quantity > 0)
                    {
                        ShowItemContextMenu(player, selectedItem);
                    }
                    break;
            }
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

                MessageSystem.DisplayMessages();

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

        private static string SelectActionFromListOverlay(List<string> actions, string title)
        {
            if (actions == null || actions.Count == 0)
                return null;

            if (actions.Count == 1)
                return actions[0];

            int selectedIndex = 0;
            ConsoleKey key;

            // Используем основной рендерер вместо создания нового
            var renderer = GameServices.BufferedRenderer;

            try
            {
                do
                {
                    renderer.BeginFrame();

                    // Позиционируем меню по центру экрана
                    int menuWidth = Math.Max(title.Length + 4, actions.Max(a => a.Length) + 6);
                    int menuHeight = actions.Count + 6;
                    int left = (Console.WindowWidth - menuWidth) / 2;
                    int top = (Console.WindowHeight - menuHeight) / 2;

                    // Рендерим полупрозрачный фон для меню
                    for (int y = top; y < top + menuHeight; y++)
                    {
                        for (int x = left; x < left + menuWidth; x++)
                        {
                            if (x >= 0 && x < Console.WindowWidth && y >= 0 && y < Console.WindowHeight)
                            {
                                renderer.Write(x, y, " ", ConsoleColor.White, ConsoleColor.DarkGray);
                            }
                        }
                    }

                    // Рамка меню
                    renderer.Write(left, top, "╔" + new string('═', menuWidth - 2) + "╗", ConsoleColor.White);
                    renderer.Write(left, top + menuHeight - 1, "╚" + new string('═', menuWidth - 2) + "╝", ConsoleColor.White);

                    for (int y = top + 1; y < top + menuHeight - 1; y++)
                    {
                        renderer.Write(left, y, "║", ConsoleColor.White);
                        renderer.Write(left + menuWidth - 1, y, "║", ConsoleColor.White);
                    }

                    // Заголовок
                    renderer.Write(left + 2, top + 2, title, ConsoleColor.Yellow);

                    // Пункты меню
                    for (int i = 0; i < actions.Count; i++)
                    {
                        int yPos = top + 4 + i;
                        string prefix = i == selectedIndex ? "> " : "  ";
                        ConsoleColor color = i == selectedIndex ? ConsoleColor.Green : ConsoleColor.White;
                        renderer.Write(left + 2, yPos, prefix + actions[i], color);
                    }

                    // Подсказка управления
                    renderer.Write(left + 2, top + menuHeight - 2, "W/S - Выбор, E - Выбрать, Q - Назад", ConsoleColor.DarkGray);

                    renderer.EndFrame();

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
                            return "Назад";
                        case ConsoleKey.E:
                            return actions[selectedIndex];
                    }
                } while (true);
            }
            finally
            {
                // После закрытия меню принудительно перерисовываем основной экран
                ScreenManager.RequestFullRedraw();
            }
        }
        private static void RenderMenuOverlay(int left, int top, int width, int height)
        {
            ConsoleColor prevBg = Console.BackgroundColor;
            ConsoleColor prevFg = Console.ForegroundColor;

            // Полупрозрачный темный фон
            Console.BackgroundColor = ConsoleColor.DarkGray;
            Console.ForegroundColor = ConsoleColor.White;

            for (int y = top; y < top + height; y++)
            {
                Console.SetCursorPosition(left, y);
                Console.Write(new string(' ', width));
            }

            // Рамка
            Console.SetCursorPosition(left, top);
            Console.Write("╔" + new string('═', width - 2) + "╗");

            Console.SetCursorPosition(left, top + height - 1);
            Console.Write("╚" + new string('═', width - 2) + "╝");

            for (int y = top + 1; y < top + height - 1; y++)
            {
                Console.SetCursorPosition(left, y);
                Console.Write("║");
                Console.SetCursorPosition(left + width - 1, y);
                Console.Write("║");
            }

            Console.BackgroundColor = prevBg;
            Console.ForegroundColor = prevFg;
        }
        private static void HandleItemDiscard(Player player, InventoryItem item)
        {
            if (item.Quantity > 1)
            {
                Console.Write($"Сколько выбросить? (1-{item.Quantity}): ");
                if (int.TryParse(Console.ReadLine(), out int amount) && amount > 0 && amount <= item.Quantity)
                {
                    item.Quantity -= amount;
                    if (item.Quantity <= 0)
                    {
                        player.Inventory.RemoveItem(item);
                    }
                    Console.WriteLine($"Выброшено {amount} шт. предмета {item.Details.Name}");
                }
            }
            else
            {
                player.Inventory.RemoveItem(item);
                Console.WriteLine($"Предмет {item.Details.Name} выброшен");
            }
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
            public string SlotName { get; }
            public Equipment Equipment { get; }

            public EquipmentSlotItem(string slotName, Equipment equipment)
            {
                SlotName = slotName;
                Equipment = equipment;
            }

            public override string ToString()
            {
                return $"{SlotName}: {Equipment.Name}";
            }
        }

        // В класс InventoryUI добавляем этот метод
        public static List<object> PrepareInventoryItems(Player player)
        {
            var allItems = new List<object>();

            // Добавляем экипированные предметы как EquipmentSlotItem
            if (player.Inventory.Helmet != null)
                allItems.Add(new EquipmentSlotItem("Шлем", player.Inventory.Helmet));
            if (player.Inventory.Armor != null)
                allItems.Add(new EquipmentSlotItem("Броня", player.Inventory.Armor));
            if (player.Inventory.Gloves != null)
                allItems.Add(new EquipmentSlotItem("Перчатки", player.Inventory.Gloves));
            if (player.Inventory.Boots != null)
                allItems.Add(new EquipmentSlotItem("Ботинки", player.Inventory.Boots));
            if (player.Inventory.MainHand != null)
                allItems.Add(new EquipmentSlotItem("Оружие", player.Inventory.MainHand));
            if (player.Inventory.OffHand != null)
                allItems.Add(new EquipmentSlotItem("Щит", player.Inventory.OffHand));
            if (player.Inventory.Amulet != null)
                allItems.Add(new EquipmentSlotItem("Амулет", player.Inventory.Amulet));
            if (player.Inventory.Ring1 != null)
                allItems.Add(new EquipmentSlotItem("Кольцо 1", player.Inventory.Ring1));
            if (player.Inventory.Ring2 != null)
                allItems.Add(new EquipmentSlotItem("Кольцо 2", player.Inventory.Ring2));

            // Добавляем предметы из инвентаря
            allItems.AddRange(player.Inventory.Items.Cast<object>());

            return allItems;
        }

        private static char[,] SaveScreenArea(int left, int top, int width, int height)
        {
            var buffer = new char[width, height];

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (top + y < Console.WindowHeight && left + x < Console.WindowWidth)
                    {
                        Console.SetCursorPosition(left + x, top + y);
                        buffer[x, y] = (char)Console.Read();
                    }
                }
            }

            return buffer;
        }

        private static void RestoreScreenArea(char[,] buffer, int left, int top)
        {
            int width = buffer.GetLength(0);
            int height = buffer.GetLength(1);

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    if (top + y < Console.WindowHeight && left + x < Console.WindowWidth)
                    {
                        Console.SetCursorPosition(left + x, top + y);
                        Console.Write(buffer[x, y]);
                    }
                }
            }
        }


    }
}