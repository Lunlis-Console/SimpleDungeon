using System.Text;

namespace Engine
{
    public class BufferedRenderer : IDisposable
    {
        private readonly IOutputService _output;
        private CharInfo[,] _backBuffer;
        private CharInfo[,] _frontBuffer;

        public int Width => _width;
        public int Height => _height;

        private int _width;
        private int _height;
        private bool _needsFullRedraw;
        private bool _disposed;

        public BufferedRenderer(IOutputService output)
        {
            _output = output;
            InitializeBuffers();
            Console.CancelKeyPress += OnCancelKeyPress;
        }

        private struct CharInfo
        {
            public char Character;
            public ConsoleColor ForegroundColor;
            public ConsoleColor BackgroundColor;

            public CharInfo(char character, ConsoleColor foreground = ConsoleColor.White,
                          ConsoleColor background = ConsoleColor.Black)
            {
                Character = character;
                ForegroundColor = foreground;
                BackgroundColor = background;
            }

            public bool Equals(CharInfo other)
            {
                return Character == other.Character &&
                       ForegroundColor == other.ForegroundColor &&
                       BackgroundColor == other.BackgroundColor;
            }
        }

        private void InitializeBuffers()
        {
            _width = Console.WindowWidth;
            _height = Console.WindowHeight;

            _backBuffer = new CharInfo[_width, _height];
            _frontBuffer = new CharInfo[_width, _height];

            _needsFullRedraw = true;
            ClearBuffer(_backBuffer);
            ClearBuffer(_frontBuffer);
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
            if (_disposed) throw new ObjectDisposedException(nameof(BufferedRenderer));

            // Проверяем изменение размера окна
            if (Console.WindowWidth != _width || Console.WindowHeight != _height)
            {
                InitializeBuffers();
                _needsFullRedraw = true;
            }

            ClearBuffer(_backBuffer);
        }

        public void Write(int x, int y, string text,
                         ConsoleColor foreground = ConsoleColor.White,
                         ConsoleColor background = ConsoleColor.Black)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BufferedRenderer));
            if (string.IsNullOrEmpty(text)) return;

            for (int i = 0; i < text.Length && x + i < _width; i++)
            {
                if (y < _height && y >= 0 && x + i >= 0)
                {
                    _backBuffer[x + i, y] = new CharInfo(text[i], foreground, background);
                }
            }
        }

        public void WriteLine(int x, int y, string text,
                             ConsoleColor foreground = ConsoleColor.White,
                             ConsoleColor background = ConsoleColor.Black)
        {
            Write(x, y, text, foreground, background);
        }

        public void FillArea(int x, int y, int width, int height,
                           char fillChar = ' ',
                           ConsoleColor foreground = ConsoleColor.White,
                           ConsoleColor background = ConsoleColor.Black)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BufferedRenderer));

            for (int dy = 0; dy < height; dy++)
            {
                for (int dx = 0; dx < width; dx++)
                {
                    int currentX = x + dx;
                    int currentY = y + dy;

                    if (currentX >= 0 && currentX < _width && currentY >= 0 && currentY < _height)
                    {
                        _backBuffer[currentX, currentY] = new CharInfo(fillChar, foreground, background);
                    }
                }
            }
        }

        public void EndFrame()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(BufferedRenderer));
            Render();
            Array.Copy(_backBuffer, _frontBuffer, _backBuffer.Length);
        }

        private void Render()
        {
            if (_needsFullRedraw)
            {
                RenderFull();
                _needsFullRedraw = false;
            }
            else
            {
                RenderPartial();
            }
        }

        private void RenderFull()
        {
            _output.Clear();

            for (int y = 0; y < _height; y++)
            {
                Console.SetCursorPosition(0, y);

                ConsoleColor currentForeground = _frontBuffer[0, y].ForegroundColor;
                ConsoleColor currentBackground = _frontBuffer[0, y].BackgroundColor;

                Console.ForegroundColor = currentForeground;
                Console.BackgroundColor = currentBackground;

                StringBuilder line = new StringBuilder();

                for (int x = 0; x < _width; x++)
                {
                    var charInfo = _frontBuffer[x, y];

                    if (charInfo.ForegroundColor != currentForeground ||
                        charInfo.BackgroundColor != currentBackground)
                    {
                        Console.Write(line.ToString());
                        line.Clear();

                        currentForeground = charInfo.ForegroundColor;
                        currentBackground = charInfo.BackgroundColor;

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
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (!_backBuffer[x, y].Equals(_frontBuffer[x, y]))
                    {
                        Console.SetCursorPosition(x, y);
                        Console.ForegroundColor = _backBuffer[x, y].ForegroundColor;
                        Console.BackgroundColor = _backBuffer[x, y].BackgroundColor;
                        Console.Write(_backBuffer[x, y].Character);
                    }
                }
            }

            Console.ResetColor();
        }

        public void SetNeedsFullRedraw()
        {
            _needsFullRedraw = true;
        }

        public void CheckWindowResize()
        {
            if (Console.WindowWidth != _width || Console.WindowHeight != _height)
            {
                InitializeBuffers();
                SetNeedsFullRedraw();
            }
        }

        private void OnCancelKeyPress(object sender, ConsoleCancelEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Console.CancelKeyPress -= OnCancelKeyPress;
                Console.ResetColor();
                Console.Clear();
                _disposed = true;
            }
        }
    }
}