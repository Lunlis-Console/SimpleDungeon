using Engine.Core;
using Engine.Entities;

namespace Engine.UI
{
    public class SkillsScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex = 0;
        private readonly List<SkillInfo> _skills;

        public SkillsScreen(Player player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _skills = new List<SkillInfo>
            {
                new SkillInfo("Взлом замков", "lockpicking", _player.Skills.Lockpicking),
                new SkillInfo("Боевые навыки", "combat", _player.Skills.Combat),
                new SkillInfo("Магия", "magic", _player.Skills.Magic),
                new SkillInfo("Ремесло", "crafting", _player.Skills.Crafting),
                new SkillInfo("Скрытность", "stealth", _player.Skills.Stealth)
            };
        }

        private class SkillInfo
        {
            public string DisplayName { get; }
            public string InternalName { get; }
            public Skill Skill { get; }

            public SkillInfo(string displayName, string internalName, Skill skill)
            {
                DisplayName = displayName;
                InternalName = internalName;
                Skill = skill;
            }
        }

        public override void Render()
        {
            ClearScreen();
            RenderHeader("НАВЫКИ ПЕРСОНАЖА");
            RenderPlayerInfo();
            RenderSkillsList();
            RenderSkillDetails();
            RenderFooter("W/S - выбор │ Q - назад");
        }

        private void RenderPlayerInfo()
        {
            int y = 4;
            _renderer.Write(2, y, $"Имя: {_player.Name}", ConsoleColor.Cyan);
            _renderer.Write(40, y, $"Уровень: {_player.Level}", ConsoleColor.Cyan);
            y++;
            _renderer.Write(2, y, $"Золото: {_player.Gold:N0}", ConsoleColor.Yellow);
            y += 2;
        }

        private void RenderSkillsList()
        {
            int startY = 8;
            int maxSkills = Console.WindowHeight - 15; // Оставляем место для деталей

            _renderer.Write(2, startY, "=== СПИСОК НАВЫКОВ ===", ConsoleColor.Cyan);
            startY += 2;

            for (int i = 0; i < Math.Min(_skills.Count, maxSkills); i++)
            {
                bool isSelected = i == _selectedIndex;
                var skillInfo = _skills[i];
                var level = _player.Skills.GetSkillLevel(skillInfo.InternalName);
                var levelName = _player.Skills.GetSkillLevelName(skillInfo.InternalName);

                // Цвет уровня навыка
                ConsoleColor levelColor = level switch
                {
                    SkillLevel.Novice => ConsoleColor.Gray,
                    SkillLevel.Apprentice => ConsoleColor.Green,
                    SkillLevel.Adept => ConsoleColor.Cyan,
                    SkillLevel.Expert => ConsoleColor.Yellow,
                    SkillLevel.Master => ConsoleColor.Red,
                    _ => ConsoleColor.Gray
                };

                if (isSelected)
                {
                    _renderer.Write(2, startY + i, "► ");
                    _renderer.Write(4, startY + i, $"{skillInfo.DisplayName}", ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, startY + i, $"{skillInfo.DisplayName}");
                }

                // Уровень навыка справа
                _renderer.Write(35, startY + i, $"{levelName}", levelColor);
                _renderer.Write(50, startY + i, $"Ур. {skillInfo.Skill.Level}", ConsoleColor.White);
            }

            // Индикатор прокрутки
            if (_skills.Count > maxSkills)
            {
                _renderer.Write(Console.WindowWidth - 3, startY, "↑", ConsoleColor.DarkGray);
                _renderer.Write(Console.WindowWidth - 3, startY + maxSkills - 1, "↓", ConsoleColor.DarkGray);
            }
        }

        private void RenderSkillDetails()
        {
            int detailsY = Console.WindowHeight - 12;
            int detailsHeight = 8;

            // Очищаем область деталей
            _renderer.FillArea(0, detailsY, Console.WindowWidth, detailsHeight, ' ',
                              ConsoleColor.White, ConsoleColor.Black);

            // Разделительная линия
            _renderer.Write(0, detailsY, new string('─', Console.WindowWidth), ConsoleColor.DarkGray);

            if (_selectedIndex >= 0 && _selectedIndex < _skills.Count)
            {
                var selectedSkill = _skills[_selectedIndex];
                RenderSelectedSkillDetails(selectedSkill, detailsY + 1);
            }
            else
            {
                _renderer.Write(2, detailsY + 2, "Выберите навык для просмотра деталей", ConsoleColor.DarkGray);
            }
        }

