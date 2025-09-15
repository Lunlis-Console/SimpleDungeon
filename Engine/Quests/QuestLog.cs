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
            DebugConsole.Log($"QuestLog.StartQuest called for quest ID: {questID}");
            DebugConsole.Log($"AvailableQuests before: {AvailableQuests.Count}");
            DebugConsole.Log($"ActiveQuests before: {ActiveQuests.Count}");
            
            var quest = AvailableQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null)
            {
                DebugConsole.Log($"Quest {questID} not found in AvailableQuests");
                return false;
            }
            
            DebugConsole.Log($"Found quest: {quest.Name} (ID: {quest.ID}, State: {quest.State})");
            
            if (!quest.CanStart(_player, this))
            {
                DebugConsole.Log($"Quest {questID} cannot start");
                return false;
            }

            quest.StartQuest(_player);
            DebugConsole.Log($"Quest {questID} started, State: {quest.State}");
            
            AvailableQuests.Remove(quest);
            ActiveQuests.Add(quest);
            
            DebugConsole.Log($"AvailableQuests after: {AvailableQuests.Count}");
            DebugConsole.Log($"ActiveQuests after: {ActiveQuests.Count}");
            
            return true;
        }

        /// <summary>
        /// Завершает квест
        /// </summary>
        public bool CompleteQuest(int questID)
        {
            DebugConsole.Log($"QuestLog.CompleteQuest called for quest ID: {questID}");
            DebugConsole.Log($"ActiveQuests before: {ActiveQuests.Count}");
            DebugConsole.Log($"CompletedQuests before: {CompletedQuests.Count}");
            
            var quest = ActiveQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null)
            {
                DebugConsole.Log($"Quest {questID} not found in ActiveQuests");
                return false;
            }
            
            DebugConsole.Log($"Found quest: {quest.Name} (ID: {quest.ID}, State: {quest.State})");
            
            // Проверяем, готов ли квест к завершению
            if (quest.State != QuestState.ReadyToComplete)
            {
                DebugConsole.Log($"Quest {questID} is not ready to complete (State: {quest.State})");
                return false;
            }

            // Завершаем квест
            quest.CompleteQuest(_player);
            DebugConsole.Log($"Quest {questID} completed, State: {quest.State}");
            
            // Перемещаем квест из активных в завершенные
            ActiveQuests.Remove(quest);
            CompletedQuests.Add(quest);
            
            DebugConsole.Log($"ActiveQuests after: {ActiveQuests.Count}");
            DebugConsole.Log($"CompletedQuests after: {CompletedQuests.Count}");
            
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