namespace Engine.Core
{
    public static class MessageSystem
    {
        private static readonly Queue<MessageData> _messages = new Queue<MessageData>();
        private const int MaxMessages = 3;
        private const int MessageLifetime = 5000; // 5 секунд

        public class MessageData
        {
            public string Text { get; set; } = string.Empty;
            public DateTime CreatedTime { get; set; }
            public float Alpha { get; set; } = 1.0f;
        }

        public static IEnumerable<MessageData> Messages => _messages;

        public static void AddMessage(string message)
        {
            // Проверяем, нет ли уже такого сообщения в очереди
            if (_messages.Any(m => m.Text == message && 
                                 (DateTime.Now - m.CreatedTime).TotalMilliseconds < 1000))
            {
                return; // Не добавляем дубликаты
            }

            _messages.Enqueue(new MessageData 
            { 
                Text = message, 
                CreatedTime = DateTime.Now 
            });
            
            while (_messages.Count > MaxMessages)
            {
                _messages.Dequeue();
            }
        }

        public static void ClearMessages()
        {
            _messages.Clear();
        }

        public static void UpdateMessages()
        {
            var currentTime = DateTime.Now;
            var messagesToRemove = new List<MessageData>();
            
            // Обновляем прозрачность и находим сообщения для удаления
            foreach (var message in _messages)
            {
                var age = (currentTime - message.CreatedTime).TotalMilliseconds;
                if (age > MessageLifetime)
                {
                    messagesToRemove.Add(message);
                }
                else if (age > MessageLifetime - 1000)
                {
                    message.Alpha = 1.0f - (float)(age - (MessageLifetime - 1000)) / 1000;
                }
            }
            
            // Удаляем старые сообщения
            foreach (var message in messagesToRemove)
            {
                // Создаем новую очередь без удаляемых сообщений
                var newQueue = new Queue<MessageData>();
                foreach (var msg in _messages)
                {
                    if (!messagesToRemove.Contains(msg))
                    {
                        newQueue.Enqueue(msg);
                    }
                }
                _messages.Clear();
                foreach (var msg in newQueue)
                {
                    _messages.Enqueue(msg);
                }
            }
        }
    }
}