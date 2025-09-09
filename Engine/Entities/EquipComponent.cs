using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public class EquipComponent : IItemComponent
    {
        public string ComponentType => "Equip";

        // Какой слот (например: Weapon, Armor, Helmet)
        public string Slot { get; set; } = "";

        // Бонусы от предмета
        public int AttackBonus { get; set; }
        public int DefenceBonus { get; set; }
        public int AgilityBonus { get; set; }
        public int HealthBonus { get; set; }
    }
}

