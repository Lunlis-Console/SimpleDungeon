public static class DisplayUtilities
{
    public static void DrawHealthBar(int current, int max, int length, string label = "Здоровье")
    {
        // Защита от отрицательных значений
        current = Math.Max(current, 0);
        max = Math.Max(max, 1);

        float percentage = (float)current / max;
        int bars = (int)(length * percentage);
        bars = Math.Max(0, Math.Min(bars, length));
        int emptyBars = length - bars;

        string healthText = $"{current}/{max}";

        Console.Write($"{label}: [");

        if (percentage > 0.5f)
            Console.ForegroundColor = ConsoleColor.Green;
        else if (percentage > 0.25f)
            Console.ForegroundColor = ConsoleColor.Yellow;
        else
            Console.ForegroundColor = ConsoleColor.Red;

        Console.Write(new string('█', bars));
        Console.Write(new string('░', emptyBars));
        Console.ResetColor();

        Console.WriteLine($"] {healthText}");
    }

    public static void DrawSpeedBar(int current, int length, string label = "Скорость")
    {
        float percentage = (float)current / 100;
        int bars = (int)(length * percentage);
        bars = Math.Max(0, Math.Min(bars, length));
        int emptyBars = length - bars;

        Console.Write($"{label}: [");
        Console.ForegroundColor = ConsoleColor.Cyan;
        Console.Write(new string('█', bars));
        Console.Write(new string('░', emptyBars));
        Console.ResetColor();
        Console.WriteLine($"] {current}%");
    }

    public static void DrawStatBlock(int attack, int defence, int agility, int evasionChance)
    {
        Console.WriteLine($"АТК: {attack} | ЗЩТ: {defence} | ЛОВ: {agility} | УКЛ: {evasionChance}%");
    }

    public static void DrawHeader(string text, ConsoleColor color = ConsoleColor.Yellow)
    {
        Console.ForegroundColor = color;
        Console.WriteLine(text);
        Console.ResetColor();
    }

}