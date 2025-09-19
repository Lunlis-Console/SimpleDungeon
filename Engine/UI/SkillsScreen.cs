using Engine.Core;
using Engine.Entities;

namespace Engine.UI
{
    public class SkillsScreen : BaseScreen
    {
        private readonly Player _player;

        public SkillsScreen(Player player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public override void Render()
        {
            var renderer = GameServices.BufferedRenderer;
            if (renderer == null) return;

            renderer.BeginFrame();

            // Заголовок
            renderer.Write(2, 1, "=== НАВЫКИ ИГРОКА ===", ConsoleColor.Yellow);
            renderer.Write(2, 2, $"Игрок: {_player.Name}", ConsoleColor.Cyan);

            int y = 4;

            // Навык взлома
            RenderSkill(renderer, "Взлом замков", _player.Skills.Lockpicking, y);
            y += 4;

            // Навык боя
            RenderSkill(renderer, "Боевые навыки", _player.Skills.Combat, y);
            y += 4;

            // Навык магии
            RenderSkill(renderer, "Магия", _player.Skills.Magic, y);
            y += 4;

            // Навык ремесла
            RenderSkill(renderer, "Ремесло", _player.Skills.Crafting, y);
            y += 4;

            // Навык скрытности
            RenderSkill(renderer, "Скрытность", _player.Skills.Stealth, y);
            y += 4;

            // Подсказки
            renderer.Write(2, renderer.Height - 3, "Нажмите любую клавишу для возврата...", ConsoleColor.Gray);

            renderer.EndFrame();
        }

        private void RenderSkill(EnhancedBufferedRenderer renderer, string skillName, Skill skill, int y)
        {
            var level = _player.Skills.GetSkillLevel(skillName.ToLower());
            var levelName = _player.Skills.GetSkillLevelName(skillName.ToLower());
            
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

            // Название навыка
            renderer.Write(2, y, $"{skillName}:", ConsoleColor.White);
            
            // Уровень и значение
            renderer.Write(2, y + 1, $"  Уровень: {skill.Level} ({levelName})", levelColor);
            renderer.Write(2, y + 2, $"  Опыт: {skill.GetExperienceProgress()}/{skill.GetTotalExperienceToNextLevel()}");

            // Прогресс-бар
            int barWidth = 30;
            int filledWidth = (int)(skill.GetProgressPercentage() / 100.0 * barWidth);
            
            renderer.Write(2, y + 3, "  Прогресс: [");
            for (int i = 0; i < barWidth; i++)
            {
                if (i < filledWidth)
                {
                    renderer.Write(14 + i, y + 3, "█", levelColor);
                }
                else
                {
                    renderer.Write(14 + i, y + 3, "░", ConsoleColor.DarkGray);
                }
            }
            renderer.Write(14 + barWidth, y + 3, "]");
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            // Возвращаемся к предыдущему экрану
            ScreenManager.PopScreen();
        }
    }
}
