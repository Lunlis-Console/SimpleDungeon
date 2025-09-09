// JsonWorldRepository.cs
using Engine.Core;
using Engine.Data;
using Engine.Entities;
using Engine.Quests;
using Engine.Titles;
using Engine.Trading;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.World
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

        private readonly JsonSerializerOptions _jsonOptions;

        public JsonWorldRepository(string jsonFilePath)
        {
            if (string.IsNullOrWhiteSpace(jsonFilePath))
                throw new ArgumentException("jsonFilePath is null or empty", nameof(jsonFilePath));

            _jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
            _jsonOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
            _jsonOptions.Converters.Add(new ItemComponentConverter());
            _jsonOptions.Converters.Add(new ItemTypeConverter());

            LoadFromJson(jsonFilePath);
        }

        // --- подробный лог дублей
        private void LogDuplicatesVerbose(GameData data)
        {
            if (data == null) return;

            void LogFor<T>(IEnumerable<T> list, string typeName)
            {
                if (list == null) return;
                var idProp = typeof(T).GetProperty("ID") ?? typeof(T).GetProperty("Id");
                var nameProp = typeof(T).GetProperty("Name") ?? typeof(T).GetProperty("Title") ?? typeof(T).GetProperty("Id");
                if (idProp == null) return;

                var groups = list.Cast<object>()
                                 .GroupBy(x => (int)idProp.GetValue(x))
                                 .Where(g => g.Count() > 1);

                foreach (var g in groups)
                {
                    var id = g.Key;
                    var names = g.Select(x =>
                    {
                        if (nameProp != null) return nameProp.GetValue(x)?.ToString() ?? "<no-name>";
                        return x.ToString();
                    });
                    DebugConsole.Log($"Duplicate {typeName} ID={id}: {string.Join(", ", names)}");
                }
            }

            LogFor(data.Items, "Item");
            LogFor(data.Monsters, "Monster");
            LogFor(data.NPCs, "NPC");
            LogFor(data.Locations, "Location");
            LogFor(data.Quests, "Quest");
            LogFor(data.Titles, "Title");
        }

        public void LoadFromJson(string jsonFilePath)
        {
            try
            {
                DebugConsole.Log($"[LoadFromJson] START. Path: {jsonFilePath}");
                Console.WriteLine($"[LoadFromJson] START. Path: {jsonFilePath}");

                if (!File.Exists(jsonFilePath))
                {
                    DebugConsole.Log($"[LoadFromJson] File not found: {jsonFilePath}");
                    Console.WriteLine($"[LoadFromJson] File not found: {jsonFilePath}");
                    throw new FileNotFoundException($"JSON файл не найден: {jsonFilePath}");
                }

                string json;
                try
                {
                    json = File.ReadAllText(jsonFilePath);
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"[LoadFromJson] Ошибка чтения файла: {ex.Message}");
                    Console.WriteLine($"[LoadFromJson] Ошибка чтения файла: {ex}");
                    throw;
                }

                DebugConsole.Log($"[LoadFromJson] Размер файла: {json?.Length ?? 0} символов");
                Console.WriteLine($"[LoadFromJson] Размер файла: {json?.Length ?? 0} символов");

                if (string.IsNullOrWhiteSpace(json))
                {
                    DebugConsole.Log("[LoadFromJson] JSON пустой");
                    Console.WriteLine("[LoadFromJson] JSON пустой");
                    throw new Exception("JSON файл пустой");
                }

                // Десериализация
                try
                {
                    _gameData = JsonSerializer.Deserialize<GameData>(json, _jsonOptions) ?? new GameData();
                    DebugConsole.Log("[LoadFromJson] JSON десериализован успешно");
                    Console.WriteLine("[LoadFromJson] JSON десериализован успешно");
                    DebugConsole.Log($"[LoadFromJson] Counts: Items={_gameData?.Items?.Count ?? 0}, Monsters={_gameData?.Monsters?.Count ?? 0}, NPCs={_gameData?.NPCs?.Count ?? 0}, Quests={_gameData?.Quests?.Count ?? 0}, Locations={_gameData?.Locations?.Count ?? 0}");
                    Console.WriteLine($"[LoadFromJson] Counts: Items={_gameData?.Items?.Count ?? 0}, Monsters={_gameData?.Monsters?.Count ?? 0}, NPCs={_gameData?.NPCs?.Count ?? 0}, Quests={_gameData?.Quests?.Count ?? 0}, Locations={_gameData?.Locations?.Count ?? 0}");
                }
                catch (JsonException jex)
                {
                    DebugConsole.Log($"[LoadFromJson] JsonException: {jex.Message} at {jex.BytePositionInLine}");
                    Console.WriteLine($"[LoadFromJson] JsonException: {jex}");
                    throw;
                }

                // Валидация уникальности ID
                var errors = UniqueIdHelper.ValidateUniqueIds(_gameData);
                DebugConsole.Log($"[LoadFromJson] UniqueIdHelper returned {errors.Count} errors");
                Console.WriteLine($"[LoadFromJson] UniqueIdHelper returned {errors.Count} errors");

                if (errors.Any())
                {
                    // подробный лог дублей
                    DebugConsole.Log("[LoadFromJson] ПОДРОБНЫЕ ДУБЛИ (если есть):");
                    Console.WriteLine("[LoadFromJson] ПОДРОБНЫЕ ДУБЛИ (если есть):");
                    LogDuplicatesVerbose(_gameData);

                    DebugConsole.Log("[LoadFromJson] Errors: " + string.Join(" | ", errors));
                    Console.WriteLine("[LoadFromJson] Errors: " + string.Join(" | ", errors));

#if DEBUG
                    DebugConsole.Log("[LoadFromJson] DEBUG mode: attempting auto-fix of duplicates...");
                    Console.WriteLine("[LoadFromJson] DEBUG mode: attempting auto-fix of duplicates...");

                    var mappingPerType = UniqueIdHelper.FixDuplicateIds(_gameData);
                    DebugConsole.Log("[LoadFromJson] Auto-fixed mapping:\n" + FormatMapping(mappingPerType));
                    Console.WriteLine("[LoadFromJson] Auto-fixed mapping:\n" + FormatMapping(mappingPerType));

                    // Создаем бэкап и записываем исправленный файл
                    try
                    {
                        var bak = jsonFilePath + ".bak";
                        File.Copy(jsonFilePath, bak, overwrite: true);
                        DebugConsole.Log($"[LoadFromJson] Backup created: {bak}");
                        Console.WriteLine($"[LoadFromJson] Backup created: {bak}");

                        var newJson = JsonSerializer.Serialize(_gameData, _jsonOptions);
                        File.WriteAllText(jsonFilePath, newJson);
                        DebugConsole.Log($"[LoadFromJson] Fixed GameData saved to: {jsonFilePath}");
                        Console.WriteLine($"[LoadFromJson] Fixed GameData saved to: {jsonFilePath}");
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log($"[LoadFromJson] Failed to save fixed GameData: {ex.Message}");
                        Console.WriteLine($"[LoadFromJson] Failed to save fixed GameData: {ex}");
                    }
#else
            // В релизе — fail fast, чтобы не запускать игру с неконсистентными данными
            DebugConsole.Log("[LoadFromJson] RELEASE mode: throwing due to duplicate IDs");
            Console.WriteLine("[LoadFromJson] RELEASE mode: throwing due to duplicate IDs");
            throw new Exception("Duplicate IDs found in game data. Please fix data using the editor.");
#endif
                }
                else
                {
                    DebugConsole.Log("[LoadFromJson] UniqueIdHelper found no duplicates");
                    Console.WriteLine("[LoadFromJson] UniqueIdHelper found no duplicates");
                }

                // Продолжаем инициализацию runtime-структур
                try
                {
                    DebugConsole.Log("[LoadFromJson] Calling InitializeFromGameData...");
                    Console.WriteLine("[LoadFromJson] Calling InitializeFromGameData...");
                    InitializeFromGameData();
                    DebugConsole.Log("[LoadFromJson] InitializeFromGameData completed");
                    Console.WriteLine("[LoadFromJson] InitializeFromGameData completed");
                    SaveMigratedGameData(_gameData, jsonFilePath);
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"[LoadFromJson] Ошибка в InitializeFromGameData: {ex.Message}");
                    Console.WriteLine($"[LoadFromJson] Ошибка в InitializeFromGameData: {ex}");
                    throw;
                }

                DebugConsole.Log("[LoadFromJson] END");
                Console.WriteLine("[LoadFromJson] END");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"[LoadFromJson] Unhandled exception: {ex.Message}");
                Console.WriteLine($"[LoadFromJson] Unhandled exception: {ex}");
                throw;
            }
        }

        // Вставь этот приватный метод в класс JsonWorldRepository
        private void SaveMigratedGameData(GameData gameData, string path)
        {
            if (gameData == null || string.IsNullOrWhiteSpace(path)) return;

            try
            {
                // Создаём бэкап один раз (если .bak ещё нет)
                string backupPath = path + ".bak";
                if (!File.Exists(backupPath))
                {
                    File.Copy(path, backupPath, overwrite: false);
                    DebugConsole.Log($"[LoadFromJson] Backup saved to {backupPath}");
                    Console.WriteLine($"[LoadFromJson] Backup saved to {backupPath}");
                }

                // Используем существующие опции сериализации если они есть (_jsonOptions),
                // иначе создаём минимальные (главное — добавить ItemComponentConverter)
                JsonSerializerOptions optionsToUse = _jsonOptions ?? new JsonSerializerOptions { WriteIndented = true };

                // Убедимся, что конвертер для компонентов добавлен
                bool hasConverter = optionsToUse.Converters.Any(c => c is ItemComponentConverter);
                if (!hasConverter)
                {
                    optionsToUse.Converters.Add(new ItemComponentConverter());
                }

                // Сериализуем и перезапишем файл
                string outJson = JsonSerializer.Serialize(gameData, optionsToUse);
                File.WriteAllText(path, outJson);

                DebugConsole.Log($"[LoadFromJson] Migrated game_data.json saved to {path}");
                Console.WriteLine($"[LoadFromJson] Migrated game_data.json saved to {path}");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"[LoadFromJson] Failed to save migrated game_data.json: {ex.Message}");
                Console.WriteLine($"[LoadFromJson] Failed to save migrated game_data.json: {ex.Message}");
            }
        }


        private string FormatMapping(Dictionary<string, Dictionary<int, int>> mapping)
        {
            if (mapping == null || mapping.Count == 0) return "(no mapping)";

            var sb = new StringBuilder();
            foreach (var typeKv in mapping)
            {
                sb.AppendLine($"{typeKv.Key}:");
                if (typeKv.Value == null || typeKv.Value.Count == 0)
                {
                    sb.AppendLine("  (no changes)");
                    continue;
                }

                foreach (var kv in typeKv.Value)
                {
                    sb.AppendLine($"  {kv.Key} -> {kv.Value}");
                }
            }
            return sb.ToString();
        }

        private void InitializeFromGameData()
        {
            DebugConsole.Log("Инициализация данных из JSON...");

            // Инициализируем все словари
            _items = new Dictionary<int, Item>();
            _monsters = new Dictionary<int, Monster>();
            _locations = new Dictionary<int, Location>();
            _quests = new Dictionary<int, Quest>();
            _npcs = new Dictionary<int, NPC>();
            _titles = new Dictionary<int, Title>();

            // Items
            if (_gameData.Items != null)
            {
                _items = _gameData.Items
                    .GroupBy(item => item.ID)
                    .Select(group => group.First())
                    .ToDictionary(item => item.ID, item => CreateItemFromData(item));
            }

            // Monsters
            if (_gameData.Monsters != null)
            {
                _monsters = _gameData.Monsters
                    .GroupBy(m => m.ID)
                    .Select(g => g.First())
                    .ToDictionary(m => m.ID, m => CreateMonsterFromData(m));
            }

            DebugConsole.Log($"Загружено: {_items.Count} предметов, {_monsters.Count} монстров");

            // NPCs
            if (_gameData.NPCs != null)
            {
                _npcs = _gameData.NPCs
                    .GroupBy(m => m.ID)
                    .Select(g => g.First())
                    .ToDictionary(m => m.ID, m => CreateNPCFromData(m));
            }

            // Quests
            if (_gameData.Quests != null)
            {
                _quests = _gameData.Quests
                    .GroupBy(m => m.ID)
                    .Select(g => g.First())
                    .ToDictionary(m => m.ID, m => CreateQuestFromData(m));
            }

            DebugConsole.Log($"Загружено: {_npcs.Count} NPC, {_quests.Count} квестов");

            // Titles
            if (_gameData.Titles != null)
            {
                _titles = _gameData.Titles
                    .GroupBy(m => m.ID)
                    .Select(g => g.First())
                    .ToDictionary(m => m.ID, m => CreateTitleFromData(m));
            }

            // Locations (после того как NPCs/Monsters созданы)
            if (_gameData.Locations != null)
            {
                _locations = _gameData.Locations
                    .GroupBy(m => m.ID)
                    .Select(g => g.First())
                    .ToDictionary(m => m.ID, m => CreateLocationFromData(m));
            }

            DebugConsole.Log($"Загружено: {_titles.Count} титулов, {_locations.Count} локаций");

            // Установка связей между локациями
            EstablishLocationConnections();
        }

        private Item CreateItemFromData(ItemData itemData)
        {
            if (itemData == null) return null;

            // --- 1) Миграция legacy-полей в компоненты (если Components пусты) ---
            if (itemData.Components == null)
                itemData.Components = new List<IItemComponent>();

            bool hasLegacyBonuses =
                (itemData.AttackBonus.HasValue && itemData.AttackBonus.Value != 0) ||
                (itemData.DefenceBonus.HasValue && itemData.DefenceBonus.Value != 0) ||
                (itemData.AgilityBonus.HasValue && itemData.AgilityBonus.Value != 0) ||
                (itemData.HealthBonus.HasValue && itemData.HealthBonus.Value != 0);

            if ((itemData.Components.Count == 0) && hasLegacyBonuses)
            {
                // Мигрируем все бонусы в один EquipComponent (сохраняя старые поля)
                var migratedEquip = new EquipComponent
                {
                    Slot = itemData.Type.ToString(),
                    AttackBonus = itemData.AttackBonus ?? 0,
                    DefenceBonus = itemData.DefenceBonus ?? 0,
                    AgilityBonus = itemData.AgilityBonus ?? 0,
                    HealthBonus = itemData.HealthBonus ?? 0
                };

                itemData.Components.Add(migratedEquip);
                DebugConsole.Log($"[CreateItemFromData] Migrated legacy bonuses -> EquipComponent for Item ID={itemData.ID}");
            }

            if ((itemData.Components.Count == 0) && itemData.AmountToHeal.HasValue && itemData.AmountToHeal.Value != 0)
            {
                itemData.Components.Add(new HealComponent { HealAmount = itemData.AmountToHeal.Value });
                DebugConsole.Log($"[CreateItemFromData] Migrated AmountToHeal -> HealComponent for Item ID={itemData.ID}");
            }

            // --- 2) Если после миграции есть компоненты — обрабатываем их в приоритетном порядке ---
            if (itemData.Components != null && itemData.Components.Count > 0)
            {
                // Если есть EquipComponent -> создаём Equipment (рантайм-класс)
                var equipComp = itemData.Components.OfType<EquipComponent>().FirstOrDefault();
                if (equipComp != null)
                {
                    DebugConsole.Log($"[CreateItemFromData] Creating Equipment from EquipComponent for Item ID={itemData.ID} ({itemData.Name})");
                    return new Equipment(
                        itemData.ID,
                        string.IsNullOrWhiteSpace(itemData.NamePlural) ? itemData.Name : itemData.NamePlural, // namePlural param in constructor
                        equipComp.AttackBonus,
                        equipComp.DefenceBonus,
                        equipComp.AgilityBonus,
                        equipComp.HealthBonus,
                        itemData.Type,
                        itemData.Price,
                        itemData.Name,
                        itemData.Description
                    );
                }

                // Если есть HealComponent -> создаём HealingItem
                var healComp = itemData.Components.OfType<HealComponent>().FirstOrDefault();
                if (healComp != null)
                {
                    DebugConsole.Log($"[CreateItemFromData] Creating HealingItem from HealComponent for Item ID={itemData.ID} ({itemData.Name})");
                    return new HealingItem(
                        itemData.ID,
                        itemData.Name,
                        itemData.NamePlural,
                        itemData.Type,
                        healComp.HealAmount,
                        itemData.Price,
                        itemData.Description
                    );
                }

                // Другие компоненты: по-умолчанию создаём CompositeItem и копируем компоненты туда
                var compItem = new CompositeItem(
                    itemData.ID,
                    itemData.Name,
                    itemData.NamePlural,
                    itemData.Type,
                    itemData.Price,
                    itemData.Description
                );

                foreach (var c in itemData.Components)
                    compItem.Components.Add(c);

                DebugConsole.Log($"[CreateItemFromData] Created CompositeItem with {itemData.Components.Count} components for Item ID={itemData.ID}");
                return compItem;
            }

            // --- 3) Если компонентов нет — fallback на старую логику (legacy fields) ---
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
            if (monsterData == null) return null;

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

            if (monsterData.LootTable != null)
            {
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
            }

            return monster;
        }

        private NPC CreateNPCFromData(NPCData npcData)
        {
            if (npcData == null) return null;

            var npc = new NPC(npcData.ID, npcData.Name, npcData.Greeting, this);

            // Добавление квестов (если список присутствует)
            if (npcData.QuestsToGive != null)
            {
                foreach (var questId in npcData.QuestsToGive)
                {
                    var quest = QuestByID(questId);
                    if (quest != null)
                    {
                        npc.QuestsToGive.Add(quest);
                    }
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

                if (npcData.Merchant.ItemsForSale != null)
                {
                    foreach (var itemData in npcData.Merchant.ItemsForSale)
                    {
                        var item = ItemByID(itemData.ItemID);
                        if (item != null)
                        {
                            merchant.ItemsForSale.Add(new InventoryItem(item, itemData.Quantity));
                        }
                    }
                }

                npc.Trader = merchant;
            }

            return npc;
        }

        private Quest CreateQuestFromData(QuestData questData)
        {
            if (questData == null) return null;

            NPC questGiver = null;
            if (questData.QuestGiverID.HasValue)
            {
                questGiver = NPCByID(questData.QuestGiverID.Value);
            }

            Quest quest;

            if (string.Equals(questData.QuestType, "Collectible", StringComparison.OrdinalIgnoreCase))
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
                if (questData.SpawnLocations != null)
                {
                    foreach (var spawnData in questData.SpawnLocations)
                    {
                        collectibleQuest.SpawnLocations.Add(new CollectibleSpawn(
                            spawnData.LocationID,
                            spawnData.ItemID,
                            spawnData.Quantity
                        ));
                    }
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
            if (questData.QuestItems != null)
            {
                foreach (var questItemData in questData.QuestItems)
                {
                    var item = ItemByID(questItemData.ItemID);
                    if (item != null)
                    {
                        quest.QuestItems.Add(new QuestItem(item, questItemData.Quantity));
                    }
                }
            }

            // Добавление наград
            if (questData.RewardItems != null)
            {
                foreach (var rewardItemData in questData.RewardItems)
                {
                    var item = ItemByID(rewardItemData.ItemID);
                    if (item != null)
                    {
                        quest.RewardItems.Add(new InventoryItem(item, rewardItemData.Quantity));
                    }
                }
            }

            return quest;
        }

        private Title CreateTitleFromData(TitleData titleData)
        {
            if (titleData == null) return null;

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
            if (locationData == null) return null;

            var monsterTemplates = new List<Monster>();
            if (locationData.MonsterSpawns != null)
            {
                foreach (var spawnData in locationData.MonsterSpawns)
                {
                    var baseMonster = MonsterByID(spawnData.MonsterTemplateID);
                    if (baseMonster != null)
                    {
                        // Создаем нового монстра с нужным уровнем
                        monsterTemplates.Add(new Monster(baseMonster, spawnData.Level));
                    }
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
            if (locationData.NPCsHere != null)
            {
                foreach (var npcId in locationData.NPCsHere)
                {
                    var npc = NPCByID(npcId);
                    if (npc != null)
                    {
                        location.NPCsHere.Add(npc);
                    }
                }
            }

            return location;
        }

        private void EstablishLocationConnections()
        {
            if (_gameData?.Locations == null) return;

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
        public Item ItemByID(int id) => _items != null && _items.ContainsKey(id) ? _items[id] : null;
        public Monster MonsterByID(int id) => _monsters != null && _monsters.ContainsKey(id) ? _monsters[id] : null;
        public Location LocationByID(int id) => _locations != null && _locations.ContainsKey(id) ? _locations[id] : null;
        public Quest QuestByID(int id) => _quests != null && _quests.ContainsKey(id) ? _quests[id] : null;
        public NPC NPCByID(int id) => _npcs != null && _npcs.ContainsKey(id) ? _npcs[id] : null;
        public Title TitleByID(int id) => _titles != null && _titles.ContainsKey(id) ? _titles[id] : null;

        public List<Item> GetAllItems() => _items != null ? _items.Values.ToList() : new List<Item>();
        public List<Monster> GetAllMonsters() => _monsters != null ? _monsters.Values.ToList() : new List<Monster>();
        public List<Location> GetAllLocations() => _locations != null ? _locations.Values.ToList() : new List<Location>();
        public List<Quest> GetAllQuests() => _quests != null ? _quests.Values.ToList() : new List<Quest>();
        public List<NPC> GetAllNPCs() => _npcs != null ? _npcs.Values.ToList() : new List<NPC>();
        public List<Title> GetAllTitles() => _titles != null ? _titles.Values.ToList() : new List<Title>();

        public void Initialize()
        {
            // Уже инициализировано в конструкторе / LoadFromJson
        }
    }
}
