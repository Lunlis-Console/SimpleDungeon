using Engine;

public static class ScreenManager
{
    private static readonly Stack<BaseScreen> _screenStack = new Stack<BaseScreen>();
    private static bool _needsRedraw = true;
    private static bool _needsFullRedraw = true; // Начинаем с полной перерисовки

    public static int ScreenCount => _screenStack.Count;
    public static BaseScreen CurrentScreen => _screenStack.Count > 0 ? _screenStack.Peek() : null;

    public static void PushScreen(BaseScreen screen)
    {
        _screenStack.Push(screen);
        RequestFullRedraw();
    }

    public static BaseScreen PopScreen()
    {
        if (_screenStack.Count > 0)
        {
            var screen = _screenStack.Pop();
            RequestFullRedraw();
            return screen;
        }
        return null;
    }

    public static void HandleInput(ConsoleKeyInfo keyInfo)
    {
        var currentScreen = CurrentScreen;
        if (currentScreen != null)
        {
            currentScreen.HandleInput(keyInfo);
        }
    }

    public static void RenderCurrentScreen()
    {
        var currentScreen = CurrentScreen;
        if (currentScreen != null && _needsRedraw)
        {
            if (_needsFullRedraw)
            {
                GameServices.BufferedRenderer.SetNeedsFullRedraw();
            }

            currentScreen.Render();
            _needsRedraw = false;
        }
    }

    public static void Update()
    {
        var currentScreen = CurrentScreen;
        if (currentScreen != null)
        {
            currentScreen.Update();
        }
    }

    public static void RequestPartialRedraw()
    {
        _needsRedraw = true;
        _needsFullRedraw = false;
    }

    public static void RequestFullRedraw()
    {
        _needsRedraw = true;
        _needsFullRedraw = true;
    }

    public static void ForceRedraw()
    {
        RequestFullRedraw();
        RenderCurrentScreen();
    }

    public static void ReturnToMainScreen()
    {
        while (_screenStack.Count > 1)
        {
            _screenStack.Pop();
        }
        RequestFullRedraw();
    }

    public static void ClearAllScreens()
    {
        _screenStack.Clear();
        RequestFullRedraw();
    }
}