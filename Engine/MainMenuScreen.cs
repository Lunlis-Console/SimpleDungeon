namespace Engine
{
    public class MainMenuScreen : BaseScreen
    {
        private int _selectedIndex;
        private readonly string[] _menuItems =
        {
            "НОВАЯ ИГРА",
            "ЗАГРУЗИТЬ ИГРУ",
            "НАСТРОЙКИ",
            "ВЫХОД"
        };

        public MainMenuScreen()
        {
            _selectedIndex = 0;

            // Даем время на создание экрана
            Thread.Sleep(50);

            // Принудительно запрашиваем перерисовку
            ScreenManager.RequestFullRedraw();
        }

        public override void Render()
        {

            try
            {
                //DebugConsole.Log("MainMenuScreen.Render() called");

                // Отрисовывает весь экран в метода частичной отрисовки!!!
                GameServices.BufferedRenderer.SetNeedsFullRedraw();

                _renderer.BeginFrame();
                ClearScreen();

                RenderHeader("SIMPLE DUNGEON", 2, ConsoleColor.Magenta);
                RenderMenuOptions();
                RenderFooter("W/S - выбор │ E - выбрать │ ESC - выход", 2);
                RenderVersionInfo();

                _renderer.EndFrame();

                //DebugConsole.Log("Main menu rendered successfully");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Main menu render error: {ex.Message}");
                try { _renderer.EndFrame(); } catch { }
            }
        }
        private void RenderMenuOptions()
        {
            int startY = 8;

            for (int i = 0; i < _menuItems.Length; i++)
            {
                bool isSelected = i == _selectedIndex;
                int y = startY + i * 2;
                string displayText = _menuItems[i];
                int x = (Width - displayText.Length) / 2;

                if (isSelected)
                {
                    // Явно очищаем старую позицию курсора
                    _renderer.Write(x - 2, y, "  "); // Очищаем старый курсор
                    _renderer.Write(x - 2, y, "> ", ConsoleColor.Green);
                    _renderer.Write(x, y, displayText, ConsoleColor.Green);
                }
                else
                {
                    // Очищаем потенциальный старый курсор
                    _renderer.Write(x - 2, y, "  ");
                    _renderer.Write(x, y, displayText, ConsoleColor.White);
                }
            }
        }
        private void RenderVersionInfo()
        {
            string version = "Версия 1.0.0-alpha.1";
            string copyright = "© 2025 Ilingin.prod";

            RenderCenteredText(Height - 4, version, ConsoleColor.DarkGray);
            RenderCenteredText(Height - 3, copyright, ConsoleColor.DarkGray);
        }
        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw(); // Только частичное обновление
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_menuItems.Length - 1, _selectedIndex + 1);
                    RequestPartialRedraw(); // Только частичное обновление
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    ExecuteSelectedAction();
                    // Здесь будет автоматически вызвана полная перерисовка при смене экрана
                    break;

                case ConsoleKey.Escape:
                    Environment.Exit(0);
                    break;
            }
        }
        private void ExecuteSelectedAction()
        {
            switch (_selectedIndex)
            {
                case 0: // Новая игра
                    StartNewGame();
                    break;

                case 1: // Загрузить игру
                    ShowLoadGameMenu();
                    break;

                case 2: // Настройки
                    ShowSettingsMenu();
                    break;

                case 3: // Выход
                    Environment.Exit(0);
                    break;
            }
        }

        private void StartNewGame()
        {
            try
            {
                var player = GameServices.GameFactory.CreateNewPlayer();
                player.CurrentLocation = GameServices.WorldRepository.LocationByID(Constants.LOCATION_ID_VILLAGE);

                // Стартовые предметы
                player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_HELMET), 1);
                player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_SPIDER_SILK), 1);
                player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_ARMOR), 1);
                player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_GLOVES), 1);
                player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_LEATHER_BOOTS), 1);
                player.Inventory.AddItem(GameServices.WorldRepository.ItemByID(Constants.ITEM_ID_RUSTY_SWORD), 1);

                MessageSystem.ClearMessages();
                MessageSystem.AddMessage("Добро пожаловать в игру!");

                // ВАЖНО: Полностью очищаем стек перед добавлением игрового экрана
                while (ScreenManager.ScreenCount > 0)
                {
                    ScreenManager.PopScreen();
                }

                ScreenManager.PushScreen(new GameWorldScreen(player));

            }
            catch (Exception ex)
            {
                MessageSystem.AddMessage($"Ошибка при создании игры: {ex.Message}");
            }
        }
        private void ShowLoadGameMenu()
        {
            ScreenManager.PushScreen(new LoadGameScreen());
        }

        private void ShowSettingsMenu()
        {
            MessageSystem.AddMessage("Система настроек в разработке!");
        }
    }
}