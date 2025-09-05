namespace Engine
{
    public static class RenderUtilities
    {
        public static void RenderHealthBar(BufferedRenderer renderer, int x, int y,
            int current, int max, string label = "Здоровье", int width = 20)
        {
            current = Math.Max(current, 0);
            max = Math.Max(max, 1);

            float percentage = (float)current / max;
            int bars = (int)(width * percentage);
            bars = Math.Clamp(bars, 0, width);

            string healthText = $"{current}/{max}";
            string bar = $"{label}: [";

            ConsoleColor color = percentage > 0.5f ? ConsoleColor.Green :
                                percentage > 0.25f ? ConsoleColor.Yellow :
                                ConsoleColor.Red;

            bar += new string('█', bars);
            bar += new string('░', width - bars);
            bar += $"] {healthText}";

            renderer.Write(x, y, bar, color);
        }

        public static void RenderProgressBar(BufferedRenderer renderer, int x, int y,
            int current, int max, string label, ConsoleColor color = ConsoleColor.Cyan, int width = 20)
        {
            float percentage = (float)current / max;
            int bars = (int)(width * percentage);
            bars = Math.Clamp(bars, 0, width);

            string bar = $"{label}: [";
            bar += new string('█', bars);
            bar += new string('░', width - bars);
            bar += $"] {current}%";

            renderer.Write(x, y, bar, color);
        }

        public static void RenderCenteredText(BufferedRenderer renderer, int y, string text, ConsoleColor color)
        {
            int x = (Console.WindowWidth - text.Length) / 2;
            renderer.Write(Math.Max(0, x), y, text, color);
        }

        public static void RenderBox(BufferedRenderer renderer, int x, int y, int width, int height,
                                   string title = "", ConsoleColor borderColor = ConsoleColor.Gray)
        {
            // Верхняя граница
            renderer.Write(x, y, "╔" + new string('═', width - 2) + "╗", borderColor);

            // Заголовок
            if (!string.IsNullOrEmpty(title))
            {
                int titleX = x + (width - title.Length) / 2;
                renderer.Write(titleX, y, title, ConsoleColor.Yellow);
            }

            // Боковые границы и заполнение
            for (int i = 1; i < height - 1; i++)
            {
                renderer.Write(x, y + i, "║", borderColor);
                renderer.FillArea(x + 1, y + i, width - 2, 1, ' ', ConsoleColor.White, ConsoleColor.Black);
                renderer.Write(x + width - 1, y + i, "║", borderColor);
            }

            // Нижняя граница
            renderer.Write(x, y + height - 1, "╚" + new string('═', width - 2) + "╝", borderColor);
        }

        public static void RenderSelectionList<T>(BufferedRenderer renderer, int x, int y,
            List<T> items, int selectedIndex, Func<T, string> displaySelector,
            string title = "", ConsoleColor selectedColor = ConsoleColor.Green)
        {
            if (!string.IsNullOrEmpty(title))
            {
                renderer.Write(x, y, title, ConsoleColor.Yellow);
                y += 2;
            }

            for (int i = 0; i < items.Count; i++)
            {
                bool isSelected = i == selectedIndex;
                string prefix = isSelected ? "> " : "  ";
                ConsoleColor color = isSelected ? selectedColor : ConsoleColor.White;

                string displayText = displaySelector(items[i]);
                renderer.Write(x, y + i, prefix + displayText, color);
            }
        }

        public static void RenderStatsBlock(BufferedRenderer renderer, int x, int y,
            int attack, int defence, int agility, int evasion)
        {
            renderer.Write(x, y, $"АТК: {attack} │ ЗЩТ: {defence} │ ЛОВ: {agility} │ УКЛ: {evasion}%",
                ConsoleColor.White);
        }
    }
}