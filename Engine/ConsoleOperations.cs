public static class ConsoleOperations
{
    public static void ClearLine(int line)
    {
        int originalLeft = Console.CursorLeft;
        int originalTop = Console.CursorTop;

        try
        {
            Console.SetCursorPosition(0, line);
            Console.Write(new string(' ', Console.WindowWidth));
            Console.SetCursorPosition(originalLeft, originalTop);
        }
        catch (ArgumentOutOfRangeException)
        {
            // Игнорируем ошибку позиционирования
        }
    }

    public static void UpdateAtPosition(int line, Action drawAction)
    {
        int originalLeft = Console.CursorLeft;
        int originalTop = Console.CursorTop;

        try
        {
            ClearLine(line);
            Console.SetCursorPosition(0, line);
            drawAction();
        }
        finally
        {
            Console.SetCursorPosition(originalLeft, originalTop);
        }
    }

    public static void SaveCursorPosition()
    {
        // Дополнительные утилиты для работы с курсором
    }
}