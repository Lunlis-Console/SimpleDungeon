// DoubleBufferRenderer.cs
using System;
using System.Text;

namespace Engine
{
    public class DoubleBufferRenderer
    {
        private readonly IOutputService _output;
        private char[,] _backBuffer;
        private char[,] _frontBuffer;
        private int _width;
        private int _height;
        private bool _needsFullRedraw;

        public DoubleBufferRenderer(IOutputService output)
        {
            _output = output;
            InitializeBuffers();
        }

        private void InitializeBuffers()
        {
            _width = Console.WindowWidth;
            _height = Console.WindowHeight;
            _backBuffer = new char[_width, _height];
            _frontBuffer = new char[_width, _height];
            _needsFullRedraw = true;

            ClearBuffer(_backBuffer);
            ClearBuffer(_frontBuffer);
        }

        private void ClearBuffer(char[,] buffer)
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    buffer[x, y] = ' ';
                }
            }
        }

        public void BeginFrame()
        {
            ClearBuffer(_backBuffer);
        }

        public void Write(int x, int y, string text)
        {
            if (string.IsNullOrEmpty(text)) return;

            for (int i = 0; i < text.Length && x + i < _width; i++)
            {
                if (y < _height && y >= 0 && x + i >= 0)
                {
                    _backBuffer[x + i, y] = text[i];
                }
            }
        }

        public void WriteLine(int x, int y, string text)
        {
            Write(x, y, text);
        }

        public void EndFrame()
        {
            Render();
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

            // Копируем backBuffer в frontBuffer для следующего сравнения
            Array.Copy(_backBuffer, _frontBuffer, _backBuffer.Length);
        }

        private void RenderFull()
        {
            _output.Clear();
            StringBuilder currentLine = new StringBuilder(_width);

            for (int y = 0; y < _height; y++)
            {
                currentLine.Clear();
                for (int x = 0; x < _width; x++)
                {
                    currentLine.Append(_backBuffer[x, y]);
                }
                _output.WriteLine(currentLine.ToString());
            }
        }

        private void RenderPartial()
        {
            for (int y = 0; y < _height; y++)
            {
                for (int x = 0; x < _width; x++)
                {
                    if (_backBuffer[x, y] != _frontBuffer[x, y])
                    {
                        _output.SetCursorPosition(x, y);
                        _output.Write(_backBuffer[x, y].ToString());
                    }
                }
            }
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
    }
}