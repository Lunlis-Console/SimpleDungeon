using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class CombatEngine
    {
        public Player Player { get; set; }
        public Monster Monster { get; set; }

        // Сообщения о действиях для отображения в UI
        public string PlayerActionMessage { get; private set; }
        public string MonsterActionMessage { get; private set; }

        public CombatEngine(Player player, Monster monster)
        {
            Player = player;
            Monster = monster;
            Player.IsInCombat = true;
            Player.CurrentMonster = monster;

            PlayerActionMessage = "";
            MonsterActionMessage = "";
        }

        private List<string> _combatLog = new List<string>();
        private const int MaxLogLines = 10;
        private int _actionCounter = 0; // Счетчик действий (вместо ходов)
        private int _currentTurn = 1; // Текущий ход
        private bool _playerActedThisTurn = false;
        private bool _monsterActedThisTurn = false;

        private int monsterSpeedLine = -1;
        private int playerSpeedLine = -1;

        private const int TurnStatusLine = 0;
        private const int TurnNumberLine = 1;
        private const int MonsterHealthLine = 3;
        private const int MonsterSpeedLine = 5;
        private const int CombatLogStartLine = 9;
        private const int PlayerHealthLine = 21;
        private const int PlayerSpeedLine = 22;


        public void CombatLoop()
        {
            // Инициализация и отрисовка статического layout
            Player.CurrentSpeed = 0;
            Monster.CurrentSpeed = 0;
            _actionCounter = 0;
            _currentTurn = 1;

            // Определяем кто медленнее (ДОБАВИТЬ ЭТО)
            int slowerAgility = Math.Min(Player.Agility, Monster.Agility);
            bool playerIsSlower = Player.Agility == slowerAgility;

            RenderStaticCombatLayout();
            AddToCombatLog($"=== ХОД {_currentTurn} ===");
            UpdateCombatLog();

            System.Threading.Thread.Sleep(250);

            while (Player.IsInCombat && Player.CurrentHP > 0 && Monster.CurrentHP > 0)
            {
                UpdateSpeedMeters();

                // Обновляем только изменяющиеся элементы
                UpdateTurnStatus();
                UpdateSpeedBar(Monster.CurrentSpeed, monsterSpeedLine, "Скорость");
                UpdateSpeedBar(Player.CurrentSpeed, playerSpeedLine, "Скорость");

                if (Monster.CurrentSpeed >= 100)
                {
                    // ЗАДЕРЖКА ПЕРЕД ДЕЙСТВИЕМ МОНСТРА
                    UpdateTurnStatus(); // "ХОД МОНСТРА"
                    System.Threading.Thread.Sleep(250);

                    MonsterTurn();
                    UpdateCombatLog();
                    UpdateHealthBar(Monster.CurrentHP, Monster.MaximumHP, MonsterHealthLine, "Здоровье");
                    UpdateHealthBar(Player.CurrentHP, Player.MaximumHP, PlayerHealthLine, "Здоровье");
                    Monster.CurrentSpeed = 0;

                    if (Player.CurrentHP <= 0 || Monster.CurrentHP <= 0) break;

                    if (!playerIsSlower)
                    {
                        System.Threading.Thread.Sleep(250);
                        _currentTurn++;
                        UpdateTurnNumber();
                        AddToCombatLog($"=== ХОД {_currentTurn} ===");
                        UpdateCombatLog();
                        System.Threading.Thread.Sleep(250);
                    }

                    System.Threading.Thread.Sleep(250);
                }
                else if (Player.CurrentSpeed >= 100)
                {

                    // ОЧИЩАЕМ БУФЕР ВВОДА ОТ ВСЕХ НАКОПЛЕННЫХ НАЖАТИЙ
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                    }

                    UpdateTurnStatus(); // "ВАШ ХОД" (без задержки - ждем ввода игрока)
                    ProcessPlayerInput();
                    UpdateCombatLog();
                    UpdateHealthBar(Monster.CurrentHP, Monster.MaximumHP, MonsterHealthLine, "Здоровье");
                    UpdateHealthBar(Player.CurrentHP, Player.MaximumHP, PlayerHealthLine, "Здоровье");
                    Player.CurrentSpeed = 0;

                    if (!Player.IsInCombat || Monster.CurrentHP <= 0 || Player.CurrentHP <= 0) break;

                    if (playerIsSlower)
                    {
                        System.Threading.Thread.Sleep(250);
                        _currentTurn++;
                        UpdateTurnNumber();
                        AddToCombatLog($"=== ХОД {_currentTurn} ===");
                        UpdateCombatLog();
                        System.Threading.Thread.Sleep(250);
                    }

                    System.Threading.Thread.Sleep(250);
                }
            }

            // Завершение боя
            if (Monster.CurrentHP <= 0)
            {
                Monster.CurrentHP = 0;

                // ДОБАВЛЯЕМ ИНФОРМАЦИЮ О ПОБЕДЕ В ЛОГ
                AddToCombatLog($"{Monster.Name} побежден!");

                // ДОБАВЛЯЕМ ИНФОРМАЦИЮ О НАГРАДЕ В ЛОГ
                AddToCombatLog($"Получено: {Monster.RewardGold} золота и {Monster.RewardEXP} опыта!");

                // ОБРАБАТЫВАЕМ ДОБЫЧУ
                List<Item> loot = Monster.GetLoot();
                if (loot.Count > 0)
                {
                    AddToCombatLog("Добыча:");
                    foreach (Item item in loot)
                    {
                        Player.AddItemToInventory(item);
                        AddToCombatLog($"- {item.Name}");
                    }
                }
                else
                {
                    AddToCombatLog("Добыча: ничего");
                }

                // НАЧИСЛЯЕМ НАГРАДУ (но без вывода в консоль)
                Player.Gold += Monster.RewardGold;
                Player.CurrentEXP += Monster.RewardEXP;
                Player.MonstersKilled++;

                // ПРОВЕРЯЕМ ПОВЫШЕНИЕ УРОВНЯ
                int oldLevel = Player.Level;
                Player.CheckLevelUp();
                if (Player.Level > oldLevel)
                {
                    AddToCombatLog($"Вы достигли {Player.Level} уровня!");
                }

                UpdateCombatLog(); // ОБНОВЛЯЕМ ОТОБРАЖЕНИЕ ЛОГА
                Player.IsInCombat = false;

                Console.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey(true);
            }
            else if (Player.CurrentHP <= 0)
            {
                RenderCombatState();

                Player.CurrentHP = 0;
                // Отрисовываем финальное состояние боя
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("ВЫ ПОГИБЛИ!");
                Console.ResetColor();

                MessageSystem.AddMessage("Вы пали в бою...");
                Player.IsInCombat = false;
                Console.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey(true);
            }
        }
        private void RenderCombatState()
        {
            Console.Clear();

            // === ИНФОРМАЦИЯ О ТЕКУЩЕМ СОСТОЯНИИ ХОДА ===
            Console.ForegroundColor = ConsoleColor.Yellow;
            if (Player.CurrentSpeed >= 100)
                Console.WriteLine($"=== ВАШ ХОД ===");
            else if (Monster.CurrentSpeed >= 100)
                Console.WriteLine($"=== ХОД {Monster.Name.ToUpper()} ===");
            else
                Console.WriteLine("=== НАПОЛНЕНИЕ ШКАЛЫ СКОРОСТИ ===");

            // === ТЕКУЩИЙ ХОД ===
            Console.WriteLine($"================ ХОД {_currentTurn} ================");
            Console.ResetColor();

            // === МОНСТР ===
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"=======[{Monster.Name}][{Monster.Level}]========");
            Console.ResetColor();

            DrawHealthBar(Monster.CurrentHP, Monster.MaximumHP, 20);
            // Запоминаем позицию шкалы скорости монстра
            monsterSpeedLine = Console.CursorTop;
            DrawSpeedBar(Monster.CurrentSpeed, 20);
            Console.WriteLine($"АТК: {Monster.Attack} | ЗЩТ: {Monster.Defence} | ЛОВ: {Monster.Agility}");
            Console.WriteLine("====================================");

            // === БОЕВОЙ ЛОГ ===
            RenderCombatLog();

            Console.WriteLine("------------------------------------");

            // === ИГРОК ===
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"========[Игрок][{Player.Level}]========");
            Console.ResetColor();

            DrawHealthBar(Player.CurrentHP, Player.MaximumHP, 20);
            // Запоминаем позицию шкалы скорости игрока
            playerSpeedLine = Console.CursorTop;
            DrawSpeedBar(Player.CurrentSpeed, 20);
            Console.WriteLine($"АТК: {Player.Attack} | ЗЩТ: {Player.Defence} | ЛОВ: {Player.Agility}");

            // === ДЕЙСТВИЯ ===
            Console.WriteLine("=========Действия=========");
            Console.WriteLine("| 1 - атаковать | 2 - заклинание |");
            Console.WriteLine("| 3 - защищаться | 4 - бежать |");
        }

        private void DrawHealthBar(int current, int max, int length)
        {
            // Защита от отрицательных значений
            current = Math.Max(current, 0);

            float percentage = (float)current / max;
            int bars = (int)(length * percentage);

            // Форматируем числа с выравниванием
            string healthText = $"{current}/{max}";

            Console.Write("Здоровье: [");

            // Цвет health bar в зависимости от процентов HP
            if (percentage > 0.5f)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (percentage > 0.25f)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.Write(new string('█', bars));
            Console.Write(new string('░', length - bars));
            Console.ResetColor();

            // ИСПРАВЛЕНИЕ: Выравнивание текста здоровья
            Console.WriteLine($"] {healthText}");
        }

        private void ProcessPlayerInput()
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    PlayerActionMessage = PlayerAttack();
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    //PlayerActionMessage = PlayerSpell();
                    PlayerActionMessage = "Заклинания пока не реализованы!";
                    AddToCombatLog(PlayerActionMessage);
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    //PlayerActionMessage = PlayerDefend();
                    PlayerActionMessage = "Защита пока не реализована!";
                    AddToCombatLog(PlayerActionMessage);
                    break;
                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    PlayerActionMessage = TryToEscape();
                    break;
                default:
                    PlayerActionMessage = "Неизвестная команда!";
                    AddToCombatLog(PlayerActionMessage);
                    break;

            }

        }

        private string PlayerAttack()
        {
            _actionCounter++;

            // Шанс промаха 10%
            if (new Random().Next(100) < 10)
            {
                string message = $"[Действие {_actionCounter}] Вы промахнулись по {Monster.Name}!";
                AddToCombatLog(message);
                return message;
            }

            int baseDamage = Player.Attack + new Random().Next(1, 6);
            bool isCritical = new Random().Next(100) < 5;
            if (isCritical) baseDamage = (int)(baseDamage * 1.5f);

            int finalDamage = Math.Max(baseDamage - Monster.Defence, 0);
            Monster.CurrentHP -= finalDamage;

            string resultMessage = isCritical ?
                $"[Действие {_actionCounter}] КРИТИЧЕСКИЙ УДАР! Вы наносите {finalDamage} урона!" :
                $"[Действие {_actionCounter}] Вы наносите {finalDamage} урона!";

            AddToCombatLog(resultMessage);
            return resultMessage;
        }

        private string TryToEscape()
        {
            int baseEscapeChance = 30;
            int escapeChance = baseEscapeChance;

            if (new Random().Next(100) < escapeChance)
            {
                Player.IsInCombat = false;
                string message = $"Вам удалось сбежать!";
                AddToCombatLog(message);
                return message;
            }
            else
            {
                string escapeMessage = $"Вам не удалось сбежать!";
                AddToCombatLog(escapeMessage);
                MonsterActionMessage = MonsterAttack(); // Уже содержит номер хода
                return escapeMessage;
            }
        }

        private void MonsterTurn()
        {
            MonsterActionMessage = MonsterAttack();
        }

        private string MonsterAttack()
        {
            _actionCounter++;

            // Шанс промаха 10%
            if (new Random().Next(100) < 10)
            {
                string message = $"[Действие {_actionCounter}] {Monster.Name} промахивается!";
                AddToCombatLog(message);
                return message;
            }

            int baseDamage = Monster.Attack + new Random().Next(1, 4);
            bool isCritical = new Random().Next(100) < 5;
            if (isCritical) baseDamage = (int)(baseDamage * 1.5f);

            int finalDamage = Math.Max(baseDamage - Player.Defence, 0);
            Player.CurrentHP -= finalDamage;

            string resultMessage = isCritical ?
                $"[Действие {_actionCounter}] КРИТИЧЕСКИЙ УДАР! {Monster.Name} наносит {finalDamage} урона!" :
                $"[Действие {_actionCounter}] {Monster.Name} наносит {finalDamage} урона!";

            AddToCombatLog(resultMessage);
            return resultMessage;
        }

        private void AddToCombatLog(string message)
        {
            _combatLog.Add(message); // Добавляем в КОНЕЦ (новые идут после старых)

            // Удаляем самые старые сообщения (первые в списке) если превысили лимит
            while (_combatLog.Count > MaxLogLines)
            {
                _combatLog.RemoveAt(0);
            }
        }

        private void RenderCombatLog()
        {
            Console.WriteLine("════════════ БОЕВОЙ ЛОГ ════════════");

            // Заполняем пустыми строками СВЕРХУ если сообщений меньше
            for (int i = _combatLog.Count; i < MaxLogLines; i++)
            {
                Console.WriteLine();
            }

            // Выводим сообщения в прямом порядке - первые снизу, новые сверху
            foreach (var message in _combatLog)
            {
                Console.WriteLine($" {message}");
            }

            Console.WriteLine("════════════════════════════════════");
        }

        private void UpdateSpeedMeters()
        {
            // Увеличиваем прирост скорости для более быстрого заполнения
            Player.CurrentSpeed += Player.Agility; // Множитель для ускорения
            Monster.CurrentSpeed += Monster.Agility; // Множитель для ускорения

            // Ограничиваем максимальное значение 100
            Player.CurrentSpeed = Math.Min(Player.CurrentSpeed, 100);
            Monster.CurrentSpeed = Math.Min(Monster.CurrentSpeed, 100);
        }

        private void DrawSpeedBar(int current, int length)
        {
            float percentage = (float)current / 100;
            int bars = (int)(length * percentage);

            Console.Write("Скорость: [");
            Console.ForegroundColor = ConsoleColor.Cyan;
            Console.Write(new string('█', bars));
            Console.Write(new string('░', length - bars));
            Console.ResetColor();
            Console.WriteLine($"] {current}%");
        }
                
        private void RenderStaticCombatLayout()
        {
            monsterSpeedLine = 4; // Установите соответствующие значения
            playerSpeedLine = 22;

            Console.Clear();

            // === ИНФОРМАЦИЯ О ТЕКУЩЕМ СОСТОЯНИИ ХОДА ===
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"=== НАПОЛНЕНИЕ ШКАЛЫ СКОРОСТИ ===");
            Console.WriteLine($"================ ХОД {_currentTurn} ================");
            Console.ResetColor();

            // === МОНСТР ===
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine($"======={Monster.Name}========");
            Console.ResetColor();

            DrawHealthBar(Monster.CurrentHP, Monster.MaximumHP, 20);

            //Console.Write("Здоровье: [");
            //Console.Write(new string(' ', 20));
            //Console.WriteLine($"] {Monster.CurrentHP}/{Monster.MaximumHP}");

            monsterSpeedLine = Console.CursorTop;
            DrawSpeedBar(0, 20); // Начинаем с 0%

            //Console.Write("Скорость: [");
            //Console.Write(new string(' ', 20));
            //Console.WriteLine("] 0%");

            Console.WriteLine($"АТК: {Monster.Attack} | ЗЩТ: {Monster.Defence} | ЛОВ: {Monster.Agility}");
            Console.WriteLine("====================================");

            // === БОЕВОЙ ЛОГ ===
            Console.WriteLine("════════════ БОЕВОЙ ЛОГ ════════════");
            for (int i = 0; i < MaxLogLines; i++)
            {
                Console.WriteLine();
            }
            Console.WriteLine("════════════════════════════════════");

            Console.WriteLine("------------------------------------");

            // === ИГРОК ===
            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"========Игрок========");
            Console.ResetColor();

            DrawHealthBar(Player.CurrentHP, Player.MaximumHP, 20);

            //Console.Write("Здоровье: [");
            //Console.Write(new string(' ', 20));
            //Console.WriteLine($"] {Player.CurrentHP}/{Player.MaximumHP}");

            playerSpeedLine = Console.CursorTop;
            DrawSpeedBar(0, 20); // Начинаем с 0%

            //Console.Write("Скорость: [");
            //Console.Write(new string(' ', 20));
            //Console.WriteLine("] 0%");

            Console.WriteLine($"АТК: {Player.Attack} | ЗЩТ: {Player.Defence} | ЛОВ: {Player.Agility}");

            // === ДЕЙСТВИЯ ===
            Console.WriteLine("=========Действия=========");
            Console.WriteLine("| 1 - атаковать | 2 - заклинание |");
            Console.WriteLine("| 3 - защищаться | 4 - бежать |");
        }

        // Обновляем методы для использования фиксированных позиций
        private void UpdateTurnStatus()
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            Console.SetCursorPosition(0, TurnStatusLine);
            Console.ForegroundColor = ConsoleColor.Yellow;

            if (Player.CurrentSpeed >= 100)
                Console.Write($"=== ВАШ ХОД ===");
            else if (Monster.CurrentSpeed >= 100)
                Console.Write($"=== ХОД {Monster.Name.ToUpper()} ===");
            else
                Console.Write("=== НАПОЛНЕНИЕ ШКАЛЫ СКОРОСТИ ===");

            // Очищаем оставшуюся часть строки
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));

            Console.ResetColor();
            Console.SetCursorPosition(originalLeft, originalTop);
        }

        private void UpdateTurnNumber()
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            Console.SetCursorPosition(0, TurnNumberLine);
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.Write($"================ ХОД {_currentTurn} ================");
            Console.Write(new string(' ', Console.WindowWidth - Console.CursorLeft));
            Console.ResetColor();
            Console.SetCursorPosition(originalLeft, originalTop);
        }

        private void UpdateHealthBar(int current, int max, int line, string label)
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            Console.SetCursorPosition(0, line);

            // ОЧИСТКА СТРОКИ
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(0, line);

            current = Math.Max(current, 0);
            float percentage = (float)current / max;
            int bars = (int)(20 * percentage);

            // Гарантируем, что bars и emptyBars не будут отрицательными
            bars = Math.Max(0, Math.Min(bars, 20));
            int emptyBars = 20 - bars;

            // Форматируем текст здоровья
            string healthText = $"{current}/{max}";

            Console.Write($"{label}: [");

            if (percentage > 0.5f)
                Console.ForegroundColor = ConsoleColor.Green;
            else if (percentage > 0.25f)
                Console.ForegroundColor = ConsoleColor.Yellow;
            else
                Console.ForegroundColor = ConsoleColor.Red;

            Console.Write(new string('█', bars));
            Console.Write(new string('░', emptyBars));
            Console.ResetColor();

            // ИСПРАВЛЕНИЕ: Используем форматированный текст
            Console.WriteLine($"] {healthText}");

            Console.SetCursorPosition(originalLeft, originalTop);
        }

        private void UpdateSpeedBar(int current, int line, string label)
        {
            // Проверяем что позиция в пределах допустимого
            if (line < 0 || line >= Console.BufferHeight)
            {
                return; // Выходим если позиция невалидная
            }

            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            try
            {
                Console.SetCursorPosition(0, line);

                // ОЧИСТКА СТРОКИ перед отрисовкой
                Console.Write(new string(' ', Console.WindowWidth));
                Console.SetCursorPosition(0, line);

                // Ограничиваем current в диапазоне 0-100
                current = Math.Max(0, Math.Min(current, 100));
                float percentage = (float)current / 100;
                int bars = (int)(20 * percentage);

                // Гарантируем, что bars и emptyBars не будут отрицательными
                bars = Math.Max(0, Math.Min(bars, 20));
                int emptyBars = 20 - bars;

                Console.Write($"{label}: [");
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.Write(new string('█', bars));
                Console.Write(new string('░', emptyBars));
                Console.ResetColor();

                // ИСПРАВЛЕНИЕ: Убрать лишний символ процента
                Console.WriteLine($"] {current}%"); // БЫЛО: {current}%%
            }
            catch (ArgumentOutOfRangeException)
            {
                // Игнорируем ошибку позиционирования
            }
            finally
            {
                // Всегда возвращаем курсор на исходную позицию
                try
                {
                    Console.SetCursorPosition(originalLeft, originalTop);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Если исходная позиция тоже невалидная, просто сбрасываем курсор
                    Console.SetCursorPosition(0, 0);
                }
            }
        }

        private void UpdateCombatLog()
        {
            int originalLeft = Console.CursorLeft;
            int originalTop = Console.CursorTop;

            // Очищаем область лога
            for (int i = 0; i < MaxLogLines; i++)
            {
                Console.SetCursorPosition(0, CombatLogStartLine + i);
                Console.Write(new string(' ', Console.WindowWidth));
            }

            // Выводим новые сообщения
            for (int i = 0; i < _combatLog.Count; i++)
            {
                Console.SetCursorPosition(0, CombatLogStartLine + i);
                Console.Write($" {_combatLog[i]}");
            }

            Console.SetCursorPosition(originalLeft, originalTop);
        }
    }
}
