namespace Engine
{
    public class MenuSystem
    {
        public static T SelectFromList<T>(List<T> items, Func<T, string> displaySelector,
            string title = "Выберите вариант",
            string controlHint = "Клавиши 'W' 'S' для выбора, 'E' для подтверждения, 'Q' для выхода",
            bool showNumbers = false)
        {
            if (items == null || items.Count == 0)
                return default(T);

            if (items.Count == 1)
                return items[0];

            int selectedIndex = 0;
            ConsoleKey key;

            do
            {
                Console.Clear();
                Console.WriteLine($"{title}");
                Console.WriteLine($"{controlHint}");
                Console.WriteLine();

                for (int i = 0; i < items.Count; i++)
                {
                    if (i == selectedIndex)
                    {
                        Console.ForegroundColor = ConsoleColor.Green;
                        Console.Write(">");
                    }
                    else
                    {
                        Console.Write("  ");
                    }

                    if (showNumbers)
                    {
                        Console.Write($"{i + 1}. ");
                    }

                    Console.WriteLine(displaySelector(items[i]));
                    Console.ResetColor();
                }

                key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.UpArrow:
                        selectedIndex = (selectedIndex - 1 + items.Count) % items.Count;
                        break;
                    case ConsoleKey.S:
                    case ConsoleKey.DownArrow:
                        selectedIndex = (selectedIndex + 1) % items.Count;
                        break;
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        return default(T);
                }
            } while (key != ConsoleKey.E && key != ConsoleKey.Enter && key != ConsoleKey.Spacebar);

            Console.Clear();

            return items[selectedIndex];
        }

        public static string ShowNPCMenu(string npcName, string greeting, List<string> options)
        {
            return SelectFromList(
                options,
                option => option,
                $"======{npcName}======\n{greeting}",
                "Клавиши 'W' 'S' для выбора, 'E' для подтверждения"
            );
        }

        // Новый метод для подтверждения действий
        public static bool ConfirmAction(string message)
        {
            var options = new List<string> { "Да", "Нет" };
            var choice = SelectFromList(
                options,
                opt => opt,
                message,
                "Выберите вариант"
            );
            return choice == "Да";
        }
    }
}