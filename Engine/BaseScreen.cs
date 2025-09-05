using System.Text;

namespace Engine
{
    public abstract class BaseScreen
    {
        protected readonly BufferedRenderer _renderer;

        protected BaseScreen()
        {
            _renderer = GameServices.BufferedRenderer;
        }

        public abstract void Render();
        public abstract void HandleInput(ConsoleKeyInfo keyInfo);

        protected void RenderHeader(string title, int yOffset = 0, ConsoleColor color = ConsoleColor.Yellow)
        {
            int y = yOffset;
            int width = Console.WindowWidth;

            // Очищаем область заголовка
            _renderer.FillArea(0, y, width, 3, ' ', ConsoleColor.White, ConsoleColor.Black);

            _renderer.Write(0, y, new string('═', width), ConsoleColor.Gray);
            RenderUtilities.RenderCenteredText(_renderer, y + 1, title, color);
            _renderer.Write(0, y + 2, new string('═', width), ConsoleColor.Gray);
        }

        protected void RenderFooter(string instructions, int yOffset = 0)
        {
            int y = Console.WindowHeight - 3 + yOffset;
            int width = Console.WindowWidth;

            _renderer.Write(0, y, new string('═', width), ConsoleColor.Gray);
            _renderer.Write(2, y + 1, instructions, ConsoleColor.DarkGray);
        }

        protected void ClearScreen()
        {
            _renderer.FillArea(0, 0, Console.WindowWidth, Console.WindowHeight, ' ',
                              ConsoleColor.White, ConsoleColor.Black);
        }

        protected List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text))
                return lines;

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

        protected void RenderWrappedText(int x, int y, string text, int maxWidth, ConsoleColor color = ConsoleColor.White)
        {
            var lines = WrapText(text, maxWidth);
            foreach (var line in lines)
            {
                _renderer.Write(x, y, line, color);
                y++;
            }
        }

        protected void RenderDialogBox(string title, string message, List<string> options, int selectedIndex = 0)
        {
            int boxWidth = Math.Min(60, Console.WindowWidth - 4);
            int boxHeight = 8 + options.Count;
            int boxX = (Console.WindowWidth - boxWidth) / 2;
            int boxY = (Console.WindowHeight - boxHeight) / 2;

            // Фон
            _renderer.FillArea(boxX, boxY, boxWidth, boxHeight, ' ', ConsoleColor.White, ConsoleColor.DarkBlue);

            // Рамка
            RenderUtilities.RenderBox(_renderer, boxX, boxY, boxWidth, boxHeight, title, ConsoleColor.White);

            // Сообщение
            RenderWrappedText(boxX + 2, boxY + 2, message, boxWidth - 4, ConsoleColor.White);

            // Опции
            int optionsY = boxY + boxHeight - options.Count - 2;
            for (int i = 0; i < options.Count; i++)
            {
                bool isSelected = i == selectedIndex;
                ConsoleColor color = isSelected ? ConsoleColor.Green : ConsoleColor.White;
                string prefix = isSelected ? "> " : "  ";

                _renderer.Write(boxX + 2, optionsY + i, prefix + options[i], color);
            }
        }
    }
}