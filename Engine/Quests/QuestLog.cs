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
            DebugConsole.Log($"QuestLog.AddAvailableQuest: Добавляем квест {quest.ID} ({quest.Name}), Состояние: {quest.State}");
            DebugConsole.Log($"  Количество AvailableQuests до: {AvailableQuests.Count}");
            DebugConsole.Log($"  Количество ActiveQuests: {ActiveQuests.Count}");
            DebugConsole.Log($"  Количество CompletedQuests: {CompletedQuests.Count}");
            
            if (!AvailableQuests.Any(q => q.ID == quest.ID) && 
                !ActiveQuests.Any(q => q.ID == quest.ID) && 
                !CompletedQuests.Any(q => q.ID == quest.ID))
            {
                AvailableQuests.Add(quest);
                DebugConsole.Log($"  Квест {quest.ID} успешно добавлен в AvailableQuests");
            }
            else
            {
                DebugConsole.Log($"  Квест {quest.ID} уже существует в одном из списков, не добавляем");
            }
            
            DebugConsole.Log($"  Количество AvailableQuests после: {AvailableQuests.Count}");
        }

        /// <summary>
        /// Начинает квест
        /// </summary>
        public bool StartQuest(int questID)
        {
            DebugConsole.Log($"QuestLog.StartQuest вызван для ID квеста: {questID}");
            DebugConsole.Log($"AvailableQuests до: {AvailableQuests.Count}");
            DebugConsole.Log($"ActiveQuests до: {ActiveQuests.Count}");
            
            var quest = AvailableQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null)
            {
                DebugConsole.Log($"Квест {questID} не найден в AvailableQuests");
                return false;
            }
            
            DebugConsole.Log($"Найден квест: {quest.Name} (ID: {quest.ID}, Состояние: {quest.State})");
            
            if (!quest.CanStart(_player, this))
            {
                DebugConsole.Log($"Квест {questID} не может быть начат");
                return false;
            }

            quest.StartQuest(_player);
            DebugConsole.Log($"Квест {questID} начат, Состояние: {quest.State}");
            
            AvailableQuests.Remove(quest);
            ActiveQuests.Add(quest);
            
            // Уведомляем QuestManager о начале квеста для спавна предметов
            var questManager = GameServices.QuestManager;
            if (questManager != null)
            {
                questManager.OnQuestStarted(quest, _player);
            }
            
            DebugConsole.Log($"AvailableQuests после: {AvailableQuests.Count}");
            DebugConsole.Log($"ActiveQuests после: {ActiveQuests.Count}");
            
            return true;
        }

        /// <summary>
        /// Завершает квест
        /// </summary>
        public bool CompleteQuest(int questID)
        {
            DebugConsole.Log($"QuestLog.CompleteQuest вызван для ID квеста: {questID}");
            DebugConsole.Log($"ActiveQuests до: {ActiveQuests.Count}");
            DebugConsole.Log($"CompletedQuests до: {CompletedQuests.Count}");
            
            var quest = ActiveQuests.FirstOrDefault(q => q.ID == questID);
            if (quest == null)
            {
                DebugConsole.Log($"Квест {questID} не найден в ActiveQuests");
                return false;
            }
            
            DebugConsole.Log($"Найден квест: {quest.Name} (ID: {quest.ID}, Состояние: {quest.State})");
            
            // Проверяем, готов ли квест к завершению
            if (quest.State != QuestState.ReadyToComplete)
            {
                DebugConsole.Log($"Квест {questID} не готов к завершению (Состояние: {quest.State})");
                return false;
            }

            // Завершаем квест
            quest.CompleteQuest(_player);
            DebugConsole.Log($"Квест {questID} завершен, Состояние: {quest.State}");
            
            // Перемещаем квест из активных в завершенные
            ActiveQuests.Remove(quest);
            CompletedQuests.Add(quest);
            
            // Уведомляем QuestManager о завершении квеста для очистки предметов
            var questManager = GameServices.QuestManager;
            if (questManager != null)
            {
                questManager.OnQuestCompleted(quest, _player);
            }
            
            DebugConsole.Log($"ActiveQuests после: {ActiveQuests.Count}");
            DebugConsole.Log($"CompletedQuests после: {CompletedQuests.Count}");
            
            return true;
        }

        /// <summary>
        /// Обновляет прогресс всех активных квестов
        /// </summary>
        public void UpdateQuestProgress(object context = null)
        {
            DebugConsole.Log($"QuestLog.UpdateQuestProgress вызван с контекстом: {context?.GetType().Name ?? "null"}");
            
            foreach (var quest in ActiveQuests)
            {
                DebugConsole.Log($"Обновление квеста {quest.ID} ({quest.Name}) - Текущее состояние: {quest.State}");
                
                bool wasCompleted = quest.CheckAllConditions(_player, context);
                
                DebugConsole.Log($"Квест {quest.ID} после обновления - Состояние: {quest.State}, Все условия выполнены: {wasCompleted}");
                
                // Логируем детали условий
                if (quest.Conditions != null)
                {
                    foreach (var condition in quest.Conditions)
                    {
                        DebugConsole.Log($"  Условие {condition.ID}: {condition.Description} - Прогресс: {condition.CurrentProgress}/{condition.RequiredAmount}, Выполнено: {condition.IsCompleted}");
                    }
                }
            }
            
            // Обрабатываем спавн предметов для активных квестов
            var questManager = GameServices.QuestManager;
            if (questManager != null)
            {
                questManager.ProcessQuestItemSpawns();
            }
        }

        /// <summary>
        /// Получает квест по ID
        /// </summary>
        public EnhancedQuest GetQuest(int questID)
        {
            var quest = AvailableQuests.FirstOrDefault(q => q.ID == questID) ??
                       ActiveQuests.FirstOrDefault(q => q.ID == questID) ??
                       CompletedQuests.FirstOrDefault(q => q.ID == questID);
            
            DebugConsole.Log($"QuestLog.GetQuest({questID}): Найден = {quest != null}, Имя = {quest?.Name}, Состояние = {quest?.State}, Квестодатель = {quest?.QuestGiverID}");
            return quest;
        }

        /// <summary>
        /// Получает доступные квесты для NPC
        /// </summary>
        public List<EnhancedQuest> GetAvailableQuestsForNPC(int npcID)
        {
            DebugConsole.Log($"QuestLog.GetAvailableQuestsForNPC: Ищем квесты для NPC {npcID}");
            DebugConsole.Log($"  Всего AvailableQuests: {AvailableQuests.Count}");
            
            var quests = AvailableQuests.Where(q => q.QuestGiverID == npcID && q.CanStart(_player, this)).ToList();
            
            DebugConsole.Log($"  Найдено {quests.Count} доступных квестов для NPC {npcID}");
            foreach (var quest in quests)
            {
                DebugConsole.Log($"    Квест {quest.ID}: {quest.Name}, Состояние: {quest.State}, Может начаться: {quest.CanStart(_player, this)}");
            }
            
            return quests;
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