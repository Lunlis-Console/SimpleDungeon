namespace Engine
{
    public static class NavigationHelper
    {
        public static void ShowMessage(string message)
        {
            MessageSystem.AddMessage(message);
            ScreenManager.SetNeedsRedraw();
        }

        public static void ReturnToGameWorld()
        {
            while (ScreenManager.ScreenCount > 1)
            {
                ScreenManager.PopScreen();
            }
            ScreenManager.SetNeedsRedraw();
        }

        public static void RefreshCurrentScreen()
        {
            ScreenManager.SetNeedsRedraw();
        }
    }
}