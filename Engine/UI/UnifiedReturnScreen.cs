namespace Engine.UI
{
    public class UnifiedReturnScreen : BaseScreen
    {
        private int _selectedIndex;
        private readonly string[] _options =
        {
            "Да, вернуться в главное меню",
            "Нет, остаться в игре"
        };

        public UnifiedReturnScreen()
        {
            _selectedIndex = 0;
        }

        public override void Render()
        {
            ClearScreen();

            RenderHeader("ПОДТВЕРЖДЕНИЕ ВЫХОДА");
            RenderWarningMessage();
            RenderOptions();
            RenderFooter("W/S - выбор │ E - подтвердить │ Q - отмена");
        }

        private void RenderWarningMessage()
        {
            int y = 6;
            var warningLines = WrapText(
                "Внимание! Несохраненный прогресс будет потерян. " +
                "Вы уверены что хотите вернуться в главное меню?",
                Console.WindowWidth - 4
            );

            foreach (var line in warningLines)
            {
                RenderCenteredText(y, line, ConsoleColor.Red);
                y++;
            }
            y++;
        }

        private void RenderOptions()
        {
            int startY = 10;

            for (int i = 0; i < _options.Length; i++)
            {
                bool isSelected = i == _selectedIndex;
                int y = startY + i * 2;

                if (isSelected)
                {
                    RenderCenteredText(y, $"► {_options[i]}", ConsoleColor.Green);
                }
                else
                {
                    RenderCenteredText(y, $"  {_options[i]}", ConsoleColor.White);
                }
            }
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
                    _selectedIndex = Math.Min(_options.Length - 1, _selectedIndex + 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    ExecuteSelection();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen(); // Просто закрываем экран подтверждения
                    break;
            }
        }

        private void ExecuteSelection()
        {
            if (_selectedIndex == 0) // "Да, вернуться"
            {
                ScreenManager.ClearAllScreens();
                ScreenManager.PushScreen(new MainMenuScreen());
            }
            else // "Нет, остаться"
            {
                ScreenManager.PopScreen();
            }
        }

        public override void Update()
        {
            // Базовая реализация - ничего не делаем
            // Рендеринг управляется через RequestPartialRedraw()
        }
    }
}