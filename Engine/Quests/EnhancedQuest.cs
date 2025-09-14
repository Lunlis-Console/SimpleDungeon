using Engine.Core;
using Engine.Entities;
using Engine.World;
using Newtonsoft.Json;

namespace Engine.Quests
{
    /// <summary>
    /// Расширенная модель квеста с поддержкой различных условий и состояний
    /// </summary>
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
        [JsonConverter(typeof(QuestConditionConverter))]
        public List<QuestCondition> Conditions { get; set; } = new List<QuestCondition>();

        [JsonProperty("rewards")]
        public QuestRewards Rewards { get; set; } = new QuestRewards();

        [JsonProperty("prerequisites")]
        public List<int> PrerequisiteQuestIDs { get; set; } = new List<int>();

        [JsonProperty("dialogueNodes")]
        public QuestDialogueNodes DialogueNodes { get; set; } = new QuestDialogueNodes();

        // Runtime properties (не сериализуются)
        [JsonIgnore]
        public QuestState State { get; set; } = QuestState.NotStarted;

        [JsonIgnore]
        public NPC QuestGiver { get; set; }

        [JsonIgnore]
        public Action<Player> OnQuestComplete { get; set; }

        [JsonIgnore]
        public Action<Player> OnQuestStart { get; set; }

        public EnhancedQuest()
        {
        }

        public EnhancedQuest(int id, string name, string description, int questGiverID)
        {
            ID = id;
            Name = name;
            Description = description;
            QuestGiverID = questGiverID;
        }

        /// <summary>
        /// Проверяет, можно ли начать квест
        /// </summary>
        public bool CanStart(Player player, QuestLog questLog)
        {
            // Проверяем предварительные условия
            foreach (var prereqID in PrerequisiteQuestIDs)
            {
                if (!questLog.CompletedQuests.Any(q => q.ID == prereqID))
                    return false;
            }

            // Проверяем, что квест еще не взят и не завершен
            return State == QuestState.NotStarted;
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
            bool allCompleted = true;

            foreach (var condition in Conditions)
            {
                condition.UpdateProgress(player, context);
                if (!condition.IsCompleted)
                {
                    allCompleted = false;
                }
            }

            // Обновляем состояние квеста
            if (allCompleted && State == QuestState.InProgress)
            {
                State = QuestState.ReadyToComplete;
            }

            return allCompleted;
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

            var progressTexts = Conditions.Select(c => c.GetProgressText()).ToList();
            return string.Join("\n", progressTexts);
        }

        /// <summary>
        /// Получает процент выполнения квеста
        /// </summary>
        public int GetProgressPercentage()
        {
            if (State == QuestState.NotStarted) return 0;
            if (State == QuestState.Completed) return 100;

            if (Conditions.Count == 0) return 0;

            int totalProgress = 0;
            foreach (var condition in Conditions)
            {
                totalProgress += (int)((double)condition.CurrentProgress / condition.RequiredAmount * 100);
            }

            return totalProgress / Conditions.Count;
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

        [JsonIgnore]
        public Item ItemDetails { get; set; }
    }

    /// <summary>
    /// Узлы диалогов для разных состояний квеста
    /// </summary>
    public class QuestDialogueNodes
    {
        [JsonProperty("offer")]
        public string OfferNodeID { get; set; }

        [JsonProperty("inProgress")]
        public string InProgressNodeID { get; set; }

        [JsonProperty("readyToComplete")]
        public string ReadyToCompleteNodeID { get; set; }

        [JsonProperty("completed")]
        public string CompletedNodeID { get; set; }
    }
}
