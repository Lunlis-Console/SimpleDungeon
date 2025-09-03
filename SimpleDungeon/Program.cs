using Engine;
using System;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using static System.Net.Mime.MediaTypeNames;

namespace SimpleDungeon
{
    public static class Program
    {
        private static Player _player;


        public static void Main(string[] args)
        {
            Console.Title = "Sipmle Dungeon";
            Console.CursorVisible = false;

            ShowMainMenu();


            ProcessKeyInput();

        }

        private static bool _needsRedraw = true;
        public static void ProcessKeyInput()
        {
            while (true)
            {
                bool keyProcessed = false;

                // Быстрая проверка ввода
                while (Console.KeyAvailable)
                {
                    keyProcessed = true;
                    var keyInfo = Console.ReadKey(true);

                    if (keyInfo.Key == ConsoleKey.F3)
                    {
                        DebugConsole.Toggle();
                        Console.Clear();
                        if (!DebugConsole.IsVisible)
                        {
                            DisplayUI();
                        }
                        break;
                    }

                    if (!DebugConsole.IsVisible)
                    {
                        ProcessGameKey(keyInfo.Key);
                        Console.Clear();
                        DisplayUI();
                        break;
                    }
                    else
                    {
                        // Передаем ввод в дебаг-консоль с символом
                        DebugConsole.HandleKey(keyInfo.Key, keyInfo.KeyChar);
                        break;
                    }
                }

                // Отрисовка дебаг-консоли если видна
                if (DebugConsole.IsVisible)
                {
                    DebugConsole.Update();
                }

                // Небольшая пауза только если не было обработки ввода
                if (!keyProcessed)
                {
                    Thread.Sleep(DebugConsole.IsVisible ? 1 : 10);
                }
            }
        }
        private static void ProcessGameKey(ConsoleKey key)
        {
            switch (key)
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
                        //Console.Clear();
                        break;
                    case ConsoleKey.E:
                        InteractWithWorld();
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
                        ShowGameMenu();
                        break;
                    case ConsoleKey.F5:
                        SaveManager.SaveGame(_player, "quicksave");
                        break;
                    case ConsoleKey.F9:
                        try
                        {
                            _player = SaveManager.LoadGame("quicksave");
                            MessageSystem.AddMessage("Быстрая загрузка выполнена!");
                        }
                        catch
                        {
                            MessageSystem.AddMessage("Быстрое сохранение не найдено!");
                        }
                        break;
                    //case ConsoleKey.Escape:
                    //    if (MenuSystem.ConfirmAction("Сохранить игру перед выходом?"))
                    //    {
                    //        SaveManager.SaveGame(_player, $"save_{DateTime.Now:yyyyMMdd_HHmmss}");
                    //    }
                        //return;

                    default:
                        Console.WriteLine("СИСТЕМА: Неизвестная команда. Нажмите H для помощи.");
                        //Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                        //Console.ReadKey();
                        break;
                }

                //Console.Clear();
        }

