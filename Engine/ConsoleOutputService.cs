namespace Engine
{
    public class ConsoleOutputService : IOutputService, IDisposable
    {
        private bool _disposed;

        public void Write(string message)
        {
            if (_disposed) return;
            Console.Write(message);
        }

        public void WriteLine(string message)
        {
            if (_disposed) return;
            Console.WriteLine(message);
        }

        public void Clear()
        {
            if (_disposed) return;
            Console.Clear();
        }

        public void SetCursorPosition(int left, int top)
        {
            if (_disposed) return;
            Console.SetCursorPosition(left, top);
        }

        public void BeginBuffer() { }
        public void EndBuffer() { }
        public void Render() { }
        public void RenderPartial(int left, int top, int width, int height) { }

        public void Dispose()
        {
            _disposed = true;
            Console.ResetColor();
        }
    }
}