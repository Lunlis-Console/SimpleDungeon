using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;

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
                        Console.Write(">");
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

        public static object SelectItemFromCombinedList(
    List<object> items,
    string title,
    Equipment mainHand,
    Equipment offHand,
    Equipment helmet,
    Equipment armor,
    Equipment gloves,
    Equipment boots,
    Equipment weapon,
    Equipment amulet,
    Equipment ring1,
    Equipment ring2,
    int playerGold,
    int playerDefence,
    int playerAttack,
    int playerAgility,
    int playerLevel,
    int playerCurrentEXP,
    int playerMaximumEXP,
    int playerCurrentHP,
    int playerMaximumHP)
        {
            if (items == null || items.Count == 0)
                return null;

            if (items.Count == 1)
                return items[0];

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                MessageSystem.DisplayMessages();

                Console.WriteLine($"{title}");

                // Отображение статистики и экипировки
                int rightColumnStart = Console.WindowWidth - 35;
                Console.SetCursorPosition(rightColumnStart, 1);
                Console.WriteLine("======Экипировано======");
                Console.SetCursorPosition(rightColumnStart, 2);
                Console.WriteLine($"Основная рука: {(mainHand?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 3);
                Console.WriteLine($"Вторая рука: {(offHand?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 4);
                Console.WriteLine($"Шлем: {(helmet?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 5);
                Console.WriteLine($"Броня: {(armor?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 6);
                Console.WriteLine($"Перчатки: {(gloves?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 7);
                Console.WriteLine($"Ботинки: {(boots?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 8);
                Console.WriteLine($"Амулет: {(amulet?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 9);
                Console.WriteLine($"Кольцо 1: {(ring1?.Name ?? "Пусто")}");
                Console.SetCursorPosition(rightColumnStart, 10);
                Console.WriteLine($"Кольцо 2: {(ring2?.Name ?? "Пусто")}");

                Console.SetCursorPosition(rightColumnStart, 11);
                Console.WriteLine("========================");
                Console.SetCursorPosition(rightColumnStart, 12);
                Console.WriteLine($"УР: {playerLevel} ОЗ: {playerCurrentHP}/{playerMaximumHP} " +
                    $"ОП: {playerCurrentEXP}/{playerMaximumEXP}");
                Console.SetCursorPosition(rightColumnStart, 13);
                Console.WriteLine($"Атака: {playerAttack}");
                Console.SetCursorPosition(rightColumnStart, 14);
                Console.WriteLine($"Защита: {playerDefence}");
                Console.SetCursorPosition(rightColumnStart, 15);
                Console.WriteLine($"Ловкость: {playerAgility}");

                Console.SetCursorPosition(0, 1);
                Console.WriteLine("=========Сумка=========");

                // Отображение предметов
                for (int i = 0; i < items.Count; i++)
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

                    string displayText = items[i] switch
                    {
                        InventoryItem invItem => $"{invItem.Details.Name} x{invItem.Quantity}",
                        EquipmentSlotItem eqItem => $"[Экипировано] {eqItem}",
                        _ => items[i].ToString()
                    };

                    Console.WriteLine(displayText);
                    Console.ResetColor();
                }

                Console.WriteLine("======================");
                Console.WriteLine($"Золото: {playerGold}");
                //Console.WriteLine("TAB - переключить на экипированные предметы");

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        selectedIndex = (selectedIndex - 1 + items.Count) % items.Count;
                        break;
                    case ConsoleKey.S:
                        selectedIndex = (selectedIndex + 1) % items.Count;
                        break;
                    case ConsoleKey.Tab:
                        // Переключение на экипированные предметы
                        var equipmentIndex = items.FindIndex(item => item is EquipmentSlotItem);
                        if (equipmentIndex >= 0) selectedIndex = equipmentIndex;
                        break;
                    case ConsoleKey.Q:
                        return null;
                }

            } while (key != ConsoleKey.E);

            return items[selectedIndex];
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

    }
}