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
                CurrentProgress = Math.Min(CurrentProgress + 1, RequiredAmount);
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
            if (player?.CurrentLocation == null) return false;
            
            CurrentProgress = player.CurrentLocation.ID == LocationID ? 1 : 0;
            return IsCompleted;
        }

        public override void UpdateProgress(Player player, object context = null)
        {
            CheckCondition(player, context);
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
}
