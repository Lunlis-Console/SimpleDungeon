using Engine.Core;
using Engine.UI;

public static class ScreenManager
{
    private static readonly Stack<BaseScreen> _screenStack = new Stack<BaseScreen>();
    private static bool _needsRedraw = true;
    private static bool _needsFullRedraw = true; // Начинаем с полной перерисовки

    public static int ScreenCount => _screenStack.Count;
    public static BaseScreen? CurrentScreen => _screenStack.Count > 0 ? _screenStack.Peek() : null;

    // В ScreenManager.PushScreen
    public static void PushScreen(BaseScreen screen)
    {
        _screenStack.Push(screen);
        DebugConsole.Log($"[менеджер экранов] PushScreen: {screen?.GetType().Name}");
        RequestFullRedraw();

        // Попробуем немедленно показать экран, если рендерер сейчас НЕ в кадре.
        // RenderCurrentScreen сам корректно работает с GameServices.BufferedRenderer.InFrame,
        // но мы дополнительно здесь не вызываем RenderCurrentScreen, если InFrame==true,
        // чтобы избежать потенциальной нежелательной реентрантности в середине кадра.
        try
        {
            var renderer = GameServices.BufferedRenderer;
            if (renderer == null)
            {
                DebugConsole.Log("[менеджер экранов] PushScreen: BufferedRenderer равен null, откладываем отрисовку");
                return;
            }

            if (renderer.InFrame)
            {
                // Мы уже внутри кадра — текущий кадр завершится нормально и учтёт флажки _needsRedraw.
                DebugConsole.Log("[менеджер экранов] PushScreen: отрисовщик сейчас InFrame — отложена немедленная отрисовка");
                return;
            }

            // Безопасно вызвать немедленный рендер — RenderCurrentScreen начнёт кадр и отрисует новый экран
            RenderCurrentScreen();
            DebugConsole.Log("[менеджер экранов] PushScreen: немедленная отрисовка выполнена");
        }
        catch (Exception ex)
        {
            DebugConsole.Log("[screenmanager] PushScreen immediate render failed: " + ex.Message);
        }
    }

    public static BaseScreen? PopScreen()
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
    // Пример: ScreenManager.RenderCurrentScreen() или аналогичный метод
    public static void RenderCurrentScreen()
    {
        try
        {
            var cur = CurrentScreen; // замените на ваше поле/свойство текущего экрана
            if (cur == null) return;

            var renderer = GameServices.BufferedRenderer;
            if (renderer == null)
            {
                DebugConsole.Log("[ScreenManager] BufferedRenderer is null");
                return;
            }

            bool startedFrame = false;
            if (!renderer.InFrame)
            {
                renderer.BeginFrame();
                startedFrame = true;
            }

            try
            {
                cur.Render();         // НЕ должен внутри себя вызывать Begin/End
                cur.RenderOverlay();  // отрисовываем оверлей в том же фрейме
            }
            finally
            {
                if (startedFrame)
                {
                    renderer.EndFrame();
                }
            }
        }
        catch (Exception ex)
        {
            DebugConsole.Log("[ScreenManager] RenderCurrentScreen failed: " + ex.Message);
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

    public static T? GetScreen<T>() where T : BaseScreen
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