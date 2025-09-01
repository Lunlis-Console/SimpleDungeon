using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public static class MessageSystem
    {
        private static readonly Queue<string> messages = new Queue<string>();
        private const int MaxMessages = 1;

        public static void AddMessage(string message)
        {
            messages.Enqueue(message);
            while (messages.Count > MaxMessages)
            {
                messages.Dequeue();
            }
        }

        public static void ClearMessages()
        {
            messages.Clear();
        }

        public static void DisplayMessages()
        {
            if (messages.Count == 0)
                return;

            //Console.WriteLine("=== СИСТЕМНЫЕ СООБЩЕНИЯ ===");
            foreach (var message in messages)
            {
                Console.Write($" - {message}");
            }
            //Console.WriteLine("=======================");
            Console.WriteLine();
        }
    }

}
