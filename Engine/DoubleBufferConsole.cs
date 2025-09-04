// DoubleBufferConsole.cs
using System;
using System.Text;
using System.Collections.Generic;

namespace Engine
{
    public class DoubleBufferConsole : IOutputService
    {
        private readonly char[,] _buffer;
        private readonly int _width;
        private readonly int _height;
        private int _cursorLeft;
        private int _cursorTop;
        private ConsoleColor _foregroundColor;
        private ConsoleColor _backgroundColor;
        private bool _isBuffering = false;
        private readonly List<string> _bufferCommands = new List<string>();

        public DoubleBufferConsole()
        {
            _width = Console.WindowWidth;
            _height = Console.WindowHeight;
            _buffer = new char[_height, _width];
            _foregroundColor = Console.ForegroundColor;
            _backgroundColor = Console.BackgroundColor;
            Clear();
        }

        public void Write(string message)
        {
            if (_isBuffering)
            {
                _bufferCommands.Add($"WRITE:{message}");
            }
            else
            {
                // Старая реализация
                foreach (char c in message)
                {
                    if (_cursorTop >= _height) return;

                    if (c == '\n')
                    {
                        _cursorTop++;
                        _cursorLeft = 0;
                    }
                    else
                    {
                        if (_cursorLeft >= _width)
                        {
                            _cursorLeft = 0;
                            _cursorTop++;
                        }

                        if (_cursorTop < _height && _cursorLeft < _width)
                        {
                            _buffer[_cursorTop, _cursorLeft] = c;
                            _cursorLeft++;
                        }
                    }
                }
            }
        }
        public void WriteLine(string message)
        {
            Write(message + "\n");
        }

        public void Clear()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    _buffer[y, x] = ' ';
                }
            }
            _cursorLeft = 0;
            _cursorTop = 0;
        }

        public void SetCursorPosition(int left, int top)
        {
            _cursorLeft = Math.Max(0, Math.Min(left, _width - 1));
            _cursorTop = Math.Max(0, Math.Min(top, _height - 1));
        }

        public void Render()
        {
            // Сохраняем текущие цвета
            ConsoleColor originalForeground = Console.ForegroundColor;
            ConsoleColor originalBackground = Console.BackgroundColor;

            Console.SetCursorPosition(0, 0);

            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    Console.Write(_buffer[y, x]);
                }
            }

            // Восстанавливаем курсор и цвета
            Console.SetCursorPosition(_cursorLeft, _cursorTop);
            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
        }

        public void RenderPartial(int left, int top, int width, int height)
        {
            ConsoleColor originalForeground = Console.ForegroundColor;
            ConsoleColor originalBackground = Console.BackgroundColor;

            for (int y = top; y < top + height && y < _height; y++)
            {
                Console.SetCursorPosition(left, y);
                for (int x = left; x < left + width && x < _width; x++)
                {
                    Console.Write(_buffer[y, x]);
                }
            }

            Console.SetCursorPosition(_cursorLeft, _cursorTop);
            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
        }

        public void BeginBuffer()
        {
            _isBuffering = true;
            _bufferCommands.Clear();
        }

        public void EndBuffer()
        {
            _isBuffering = false;
            // Применяем все накопленные команды
            foreach (var command in _bufferCommands)
            {
                // Реализация применения команд
            }
        }
    }
}