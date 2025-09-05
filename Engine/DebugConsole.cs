public static class DebugConsole
{
    private static readonly List<string> _logMessages = new List<string>();
    private static bool _isVisible = false;
    private static string _inputBuffer = "";
    private static int _viewOffset = 0;
    private const int MAX_MESSAGES = 200;
    private static bool _needsRedraw = false;
    private static bool _enabled = true; // Флаг включения/выключения консоли

    public static bool IsVisible => _isVisible;
    public static bool Enabled => _enabled;

    // Включить/выключить консоль полностью
    public static void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled && _isVisible)
        {
            _isVisible = false;
        }
    }

    public static void Toggle()
    {
        if (!_enabled) return;

        _isVisible = !_isVisible;
        if (_isVisible)
        {
            _inputBuffer = "";
            _viewOffset = 0;
        }

        _needsRedraw = true;
        ScreenManager.SetNeedsRedraw(); // Убедитесь, что эта строка есть
    }

    public static void Log(string message)
    {
        if (!_enabled) return;

        _logMessages.Add($"[{DateTime.Now:HH:mm:ss}] {message}");
        if (_logMessages.Count > MAX_MESSAGES)
        {
            _logMessages.RemoveAt(0);
        }
        _viewOffset = 0;
        _needsRedraw = true;
    }

    public static void Update()
    {
        if (!_isVisible || !_enabled) return;

        while (Console.KeyAvailable)
        {
            var key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.Escape:
                    _isVisible = false;
                    _needsRedraw = true;
                    break;

                case ConsoleKey.Enter:
                    ExecuteCommand(_inputBuffer);
                    _inputBuffer = "";
                    _needsRedraw = true;
                    break;

                case ConsoleKey.Backspace:
                    if (_inputBuffer.Length > 0)
                    {
                        _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                        _needsRedraw = true;
                    }
                    break;

                case ConsoleKey.PageUp:
                    _viewOffset = Math.Min(_viewOffset + 1, Math.Max(0, _logMessages.Count - (Console.WindowHeight - 6)));
                    _needsRedraw = true;
                    break;

                case ConsoleKey.PageDown:
                    _viewOffset = Math.Max(0, _viewOffset - 1);
                    _needsRedraw = true;
                    break;

                case ConsoleKey.Home:
                    _viewOffset = Math.Max(0, _logMessages.Count - (Console.WindowHeight - 6));
                    _needsRedraw = true;
                    break;

                case ConsoleKey.End:
                    _viewOffset = 0;
                    _needsRedraw = true;
                    break;

                default:
                    if (!char.IsControl(key.KeyChar))
                    {
                        _inputBuffer += key.KeyChar;
                        _needsRedraw = true;
                    }
                    break;
            }
        }

        if (_needsRedraw)
        {
            Draw();
            _needsRedraw = false;
        }
    }

    public static void Draw()
    {
        if (!_isVisible || !_enabled) return;

        var prevForeground = Console.ForegroundColor;
        var prevBackground = Console.BackgroundColor;

        int width = Console.WindowWidth - 4;
        int height = Math.Min(20, Console.WindowHeight - 6);
        int maxMessagesToShow = height - 2;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.BackgroundColor = ConsoleColor.Black;

        // Рамка консоли
        Console.SetCursorPosition(2, 2);
        Console.Write("╔" + new string('═', width) + "╗");

        for (int i = 0; i < height; i++)
        {
            Console.SetCursorPosition(2, 3 + i);
            Console.Write("║" + new string(' ', width) + "║");
        }

        Console.SetCursorPosition(2, 3 + height);
        Console.Write("╚" + new string('═', width) + "╝");

        // Заголовок
        Console.ForegroundColor = ConsoleColor.Yellow;
        Console.SetCursorPosition(4, 2);
        string title = $"DEBUG CONSOLE (Сообщений: {_logMessages.Count})";
        if (_logMessages.Count > maxMessagesToShow)
        {
            int totalPages = (int)Math.Ceiling((double)_logMessages.Count / maxMessagesToShow);
            int currentPage = (int)Math.Ceiling((double)_viewOffset / maxMessagesToShow) + 1;
            title += $" [Страница {currentPage}/{totalPages}]";
        }
        Console.Write(title.PadRight(width - 2));

        // Сообщения
        Console.ForegroundColor = ConsoleColor.White;
        int startIndex = Math.Max(0, _logMessages.Count - maxMessagesToShow - _viewOffset);
        int endIndex = Math.Min(_logMessages.Count, startIndex + maxMessagesToShow);

        for (int i = 0; i < maxMessagesToShow; i++)
        {
            int messageIndex = startIndex + i;
            if (messageIndex < _logMessages.Count)
            {
                Console.SetCursorPosition(4, 4 + i);
                string message = _logMessages[messageIndex];
                if (message.Length > width - 2)
                    message = message.Substring(0, width - 2);
                Console.Write(message.PadRight(width - 2));
            }
        }

        // Подсказка управления
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.SetCursorPosition(4, 3 + height - 1);
        string helpText = "PgUp: старее ↑ | PgDn: новее ↓ | Home: начало | End: конец";
        if (helpText.Length > width - 2)
            helpText = helpText.Substring(0, width - 2);
        Console.Write(helpText.PadRight(width - 2));

        // Ввод
        Console.ForegroundColor = ConsoleColor.Green;
        Console.SetCursorPosition(4, 3 + height);
        string inputDisplay = "> " + _inputBuffer;
        if (inputDisplay.Length > width - 2)
            inputDisplay = inputDisplay.Substring(0, width - 2);
        Console.Write(inputDisplay.PadRight(width - 2));

        // Восстанавливаем цвета
        Console.ForegroundColor = prevForeground;
        Console.BackgroundColor = prevBackground;
    }

    private static void ExecuteCommand(string command)
    {
        Log($"Executing: {command}");

        var parts = command.Split(' ');
        var cmd = parts[0].ToLower();
        var args = parts.Skip(1).ToArray();

        switch (cmd)
        {
            case "help":
                Log("Доступные команды:");
                Log("  help - показать справку");
                Log("  clear - очистить консоль");
                Log("  enable - включить консоль");
                Log("  disable - выключить консоль");
                Log("  top - к старым сообщениям");
                Log("  bottom - к новым сообщениям");
                break;

            case "clear":
                _logMessages.Clear();
                Log("Консоль очищена");
                break;

            case "enable":
                SetEnabled(true);
                Log("Консоль включена");
                break;

            case "disable":
                SetEnabled(false);
                Log("Консоль выключена");
                break;

            case "top":
                _viewOffset = Math.Max(0, _logMessages.Count - (Console.WindowHeight - 6));
                Log("Переход к старым сообщениям");
                break;

            case "bottom":
                _viewOffset = 0;
                Log("Переход к новым сообщениям");
                break;

            default:
                Log($"Неизвестная команда: {cmd}");
                break;
        }
    }

    public static void ProcessInput(ConsoleKeyInfo keyInfo)
    {
        if (!_enabled) return;

        switch (keyInfo.Key)
        {
            case ConsoleKey.Escape:
                _isVisible = false;
                _needsRedraw = true;
                ScreenManager.SetNeedsRedraw();
                break;

            case ConsoleKey.Enter:
                ExecuteCommand(_inputBuffer);
                _inputBuffer = "";
                _needsRedraw = true;
                break;

            case ConsoleKey.Backspace:
                if (_inputBuffer.Length > 0)
                {
                    _inputBuffer = _inputBuffer.Substring(0, _inputBuffer.Length - 1);
                    _needsRedraw = true;
                }
                break;

            case ConsoleKey.PageUp:
                _viewOffset = Math.Min(_viewOffset + 1, Math.Max(0, _logMessages.Count - (Console.WindowHeight - 6)));
                _needsRedraw = true;
                break;

            case ConsoleKey.PageDown:
                _viewOffset = Math.Max(0, _viewOffset - 1);
                _needsRedraw = true;
                break;

            case ConsoleKey.Home:
                _viewOffset = Math.Max(0, _logMessages.Count - (Console.WindowHeight - 6));
                _needsRedraw = true;
                break;

            case ConsoleKey.End:
                _viewOffset = 0;
                _needsRedraw = true;
                break;

            default:
                // Используем keyInfo.KeyChar вместо keyChar
                if (!char.IsControl(keyInfo.KeyChar) && keyInfo.KeyChar != '\0')
                {
                    _inputBuffer += keyInfo.KeyChar;
                    _needsRedraw = true;
                }
                break;
        }
    }

    public static void HandleKey(ConsoleKey key, char keyChar = '\0')
    {
        
    }

    // Метод для глобального доступа из любого меню
    public static void GlobalUpdate()
    {
        if (!_enabled) return;

        // Убрать проверку F3 здесь, так как она теперь обрабатывается в ProcessKeyInput
        if (_isVisible)
        {
            Update();

            // После обновления консоли устанавливаем флаг перерисовки
            if (_needsRedraw)
            {
                ScreenManager.SetNeedsRedraw();
            }
        }
    }
    // Метод для глобальной отрисовки из любого меню
    public static void GlobalDraw()
    {
        if (!_isVisible || !_enabled) return;
        {
            Draw();
        }
    }
}