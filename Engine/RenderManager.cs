using Engine.Core;

public static class RenderManager
{
    public static void ForceFullRedraw()
    {
        GameServices.BufferedRenderer.SetNeedsFullRedraw();
        ScreenManager.RequestPartialRedraw();
        ScreenManager.RequestFullRedraw();
    }

    public static void ScheduleRedraw()
    {
        ScreenManager.RequestPartialRedraw();
    }
}