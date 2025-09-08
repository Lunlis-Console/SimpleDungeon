namespace Engine.Core
{
    public static class NavigationHelper
    {
        public static void ShowMessage(string message)
        {
            MessageSystem.AddMessage(message);
            ScreenManager.RequestPartialRedraw();
        }

        public static void ReturnToGameWorld()
        {
            while (ScreenManager.ScreenCount > 1)
            {
                ScreenManager.PopScreen();
            }
            ScreenManager.RequestPartialRedraw();
        }

        public static void RefreshCurrentScreen()
        {
            ScreenManager.RequestPartialRedraw();
        }
    }
}