// GameData.cs
using Engine.Core;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Engine
{
    public class GameData
    {
        public List<ItemData> Items { get; set; } = new List<ItemData>();
        public List<MonsterData> Monsters { get; set; } = new List<MonsterData>();
        public List<LocationData> Locations { get; set; } = new List<LocationData>();
        public List<QuestData> Quests { get; set; } = new List<QuestData>();
        public List<NPCData> NPCs { get; set; } = new List<NPCData>();
        public List<TitleData> Titles { get; set; } = new List<TitleData>();
    }

    public class ItemData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NamePlural { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public int Price { get; set; }
        public string Description { get; set; } = string.Empty;

        // Для Equipment
        public int? AttackBonus { get; set; }
        public int? DefenceBonus { get; set; }
        public int? AgilityBonus { get; set; }
        public int? HealthBonus { get; set; }

        // Для HealingItem
        public int? AmountToHeal { get; set; }
    }

    public class MonsterData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public int Level { get; set; }
        public int CurrentHP { get; set; }
        public int MaximumHP { get; set; }
        public int RewardEXP { get; set; }
        public int RewardGold { get; set; }
        public Attributes Attributes { get; set; } = new Attributes();
        public List<LootItemData> LootTable { get; set; } = new List<LootItemData>();
    }

    public class LootItemData
    {
        public int ItemID { get; set; }
        public int DropPercentage { get; set; }
        public bool IsUnique { get; set; }
    }

    public class LocationData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public List<MonsterSpawnData> MonsterSpawns { get; set; } = new List<MonsterSpawnData>();
        public List<int> NPCsHere { get; set; } = new List<int>();
        public List<int> MonsterTemplates { get; set; } = new List<int>();
        public bool ScaleMonstersToPlayerLevel { get; set; }
        public int? LocationToNorth { get; set; }
        public int? LocationToEast { get; set; }
        public int? LocationToSouth { get; set; }
        public int? LocationToWest { get; set; }
    }
    public class MonsterSpawnData
    {
        public int MonsterTemplateID { get; set; }
        public int Level { get; set; }
        public int SpawnWeight { get; set; } // Вероятность появления
    }

    public class QuestData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int RewardEXP { get; set; }
        public int RewardGold { get; set; }
        public int? QuestGiverID { get; set; }
        public List<QuestItemData> QuestItems { get; set; } = new List<QuestItemData>();
        public List<InventoryItemData> RewardItems { get; set; } = new List<InventoryItemData>();
        public string QuestType { get; set; } = string.Empty; // "Standard" или "Collectible"
        public List<CollectibleSpawnData> SpawnLocations { get; set; } = new List<CollectibleSpawnData>();
    }

    public class QuestItemData
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }
    }

    public class NPCData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Greeting { get; set; } = string.Empty;
        public List<int> QuestsToGive { get; set; } = new List<int>();
        public MerchantData Merchant { get; set; } = new MerchantData();
    }

    public class MerchantData
    {
        public string Name { get; set; } = string.Empty;
        public string ShopGreeting { get; set; } = string.Empty;
        public int Gold { get; set; }
        public List<InventoryItemData> ItemsForSale { get; set; } = new List<InventoryItemData>();
    }

    public class TitleData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string RequirementType { get; set; } = string.Empty;
        public string RequirementTarget { get; set; } = string.Empty;
        public int RequirementAmount { get; set; }
        public int AttackBonus { get; set; }
        public int DefenceBonus { get; set; }
        public int HealthBonus { get; set; }
        public string BonusAgainstType { get; set; } = string.Empty;
        public int BonusAgainstAmount { get; set; }
    }

    public class InventoryItemData
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }

        // Конструктор по умолчанию (обязателен для JSON)
        public InventoryItemData() { }

        // Конструктор с параметрами
        public InventoryItemData(int itemID, int quantity)
        {
            ItemID = itemID;
            Quantity = quantity;
        }
    }

    public class CollectibleSpawnData
    {
        public int LocationID { get; set; }
        public int ItemID { get; set; }
        public int Quantity { get; set; }
    }
}