using Engine.Entities;

namespace Engine.UI
{
    public class UnifiedSaveGameScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;
        private string _saveName = "";
        private List<SaveFileInfo> _saveFiles;
        private bool _isEnteringName = true;

        public UnifiedSaveGameScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;
            LoadSaveFiles();
        }

        // Реализация абстрактного метода Render
        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("СОХРАНЕНИЕ ИГРЫ");
            RenderInstructions();

            if (_isEnteringName)
            {
                RenderNameInput();
            }
            else
            {
                RenderSavesList();
            }

            RenderFooter("W/S - выбор │ E - подтвердить │ Q - отмена │ Backspace - удалить");

            _renderer.EndFrame();
        }

        // Реализация абстрактного метода HandleInput
        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (_isEnteringName)
            {
                HandleNameInput(keyInfo);
            }
            else
            {
                HandleSavesList(keyInfo);
            }
        }

        // Реализация абстрактного метода Update (если нужно)
        public override void Update()
        {
            // Базовая реализация
        }

        private class SaveFileInfo
        {
            public string Name { get; set; } = string.Empty;
            public DateTime LastWriteTime { get; set; }
        }

        private void LoadSaveFiles()
        {
            _saveFiles = new List<SaveFileInfo>();
            var saveNames = SaveManager.GetAvailableSaves();

            foreach (var saveName in saveNames)
            {
                string filePath = Path.Combine(SaveManager.SavesDirectory, $"{saveName}.json");
                if (File.Exists(filePath))
                {
                    var fileInfo = new FileInfo(filePath);
                    _saveFiles.Add(new SaveFileInfo
                    {
                        Name = saveName,
                        LastWriteTime = fileInfo.LastWriteTime
                    });
                }
            }

            _saveFiles = _saveFiles
                .OrderByDescending(f => f.LastWriteTime)
                .ToList();
        }

        private void RenderInstructions()
        {
            int y = 4;
            var instructions = WrapText(
                "Введите название сохранения или выберите существующее для перезаписи:",
                Console.WindowWidth - 4
            );

            foreach (var line in instructions)
            {
                _renderer.Write(2, y, line, ConsoleColor.Gray);
                y++;
            }
            y++;
        }

        private void RenderNameInput()
        {
            int y = 7;

            _renderer.Write(2, y, "Название сохранения:", ConsoleColor.Cyan);
            y++;

            string inputDisplay = _saveName;
            if (DateTime.Now.Second % 2 == 0)
            {
                inputDisplay += "_";
            }

            _renderer.Write(4, y, inputDisplay, ConsoleColor.White);
            y += 2;

            _renderer.Write(2, y, "[Enter] - Сохранить", ConsoleColor.DarkGray);
            y++;
            _renderer.Write(2, y, "[Tab] - Показать существующие сохранения", ConsoleColor.DarkGray);
        }

        private void RenderSavesList()
        {
            int y = 7;

            _renderer.Write(2, y, "Существующие сохранения (новые сверху):", ConsoleColor.Cyan);
            y += 2;

            if (_saveFiles.Count == 0)
            {
                _renderer.Write(4, y, "Нет сохранений", ConsoleColor.DarkGray);
                y++;
            }
            else
            {
                for (int i = 0; i < _saveFiles.Count; i++)
                {
                    bool isSelected = i == _selectedIndex;
                    var saveFile = _saveFiles[i];
                    string displayName = $"{saveFile.Name} [{saveFile.LastWriteTime:dd.MM.yy HH:mm}]";

                    if (isSelected)
                    {
                        _renderer.Write(2, y, "► ", ConsoleColor.Green);
                        _renderer.Write(4, y, displayName, ConsoleColor.Green);
                    }
                    else
                    {
                        _renderer.Write(4, y, displayName);
                    }
                    y++;
                }
            }

            y += 2;
            _renderer.Write(2, y, "[Tab] - Вернуться к вводу названия", ConsoleColor.DarkGray);
        }

        private void HandleNameInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Enter:
                    SaveGame();
                    break;

                case ConsoleKey.Tab:
                    _isEnteringName = false;
                    RequestFullRedraw();
                    break;

                case ConsoleKey.Backspace:
                    if (_saveName.Length > 0)
                    {
                        _saveName = _saveName.Substring(0, _saveName.Length - 1);
                        RequestPartialRedraw();
                    }
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;

                default:
                    if (!char.IsControl(keyInfo.KeyChar))
                    {
                        _saveName += keyInfo.KeyChar;
                        RequestPartialRedraw();
                    }
                    break;
            }
        }

        private void HandleSavesList(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_saveFiles.Count - 1, _selectedIndex + 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.Enter:
                    _saveName = _saveFiles[_selectedIndex].Name;
                    SaveGame();
                    break;

                case ConsoleKey.Tab:
                    _isEnteringName = true;
                    RequestFullRedraw();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void SaveGame()
        {
            if (string.IsNullOrWhiteSpace(_saveName))
            {
                _saveName = $"save_{DateTime.Now:yyyyMMdd_HHmmss}";
            }

            SaveManager.SaveGame(_player, _saveName);
            MessageSystem.AddMessage($"Игра сохранена: {_saveName}");
            ScreenManager.PopScreen();
        }
    }
}