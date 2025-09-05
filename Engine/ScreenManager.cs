using Engine;

public static class ScreenManager
{
    private static Stack<BaseScreen> _screenStack = new Stack<BaseScreen>();
    private static bool _needsRedraw = true;

    public static void PushScreen(BaseScreen screen)
    {
        _screenStack.Push(screen);
        _needsRedraw = true;
    }

    public static void PopScreen()
    {
        if (_screenStack.Count > 1)
            _screenStack.Pop();
        _needsRedraw = true;
    }

    public static void RenderCurrentScreen()
    {
        if (_screenStack.Count == 0) return;

        _screenStack.Peek().Render();
        _needsRedraw = false; // ВАЖНО: сбрасываем флаг после отрисовки
    }

    public static void HandleInput(ConsoleKeyInfo keyInfo)
    {
        if (_screenStack.Count == 0) return;
        _screenStack.Peek().HandleInput(keyInfo);
        _needsRedraw = true; // После ввода запрашиваем перерисовку
    }

    public static bool NeedsRedraw => _needsRedraw;
    public static void SetNeedsRedraw() => _needsRedraw = true;

    public static void ReturnToMainScreen()
    {
        while (_screenStack.Count > 1)
        {
            _screenStack.Pop();
        }
        _needsRedraw = true;
    }

    public static int ScreenCount => _screenStack.Count;
}