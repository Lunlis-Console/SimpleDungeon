using Engine.Core;
using Engine.Dialogue;
using Engine.Entities;
using Engine.World;
using Newtonsoft.Json;

namespace Engine.Quests
{
    /// <summary>
    /// Менеджер для управления квестами в игре
    /// </summary>
    public class QuestManager
    {
        private readonly IWorldRepository _worldRepository;
        private readonly QuestLog _questLog;
        private List<EnhancedQuest> _allQuests;

        public QuestManager(IWorldRepository worldRepository, QuestLog questLog)
        {
            _worldRepository = worldRepository;
            _questLog = questLog;
            _allQuests = new List<EnhancedQuest>();
        }

        /// <summary>
        /// Инициализирует квесты после загрузки
        /// </summary>
        private void InitializeQuests()
        {
            DebugConsole.Log($"QuestManager.InitializeQuests: Начинаем инициализацию {_allQuests.Count} квестов");
            
            foreach (var quest in _allQuests)
            {
                try
                {
                    DebugConsole.Log($"Инициализация квеста: {quest.Name} (ID: {quest.ID}, Состояние: {quest.State}, ID квестодателя: {quest.QuestGiverID})");
                    
                    // Связываем квестодателя
                    quest.QuestGiver = _worldRepository.NPCByID(quest.QuestGiverID);
                    
                    // Валидация: проверяем, что NPC существует
                    if (quest.QuestGiver == null)
                    {
                        DebugConsole.Log($"ПРЕДУПРЕЖДЕНИЕ: Квест {quest.ID} ({quest.Name}) имеет QuestGiverID {quest.QuestGiverID}, но NPC не найден!");
                    }
                    else
                    {
                        DebugConsole.Log($"Квест {quest.ID} связан с NPC {quest.QuestGiver.ID} ({quest.QuestGiver.Name})");
                    }
                    
                    // Инициализируем условия квеста
                    DebugConsole.Log($"QuestManager.InitializeQuests: Инициализация условий для квеста {quest.ID}");
                    quest.InitializeConditions();
                    DebugConsole.Log($"QuestManager.InitializeQuests: Условия инициализированы для квеста {quest.ID}");
                    
                    // Инициализируем предметы-награды
                    DebugConsole.Log($"QuestManager.InitializeQuests: Инициализация наград для квеста {quest.ID}");
                    foreach (var rewardItem in quest.Rewards.Items)
                    {
                        rewardItem.ItemDetails = _worldRepository.ItemByID(rewardItem.ItemID);
                    }
                    DebugConsole.Log($"QuestManager.InitializeQuests: Награды инициализированы для квеста {quest.ID}");

                    // Добавляем в доступные квесты
                    DebugConsole.Log($"QuestManager.InitializeQuests: Собираемся добавить квест {quest.ID} в AvailableQuests");
                    _questLog.AddAvailableQuest(quest);
                    DebugConsole.Log($"QuestManager.InitializeQuests: Квест {quest.ID} добавлен в AvailableQuests. Финальное состояние: {quest.State}");
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"ОШИБКА: Не удалось инициализировать квест {quest.ID}: {ex.Message}");
                    DebugConsole.Log($"Трассировка стека: {ex.StackTrace}");
                }
            }
            
            // Валидация всех связей квест-NPC
            DebugConsole.Log("Запуск валидации квест-NPC...");
            try
            {
                Engine.Tools.QuestNPCValidator.ValidateQuestNPCConnections(_questLog, _worldRepository);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"ОШИБКА: Валидация квест-NPC не удалась: {ex.Message}");
            }
            
