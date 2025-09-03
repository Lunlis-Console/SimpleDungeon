using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class WorldEntity
    {
        public IInteractable Entity { get; }
        public EntityType Type { get; }
        public string DisplayName { get; }

        public WorldEntity(IInteractable entity, EntityType type, string displayName)
        {
            Entity = entity;
            Type = type;
            DisplayName = displayName;
        }

        public override string ToString()
        {
            return DisplayName;
        }
    }
}
