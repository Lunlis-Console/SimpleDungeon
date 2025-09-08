using Engine.Core;

namespace Engine.UI
{
    public class LoadGameScreen : BaseScreen
    {
        private int _selectedIndex;
        private List<string> _saves;

        public LoadGameScreen()
        {
            _saves = SaveManager.GetAvailableSaves();
            _selectedIndex = 0;
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("ЗАГРУЗИТЬ ИГРУ");
            RenderSavesList();
            RenderFooter("W/S - выбор │ E - загрузить │ D - удалить │ Q - назад");

            _renderer.EndFrame();
        }

        private void RenderSavesList()
        {
            int y = 4;

            if (_saves.Count == 0)
            {
                _renderer.Write(2, y, "Нет доступных сохранений.", ConsoleColor.DarkGray);
                return;
            }

            for (int i = 0; i < _saves.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                if (isSelected)
                {
                    _renderer.Write(2, y, "► ");
                    _renderer.Write(4, y, _saves[i], ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, y, _saves[i]);
                }
                y++;
            }
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (_saves.Count == 0) return;

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_saves.Count - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    LoadSelectedSave();
                    break;

                case ConsoleKey.D:
                    DeleteSelectedSave();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void LoadSelectedSave()
        {
            try
            {
                var player = SaveManager.LoadGame(_saves[_selectedIndex], GameServices.WorldRepository);
                // Здесь нужно обновить игрока и вернуться в игру
                MessageSystem.AddMessage($"Игра загружена: {_saves[_selectedIndex]}");
                ScreenManager.ReturnToMainScreen();
            }
            catch (Exception ex)
            {
                MessageSystem.AddMessage($"Ошибка загрузки: {ex.Message}");
            }
        }

        private void DeleteSelectedSave()
        {
            if (ConfirmAction($"Удалить сохранение '{_saves[_selectedIndex]}'?"))
            {
                SaveManager.DeleteSave(_saves[_selectedIndex]);
                _saves = SaveManager.GetAvailableSaves();
                _selectedIndex = Math.Min(_selectedIndex, _saves.Count - 1);
                MessageSystem.AddMessage("Сохранение удалено.");
            }
        }

        private bool ConfirmAction(string message)
        {
            // Реализация подтверждения
            return true;
        }
    }
}