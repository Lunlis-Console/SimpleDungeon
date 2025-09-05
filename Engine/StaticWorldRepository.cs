namespace Engine
{
    public class StaticWorldRepository : IWorldRepository
    {
        #region Коллекции игровых объектов

        private readonly List<Item> _items = new List<Item>();
        private readonly List<Monster> _monsters = new List<Monster>();
        private readonly List<Location> _locations = new List<Location>();
        private readonly List<Quest> _quests = new List<Quest>();
        private readonly List<Title> _titles = new List<Title>();
        private readonly List<NPC> _npcs = new List<NPC>();

        #endregion

        public StaticWorldRepository()
        {
            Initialize();
        }

        public void Initialize()
        {
            try
            {
                DebugConsole.Log("DEBUG: World initialization started");

                // 1. Сначала создаем базовые объекты
                PopulateItems();
                DebugConsole.Log("DEBUG: Items populated");

                PopulateMonsters();
                DebugConsole.Log("DEBUG: Monsters populated");

                PopulateTitles();
                DebugConsole.Log("DEBUG: Titles populated");

                // 2. Создаем NPC (без квестов пока)
                PopulateNPC();
                DebugConsole.Log("DEBUG: NPC populated");

                // 3. Создаем квесты (теперь NPC уже существуют)
                PopulateQuests();
                DebugConsole.Log("DEBUG: Quests populated");

                // 4. Создаем локации (все объекты уже созданы)
                PopulateLocations();
                DebugConsole.Log("DEBUG: Locations populated");

                DebugConsole.Log("DEBUG: World initialization completed successfully");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"DEBUG: Initialization failed: {ex}");
                throw;
            }
        }

        #region Методы доступа к данным

        public Item ItemByID(int id) => _items.FirstOrDefault(item => item.ID == id);
        public Monster MonsterByID(int id) => _monsters.FirstOrDefault(monster => monster.ID == id);
        public Location LocationByID(int id) => _locations.FirstOrDefault(location => location.ID == id);
        public Quest QuestByID(int id) => _quests.FirstOrDefault(q => q.ID == id);
        public NPC NPCByID(int id) => _npcs.FirstOrDefault(n => n.ID == id);
        public Title TitleByID(int id) => _titles.FirstOrDefault(t => t.ID == id);

        public List<Item> GetAllItems() => _items.ToList();
        public List<Monster> GetAllMonsters() => _monsters.ToList();
        public List<Location> GetAllLocations() => _locations.ToList();
        public List<Quest> GetAllQuests() => _quests.ToList();
        public List<NPC> GetAllNPCs() => _npcs.ToList();
        public List<Title> GetAllTitles() => _titles.ToList();

        #endregion

        #region Методы заполнения данных

        #region Заполнение предметов

        private void PopulateItems()
        {
            PopulateConsumables();
            PopulateMaterials();
            PopulateWeapons();
            PopulateArmor();
            PopulateJewelry();
            PopulateQuestItems();
        }

        private void PopulateConsumables()
        {
            _items.Add(new HealingItem(Constants.ITEM_ID_RATS_MEAT, "Крысиное мясо", "Крысиное мясо",
                ItemType.Consumable, 5, 5, "Data/Descriptions/rat_meat.txt"));

            _items.Add(new HealingItem(Constants.ITEM_ID_WEAK_HEALING_POTION, "Слабое зелье лечения", "",
                ItemType.Consumable, 25, 10));
        }

        private void PopulateWeapons()
        {
            AddWeapon(Constants.ITEM_ID_RUSTY_SWORD, "Ржавый меч", 10, 10, ItemType.OneHandedWeapon);
            AddWeapon(Constants.ITEM_ID_IRON_SWORD, "Железный меч", 15, 10, ItemType.OneHandedWeapon);
            AddWeapon(Constants.ITEM_ID_IRON_GREATSWORD, "Железный двуручник", 25, 100, ItemType.TwoHandedWeapon);
        }

        private void PopulateArmor()
        {
            // Щиты
            AddArmor(Constants.ITEM_ID_IRON_SHIELD, "Железный щит", 10, 50, ItemType.OffHand);

            // Железная броня
            AddArmor(Constants.ITEM_ID_IRON_HELMET, "Железный шлем", 5, 50, ItemType.Helmet);
            AddArmor(Constants.ITEM_ID_IRON_ARMOR, "Железная броня", 10, 100, ItemType.Armor);
            AddArmor(Constants.ITEM_ID_IRON_GLOVES, "Железные перчатки", 2, 40, ItemType.Gloves);
            AddArmor(Constants.ITEM_ID_IRON_BOOTS, "Железные сапоги", 2, 40, ItemType.Boots);

            // Кожаная броня
            AddArmor(Constants.ITEM_ID_LEATHER_HELMET, "Кожаный шлем", 1, 10, ItemType.Helmet);
            AddArmor(Constants.ITEM_ID_LEATHER_ARMOR, "Кожаная броня", 2, 10, ItemType.Armor);
            AddArmor(Constants.ITEM_ID_LEATHER_GLOVES, "Кожаные перчатки", 1, 10, ItemType.Gloves);
            AddArmor(Constants.ITEM_ID_LEATHER_BOOTS, "Кожаные сапоги", 1, 10, ItemType.Boots);
        }

        private void PopulateJewelry()
        {
            _items.Add(new Equipment(Constants.ITEM_ID_FAMILY_RING, "", 1, 1, 1, 1,
                ItemType.Ring, 100, "Фамильное кольцо"));

            _items.Add(new Equipment(Constants.ITEM_ID_GOLD_RING, "", 0, 2, 0, 0,
                ItemType.Ring, 50, "Золотое кольцо"));

            _items.Add(new Equipment(Constants.ITEM_ID_RICH_AMULET, "", 0, 0, 0, 10,
                ItemType.Amulet, 50, "Дорогое ожерелье"));
        }

        private void PopulateMaterials()
        {
            _items.Add(new Item(Constants.ITEM_ID_SPIDER_SILK, "Паучий шелк", "",
                ItemType.Stuff, 10, ""));
        }

        private void PopulateQuestItems()
        {
            // Создаем ящики как служебные предметы для квестов
            _items.Add(new Item(Constants.ITEM_ID_CRATE, "Ящик с товарами", "Потерянный ящик торговца",
                ItemType.Quest, 0, ""));

            _items.Add(new Item(Constants.ITEM_ID_CRATE2, "Ящик с инструментами", "Потерянный ящик торговца",
                ItemType.Quest, 0, ""));

            _items.Add(new Item(Constants.ITEM_ID_CRATE3, "Ящик с припасами", "Потерянный ящик торговца",
                ItemType.Quest, 0, ""));

            _items.Add(new Item(Constants.ITEM_ID_CRATE4, "Ящик с украшениями", "Потерянный ящик торговца",
                ItemType.Quest, 0, ""));
        }

        #endregion

        #region Заполнение монстров

        private void PopulateMonsters()
        {
            // Крыса
            Monster rat = new Monster(1, "Крыса", 1, 10, 10, 5, 5,
                new Attributes(
                    strength: 8,
                    constitution: 6,
                    dexterity: 12,
                    intelligence: 2,
                    wisdom: 4,
                    charisma: 3
                    ));
            rat.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_GOLD_RING), 25, false));
            rat.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_RATS_MEAT), 100, false));

            // Паук
            Monster spider = new Monster(Constants.MONSTER_ID_SPIDER, "Паук", 1, 25, 25, 10, 5,
                new Attributes(
                    strength: 12,
                    constitution: 10,
                    dexterity: 10,
                    intelligence: 6,
                    wisdom: 5,
                    charisma: 4
                    ));
            spider.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_RICH_AMULET), 25, false));
            spider.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_SPIDER_SILK), 60, false));

            // Матерая крыса
            Monster olderRat = new Monster(Constants.MONSTER_ID_OLDER_RAT, "Матерая крыса", 1, 30, 30, 20, 10,
                new Attributes(
                    strength: 12,
                    constitution: 10,
                    dexterity: 10,
                    intelligence: 6,
                    wisdom: 5,
                    charisma: 4
                    ));
            olderRat.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_FAMILY_RING), 25, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_RATS_MEAT), 100, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_RATS_MEAT), 100, false));
            olderRat.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_RATS_MEAT), 100, false));

            // Матерый паук
            Monster olderSpider = new Monster(Constants.MONSTER_ID_OLDER_SPIDER, "Матерый паук", 1, 50, 50, 40, 25,
                new Attributes(
                    strength: 12,
                    constitution: 10,
                    dexterity: 10,
                    intelligence: 6,
                    wisdom: 5,
                    charisma: 4
                    ));
            olderSpider.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_RICH_AMULET), 25, false));
            olderSpider.LootTable.Add(new LootItem(ItemByID(Constants.ITEM_ID_SPIDER_SILK), 60, false));

            _monsters.Add(rat);
            _monsters.Add(spider);
            _monsters.Add(olderRat);
            _monsters.Add(olderSpider);
        }

        #endregion

        #region Заполнение NPC

        private void PopulateNPC()
        {
            // Создаем торговца
            var merchant = new Merchant("Купец Зарубий", "Добро пожаловать в мою лавку! Есть чем поторговать?", 1500);
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_WEAK_HEALING_POTION), 25));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_SWORD), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_LEATHER_HELMET), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_LEATHER_ARMOR), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_SHIELD), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_HELMET), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_ARMOR), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_GLOVES), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_BOOTS), 1));
            merchant.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_GREATSWORD), 1));

            // Создаем NPC
            NPC villageTrader = new NPC(Constants.NPC_ID_VILLAGE_TRADER, "Купец Зарубий",
                "Добро пожаловать в нашу деревню! Есть чем поторговать?");
            villageTrader.Trader = merchant;

            NPC villageElder = new NPC(Constants.NPC_ID_VILLAGE_ELDER, "Староста деревня",
                "Добро пожаловать в нашу деревню, путник! Нам нужна твоя помощь.");

            NPC craftsman = new NPC(Constants.NPC_ID_VILLAGE_CRAFTSMAN, "Ремесленник",
                "Приветствую! Ищу качественные материалы для своих изделий.");

            NPC hunter = new NPC(Constants.NPC_ID_VILLAGE_HUNTER, "Охотник",
                "Эй, ищешь работу? У меня есть пара заданий для смельчака.");

            // Добавляем NPC в общий список
            _npcs.Add(villageTrader);
            _npcs.Add(villageElder);
            _npcs.Add(craftsman);
            _npcs.Add(hunter);
        }

        #endregion

        #region Заполнение локаций

        private void PopulateLocations()
        {
            // Получаем NPC из общего списка
            NPC villageTrader = NPCByID(Constants.NPC_ID_VILLAGE_TRADER);
            NPC villageElder = NPCByID(Constants.NPC_ID_VILLAGE_ELDER);
            NPC craftsman = NPCByID(Constants.NPC_ID_VILLAGE_CRAFTSMAN);
            NPC hunter = NPCByID(Constants.NPC_ID_VILLAGE_HUNTER);

            // Шаблоны монстров
            Monster ratTemplate = MonsterByID(Constants.MONSTER_ID_RAT);
            Monster spiderTemplate = MonsterByID(Constants.MONSTER_ID_SPIDER);
            Monster olderSpiderTemplate = MonsterByID(Constants.MONSTER_ID_OLDER_SPIDER);
            Monster olderRatTemplate = MonsterByID(Constants.MONSTER_ID_OLDER_RAT);

            // Создание локаций
            Location village = new Location(Constants.LOCATION_ID_VILLAGE, "Деревня", "Здесь вы родились, тут безопасно.");

            // Добавление NPC в деревню
            if (villageTrader != null) village.NPCsHere.Add(villageTrader);
            if (villageElder != null) village.NPCsHere.Add(villageElder);
            if (craftsman != null) village.NPCsHere.Add(craftsman);
            if (hunter != null) village.NPCsHere.Add(hunter);

            // Северная поляна (крысы)
            List<Monster> northFieldMonsterTemplate = new List<Monster>
            {
                ratTemplate, ratTemplate, ratTemplate
            };
            Location fieldOfNorth = new Location(Constants.LOCATION_ID_FIELD_OF_NORTH, "Северная Поляна",
                "Поляна к северу от деревни, тут обитают крысы.", northFieldMonsterTemplate);

            // Южная поляна (крысы + матерая крыса)
            List<Monster> southFieldMonsterTemplate = new List<Monster>
            {
                ratTemplate, ratTemplate, ratTemplate, olderRatTemplate
            };
            Location fieldOfSouth = new Location(Constants.LOCATION_ID_FIELD_OF_SOUTH, "Южная Поляна",
                "Поляна к югу от деревни, тут обитают крысы.", southFieldMonsterTemplate);

            // Восточная поляна (пауки)
            List<Monster> eastFieldMonsterTemplate = new List<Monster>
            {
                spiderTemplate, spiderTemplate, spiderTemplate
            };
            Location fieldOfEast = new Location(Constants.LOCATION_ID_FIELD_OF_EAST, "Восточная Поляна",
                "Поляна к востоку от деревни, тут обитают пауки.", eastFieldMonsterTemplate);

            // Западная поляна (пауки + матерый паук)
            List<Monster> westFieldMonsterTemplate = new List<Monster>
            {
                spiderTemplate, spiderTemplate, spiderTemplate, olderSpiderTemplate
            };
            Location fieldOfWest = new Location(Constants.LOCATION_ID_FIELD_OF_WEST, "Западная Поляна",
                "Поляна к западу от деревни, тут обитают пауки.", westFieldMonsterTemplate);

            // Настройка связей между локациями
            village.LocationToNorth = fieldOfNorth;
            village.LocationToSouth = fieldOfSouth;
            village.LocationToEast = fieldOfEast;
            village.LocationToWest = fieldOfWest;

            fieldOfNorth.LocationToSouth = village;
            fieldOfSouth.LocationToNorth = village;
            fieldOfEast.LocationToWest = village;
            fieldOfWest.LocationToEast = village;

            // Добавление локаций в мир
            _locations.Add(village);
            _locations.Add(fieldOfNorth);
            _locations.Add(fieldOfSouth);
            _locations.Add(fieldOfEast);
            _locations.Add(fieldOfWest);
        }

        #endregion

        #region Заполнение квестов

        private void PopulateQuests()
        {
            // Получаем NPC из общего списка
            NPC villageElder = NPCByID(Constants.NPC_ID_VILLAGE_ELDER);
            NPC craftsman = NPCByID(Constants.NPC_ID_VILLAGE_CRAFTSMAN);
            NPC villageTrader = NPCByID(Constants.NPC_ID_VILLAGE_TRADER);

            if (villageElder == null || craftsman == null || villageTrader == null)
            {
                DebugConsole.Log("ERROR: NPC for quests are null!");
                return;
            }

            // Квест на охоту на крыс
            Quest ratHunt = new Quest(Constants.QUEST_ID_RAT_HUNT, "Охота на крыс",
                "Избавь деревню от надоедливых крыс. Принеси 5 кусков крысиного мяса.",
                50, 25, villageElder);

            ratHunt.QuestItems.Add(new QuestItem(ItemByID(Constants.ITEM_ID_RATS_MEAT), 5));
            ratHunt.RewardItems.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_WEAK_HEALING_POTION), 5));
            villageElder.QuestsToGive = new List<Quest> { ratHunt };

            // Квест на паутину
            Quest spiderSilk = new Quest(Constants.QUEST_ID_SPIDER_SILK, "Шелк паука",
                "Собери 3 паучьих шелка для местного ремесленника.",
                75, 100, craftsman);

            spiderSilk.QuestItems.Add(new QuestItem(ItemByID(Constants.ITEM_ID_SPIDER_SILK), 3));
            spiderSilk.RewardItems.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_WEAK_HEALING_POTION), 10));
            craftsman.QuestsToGive = new List<Quest> { spiderSilk };

            // Квест на сбор ящиков
            var crateQuest = new CollectibleQuest(
                Constants.QUEST_ID_LOST_CRATES,
                "Потерянные ящики",
                "Торговец потерял 4 ящика с товарами. Помоги найти их!",
                100, 200, villageTrader,
                new List<QuestItem>
                {
                    new QuestItem(ItemByID(Constants.ITEM_ID_CRATE), 1),
                    new QuestItem(ItemByID(Constants.ITEM_ID_CRATE2), 1),
                    new QuestItem(ItemByID(Constants.ITEM_ID_CRATE3), 1),
                    new QuestItem(ItemByID(Constants.ITEM_ID_CRATE4), 1)
                },
                this // если обращаться к репозиторию, будет StackOverFlow
            );

            crateQuest.SpawnLocations.AddRange(new[]
            {
                new CollectibleSpawn(Constants.LOCATION_ID_FIELD_OF_NORTH, Constants.ITEM_ID_CRATE),
                new CollectibleSpawn(Constants.LOCATION_ID_FIELD_OF_SOUTH, Constants.ITEM_ID_CRATE2),
                new CollectibleSpawn(Constants.LOCATION_ID_FIELD_OF_EAST, Constants.ITEM_ID_CRATE3),
                new CollectibleSpawn(Constants.LOCATION_ID_FIELD_OF_WEST, Constants.ITEM_ID_CRATE4)
            });

            crateQuest.OnQuestComplete = (player) =>
            {
                var questTrader = NPCByID(Constants.NPC_ID_VILLAGE_TRADER);
                if (questTrader != null && questTrader.Trader != null)
                {
                    questTrader.Trader.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_IRON_GREATSWORD), 1));
                    questTrader.Trader.ItemsForSale.Add(new InventoryItem(ItemByID(Constants.ITEM_ID_RICH_AMULET), 1));
                    questTrader.Trader.Gold += 500;
                    MessageSystem.AddMessage("Ассортимент торговца расширился!");
                }
            };

            villageTrader.QuestsToGive = new List<Quest> { crateQuest };

            // Добавляем квесты в общий список
            _quests.Add(ratHunt);
            _quests.Add(spiderSilk);
            _quests.Add(crateQuest);

            //DebugConsole.Log($"Trader quests: {villageTrader.QuestsToGive?.Count ?? 0}");
        }

        #endregion

        #region Заполнение титулов

        private void PopulateTitles()
        {
            // Истребитель крыс - бонус против крыс
            _titles.Add(new Title(Constants.TITLE_ID_RAT_SLAYER, "Истребитель Крыс",
                "Убийца 50 крыс", "MonsterKill", "Rat", 50,
                bonusAgainstType: "Rat", bonusAgainstAmount: 25));

            // Охотник на пауков - бонус против пауков
            _titles.Add(new Title(Constants.TITLE_ID_SPIDER_HUNTER, "Охотник на Пауков",
                "Убийца 30 пауков", "MonsterKill", "Spider", 30,
                bonusAgainstType: "Spider", bonusAgainstAmount: 20));

            // Опытный искатель приключений - общий бонус
            _titles.Add(new Title(Constants.TITLE_ID_EXPERIENCED_ADVENTURER, "Опытный Искатель Приключений",
                "Убийца 100 монстров", "TotalMonstersKilled", "", 100,
                attackBonus: 2, defenceBonus: 2, healthBonus: 10));
        }

        #endregion

        #endregion

        #region Вспомогательные методы для создания предметов

        private void AddWeapon(int id, string name, int attack, int price, ItemType type = ItemType.OneHandedWeapon)
        {
            _items.Add(new Equipment(id, "", attack, 0, 10, 0, type, price, name));
        }

        private void AddArmor(int id, string name, int defence, int price, ItemType type)
        {
            _items.Add(new Equipment(id, "", 0, defence, 0, 0, type, price, name));
        }

        #endregion
    }
}