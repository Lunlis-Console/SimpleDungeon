using Engine.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace Engine.Rendering
{
    public static class RenderUtils
    {
        public static void RenderDialogBox(EnhancedBufferedRenderer renderer,
            string title, string message, List<string> options, int selectedIndex = 0)
        {
            int boxWidth = Math.Min(60, Console.WindowWidth - 4);
            int boxHeight = 8 + options.Count;
            int boxX = (Console.WindowWidth - boxWidth) / 2;
            int boxY = (Console.WindowHeight - boxHeight) / 2;

            // Фон
            renderer.FillArea(boxX, boxY, boxWidth, boxHeight, ' ',
                ConsoleColor.White, ConsoleColor.DarkBlue);

            // Рамка
            RenderBox(renderer, boxX, boxY, boxWidth, boxHeight, title);

            // Сообщение
            RenderWrappedText(renderer, boxX + 2, boxY + 2, message,
                boxWidth - 4, ConsoleColor.White);

            // Опции
            int optionsY = boxY + boxHeight - options.Count - 2;
            for (int i = 0; i < options.Count; i++)
            {
                bool isSelected = i == selectedIndex;
                ConsoleColor color = isSelected ? ConsoleColor.Green : ConsoleColor.White;
                string prefix = isSelected ? "► " : "  ";

                renderer.Write(boxX + 2, optionsY + i, prefix + options[i], color);
            }
        }

        public static void RenderBox(EnhancedBufferedRenderer renderer,
            int x, int y, int width, int height, string title = "")
        {
            // Верхняя граница
            renderer.Write(x, y, "╔" + new string('═', width - 2) + "╗");

            // Заголовок
            if (!string.IsNullOrEmpty(title))
            {
                int titleX = x + (width - title.Length) / 2;
                renderer.Write(titleX, y, title, ConsoleColor.Yellow);
            }

            // Боковые границы
            for (int i = 1; i < height - 1; i++)
            {
                renderer.Write(x, y + i, "║");
                renderer.Write(x + width - 1, y + i, "║");
            }

            // Нижняя граница
            renderer.Write(x, y + height - 1, "╚" + new string('═', width - 2) + "╝");
        }

        public static void RenderWrappedText(EnhancedBufferedRenderer renderer,
            int x, int y, string text, int maxWidth, ConsoleColor color = ConsoleColor.White)
        {
            var lines = WrapText(text, maxWidth);
            foreach (var line in lines)
            {
                renderer.Write(x, y, line, color);
                y++;
            }
        }

        // Исправление 1: Добавляем static
        // Исправление 2: Меняем protected на private (или public)
        private static List<string> WrapText(string text, int maxWidth)
        {
            var lines = new List<string>();

            if (string.IsNullOrEmpty(text) || maxWidth <= 0)
                return lines;

            int startIndex = 0;
            while (startIndex < text.Length)
            {
                int length = Math.Min(maxWidth, text.Length - startIndex);
                string line = text.Substring(startIndex, length);
                lines.Add(line);
                startIndex += length;
            }

            return lines;
        }
    }
}