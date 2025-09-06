namespace Engine
{
    public static class SafeRender
    {
        public static void RenderBorder(EnhancedBufferedRenderer renderer,
            int x, int y, int width, int height,
            ConsoleColor color = ConsoleColor.Gray)
        {
            // Обеспечиваем безопасные координаты
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            width = Math.Min(width, renderer.Width - x);
            height = Math.Min(height, renderer.Height - y);

            if (width <= 0 || height <= 0) return;

            // Верхняя и нижняя границы
            renderer.Write(x, y, "╔" + new string('═', width - 2) + "╗", color);
            renderer.Write(x, y + height - 1, "╚" + new string('═', width - 2) + "╝", color);

            // Боковые границы
            for (int i = 1; i < height - 1; i++)
            {
                renderer.Write(x, y + i, "║", color);
                renderer.Write(x + width - 1, y + i, "║", color);
            }
        }

        public static void RenderProgressBar(EnhancedBufferedRenderer renderer,
            int x, int y, int width, float percentage,
            string label = "", ConsoleColor color = ConsoleColor.Green)
        {
            x = Math.Max(0, x);
            y = Math.Max(0, y);
            width = Math.Min(width, renderer.Width - x);

            int filledWidth = (int)(width * Math.Max(0, Math.Min(percentage, 1f)));
            string bar = new string('█', filledWidth) + new string('░', width - filledWidth);

            if (!string.IsNullOrEmpty(label))
            {
                renderer.Write(x, y, $"{label}: [{bar}]", color);
            }
            else
            {
                renderer.Write(x, y, $"[{bar}]", color);
            }
        }
    }
}