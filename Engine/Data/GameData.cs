// GameData.cs
using Engine.Core;
using Engine.Entities;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Engine.Data
{
    public class GameData
    {
        [JsonPropertyName("Items")]
        public List<ItemData> Items { get; set; } = new List<ItemData>();

        [JsonPropertyName("Monsters")]
        public List<MonsterData> Monsters { get; set; } = new List<MonsterData>();

        [JsonPropertyName("Locations")]
        public List<LocationData> Locations { get; set; } = new List<LocationData>();

        [JsonPropertyName("Quests")]
        public List<QuestData> Quests { get; set; } = new List<QuestData>();

        [JsonPropertyName("NPCs")]
        public List<NPCData> NPCs { get; set; } = new List<NPCData>();

        [JsonPropertyName("Titles")]
        public List<TitleData> Titles { get; set; } = new List<TitleData>();

        [JsonPropertyName("Dialogues")]
        public List<DialogueData> Dialogues { get; set; } = new List<DialogueData>();
    }

    public class ItemData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string NamePlural { get; set; } = string.Empty;
        public ItemType Type { get; set; }
        public int Price { get; set; }
        public string Description { get; set; } = string.Empty;

        // Старые поля
        public int? AttackBonus { get; set; }
        public int? DefenceBonus { get; set; }
        public int? AgilityBonus { get; set; }
        public int? HealthBonus { get; set; }
        public int? AmountToHeal { get; set; }

        // Компонентная система
        public List<IItemComponent> Components { get; set; } = new List<IItemComponent>();
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
        public List<MonsterSpawnData> MonsterSpawns { get; set; } = new();
        public List<int> NPCsHere { get; set; } = new();
        public List<int> MonsterTemplates { get; set; } = new();
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
        public int SpawnWeight { get; set; }
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
        public string QuestType { get; set; } = string.Empty;
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
        public string GreetingDialogueId { get; set; } = null;
    }

    public class DialogueData
    {
        [JsonPropertyName("id")]
        public string Id { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        // Стартовая нода (id). Форма и парсеры ожидают это поле.
        [JsonPropertyName("start")]
        public string Start { get; set; } = null;

        [JsonPropertyName("nodes")]
        public List<DialogueNodeData> Nodes { get; set; } = new List<DialogueNodeData>();
    }

    public class DialogueNodeData
    {
        public string Id { get; set; } = string.Empty;
        public string Text { get; set; } = string.Empty;
        public string ParentId { get; set; }

        [JsonPropertyName("Choices")]
        public List<DialogueChoiceData> Choices { get; set; } = new List<DialogueChoiceData>();
    }

    public class DialogueChoiceData
    {
        public string Text { get; set; } = string.Empty;
        public string NextNodeId { get; set; }

        // Для одиночного действия (обратная совместимость)
        public DialogueAction Action { get; set; } = DialogueAction.None;
        public string ActionParameter { get; set; }

        // Для множественных действий
        public List<DialogueActionData> Actions { get; set; } = new List<DialogueActionData>();
    }

    // Новый класс для хранения данных действия
    public class DialogueActionData
    {
        public DialogueAction Type { get; set; }
        public string Parameter { get; set; }
    }

    public class ItemReward
    {
        public int ItemId { get; set; }
        public int Quantity { get; set; }
    }

    // В Engine.Data.cs добавьте:
    public enum DialogueAction
    {
        None,
        GiveItem,
        GiveGold,
        StartQuest,
        CompleteQuest,
        SetFlag,
        StartTrade,
        EndDialogue
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

        public InventoryItemData() { }
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
