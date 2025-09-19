using Engine.Core;

namespace Engine.Entities
{
    public class PlayerSkills
    {
        public Skill Lockpicking { get; set; } = new Skill();
        public Skill Combat { get; set; } = new Skill();
        public Skill Magic { get; set; } = new Skill();
        public Skill Crafting { get; set; } = new Skill();
        public Skill Stealth { get; set; } = new Skill();

        public PlayerSkills()
        {
        }

        public void GainExperience(string skillName, int amount)
        {
            switch (skillName.ToLower())
            {
                case "lockpicking":
                case "взлом":
                    Lockpicking.GainExperience(amount);
                    break;
                case "combat":
                case "бой":
                    Combat.GainExperience(amount);
                    break;
                case "magic":
                case "магия":
                    Magic.GainExperience(amount);
                    break;
                case "crafting":
                case "ремесло":
                    Crafting.GainExperience(amount);
                    break;
                case "stealth":
                case "скрытность":
                    Stealth.GainExperience(amount);
                    break;
            }
        }

        public SkillLevel GetSkillLevel(string skillName)
        {
            int level = skillName.ToLower() switch
            {
                "lockpicking" or "взлом" => Lockpicking.Level,
                "combat" or "бой" => Combat.Level,
                "magic" or "магия" => Magic.Level,
                "crafting" or "ремесло" => Crafting.Level,
                "stealth" or "скрытность" => Stealth.Level,
                _ => 1
            };

            return level switch
            {
                >= 1 and <= 24 => SkillLevel.Novice,
                >= 25 and <= 49 => SkillLevel.Apprentice,
                >= 50 and <= 74 => SkillLevel.Adept,
                >= 75 and <= 99 => SkillLevel.Expert,
                >= 100 => SkillLevel.Master,
                _ => SkillLevel.Novice
            };
        }

        public string GetSkillLevelName(string skillName)
        {
            var level = GetSkillLevel(skillName);
            return level switch
            {
                SkillLevel.Novice => "Новичок",
                SkillLevel.Apprentice => "Ученик",
                SkillLevel.Adept => "Адепт",
                SkillLevel.Expert => "Эксперт",
                SkillLevel.Master => "Мастер",
                _ => "Новичок"
            };
        }

        public Skill GetSkill(string skillName)
        {
            return skillName.ToLower() switch
            {
                "lockpicking" or "взлом" => Lockpicking,
                "combat" or "бой" => Combat,
                "magic" or "магия" => Magic,
                "crafting" or "ремесло" => Crafting,
                "stealth" or "скрытность" => Stealth,
                _ => new Skill()
            };
        }
    }

    public class Skill
    {
        public int Level { get; set; } = 1;
        public int Experience { get; set; } = 0;
        public int ExperienceToNextLevel { get; set; } = 100;

        public Skill()
        {
            UpdateExperienceToNextLevel();
        }

        public void GainExperience(int amount)
        {
            Experience += amount;
            
            while (Experience >= ExperienceToNextLevel)
            {
                Experience -= ExperienceToNextLevel;
                Level++;
                UpdateExperienceToNextLevel();
            }
        }

        private void UpdateExperienceToNextLevel()
        {
            // Каждый уровень требует больше опыта: 100, 200, 300, 400, 500...
            ExperienceToNextLevel = Level * 100;
        }

        public int GetExperienceProgress()
        {
            return Experience;
        }

        public int GetTotalExperienceToNextLevel()
        {
            return ExperienceToNextLevel;
        }

        public double GetProgressPercentage()
        {
            if (ExperienceToNextLevel == 0) return 100.0;
            return (double)Experience / ExperienceToNextLevel * 100.0;
        }
    }

    public enum SkillLevel
    {
        Novice = 1,      // Уровень 1-24
        Apprentice = 25, // Уровень 25-49
        Adept = 50,      // Уровень 50-74
        Expert = 75,     // Уровень 75-99
        Master = 100     // Уровень 100
    }
}
