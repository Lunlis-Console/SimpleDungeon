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
                if (_needsRedraw)
                {
                    GameServices.Renderer.RenderGameWorld(_player, _player.CurrentLocation);
                    _needsRedraw = false;
                }

                // Быстрая проверка ввода
                while (Console.KeyAvailable)
                {
                    var keyInfo = Console.ReadKey(true);

                    if (keyInfo.Key == ConsoleKey.F3)
                    {
                        DebugConsole.Toggle();
                        if (!DebugConsole.IsVisible)
                        {
                            // Перерисовываем игру после закрытия консоли
                            _needsRedraw = true;
                        }
                        break;
                    }

                    // Если дебаг-консоль не видна, обрабатываем игровые клавиши
                    if (!DebugConsole.IsVisible)
                    {
                        ProcessGameKey(keyInfo.Key);
                    }

                    Thread.Sleep(DebugConsole.IsVisible ? 1 : 10);
                }

                // Отрисовка дебаг-консоли если видна
                if (DebugConsole.IsVisible)
                {
                    DebugConsole.Update();
                }

                Thread.Sleep(DebugConsole.IsVisible ? 1 : 10);
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
                    _needsRedraw = true;
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
                    CharacterScreen.Show(_player);
                    _needsRedraw = true;
                    break;
                case ConsoleKey.J:
                    _player.QuestLog.DisplayQuestLog();
                    _needsRedraw = true;
                    break;
                case ConsoleKey.Escape:
                    ShowGameMenu();
                    _needsRedraw = true;
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

            Console.Clear();
            //DisplayUI();
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

