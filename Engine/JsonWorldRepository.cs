// JsonWorldRepository.cs
using Engine.Entities;
using Engine.Quests;
using Engine.World;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Engine
{
    public class JsonWorldRepository : IWorldRepository
    {
        private GameData _gameData;
        private Dictionary<int, Item> _items;
        private Dictionary<int, Monster> _monsters;
        private Dictionary<int, Location> _locations;
        private Dictionary<int, Quest> _quests;
        private Dictionary<int, NPC> _npcs;
        private Dictionary<int, Title> _titles;

        public JsonWorldRepository(string jsonFilePath)
        {
            LoadFromJson(jsonFilePath);
        }



        public void LoadFromJson(string jsonFilePath)
        {
            try
            {
                DebugConsole.Log($"Загрузка JSON из: {jsonFilePath}");
                string json = File.ReadAllText(jsonFilePath);
                DebugConsole.Log($"Размер файла: {json.Length} символов");

                // Проверяем валидность JSON
                if (string.IsNullOrWhiteSpace(json))
                {
                    throw new Exception("JSON файл пустой");
                }

                _gameData = JsonSerializer.Deserialize<GameData>(json);
                DebugConsole.Log("JSON десериализован успешно");

                InitializeFromGameData();
            }
            catch (JsonException ex)
            {
                DebugConsole.Log($"Ошибка JSON: {ex.Message}");
                DebugConsole.Log($"Позиция ошибки: {ex.BytePositionInLine}");
                throw new Exception($"Невалидный JSON файл: {ex.Message}", ex);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Ошибка загрузки: {ex.Message}");
                throw;
            }
        }

        private void InitializeFromGameData()
        {
            DebugConsole.Log("Инициализация данных из JSON...");

            // Инициализируем все словари
            _items = new Dictionary<int, Item>();
            _monsters = new Dictionary<int, Monster>();
            _locations = new Dictionary<int, Location>();
            _quests = new Dictionary<int, Quest>();  // ← ДОБАВЬТЕ ЭТУ СТРОЧКУ
            _npcs = new Dictionary<int, NPC>();
            _titles = new Dictionary<int, Title>();

            // Очищаем дубликаты в предметах
            _items = _gameData.Items
                .GroupBy(item => item.ID)  // Группируем по ID
                .Select(group => group.First())  // Берем первый из каждой группы
                .ToDictionary(item => item.ID, item => CreateItemFromData(item));

            // Загрузка монстров
            _monsters = _gameData.Monsters
                .GroupBy(m => m.ID)
                .Select(g => g.First())
                .ToDictionary(m => m.ID, m => CreateMonsterFromData(m));

            DebugConsole.Log($"Загружено: {_items.Count} предметов, {_monsters.Count} монстров");

            // Загрузка NPC
            _npcs = _gameData.NPCs
                .GroupBy(m => m.ID)
                .Select(g => g.First())
                .ToDictionary(m => m.ID, m => CreateNPCFromData(m));


            // Загрузка квестов
            _quests = _gameData.Quests
                .GroupBy(m => m.ID)
                .Select(g => g.First())
                .ToDictionary(m => m.ID, m => CreateQuestFromData(m));

            DebugConsole.Log($"Загружено: {_npcs.Count} NPC, {_quests.Count} квестов");

            // Загрузка титулов
             _titles = _gameData.Titles
                .GroupBy(m => m.ID)
                .Select(g => g.First())
                .ToDictionary(m => m.ID, m => CreateTitleFromData(m));

            _locations = _gameData.Locations
                .GroupBy(m => m.ID)
                .Select(g => g.First())
                .ToDictionary(m => m.ID, m => CreateLocationFromData(m));

            DebugConsole.Log($"Загружено: {_titles.Count} титулов, {_locations.Count} локаций");

            // Установка связей между локациями
            EstablishLocationConnections();
        }

        private Item CreateItemFromData(ItemData itemData)
        {
            if (itemData.AmountToHeal.HasValue)
            {
                return new HealingItem(
                    itemData.ID,
                    itemData.Name,
                    itemData.NamePlural,
                    itemData.Type,
                    itemData.AmountToHeal.Value,
                    itemData.Price,
                    itemData.Description
                );
            }
            else if (itemData.AttackBonus.HasValue || itemData.DefenceBonus.HasValue ||
                     itemData.AgilityBonus.HasValue || itemData.HealthBonus.HasValue)
            {
                return new Equipment(
                    itemData.ID,
                    itemData.NamePlural,
                    itemData.AttackBonus ?? 0,
                    itemData.DefenceBonus ?? 0,
                    itemData.AgilityBonus ?? 0,
                    itemData.HealthBonus ?? 0,
                    itemData.Type,
                    itemData.Price,
                    itemData.Name,
                    itemData.Description
                );
            }
            else
            {
                return new Item(
                    itemData.ID,
                    itemData.Name,
                    itemData.NamePlural,
                    itemData.Type,
                    itemData.Price,
                    itemData.Description
                );
            }
        }

        private Monster CreateMonsterFromData(MonsterData monsterData)
        {
            var monster = new Monster(
                monsterData.ID,
                monsterData.Name,
                monsterData.Level,
                monsterData.CurrentHP,
                monsterData.MaximumHP,
                monsterData.RewardEXP,
                monsterData.RewardGold,
                monsterData.Attributes
            );

            foreach (var lootItemData in monsterData.LootTable)
            {
                var item = ItemByID(lootItemData.ItemID);
                if (item != null)
                {
                    monster.LootTable.Add(new LootItem(
                        item,
                        lootItemData.DropPercentage,
                        lootItemData.IsUnique
                    ));
                }
            }

            return monster;
        }

        private NPC CreateNPCFromData(NPCData npcData)
        {
            var npc = new NPC(npcData.ID, npcData.Name, npcData.Greeting, this);

            // Добавление квестов
            foreach (var questId in npcData.QuestsToGive)
            {
                var quest = QuestByID(questId);
                if (quest != null)
                {
                    npc.QuestsToGive.Add(quest);
                }
            }

            // Настройка торговца
            if (npcData.Merchant != null)
            {
                var merchant = new Merchant(
                    npcData.Merchant.Name,
                    npcData.Merchant.ShopGreeting,
                    npcData.Merchant.Gold
                );

                foreach (var itemData in npcData.Merchant.ItemsForSale)
                {
                    var item = ItemByID(itemData.ItemID);
                    if (item != null)
                    {
                        merchant.ItemsForSale.Add(new InventoryItem(item, itemData.Quantity));
                    }
                }

                npc.Trader = merchant;
            }

            return npc;
        }

        private Quest CreateQuestFromData(QuestData questData)
        {
            NPC questGiver = null;
            if (questData.QuestGiverID.HasValue)
            {
                questGiver = NPCByID(questData.QuestGiverID.Value);
            }

            Quest quest;

            if (questData.QuestType == "Collectible")
            {
                quest = new CollectibleQuest(
                    questData.ID,
                    questData.Name,
                    questData.Description,
                    questData.RewardEXP,
                    questData.RewardGold,
                    questGiver,
                    new List<QuestItem>(),
                    this
                );

                var collectibleQuest = (CollectibleQuest)quest;
                foreach (var spawnData in questData.SpawnLocations)
                {
                    collectibleQuest.SpawnLocations.Add(new CollectibleSpawn(
                        spawnData.LocationID,
                        spawnData.ItemID,
                        spawnData.Quantity
                    ));
                }
            }
            else
            {
                quest = new Quest(
                    questData.ID,
                    questData.Name,
                    questData.Description,
                    questData.RewardEXP,
                    questData.RewardGold,
                    questGiver
                );
            }

            // Добавление квестовых предметов
            foreach (var questItemData in questData.QuestItems)
            {
                var item = ItemByID(questItemData.ItemID);
                if (item != null)
                {
                    quest.QuestItems.Add(new QuestItem(item, questItemData.Quantity));
                }
            }

            // Добавление наград
            foreach (var rewardItemData in questData.RewardItems)
            {
                var item = ItemByID(rewardItemData.ItemID);
                if (item != null)
                {
                    quest.RewardItems.Add(new InventoryItem(item, rewardItemData.Quantity));
                }
            }

            return quest;
        }

        private Title CreateTitleFromData(TitleData titleData)
        {
            return new Title(
                titleData.ID,
                titleData.Name,
                titleData.Description,
                titleData.RequirementType,
                titleData.RequirementTarget,
                titleData.RequirementAmount,
                titleData.AttackBonus,
                titleData.DefenceBonus,
                titleData.HealthBonus,
                0, 0, // goldBonus, expBonus
                titleData.BonusAgainstType,
                titleData.BonusAgainstAmount
            );
        }

        private Location CreateLocationFromData(LocationData locationData)
        {
            var monsterTemplates = new List<Monster>();
            foreach (var spawnData in locationData.MonsterSpawns)
            {
                var baseMonster = MonsterByID(spawnData.MonsterTemplateID);
                if (baseMonster != null)
                {
                    // Создаем нового монстра с нужным уровнем
                    monsterTemplates.Add(new Monster(baseMonster, spawnData.Level));
                }
            }

            var location = new Location(
                locationData.ID,
                locationData.Name,
                locationData.Description,
                monsterTemplates,
                locationData.ScaleMonstersToPlayerLevel
            );

            // Добавление NPC
            foreach (var npcId in locationData.NPCsHere)
            {
                var npc = NPCByID(npcId);
                if (npc != null)
                {
                    location.NPCsHere.Add(npc);
                }
            }

            return location;
        }

        private void EstablishLocationConnections()
        {
            foreach (var locationData in _gameData.Locations)
            {
                var location = LocationByID(locationData.ID);
                if (location != null)
                {
                    location.LocationToNorth = locationData.LocationToNorth.HasValue ?
                        LocationByID(locationData.LocationToNorth.Value) : null;
                    location.LocationToEast = locationData.LocationToEast.HasValue ?
                        LocationByID(locationData.LocationToEast.Value) : null;
                    location.LocationToSouth = locationData.LocationToSouth.HasValue ?
                        LocationByID(locationData.LocationToSouth.Value) : null;
                    location.LocationToWest = locationData.LocationToWest.HasValue ?
                        LocationByID(locationData.LocationToWest.Value) : null;
                }
            }
        }

        // Реализация методов интерфейса IWorldRepository
        public Item ItemByID(int id) => _items.ContainsKey(id) ? _items[id] : null;
        public Monster MonsterByID(int id) => _monsters.ContainsKey(id) ? _monsters[id] : null;
        public Location LocationByID(int id) => _locations.ContainsKey(id) ? _locations[id] : null;
        public Quest QuestByID(int id) => _quests.ContainsKey(id) ? _quests[id] : null;
        public NPC NPCByID(int id) => _npcs.ContainsKey(id) ? _npcs[id] : null;
        public Title TitleByID(int id) => _titles.ContainsKey(id) ? _titles[id] : null;

        public List<Item> GetAllItems() => _items.Values.ToList();
        public List<Monster> GetAllMonsters() => _monsters.Values.ToList();
        public List<Location> GetAllLocations() => _locations.Values.ToList();
        public List<Quest> GetAllQuests() => _quests.Values.ToList();
        public List<NPC> GetAllNPCs() => _npcs.Values.ToList();
        public List<Title> GetAllTitles() => _titles.Values.ToList();

        public void Initialize()
        {
            // Уже инициализировано в конструкторе
        }
    }
}