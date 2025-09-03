using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class WorldEntity
    {
        public object Entity { get; }
        public EntityType Type { get; }
        public string DisplayName { get; }

        // Добавляем свойство для удобного доступа к имени
        public string Name
        {
            get
            {
                return Entity switch
                {
                    Monster m => m.Name,
                    NPC n => n.Name,
                    InventoryItem i => i.Details.Name,
                    _ => "Неизвестный объект"
                };
            }
        }

        public WorldEntity(object entity, EntityType type, string displayName)
        {
            Entity = entity;
            Type = type;
            DisplayName = displayName;
        }
    }
}
