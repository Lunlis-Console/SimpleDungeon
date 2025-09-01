using System;
using System.Collections.Generic;

namespace Engine
{
    public static class InventoryUI
    {
        public static InventoryItem SelectItemFromInventory(List<InventoryItem> inventory, string title = "Выберите предмет")
        {
            if (inventory == null || inventory.Count == 0)
                return null;

            if (inventory.Count == 1)
                return inventory[0];

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();

                MessageSystem.DisplayMessages();

                Console.WriteLine($"{title}");
                Console.WriteLine("Клавиши 'W' 'S' для выбора, 'E' для подтверждения, 'Q' для выхода");
                Console.WriteLine();

                for (int i = 0; i < inventory.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }

                    Console.WriteLine($"{inventory[i].Details.Name} x{inventory[i].Quantity}");
                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        selectedIndex = (selectedIndex - 1 + inventory.Count) % inventory.Count;
                        break;
                    case ConsoleKey.S:
                        selectedIndex = (selectedIndex + 1) % inventory.Count;
                        break;
                    case ConsoleKey.Q:
                        return null;
                }

            } while (key != ConsoleKey.E);

            return inventory[selectedIndex];
        }

        public static void ShowItemContextMenu(Player player, InventoryItem selectedItem)
        {
            if (selectedItem == null) return;

            MessageSystem.DisplayMessages();

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

            string selectedAction = SelectActionFromList(actions, $"Предмет: {selectedItem.Details.Name} x{selectedItem.Quantity}");

            if (selectedAction == null || selectedAction == "Назад") return;

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
                    break;

                case "Выбросить":
                    HandleItemDiscard(player, selectedItem);
                    break;
            }

            //Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            //Console.ReadKey();
        }

        private static string SelectActionFromList(List<string> actions, string title)
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
                        Console.Write("> ");
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
                        player.Inventory.Remove(item);
                    }
                    Console.WriteLine($"Выброшено {amount} шт. предмета {item.Details.Name}");
                }
            }
            else
            {
                player.Inventory.Remove(item);
                Console.WriteLine($"Предмет {item.Details.Name} выброшен");
            }
        }

        public static InventoryItem SelectItemFromInventoryWithEquipment(
            List<InventoryItem> inventory,
            string title,
            Equipment helmet,
            Equipment armor,
            Equipment gloves,
            Equipment boots,
            Equipment weapon)
        {
            if (inventory == null || inventory.Count == 0)
                return null;
            if(inventory.Count == 1) 
                return inventory[0];

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();

                MessageSystem.DisplayMessages();

                Console.WriteLine($"{title}");

                // Отображаем экипированные предметы
                Console.WriteLine("======Экипировано======");
                Console.WriteLine($"Оружие: {(weapon?.Name ?? "Пусто")}");
                Console.WriteLine($"Голова: {(helmet?.Name ?? "Пусто")}");
                Console.WriteLine($"Тело: {(armor?.Name ?? "Пусто")}");
                Console.WriteLine($"Руки: {(gloves?.Name ?? "Пусто")}");
                Console.WriteLine($"Ноги: {(boots?.Name ?? "Пусто")}");
                Console.WriteLine("========================");

                Console.WriteLine("Клавиши 'W' 'S' для выбора, 'E' для подтверждения, 'Q' для выхода");
                Console.WriteLine();

                for (int i = 0; i < inventory.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }

                    Console.WriteLine($"{inventory[i].Details.Name} x{inventory[i].Quantity}");
                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        selectedIndex = (selectedIndex - 1 + inventory.Count) % inventory.Count;
                        break;
                    case ConsoleKey.S:
                        selectedIndex = (selectedIndex + 1) % inventory.Count;
                        break;
                    case ConsoleKey.Q:
                        return null;
                }

            } while (key != ConsoleKey.E);

            return inventory[selectedIndex];
        }
    }
}