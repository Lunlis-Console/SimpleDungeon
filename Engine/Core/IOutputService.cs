namespace Engine.Core
{
    public interface IOutputService
    {
        void Write(string message);
        void WriteLine(string message);
        void Clear();
        void SetCursorPosition(int left, int top);

        // Новые методы для буферизации
        void BeginBuffer();
        void EndBuffer();
        void Render();
        void RenderPartial(int left, int top, int width, int height);
    }
}