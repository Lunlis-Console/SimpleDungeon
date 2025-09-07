using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Engine;

public static class DebugConsole
{
    private static readonly List<string> _logMessages = new List<string>();
    private static bool _isVisible = false;
    private static string _inputBuffer = "";
    private static int _viewOffset = 0;
    private const int MAX_MESSAGES = 200;
    private static bool _needsRedraw = false;
    private static bool _enabled = true; // Флаг включения/выключения консоли
    private static bool _needsRedrawAfterHide = false;

    // Добавляем путь к файлу логов
    private static readonly string _logFilePath = "debug_console.log";
    private static bool _logToFile = false; // Флаг для включения/выключения записи в файл
    public static bool NeedsRedrawAfterHide => _needsRedrawAfterHide;
    public static bool IsVisible => _isVisible;
    public static bool Enabled => _enabled;

    static DebugConsole()
    {
        Initialize();
    }

    public static void Initialize()
    {
        // Автоматически включаем запись в файл при старте
        SetFileLogging(true);

        // Автоматически показываем консоль при старте (опционально)
        _isVisible = true;
        _needsRedraw = true;

        DebugConsole.Log("Инициализация консоли в Initialize()");

        Log("Консоль отладки инициализирована");
        Log($"Запись логов в файл: {Path.GetFullPath(_logFilePath)}");
    }

    // Включить/выключить запись в файл
    public static void SetFileLogging(bool enable)
    {
        _logToFile = enable;
        if (enable)
        {
            Log("Запись логов в файл включена");
        }
        else
        {
            Log("Запись логов в файл выключена");
        }
    }

    // Метод для записи в файл
    private static void WriteToLogFile(string message)
    {
        if (!_logToFile) return;

        try
        {
            using (StreamWriter writer = new StreamWriter(_logFilePath, true))
            {
                writer.WriteLine(message);
            }
        }
        catch (Exception ex)
        {
            // В случае ошибки записи, выводим сообщение в консоль
            Console.WriteLine($"Ошибка записи в файл логов: {ex.Message}");
        }
    }

