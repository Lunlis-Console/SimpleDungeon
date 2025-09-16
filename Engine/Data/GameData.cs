// GameData.cs
using Engine.Core;
using Engine.Entities;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.ComponentModel;
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
        public List<Engine.Quests.EnhancedQuest> Quests { get; set; } = new List<Engine.Quests.EnhancedQuest>();

        [JsonPropertyName("NPCs")]
        public List<NPCData> NPCs { get; set; } = new List<NPCData>();

        [JsonPropertyName("Titles")]
        public List<TitleData> Titles { get; set; } = new List<TitleData>();

        [JsonPropertyName("Dialogues")]
        public List<DialogueData> Dialogues { get; set; } = new List<DialogueData>();

        [JsonPropertyName("Rooms")]
        public List<RoomData> Rooms { get; set; } = new List<RoomData>();

        [JsonPropertyName("RoomEntrances")]
        public List<RoomEntranceData> RoomEntrances { get; set; } = new List<RoomEntranceData>();
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
        public List<int> NPCsHere { get; set; } = new();              // старое, оставить
        public List<NPCSpawnData> NPCSpawns { get; set; } = new();    // новое поле с количеством
        public List<int> MonsterTemplates { get; set; } = new();
        
        // Предметы на земле
        public List<InventoryItemData> GroundItems { get; set; } = new();

        // Входы в помещения
        public List<int> RoomEntrances { get; set; } = new();

        public bool ScaleMonstersToPlayerLevel { get; set; }
        public int? LocationToNorth { get; set; }
        public int? LocationToEast { get; set; }
        public int? LocationToSouth { get; set; }
        public int? LocationToWest { get; set; }
    }

    public class NPCSpawnData
    {
        public int NPCID { get; set; }
        public int Count { get; set; } = 1; // по умолчанию 1 — обратная совместимость
    }


    public class MonsterSpawnData
    {
        public int MonsterTemplateID { get; set; }
        public int Level { get; set; }
        public int SpawnWeight { get; set; }
        public int Count { get; set; } = 1; // по умолчанию 1
    }


    public class NPCData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Greeting { get; set; } = string.Empty;
        public List<int> QuestsToGive { get; set; } = new List<int>();
        public MerchantData Merchant { get; set; } = new MerchantData();
        
        [Obsolete("Используйте DefaultDialogueId вместо GreetingDialogueId. Поле будет удалено в будущих версиях.")]
        public string GreetingDialogueId { get; set; } = null;
        
        public string DefaultDialogueId { get; set; } = null;
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
        public string Type { get; set; } = string.Empty;
        public List<string> Tags { get; set; } = new List<string>();

        [JsonPropertyName("Choices")]
        public List<DialogueChoiceData> Choices { get; set; } = new List<DialogueChoiceData>();
    }

    public class DialogueChoiceData
    {
        public string Text { get; set; } = string.Empty;
        public string NextNodeId { get; set; }
        public string Condition { get; set; } = string.Empty;

        // Для одиночного действия (обратная совместимость)
        public DialogueAction Action { get; set; } = DialogueAction.None;
        public string ActionParameter { get; set; }

        // Для множественных действий
        public List<DialogueActionData> Actions { get; set; } = new List<DialogueActionData>();

        [JsonIgnore]
        public string ActionSummary
        {
            get
            {
                if (Actions != null && Actions.Count > 0)
                    return string.Join(", ", Actions.Select(a => a.ToString()));
                if (Action != DialogueAction.None)
                    return $"{Action} ({ActionParameter})";
                return "";
            }
        }
    }

    // Новый класс для хранения данных действия
    public class DialogueActionData
    {
        public DialogueActionData() { Type = DialogueAction.None; }

        public DialogueAction Type { get; set; }
        public string Parameter { get; set; }

        public override string ToString()
        {
            if (string.IsNullOrEmpty(Parameter))
                return Type.ToString();
            return $"{Type} ({Parameter})";
        }
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

    public class RoomData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int ParentLocationID { get; set; }

        // Содержимое помещения
        public List<int> NPCsHere { get; set; } = new List<int>();
        public List<InventoryItemData> GroundItems { get; set; } = new List<InventoryItemData>();
        public List<MonsterSpawnData> MonsterSpawns { get; set; } = new List<MonsterSpawnData>();
        public List<int> MonsterTemplates { get; set; } = new List<int>();

        // Входы в другие помещения
        public List<int> Entrances { get; set; } = new List<int>();

        // Навигация между помещениями
        public int? RoomToNorth { get; set; }
        public int? RoomToEast { get; set; }
        public int? RoomToSouth { get; set; }
        public int? RoomToWest { get; set; }

        public bool ScaleMonstersToPlayerLevel { get; set; } = true;
    }

    public class RoomEntranceData
    {
        public int ID { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int TargetRoomID { get; set; }
        public int ParentLocationID { get; set; }
        public string EntranceType { get; set; } = "entrance";
        public bool IsLocked { get; set; } = false;
        public string LockDescription { get; set; } = string.Empty;
        public bool RequiresKey { get; set; } = false;
        public int RequiredKeyID { get; set; } = 0;
        public List<int> RequiredItemIDs { get; set; } = new List<int>();
    }

}