        private void RenderSelectedSkillDetails(SkillInfo skillInfo, int startY)
        {
            var level = _player.Skills.GetSkillLevel(skillInfo.InternalName);
            var levelName = _player.Skills.GetSkillLevelName(skillInfo.InternalName);

            // Цвет уровня навыка
            ConsoleColor levelColor = level switch
            {
                SkillLevel.Novice => ConsoleColor.Gray,
                SkillLevel.Apprentice => ConsoleColor.Green,
                SkillLevel.Adept => ConsoleColor.Cyan,
                SkillLevel.Expert => ConsoleColor.Yellow,
                SkillLevel.Master => ConsoleColor.Red,
                _ => ConsoleColor.Gray
            };

            int y = startY;

            // Название навыка
            _renderer.Write(2, y, $"{skillInfo.DisplayName}", ConsoleColor.White);
            y++;

            // Уровень и название уровня
            _renderer.Write(2, y, $"Уровень: {skillInfo.Skill.Level} ({levelName})", levelColor);
            y++;

            // Опыт
            _renderer.Write(2, y, $"Опыт: {skillInfo.Skill.GetExperienceProgress()}/{skillInfo.Skill.GetTotalExperienceToNextLevel()}", ConsoleColor.Cyan);
            y++;

            // Прогресс-бар
            RenderProgressBar(2, y, skillInfo.Skill, levelColor);
            y += 2;

            // Описание навыка
            string description = GetSkillDescription(skillInfo.InternalName);
            var descriptionLines = WrapText(description, Console.WindowWidth - 4);
            for (int i = 0; i < Math.Min(2, descriptionLines.Count); i++)
            {
                if (y < Console.WindowHeight - 3)
                {
                    _renderer.Write(2, y, descriptionLines[i], ConsoleColor.Gray);
                    y++;
                }
            }
        }

        private void RenderProgressBar(int x, int y, Skill skill, ConsoleColor color)
        {
            int barWidth = 40;
            int filledWidth = (int)(skill.GetProgressPercentage() / 100.0 * barWidth);
            
            _renderer.Write(x, y, "Прогресс: [");
            for (int i = 0; i < barWidth; i++)
            {
                if (i < filledWidth)
                {
                    _renderer.Write(x + 12 + i, y, "█", color);
                }
                else
                {
                    _renderer.Write(x + 12 + i, y, "░", ConsoleColor.DarkGray);
                }
            }
            _renderer.Write(x + 12 + barWidth, y, $"] {skill.GetProgressPercentage():F1}%");
        }

        private string GetSkillDescription(string skillName)
        {
            return skillName switch
            {
                "lockpicking" => "Позволяет взламывать замки различной сложности. Чем выше уровень, тем сложнее замки вы можете открыть.",
                "combat" => "Улучшает ваши боевые способности, увеличивая урон и точность атак в сражениях.",
                "magic" => "Открывает доступ к магическим заклинаниям и увеличивает магическую силу персонажа.",
                "crafting" => "Позволяет создавать и улучшать предметы, а также ремонтировать поврежденное снаряжение.",
                "stealth" => "Повышает скрытность персонажа, позволяя избегать обнаружения врагами и совершать скрытные атаки.",
                _ => "Описание навыка недоступно."
            };
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            HandleCommonInput(keyInfo);

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    if (_skills.Count > 0)
                    {
                        _selectedIndex = Math.Max(0, _selectedIndex - 1);
                        RequestPartialRedraw();
                    }
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (_skills.Count > 0)
                    {
                        _selectedIndex = Math.Min(_skills.Count - 1, _selectedIndex + 1);
                        RequestPartialRedraw();
                    }
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        public override void Update()
        {
            if (_needsRedraw)
            {
                if (_needsFullRedraw)
                {
                    Render();
                }
                else
                {
                    // Частичная перерисовка только области деталей навыка
                    RenderSkillDetails();
                }
                _needsRedraw = false;
                _needsFullRedraw = false;
            }
        }
    }
}

