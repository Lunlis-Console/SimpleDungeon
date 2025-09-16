using Newtonsoft.Json;

namespace Engine.Quests
{
    /// <summary>
    /// DTO для данных условия квеста (только данные, без логики)
    /// </summary>
    [Serializable]
    public class QuestConditionData
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("type")]
        public string Type { get; set; } // "CollectItems", "KillMonsters", etc.

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("requiredAmount")]
        public int RequiredAmount { get; set; }

        [JsonProperty("targetId")]
        public int TargetID { get; set; } // ID предмета, монстра, NPC или локации

        [JsonProperty("currentProgress")]
        public int CurrentProgress { get; set; }

        [JsonProperty("isCompleted")]
        public bool IsCompleted { get; set; }

        public QuestConditionData() { }

        // Конструкторы для удобства создания из различных условий
        public QuestConditionData(CollectItemsCondition condition)
        {
            ID = condition.ID;
            Type = "CollectItems";
            Description = condition.Description;
            RequiredAmount = condition.RequiredAmount;
            TargetID = condition.ItemID;
            CurrentProgress = condition.CurrentProgress;
            IsCompleted = condition.IsCompleted;
        }

        public QuestConditionData(KillMonstersCondition condition)
        {
            ID = condition.ID;
            Type = "KillMonsters";
            Description = condition.Description;
            RequiredAmount = condition.RequiredAmount;
            TargetID = condition.MonsterID;
            CurrentProgress = condition.CurrentProgress;
            IsCompleted = condition.IsCompleted;
        }

        public QuestConditionData(VisitLocationCondition condition)
        {
            ID = condition.ID;
            Type = "VisitLocation";
            Description = condition.Description;
            RequiredAmount = condition.RequiredAmount;
            TargetID = condition.LocationID;
            CurrentProgress = condition.CurrentProgress;
            IsCompleted = condition.IsCompleted;
        }

        public QuestConditionData(TalkToNPCCondition condition)
        {
            ID = condition.ID;
            Type = "TalkToNPC";
            Description = condition.Description;
            RequiredAmount = condition.RequiredAmount;
            TargetID = condition.NPCID;
            CurrentProgress = condition.CurrentProgress;
            IsCompleted = condition.IsCompleted;
        }

        public QuestConditionData(ReachLevelCondition condition)
        {
            ID = condition.ID;
            Type = "ReachLevel";
            Description = condition.Description;
            RequiredAmount = condition.RequiredAmount;
            TargetID = condition.RequiredLevel;
            CurrentProgress = condition.CurrentProgress;
            IsCompleted = condition.IsCompleted;
        }

        /// <summary>
        /// Преобразует DTO обратно в объект условия
        /// </summary>
        public QuestCondition ToQuestCondition()
        {
            return Type switch
            {
                "CollectItems" => new CollectItemsCondition
                {
                    ID = ID,
                    Description = Description,
                    RequiredAmount = RequiredAmount,
                    ItemID = TargetID,
                    CurrentProgress = CurrentProgress
                },
                "KillMonsters" => new KillMonstersCondition
                {
                    ID = ID,
                    Description = Description,
                    RequiredAmount = RequiredAmount,
                    MonsterID = TargetID,
                    CurrentProgress = CurrentProgress
                },
                "VisitLocation" => new VisitLocationCondition
                {
                    ID = ID,
                    Description = Description,
                    RequiredAmount = RequiredAmount,
                    LocationID = TargetID,
                    CurrentProgress = CurrentProgress
                },
                "TalkToNPC" => new TalkToNPCCondition
                {
                    ID = ID,
                    Description = Description,
                    NPCID = TargetID,
                    CurrentProgress = CurrentProgress
                },
                "ReachLevel" => new ReachLevelCondition
                {
                    ID = ID,
                    Description = Description,
                    RequiredLevel = TargetID,
                    CurrentProgress = CurrentProgress
                },
                _ => throw new ArgumentException($"Unknown condition type: {Type}")
            };
        }
    }
}