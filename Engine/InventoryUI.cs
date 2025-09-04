using System;
using System.Collections;
using System.Collections.Generic;
using System.Net.NetworkInformation;

namespace Engine
{
    public static class InventoryUI
    {
        //public static object SelectItemFromCombinedList(List<object> items, Player player)
        //{
        //    if (items == null || items.Count == 0)
        //        return null;

        //    if (items.Count == 1)
        //        return items[0];

        //    int selectedIndex = 0;
        //    ConsoleKey key;

        //    do
        //    {
        //        var inventoryData = new InventoryRenderData
        //        {
        //            Items = items,
        //            SelectedIndex = selectedIndex,
        //            Player = player,
        //            Title = "Выберите предмет"
        //        };

        //        GameServices.Renderer.RenderInventory(inventoryData);

        //        key = Console.ReadKey(true).Key;

        //        switch (key)
        //        {
        //            case ConsoleKey.W:
        //            case ConsoleKey.UpArrow:
        //                selectedIndex = Math.Max(0, selectedIndex - 1);
        //                break;

        //            case ConsoleKey.S:
        //            case ConsoleKey.DownArrow:
        //                selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
        //                break;

        //            case ConsoleKey.Q:
        //            case ConsoleKey.Escape:
        //                return null;

        //            case ConsoleKey.E:
        //            case ConsoleKey.Enter:
        //            case ConsoleKey.Spacebar:
        //                return items[selectedIndex];
        //        }

        //    } while (true);
        //}
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

        public static object SelectItemFromCombinedList(List<object> items, Player player)
        {
            if (items == null || items.Count == 0)
                return null;

            if (items.Count == 1)
                return items[0];

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                // Вместо прямой отрисовки - создаем данные для рендерера
                var inventoryData = new InventoryRenderData
                {
                    Items = items,
                    SelectedIndex = selectedIndex,
                    Player = player,
                    Title = "Выберите предмет"
                };

                // Отрисовываем через Renderer
                GameServices.Renderer.RenderInventory(inventoryData);

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        selectedIndex = Math.Max(0, selectedIndex - 1);
                        break;

                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        selectedIndex = Math.Min(items.Count - 1, selectedIndex + 1);
                        break;

                    case ConsoleKey.Tab:
                        // Переключение на экипированные предметы
                        var equipmentIndex = items.FindIndex(item => item is EquipmentSlotItem);
                        if (equipmentIndex >= 0) selectedIndex = equipmentIndex;
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        return null;

                    case ConsoleKey.E:
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        return items[selectedIndex];
                }

            } while (true);
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