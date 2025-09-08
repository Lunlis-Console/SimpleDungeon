namespace Engine.Core
{
    public static class InputHandler
    {
        public static ConsoleKey WaitForKey(params ConsoleKey[] validKeys)
        {
            while (true)
            {
                var key = Console.ReadKey(true).Key;
                if (validKeys.Length == 0 || validKeys.Contains(key))
                {
                    return key;
                }
            }
        }

    }
}
