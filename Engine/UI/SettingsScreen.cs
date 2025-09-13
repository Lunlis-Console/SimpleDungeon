using Engine.Core;
using Engine.Audio;

namespace Engine.UI
{
    public class SettingsScreen : BaseScreen
    {
        private int _selectedIndex;
        private readonly string[] _menuItems =
        {
            "ГРОМКОСТЬ МУЗЫКИ",
            "НАЗАД"
        };

        public override void Render()
        {
            ClearScreen();
            RenderHeader("НАСТРОЙКИ", 2, ConsoleColor.Yellow);

            int startY = 8;
            for (int i = 0; i < _menuItems.Length; i++)
            {
                bool isSelected = i == _selectedIndex;
                int y = startY + i * 2;

                string text = _menuItems[i];
                if (i == 0)
                {
                    int percent = (int)(GameSettings.MusicVolume * 100);
                    text += $": {percent}%";
                }

                RenderCenteredText(y, text, isSelected ? ConsoleColor.Green : ConsoleColor.White);
            }

            RenderFooter("W/S - выбор │ A/D - изменить │ ESC - назад", 0);
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            HandleCommonInput(keyInfo);

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_menuItems.Length - 1, _selectedIndex + 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    if (_selectedIndex == 0)
                        GameSettings.MusicVolume -= 0.1f;
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    if (_selectedIndex == 0)
                        GameSettings.MusicVolume += 0.1f;
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.Enter:
                case ConsoleKey.E:
                    if (_selectedIndex == 1)
                        ScreenManager.PopScreen();
                    break;

                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }
    }
}
