using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Location
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public List<Item> ItemsHere { get; set; }
        public List<Chest> ChestsHere { get; set; }
        public List<NPC> NPCsHere { get; set; }
        public List<Monster> MonstersHere { get; set; }
        public Location LocationToNorth { get; set; }
        public Location LocationToEast { get; set; }
        public Location LocationToSouth { get; set; }
        public Location LocationToWest { get; set; }
        public List<Monster> MonsterTamplates { get; set; }
        public bool ScaleMonstersToPlayerLevel {  get; set; }


        public Location(int id, string name, string description, List<Monster> monsterTamplates = null,
            bool scaleMonstersToPlayerLevel = true)
        {
            ID = id;
            Name = name;
            Description = description;
            MonsterTamplates = monsterTamplates ?? new List<Monster>();
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

            foreach(Monster template in MonsterTamplates)
            {
                Monster monsterToSpawn;
                if(ScaleMonstersToPlayerLevel)
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
    }
}
