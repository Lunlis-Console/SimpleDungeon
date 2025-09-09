using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public class BuffComponent : IItemComponent
    {
        public string ComponentType => "Buff";
        public string Attribute { get; set; } = "";
        public int Amount { get; set; }
        public int DurationTurns { get; set; }
    }
}
