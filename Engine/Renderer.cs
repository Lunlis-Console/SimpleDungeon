// Renderer.cs
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class Renderer
    {
        private readonly IOutputService _output;
        private readonly DoubleBufferRenderer _doubleBuffer;

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
            _doubleBuffer = new DoubleBufferRenderer(outputService);
        }

        public void BeginFrame()
        {
            _doubleBuffer.CheckWindowResize();
            _doubleBuffer.BeginFrame();
        }

        public void EndFrame()
        {
            _doubleBuffer.EndFrame();
        }


        // Основной метод отрисовки игрового мира
        // Модифицируем методы рендеринга для использования буфера
        public void RenderGameWorld(Player player, Location location)
        {
            BeginFrame();

            RenderMessagesToBuffer();
            RenderLocationInfoToBuffer(location);
            RenderCreaturesToBuffer(location);
            RenderGroundItemsToBuffer(location);
            RenderAvailableDirectionsToBuffer(location);

            EndFrame();
        }


        private void RenderMessagesToBuffer()
        {
            if (MessageSystem.messages.Count == 0) return;

            int y = 0;
            foreach (var message in MessageSystem.messages)
            {
                _doubleBuffer.Write(0, y, $" - {message}");
                y++;
            }
        }

        private void RenderLocationInfoToBuffer(Location location)
        {
            int y = MessageSystem.messages.Count + 2;

            _doubleBuffer.Write(0, y, "=========================Окружение========================");
            y++;
            _doubleBuffer.Write(0, y, $"Текущая локация: {location.Name}");
            y++;

            if (!string.IsNullOrEmpty(location.Description))
            {
                _doubleBuffer.Write(0, y, $"Описание: {location.Description}");
                y++;
            }
            y++;
        }

        private void RenderCreaturesToBuffer(Location location)
        {
            int y = MessageSystem.messages.Count + 4;

            var monsters = location.FindMonsters();
            if (monsters.Count > 0)
            {
                _doubleBuffer.Write(0, y, "\nМонстры: ");
                foreach (var monster in monsters)
                {
                    _doubleBuffer.Write(0, y, $"- {monster.Name} [{monster.Level}].");
                }
            }

            if (location.NPCsHere.Count > 0)
            {
                _doubleBuffer.Write(0, y, "\nЖители:");
                foreach (var npc in location.NPCsHere)
                {
                    _doubleBuffer.Write(0, y, $"- {npc.Name}");
                }
            }
        }

        private void RenderGroundItemsToBuffer(Location location)
        {
            int y = MessageSystem.messages.Count + 4;

            if (location.GroundItems.Count == 0) return;

            _doubleBuffer.Write(0, y, "\nПредметы на земле:");
            foreach (var itemGroup in location.GroundItems.GroupBy(i => i.Details.ID))
            {
                var firstItem = itemGroup.First();
                _doubleBuffer.Write(0, y, $"- {firstItem.Details.Name} x{itemGroup.Sum(i => i.Quantity)}");
            }
        }

        private void RenderAvailableDirectionsToBuffer(Location location)
        {
            int y = MessageSystem.messages.Count + 6;

            _doubleBuffer.Write(0, y, "=========================Действие=========================");
            _doubleBuffer.Write(0, y, "Доступные направления: ");

            if (location.LocationToNorth != null) _doubleBuffer.Write(0, y, "W - Север");
            if (location.LocationToWest != null) _doubleBuffer.Write(0, y, "A - Запад");
            if (location.LocationToSouth != null) _doubleBuffer.Write(0, y, "S - Юг");
            if (location.LocationToEast != null) _doubleBuffer.Write(0, y, "D - Восток");

            _doubleBuffer.Write(0, y, "| C - Характеристики | I - Сумка | J - Журнал | L - Осмотреться | E - Взаимодействовать | H - Помощь |");
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

        // Добавляем в Renderer вспомогательные методы
        private void RenderToBuffer(int x, int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            // Сохраняем текущий цвет
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            _doubleBuffer.Write(x, y, text);

            // Восстанавливаем цвет
            Console.ForegroundColor = previousColor;
        }

        private void RenderHealthBarToBuffer(int x, int y, int current, int max, string label = "Здоровье")
        {
            current = Math.Max(current, 0);
            max = Math.Max(max, 1);

            float percentage = (float)current / max;
            int bars = (int)(20 * percentage);
            bars = Math.Clamp(bars, 0, 20);
            int emptyBars = 20 - bars;

            string healthText = $"{current}/{max}";

            string bar = $"{label}: [";

            // Выбираем цвет в зависимости от процента здоровья
            ConsoleColor color = percentage > 0.5f ? ConsoleColor.Green :
                                percentage > 0.25f ? ConsoleColor.Yellow :
                                ConsoleColor.Red;

            bar += new string('█', bars);
            bar += new string('░', emptyBars);
            bar += $"] {healthText}";

            RenderToBuffer(x, y, bar, color);
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
        // Метод для отрисовки экрана персонажа (Классический вариант)
        public void RenderCharacterScreen(Player player)
        {
            if (player == null) throw new ArgumentNullException(nameof(player));

            _output.Clear();

            // Верхний заголовок
            Console.ForegroundColor = ConsoleColor.Yellow;
            _output.WriteLine("=============ХАРАКТЕРИСТИКИ ПЕРСОНАЖА=============");
            Console.ResetColor();

            // Основная информация
            string titleInfo = player.ActiveTitle != null ? $"[{player.ActiveTitle.Name}]" : "";
            _output.WriteLine($"Игрок {titleInfo,-20} Ур. {player.Level}");
            _output.WriteLine($"Опыт: {player.CurrentEXP:N0}/{player.MaximumEXP:N0} {"",-10} Золото: {player.Gold:N0}");

            // Полоса здоровья
            RenderHealthBar(player.CurrentHP, player.TotalMaximumHP, "Здоровье");
            _output.WriteLine("");

            // Атрибуты в две колонки
            Console.ForegroundColor = ConsoleColor.Cyan;
            _output.WriteLine("-----------------ОСНОВНЫЕ АТРИБУТЫ-----------------");
            Console.ResetColor();

            _output.WriteLine($"СИЛ: {player.Attributes.Strength,-2} (+{player.Attributes.Strength - 10,-2}) | ТЕЛ: {player.Attributes.Constitution,-2} (+{player.Attributes.Constitution - 10,-2}) | ЛОВ: {player.Attributes.Dexterity,-2} (+{player.Attributes.Dexterity - 10,-2})");
            _output.WriteLine($"ИНТ: {player.Attributes.Intelligence,-2} (+{player.Attributes.Intelligence - 10,-2}) | МУД: {player.Attributes.Wisdom,-2} (+{player.Attributes.Wisdom - 10,-2}) | ХАР: {player.Attributes.Charisma,-2} (+{player.Attributes.Charisma - 10,-2})");
            _output.WriteLine("");

            // Боевые параметры
            Console.ForegroundColor = ConsoleColor.Cyan;
            _output.WriteLine("--------------БОЕВЫЕ ПАРАМЕТРЫ--------------");
            Console.ResetColor();

            _output.WriteLine($"АТК: {player.Attack,-3} | ЗЩТ: {player.Defence,-3} | СКР: {player.Agility,-3} | УКЛ: {player.EvasionChance}%");
            _output.WriteLine("");

            // Экипировка
            Console.ForegroundColor = ConsoleColor.Cyan;
            _output.WriteLine("-----------------ЭКИПИРОВКА-----------------");
            Console.ResetColor();

            // ОРУЖИЕ - два слота
            RenderEquipmentSlot("Осн. рука", player.Inventory.MainHand, player.Inventory.MainHand?.AttackBonus ?? 0, "АТК");
            RenderEquipmentSlot("Втор. рука", player.Inventory.OffHand, player.Inventory.OffHand?.AttackBonus ?? 0, "АТК");
            // БРОНЯ
            RenderEquipmentSlot("Шлем", player.Inventory.Helmet, player.Inventory.Helmet?.DefenceBonus ?? 0, "ЗЩТ");
            RenderEquipmentSlot("Броня", player.Inventory.Armor, player.Inventory.Armor?.DefenceBonus ?? 0, "ЗЩТ");
            RenderEquipmentSlot("Перчатки", player.Inventory.Gloves, player.Inventory.Gloves?.DefenceBonus ?? 0, "ЗЩТ");
            RenderEquipmentSlot("Ботинки", player.Inventory.Boots, player.Inventory.Boots?.DefenceBonus ?? 0, "ЗЩТ");
            // БИЖУТЕРИЯ
            RenderEquipmentSlot("Амулет", player.Inventory.Amulet, player.Inventory.Amulet?.DefenceBonus ?? 0, "ЗЩТ");
            RenderEquipmentSlot("Кольцо 1", player.Inventory.Ring1, player.Inventory.Ring1?.DefenceBonus ?? 0, "ЗЩТ");
            RenderEquipmentSlot("Кольцо 2", player.Inventory.Ring2, player.Inventory.Ring2?.DefenceBonus ?? 0, "ЗЩТ");
            _output.WriteLine("");

            // Бонусы от экипировки (компактно)
            Console.ForegroundColor = ConsoleColor.Cyan;
            _output.WriteLine("---------------БОНУСЫ ОТ ЭКИПИРОВКИ---------------");
            Console.ResetColor();

            int totalAttackBonus = player.Inventory.CalculateTotalAttack();
            int totalDefenceBonus = player.Inventory.CalculateTotalDefence();
            int totalAgilityBonus = player.Inventory.CalculateTotalAgility();
            int totalHealthBonus = player.Inventory.CalculateTotalHealth();

            _output.WriteLine($"АТК: +{totalAttackBonus,-3} | ЗЩТ: +{totalDefenceBonus,-3} | ЛОВ: +{totalAgilityBonus,-3} | ЗДР: +{totalHealthBonus,-3}");
            _output.WriteLine("");

            // Статистика
            Console.ForegroundColor = ConsoleColor.Cyan;
            _output.WriteLine("-----------------СТАТИСТИКА-----------------");
            Console.ResetColor();

            _output.WriteLine($"Убито монстров: {player.MonstersKilled,-5} | Выполнено квестов: {player.QuestsCompleted}");
            _output.WriteLine("");

            // Управление
            Console.ForegroundColor = ConsoleColor.DarkGray;
            _output.WriteLine("Q - закрыть, I - инвентарь, S - навыки, T - титулы");
            Console.ResetColor();
        }

        // Обновленный метод для отображения слота экипировки
        private void RenderEquipmentSlot(string slotName, Equipment equipment, int bonus, string bonusType)
        {
            string equipmentName = equipment?.Name ?? "Пусто";
            string bonusText = bonus > 0 ? $"(+{bonus} {bonusType})" : "";

            _output.WriteLine($"{slotName,-10}: {equipmentName,-25} {bonusText}");
        }

        // Улучшенный метод для отрисовки полосы здоровья
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