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
        public static readonly List<Title> Titles = new List<Title>();

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
        public const int ITEM_ID_CRATE = 19;
        public const int ITEM_ID_CRATE2 = 20;
        public const int ITEM_ID_CRATE3 = 21;
        public const int ITEM_ID_CRATE4 = 22;

        public const int NPC_ID_VILLAGE_TRADER = 1;
        public const int NPC_ID_VILLAGE_ELDER = 2;
        public const int NPC_ID_VILLAGE_CRAFTSMAN = 3;
        public const int NPC_ID_VILLAGE_HUNTER = 4;

        public const int QUEST_ID_RAT_HUNT = 1;
        public const int QUEST_ID_SPIDER_SILK = 2;
        public const int QUEST_ID_LOST_CRATES = 3;

        public const int MONSTER_ID_RAT = 1;
        public const int MONSTER_ID_SPIDER = 2;
        public const int MONSTER_ID_OLDER_RAT = 3;
        public const int MONSTER_ID_OLDER_SPIDER = 4;

        public const int LOCATION_ID_VILLAGE = 1;
        public const int LOCATION_ID_FIELD_OF_NORTH = 2;
        public const int LOCATION_ID_FIELD_OF_SOUTH = 3;
        public const int LOCATION_ID_FIELD_OF_EAST = 4;
        public const int LOCATION_ID_FIELD_OF_WEST = 5;

        public const int TITLE_ID_RAT_SLAYER = 1;
        public const int TITLE_ID_SPIDER_HUNTER = 2;
        public const int TITLE_ID_EXPERIENCED_ADVENTURER = 3;

        static World()
        {
            PopulateItems();
            PopulateMonsters();
            PopulateQuests();
            PopulateLocations();
            PopulateTitles();



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
            Monster rat = new Monster(1, "Крыса", 1, 10, 10, 5, 5,
                new Attributes(strength: 8, constitution: 6, dexterity: 12,
                      intelligence: 2, wisdom: 4, charisma: 3));
            rat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_GOLD_RING), 25, false));
            rat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));

            Monster goblin = new Monster(2, "Гоблин", 2, 20, 20, 15, 10,
                new Attributes(strength: 12, constitution: 10, dexterity: 10,
                      intelligence: 6, wisdom: 5, charisma: 4));


            Monster spider = new Monster(MONSTER_ID_SPIDER, "Паук", 1, 25, 25, 10, 5,
                new Attributes(strength: 12, constitution: 10, dexterity: 10,
                      intelligence: 6, wisdom: 5, charisma: 4));
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RICH_AMULET), 25, false));
            spider.LootTable.Add(new LootItem(ItemByID(ITEM_ID_SPIDER_SILK), 60, false));

            Monster olderRat = new Monster(MONSTER_ID_OLDER_RAT, "Матерая крыса", 1, 30, 30, 20, 10, 
                new Attributes(strength: 12, constitution: 10, dexterity: 10,
                      intelligence: 6, wisdom: 5, charisma: 4));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_FAMILY_RING), 25, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(ITEM_ID_RATS_MEAT), 100, false));

            Monster olderSpider = new Monster(MONSTER_ID_OLDER_SPIDER, "Матерый паук", 1, 50, 50, 40, 25, 
                new Attributes(strength: 12, constitution: 10, dexterity: 10,
                      intelligence: 6, wisdom: 5, charisma: 4));
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

            // Получаем NPC из квестов (они уже созданы в PopulateQuests с квестами)
            NPC villageElder = Quests.First(q => q.ID == QUEST_ID_RAT_HUNT).QuestGiver;
            NPC craftsman = Quests.First(q => q.ID == QUEST_ID_SPIDER_SILK).QuestGiver;

            // Обновляем приветствия для NPC из квестов
            villageElder.Greeting = "Добро пожаловать в нашу деревню, путник! Нам нужна твоя помощь.";
            craftsman.Greeting = "Приветствую! Ищу качественные материалы для своих изделий.";

            // 3. Охотник - может давать оба квеста (создаем нового, т.к. он не дает квестов)
            NPC hunter = new NPC(NPC_ID_VILLAGE_HUNTER, "Охотник",
                "Эй, ищешь работу? У меня есть пара заданий для смельчака.");

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
            // Сначала создаем NPC (локально)
            NPC villageElder = new NPC(NPC_ID_VILLAGE_ELDER, "Старейшина", "Приветствие");
            NPC craftsman = new NPC(NPC_ID_VILLAGE_CRAFTSMAN, "Ремесленник", "Приветствие");

            // Квест на охоту на крыс
            Quest ratHunt = new Quest(QUEST_ID_RAT_HUNT, "Охота на крыс",
                "Избавь деревню от надоедливых крыс. Принеси 5 кусков крысиного мяса.", 50, 25, villageElder);
            ratHunt.QuestItems.Add(new QuestItem(ItemByID(ITEM_ID_RATS_MEAT), 5));
            ratHunt.RewardItems.Add(new InventoryItem(ItemByID(ITEM_ID_WEAK_HEALING_POTION), 5));
            villageElder.AddQuest(ratHunt);

            // Квест на паутину
            Quest spiderSilk = new Quest(QUEST_ID_SPIDER_SILK, "Шелк паука",
                "Собери 3 паучьих шелка для местного ремесленника.", 75, 100, craftsman);
            spiderSilk.QuestItems.Add(new QuestItem(ItemByID(ITEM_ID_SPIDER_SILK), 3));
            spiderSilk.RewardItems.Add(new InventoryItem(ItemByID(ITEM_ID_WEAK_HEALING_POTION), 10));
            craftsman.AddQuest(spiderSilk);


            // Квест на сбор ящиков для торговца
            var trader = (Trader)NPCByID(NPC_ID_VILLAGE_TRADER);

            var crateQuest = new CollectibleQuest(
                QUEST_ID_LOST_CRATES,
                "Потерянные ящики",
                "Торговец потерял 4 ящика с товарами. Помоги найти их!",
                100, 200, trader,
                new List<QuestItem>
                {
            new QuestItem(ItemByID(ITEM_ID_CRATE), 1),
            new QuestItem(ItemByID(ITEM_ID_CRATE2), 1),
            new QuestItem(ItemByID(ITEM_ID_CRATE3), 1),
            new QuestItem(ItemByID(ITEM_ID_CRATE4), 1)
                }
            );

            // Настраиваем места спавна
            crateQuest.SpawnLocations.AddRange(new[]
            {
                new CollectibleSpawn(LOCATION_ID_FIELD_OF_NORTH, ITEM_ID_CRATE),
                new CollectibleSpawn(LOCATION_ID_FIELD_OF_SOUTH, ITEM_ID_CRATE2),
                new CollectibleSpawn(LOCATION_ID_FIELD_OF_EAST, ITEM_ID_CRATE3),
                new CollectibleSpawn(LOCATION_ID_FIELD_OF_WEST, ITEM_ID_CRATE4)
            });

            // ДОБАВЛЯЕМ КОЛБЭК ЗДЕСЬ - после создания квеста
            crateQuest.OnQuestComplete = (player) =>
            {
                var questTrader = (Trader)crateQuest.QuestGiver;

                // Расширяем ассортимент
                questTrader.ItemsForSale.Add(new InventoryItem(ItemByID(ITEM_ID_IRON_GREATSWORD), 1));
                questTrader.ItemsForSale.Add(new InventoryItem(ItemByID(ITEM_ID_RICH_AMULET), 1));
                questTrader.Gold += 500;

                MessageSystem.AddMessage("Ассортимент торговца расширился!");
            };

            trader.AddQuest(crateQuest);
            Quests.Add(crateQuest);

            Quests.Add(ratHunt);
            Quests.Add(spiderSilk);
        }

        private static void PopulateTitles()
        {
            // Истребитель крыс - бонус против крыс
            Titles.Add(new Title(TITLE_ID_RAT_SLAYER, "Истребитель Крыс",
                "Убийца 50 крыс", "MonsterKill", "Rat", 50,
                bonusAgainstType: "Rat", bonusAgainstAmount: 25));

            // Охотник на пауков - бонус против пауков
            Titles.Add(new Title(TITLE_ID_SPIDER_HUNTER, "Охотник на Пауков",
                "Убийца 30 пауков", "MonsterKill", "Spider", 30,
                bonusAgainstType: "Spider", bonusAgainstAmount: 20));

            // Опытный искатель приключений - общий бонус
            Titles.Add(new Title(TITLE_ID_EXPERIENCED_ADVENTURER, "Опытный Искатель Приключений",
                "Убийца 100 монстров", "TotalMonstersKilled", "", 100,
                attackBonus: 2, defenceBonus: 2, healthBonus: 10));
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

        public static NPC NPCByID(int id)
        {
            // Ищем NPC среди всех NPC в локациях
            foreach (var location in Locations)
            {
                var npc = location.NPCsHere.FirstOrDefault(n => n.ID == id);
                if (npc != null) return npc;
            }
            return null;
        }

        public static Title TitleByID(int id)
        {
            return Titles.FirstOrDefault(t => t.ID == id);
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
