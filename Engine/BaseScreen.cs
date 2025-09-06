using System.Text;

namespace Engine
{
    public abstract class BaseScreen
    {
        protected readonly EnhancedBufferedRenderer _renderer;
        protected bool _needsRedraw = true;
        protected bool _needsFullRedraw = true; // Первая отрисовка всегда полная

        public virtual int Width => _renderer.Width;
        public virtual int Height => _renderer.Height;

        protected BaseScreen()
        {
            _renderer = GameServices.BufferedRenderer;

        }

        protected void RequestPartialRedraw()
        {
            _needsRedraw = true;
            _needsFullRedraw = false;
            ScreenManager.RequestPartialRedraw();
        }

        protected void RequestFullRedraw()
        {
            _needsRedraw = true;
            _needsFullRedraw = true;
            ScreenManager.RequestFullRedraw();
        }

        protected void ClearScreen()
        {
            _renderer.FillArea(0, 0, Width, Height, ' ', ConsoleColor.White, ConsoleColor.Black);
        }

        protected void ClearArea(int x, int y, int width, int height)
        {
            _renderer.FillArea(x, y, width, height, ' ', ConsoleColor.White, ConsoleColor.Black);
        }

        protected void RenderText(int x, int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            // Обеспечиваем безопасное позиционирование
            x = Math.Max(0, Math.Min(x, Width - 1));
            y = Math.Max(0, Math.Min(y, Height - 1));

            if (!string.IsNullOrEmpty(text) && y < Height)
            {
                _renderer.Write(x, y, text, color);
            }
        }

        protected void RenderCenteredText(int y, string text, ConsoleColor color = ConsoleColor.White)
        {
            if (string.IsNullOrEmpty(text)) return;

            int x = (Width - text.Length) / 2;
            RenderText(Math.Max(0, x), Math.Max(0, y), text, color);
        }

        protected void RenderButton(int x, int y, string text, bool isSelected = false)
        {
            var bgColor = isSelected ? ConsoleColor.DarkGreen : ConsoleColor.DarkGray;
            var fgColor = isSelected ? ConsoleColor.White : ConsoleColor.Gray;

            // Ограничиваем размер кнопки шириной экрана
            int buttonWidth = Math.Min(text.Length + 4, Width - x);
            int buttonHeight = Math.Min(3, Height - y);

            _renderer.FillArea(x, y, buttonWidth, buttonHeight, ' ', fgColor, bgColor);
            RenderText(x + 2, y + 1, text, fgColor);
        }

        public abstract void Render();

        public abstract void HandleInput(ConsoleKeyInfo keyInfo);

        protected List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text))
                return lines;

            // Учитываем границы экрана
            maxWidth = Math.Min(maxWidth, Width - 4);

            var words = text.Split(' ');
            var currentLine = new StringBuilder();

            foreach (var word in words)
            {
                if (currentLine.Length + word.Length + 1 > maxWidth)
                {
                    lines.Add(currentLine.ToString().Trim());
                    currentLine.Clear();
                }

                if (currentLine.Length > 0)
                    currentLine.Append(' ');

                currentLine.Append(word);
            }

            if (currentLine.Length > 0)
                lines.Add(currentLine.ToString().Trim());

            return lines;
        }

        protected void RenderFooter(string instructions, int yOffset = 0)
        {
            int y = Math.Max(0, Math.Min(Height - 3 + yOffset, Height - 1));
            int width = Math.Min(Width, Console.WindowWidth);

            // Очищаем область футера
            _renderer.FillArea(0, y, width, 3, ' ', ConsoleColor.White, ConsoleColor.Black);

            _renderer.Write(0, y - 1, new string('═', width), ConsoleColor.Gray);
            _renderer.Write(2, y, instructions, ConsoleColor.DarkGray);
        }

        protected void RenderHeader(string title, int yOffset = 0, ConsoleColor color = ConsoleColor.Yellow)
        {
            int y = Math.Max(0, yOffset);
            int width = Math.Min(Width, Console.WindowWidth);

            // Очищаем область заголовка
            _renderer.FillArea(0, y, width, 3, ' ', ConsoleColor.White, ConsoleColor.Black);

            _renderer.Write(0, y, new string('═', width), ConsoleColor.Gray);
            RenderCenteredText(y + 1, title, color);
            _renderer.Write(0, y + 2, new string('═', width), ConsoleColor.Gray);
        }

        public virtual void Update()
        {
            if (_needsRedraw)
            {
                Render();
                _needsRedraw = false;
                _needsFullRedraw = false;
            }
        }

        public void RequestRedraw()
        {
            _needsRedraw = true;
        }
    }
}