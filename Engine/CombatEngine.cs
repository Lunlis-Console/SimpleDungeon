namespace Engine
{
    public class CombatEngine
    {
        public Player Player { get; set; }
        public Monster Monster { get; set; }

        public string PlayerActionMessage { get; private set; }
        public string MonsterActionMessage { get; private set; }
        private readonly IWorldRepository _worldRepository;
        private readonly CombatRenderer _combatRenderer;
        private CombatState _previousState;

        public CombatEngine(Player player, Monster monster, IWorldRepository worldRepository)
        {
            Player = player;
            Monster = monster;
            Player.IsInCombat = true;
            Player.CurrentMonster = monster;

            PlayerActionMessage = "";
            MonsterActionMessage = "";

            _worldRepository = worldRepository;

            _combatRenderer = new CombatRenderer(GameServices.BufferedRenderer);
        }

        private List<string> _combatLog = new List<string>();
        private const int MaxLogLines = 10;
        private int _actionCounter = 0;
        private int _currentTurn = 1;

        private const int TurnStatusLine = 0;
        private const int TurnNumberLine = 1;
        private const int MonsterHealthLine = 3;    // "Здоровье: [" на строке 3
        private const int MonsterSpeedLine = 4;     // "Скорость: [" на строке 4  
        private const int MonsterStatsLine = 5;     // "АТК: " на строке 5
        private const int CombatLogStartLine = 7;   // Лог начинается после разделителя
        private const int PlayerHealthLine = 18;    // "Здоровье: [" игрока
        private const int PlayerSpeedLine = 21;     // "Скорость: [" игрока
        private const int PlayerStatsLine = 20;     // "АТК: " игрока

        

        public void CombatLoop()
        {
            // Инициализация...

            Player.CurrentSpeed = 0;
            Monster.CurrentSpeed = 0;
            _actionCounter = 0;
            _currentTurn = 1;

            int slowerAgility = Math.Min(Player.Agility, Monster.Agility);
            bool playerIsSlower = Player.Agility == slowerAgility;


            _combatRenderer.SetNeedsFullRedraw(); // Первая отрисовка - полная

            while (Player.IsInCombat && Player.CurrentHP > 0 && Monster.CurrentHP > 0)
            {
                UpdateSpeedMeters();

                // Рендерим кадр с двойной буферизацией
                _combatRenderer.RenderCombatFrame(
                    Player, Monster, _combatLog, _currentTurn,
                    Player.CurrentSpeed, Monster.CurrentSpeed
                );

                if (Monster.CurrentSpeed >= 100)
                {
                    Thread.Sleep(250);
                    MonsterTurn();
                    Monster.CurrentSpeed = 0;
                    if (Player.CurrentHP <= 0 || Monster.CurrentHP <= 0) break;

                    if (!playerIsSlower)
                    {
                        Thread.Sleep(250);
                        _currentTurn++;
                        AddToCombatLog($"=== ХОД {_currentTurn} ===");
                        Thread.Sleep(250);
                    }
                    Thread.Sleep(250);
                }
                else if (Player.CurrentSpeed >= 100)
                {
                    // Очищаем буфер ввода
                    while (Console.KeyAvailable)
                    {
                        Console.ReadKey(true);
                    }

                    ProcessPlayerInput();
                    Player.CurrentSpeed = 0;
                    if (!Player.IsInCombat || Monster.CurrentHP <= 0 || Player.CurrentHP <= 0) break;

                    if (playerIsSlower)
                    {
                        Thread.Sleep(250);
                        _currentTurn++;
                        AddToCombatLog($"=== ХОД {_currentTurn} ===");
                        Thread.Sleep(250);
                    }
                    Thread.Sleep(250);
                }

                Thread.Sleep(50); // Небольшая задержка между кадрами
            }

            EndCombat();
        }
        private void ProcessPlayerInput()
        {
            ConsoleKeyInfo key;
            bool validInput = false;

            while (!validInput)
            {
                key = Console.ReadKey(true);

                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        PlayerActionMessage = PlayerAttack();
                        validInput = true;
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        PlayerActionMessage = "Заклинания пока не реализованы!";
                        AddToCombatLog(PlayerActionMessage);
                        validInput = true;
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        PlayerActionMessage = "Защита пока не реализована!";
                        AddToCombatLog(PlayerActionMessage);
                        validInput = true;
                        break;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        PlayerActionMessage = TryToEscape();
                        validInput = true;
                        break;
                    default:
                        // Короткий звуковой сигнал или вибрация
                        break;
                }
            }
        }

        private string PlayerAttack()
        {
            _actionCounter++;
            Random random = new Random();

            if (random.Next(100) < 10)
            {
                string message = $"[Действие {_actionCounter}] Вы промахнулись по {Monster.Name}!";
                AddToCombatLog(message);
                return message;
            }

            if (random.Next(100) < Monster.EvasionChance)
            {
                string message = $"[Действие {_actionCounter}] {Monster.Name} ловко уклонился от вашей атаки!";
                AddToCombatLog(message);
                return message;
            }

            int baseDamage = Player.GetTotalAttack(Monster) + random.Next(1, 6);
            bool isCritical = random.Next(100) < 5;
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
                MonsterActionMessage = MonsterAttack();
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
            Random random = new Random();

            if (random.Next(100) < 10)
            {
                string message = $"[Действие {_actionCounter}] {Monster.Name} промахивается!";
                AddToCombatLog(message);
                return message;
            }

            if (random.Next(100) < Player.EvasionChance)
            {
                string message = $"[Действие {_actionCounter}] Вы уверенно уворачиваетесь от атаки {Monster.Name}!";
                AddToCombatLog(message);
                return message;
            }

            int baseDamage = Monster.Attack + random.Next(1, 4);
            bool isCritical = random.Next(100) < 5;
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
            _combatLog.Add(message);
            while (_combatLog.Count > MaxLogLines)
            {
                _combatLog.RemoveAt(0);
            }
        }

        private void UpdateSpeedMeters()
        {
            Player.CurrentSpeed += Player.Agility;
            Monster.CurrentSpeed += Monster.Agility;

            Player.CurrentSpeed = Math.Min(Player.CurrentSpeed, 100);
            Monster.CurrentSpeed = Math.Min(Monster.CurrentSpeed, 100);
        }

        private void EndCombat()
        {
            if (Monster.CurrentHP <= 0)
            {
                Monster.CurrentHP = 0;
                AddToCombatLog($"{Monster.Name} побежден!");
                AddToCombatLog($"Получено: {Monster.RewardGold} золота и {Monster.RewardEXP} опыта!");

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

                Player.Gold += Monster.RewardGold;
                Player.CurrentEXP += Monster.RewardEXP;
                Player.MonstersKilled++;
                Player.AddMonsterKill(Monster.Name);

                int oldLevel = Player.Level;
                Player.CheckLevelUp();
                if (Player.Level > oldLevel)
                {
                    AddToCombatLog($"Вы достигли {Player.Level} уровня!");
                }

                _combatRenderer.RenderCombatFrame(Player, Monster, _combatLog, _currentTurn,
                    Player.CurrentSpeed, Monster.CurrentSpeed);
                Player.IsInCombat = false;

                //GameServices.OutputService.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey(true);
            }
            else if (Player.CurrentHP <= 0)
            {
                Player.CurrentHP = 0;
                Console.ForegroundColor = ConsoleColor.Red;
                //GameServices.OutputService.WriteLine("ВЫ ПОГИБЛИ!");
                Console.ResetColor();

                MessageSystem.AddMessage("Вы пали в бою...");
                Player.IsInCombat = false;
                //GameServices.OutputService.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey(true);
            }
        }
    }
}