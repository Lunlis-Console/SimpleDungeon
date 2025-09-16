using Engine.Entities;
using Engine.World;
using Newtonsoft.Json;

namespace Engine.Quests
{
    /// <summary>
    /// Базовый класс для всех условий квеста
    /// </summary>
    [JsonConverter(typeof(QuestConditionConverter))]
    public abstract class QuestCondition
    {
        public int ID { get; set; }
        public string Description { get; set; }
        public int RequiredAmount { get; set; }
        public int CurrentProgress { get; set; }
        public bool IsCompleted => CurrentProgress >= RequiredAmount;

        public QuestCondition() { }

        protected QuestCondition(int id, string description, int requiredAmount)
        {
            ID = id;
            Description = description;
            RequiredAmount = requiredAmount;
            CurrentProgress = 0;
        }

        public abstract bool CheckCondition(Player player, object context = null);
        public abstract void UpdateProgress(Player player, object context = null);
        public abstract string GetProgressText();
    }

    /// <summary>
    /// Условие: собрать определенное количество предметов
    /// </summary>
    public class CollectItemsCondition : QuestCondition
    {
        public int ItemID { get; set; }
        
        /// <summary>
        /// Список локаций, где могут спавниться предметы для этого квеста
        /// </summary>
        public List<QuestItemSpawnData> SpawnLocations { get; set; } = new List<QuestItemSpawnData>();

        public CollectItemsCondition() : base() { }

        public CollectItemsCondition(int id, string description, int itemID, int requiredAmount)
            : base(id, description, requiredAmount)
        {
            ItemID = itemID;
        }

        public override bool CheckCondition(Player player, object context = null)
        {
            if (player?.Inventory == null) return false;
            
            var item = player.Inventory.Items.Find(i => i.Details.ID == ItemID);
            CurrentProgress = item?.Quantity ?? 0;
            return IsCompleted;
        }

        public override void UpdateProgress(Player player, object context = null)
        {
            CheckCondition(player, context);
        }

        public override string GetProgressText()
        {
            return $"{Description}: {CurrentProgress}/{RequiredAmount}";
        }
    }

    /// <summary>
    /// Условие: убить определенное количество монстров
    /// </summary>
    public class KillMonstersCondition : QuestCondition
    {
        public int MonsterID { get; set; }

        public KillMonstersCondition() : base() { }

        public KillMonstersCondition(int id, string description, int monsterID, int requiredAmount)
            : base(id, description, requiredAmount)
        {
            MonsterID = monsterID;
        }

        public override bool CheckCondition(Player player, object context = null)
        {
            // Прогресс обновляется через события боя
            return IsCompleted;
        }

        public override void UpdateProgress(Player player, object context = null)
        {
            if (context is Monster killedMonster && killedMonster.ID == MonsterID)
            {
                int oldProgress = CurrentProgress;
                CurrentProgress = Math.Min(CurrentProgress + 1, RequiredAmount);
                DebugConsole.Log($"KillMonstersCondition.UpdateProgress: Monster {MonsterID} killed. Progress: {oldProgress} -> {CurrentProgress}/{RequiredAmount}");
            }
            else
            {
                DebugConsole.Log($"KillMonstersCondition.UpdateProgress: Context is not Monster or wrong ID. Context: {context?.GetType().Name}, MonsterID: {MonsterID}");
            }
        }

        public override string GetProgressText()
        {
            return $"{Description}: {CurrentProgress}/{RequiredAmount}";
        }
    }

    /// <summary>
    /// Условие: посетить определенную локацию
    /// </summary>
    public class VisitLocationCondition : QuestCondition
    {
        public int LocationID { get; set; }

        public VisitLocationCondition() : base() { }

        public VisitLocationCondition(int id, string description, int locationID)
            : base(id, description, 1)
        {
            LocationID = locationID;
        }

        public override bool CheckCondition(Player player, object context = null)
        {
            // Для условий посещения локации не проверяем автоматически при каждом вызове
            // Прогресс обновляется только при смене локации через UpdateProgress
            return IsCompleted;
        }

