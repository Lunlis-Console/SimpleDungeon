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
        public const int ITEM_ID_LEATHER_HELMET = 6;
        public const int ITEM_ID_LEATHER_ARMOR = 7;
        public const int ITEM_ID_LEATHER_GLOVES = 8;
        public const int ITEM_ID_LEATHER_BOOTS = 9;
        public const int ITEM_ID_FAMILY_RING = 10;
        public const int ITEM_ID_GOLD_RING = 11;
        public const int ITEM_ID_RICH_AMULET = 12;
        public const int ITEM_ID_IRON_GREATSWORD = 13;
        public const int ITEM_ID_IRON_SHIELD = 14;
        public const int ITEM_ID_IRON_HELMET = 15;
        public const int ITEM_ID_IRON_ARMOR = 16;
        public const int ITEM_ID_IRON_GLOVES = 17;
        public const int ITEM_ID_IRON_BOOTS = 18;

        public const int NPC_ID_VILLAGE_TRADER = 1;
        public const int NPC_ID_VILLAGE_ELDER = 2;
        public const int NPC_ID_VILLAGE_CRAFTSMAN = 3;
        public const int NPC_ID_VILLAGE_HUNTER = 4;

        public const int QUEST_ID_RAT_HUNT = 1;
        public const int QUEST_ID_SPIDER_SILK = 2;

        public const int MONSTER_ID_RAT = 1;
        public const int MONSTER_ID_SPIDER = 2;
        public const int MONSTER_ID_OLDER_RAT = 3;
        public const int MONSTER_ID_OLDER_SPIDER = 4;

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
            PopulateConsumables();
            PopulateMaterials();
            PopulateWeapons();
            PopulateArmor();
            PopulateJewelry();
        }

        private static void PopulateConsumables()
        {
            Items.Add(new HealingItem(ITEM_ID_RATS_MEAT, "Крысиное мясо", "Крысиное мясо", ItemType.Consumable, 5, 5,
                "Data/Descriptions/rat_meat.txt"));
            Items.Add(new HealingItem(ITEM_ID_WEAK_HEALING_POTION, "Слабое зелье лечения", "", ItemType.Consumable, 25, 10));
        }
        private static void PopulateWeapons()
        {
            AddWeapon(ITEM_ID_RUSTY_SWORD, "Ржавый меч", 10, 10, ItemType.OneHandedWeapon);
            AddWeapon(ITEM_ID_IRON_SWORD, "Железный меч", 15, 10, ItemType.OneHandedWeapon);

            AddWeapon(ITEM_ID_IRON_GREATSWORD, "Железный двуручник", 25, 100,ItemType.TwoHandedWeapon);
        }
        

        private static void PopulateArmor()
        {
            AddArmor(ITEM_ID_IRON_SHIELD, "Желеный щит", 10, 50, ItemType.OffHand);

            AddArmor(ITEM_ID_IRON_HELMET, "Железный шлем", 5, 50, ItemType.Helmet);
            AddArmor(ITEM_ID_IRON_ARMOR, "Железная броня", 10, 100, ItemType.Armor);
            AddArmor(ITEM_ID_IRON_GLOVES, "Железные перчатки", 2, 40, ItemType.Gloves);
            AddArmor(ITEM_ID_IRON_BOOTS, "Железные перчатки", 2, 40, ItemType.Boots);

            AddArmor(ITEM_ID_LEATHER_HELMET, "Кожаный шлем", 1, 10, ItemType.Helmet);
            AddArmor(ITEM_ID_LEATHER_ARMOR, "Кожаная броня", 2, 10, ItemType.Armor);
            AddArmor(ITEM_ID_LEATHER_GLOVES, "Кожаные перчатки", 1, 10, ItemType.Gloves);
            AddArmor(ITEM_ID_LEATHER_BOOTS, "Кожаные сапоги", 1, 10, ItemType.Boots);
        }
        private static void PopulateJewelry()
        {
            Items.Add(new Equipment(ITEM_ID_FAMILY_RING, "", 1, 1, 1, 1, ItemType.Ring, 100, "Фамильное кольцо"));
            Items.Add(new Equipment(ITEM_ID_GOLD_RING, "", 0, 2, 0, 0, ItemType.Ring, 50, "Золотое кольцо"));

            Items.Add(new Equipment(ITEM_ID_RICH_AMULET, "", 0, 0, 0, 10, ItemType.Amulet, 50, "Дорогое ожерелье"));
        }
        private static void PopulateMaterials()
        {
            Items.Add(new Item(ITEM_ID_SPIDER_SILK, "Паучий шелк", "", ItemType.Stuff, 10, ""));

        }

        private static void PopulateMonsters()
        {
            Monster rat = new Monster(MONSTER_ID_RAT, "Крыса", 1, 10, 10, 5, 5, 5, 5, 20);
            rat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_GOLD_RING), 25, false));
            rat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));

            Monster spider = new Monster(MONSTER_ID_SPIDER, "Паук", 1, 25, 25, 10, 5, 10, 10, 15);
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RICH_AMULET), 25, false));
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_SPIDER_SILK), 60, false));

            Monster olderRat = new Monster(MONSTER_ID_OLDER_RAT, "Матерая крыса", 1, 30, 30, 20, 10, 25, 25, 20);
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_FAMILY_RING), 25, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));

            Monster olderSpider = new Monster(MONSTER_ID_OLDER_SPIDER, "Матерый паук", 1, 50, 50, 40, 25, 50, 50, 15);
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RICH_AMULET), 25, false));
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_SPIDER_SILK), 60, false));

            Monsters.Add(rat);
            Monsters.Add(spider);
            Monsters.Add(olderRat);
            Monsters.Add(olderSpider);
        }

        private static void PopulateLocations()
        {
            
            List<InventoryItem> villageTraderInventory = new List<InventoryItem>();
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_WEAK_HEALING_POTION), 25));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_SWORD), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_LEATHER_HELMET), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_LEATHER_ARMOR), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_SHIELD), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_HELMET), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_ARMOR), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_GLOVES), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_BOOTS), 1));
            villageTraderInventory.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_GREATSWORD), 1));

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
            Monster olderSpiderTemplate = MonsterByID(MONSTER_ID_OLDER_SPIDER);
            Monster olderRatTemplate = MonsterByID(MONSTER_ID_OLDER_RAT);

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
                ratTemplate,
                olderRatTemplate
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
                spiderTemplate,
                olderSpiderTemplate
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

        private static void AddWeapon(int id, string name, int attack, int price, ItemType type = ItemType.OneHandedWeapon)
        {
            Items.Add(new Equipment(id, "", attack, 0, 10, 0, type, price, name));
        }
        private static void AddArmor(int id, string name, int defence, int price, ItemType type)
        {
            Items.Add(new Equipment(id, "", 0, defence, 0, 0, type, price, name));
        }
    }
}
