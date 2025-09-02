using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public static class World
    {
        public static readonly List<Item> Items = new List<Item>();
        public static readonly List<Monster> Monsters = new List<Monster>();
        public static readonly List<Location> Locations = new List<Location>();
        public static readonly List<Trader> Traders = new List<Trader>();
        public static readonly List<Quest> Quests = new List<Quest>();

        public const int ITEM_ID_RATS_MEAT = 1;
        public const int ITEM_ID_RUSTY_SWORD = 2;
        public const int ITEM_ID_IRON_SWORD = 3;
        public const int ITEM_ID_WEAK_HEALING_POTION = 4;
        public const int ITEM_ID_SPIDER_SILK = 5;

        public const int ITEM_ID_LEATHER_HELMET = 4;
        public const int ITEM_ID_LEATHER_ARMOR = 5;
        public const int ITEM_ID_LEATHER_GLOVES = 6;
        public const int ITEM_ID_LEATHER_BOOTS = 7;

        public const int NPC_ID_VILLAGE_TRADER = 1;
        public const int NPC_ID_VILLAGE_ELDER = 2;
        public const int NPC_ID_VILLAGE_CRAFTSMAN = 3;
        public const int NPC_ID_VILLAGE_HUNTER = 4;

        public const int QUEST_ID_RAT_HUNT = 1;
        public const int QUEST_ID_SPIDER_SILK = 2;

        public const int MONSTER_ID_RAT = 1;
        public const int MONSTER_ID_SPIDER = 2;

        public const int LOCATION_ID_VILLAGE = 1;
        public const int LOCATION_ID_FIELD_OF_NORTH = 2;
        public const int LOCATION_ID_FIELD_OF_SOUTH = 3;
        public const int LOCATION_ID_FIELD_OF_EAST = 4;
        public const int LOCATION_ID_FIELD_OF_WEST = 5;

        static World()
        {
            PopulateItems();
            PopulateMonsters();
            PopulateQuests();
            PopulateLocations();
            
        }

        private static void PopulateItems()
        {
            Items.Add(new HealingItem(ITEM_ID_RATS_MEAT, "Крысиное мясо", "Крысиное мясо", ItemType.Consumable, 5, 5, 
                "Data/Descriptions/rat_meat.txt"));
            Items.Add(new Item(ITEM_ID_SPIDER_SILK, "Паучий шелк", "", ItemType.Stuff, 10, ""));
            Items.Add(new HealingItem(ITEM_ID_WEAK_HEALING_POTION, "Слабое зелье лечения", "", ItemType.Consumable, 25, 10));
            Items.Add(new Equipment(ITEM_ID_RUSTY_SWORD, "", 5, 0, 0, ItemType.Sword, 5, "Ржавый меч", 
                "Data/Descriptions/rusty_sword.txt"));
            Items.Add(new Equipment(ITEM_ID_IRON_SWORD, "", 10, 0, 0, ItemType.Sword, 10, "Железный меч"));
            Items.Add(new Equipment(ITEM_ID_LEATHER_HELMET, "", 0, 1, 0, ItemType.Helmet, 10, "Кожаный шлем"));
            Items.Add(new Equipment(ITEM_ID_LEATHER_ARMOR, "", 0, 2, 0, ItemType.Armor, 10, "Кожаная броня"));
            Items.Add(new Equipment(ITEM_ID_LEATHER_GLOVES, "", 0, 1, 0, ItemType.Gloves, 10, "Кожаные перчатки"));
            Items.Add(new Equipment(ITEM_ID_LEATHER_BOOTS, "", 0, 1, 0, ItemType.Boots, 10, "Кожаные сапоги"));
        }

        private static void PopulateMonsters()
        {
            Monster rat = new Monster(MONSTER_ID_RAT, "Крыса", 1, 5, 5, 1, 5, 5, 5, 20);
            rat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RUSTY_SWORD), 75, false));
            rat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));

            Monster spider = new Monster(MONSTER_ID_SPIDER, "Паук", 1, 10, 10, 10, 5, 10, 10, 15);
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_IRON_SWORD), 75, false));
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_SPIDER_SILK), 60, false));

            Monsters.Add(rat);
            Monsters.Add(spider);
        }

        private static void PopulateLocations()
        {
            
            List<InventoryItem> villageTraderInventory = new List<InventoryItem>();
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_WEAK_HEALING_POTION), 5));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_SWORD), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_LEATHER_ARMOR), 1));

            Trader villageTrader = new Trader(NPC_ID_VILLAGE_TRADER, "Купец Зарубий", "Добро пожаловать в мою лавку, путник!" +
                " Товары самого высшего качества, для тебя особенная цена!", villageTraderInventory);

            // 1. Старейшина деревни - дает квест на крыс
            NPC villageElder = new NPC(NPC_ID_VILLAGE_ELDER, "Старейшина",
                "Добро пожаловать в нашу деревню, путник! Нам нужна твоя помощь.");
            villageElder.AddQuest(QuestByID(QUEST_ID_RAT_HUNT));

            // 2. Местный ремесленник - дает квест на паучий шелк
            NPC craftsman = new NPC(NPC_ID_VILLAGE_CRAFTSMAN, "Ремесленник",
                "Приветствую! Ищу качественные материалы для своих изделий.");
            craftsman.AddQuest(QuestByID(QUEST_ID_SPIDER_SILK));

            // 3. Охотник - может давать оба квеста
            NPC hunter = new NPC(NPC_ID_VILLAGE_HUNTER, "Охотник",
                "Эй, ищешь работу? У меня есть пара заданий для смельчака.");
            hunter.AddQuest(QuestByID(QUEST_ID_RAT_HUNT));
            hunter.AddQuest(QuestByID(QUEST_ID_SPIDER_SILK));



            Monster ratTemplate = MonsterByID(MONSTER_ID_RAT);
            Monster spiderTemplate = MonsterByID(MONSTER_ID_SPIDER);

            Traders.Add(villageTrader);


            Location village = new Location(LOCATION_ID_VILLAGE, "Деревня", "Здесь вы родились, тут безопасно.",
                null, false);

            village.NPCsHere.Add(villageTrader);
            village.NPCsHere.Add(villageElder);
            village.NPCsHere.Add(craftsman);
            village.NPCsHere.Add(hunter);

            List<Monster> northFieldMonsterTemplate = new List<Monster>
            {
                ratTemplate,
                ratTemplate,
                ratTemplate
            };

            Location fieldOfNorth = new Location(LOCATION_ID_FIELD_OF_NORTH, "Северная Поляна", "Поляна к северу от деревни " +
                "тут обитают крысы.", northFieldMonsterTemplate);

            List<Monster> southFieldMonsterTemplate = new List<Monster>
            {
                ratTemplate,
                ratTemplate,
                ratTemplate
            };

            Location fieldOfSouth = new Location(LOCATION_ID_FIELD_OF_SOUTH, "Южная Поляна", "Поляна к югу от деревни " +
                "тут обитают крысы.", southFieldMonsterTemplate);

            List<Monster> eastFieldMonsterTemplate = new List<Monster>
            {
                spiderTemplate,
                spiderTemplate,
                spiderTemplate
            };

            Location fieldOfEast = new Location(LOCATION_ID_FIELD_OF_EAST, "Восточная Поляна", "Поляна к востоку от деревни " +
                "тут обитают пауки.", eastFieldMonsterTemplate);

            List<Monster> westFieldMonsterTemplate = new List<Monster>
            {
                spiderTemplate,
                spiderTemplate,
                spiderTemplate
            };

            Location fieldOfWest = new Location(LOCATION_ID_FIELD_OF_WEST, "Западная Поляна", "Поляна к западу от деревни " +
                "тут обитают пауки.", westFieldMonsterTemplate);

            village.LocationToNorth = fieldOfNorth;
            village.LocationToSouth = fieldOfSouth;
            village.LocationToEast = fieldOfEast;
            village.LocationToWest = fieldOfWest;

            fieldOfNorth.LocationToSouth = village;
            
            fieldOfSouth.LocationToNorth = village;

            fieldOfEast.LocationToWest = village;

            fieldOfWest.LocationToEast = village;

            Locations.Add(village);
            Locations.Add(fieldOfNorth);
            Locations.Add(fieldOfSouth);
            Locations.Add(fieldOfEast);
            Locations.Add(fieldOfWest);
        }
        
        private static void PopulateQuests()
        {
            // Квест на охоту на крыс
            Quest ratHunt = new Quest(QUEST_ID_RAT_HUNT, "Охота на крыс",
                "Избавь деревню от надоедливых крыс.Принеси 5 кусков крысиного мяса.", 50, 25);
            ratHunt.QuestItems.Add(new QuestItem(ItemByID(ITEM_ID_RATS_MEAT), 5));
            ratHunt.RewardItems.Add(new InventoryItem(ItemByID(ITEM_ID_WEAK_HEALING_POTION), 5));

            // Квест на паутину
            Quest spiderSilk = new Quest(QUEST_ID_SPIDER_SILK, "Шелк паука",
            "Собери 3 паучьих шелка для местного ремесленника.", 75, 100);
            spiderSilk.QuestItems.Add(new QuestItem(ItemByID(ITEM_ID_SPIDER_SILK), 3));
            spiderSilk.RewardItems.Add(new InventoryItem(ItemByID(ITEM_ID_WEAK_HEALING_POTION), 10));

            Quests.Add(ratHunt);
            Quests.Add(spiderSilk);
        }

        public static Item ItemByID(int id)
        {
            foreach(Item item in Items)
            {
                if (item.ID == id) return item;
            }

            return null;
        }

        public static Monster MonsterByID(int id)
        {
            foreach(Monster monster in Monsters)
            {
                if (monster.ID == id) return monster;
            }

            return null;
        }

        public static Location LocationByID(int id)
        {
            foreach(Location location in Locations)
            {
                if(location.ID == id) return location;
            }

            return null;
        }

        public static Trader TraderByID(int id)
        {
            foreach(Trader trader in Traders)
            {
                if (trader.ID == id) return trader;
            }
            return null;
        }

        public static Quest QuestByID(int id)
        {
            return Quests.FirstOrDefault(q => q.ID == id);
        }
    }
}
