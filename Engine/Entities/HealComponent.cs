using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public class HealComponent : IItemComponent
    {
        public string ComponentType => "Heal";
        public int HealAmount { get; set; }
    }
}