            DebugConsole.Log($"QuestManager.InitializeQuests: Инициализация завершена");
        }

        /// <summary>
        /// Сохраняет квесты в JSON файл
        /// </summary>
        public void SaveQuests(string questsFilePath)
        {
            try
            {
                var questsData = new QuestsData
                {
                    Quests = _allQuests
                };

                var json = JsonConvert.SerializeObject(questsData, Formatting.Indented);
                File.WriteAllText(questsFilePath, json);
                DebugConsole.Log($"Квесты сохранены в {questsFilePath}");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Ошибка сохранения квестов: {ex.Message}");
            }
        }

        /// <summary>
        /// Создает новый квест
        /// </summary>
        public EnhancedQuest CreateQuest(int id, string name, string description, int questGiverID)
        {
            var quest = new EnhancedQuest(id, name, description, questGiverID);
            _allQuests.Add(quest);
            return quest;
        }

        /// <summary>
        /// Устанавливает список всех квестов (для инициализации из GameServices)
        /// </summary>
        public void SetAllQuests(List<EnhancedQuest> quests)
        {
            DebugConsole.Log($"QuestManager.SetAllQuests: Устанавливаем {quests?.Count ?? 0} квестов");
            DebugConsole.Log($"QuestManager.SetAllQuests: _questLog равен null: {_questLog == null}");
            
            _allQuests = quests ?? new List<EnhancedQuest>();
            DebugConsole.Log($"QuestManager.SetAllQuests: количество _allQuests: {_allQuests.Count}");
            
            InitializeQuests();
            DebugConsole.Log($"Установлено {_allQuests.Count} квестов из GameData");
        }

        /// <summary>
        /// Получает менеджер диалогов квестов
        /// </summary>
        public QuestDialogueManager GetQuestDialogueManager()
        {
            DebugConsole.Log($"QuestManager.GetQuestDialogueManager: _questLog равен null: {_questLog == null}");
            if (_questLog == null)
            {
                DebugConsole.Log("QuestManager.GetQuestDialogueManager: _questLog равен null, возвращаем null");
                return null;
            }
            return new QuestDialogueManager(_questLog);
        }

        /// <summary>
        /// Обновляет прогресс квестов при событиях в игре
        /// </summary>
        public void UpdateQuestProgress(object context = null)
        {
            _questLog.UpdateQuestProgress(context);
        }

        /// <summary>
        /// Обрабатывает событие убийства монстра
        /// </summary>
        public void OnMonsterKilled(Monster monster, Player player)
        {
            UpdateQuestProgress(monster);
        }

        /// <summary>
        /// Обрабатывает событие разговора с NPC
        /// </summary>
        public void OnNPCTalked(NPC npc, Player player)
        {
            UpdateQuestProgress(npc);
        }

        /// <summary>
        /// Обрабатывает событие смены локации
        /// </summary>
        public void OnLocationChanged(Player player)
        {
            UpdateQuestProgress();
        }

        /// <summary>
        /// Обрабатывает событие получения предмета
        /// </summary>
        public void OnItemObtained(Item item, Player player)
        {
            UpdateQuestProgress();
        }

        /// <summary>
        /// Обрабатывает спавн предметов для активных квестов
        /// </summary>
        public void ProcessQuestItemSpawns()
        {
            var activeQuests = _questLog.ActiveQuests;
            QuestItemSpawnManager.Instance.InitializeQuestItemSpawns(activeQuests);
        }

        /// <summary>
        /// Обрабатывает начало квеста и спавн предметов
        /// </summary>
        public void OnQuestStarted(EnhancedQuest quest, Player player)
        {
            DebugConsole.Log($"QuestManager.OnQuestStarted: Квест {quest.ID} начат");
            
            // Обрабатываем спавн предметов для условий собирания
            var collectConditions = quest.GetRuntimeConditions()
                .OfType<CollectItemsCondition>()
                .Where(c => c.SpawnLocations.Any());
            
            foreach (var condition in collectConditions)
            {
                DebugConsole.Log($"QuestManager.OnQuestStarted: Принудительный спавн предметов для условия {condition.ID}");
                QuestItemSpawnManager.Instance.ForceSpawnQuestItems(condition);
            }
        }

        /// <summary>
        /// Обрабатывает завершение квеста и очистку предметов
        /// </summary>
        public void OnQuestCompleted(EnhancedQuest quest, Player player)
        {
            DebugConsole.Log($"QuestManager.OnQuestCompleted: Квест {quest.ID} завершен");
            
            // Очищаем предметы квеста с локаций и из инвентаря игрока
            var collectConditions = quest.GetRuntimeConditions()
                .OfType<CollectItemsCondition>()
                .Where(c => c.SpawnLocations.Any());
            
            foreach (var condition in collectConditions)
            {
                DebugConsole.Log($"QuestManager.OnQuestCompleted: Очистка предметов для условия {condition.ID}");
                
                // Удаляем предметы квеста с локаций
                QuestItemSpawnManager.Instance.CleanupQuestItems(condition);
                
                // Удаляем предметы квеста из инвентаря игрока
                QuestItemSpawnManager.Instance.RemoveQuestItemsFromPlayer(condition, player);
            }
        }
    }

    /// <summary>
    /// Контейнер для данных квестов
    /// </summary>
    public class QuestsData
    {
        [JsonProperty("quests")]
        public List<EnhancedQuest> Quests { get; set; } = new List<EnhancedQuest>();
    }
}
