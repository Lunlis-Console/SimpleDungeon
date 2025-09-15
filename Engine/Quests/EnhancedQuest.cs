using Engine.Core;
using Engine.Data;
using Engine.Entities;
using Engine.World;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

namespace Engine.Quests
{
    /// <summary>
    /// Расширенная модель квеста с поддержкой различных условий и состояний
    /// </summary>
    [Serializable]
    public class EnhancedQuest
    {
        [JsonProperty("id")]
        public int ID { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("description")]
        public string Description { get; set; }

        [JsonProperty("detailedDescription")]
        public string DetailedDescription { get; set; }

        [JsonProperty("questGiverId")]
        public int QuestGiverID { get; set; }

        [JsonProperty("conditions")]
        public List<QuestConditionData> Conditions { get; set; } = new List<QuestConditionData>();

        [JsonProperty("rewards")]
        public QuestRewards Rewards { get; set; } = new QuestRewards();

        [JsonProperty("prerequisites")]
        public List<int> PrerequisiteQuestIDs { get; set; } = new List<int>();

        [JsonProperty("dialogueNodes")]
        public QuestDialogueNodes DialogueNodes { get; set; } = new QuestDialogueNodes();

        public EnhancedQuest()
        {
            DebugConsole.Log("EnhancedQuest constructor called");
            DialogueNodes = new QuestDialogueNodes();
        }

        // Runtime properties (не сериализуются)
        [Newtonsoft.Json.JsonIgnore]
        public QuestState State { get; set; } = QuestState.NotStarted;

