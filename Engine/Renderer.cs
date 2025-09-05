// Renderer.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class Renderer
    {
        private readonly IOutputService _output;

        // Константы для позиционирования в бою
        private const int TurnStatusLine = 0;
        private const int TurnNumberLine = 1;
        private const int MonsterHealthLine = 3;
        private const int MonsterSpeedLine = 5;
        private const int CombatLogStartLine = 9;
        private const int PlayerHealthLine = 21;
        private const int PlayerSpeedLine = 22;

        public Renderer(IOutputService outputService)
        {
            _output = outputService ?? throw new ArgumentNullException(nameof(outputService));
        }

        // Основной метод отрисовки игрового мира
        public void RenderGameWorld(Player player, Location location)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (location == null) throw new ArgumentNullException(nameof(location));

            _output.Clear();

            RenderMessages();
            _output.WriteLine("=========================Окружение========================");
            _output.WriteLine($"Текущая локация: {location.Name}");

            if (!string.IsNullOrEmpty(location.Description))
            {
                _output.WriteLine($"\nОписание: {location.Description}");
            }

            RenderCreatures(location);
            RenderGroundItems(location);
            RenderAvailableDirections(location);
        }

        private void RenderMessages()
        {
            if (MessageSystem.messages.Count == 0) return;

            foreach (var message in MessageSystem.messages)
            {
                _output.Write($" - {message}");
            }
            _output.WriteLine("");
        }

        private void RenderCreatures(Location location)
        {
            var monsters = location.FindMonsters();
            if (monsters.Count > 0)
            {
                _output.WriteLine("\nМонстры: ");
                foreach (var monster in monsters)
                {
                    _output.WriteLine($"- {monster.Name} [{monster.Level}].");
                }
            }

            if (location.NPCsHere.Count > 0)
            {
                _output.WriteLine("\nЖители:");
                foreach (var npc in location.NPCsHere)
                {
                    _output.WriteLine($"- {npc.Name}");
                }
            }
        }

        private void RenderGroundItems(Location location)
        {
            if (location.GroundItems.Count == 0) return;

            _output.WriteLine("\nПредметы на земле:");
            foreach (var itemGroup in location.GroundItems.GroupBy(i => i.Details.ID))
            {
                var firstItem = itemGroup.First();
                _output.WriteLine($"- {firstItem.Details.Name} x{itemGroup.Sum(i => i.Quantity)}");
            }
        }

        private void RenderAvailableDirections(Location location)
        {
            _output.WriteLine("=========================Действие=========================");
            _output.WriteLine("Доступные направления: ");

            if (location.LocationToNorth != null) _output.WriteLine("W - Север");
            if (location.LocationToWest != null) _output.WriteLine("A - Запад");
            if (location.LocationToSouth != null) _output.WriteLine("S - Юг");
            if (location.LocationToEast != null) _output.WriteLine("D - Восток");

            _output.WriteLine("| C - Характеристики | I - Сумка | J - Журнал | L - Осмотреться | E - Взаимодействовать | H - Помощь |");
        }

        // Методы для боевой системы
        public void RenderCombatState(Player player, Monster monster, List<string> combatLog, int currentTurn, int playerSpeed, int monsterSpeed)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (monster == null) throw new ArgumentNullException(nameof(monster));

            _output.Clear();
            RenderStaticCombatLayout(player, monster, currentTurn, playerSpeed, monsterSpeed, combatLog);
        }

        public void RenderStaticCombatLayout(Player player, Monster monster, int currentTurn, int playerSpeed, int monsterSpeed, List<string> combatLog = null)
        {
            _output.Clear();
            Console.ResetColor();

            RenderTurnStatus(playerSpeed, monsterSpeed, monster);
            RenderTurnNumber(currentTurn);
            RenderMonsterInfo(monster, monsterSpeed);
            RenderCombatLog(combatLog);
            RenderPlayerInfo(player, playerSpeed);
            RenderCombatActions();
        }

        private void RenderTurnStatus(int playerSpeed, int monsterSpeed, Monster monster)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            string status = playerSpeed >= 100 ? "=== ВАШ ХОД ===" :
                          monsterSpeed >= 100 ? $"=== ХОД {monster.Name.ToUpper()} ===" :
                          "=== НАПОЛНЕНИЕ ШКАЛЫ СКОРОСТИ ===";
            _output.WriteLine(status);
            Console.ResetColor();
        }

        private void RenderTurnNumber(int currentTurn)
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            _output.WriteLine($"================ ХОД {currentTurn} ================");
            Console.ResetColor();
        }

        private void RenderMonsterInfo(Monster monster, int monsterSpeed)
        {
            _output.WriteLine($" [{monster.Name}][{monster.Level}]");
            RenderHealthBar(monster.CurrentHP, monster.MaximumHP, "Здоровье");
            RenderSpeedBar(monsterSpeed, "Скорость");
            RenderStats(monster.Attack, monster.Defence, monster.Agility, monster.EvasionChance);
            _output.WriteLine(new string('=', Console.WindowWidth));
        }

        private void RenderCombatLog(List<string> combatLog)
        {
            Console.ForegroundColor = ConsoleColor.Gray;
            _output.WriteLine("════════════ БОЕВОЙ ЛОГ ════════════");
            Console.ResetColor();

            if (combatLog != null)
            {
                foreach (var message in combatLog)
                {
                    Console.ForegroundColor = GetMessageColor(message);
                    _output.WriteLine(message);
                    Console.ResetColor();
                }
            }

            // Заполняем оставшиеся строки лога
            int logLines = combatLog?.Count ?? 0;
            for (int i = logLines; i < 10; i++)
            {
                _output.WriteLine("");
            }

            Console.ForegroundColor = ConsoleColor.Gray;
            _output.WriteLine(new string('═', Console.WindowWidth));
            Console.ResetColor();
        }

        private void RenderPlayerInfo(Player player, int playerSpeed)
        {
            _output.WriteLine($" [Игрок][{player.Level}]");
            RenderHealthBar(player.CurrentHP, player.TotalMaximumHP, "Здоровье");
            RenderSpeedBar(playerSpeed, "Скорость");
            RenderStats(player.Attack, player.Defence, player.Agility, player.EvasionChance);
        }

        private void RenderCombatActions()
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            _output.WriteLine("=========Действия========");
            _output.WriteLine("| 1 - атаковать | 2 - заклинание |");
            _output.WriteLine("| 3 - защищаться | 4 - бежать |");
            Console.ResetColor();
        }

        private void RenderHealthBar(int current, int max, string label)
        {
            current = Math.Max(current, 0);
            max = Math.Max(max, 1);

            float percentage = (float)current / max;
            int bars = (int)(20 * percentage);
            bars = Math.Clamp(bars, 0, 20);
            int emptyBars = 20 - bars;

            Console.Write($"{label}: [");

            if (percentage > 0.75f)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (percentage > 0.25f)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.Write(new string('█', bars));
            Console.Write(new string('░', emptyBars));
            Console.ResetColor();

            Console.WriteLine($"] {current}/{max}");
        }

        private void RenderSpeedBar(int current, string label)
        {
            float percentage = (float)current / 100;
            int bars = (int)(20 * percentage);
            bars = Math.Clamp(bars, 0, 20);
            int emptyBars = 20 - bars;

            Console.Write($"{label}: [");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(new string('█', bars));
            Console.Write(new string('░', emptyBars));
            Console.ResetColor();
            Console.WriteLine($"] {current}%");
        }

        private void RenderStats(int attack, int defence, int agility, int evasionChance)
        {
            Console.WriteLine($"АТК: {attack} | ЗЩТ: {defence} | ЛОВ: {agility} | УКЛ: {evasionChance}%");
        }

        private ConsoleColor GetMessageColor(string message)
        {
            if (string.IsNullOrEmpty(message)) return ConsoleColor.Gray;

            return message switch
            {
                string s when s.Contains("Вы ") || s.Contains("Вы достигли") => ConsoleColor.Green,
                string s when s.Contains("КРИТИЧЕСКИЙ УДАР") => ConsoleColor.Red,
                string s when s.Contains("ХОД") || s.Contains("====") => ConsoleColor.Yellow,
                string s when s.Contains("Добыча") || s.Contains("Получено") => ConsoleColor.Cyan,
                string s when s.Contains("побежден") || s.Contains("побеждён") => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };
        }

        // Метод для отрисовки экрана персонажа
        // Метод для отрисовки экрана персонажа
        public void RenderCharacterScreen(Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            _output.Clear();

            _output.WriteLine("=============ХАРАКТЕРИСТИКИ ПЕРСОНАЖА=============");
            _output.WriteLine($"Имя: Игрок | Уровень: {player.Level}");
            _output.WriteLine($"Опыт: {player.CurrentEXP}/{player.MaximumEXP}");
            _output.WriteLine($"Здоровье: {player.CurrentHP}/{player.TotalMaximumHP} | Золото: {player.Gold}");

            if (player.ActiveTitle != null)
            {
                _output.WriteLine($"Титул: {player.ActiveTitle.Name}");
            }

            _output.WriteLine("\n-----------------АТРИБУТЫ-----------------");
            _output.WriteLine($"Сил: {player.Attributes.Strength} | Тел: {player.Attributes.Constitution}");
            _output.WriteLine($"Лов: {player.Attributes.Dexterity} | Инт: {player.Attributes.Intelligence}");
            _output.WriteLine($"Муд: {player.Attributes.Wisdom} | Хар: {player.Attributes.Charisma}");

            _output.WriteLine("\n--------------БОЕВЫЕ ПАРАМЕТРЫ--------------");
            _output.WriteLine($"Атака: {player.Attack} | Защита: {player.Defence}");
            _output.WriteLine($"Скорость: {player.Agility}");

            _output.WriteLine("\n-----------------ЭКИПИРОВКА-----------------");
            RenderEquipmentSlot("Оружие", player.Inventory.Weapon, player.Inventory.Weapon?.AttackBonus ?? 0);
            RenderEquipmentSlot("Шлем", player.Inventory.Helmet, player.Inventory.Helmet?.DefenceBonus ?? 0);
            RenderEquipmentSlot("Броня", player.Inventory.Armor, player.Inventory.Armor?.DefenceBonus ?? 0);
            RenderEquipmentSlot("Перчатки", player.Inventory.Gloves, player.Inventory.Gloves?.DefenceBonus ?? 0);
            RenderEquipmentSlot("Ботинки", player.Inventory.Boots, player.Inventory.Boots?.DefenceBonus ?? 0);

            // Сокращаем отображение бонусов
            _output.WriteLine("\n---------------БОНУСЫ ОТ ЭКИПИРОВКИ---------------");
            int totalAttackBonus = player.Inventory.CalculateTotalAttack();
            int totalDefenceBonus = player.Inventory.CalculateTotalDefence();
            int totalAgilityBonus = player.Inventory.CalculateTotalAgility();
            int totalHealthBonus = player.Inventory.CalculateTotalHealth();

            _output.WriteLine($"АТК: +{totalAttackBonus} | ЗЩТ: +{totalDefenceBonus}");
            _output.WriteLine($"ЛОВ: +{totalAgilityBonus} | ЗДР: +{totalHealthBonus}");

            // Сокращаем статистику
            _output.WriteLine("\n-----------------СТАТИСТИКА-----------------");
            _output.WriteLine($"Монстры: {player.MonstersKilled} | Квесты: {player.QuestsCompleted}");

            // Добавляем подсказку для управления
            _output.WriteLine("\nQ - закрыть, I - инвентарь, S - навыки, T - титулы");
        }
        private void RenderEquipmentSlot(string slotName, Equipment equipment, int bonus)
        {
            string equipmentName = equipment?.Name ?? "Пусто";
            string bonusText = bonus > 0 ? $"(+{bonus})" : "";
            _output.WriteLine($"{slotName.PadRight(10)}: {equipmentName.PadRight(20)} {bonusText}");
        }

        // Метод для отрисовки инвентаря
        public void RenderInventory(InventoryRenderData inventoryData)
        {
            if (inventoryData == null) throw new ArgumentNullException(nameof(inventoryData));

            _output.Clear();

            int windowWidth = Console.WindowWidth;
            int dividerPosition = windowWidth / 2;

            // Заголовок
            _output.WriteLine("=========СУМКА=========");
            _output.WriteLine("");

            // Левая колонка - предметы
            int maxLeftItems = Math.Min(inventoryData.Items.Count, Console.WindowHeight - 15);

            for (int i = 0; i < maxLeftItems; i++)
            {
                string displayText = GetItemDisplayText(inventoryData.Items[i]);
                if (displayText.Length > dividerPosition - 3)
                    displayText = displayText.Substring(0, dividerPosition - 6) + "...";

                if (i == inventoryData.SelectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    _output.Write("> ");
                }
                else
                {
                    _output.Write("  ");
                }

                _output.WriteLine(displayText);
                Console.ResetColor();
            }

            // Заполняем оставшиеся строки в левой колонке
            for (int i = maxLeftItems; i < Console.WindowHeight - 15; i++)
            {
                _output.WriteLine("");
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

            // Отрисовываем правую колонку с правильным позиционированием
            for (int i = 0; i < rightContent.Length && i < Console.WindowHeight - 1; i++)
            {
                _output.SetCursorPosition(dividerPosition + 1, i + 1);
                _output.Write(rightContent[i]);
            }

            // Управление
            string controls = "W/S - Выбор  |  E - Действие  |  Q - Назад";
            int controlsX = (windowWidth - controls.Length) / 2;
            _output.SetCursorPosition(controlsX, Console.WindowHeight - 2);
            _output.Write(controls);
        }
        private string GetItemDisplayText(object item)
        {
            return item switch
            {
                InventoryItem invItem => $"{invItem.Details.Name} x{invItem.Quantity}",
                InventoryUI.EquipmentSlotItem eqItem => $"[Экипировано] {eqItem}",
                _ => item.ToString() ?? string.Empty
            };
        }

        public void ForceInventoryRedraw()
        {
            // Метод оставлен для совместимости, но теперь не нужен
        }

        // В класс Renderer добавить:
        public void RenderMenu(List<string> options, int selectedIndex, string title, string instructions = "")
        {
            _output.Clear();

            _output.WriteLine($"====== {title} ======");
            if (!string.IsNullOrEmpty(instructions))
            {
                _output.WriteLine(instructions);
                _output.WriteLine("");
            }

            for (int i = 0; i < options.Count; i++)
            {
                if (i == selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    _output.Write("> ");
                }
                else
                {
                    _output.Write("  ");
                }
                _output.WriteLine(options[i]);
                Console.ResetColor();
            }
        }
    }
}