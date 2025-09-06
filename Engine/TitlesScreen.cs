namespace Engine
{
    public class TitlesScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;

        public TitlesScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;

        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("ТИТУЛЫ И ДОСТИЖЕНИЯ");
            RenderTitlesList();
            RenderSelectedTitleInfo();
            RenderFooter("W/S - выбор │ E - активировать │ Q - назад");

            _renderer.EndFrame();
        }

        private void RenderTitlesList()
        {
            int y = 4;
            var titles = _player.UnlockedTitles;

            if (titles.Count == 0)
            {
                _renderer.Write(2, y, "У вас нет разблокированных титулов.", ConsoleColor.DarkGray);
                return;
            }

            for (int i = 0; i < titles.Count; i++)
            {
                var title = titles[i];
                bool isSelected = i == _selectedIndex;
                string displayText = $"{title.Name} {(title.IsActive ? "[АКТИВЕН]" : "")}";

                if (isSelected)
                {
                    _renderer.Write(2, y, "> ");
                    _renderer.Write(4, y, displayText, ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, y, displayText);
                }
                y++;
            }
        }

        private void RenderSelectedTitleInfo()
        {
            var titles = _player.UnlockedTitles;
            if (titles.Count == 0 || _selectedIndex >= titles.Count)
                return;

            var selectedTitle = titles[_selectedIndex];
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 4;

            _renderer.Write(rightColumn, y, $"=== {selectedTitle.Name} ===", ConsoleColor.Yellow);
            y += 2;

            // Описание
            var descriptionLines = WrapText(selectedTitle.Description, Console.WindowWidth / 2 - 4);
            foreach (var line in descriptionLines)
            {
                _renderer.Write(rightColumn, y, line);
                y++;
            }
            y++;

            // Бонусы
            _renderer.Write(rightColumn, y, "Бонусы:", ConsoleColor.Cyan);
            y++;
            _renderer.Write(rightColumn, y, selectedTitle.GetBonusDescription());
            y += 2;

            // Требования
            _renderer.Write(rightColumn, y, "Требования:", ConsoleColor.Cyan);
            y++;
            _renderer.Write(rightColumn, y, GetRequirementDescription(selectedTitle));
            y++;

            // Статус
            string status = selectedTitle.IsActive ? "АКТИВЕН" : "НЕ АКТИВЕН";
            ConsoleColor statusColor = selectedTitle.IsActive ? ConsoleColor.Green : ConsoleColor.Red;
            _renderer.Write(rightColumn, y, $"Статус: {status}", statusColor);
        }

        private string GetRequirementDescription(Title title)
        {
            switch (title.RequirementType)
            {
                case "MonsterKill": return $"Убийств {title.RequirementTarget}: {title.RequirementAmount}";
                case "QuestComplete": return $"Выполнено квестов: {title.RequirementAmount}";
                case "TotalMonstersKilled": return $"Убито монстров: {title.RequirementAmount}";
                default: return $"Требуется: {title.RequirementAmount}";
            }
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            var titles = _player.UnlockedTitles;
            //if (titles.Count == 0) return;

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(titles.Count - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    ToggleTitleActivation(titles[_selectedIndex]);
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                case ConsoleKey.T:
                    ScreenManager.PopScreen();
                    break;
            }


        }

        private void ToggleTitleActivation(Title title)
        {
            if (title.IsActive)
            {
                _player.DeactivateTitle();
            }
            else
            {
                _player.ActivateTitle(title);
            }
        }
    }
}