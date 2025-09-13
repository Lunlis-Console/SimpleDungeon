// EnhancedBufferedRenderer.cs
using System;
using System.Text;
using System.Threading;

namespace Engine.Core
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
        private bool _firstRender = true;

        public bool InFrame { get; private set; }

        public int Width => _width;
        public int Height => _height;

        public EnhancedBufferedRenderer()
        {
            // Поддержка UTF-8 в консоли
            try { Console.OutputEncoding = Encoding.UTF8; } catch { /* игнорируем, если не доступно */ }

            ResizeBuffers();
            Console.CancelKeyPress += OnCancelKeyPress;
            Console.Clear();
            _needsFullRedraw = true;
        }

        private struct CharInfo : IEquatable<CharInfo>
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

            public override bool Equals(object obj) => obj is CharInfo ci && Equals(ci);

            public override int GetHashCode() => HashCode.Combine(Character, Foreground, Background);

            public static CharInfo Empty => new CharInfo(' ', ConsoleColor.Gray, ConsoleColor.Black);
        }

        private void ResizeBuffers()
        {
            lock (_renderLock)
            {
                // Минимальные размеры, чтобы интерфейс не ломался
                int minWidth = 80;
                int minHeight = 24;

                int targetWidth = Math.Max(minWidth, Console.WindowWidth > 0 ? Console.WindowWidth : minWidth);
                int targetHeight = Math.Max(minHeight, Console.WindowHeight > 0 ? Console.WindowHeight : minHeight);

                // Если размеры не изменились — ничего не делаем
                if (_backBuffer != null && _frontBuffer != null && _width == targetWidth && _height == targetHeight)
                    return;

                _width = targetWidth;
                _height = targetHeight;

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
                    buffer[x, y] = CharInfo.Empty;
                }
            }
        }

        public void BeginFrame()
        {
            try
            {
                if (_disposed) throw new ObjectDisposedException(nameof(EnhancedBufferedRenderer));

                lock (_renderLock)
                {
                    // Проверяем изменение размера окна
                    if (Console.WindowWidth != _width || Console.WindowHeight != _height)
                    {
                        DebugConsole.Log($"[renderer] Window resized: {Console.WindowWidth}x{Console.WindowHeight}");
                        ResizeBuffers();
                        _needsFullRedraw = true;
                    }

                    // Очищаем backBuffer — это важно: если не очищать, останутся "хвосты"
                    ClearBuffer(_backBuffer);
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"BeginFrame error {ex.Message}");
                throw;
            }

            InFrame = true;
        }

        public void EndFrame()
        {
            if (_disposed) return;

            lock (_renderLock)
            {
                Render();
                // NOTE: копирование back->front теперь происходит внутри RenderFull()/RenderPartial() после успешного вывода
            }

            InFrame = false;
        }

        /// <summary>
        /// Пишет строку в back buffer (не обрезает — просто пишет символы).
        /// При необходимости вызывающий код должен затирать остаток строки (или BeginFrame это сделает).
        /// </summary>
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

                // Оставшиеся позиции будут очищены в BeginFrame следующего кадра,
                // либо вызывающий код может вручную заполнить пробелами, если нужно.
            }
        }

        /// <summary>
        /// Заполнить прямоугольную область в back buffer
        /// </summary>
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

                _framesSinceFullRedraw++;
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Render error: {ex.Message}");
                _needsFullRedraw = true;
            }
        }

        /// <summary>
        /// Полная отрисовка: гарантированно перезаписывает всю видимую область,
        /// рисует содержимое backBuffer и только после успешного вывода копирует back->front.
        /// </summary>
        private void RenderFull()
        {
            try
            {
                Console.CursorVisible = false;

                // Ставим курсор в левый верхний угол
                try { Console.SetCursorPosition(0, 0); } catch { /* ignore */ }

                for (int y = 0; y < _height; y++)
                {
                    int x = 0;
                    while (x < _width)
                    {
                        // Берём начальный CharInfo и наращиваем сегмент с одинаковыми цветами
                        var startInfo = _backBuffer[x, y];
                        var fg = startInfo.Foreground;
                        var bg = startInfo.Background;
                        var sb = new StringBuilder();

                        int segStart = x;
                        do
                        {
                            var ci = _backBuffer[x, y];
                            char ch = ci.Character == '\0' ? ' ' : ci.Character;
                            sb.Append(ch);
                            x++;
                            if (x >= _width) break;
                        } while (_backBuffer[x, y].Foreground == fg && _backBuffer[x, y].Background == bg);

                        // Вывод сегмента
                        try
                        {
                            Console.ForegroundColor = fg;
                            Console.BackgroundColor = bg;
                            Console.Write(sb.ToString());
                        }
                        catch (ArgumentOutOfRangeException) { /* окно могло измениться — игнорируем */ }
                        catch (Exception) { /* молчим */ }
                    }

                    // Перейти к началу следующей строки, если нужно
                    if (y < _height - 1)
                    {
                        try { Console.SetCursorPosition(0, y + 1); } catch { /* ignore */ }
                    }
                }

                Console.ResetColor();

                // Копируем back->front после успешной отрисовки
                try
                {
                    Array.Copy(_backBuffer, _frontBuffer, _backBuffer.Length);
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"RenderFull copy error: {ex.Message}");
                }

                _needsFullRedraw = false;
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"RenderFull error: {ex.Message}");
                _needsFullRedraw = true;
            }
        }

        /// <summary>
        /// Частичная отрисовка: перебираем отличающиеся ячейки/сегменты и выводим только их.
        /// После вывода обновляем front buffer для тех позиций.
        /// </summary>
        private void RenderPartial()
        {
            Console.CursorVisible = false;

            for (int y = 0; y < _height; y++)
            {
                int x = 0;
                while (x < _width)
                {
                    var back = _backBuffer[x, y];
                    var front = _frontBuffer[x, y];

                    if (back.Equals(front))
                    {
                        x++;
                        continue;
                    }

                    // Начинаем сегмент от x
                    int segX = x;
                    var fg = back.Foreground;
                    var bg = back.Background;
                    var sb = new StringBuilder();

                    // Собираем сегмент пока ячейки отличаются и имеют одинаковые цвета
                    do
                    {
                        var ci = _backBuffer[x, y];
                        char ch = ci.Character == '\0' ? ' ' : ci.Character;
                        sb.Append(ch);
                        x++;
                        if (x >= _width) break;

                        // Останавливаемся если следующая позиция совпадает с front или имеет другие цвета
                        if (_backBuffer[x, y].Equals(_frontBuffer[x, y])) break;
                        if (_backBuffer[x, y].Foreground != fg || _backBuffer[x, y].Background != bg) break;

                    } while (true);

                    // Вывод сегмента и обновление frontBuffer для сегмента
                    try
                    {
                        Console.SetCursorPosition(segX, y);
                        Console.ForegroundColor = fg;
                        Console.BackgroundColor = bg;
                        Console.Write(sb.ToString());
                    }
                    catch (ArgumentOutOfRangeException) { /* окно могло измениться — игнорируем */ }
                    catch { /* ignore */ }

                    // Обновляем front buffer для позиций сегмента
                    for (int ix = 0; ix < sb.Length && segX + ix < _width; ix++)
                    {
                        _frontBuffer[segX + ix, y] = _backBuffer[segX + ix, y];
                    }
                }
            }

            Console.ResetColor();
        }

        public bool CheckWindowResize()
        {
            if (Console.WindowWidth != _width || Console.WindowHeight != _height)
            {
                DebugConsole.Log($"[renderer] Window resize detected: {Console.WindowWidth}x{Console.WindowHeight}");
                ResizeBuffers();
                _windowResized = true;
                return true;
            }
            return false;
        }

        public void SetNeedsFullRedraw()
        {
            _needsFullRedraw = true;
        }

        private void OnCancelKeyPress(object? sender, ConsoleCancelEventArgs e)
        {
            Dispose();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                _disposed = true;
                Console.CancelKeyPress -= OnCancelKeyPress;
                try
                {
                    Console.ResetColor();
                    Console.Clear();
                    Console.CursorVisible = true;
                }
                catch { /* ignore */ }
            }
        }

    }
}
