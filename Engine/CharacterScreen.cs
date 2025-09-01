using System;
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

                Console.WriteLine("\nQ - закрыть, I - инвентарь, S - навыки");
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
                }
            }
        }

        private static void DisplayCharacterDetails(Player player)
        {
            Console.WriteLine("=============ХАРАКТЕРИСТИКИ ПЕРСОНАЖА=============");

            // Основные параметры
            Console.WriteLine($"Имя: Игрок");
            Console.WriteLine($"Уровень: {player.Level}");
            Console.WriteLine($"Опыт: {player.CurrentEXP}/{player.MaximumEXP}");
            Console.WriteLine($"Здоровье: {player.CurrentHP}/{player.MaximumHP}");
            Console.WriteLine($"Золото: {player.Gold}");

            Console.WriteLine("\n--------------БОЕВЫЕ ПАРАМЕТРЫ--------------");
            Console.WriteLine($"Атака: {player.Attack}");
            Console.WriteLine($"Защита: {player.Defence}");

            Console.WriteLine("\n-----------------ЭКИПИРОВКА-----------------");
            DisplayEquipmentSlot("Оружие", player.EquipmentWeapon, player.Attack);
            DisplayEquipmentSlot("Шлем", player.EquipmentHelmet, player.EquipmentHelmet?.DefenceBonus ?? 0);
            DisplayEquipmentSlot("Броня", player.EquipmentArmor, player.EquipmentArmor?.DefenceBonus ?? 0);
            DisplayEquipmentSlot("Перчатки", player.EquipmentGloves, player.EquipmentGloves?.DefenceBonus ?? 0);
            DisplayEquipmentSlot("Ботинки", player.EquipmentBoots, player.EquipmentBoots?.DefenceBonus ?? 0);

            Console.WriteLine("\n---------------БОНУСЫ ОТ ЭКИПИРОВКИ---------------");
            int totalAttackBonus = player.EquipmentWeapon?.AttackBonus ?? 0;
            int totalDefenceBonus = (player.EquipmentHelmet?.DefenceBonus ?? 0) +
                                  (player.EquipmentArmor?.DefenceBonus ?? 0) +
                                  (player.EquipmentGloves?.DefenceBonus ?? 0) +
                                  (player.EquipmentBoots?.DefenceBonus ?? 0);

            Console.WriteLine($"Суммарная атака: {player.Attack} (+{totalAttackBonus} от экипировки)");
            Console.WriteLine($"Суммарная защита: {player.Defence} (+{totalDefenceBonus} от экипировки)");

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