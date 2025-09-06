using System.Text;

namespace Engine
{
    public class EnhancedBufferedRenderer : IDisposable
    {
        private readonly object _renderLock = new object();
        private CharInfo[,] _backBuffer;
        private CharInfo[,] _frontBuffer;
        private int _width;
        private int _height;
        private bool _disposed;
        private bool _needsFullRedraw = true;
        private bool _windowResized = false;
        private int _framesSinceFullRedraw = 0;

        public int Width => _width;
        public int Height => _height;

        public EnhancedBufferedRenderer()
        {
            InitializeBuffers();
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.Clear();
        }

        private struct CharInfo
        {
            public char Character;
            public ConsoleColor Foreground;
            public ConsoleColor Background;

            public CharInfo(char character, ConsoleColor foreground, ConsoleColor background)
            {
                Character = character;
                Foreground = foreground;
                Background = background;
            }

            public bool Equals(CharInfo other)
            {
                return Character == other.Character &&
                       Foreground == other.Foreground &&
                       Background == other.Background;
            }
        }

        private void InitializeBuffers()
        {
            lock (_renderLock)
            {
                // Добавляем проверку на минимальный размер
                int minWidth = 80;
                int minHeight = 24;

                if (Console.WindowWidth < minWidth) Console.WindowWidth = minWidth;
                if (Console.WindowHeight < minHeight) Console.WindowHeight = minHeight;

                _width = Console.WindowWidth;
                _height = Console.WindowHeight;

                _backBuffer = new CharInfo[_width, _height];
                _frontBuffer = new CharInfo[_width, _height];

                ClearBuffer(_backBuffer);
                ClearBuffer(_frontBuffer);
                _needsFullRedraw = true;
                _windowResized = true;
            }
        }

        private void ClearBuffer(CharInfo[,] buffer)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    buffer[x, y] = new CharInfo(' ', ConsoleColor.Gray, ConsoleColor.Black);
                }
            }
        }

        public void BeginFrame()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(EnhancedBufferedRenderer));

            lock (_renderLock)
            {
                // Проверяем изменение размера окна
                if (Console.WindowWidth != _width || Console.WindowHeight != _height)
                {
                    DebugConsole.Log($"Window resized: {Console.WindowWidth}x{Console.WindowHeight}");
                    InitializeBuffers();
                    _needsFullRedraw = true;
                }

                ClearBuffer(_backBuffer);
            }
        }

        public void EndFrame()
        {
            if (_disposed) return;

            lock (_renderLock)
            {
                Render();
                // Копируем back buffer в front buffer
                Array.Copy(_backBuffer, _frontBuffer, _backBuffer.Length);
            }
        }

        public void Write(int x, int y, string text, ConsoleColor foreground = ConsoleColor.White,
                         ConsoleColor background = ConsoleColor.Black)
        {
            if (_disposed || string.IsNullOrEmpty(text)) return;

            lock (_renderLock)
            {
                x = Math.Max(0, Math.Min(x, _width - 1));
                y = Math.Max(0, Math.Min(y, _height - 1));

                for (int i = 0; i < text.Length && x + i < _width; i++)
                {
                    if (y < _height && y >= 0 && x + i >= 0)
                    {
                        _backBuffer[x + i, y] = new CharInfo(text[i], foreground, background);
                    }
                }
            }
        }

        public void FillArea(int x, int y, int width, int height, char fillChar = ' ',
                            ConsoleColor foreground = ConsoleColor.White,
                            ConsoleColor background = ConsoleColor.Black)
        {
            if (_disposed) return;

            lock (_renderLock)
            {
                x = Math.Max(0, x);
                y = Math.Max(0, y);
                width = Math.Min(width, _width - x);
                height = Math.Min(height, _height - y);

                for (int dy = 0; dy < height; dy++)
                {
                    for (int dx = 0; dx < width; dx++)
                    {
                        int currentX = x + dx;
                        int currentY = y + dy;

                        if (currentX >= 0 && currentX < _width &&
                            currentY >= 0 && currentY < _height)
                        {
                            _backBuffer[currentX, currentY] = new CharInfo(fillChar, foreground, background);
                        }
                    }
                }
            }
        }

        private void Render()
        {
            try
            {
                if (_needsFullRedraw || _windowResized)
                {
                    RenderFull();
                    _needsFullRedraw = false;
                    _windowResized = false;
                }
                else
                {
                    RenderPartial();
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Render error: {ex.Message}");
                _needsFullRedraw = true;
            }
        }

        private void RenderFull()
        {
            Console.CursorVisible = false;

            // Используем безопасный метод очистки
            try
            {
                Console.Clear();
            }
            catch
            {
                // Альтернативный метод очистки если Console.Clear() fails
                Console.SetCursorPosition(0, 0);
                Console.Write(new string(' ', Console.WindowWidth * Console.WindowHeight));
                Console.SetCursorPosition(0, 0);
            }

            for (int y = 0; y < _height; y++)
            {
                Console.SetCursorPosition(0, y);

                ConsoleColor currentForeground = _frontBuffer[0, y].Foreground;
                ConsoleColor currentBackground = _frontBuffer[0, y].Background;

                Console.ForegroundColor = currentForeground;
                Console.BackgroundColor = currentBackground;

                StringBuilder line = new StringBuilder();

                for (int x = 0; x < _width; x++)
                {
                    var charInfo = _frontBuffer[x, y];

                    if (charInfo.Foreground != currentForeground ||
                        charInfo.Background != currentBackground)
                    {
                        if (line.Length > 0)
                        {
                            Console.Write(line.ToString());
                            line.Clear();
                        }

                        currentForeground = charInfo.Foreground;
                        currentBackground = charInfo.Background;

                        Console.ForegroundColor = currentForeground;
                        Console.BackgroundColor = currentBackground;
                    }

                    line.Append(charInfo.Character);
                }

                if (line.Length > 0)
                {
                    Console.Write(line.ToString());
                }
            }

            Console.ResetColor();
        }

        private void RenderPartial()
        {
            Console.CursorVisible = false;

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (!_backBuffer[x, y].Equals(_frontBuffer[x, y]))
                    {
                        try
                        {
                            Console.SetCursorPosition(x, y);
                            Console.ForegroundColor = _backBuffer[x, y].Foreground;
                            Console.BackgroundColor = _backBuffer[x, y].Background;
                            Console.Write(_backBuffer[x, y].Character);
                        }
                        catch (ArgumentOutOfRangeException)
                        {
                            // Игнорируем ошибки позиционирования
                        }
                    }
                }
            }

            Console.ResetColor();
        }

        public bool CheckWindowResize()
        {
            if (Console.WindowWidth != _width || Console.WindowHeight != _height)
            {
                DebugConsole.Log($"Window resize detected: {Console.WindowWidth}x{Console.WindowHeight}");
                InitializeBuffers();
                _windowResized = true;
                return true;
            }
            return false;
        }

        public void SetNeedsFullRedraw()
        {
            _needsFullRedraw = true;
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.CancelKeyPress -= OnCancelKeyPress;
                Console.ResetColor();
                Console.Clear();
                Console.CursorVisible = true;
            }
        }
    }
}