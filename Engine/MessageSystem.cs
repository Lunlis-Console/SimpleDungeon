namespace Engine
{
    public static class MessageSystem
    {
        public static readonly Queue<string> messages = new Queue<string>();
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

            foreach (var message in messages)
            {
                GameServices.OutputService.Write($" - {message}");
            }
            GameServices.OutputService.WriteLine("");
        }
    }

}
