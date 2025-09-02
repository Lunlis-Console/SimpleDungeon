using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Equipment : Item
    {
        public int AttackBonus { get; set; }
        public int DefenceBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int HealthBonus { get; set; }

        public Equipment(int id, string namePlural, int attackBonus, int defenceBonus, int agilityBonus,
            int healthBonus, ItemType type, int price, string name, string description = "") :
                base(id, name, namePlural, type, price, description)
        {
            AttackBonus = attackBonus;
            DefenceBonus = defenceBonus;
            AgilityBonus = agilityBonus;
            HealthBonus = healthBonus;
        }
    }
}
