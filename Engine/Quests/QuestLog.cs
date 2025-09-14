using Engine.Core;
using Engine.Entities;
using Engine.UI;

namespace Engine.Quests
{
    public class QuestLog
    {
        private readonly Player _player;

        // Система квестов
        public List<EnhancedQuest> ActiveQuests { get; set; }
        public List<EnhancedQuest> CompletedQuests { get; set; }
        public List<EnhancedQuest> AvailableQuests { get; set; }

        public QuestLog(Player player)
        {
            _player = player;
            ActiveQuests = new List<EnhancedQuest>();
            CompletedQuests = new List<EnhancedQuest>();
            AvailableQuests = new List<EnhancedQuest>();
        }

        public void DisplayQuestLog()
        {
            ScreenManager.PushScreen(new EnhancedQuestLogScreen(_player));
        }

        /// <summary>
        /// Добавляет квест в список доступных квестов
        /// </summary>
        public void AddAvailableQuest(EnhancedQuest quest)
        {
            if (!AvailableQuests.Any(q => q.ID == quest.ID) && 
                !ActiveQuests.Any(q => q.ID == quest.ID) && 
                !CompletedQuests.Any(q => q.ID == quest.ID))
            {
                AvailableQuests.Add(quest);
            }
        }

        /// <summary>
        /// Начинает квест
        /// </summary>
        public bool StartQuest(int questID)
        {
            var quest = AvailableQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null || !quest.CanStart(_player, this))
                return false;

            quest.StartQuest(_player);
            AvailableQuests.Remove(quest);
            ActiveQuests.Add(quest);
            return true;
        }

        /// <summary>
        /// Завершает квест
        /// </summary>
        public bool CompleteQuest(int questID)
        {
            var quest = ActiveQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null || quest.State != QuestState.ReadyToComplete)
                return false;

            quest.CompleteQuest(_player);
            ActiveQuests.Remove(quest);
            CompletedQuests.Add(quest);
            return true;
        }

        /// <summary>
        /// Обновляет прогресс всех активных квестов
        /// </summary>
        public void UpdateQuestProgress(object context = null)
        {
            foreach (var quest in ActiveQuests)
            {
                quest.CheckAllConditions(_player, context);
            }
        }

        /// <summary>
        /// Получает квест по ID
        /// </summary>
        public EnhancedQuest GetQuest(int questID)
        {
            return AvailableQuests.FirstOrDefault(q => q.ID == questID) ??
                   ActiveQuests.FirstOrDefault(q => q.ID == questID) ??
                   CompletedQuests.FirstOrDefault(q => q.ID == questID);
        }

        /// <summary>
        /// Получает доступные квесты для NPC
        /// </summary>
        public List<EnhancedQuest> GetAvailableQuestsForNPC(int npcID)
        {
            return AvailableQuests.Where(q => q.QuestGiverID == npcID && q.CanStart(_player, this)).ToList();
        }

        /// <summary>
        /// Получает активные квесты для NPC
        /// </summary>
        public List<EnhancedQuest> GetActiveQuestsForNPC(int npcID)
        {
            return ActiveQuests.Where(q => q.QuestGiverID == npcID).ToList();
        }

        /// <summary>
        /// Получает завершенные квесты для NPC
        /// </summary>
        public List<EnhancedQuest> GetCompletedQuestsForNPC(int npcID)
        {
            return CompletedQuests.Where(q => q.QuestGiverID == npcID).ToList();
        }
    }
}