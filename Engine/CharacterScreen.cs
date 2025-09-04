﻿using System;
using System.Collections.Generic;

namespace Engine
{
    public static class CharacterScreen
    {
        public static void Show(Player player)
        {
            bool viewing = true;

            while (viewing)
            {
                Console.Clear();
                DisplayCharacterDetails(player);

                Console.WriteLine("\nQ - закрыть, I - инвентарь, S - навыки, T - титулы");
                Console.Write("> ");

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        viewing = false;
                        break;

                    case ConsoleKey.I:
                        player.DisplayInventory();
                        break;

                    case ConsoleKey.S:
                        // Можно добавить меню навыков позже
                        MessageSystem.AddMessage("Система навыков в разработке!");
                        break;

                    case ConsoleKey.T:
                        ShowTitlesMenu(player);
                        break;
                }
            }
        }

        private static void ShowTitlesMenu(Player player)
        {
            var titles = player.UnlockedTitles;

            if (titles.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("У вас нет разблокированных титулов!");
                Console.WriteLine("\nНажмите любую клавишу...");
                Console.ReadKey();
                return;
            }

            var selectedTitle = MenuSystem.SelectFromList(
                titles,
                title => $"{title.Name} {(title.IsActive ? "[АКТИВЕН]" : "")}",
                "====== ВАШИ ТИТУЛЫ ======",
                "Клавиши 'W' 'S' для выбора, 'E' - активировать/деактивировать, 'Q' - назад"
            );

            if (selectedTitle != null)
            {
                if (selectedTitle.IsActive)
                {
                    player.DeactivateTitle();
                }
                else
                {
                    player.ActivateTitle(selectedTitle);
                }

                // Показываем информацию о титуле
                Console.Clear();
                Console.WriteLine($"====== {selectedTitle.Name} ======");
                Console.WriteLine($"Описание: {selectedTitle.Description}");
                Console.WriteLine($"Бонусы: {selectedTitle.GetBonusDescription()}");
                Console.WriteLine($"Требование: {selectedTitle.RequirementAmount} {GetRequirementDescription(selectedTitle)}");
                Console.WriteLine("\nНажмите любую клавишу...");
                Console.ReadKey();
            }
        }

        private static string GetRequirementDescription(Title title)
        {
            switch (title.RequirementType)
            {
                case "MonsterKill": return $"убийств {title.RequirementTarget}";
                case "QuestComplete": return "выполненных квестов";
                case "TotalMonstersKilled": return "убитых монстров";
                default: return "";
            }
        }

        private static void DisplayCharacterDetails(Player player)
        {
            Console.WriteLine("=============ХАРАКТЕРИСТИКИ ПЕРСОНАЖА=============");

            // Основные параметры
            Console.WriteLine($"Имя: Игрок");
            Console.WriteLine($"Уровень: {player.Level}");
            Console.WriteLine($"Опыт: {player.CurrentEXP}/{player.MaximumEXP}");
            Console.WriteLine($"Здоровье: {player.CurrentHP}/{player.TotalMaximumHP}");
            Console.WriteLine($"Золото: {player.Gold}");

            if (player.ActiveTitle != null)
            {
                Console.WriteLine($"Титул: {player.ActiveTitle.Name}");
            }

            // Атрибуты персонажа
            Console.WriteLine("\n-----------------АТРИБУТЫ-----------------");
            Console.WriteLine($"Сила: {player.Attributes.Strength}");
            Console.WriteLine($"Телосложение: {player.Attributes.Constitution}");
            Console.WriteLine($"Ловкость: {player.Attributes.Dexterity}");
            Console.WriteLine($"Интеллект: {player.Attributes.Intelligence}");
            Console.WriteLine($"Мудрость: {player.Attributes.Wisdom}");
            Console.WriteLine($"Харизма: {player.Attributes.Charisma}");

            Console.WriteLine("\n--------------БОЕВЫЕ ПАРАМЕТРЫ--------------");
            Console.WriteLine($"Атака: {player.Attack}");
            Console.WriteLine($"Защита: {player.Defence}");
            Console.WriteLine($"Скорость: {player.Agility}");

            Console.WriteLine("\n-----------------ЭКИПИРОВКА-----------------");
            DisplayEquipmentSlot("Оружие", player.Inventory.Weapon, player.Inventory.Weapon?.AttackBonus ?? 0);
            DisplayEquipmentSlot("Шлем", player.Inventory.Helmet, player.Inventory.Helmet?.DefenceBonus ?? 0);
            DisplayEquipmentSlot("Броня", player.Inventory.Armor, player.Inventory.Armor?.DefenceBonus ?? 0);
            DisplayEquipmentSlot("Перчатки", player.Inventory.Gloves, player.Inventory.Gloves?.DefenceBonus ?? 0);
            DisplayEquipmentSlot("Ботинки", player.Inventory.Boots, player.Inventory.Boots?.DefenceBonus ?? 0);

            Console.WriteLine("\n---------------БОНУСЫ ОТ ЭКИПИРОВКИ---------------");
            int totalAttackBonus = player.Inventory.CalculateTotalAttack();
            int totalDefenceBonus = player.Inventory.CalculateTotalDefence();
            int totalAgilityBonus = player.Inventory.CalculateTotalAgility();
            int totalHealthBonus = player.Inventory.CalculateTotalHealth();

            Console.WriteLine($"Суммарная атака: {player.Attack} (+{totalAttackBonus} от экипировки)");
            Console.WriteLine($"Суммарная защита: {player.Defence} (+{totalDefenceBonus} от экипировки)");
            Console.WriteLine($"Суммарная ловкость: {player.Agility} (+{totalAgilityBonus} от экипировки)");
            Console.WriteLine($"Суммарное здоровье: {player.TotalMaximumHP} (+{totalHealthBonus} от экипировки)");

            // Статистика (можно расширить)
            Console.WriteLine("\n-----------------СТАТИСТИКА-----------------");
            Console.WriteLine($"Всего убито монстров: {player.MonstersKilled}");
            Console.WriteLine($"Всего выполнено квестов: {player.QuestsCompleted}");
        }

        private static void DisplayEquipmentSlot(string slotName, Equipment equipment, int bonus)
        {
            string equipmentName = equipment?.Name ?? "Пусто";
            string bonusText = bonus > 0 ? $"(+{bonus})" : "";

            Console.WriteLine($"{slotName.PadRight(10)}: {equipmentName.PadRight(20)} {bonusText}");
        }
    }
}
