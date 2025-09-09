using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public class DamageComponent : IItemComponent
    {
        public string ComponentType => "Damage";
        public int Damage { get; set; }
        public int Range { get; set; }
    }
}
