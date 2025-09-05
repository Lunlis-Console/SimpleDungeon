using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
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

        public static string WaitForInput()
        {
            return Console.ReadLine();
        }
    }
}