        private static void MoveNorth()
        {
            if (_player.CurrentLocation.LocationToNorth == null)
            {
                MessageSystem.AddMessage("Вы не можете двигаться на север.");
            }
            else
            {
                _player.MoveNorth();
                MessageSystem.AddMessage($"Вы переместились в {_player.CurrentLocation.Name}");
                _needsRedraw = true;
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
                _needsRedraw = true;
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
                _needsRedraw = true;
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
                _needsRedraw = true;
            }
        }
        private static void StartCombat()
        {
            var monsters = _player.CurrentLocation.FindMonsters();
            if (monsters.Count > 0)
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
                MessageSystem.AddMessage("Здесь нет монстров для атаки.");
                //Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
                //Console.ReadKey();
            }
        }
        private static void TalkToNPC()
        {
            if (_player.CurrentLocation.NPCsHere.Count > 0)
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

                    if (selectedNPC != null)
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
            Console.SetCursorPosition(0, 0);

            // СБРАСЫВАЕМ ЦВЕТА ПЕРЕД ОТРИСОВКОЙ
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.White;
            Console.BackgroundColor = ConsoleColor.Black;

            MessageSystem.DisplayMessages();

            Console.WriteLine("=========================Окружение========================");
            DisplayCurrentLocation();
            DisplayMonstersAndNPCs();

            // ДОБАВЛЯЕМ ОТОБРАЖЕНИЕ ПРЕДМЕТОВ НА ЗЕМЛЕ
            DisplayGroundItems();

            Console.WriteLine("=========================Действие=========================");
            DisplayAvailabelDirecrions();

            // СБРАСЫВАЕМ ЦВЕТА ПОСЛЕ ОТРИСОВКИ
            Console.ResetColor();
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

        private static void DisplayGroundItems()
        {
            var groundItems = _player.CurrentLocation.GroundItems;
            if (groundItems.Count > 0)
            {
                Console.WriteLine("\nПредметы на земле:");
                foreach (var item in groundItems.GroupBy(i => i.Details.ID))
                {
                    var firstItem = item.First();
                    Console.WriteLine($"- {firstItem.Details.Name} x{item.Sum(i => i.Quantity)}");
                }
            }
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

            if (_player.CurrentLocation.LocationToNorth != null)
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

            Console.WriteLine("| C - Характеристики | I - Сумка | J - Журнал | L - Осмотреться | E - Взаимодействовать | H - Помощь |");
        }
        private static void ShowMainMenu()
        {
            int selectedIndex = 0;
            string[] menuItems = { "Новая игра", "Загрузить игру", "Выход" };

            while (true)
            {
                Console.Clear();
                Console.WriteLine("===================================");
                Console.WriteLine("          SIMPLE DUNGEON");
                Console.WriteLine("===================================");

                // Отображаем пункты меню
                for (int i = 0; i < menuItems.Length; i++)
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

                    Console.WriteLine(menuItems[i]);
                    Console.ResetColor();
                }

                Console.WriteLine("===================================");

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                        selectedIndex = (selectedIndex - 1 + menuItems.Length) % menuItems.Length;
                        break;

                    case ConsoleKey.S:
                        selectedIndex = (selectedIndex + 1) % menuItems.Length;
                        break;

                    case ConsoleKey.E:
                        ExecuteMainMenuChoice(selectedIndex);
                        break;

                    case ConsoleKey.Q:
                        Console.WriteLine("\nВыход из игры...");
                        return;
                }
            }
        }
        private static void ExecuteMainMenuChoice(int choice)
        {
            switch (choice)
            {
                case 0: // Новая игра
                    StartNewGame();
                    break;

                case 1: // Загрузить игру
                    ShowLoadGameMenu();
                    break;

                case 2: // Выход
                    Console.WriteLine("\nВыход из игры...");
                    Environment.Exit(0);
                    break;
            }
        }
        private static void ShowLoadGameMenu()
        {
            var saves = SaveManager.GetAvailableSaves();

            if (saves.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("Нет доступных сохранений!");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
                return;
            }

            var selectedSave = MenuSystem.SelectFromList(
                saves,
                save => save,
                "====== ЗАГРУЗИТЬ ИГРУ ======",
                "Выберите сохранение для загрузки"
            );

            if (selectedSave != null)
            {
                try
                {
                    _player = SaveManager.LoadGame(selectedSave);
                    MessageSystem.AddMessage($"Игра загружена: {selectedSave}");
                    ProcessKeyInput();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }
        private static void StartNewGame()
        {
            _player = new Player(0, 100, 100, 0, 100, 1, 0, 0, 10);
            _player.CurrentLocation = World.LocationByID(World.LOCATION_ID_VILLAGE);

            // Стартовые предметы

            _player.Inventory.AddItem(World.ItemByID(World.ITEM_ID_LEATHER_HELMET), 1);
            _player.Inventory.AddItem(World.ItemByID(World.ITEM_ID_SPIDER_SILK), 1);
            _player.Inventory.AddItem(World.ItemByID(World.ITEM_ID_LEATHER_ARMOR), 1);
            _player.Inventory.AddItem(World.ItemByID(World.ITEM_ID_LEATHER_GLOVES), 1);
            _player.Inventory.AddItem(World.ItemByID(World.ITEM_ID_LEATHER_BOOTS), 1);
            _player.Inventory.AddItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 1);

            ProcessKeyInput();
        }
        private static void ShowGameMenu()
        {


            var menuOptions = new List<MenuOption>
            {
                new MenuOption("Вернуться в игру", () => { }),
                new MenuOption("Сохранить игру", () => SaveGameMenu()),
                new MenuOption("Загрузить игру", () => LoadGameMenu()),
                new MenuOption("Главное меню", () => {
                    if (MenuSystem.ConfirmAction("Вернуться в главное меню? Несохраненный прогресс будет потерян."))
                    {
                        Console.Clear();
                        ShowMainMenu();
                        Environment.Exit(0);
                    }
                }),
                
            };

            var selected = MenuSystem.SelectFromList(
                menuOptions,
                opt => opt.DisplayText,
                "====== МЕНЮ ИГРЫ ======",
                "Клавиши 'W' 'S' для выбора, 'E' для подтверждения"
            );

            selected?.Action();
        }
        private static void SaveGameMenu()
        {
            Console.Clear();
            Console.Write("Введите название сохранения: ");
            string saveName = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(saveName))
            {
                SaveManager.SaveGame(_player, saveName);
                Console.WriteLine($"Игра сохранена как: {saveName}");
            }
            else
            {
                SaveManager.SaveGame(_player, $"save_{DateTime.Now:yyyyMMdd_HHmmss}");
                Console.WriteLine("Игра сохранена с автоматическим названием");
            }

            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }
        private static void LoadGameMenu()
        {
            var saves = SaveManager.GetAvailableSaves();

            if (saves.Count == 0)
            {
                Console.Clear();
                Console.WriteLine("Нет доступных сохранений!");
                Console.WriteLine("Нажмите любую клавишу...");
                Console.ReadKey();
                return;
            }

            var selectedSave = MenuSystem.SelectFromList(
                saves,
                save => save,
                "====== ЗАГРУЗИТЬ ИГРУ ======",
                "Выберите сохранение для загрузки"
            );

            if (selectedSave != null)
            {
                try
                {
                    _player = SaveManager.LoadGame(selectedSave);
                    MessageSystem.AddMessage($"Игра загружена: {selectedSave}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка загрузки: {ex.Message}");
                    Console.ReadKey();
                }
            }
        }
        private class MenuOption
        {
            public string DisplayText { get; }
            public Action Action { get; }

            public MenuOption(string displayText, Action action)
            {
                DisplayText = displayText;
                Action = action;
            }
        }

        private static void InteractWithWorld()
        {
            var worldEntities = new List<WorldEntity>();

            // Добавляем монстров
            var monstersHere = _player.CurrentLocation.FindMonsters();
            foreach (var monster in monstersHere)
            {
                worldEntities.Add(new WorldEntity(monster, EntityType.Monster, $"{monster.Name} [Ур. {monster.Level}]"));
            }

            // Добавляем NPC
            foreach (var npc in _player.CurrentLocation.NPCsHere)
            {
                worldEntities.Add(new WorldEntity(npc, EntityType.NPC, npc.Name));
            }

            // Добавляем предметы на земле
            foreach (var item in _player.CurrentLocation.GroundItems)
            {
                worldEntities.Add(new WorldEntity(item, EntityType.Item, $"{item.Details.Name}"));
            }

            // Если не с чем взаимодействовать
            if (worldEntities.Count == 0)
            {
                MessageSystem.AddMessage("Здесь не с чем взаимодействовать.");
                return;
            }

            // Выбор сущности для взаимодействия
            var selectedWorldEntity = MenuSystem.SelectFromList(
                worldEntities,
                entity => entity.DisplayName,
                "ВЫБЕРИТЕ ЦЕЛЬ",
                "Клавиши 'W' 'S' для выбора, 'E' - взаимодействовать, 'Q' - отмена"
            );

            if (selectedWorldEntity != null)
            {
                InteractWithEntity(selectedWorldEntity);
            }
        }
        private static void InteractWithEntity(WorldEntity worldEntity)
        {
            // Если это предмет на земле - обрабатываем отдельно
            if (worldEntity.Entity is InventoryItem groundItem)
            {
                PickUpItem(groundItem);
                return;
            }

            // Для всех остальных типов сущностей (монстры, NPC и т.д.)
            if (worldEntity.Entity is IInteractable interactable)
            {
                bool continueInteraction = true;

                while (continueInteraction)
                {
                    Console.Clear();
                    MessageSystem.DisplayMessages();

                    List<string> actions = interactable.GetAvailableActions(_player);
                    actions.Add("Назад"); // Добавляем опцию возврата

                    var selectedAction = MenuSystem.SelectFromList(
                        actions,
                        action => action,
                        $"ВЗАИМОДЕЙСТВИЕ: {interactable.Name}",
                        "Клавиши 'W' 'S' для выбора, 'E' - выполнить, 'Q' - назад"
                    );

                    if (selectedAction != null && selectedAction != "Назад")
                    {
                        interactable.ExecuteAction(_player, selectedAction);

                        // Если действие завершает взаимодействие (например, атака или уход)
                        if (selectedAction == "Атаковать" || selectedAction == "Уйти")
                        {
                            continueInteraction = false;
                        }
                    }
                    else
                    {
                        continueInteraction = false;
                    }
                }
            }
            else
            {
                MessageSystem.AddMessage("Нельзя взаимодействовать с этим объектом.");
            }
        }
        // НОВЫЙ МЕТОД ДЛЯ ОБРАБОТКИ ДЕЙСТВИЙ С ПРЕДМЕТАМИ
        private static void HandleItemAction(InventoryItem item, string action, ref bool continueInteraction)
        {
            switch (action)
            {
                case "Подобрать":
                    _player.AddItemToInventory(item.Details, item.Quantity);
                    _player.CurrentLocation.GroundItems.Remove(item);
                    CheckQuestItemPickup(item.Details, item.Quantity);
                    MessageSystem.AddMessage($"Вы подобрали: {item.Details.Name} x{item.Quantity}");
                    continueInteraction = false;
                    break;

                case "Осмотреть":
                    item.Details.Read();
                    Console.WriteLine("\nНажмите любую клавишу...");
                    Console.ReadKey();
                    continueInteraction = true; // Продолжаем взаимодействие
                    break;

                case "Назад":
                    continueInteraction = false;
                    break;
            }
        }        // НОВЫЙ МЕТОД ДЛЯ ПОДБОРА ПРЕДМЕТОВ
        private static void PickUpItem(InventoryItem item)
        {
            Console.Clear();
            Console.WriteLine($"Вы нашли: {item.Details.Name} x{item.Quantity}");

            var actions = new List<string> { "Подобрать", "Осмотреть", "Оставить" };
            var selectedAction = MenuSystem.SelectFromList(
                actions,
                action => action,
                $"ВЗАИМОДЕЙСТВИЕ: {item.Details.Name}",
                "Выберите действие"
            );

            switch (selectedAction)
            {
                case "Подобрать":
                    _player.AddItemToInventory(item.Details, item.Quantity);
                    _player.CurrentLocation.GroundItems.Remove(item);

                    // Проверяем квестовые предметы
                    CheckQuestItemPickup(item.Details, item.Quantity);

                    MessageSystem.AddMessage($"Вы подобрали: {item.Details.Name} x{item.Quantity}");
                    break;

                case "Осмотреть":
                    item.Details.Read();
                    Console.WriteLine("\nНажмите любую клавишу...");
                    Console.ReadKey();
                    PickUpItem(item); // Возвращаемся к выбору действия
                    break;

                case "Оставить":
                    MessageSystem.AddMessage($"Вы оставили {item.Details.Name} на земле");
                    break;
            }
        }

        // Метод для проверки квестовых предметов
        private static void CheckQuestItemPickup(Item item, int quantity)
        {
            foreach (var quest in _player.QuestLog.ActiveQuests)
            {
                var questItem = quest.QuestItems.FirstOrDefault(qi => qi.Details.ID == item.ID);
                if (questItem != null)
                {
                    MessageSystem.AddMessage($"Найден предмет для квеста: {quest.Name}");
                }
            }
        }


    }

}

