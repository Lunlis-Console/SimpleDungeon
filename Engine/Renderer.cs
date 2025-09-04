// Renderer.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class Renderer
    {
        private readonly IOutputService _output;
        private readonly DoubleBufferConsole _doubleBuffer;
        private readonly bool _useDoubleBuffering = true;

        // Константы для позиционирования в бою
        private const int TurnStatusLine = 0;
        private const int TurnNumberLine = 1;
        private const int MonsterHealthLine = 3;
        private const int MonsterSpeedLine = 5;
        private const int CombatLogStartLine = 9;
        private const int PlayerHealthLine = 21;
        private const int PlayerSpeedLine = 22;

        // Отслеживание состояния инвентаря
        private int _lastSelectedIndex = -1;
        private bool _inventoryNeedsFullRedraw = true;
        private readonly int _dividerPosition;

        public Renderer(IOutputService outputService)
        {
            _output = outputService ?? throw new ArgumentNullException(nameof(outputService));
            _doubleBuffer = new DoubleBufferConsole();
            _dividerPosition = Console.WindowWidth / 2;
        }

        // Основной метод отрисовки игрового мира
        public void RenderGameWorld(Player player, Location location)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (location == null) throw new ArgumentNullException(nameof(location));

            var output = GetOutputService();
            output.Clear();

            RenderMessages(output);
            output.WriteLine("=========================Окружение========================");
            output.WriteLine($"Текущая локация: {location.Name}");

            if (!string.IsNullOrEmpty(location.Description))
            {
                output.WriteLine($"\nОписание: {location.Description}");
            }

            RenderCreatures(output, location);
            RenderGroundItems(output, location);
            RenderAvailableDirections(output, location);

            RenderIfDoubleBuffering();
        }

        public void RenderMessages(IOutputService output)
        {
            if (MessageSystem.messages.Count == 0) return;

            foreach (var message in MessageSystem.messages)
            {
                output.Write($" - {message}");
            }
            output.WriteLine("");
        }

        private void RenderCreatures(IOutputService output, Location location)
        {
            var monsters = location.FindMonsters();
            if (monsters.Count > 0)
            {
                output.WriteLine("\nМонстры: ");
                foreach (var monster in monsters)
                {
                    output.WriteLine($"- {monster.Name} [{monster.Level}].");
                }
            }

            if (location.NPCsHere.Count > 0)
            {
                output.WriteLine("\nЖители:");
                foreach (var npc in location.NPCsHere)
                {
                    output.WriteLine($"- {npc.Name}");
                }
            }
        }

        private void RenderGroundItems(IOutputService output, Location location)
        {
            if (location.GroundItems.Count == 0) return;

            output.WriteLine("\nПредметы на земле:");
            foreach (var itemGroup in location.GroundItems.GroupBy(i => i.Details.ID))
            {
                var firstItem = itemGroup.First();
                output.WriteLine($"- {firstItem.Details.Name} x{itemGroup.Sum(i => i.Quantity)}");
            }
        }

        private void RenderAvailableDirections(IOutputService output, Location location)
        {
            output.WriteLine("=========================Действие=========================");
            output.WriteLine("Доступные направления: ");

            if (location.LocationToNorth != null) output.WriteLine("W - Север");
            if (location.LocationToWest != null) output.WriteLine("A - Запад");
            if (location.LocationToSouth != null) output.WriteLine("S - Юг");
            if (location.LocationToEast != null) output.WriteLine("D - Восток");

            output.WriteLine("| C - Характеристики | I - Сумка | J - Журнал | L - Осмотреться | E - Взаимодействовать | H - Помощь |");
        }

        // Методы для боевой системы
        public void RenderCombatState(Player player, Monster monster, List<string> combatLog, int currentTurn, int playerSpeed, int monsterSpeed)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));
            if (monster == null) throw new ArgumentNullException(nameof(monster));

            var output = GetOutputService();
            output.Clear();
            RenderStaticCombatLayout(player, monster, currentTurn, playerSpeed, monsterSpeed, combatLog);
        }

        public void RenderStaticCombatLayout(Player player, Monster monster, int currentTurn, int playerSpeed, int monsterSpeed, List<string> combatLog = null)
        {
            var output = GetOutputService();
            output.Clear();
            Console.ResetColor();

            RenderTurnStatus(playerSpeed, monsterSpeed, monster);
            RenderTurnNumber(currentTurn);
            RenderMonsterInfo(monster, monsterSpeed);
            RenderCombatLog(combatLog);
            RenderPlayerInfo(player, playerSpeed);
            RenderCombatActions();

            RenderIfDoubleBuffering();
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

        // Методы для обновления отдельных элементов в бою
        public void UpdateAtPosition(int line, Action drawAction)
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            try
            {
                ClearLine(line);
                Console.SetCursorPosition(0, line);
                drawAction();
            }
            finally
            {
                Console.SetCursorPosition(originalLeft, originalTop);
            }
        }

        public void ClearLine(int line)
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            try
            {
                Console.SetCursorPosition(0, line);
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(originalLeft, originalTop);
            }
            catch (ArgumentOutOfRangeException)
            {
                // Игнорируем ошибку позиционирования
            }
        }

        public void UpdateTurnStatus(Player player, Monster monster)
        {
            UpdateAtPosition(TurnStatusLine, () =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                if (player.CurrentSpeed >= 100)
                    Console.Write("=== ВАШ ХОД ===");
                else if (monster.CurrentSpeed >= 100)
                    Console.Write($"=== ХОД {monster.Name.ToUpper()} ===");
                else
                    Console.Write("=== НАПОЛНЕНИЕ ШКАЛЫ СКОРОСТИ ===");
                Console.ResetColor();
            });
        }

        public void UpdateTurnNumber(int currentTurn)
        {
            UpdateAtPosition(TurnNumberLine, () =>
            {
                Console.ForegroundColor = ConsoleColor.Yellow;
                Console.Write($"================ ХОД {currentTurn} ================");
                Console.ResetColor();
            });
        }

        public void UpdateHealthBar(int current, int max, int line, string label)
        {
            if (line < 0) return;

            UpdateAtPosition(line, () =>
            {
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, line);
                RenderHealthBar(current, max, label);
            });
        }

        public void UpdateSpeedBar(int current, int line, string label)
        {
            if (line < 0) return;

            UpdateAtPosition(line, () =>
            {
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, line);
                RenderSpeedBar(current, label);
            });
        }

        public void UpdateCombatLog(List<string> combatLog)
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            // Очищаем область лога
            for (int i = 0; i < 10; i++)
            {
                Console.SetCursorPosition(0, CombatLogStartLine + i);
                Console.Write(new string(' ', Console.WindowWidth));
            }

            // Выводим новые сообщения с цветом
            for (int i = 0; i < combatLog.Count; i++)
            {
                Console.SetCursorPosition(0, CombatLogStartLine + i);
                Console.ForegroundColor = GetMessageColor(combatLog[i]);
                Console.Write($" {combatLog[i]}");
                Console.ResetColor();
            }

            Console.SetCursorPosition(originalLeft, originalTop);
        }

        // Метод для отрисовки экрана персонажа
        public void RenderCharacterScreen(Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            var output = GetOutputService();
            output.Clear();

            output.WriteLine("=============ХАРАКТЕРИСТИКИ ПЕРСОНАЖА=============");
            output.WriteLine($"Имя: Игрок");
            output.WriteLine($"Уровень: {player.Level}");
            output.WriteLine($"Опыт: {player.CurrentEXP}/{player.MaximumEXP}");
            output.WriteLine($"Здоровье: {player.CurrentHP}/{player.TotalMaximumHP}");
            output.WriteLine($"Золото: {player.Gold}");

            if (player.ActiveTitle != null)
            {
                output.WriteLine($"Титул: {player.ActiveTitle.Name}");
            }

            output.WriteLine("\n-----------------АТРИБУТЫ-----------------");
            output.WriteLine($"Сила: {player.Attributes.Strength}");
            output.WriteLine($"Телосложение: {player.Attributes.Constitution}");
            output.WriteLine($"Ловкость: {player.Attributes.Dexterity}");
            output.WriteLine($"Интеллект: {player.Attributes.Intelligence}");
            output.WriteLine($"Мудрость: {player.Attributes.Wisdom}");
            output.WriteLine($"Харизма: {player.Attributes.Charisma}");

            output.WriteLine("\n--------------БОЕВЫЕ ПАРАМЕТРЫ--------------");
            output.WriteLine($"Атака: {player.Attack}");
            output.WriteLine($"Защита: {player.Defence}");
            output.WriteLine($"Скорость: {player.Agility}");

            output.WriteLine("\n-----------------ЭКИПИРОВКА-----------------");
            RenderEquipmentSlot(output, "Оружие", player.Inventory.Weapon, player.Inventory.Weapon?.AttackBonus ?? 0);
            RenderEquipmentSlot(output, "Шлем", player.Inventory.Helmet, player.Inventory.Helmet?.DefenceBonus ?? 0);
            RenderEquipmentSlot(output, "Броня", player.Inventory.Armor, player.Inventory.Armor?.DefenceBonus ?? 0);
            RenderEquipmentSlot(output, "Перчатки", player.Inventory.Gloves, player.Inventory.Gloves?.DefenceBonus ?? 0);
            RenderEquipmentSlot(output, "Ботинки", player.Inventory.Boots, player.Inventory.Boots?.DefenceBonus ?? 0);

            output.WriteLine("\n---------------БОНУСЫ ОТ ЭКИПИРОВКИ---------------");
            int totalAttackBonus = player.Inventory.CalculateTotalAttack();
            int totalDefenceBonus = player.Inventory.CalculateTotalDefence();
            int totalAgilityBonus = player.Inventory.CalculateTotalAgility();
            int totalHealthBonus = player.Inventory.CalculateTotalHealth();

            output.WriteLine($"Суммарная атака: {player.Attack} (+{totalAttackBonus} от экипировки)");
            output.WriteLine($"Суммарная защита: {player.Defence} (+{totalDefenceBonus} от экипировки)");
            output.WriteLine($"Суммарная ловкость: {player.Agility} (+{totalAgilityBonus} от экипировки)");
            output.WriteLine($"Суммарное здоровье: {player.TotalMaximumHP} (+{totalHealthBonus} от экипировки)");

            output.WriteLine("\n-----------------СТАТИСТИКА-----------------");
            output.WriteLine($"Всего убито монстров: {player.MonstersKilled}");
            output.WriteLine($"Всего выполнено квестов: {player.QuestsCompleted}");

            RenderIfDoubleBuffering();
        }

        private void RenderEquipmentSlot(IOutputService output, string slotName, Equipment equipment, int bonus)
        {
            string equipmentName = equipment?.Name ?? "Пусто";
            string bonusText = bonus > 0 ? $"(+{bonus})" : "";
            output.WriteLine($"{slotName.PadRight(10)}: {equipmentName.PadRight(20)} {bonusText}");
        }

        // Метод для отрисовки главного меню
        public void RenderMainMenu(string[] menuItems, int selectedIndex)
        {
            if (menuItems == null) throw new ArgumentNullException(nameof(menuItems));

            var output = GetOutputService();
            output.Clear();

            output.WriteLine("===================================");
            output.WriteLine("          SIMPLE DUNGEON");
            output.WriteLine("===================================");

            for (int i = 0; i < menuItems.Length; i++)
            {
                if (i == selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Green;
                    output.Write(">");
                }
                else
                {
                    output.Write("  ");
                }

                output.WriteLine(menuItems[i]);
                Console.ResetColor();
            }

            output.WriteLine("===================================");
            RenderIfDoubleBuffering();
        }

        // Метод для отрисовки меню загрузки
        public void RenderLoadMenu(List<string> saves, int selectedIndex)
        {
            var output = GetOutputService();
            output.Clear();

            output.WriteLine("====== ЗАГРУЗИТЬ ИГРУ ======");

            if (saves == null || saves.Count == 0)
            {
                output.WriteLine("Нет доступных сохранений!");
            }
            else
            {
                for (int i = 0; i < saves.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        output.Write(">");
                    }
                    else
                    {
                        output.Write("  ");
                    }

                    output.WriteLine(saves[i]);
                    Console.ResetColor();
                }
            }

            RenderIfDoubleBuffering();
        }

        // Метод для отрисовки инвентаря
        public void RenderInventory(InventoryRenderData inventoryData)
        {
            if (inventoryData == null) throw new ArgumentNullException(nameof(inventoryData));

            if (!_useDoubleBuffering)
            {
                RenderInventoryDirect(inventoryData);
                return;
            }

            RenderInventoryOptimized(inventoryData);
        }

        private void RenderInventoryOptimized(InventoryRenderData inventoryData)
        {
            _doubleBuffer.BeginBuffer();

            try
            {
                if (_inventoryNeedsFullRedraw)
                {
                    RenderInventoryBuffered(inventoryData);
                    _inventoryNeedsFullRedraw = false;
                }
                else
                {
                    UpdateInventorySelection(inventoryData);
                }
            }
            finally
            {
                _doubleBuffer.EndBuffer();
                _doubleBuffer.Render();
            }
        }

        private void UpdateInventorySelection(InventoryRenderData inventoryData)
        {
            if (_lastSelectedIndex >= 0 && _lastSelectedIndex < inventoryData.Items.Count)
            {
                int y = _lastSelectedIndex + 1;
                _doubleBuffer.SetCursorPosition(0, y);
                _doubleBuffer.Write(" ");
                string displayText = GetItemDisplayText(inventoryData.Items[_lastSelectedIndex]);
                _doubleBuffer.Write(displayText.PadRight(_dividerPosition - 3));
            }

            if (inventoryData.SelectedIndex >= 0 && inventoryData.SelectedIndex < inventoryData.Items.Count)
            {
                int y = inventoryData.SelectedIndex + 1;
                _doubleBuffer.SetCursorPosition(0, y);
                _doubleBuffer.Write(">");
                string displayText = GetItemDisplayText(inventoryData.Items[inventoryData.SelectedIndex]);
                _doubleBuffer.Write(displayText.PadRight(_dividerPosition - 3));
            }

            _lastSelectedIndex = inventoryData.SelectedIndex;
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
            _inventoryNeedsFullRedraw = true;
            _lastSelectedIndex = -1;
        }

        private void RenderInventoryBuffered(InventoryRenderData inventoryData)
        {
            _doubleBuffer.Clear();

            int windowWidth = Console.WindowWidth;
            int dividerPosition = windowWidth / 2;

            // Заголовок
            _doubleBuffer.SetCursorPosition(0, 0);
            _doubleBuffer.Write("=========СУМКА=========");

            // Левая колонка - предметы
            for (int i = 0; i < inventoryData.Items.Count && i < Console.WindowHeight - 3; i++)
            {
                int y = i + 1;
                _doubleBuffer.SetCursorPosition(0, y);

                string displayText = GetItemDisplayText(inventoryData.Items[i]);
                if (displayText.Length > dividerPosition - 3)
                    displayText = displayText.Substring(0, dividerPosition - 6) + "...";

                if (i == inventoryData.SelectedIndex)
                {
                    _doubleBuffer.Write(">");
                    _doubleBuffer.Write(displayText.PadRight(dividerPosition - 3));
                }
                else
                {
                    _doubleBuffer.Write(" ");
                    _doubleBuffer.Write(displayText.PadRight(dividerPosition - 3));
                }
            }

            // Правая колонка - экипировка
            string[] rightContent = {
                "======ЭКИПИРОВКА======",
                $"Оружие: {(inventoryData.MainHand?.Name ?? "Пусто")}",
                // ... остальной контент
            };

            for (int i = 0; i < rightContent.Length && i < Console.WindowHeight - 1; i++)
            {
                _doubleBuffer.SetCursorPosition(dividerPosition + 1, i);
                _doubleBuffer.Write(rightContent[i].PadRight(windowWidth - dividerPosition - 1));
            }

            // Разделительная линия
            for (int y = 0; y < Console.WindowHeight; y++)
            {
                _doubleBuffer.SetCursorPosition(dividerPosition, y);
                _doubleBuffer.Write("│");
            }

            // Управление
            string controls = "W/S - Выбор  |  E - Действие  |  Q - Назад";
            int controlsX = (windowWidth - controls.Length) / 2;
            _doubleBuffer.SetCursorPosition(controlsX, Console.WindowHeight - 2);
            _doubleBuffer.Write(controls);
        }

        private void RenderInventoryDirect(InventoryRenderData inventoryData)
        {
            _output.Clear();
            // Реализация прямого рендеринга инвентаря
        }

        // Вспомогательные методы
        private IOutputService GetOutputService()
        {
            return _useDoubleBuffering ? _doubleBuffer : _output;
        }

        private void RenderIfDoubleBuffering()
        {
            if (_useDoubleBuffering)
            {
                _doubleBuffer.Render();
            }
        }
    }
}