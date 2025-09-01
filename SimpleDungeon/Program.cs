using Engine;
using System;
using System.Threading;

namespace SimpleDungeon
{
    public static class Program
    {
        private static Player _player;


        public static void Main(string[] args)
        {
            _player = new Player(0, 100, 100, 0, 100, 1, 0, 0);
            _player.CurrentLocation = World.LocationByID(World.LOCATION_ID_VILLAGE);

            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_LEATHER_HELMET), 10));
            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_LEATHER_ARMOR), 1));
            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_LEATHER_GLOVES), 1));
            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_LEATHER_BOOTS), 1));

            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RATS_MEAT), 10));
            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 10));
            _player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_IRON_SWORD), 10));

            ProcessKeyInput();

            while (true)
            {
                DisplayUI();

                Console.Write(">");

                string userInput = Console.ReadLine();

                Console.Clear();

                if (string.IsNullOrWhiteSpace(userInput))
                {
                    continue;
                }

                string cleanedInput = userInput.ToLower();

                ParseInput(cleanedInput);
            }

        }

        private static void ParseInput(string input)
        {
            if (input.Contains("помощь") || input == "h")
            {
                HelpWorld();
            }
            else if (input.Contains("север") || input == "с")
            {
                MoveNorth();

                //if (_player.CurrentLocation.LocationToNorth == null)
                //{
                //    Console.WriteLine("СИСТЕМА: Вы не можете двигаться на север.");
                //}
                //else
                //{
                //    _player.MoveNorth();
                //}
            }
            else if (input.Contains("восток") || input == "в")
            {
                MoveEast();

                //if (_player.CurrentLocation.LocationToEast == null)
                //{
                //    Console.WriteLine("СИСТЕМА: Вы не можете двигаться на восток.");
                //}
                //else
                //{
                //    _player.MoveEast();
                //}
            }
            else if (input.Contains("запад") || input == "з")
            {
                MoveWest();

                //if (_player.CurrentLocation.LocationToWest == null)
                //{
                //    Console.WriteLine("СИСТЕМА: Вы не можете двигаться на запад.");
                //}
                //else
                //{
                //    _player.MoveWest();
                //}
            }
            else if (input.Contains("юг") || input == "ю")
            {
                MoveSouth();

                //if (_player.CurrentLocation.LocationToSouth == null)
                //{
                //    Console.WriteLine("СИСТЕМА: Вы не можете двигаться на юг.");
                //}
                //else
                //{
                //    _player.MoveSouth();
                //}
            }
            else if (input.Contains("сумка") || input == "s")
            {
                _player.DisplayInventory();
            }
            else if (input.Contains("смотреть") || input == "l")
            {
                _player.LookAround();
            }
            else if (input.Contains("атаковать") || input == "a")
            {
                StartCombat();

                //string monsterName = input.Replace("атаковать", "").Trim();

                //List<Monster> monsters = _player.CurrentLocation.FindMonsters();

                //foreach (Monster monster in monsters)
                //{
                //    if (monster.Name.ToLower().Trim() == monsterName)
                //    {
                //        _player.StartCombat(monster);
                //        break;
                //    }
                //}

            }
            else if (input.Contains("говорить") || input == "t")
            {
                TalkToNPC();

                //string npcName = input.Replace("говорить", "").Trim();
                //_player.TalkTo(npcName);
            }
            else
            {
                Console.WriteLine("СИСТЕМА: Неизвестная команда. Нажмите H для помощи.");
                Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                Console.ReadKey();
            }
        }
        public static void ProcessKeyInput()
        {
            while (true)
            {
                DisplayUI();

                ConsoleKeyInfo key = Console.ReadKey();

                switch(key.Key)
                {
                    case ConsoleKey.W:
                        MoveNorth();
                        break;
                    case ConsoleKey.D:
                        MoveEast();
                        break;
                    case ConsoleKey.S:
                        MoveSouth();
                        break;
                    case ConsoleKey.A:
                        MoveWest();
                        break;
                    case ConsoleKey.I:
                        _player.DisplayInventory();
                        break;
                    case ConsoleKey.L:
                        _player.LookAround();
                        Console.Clear();
                        break;
                    case ConsoleKey.F:
                        StartCombat();
                        break;
                    case ConsoleKey.T:
                        TalkToNPC();
                        break;
                    case ConsoleKey.H:
                        HelpWorld();
                        break;
                    case ConsoleKey.C:
                        CharacterScreen.Show(_player);
                        break;
                    case ConsoleKey.J:
                        _player.QuestLog.DisplayQuestLog();
                        break;
                    case ConsoleKey.Escape:
                        Console.WriteLine("Выход из игры...");
                        return;

                    default:
                        Console.WriteLine("СИСТЕМА: Неизвестная команда. Нажмите H для помощи.");
                        Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                        Console.ReadKey();
                        break;
                }

                Console.Clear();
            }
        }
        private static void MoveNorth()
        {
            if(_player.CurrentLocation.LocationToNorth == null)
            {
                MessageSystem.AddMessage("Вы не можете двигаться на север.");
            }
            else
            {
                _player.MoveNorth();
                MessageSystem.AddMessage($"Вы переместились в {_player.CurrentLocation.Name}");
            }
        }
        private static void MoveEast()
        {
            if (_player.CurrentLocation.LocationToEast == null)
            {
                MessageSystem.AddMessage("Вы не можете двигаться на восток.");
            }
            else
            {
                _player.MoveEast();
                MessageSystem.AddMessage($"Вы переместились в {_player.CurrentLocation.Name}");
            }
        }
        private static void MoveWest()
        {
            if (_player.CurrentLocation.LocationToWest == null)
            {
                MessageSystem.AddMessage("Вы не можете двигаться на запад.");
            }
            else
            {
                _player.MoveWest();
                MessageSystem.AddMessage($"Вы переместились в {_player.CurrentLocation.Name}");
            }
        }
        private static void MoveSouth()
        {
            if (_player.CurrentLocation.LocationToSouth == null)
            {
                MessageSystem.AddMessage("Вы не можете двигаться на юг.");
            }
            else
            {
                _player.MoveSouth();
                MessageSystem.AddMessage($"Вы переместились в {_player.CurrentLocation.Name}");
            }
        }
        private static void StartCombat()
        {
            var monsters = _player.CurrentLocation.FindMonsters();
            if(monsters.Count > 0)
            {
                if (monsters.Count == 1)
                {
                    _player.StartCombat(monsters[0]);
                }
                else
                {
                    var selecterMonster = MenuSystem.SelectFromList(
                        monsters,
                        monster => $"{monster.Name} [{monster.Level}]",
                        "Выберите монстра для атаки"
                    );

                    if (selecterMonster != null)
                    {
                        _player.StartCombat(selecterMonster);
                    }
                }
            }
            else
            {
                Console.WriteLine("СИСТЕМА: Здесь нет монстров для атаки.");
                Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                Console.ReadKey();
            }
        }
        private static void TalkToNPC()
        {
            if(_player.CurrentLocation.NPCsHere.Count > 0)
            {
                if (_player.CurrentLocation.NPCsHere.Count == 1)
                {
                    _player.TalkTo(_player.CurrentLocation.NPCsHere[0].Name);
                }
                else
                {
                    var selectedNPC = MenuSystem.SelectFromList(
                        _player.CurrentLocation.NPCsHere,
                        npc => npc.Name,
                        "Выберите для разговора"
                    );

                    if(selectedNPC != null)
                    {
                        _player.TalkTo(selectedNPC.Name);
                    }
                }
            }
            else
            {
                Console.WriteLine("СИСТЕМА: Здесь нет NPC для разговора.");
                Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                Console.ReadKey();
            }
        }
        private static void DisplayUI()
        {
            Console.Clear();

            int consoleWidth = Console.WindowWidth;
            int leftPanelWidth = consoleWidth - 30;

            Console.SetCursorPosition(0, 0);
            MessageSystem.DisplayMessages();

            Console.WriteLine("==========================Статус==========================");
            Console.WriteLine($"| Уровень: {_player.Level} " +
                $"| Здоровье: {_player.CurrentHP}/{_player.MaximumHP} " +
                $"| Опыт: {_player.CurrentEXP}/{_player.MaximumEXP} " +
                $"| Золото: {_player.Gold} ");
            Console.WriteLine("=========================Окружение========================");
            DisplayCurrentLocation();
            DisplayMonstersAndNPCs();

            Console.WriteLine("=========================Действие=========================");
            DisplayAvailabelDirecrions();
                        
        }

        private static void DisplayCurrentLocation()
        {
            Console.WriteLine($"Текущаяя локация: {_player.CurrentLocation.Name}");

            if (_player.CurrentLocation.Description != "")
            {
                Console.WriteLine($"\nОписание: {_player.CurrentLocation.Description}");
            }
        }
        private static void DisplayMonstersAndNPCs()
        {

            var monsters = _player.CurrentLocation.FindMonsters();
            if (monsters.Count > 0)
            {
                Console.WriteLine("\nМонстры: ");
                foreach (var monster in monsters)
                {
                    Console.WriteLine($"- {monster.Name} [{monster.Level}].");
                }
            }
            //else
            //{
            //    Console.WriteLine("\nМонстры: отсутствуют");
            //}

            if (_player.CurrentLocation.NPCsHere.Count > 0)
            {
                Console.WriteLine("\nЖители:");
                foreach (var npc in _player.CurrentLocation.NPCsHere)
                {
                    Console.WriteLine($"- {npc.Name}");
                }
            }
            //else
            //{
            //    Console.WriteLine("\nЖители: отсутствуют");
            //}
        }
        private static void HelpWorld()
        {
            Console.Clear();

            Console.WriteLine("=================Помощь=================");
            Console.WriteLine("WASD - Перемещение между локациями");
            Console.WriteLine("I - Открыть сумку (инвентарь)");
            Console.WriteLine("L - Осмотреться вокруг");
            Console.WriteLine("F - Атаковать монстра");
            Console.WriteLine("T - Говорить с NPC");
            Console.WriteLine("H - Показать помощь");
            Console.WriteLine("ESC - Выйти из игры");
            Console.WriteLine("========================================");

            Console.WriteLine("\nНажмите любую клавишу, чтобы закрыть описание...");
            Console.ReadKey();
            Console.Clear();
        }
        private static void DisplayAvailabelDirecrions()
        {
            Console.WriteLine("Доступние направления: ");

            if(_player.CurrentLocation.LocationToNorth != null)
            {
                Console.WriteLine("W - Север");
            }
            if (_player.CurrentLocation.LocationToWest != null)
            {
                Console.WriteLine("A - Запад");
            }
            if (_player.CurrentLocation.LocationToSouth != null)
            {
                Console.WriteLine("S - Юг");
            }
            if (_player.CurrentLocation.LocationToEast != null)
            {
                Console.WriteLine("D - Восток");
            }

            Console.WriteLine("| C - Характеристики | I - Сумка | L - Осмотреться | F - Атаковать | T - Говорить | H - Помощь |");
        }
        

    }

}

