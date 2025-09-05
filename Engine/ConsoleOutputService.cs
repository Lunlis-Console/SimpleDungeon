// ConsoleOutputService.cs
using System;

namespace Engine
{
    public class ConsoleOutputService : IOutputService
    {
        public void Write(string message) => Console.Write(message);
        public void WriteLine(string message) => Console.WriteLine(message);
        public void Clear() => Console.Clear();
        public void SetCursorPosition(int left, int top) => Console.SetCursorPosition(left, top);

        // Пустые реализации для совместимости
        public void BeginBuffer() { }
        public void EndBuffer() { }
        public void Render() { }
        public void RenderPartial(int left, int top, int width, int height) { }
    }
}