        [Newtonsoft.Json.JsonIgnore]
        public NPC QuestGiver { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Action<Player> OnQuestComplete { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Action<Player> OnQuestStart { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        private List<QuestCondition> _runtimeConditions = new List<QuestCondition>();


        public EnhancedQuest(int id, string name, string description, int questGiverID)
        {
            ID = id;
            Name = name;
            Description = description;
            QuestGiverID = questGiverID;
        }

        /// <summary>
        /// Инициализирует runtime условия из DTO данных
        /// </summary>
        public void InitializeConditions()
        {
            DebugConsole.Log($"EnhancedQuest.InitializeConditions: Quest {ID} ({Name}), Conditions count: {Conditions?.Count ?? 0}");
            
            if (Conditions == null)
            {
                _runtimeConditions = new List<QuestCondition>();
                DebugConsole.Log($"EnhancedQuest.InitializeConditions: Quest {ID} - No conditions, created empty list");
            }
            else
            {
                _runtimeConditions = Conditions.Select(c => c.ToQuestCondition()).ToList();
                DebugConsole.Log($"EnhancedQuest.InitializeConditions: Quest {ID} - Created {_runtimeConditions.Count} runtime conditions");
                
                foreach (var condition in _runtimeConditions)
                {
                    DebugConsole.Log($"  Runtime condition {condition.ID}: {condition.Description}, Required: {condition.RequiredAmount}, Current: {condition.CurrentProgress}");
                }
            }
        }

        /// <summary>
        /// Проверяет, можно ли начать квест
        /// </summary>
        public bool CanStart(Player player, QuestLog questLog)
        {
            DebugConsole.Log($"EnhancedQuest.CanStart: Quest {ID} ({Name}), State: {State}");
            
            // Проверяем предварительные условия
            foreach (var prereqID in PrerequisiteQuestIDs)
            {
                if (!questLog.CompletedQuests.Any(q => q.ID == prereqID))
                {
                    DebugConsole.Log($"  Cannot start: Prerequisite quest {prereqID} not completed");
                    return false;
                }
            }

            // Проверяем, что квест еще не взят и не завершен
            bool canStart = State == QuestState.NotStarted;
            DebugConsole.Log($"  Can start: {canStart} (State: {State})");
            
            return canStart;
        }

        /// <summary>
        /// Начинает квест
        /// </summary>
        public void StartQuest(Player player)
        {
            if (State != QuestState.NotStarted) return;

            State = QuestState.InProgress;
            OnQuestStart?.Invoke(player);
            MessageSystem.AddMessage($"Получен новый квест: {Name}");
        }

        /// <summary>
        /// Проверяет выполнение всех условий квеста
        /// </summary>
        public bool CheckAllConditions(Player player, object context = null)
        {
            DebugConsole.Log($"EnhancedQuest.CheckAllConditions: Quest {ID} ({Name}), State: {State}, Context: {context?.GetType().Name ?? "null"}");
            
            bool allCompleted = true;

            foreach (var condition in _runtimeConditions)
            {
                DebugConsole.Log($"  Checking condition {condition.ID}: {condition.Description}, Current: {condition.CurrentProgress}/{condition.RequiredAmount}, Completed: {condition.IsCompleted}");
                
                condition.UpdateProgress(player, context);
                
                DebugConsole.Log($"  After update: Current: {condition.CurrentProgress}/{condition.RequiredAmount}, Completed: {condition.IsCompleted}");
                
                if (!condition.IsCompleted)
                {
                    allCompleted = false;
                }
            }

            // Обновляем DTO данные
            UpdateConditionData();

            // Обновляем состояние квеста
            if (allCompleted && State == QuestState.InProgress)
            {
                DebugConsole.Log($"EnhancedQuest.CheckAllConditions: Quest {ID} transitioning from InProgress to ReadyToComplete");
                State = QuestState.ReadyToComplete;
            }

            DebugConsole.Log($"EnhancedQuest.CheckAllConditions: Quest {ID} final state: {State}, allCompleted: {allCompleted}");
            return allCompleted;
        }

        /// <summary>
        /// Обновляет DTO данные из runtime условий
        /// </summary>
        private void UpdateConditionData()
        {
            if (_runtimeConditions == null)
            {
                Conditions = new List<QuestConditionData>();
            }
            else
            {
                Conditions = _runtimeConditions.Select(c =>
                {
                    return c switch
                    {
                        CollectItemsCondition collect => new QuestConditionData(collect),
                        KillMonstersCondition kill => new QuestConditionData(kill),
                        VisitLocationCondition visit => new QuestConditionData(visit),
                        TalkToNPCCondition talk => new QuestConditionData(talk),
                        ReachLevelCondition level => new QuestConditionData(level),
                        _ => throw new ArgumentException($"Unknown condition type: {c.GetType().Name}")
                    };
                }).ToList();
            }
        }

        /// <summary>
        /// Принудительно обновляет прогресс определенного условия
        /// </summary>
        public void UpdateConditionProgress(int conditionId, int progress)
        {
            var condition = _runtimeConditions.FirstOrDefault(c => c.ID == conditionId);
            if (condition != null)
            {
                condition.CurrentProgress = Math.Min(progress, condition.RequiredAmount);
                UpdateConditionData();
            }
        }

        /// <summary>
        /// Добавляет прогресс определенному условию
        /// </summary>
        public void AddConditionProgress(int conditionId, int amount = 1)
        {
            var condition = _runtimeConditions.FirstOrDefault(c => c.ID == conditionId);
            if (condition != null)
            {
                condition.CurrentProgress = Math.Min(condition.CurrentProgress + amount, condition.RequiredAmount);
                UpdateConditionData();
            }
        }

        /// <summary>
        /// Завершает квест
        /// </summary>
        public void CompleteQuest(Player player)
        {
            if (State != QuestState.ReadyToComplete) return;

            // Выдаем награды
            Rewards.GiveRewards(player);

            State = QuestState.Completed;
            OnQuestComplete?.Invoke(player);
            MessageSystem.AddMessage($"Квест '{Name}' завершен!");
        }

        /// <summary>
        /// Получает текст прогресса квеста
        /// </summary>
        public string GetProgressText()
        {
            if (State == QuestState.NotStarted)
                return "Не начат";

            if (State == QuestState.Completed)
                return "Завершен";

            var progressTexts = _runtimeConditions.Select(c => c.GetProgressText()).ToList();
            return string.Join("\n", progressTexts);
        }

        /// <summary>
        /// Получает процент выполнения квеста
        /// </summary>
        public int GetProgressPercentage()
        {
            if (State == QuestState.NotStarted) return 0;
            if (State == QuestState.Completed) return 100;

            if (_runtimeConditions.Count == 0) return 0;

            int totalProgress = 0;
            foreach (var condition in _runtimeConditions)
            {
                totalProgress += (int)((double)condition.CurrentProgress / condition.RequiredAmount * 100);
            }

            return totalProgress / _runtimeConditions.Count;
        }

        /// <summary>
        /// Получает runtime условие по ID
        /// </summary>
        public QuestCondition GetRuntimeCondition(int conditionId)
        {
            return _runtimeConditions.FirstOrDefault(c => c.ID == conditionId);
        }

        /// <summary>
        /// Получает все runtime условия
        /// </summary>
        public List<QuestCondition> GetRuntimeConditions()
        {
            return _runtimeConditions.ToList();
        }
    }

    /// <summary>
    /// Награды за квест
    /// </summary>
    public class QuestRewards
    {
        [JsonProperty("experience")]
        public int Experience { get; set; }

        [JsonProperty("gold")]
        public int Gold { get; set; }

        [JsonProperty("items")]
        public List<QuestRewardItem> Items { get; set; } = new List<QuestRewardItem>();

        public void GiveRewards(Player player)
        {
            player.Gold += Gold;
            player.CurrentEXP += Experience;
            player.QuestsCompleted++;

            foreach (var rewardItem in Items)
            {
                player.AddItemToInventory(rewardItem.ItemDetails, rewardItem.Quantity);
            }
        }
    }

    /// <summary>
    /// Предмет-награда за квест
    /// </summary>
    public class QuestRewardItem
    {
        [JsonProperty("itemId")]
        public int ItemID { get; set; }

        [JsonProperty("quantity")]
        public int Quantity { get; set; }

        [Newtonsoft.Json.JsonIgnore]
        public Item ItemDetails { get; set; }
    }

    /// <summary>
    /// Узлы диалогов для разных состояний квеста
    /// </summary>
    public class QuestDialogueNodes
    {
        [JsonPropertyName("offer")]
        public string Offer { get; set; }

        [JsonPropertyName("inProgress")]
        public string InProgress { get; set; }

        [JsonPropertyName("readyToComplete")]
        public string ReadyToComplete { get; set; }

        [JsonPropertyName("completed")]
        public string Completed { get; set; }

        public QuestDialogueNodes()
        {
            DebugConsole.Log("QuestDialogueNodes constructor called");
        }

        public override string ToString()
        {
            return $"Offer='{Offer}', InProgress='{InProgress}', ReadyToComplete='{ReadyToComplete}', Completed='{Completed}'";
        }
    }
}