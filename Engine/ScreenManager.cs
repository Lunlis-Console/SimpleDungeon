using Engine;

public static class ScreenManager
{
    private static readonly Stack<BaseScreen> _screenStack = new Stack<BaseScreen>();
    private static bool _needsRedraw = true;
    private static bool _needsFullRedraw = true; // Начинаем с полной перерисовки

    public static int ScreenCount => _screenStack.Count;
    public static BaseScreen CurrentScreen => _screenStack.Count > 0 ? _screenStack.Peek() : null;

    // В ScreenManager.PushScreen
    public static void PushScreen(BaseScreen screen)
    {
        _screenStack.Push(screen);
        DebugConsole.Log($"[screenmanager] PushScreen: {screen?.GetType().Name}");
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

    // В методе, который вызывает Render у активного экрана (или внутри ScreenManager.Render)
    public static void RenderCurrentScreen()
    {
        var cur = ScreenManager.CurrentScreen;
        if (cur != null)
        {
            try
            {
                //DebugConsole.Log($"[screenmanager] about to Render {cur.GetType().Name}");
                cur.Render();
            }
            catch (Exception ex)
            {
                //DebugConsole.Log($"[screenmanager] Render of {cur.GetType().Name} threw {ex.GetType().Name}: {ex.Message}");
                DebugConsole.Log(ex.StackTrace ?? "");
            }
        }
        else
        {
            //DebugConsole.Log("[screenmanager] CurrentScreen is NULL in render loop");
        }
    }

    // Возвращаемся к экрану типа T, удаляя экраны выше него в стеке.
    // Защита: не удаляем последний экран в стеке (чтобы стек не стал пустым).
    public static void PopUntil<T>() where T : BaseScreen
    {
        while (_screenStack.Count > 1 && !(_screenStack.Peek() is T))
        {
            _screenStack.Pop();
        }
        RequestFullRedraw();
    }

    // Альтернатива: возвращаемся к экрану по System.Type
    public static void PopUntil(Type screenType)
    {
        if (screenType == null) return;
        while (_screenStack.Count > 1 && _screenStack.Peek()?.GetType() != screenType)
        {
            _screenStack.Pop();
        }
        RequestFullRedraw();
    }

    // Удобство: по имени класса (например "WorldScreen" или "MapScreen")
    public static void PopUntil(string screenTypeName)
    {
        if (string.IsNullOrEmpty(screenTypeName)) return;
        while (_screenStack.Count > 1 && _screenStack.Peek()?.GetType().Name != screenTypeName)
        {
            _screenStack.Pop();
        }
        RequestFullRedraw();
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

    public static T GetScreen<T>() where T : BaseScreen
    {
        // Ищем экран в стеке (кроме текущего активного)
        foreach (var screen in _screenStack)
        {
            if (screen is T result && screen != CurrentScreen)
            {
                return result;
            }
        }
        return null;
    }
}