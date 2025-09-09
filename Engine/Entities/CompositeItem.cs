using Engine.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine.Entities
{
    public class CompositeItem : Item
    {
        public List<IItemComponent> Components { get; } = new List<IItemComponent>();

        public CompositeItem(int id, string name, string namePlural, ItemType type, int price, string description = "")
            : base(id, name, namePlural, type, price, description)
        {
        }
    }
}
