using Engine.Core;
using Engine.Entities;
using Engine.Saving;

namespace Engine.UI
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
            ClearScreen();

            RenderHeader("МЕНЮ ИГРЫ");
            RenderMenuOptions();
            RenderPlayerInfo();
            RenderFooter("W/S - выбор │ E - выбрать │ Q - назад в игру");
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
                    _renderer.Write(2, 4 + i, "► ");
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
            HandleCommonInput(keyInfo);

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(4, _selectedIndex + 1);
                    RequestPartialRedraw();
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
            // Заменяем старую реализацию с Console.ReadLine():
            ScreenManager.PushScreen(new UnifiedSaveGameScreen(_player));
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
            ScreenManager.RequestPartialRedraw();
        }

        private void ReturnToMainMenu()
        {
            // Вместо старой реализации:
            ScreenManager.PushScreen(new UnifiedReturnScreen());
        }

        private void ExitGame()
        {
            // Заменяем старую реализацию:
            ScreenManager.PushScreen(new UnifiedExitScreen());
        }
    }
}