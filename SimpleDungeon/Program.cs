using Engine;

namespace SimpleDungeon
{
    public static class Program
    {
        private static Player _player;
        private static bool _needsRedraw = true;

        public static void Main(string[] args)
        {
            Console.Title = "Simple Dungeon";
            Console.CursorVisible = false;

            // Включим консоль сразу для диагностики
            DebugConsole.SetEnabled(true);
            DebugConsole.Log("Application started");

            // Инициализация сервисов
            try
            {
                GameServices.Initialize();
                DebugConsole.Log("GameServices initialized successfully");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"GameServices init failed: {ex.Message}");
                Console.WriteLine($"Ошибка инициализации: {ex.Message}");
                Console.ReadKey();
                return;
            }

            bool running = true;

            while (running)
            {
                ShowMainMenu();

                // После возврата из игрового цикла спрашиваем, что делать дальше
                //Console.Clear();
                Console.WriteLine("1. Новая игра");
                Console.WriteLine("2. Выйти");

                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.D2 || key.Key == ConsoleKey.NumPad2)
                {
                    running = false;
                }
            }

            GameServices.Shutdown();
        }


        // В Program.cs замените ProcessKeyInput
        private static void ProcessKeyInput()
        {
            bool inGame = true;
            bool firstFrame = true; // Добавляем флаг первого кадра

            ScreenManager.RenderCurrentScreen();

            while (inGame)
            {
                try
                {
                    if (firstFrame)
                    {
                        // Принудительная полная перерисовка на первом кадре
                        GameServices.BufferedRenderer.SetNeedsFullRedraw();
                        ScreenManager.SetNeedsRedraw();
                        firstFrame = false;
                    }
                    // Обновляем входные данные
                    InputManager.Update();

                    // Проверка изменения размера окна
                    GameServices.BufferedRenderer.CheckWindowResize();

                    // Глобальные горячие клавиши (обрабатываются в первую очередь)
                    if (InputManager.GetKeyDown(ConsoleKey.F3))
                    {
                        DebugConsole.Toggle();
                        InputManager.Clear();
                        continue;
                    }

                    // Если консоль видна - передаем управление ей
                    if (DebugConsole.IsVisible)
                    {
                        var keyInfo = InputManager.GetKeyInfo();
                        if (keyInfo.Key != ConsoleKey.NoName)
                        {
                            DebugConsole.ProcessInput(keyInfo);
                        }

                        // Отрисовываем независимо от ввода, если консоль видима
                        ScreenManager.RenderCurrentScreen();
                        DebugConsole.GlobalDraw();

                        Thread.Sleep(50);
                        continue;
                    }

                    // Обычная обработка ввода через ScreenManager
                    var inputKey = InputManager.GetKeyInfo();
                    if (inputKey.Key != ConsoleKey.NoName)
                    {
                        ScreenManager.HandleInput(inputKey);
                    }

                    // Добавьте проверку на возврат в главное меню
                    if (ScreenManager.ScreenCount == 0)
                    {
                        DebugConsole.Log("Returning to main menu");
                        inGame = false;
                        break;
                    }

                    // Отрисовка
                    if (ScreenManager.NeedsRedraw || GameServices.BufferedRenderer.CheckWindowResize())
                    {
                        ScreenManager.RenderCurrentScreen();
                        DebugConsole.GlobalDraw();
                    }

                    Thread.Sleep(50);
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"Ошибка в игровом цикле: {ex.Message}");
                    Thread.Sleep(100);
                }
            }
        }
        private static BaseScreen GetCurrentScreen()
        {
            // Здесь нужно будет реализовать логику получения текущего экрана
            // Это может быть через рефлексию или систему регистрации экранов
            return null; // заглушка
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
                    ScreenManager.PushScreen(new InventoryScreen(_player));
                    break;
                case ConsoleKey.L:
                    _player.LookAround();
                    _needsRedraw = true;
                    break;
                case ConsoleKey.E:
                    InteractWithWorld();
                    _needsRedraw = true;
                    break;
                case ConsoleKey.H:
                    HelpWorld();
                    _needsRedraw = true;
                    break;
                case ConsoleKey.C:
                    ScreenManager.PushScreen(new CharacterScreen(_player));
                    _needsRedraw = true;
                    break;
                case ConsoleKey.J:
                    ScreenManager.PushScreen(new QuestLogScreen(_player));
                    break;
                case ConsoleKey.Escape:
                    ScreenManager.PushScreen(new GameMenuScreen(_player));
                    break;
                case ConsoleKey.F5:
                    SaveManager.SaveGame(_player, "quicksave");
                    MessageSystem.AddMessage("Игра сохранена!");
                    _needsRedraw = true;
                    break;
                case ConsoleKey.F9:
                    try
                    {
                        _player = SaveManager.LoadGame("quicksave", GameServices.WorldRepository);
                        MessageSystem.AddMessage("Быстрая загрузка выполнена!");
                        _needsRedraw = true;
                    }
                    catch
                    {
                        MessageSystem.AddMessage("Быстрое сохранение не найдено!");
                        _needsRedraw = true;
                    }
                    break;
                default:
                    MessageSystem.AddMessage("Неизвестная команда. Нажмите H для помощи.");
                    _needsRedraw = true;
                    break;
            }
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
        private static void HelpWorld()
        {
            Console.Clear();

            Console.WriteLine("=================Помощь=================");
            Console.WriteLine("WASD - Перемещение между локациями");
            Console.WriteLine("I - Открыть сумку (инвентарь)");
            Console.WriteLine("L - Осмотреться вокруг (в разработке)");
            Console.WriteLine("E - Взаимодействие");
            Console.WriteLine("J - Журнал заданий");
            Console.WriteLine("H - Показать помощь");
            Console.WriteLine("ESC - Выйти из игры");
            Console.WriteLine("========================================");

            Console.WriteLine("\nНажмите любую клавишу, чтобы закрыть описание...");
            Console.ReadKey();
            Console.Clear();
        }
        private static void ShowMainMenu()
        {
            int selectedIndex = 0;
            string[] menuItems = { "Новая игра", "Загрузить игру", "Выход" };
            bool inMenu = true;

            while (inMenu)
            {
                Console.Clear();
                Console.WriteLine("===================================");
                Console.WriteLine("         SIMPLE DUNGEON");
                Console.WriteLine("===================================");

                // Отображаем пункты меню
                for (int i = 0; i < menuItems.Length; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write("> ");
                    }
                    else
                    {
                        Console.Write("  ");
                    }
                    Console.WriteLine(menuItems[i]);
                    Console.ResetColor();
                }

                Console.WriteLine("===================================");
                Console.WriteLine("W/S - выбор, Enter - подтвердить");

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + menuItems.Length) % menuItems.Length;
                        break;

                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % menuItems.Length;
                        break;

                    case ConsoleKey.Enter:
                    case ConsoleKey.E:
                        inMenu = false;
                        ExecuteMainMenuChoice(selectedIndex);
                        break;

                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        Environment.Exit(0);
                        break;
                }
            }
        }
        private static void ExecuteMainMenuChoice(int choice)
        {
            switch (choice)
            {
                case 0: // Новая игра
                    StartNewGame();
                    // ProcessKeyInput() ЗАПУСКАЕТСЯ внутри StartNewGame()
                    break;

                case 1: // Загрузить игру
                    ShowLoadGameMenu();
                    break;

                case 2: // Выход
                    Console.WriteLine("\nВыход из игры...");
                    Environment.Exit(0);
                    break;
            }

            // После завершения игрового цикла автоматически возвращаемся в главное меню
            // НЕ очищаем консоль!
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
                    _player = SaveManager.LoadGame(selectedSave, GameServices.WorldRepository);
                    MessageSystem.AddMessage($"Игра загружена: {selectedSave}");

                    Console.Clear();
                    //DisplayUI();

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
            try
            {
                GameServices.Initialize();
                _player = GameServices.GameFactory.CreateNewPlayer();
                _player.CurrentLocation = GameServices.WorldRepository.LocationByID(Constants.LOCATION_ID_VILLAGE);

                // Стартовые предметы
                _player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_HELMET), 1);
                _player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_SPIDER_SILK), 1);
                _player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_ARMOR), 1);
                _player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_GLOVES), 1);
                _player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_BOOTS), 1);
                _player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_RUSTY_SWORD), 1);

                MessageSystem.ClearMessages();
                MessageSystem.AddMessage("Добро пожаловать в игру!");

                

                // Создаем экран и НЕМЕДЛЕННО отрисовываем
                ScreenManager.PushScreen(new GameWorldScreen(_player));
                ScreenManager.RenderCurrentScreen(); // ДОБАВИТЬ ЭТУ СТРОКУ
                Console.WriteLine("Запуск RenderCurrentScreen()");

                Thread.Sleep(1000);

                // Запускаем игровой цикл
                ProcessKeyInput();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при создании игры: {ex.Message}");
                Console.ReadKey();
            }
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
                    _player = SaveManager.LoadGame(selectedSave, GameServices.WorldRepository);

                    MessageSystem.AddMessage($"Игра загружена: {selectedSave}");

                    Console.Clear();
                    //DisplayUI();

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

