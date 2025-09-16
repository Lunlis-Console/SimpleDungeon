using Engine.Entities;

namespace Engine.World
{
    /// <summary>
    /// Представляет помещение внутри локации (подземелье, лабиринт, город и т.д.)
    /// </summary>
    public class Room
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public int ParentLocationID { get; set; } // ID родительской локации
        
        // Содержимое помещения (аналогично Location)
        public List<Item> ItemsHere { get; set; }
        public List<Chest> ChestsHere { get; set; }
        public List<NPC> NPCsHere { get; set; }
        public List<Monster> MonstersHere { get; set; }
        public List<InventoryItem> GroundItems { get; set; } = new List<InventoryItem>();
        
        // Навигация между помещениями (аналогично Location)
        public Room RoomToNorth { get; set; }
        public Room RoomToEast { get; set; }
        public Room RoomToSouth { get; set; }
        public Room RoomToWest { get; set; }
        
        // Шаблоны монстров для спавна
        public List<Monster> MonsterTemplates { get; set; }
        public bool ScaleMonstersToPlayerLevel { get; set; }
        
        // Входы в другие помещения (если есть)
        public List<RoomEntrance> Entrances { get; set; } = new List<RoomEntrance>();

        public Room(int id, string name, string description, int parentLocationID, 
            List<Monster> monsterTemplates = null, bool scaleMonstersToPlayerLevel = true)
        {
            ID = id;
            Name = name;
            Description = description;
            ParentLocationID = parentLocationID;
            MonsterTemplates = monsterTemplates ?? new List<Monster>();
            ScaleMonstersToPlayerLevel = scaleMonstersToPlayerLevel;

            ItemsHere = new List<Item>();
            ChestsHere = new List<Chest>();
            NPCsHere = new List<NPC>();
            MonstersHere = new List<Monster>();
        }

        public List<Monster> FindMonsters()
        {
            return MonstersHere.Where(monster => monster.CurrentHP > 0).ToList();
        }

        public void AddMonster(Monster monster)
        {
            MonstersHere.Add(monster);
        }

        public void CleanDeadMonster()
        {
            MonstersHere.RemoveAll(monster => monster.CurrentHP <= 0);
        }

        public void SpawnMonsters(int playerLevel)
        {
            MonstersHere.Clear();

            foreach (Monster template in MonsterTemplates)
            {
                Monster monsterToSpawn;
                if (ScaleMonstersToPlayerLevel)
                {
                    int monsterLevel = CalculateMonsterLevel(playerLevel);
                    monsterToSpawn = new Monster(template, monsterLevel);
                }
                else
                {
                    monsterToSpawn = new Monster(template, template.Level);
                }

                MonstersHere.Add(monsterToSpawn);
            }
        }

        private int CalculateMonsterLevel(int playerLevel)
        {
            Random random = new Random();

            int minLevel = Math.Max(1, playerLevel - 2);
            int maxLevel = playerLevel + 1;

            return random.Next(minLevel, maxLevel + 1);
        }

        // Метод для отображения предметов на земле
        public string GetGroundItemsDescription()
        {
            if (GroundItems.Count == 0) return "";

            var items = GroundItems
                .GroupBy(item => item.Details.Name)
                .Select(group => $"{group.Key} x{group.Sum(item => item.Quantity)}");

            return "На земле: " + string.Join(", ", items);
        }
    }
}
