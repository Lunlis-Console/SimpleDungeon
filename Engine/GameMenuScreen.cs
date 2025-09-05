namespace Engine
{
    public class GameMenuScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;

        public GameMenuScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("МЕНЮ ИГРЫ");
            RenderMenuOptions();
            RenderPlayerInfo();
            RenderFooter("W/S - выбор │ E - выбрать │ Q - назад в игру");

            _renderer.EndFrame();
        }

        private void RenderMenuOptions()
        {
            var options = new List<string>
            {
                "Сохранить игру",
                "Загрузить игру",
                "Настройки",
                "Главное меню",
                "Выйти из игры"
            };

            for (int i = 0; i < options.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                if (isSelected)
                {
                    _renderer.Write(2, 4 + i, "> ");
                    _renderer.Write(4, 4 + i, options[i], ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, 4 + i, options[i]);
                }
            }
        }

        private void RenderPlayerInfo()
        {
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 4;

            _renderer.Write(rightColumn, y, "=== ИНФОРМАЦИЯ ===", ConsoleColor.Yellow);
            y += 2;

            _renderer.Write(rightColumn, y, $"Игрок: Ур. {_player.Level}");
            y++;
            _renderer.Write(rightColumn, y, $"Локация: {_player.CurrentLocation.Name}");
            y++;
            _renderer.Write(rightColumn, y, $"Здоровье: {_player.CurrentHP}/{_player.TotalMaximumHP}");
            y++;
            _renderer.Write(rightColumn, y, $"Золото: {_player.Gold}");
            y++;
            _renderer.Write(rightColumn, y, $"Убито монстров: {_player.MonstersKilled}");
            y++;
            _renderer.Write(rightColumn, y, $"Квестов: {_player.QuestsCompleted}");
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(4, _selectedIndex + 1);
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    ExecuteSelectedAction();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void ExecuteSelectedAction()
        {
            switch (_selectedIndex)
            {
                case 0: // Сохранить игру
                    ShowSaveGameMenu();
                    break;
                case 1: // Загрузить игру
                    ShowLoadGameMenu();
                    break;
                case 2: // Настройки
                    ShowSettingsMenu();
                    break;
                case 3: // Главное меню
                    ReturnToMainMenu();
                    break;
                case 4: // Выйти из игры
                    ExitGame();
                    break;
            }
        }

        private void ShowSaveGameMenu()
        {
            Console.Clear();
            Console.Write("Введите название сохранения: ");
            string saveName = Console.ReadLine();

            if (!string.IsNullOrWhiteSpace(saveName))
            {
                SaveManager.SaveGame(_player, saveName);
                MessageSystem.AddMessage($"Игра сохранена: {saveName}");
            }
            else
            {
                SaveManager.SaveGame(_player, $"save_{DateTime.Now:yyyyMMdd_HHmmss}");
                MessageSystem.AddMessage("Игра сохранена с автоматическим названием");
            }

            ScreenManager.SetNeedsRedraw();
        }

        private void ShowLoadGameMenu()
        {
            var saves = SaveManager.GetAvailableSaves();

            if (saves.Count == 0)
            {
                MessageSystem.AddMessage("Нет доступных сохранений!");
                return;
            }

            ScreenManager.PushScreen(new LoadGameScreen());
        }

        private void ShowSettingsMenu()
        {
            MessageSystem.AddMessage("Система настроек в разработке!");
            ScreenManager.SetNeedsRedraw();
        }

        private void ReturnToMainMenu()
        {
            if (ConfirmAction("Вернуться в главное меню? Несохраненный прогресс будет потерян."))
            {
                // Вместо ScreenManager.ReturnToMainScreen() используем:
                while (ScreenManager.ScreenCount > 0)
                {
                    ScreenManager.PopScreen();
                }
                // Теперь ProcessKeyInput() автоматически завершится и вернет в главное меню
            }
        }

        private void ExitGame()
        {
            if (ConfirmAction("Выйти из игры?"))
            {
                Environment.Exit(0);
            }
        }

        private bool ConfirmAction(string message)
        {
            // Простая реализация подтверждения
            Console.Clear();
            Console.WriteLine(message);
            Console.WriteLine("Нажмите Y для подтверждения или любую другую клавишу для отмены");
            return Console.ReadKey(true).Key == ConsoleKey.Y;
        }
    }
}