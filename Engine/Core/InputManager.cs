namespace Engine.Core
{
    public static class InputManager
    {
        private static ConsoleKeyInfo _lastKey;
        private static bool _keyProcessed = true;

        public static void Update()
        {
            if (Console.KeyAvailable)
            {
                _lastKey = Console.ReadKey(true);
                _keyProcessed = false;
            }
        }

        public static bool GetKey(ConsoleKey key)
        {
            return !_keyProcessed && _lastKey.Key == key;
        }

        public static bool GetKeyDown(ConsoleKey key)
        {
            if (GetKey(key))
            {
                _keyProcessed = true;
                return true;
            }
            return false;
        }

        public static ConsoleKeyInfo GetKeyInfo()
        {
            if (!_keyProcessed)
            {
                _keyProcessed = true;
                return _lastKey;
            }
            return new ConsoleKeyInfo();
        }

        public static void Clear()
        {
            _keyProcessed = true;
            // Очищаем буфер ввода
            while (Console.KeyAvailable)
                Console.ReadKey(true);
        }
    }
}