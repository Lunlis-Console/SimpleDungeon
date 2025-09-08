using Engine;

public static class RenderHelper
{
    public static bool NeedsFullRedrawForScreenChange => true;
    public static bool NeedsPartialRedrawForSelection => false;

    public static void RequestAppropriateRedraw(bool isMajorChange)
    {
        if (isMajorChange)
        {
            ScreenManager.RequestFullRedraw();
        }
        else
        {
            ScreenManager.RequestPartialRedraw();
        }
    }
}