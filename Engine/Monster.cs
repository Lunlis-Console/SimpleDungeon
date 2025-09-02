using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Monster : LivingCreature
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public int Level { get; set; }
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int Agility { get; set; }
        public int CurrentSpeed { get; set; }
        public int RewardEXP { get; set; }
        public int RewardGold { get; set; }
        public List<LootItem> LootTable { get; set; }

        public Monster (int id, string name, int level, int currentHP, int maximumHP, 
            int attack, int defence, int rewardEXP, int rewardGold, int agility) :
                base (currentHP, maximumHP)
        {
            ID = id;
            Name = name;
            Level = level;
            Attack = attack;
            Defence = defence;
            Agility = agility;
            CurrentSpeed = 0;
            RewardEXP = rewardEXP;
            RewardGold = rewardGold;

            LootTable = new List<LootItem> ();
        }

        public Monster(Monster baseMonster, int newLevel) :
            base (CalculateScaledHP(baseMonster, newLevel), CalculateScaledHP(baseMonster, newLevel))
        {
            ID = baseMonster.ID;
            Name = baseMonster.Name;
            Level = newLevel;
            Attack = (int)(baseMonster.Attack * (1 - (newLevel - baseMonster.Level) * 0.2));
            Defence = (int)(baseMonster.Defence * (1 - (newLevel - baseMonster.Level) * 0.15));
            Agility = (int)(baseMonster.Agility * (1 + (newLevel - baseMonster.Level) * 0.1));
            CurrentSpeed = 0;
            RewardEXP = (int)(baseMonster.RewardEXP * (1 + (newLevel - baseMonster.Level) * 0.5));
            RewardGold = (int)(baseMonster.RewardGold * (1 + (newLevel - baseMonster.Level) * 0.3));
            LootTable = new List<LootItem>(baseMonster.LootTable);
        }

        private static int CalculateScaledHP(Monster baseMonster, int newLevel)
        {
            double levelRatio = (double)newLevel / baseMonster.Level;
            return (int)(baseMonster.MaximumHP * levelRatio * 1.1);
        }
        public List<Item> GetLoot()
        {
            List<Item> loot = new List<Item> ();
            Random random = new Random ();

            foreach(LootItem lootItem in LootTable)
            {
                if (random.Next(100) < lootItem.DropPercentage)
                {
                    loot.Add(lootItem.Details);
                }

                if(lootItem.IsUnique)
                {
                    LootTable.Remove(lootItem);
                }
            } 
            return loot;
        }
    }
}