    // Очистка файла логов
    public static void ClearLogFile()
    {
        try
        {
            if (File.Exists(_logFilePath))
            {
                File.WriteAllText(_logFilePath, string.Empty);
                Log("Файл логов очищен");
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка очистки файла логов: {ex.Message}");
        }
    }



    // Включить/выключить консоль полностью
    public static void SetEnabled(bool enabled)
    {
        _enabled = enabled;
        if (!enabled && _isVisible)
        {
            _isVisible = false;
        }
    }

    public static void ResetRedrawFlag()
    {
        _needsRedrawAfterHide = false;
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
        else
        {
            // Устанавливаем флаг необходимости перерисовки после скрытия
            _needsRedrawAfterHide = true;
            GameServices.BufferedRenderer.SetNeedsFullRedraw();
            ScreenManager.RequestFullRedraw();
        }

        _needsRedraw = true;
        ScreenManager.RequestFullRedraw();
    }

    public static void Log(string message)
    {
        if (!_enabled) return;

        string formattedMessage = $"[{DateTime.Now:HH:mm:ss}] {message}";
        _logMessages.Add(formattedMessage);

        // Записываем в файл
        WriteToLogFile(formattedMessage);

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

    // ------------------------
    // ЗАМЕНИТЕ текущий Draw() в DebugConsole на эту версию — с переносом строк
    // ------------------------
    public static void Draw()
    {
        if (!_isVisible || !_enabled) return;

        var prevForeground = Console.ForegroundColor;
        var prevBackground = Console.BackgroundColor;

        int width = Math.Max(20, Console.WindowWidth - 4); // минимальная ширина
        int height = Math.Min(20, Console.WindowHeight - 6);
        int maxLinesToShow = height - 2;

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.BackgroundColor = ConsoleColor.Black;

        // Рисуем рамку
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
        string title = $"DEBUG CONSOLE (сообщений: {_logMessages.Count})";
        Console.Write(title.PadRight(width - 2));

        // Подготовка списка строк с переносом
        var wrappedLines = new List<string>();
        int textWidth = Math.Max(10, width - 2);
        foreach (var msg in _logMessages)
        {
            // каждый лог предварительно переносим по словам
            wrappedLines.AddRange(WrapText(msg, textWidth));
        }

        // вычисляем стартовую строку (viewOffset теперь в строках)
        int startIndex = Math.Max(0, wrappedLines.Count - maxLinesToShow - _viewOffset);
        int endIndex = Math.Min(wrappedLines.Count, startIndex + maxLinesToShow);

        // вывод строк
        Console.ForegroundColor = ConsoleColor.White;
        for (int i = 0; i < maxLinesToShow; i++)
        {
            int lineIndex = startIndex + i;
            Console.SetCursorPosition(4, 4 + i);
            if (lineIndex < wrappedLines.Count)
            {
                string line = wrappedLines[lineIndex];
                if (line.Length > textWidth) line = line.Substring(0, textWidth);
                Console.Write(line.PadRight(textWidth));
            }
            else
            {
                Console.Write(new string(' ', textWidth));
            }
        }

        // подсказка управления (сжатая)
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.SetCursorPosition(4, 3 + height - 1);
        string helpText = "PgUp/PgDn: прокрутка | Enter: выполнить";
        if (helpText.Length > textWidth) helpText = helpText.Substring(0, textWidth);
        Console.Write(helpText.PadRight(textWidth));

        // Ввод
        Console.ForegroundColor = ConsoleColor.Green;
        Console.SetCursorPosition(4, 3 + height);
        string inputDisplay = "► " + _inputBuffer;
        if (inputDisplay.Length > textWidth) inputDisplay = inputDisplay.Substring(inputDisplay.Length - textWidth);
        Console.Write(inputDisplay.PadRight(textWidth));

        // восстанавливаем цвета
        Console.ForegroundColor = prevForeground;
        Console.BackgroundColor = prevBackground;
    }

    // Вспомогательный метод для переноса по словам
    private static IEnumerable<string> WrapText(string text, int maxWidth)
    {
        if (string.IsNullOrEmpty(text)) yield break;
        int pos = 0;
        while (pos < text.Length)
        {
            int len = Math.Min(maxWidth, text.Length - pos);
            // ищем ближайший пробел слева от конца
            if (len == maxWidth)
            {
                int lastSpace = text.LastIndexOf(' ', pos + len - 1, len);
                if (lastSpace > pos)
                {
                    len = lastSpace - pos;
                }
            }

            string part = text.Substring(pos, len).TrimEnd();
            if (part.Length > 0) yield return part;
            pos += len;

            // пропускаем пробелы в начале следующей части
            while (pos < text.Length && text[pos] == ' ') pos++;
        }
    }

    private static void ExecuteCommand(string command)
    {
        Log($"Executing: {command}");

        var parts = command.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length == 0) return;
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
                Log("  export-json [path] - экспортировать game_data в указанную папку (если путь не указан — используется ./game_data)");
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
            case "export-json":
                {
                    // синтаксис: export-json [путь_вывода]
                    string outFolder;
                    if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                        outFolder = args[0];
                    else
                        outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "game_data");

                    Log($"Запрошен экспорт JSON в папку: {outFolder}. Запуск в фоне...");

                    // Запускаем в фоне, чтобы не блокировать UI / основной цикл
                    Task.Run(() =>
                    {
                        int code;
                        string msg = TryInvokeDataExporter(outFolder, out code);
                        Log($"Экспорт завершён: {msg} (код {code})");
                    });

                    break;
                }
            case "export-test":
                {
                    string outFolder;
                    if (args != null && args.Length > 0 && !string.IsNullOrWhiteSpace(args[0]))
                        outFolder = args[0];
                    else
                        outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "..", "..", "game_data"); // безопасный проектный путь

                    Log($"[export-test] Проверка записи в: {outFolder}");

                    Task.Run(() =>
                    {
                        try
                        {
                            // пробуем создать папку (если нельзя — поймаем исключение)
                            Directory.CreateDirectory(outFolder);
                            string testFile = Path.Combine(outFolder, $"write_test_{DateTime.Now:yyyyMMddHHmmss}.txt");
                            File.WriteAllText(testFile, "write test OK");
                            Log($"[export-test] Успех — создан файл: {testFile}");
                        }
                        catch (Exception ex)
                        {
                            Log($"[export-test] Ошибка записи: {ex.GetType().Name}: {ex.Message}");
                            Log(ex.StackTrace ?? "");
                        }
                    });

                    break;
                }


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
                // При скрытии консоли запрашиваем полную перерисовку
                GameServices.BufferedRenderer.SetNeedsFullRedraw();
                ScreenManager.RequestFullRedraw(); // ← Добавить
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
                ScreenManager.RequestPartialRedraw();
            }
        }
    }
    // Метод для глобальной отрисовки из любого меню
    public static void GlobalDraw()
    {
        if (_isVisible && _enabled)
        {
            Draw();
        }
        else if (_needsRedraw)
        {
            // Если консоль скрыта, но нужна перерисовка, сбрасываем флаг
            // и запрашиваем перерисовку основного экрана
            _needsRedraw = false;
            ScreenManager.RequestFullRedraw(); // ← Добавить
        }
    }
    // Поместите этот метод внутри DebugConsole (например прямо после ExecuteCommand)
    // --- вставьте этот метод в класс DebugConsole ---
    private static string EnsureOutputDirectory(string desired)
    {
        try
        {
            // Если путь указывает на файл — считаем это ошибкой и используем fallback
            if (File.Exists(desired))
            {
                Log($"Путь вывода {desired} существует как файл — используем fallback-путь.");
                throw new UnauthorizedAccessException("Путь занят файлом");
            }

            // Если директория существует — проверим возможность записи
            if (Directory.Exists(desired))
            {
                try
                {
                    string test = Path.Combine(desired, ".write_test");
                    File.WriteAllText(test, "test");
                    File.Delete(test);
                    return desired;
                }
                catch
                {
                    Log($"Нет прав на запись в папке {desired} — использую fallback.");
                    throw new UnauthorizedAccessException("Нет прав записи");
                }
            }

            // Попытка создать директорию
            Directory.CreateDirectory(desired);
            return desired;
        }
        catch (UnauthorizedAccessException)
        {
            // fallback: папка в LocalAppData
            string appName = "SimpleDungeon";
            string fallback = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName, "game_data");
            try
            {
                Directory.CreateDirectory(fallback);
                Log($"Использую запасной путь для вывода: {fallback}");
                return fallback;
            }
            catch (Exception ex)
            {
                Log($"Не удалось создать fallback-папку {fallback}: {ex.Message}");
                throw; // пробрасываем — вызывающий обработает
            }
        }
        catch (Exception ex)
        {
            Log($"Ошибка при подготовке папки вывода '{desired}': {ex.Message}");
            throw;
        }
    }

    // --- и этот метод тоже вставьте в класс DebugConsole (заменит старый TryInvokeDataExporter) ---
    // Вставь оба метода в класс DebugConsole

    // ------------------------
    // ЗАМЕНИТЕ TryInvokeDataExporter этим кодом
    // ------------------------
    private static string TryInvokeDataExporter(string outFolder, out int exitCode)
    {
        exitCode = -1;
        string filePathToUse = null;
        try
        {
            Log("[export-json] Запрошен экспорт JSON"); // короткий стартовый лог
            var startTimeUtc = DateTime.UtcNow;

            // 1) Найти тип DataExporter (как раньше)
            Type exporterType = null;
            foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
            {
                Type[] types;
                try { types = asm.GetTypes(); }
                catch (ReflectionTypeLoadException ex) { types = ex.Types?.Where(t => t != null).Select(t => t!).ToArray() ?? Array.Empty<Type>(); }
                catch { types = Array.Empty<Type>(); }

                foreach (var t in types)
                {
                    if (t == null) continue;
                    if (string.Equals(t.Name, "DataExporter", StringComparison.OrdinalIgnoreCase))
                    {
                        exporterType = t;
                        break;
                    }
                }
                if (exporterType != null) break;
            }

            if (exporterType == null)
            {
                exitCode = 1;
                Log("[export-json] Экспортер не найден.");
                return "DataExporter не найден.";
            }

            Log($"[export-json] Экспортер: {exporterType.FullName}");

            // 2) Выбрать метод (как раньше)
            var methods = exporterType.GetMethods(BindingFlags.Public | BindingFlags.Static | BindingFlags.Instance)
                            .Where(m => m.GetParameters().Length <= 1)
                            .ToArray();

            MethodInfo chosen = null;
            bool NameContains(MethodInfo m, string s) => m.Name.IndexOf(s, StringComparison.OrdinalIgnoreCase) >= 0;

            chosen = methods.FirstOrDefault(m => string.Equals(m.Name, "ExportGameDataToJson", StringComparison.OrdinalIgnoreCase)
                                                && m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string));
            chosen ??= methods.FirstOrDefault(m => string.Equals(m.Name, "ExportGameDataToJson", StringComparison.OrdinalIgnoreCase) && m.GetParameters().Length == 0);
            chosen ??= methods.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string) && NameContains(m, "Export") && NameContains(m, "Json"));
            chosen ??= methods.FirstOrDefault(m => m.GetParameters().Length == 0 && NameContains(m, "Export") && NameContains(m, "Json"));
            chosen ??= methods.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string) && NameContains(m, "Export"));
            chosen ??= methods.FirstOrDefault(m => m.GetParameters().Length == 0 && NameContains(m, "Export"));
            chosen ??= methods.FirstOrDefault(m => m.GetParameters().Length == 1 && m.GetParameters()[0].ParameterType == typeof(string))
                     ?? methods.FirstOrDefault(m => m.GetParameters().Length == 0)
                     ?? methods.FirstOrDefault();

            if (chosen == null)
            {
                exitCode = 2;
                Log("[export-json] Не найден подходящий метод экспорта.");
                return "Не удалось подобрать метод для экспорта.";
            }

            Log($"[export-json] Выбран метод: {chosen.Name}");

            // 3) Создаём экземпляр при необходимости
            object? instance = null;
            if (!chosen.IsStatic)
            {
                try
                {
                    instance = Activator.CreateInstance(exporterType);
                }
                catch (Exception ex)
                {
                    exitCode = 2;
                    Log($"[export-json] Ошибка создания экземпляра экспортера: {ex.Message}");
                    return $"Не удалось создать экземпляр экспортера: {ex.Message}";
                }
            }

            // 4) Подготовка аргумента (string) — корректно трактуем file vs folder
            var pars = chosen.GetParameters();
            object[] invokeArgs = Array.Empty<object>();
            bool methodTakesString = pars.Length == 1 && pars[0].ParameterType == typeof(string);

            if (methodTakesString)
            {
                if (string.IsNullOrWhiteSpace(outFolder))
                    outFolder = Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "game_data");

                try
                {
                    // если строка имеет расширение => файл
                    if (Path.HasExtension(outFolder))
                    {
                        var dir = Path.GetDirectoryName(outFolder);
                        if (!string.IsNullOrWhiteSpace(dir)) Directory.CreateDirectory(dir);
                        filePathToUse = Path.GetFullPath(outFolder);
                    }
                    else if (Directory.Exists(outFolder))
                    {
                        filePathToUse = Path.Combine(Path.GetFullPath(outFolder), "game_data.json");
                    }
                    else
                    {
                        // создаём папку и используем game_data.json
                        Directory.CreateDirectory(outFolder);
                        filePathToUse = Path.Combine(Path.GetFullPath(outFolder), "game_data.json");
                    }
                }
                catch
                {
                    // быстрый fallback
                    string fallbackDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "SimpleDungeon", "game_data");
                    Directory.CreateDirectory(fallbackDir);
                    filePathToUse = Path.Combine(fallbackDir, "game_data.json");
                }

                invokeArgs = new object[] { filePathToUse };
            }

            // 5) Вызов экспортера (в background, с таймаутом)
            var invokeTimeoutMs = 15_000; // короче — лог больше не нужен
            Exception? invokeException = null;
            object? ret = null;

            try
            {
                var invokeTask = Task<object?>.Run(() =>
                {
                    try { return chosen.Invoke(instance, invokeArgs); }
                    catch (TargetInvocationException tie) { throw tie.InnerException ?? tie; }
                });

                if (!invokeTask.Wait(invokeTimeoutMs))
                {
                    // таймаут — но не фатально; всё равно сделаем поиск файлов
                }
                else
                {
                    ret = invokeTask.Result;
                }
            }
            catch (Exception ex)
            {
                invokeException = ex;
            }

            // 6) Короткий поиск новых файлов (быстрый polling)
            var baseDir = AppDomain.CurrentDomain.BaseDirectory ?? Directory.GetCurrentDirectory();
            var candidateDirs = new List<string>
        {
            Path.Combine(baseDir, "Data"),
            Path.Combine(baseDir, "data"),
            Path.Combine(baseDir, "Export"),
            Path.Combine(baseDir, "exports"),
            baseDir
        }.Distinct().ToList();

            var discovered = new List<string>();
            var pollingTimeoutSec = 6; // короткий
            var endPoll = DateTime.UtcNow.AddSeconds(pollingTimeoutSec);
            while (DateTime.UtcNow <= endPoll)
            {
                try
                {
                    foreach (var cd in candidateDirs)
                    {
                        if (!Directory.Exists(cd)) continue;
                        var files = Directory.GetFiles(cd, "*.json", SearchOption.TopDirectoryOnly)
                                             .Where(f => File.GetLastWriteTimeUtc(f) >= startTimeUtc);
                        foreach (var f in files)
                            if (!discovered.Contains(f)) discovered.Add(f);
                    }

                    if (discovered.Count > 0) break;

                    // рекурсивный быстрый проход (одноразово)
                    if (discovered.Count == 0)
                    {
                        try
                        {
                            var rec = Directory.GetFiles(baseDir, "*.json", SearchOption.AllDirectories)
                                               .Where(f => File.GetLastWriteTimeUtc(f) >= startTimeUtc);
                            foreach (var f in rec)
                                if (!discovered.Contains(f)) discovered.Add(f);
                        }
                        catch { }
                    }
                }
                catch { /* молчим */ }

                Thread.Sleep(500);
            }

            // 7) Копирование результатов (без детального логирования)
            try
            {
                string targetFolderForCopy = !string.IsNullOrWhiteSpace(outFolder) ? outFolder : Path.Combine(AppDomain.CurrentDomain.BaseDirectory ?? ".", "game_data");
                Directory.CreateDirectory(targetFolderForCopy);

                var filesToCopy = discovered.Distinct().ToList();

                // если ничего найдено через polling — попробуем использовать filePathToUse
                if (filesToCopy.Count == 0 && !string.IsNullOrWhiteSpace(filePathToUse) && File.Exists(filePathToUse))
                    filesToCopy.Add(filePathToUse);

                int copied = 0;
                foreach (var f in filesToCopy)
                {
                    try
                    {
                        var dest = Path.Combine(targetFolderForCopy, Path.GetFileName(f));
                        if (string.Equals(Path.GetFullPath(f), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                        {
                            // файл уже в целевой папке — ничего не делает
                            copied++;
                            continue;
                        }

                        if (CopyFileRobust(f, dest, maxAttempts: 6, waitMs: 300, out string copyErr))
                            copied++;
                    }
                    catch { /* тишина — минимальный вывод */ }
                }

                // Итоговый короткий лог
                if (copied > 0)
                {
                    Log($"[export-json] Экспорт завершён. Файлов скопировано: {copied}. Папка: {targetFolderForCopy}");
                }
                else
                {
                    // Возможно экспортёр записал прямо туда, куда мы просили
                    if (!string.IsNullOrWhiteSpace(filePathToUse) && File.Exists(filePathToUse))
                    {
                        Log($"[export-json] Экспорт завершён. Файл: {filePathToUse}");
                    }
                    else if (invokeException != null)
                    {
                        Log($"[export-json] Экспорт завершился с ошибкой: {invokeException.GetType().Name}: {invokeException.Message}");
                        exitCode = 5;
                        return $"Invoke error: {invokeException.Message}";
                    }
                    else
                    {
                        Log("[export-json] Экспорт завершён — файлов не найдено и не скопировано.");
                    }
                }
            }
            catch (Exception exCopy)
            {
                Log($"[export-json] Ошибка при копировании результатов: {exCopy.GetType().Name}: {exCopy.Message}");
            }

            exitCode = invokeException == null ? 0 : 4;
            return $"Вызван {chosen.Name}().";
        }
        catch (Exception ex)
        {
            exitCode = 99;
            Log($"[export-json] Непредвиденная ошибка: {ex.GetType().Name}: {ex.Message}");
            return $"Error: {ex.Message}";
        }
    }

    // ------------------------
    // Добавьте CopyFileRobust рядом с ним
    // (минимальные внутренние логи, возвращает ошибку через out string)
    // ------------------------
    private static bool CopyFileRobust(string src, string dest, int maxAttempts, int waitMs, out string error)
    {
        error = null;
        try
        {
            var destDir = Path.GetDirectoryName(dest) ?? ".";
            Directory.CreateDirectory(destDir);

            if (string.Equals(Path.GetFullPath(src), Path.GetFullPath(dest), StringComparison.OrdinalIgnoreCase))
                return true;

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                try
                {
                    File.Copy(src, dest, overwrite: true);
                    return true;
                }
                catch (IOException)
                {
                    // пробуем прочитать источник с FileShare и записать временно
                    try
                    {
                        using (var fsSrc = new FileStream(src, FileMode.Open, FileAccess.Read, FileShare.ReadWrite))
                        {
                            string tmp = Path.Combine(destDir, $".tmp_copy_{Guid.NewGuid():N}.json");
                            using (var fsTmp = new FileStream(tmp, FileMode.Create, FileAccess.Write, FileShare.None))
                            {
                                fsSrc.CopyTo(fsTmp);
                            }

                            if (File.Exists(dest))
                            {
                                try { File.Replace(tmp, dest, null); }
                                catch { if (File.Exists(dest)) File.Delete(dest); File.Move(tmp, dest); }
                            }
                            else
                            {
                                File.Move(tmp, dest);
                            }

                            return true;
                        }
                    }
                    catch
                    {
                        // ждём и повторяем
                    }
                }
                catch (UnauthorizedAccessException uaEx)
                {
                    // попробуем снять ReadOnly у dest, если есть
                    try
                    {
                        if (File.Exists(dest))
                        {
                            var attrs = File.GetAttributes(dest);
                            if ((attrs & FileAttributes.ReadOnly) == FileAttributes.ReadOnly)
                                File.SetAttributes(dest, attrs & ~FileAttributes.ReadOnly);
                        }
                    }
                    catch { }
                }

                Thread.Sleep(waitMs);
            }

            error = $"Не удалось скопировать файл после {maxAttempts} попыток.";
            return false;
        }
        catch (Exception ex)
        {
            error = ex.Message;
            return false;
        }
    }



}