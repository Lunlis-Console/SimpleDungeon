// IOutputService.cs
namespace Engine
{
    public interface IOutputService
    {
        void Write(string message);
        void WriteLine(string message);
        void Clear();
        void SetCursorPosition(int left, int top);
    }
}