        public override void UpdateProgress(Player player, object context = null)
        {
            if (player?.CurrentLocation == null) return;
            
            // Обновляем прогресс только если игрок находится в нужной локации
            if (player.CurrentLocation.ID == LocationID && CurrentProgress == 0)
            {
                CurrentProgress = 1;
                DebugConsole.Log($"VisitLocationCondition.UpdateProgress: Player visited location {LocationID}. Progress: 0 -> {CurrentProgress}/{RequiredAmount}");
            }
        }

        public override string GetProgressText()
        {
            return IsCompleted ? $"{Description}: Выполнено" : $"{Description}: Не выполнено";
        }
    }

    /// <summary>
    /// Условие: поговорить с определенным NPC
    /// </summary>
    public class TalkToNPCCondition : QuestCondition
    {
        public int NPCID { get; set; }

        public TalkToNPCCondition() : base() { }

        public TalkToNPCCondition(int id, string description, int npcID)
            : base(id, description, 1)
        {
            NPCID = npcID;
        }

        public override bool CheckCondition(Player player, object context = null)
        {
            // Прогресс обновляется через события диалога
            return IsCompleted;
        }

        public override void UpdateProgress(Player player, object context = null)
        {
            if (context is NPC talkedNPC && talkedNPC.ID == NPCID)
            {
                CurrentProgress = 1;
            }
        }

        public override string GetProgressText()
        {
            return IsCompleted ? $"{Description}: Выполнено" : $"{Description}: Не выполнено";
        }
    }

    /// <summary>
    /// Условие: достичь определенного уровня
    /// </summary>
    public class ReachLevelCondition : QuestCondition
    {
        public int RequiredLevel { get; set; }

        public ReachLevelCondition() : base() { }

        public ReachLevelCondition(int id, string description, int requiredLevel)
            : base(id, description, requiredLevel)
        {
            RequiredLevel = requiredLevel;
        }

        public override bool CheckCondition(Player player, object context = null)
        {
            if (player == null) return false;
            
            CurrentProgress = player.Level;
            return IsCompleted;
        }

        public override void UpdateProgress(Player player, object context = null)
        {
            CheckCondition(player, context);
        }

        public override string GetProgressText()
        {
            return $"{Description}: {CurrentProgress}/{RequiredAmount}";
        }

        public override string ToString()
        {
            return GetProgressText();
        }
    }

    /// <summary>
    /// Данные о спавне предметов квеста на локации
    /// </summary>
    public class QuestItemSpawnData
    {
        /// <summary>
        /// ID локации, где может спавниться предмет
        /// </summary>
        public int LocationID { get; set; }
        
        /// <summary>
        /// Шанс спавна предмета (от 0 до 100)
        /// </summary>
        public int SpawnChance { get; set; }
        
        /// <summary>
        /// Количество предметов, которое может заспавниться за раз
        /// </summary>
        public int Quantity { get; set; } = 1;
        
        /// <summary>
        /// Максимальное количество предметов этого типа на локации одновременно
        /// </summary>
        public int MaxItemsOnLocation { get; set; } = 1;
        
        /// <summary>
        /// Интервал времени между попытками спавна (в игровых циклах)
        /// </summary>
        public int SpawnInterval { get; set; } = 1;
        
        /// <summary>
        /// Счетчик для отслеживания интервала спавна
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public int SpawnCounter { get; set; } = 0;
        
        /// <summary>
        /// Флаг, указывающий, что предметы уже были заспавнены для этого квеста
        /// </summary>
        [Newtonsoft.Json.JsonIgnore]
        public bool HasSpawned { get; set; } = false;

        public QuestItemSpawnData() 
        { 
            SpawnChance = 25;
            Quantity = 1;
            MaxItemsOnLocation = 1;
            SpawnInterval = 1;
        }
        
        public QuestItemSpawnData(int locationID, int spawnChance, int quantity = 1, int maxItemsOnLocation = 1, int spawnInterval = 1)
        {
            LocationID = locationID;
            SpawnChance = spawnChance;
            Quantity = quantity;
            MaxItemsOnLocation = maxItemsOnLocation;
            SpawnInterval = spawnInterval;
        }
    }
}
