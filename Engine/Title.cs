using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Title
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string RequirementType { get; set; } // "MonsterKill", "QuestComplete", etc.
        public string RequirementTarget { get; set; } // "Rat", "Spider", etc.
        public int RequirementAmount { get; set; }
        public bool IsActive { get; set; }
        public bool IsUnlocked { get; set; }

        // Бонусы титула
        public int AttackBonus { get; set; }
        public int DefenceBonus { get; set; }
        public int HealthBonus { get; set; }
        public int GoldBonus { get; set; }
        public int ExpBonus { get; set; }

        // Бонусы против конкретных монстров
        public string BonusAgainstType { get; set; }
        public int BonusAgainstAmount { get; set; }

        public Title(int id, string name, string description, string requirementType,
                    string requirementTarget, int requirementAmount,
                    int attackBonus = 0, int defenceBonus = 0, int healthBonus = 0,
                    int goldBonus = 0, int expBonus = 0,
                    string bonusAgainstType = "", int bonusAgainstAmount = 0)
        {
            ID = id;
            Name = name;
            Description = description;
            RequirementType = requirementType;
            RequirementTarget = requirementTarget;
            RequirementAmount = requirementAmount;
            AttackBonus = attackBonus;
            DefenceBonus = defenceBonus;
            HealthBonus = healthBonus;
            GoldBonus = goldBonus;
            ExpBonus = expBonus;
            BonusAgainstType = bonusAgainstType;
            BonusAgainstAmount = bonusAgainstAmount;
            IsActive = false;
            IsUnlocked = false;
        }

        public bool CheckRequirements(Player player)
        {
            switch (RequirementType)
            {
                case "MonsterKill":
                    // Здесь нужно будет добавить счетчик убийств по типам монстров в Player
                    return player.GetMonstersKilled(RequirementTarget) >= RequirementAmount;

                case "QuestComplete":
                    return player.QuestsCompleted >= RequirementAmount;

                case "TotalMonstersKilled":
                    return player.MonstersKilled >= RequirementAmount;

                default:
                    return false;
            }
        }

        public string GetBonusDescription()
        {
            List<string> bonuses = new List<string>();

            if (AttackBonus > 0) bonuses.Add($"Атака +{AttackBonus}");
            if (DefenceBonus > 0) bonuses.Add($"Защита +{DefenceBonus}");
            if (HealthBonus > 0) bonuses.Add($"Здоровье +{HealthBonus}");
            if (GoldBonus > 0) bonuses.Add($"Золото +{GoldBonus}%");
            if (ExpBonus > 0) bonuses.Add($"Опыт +{ExpBonus}%");
            if (!string.IsNullOrEmpty(BonusAgainstType) && BonusAgainstAmount > 0)
                bonuses.Add($"Против {BonusAgainstType}: +{BonusAgainstAmount}% урона");

            return string.Join(", ", bonuses);
        }
    }
}
