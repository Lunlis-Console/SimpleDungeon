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
            DebugConsole.Log($"QuestManager.InitializeQuests: Starting initialization of {_allQuests.Count} quests");
            
            foreach (var quest in _allQuests)
            {
                try
                {
                    DebugConsole.Log($"Initializing quest: {quest.Name} (ID: {quest.ID}, State: {quest.State}, QuestGiverID: {quest.QuestGiverID})");
                    
                    // Связываем квестодателя
                    quest.QuestGiver = _worldRepository.NPCByID(quest.QuestGiverID);
                    
                    // Валидация: проверяем, что NPC существует
                    if (quest.QuestGiver == null)
                    {
                        DebugConsole.Log($"WARNING: Quest {quest.ID} ({quest.Name}) has QuestGiverID {quest.QuestGiverID} but NPC not found!");
                    }
                    else
                    {
                        DebugConsole.Log($"Quest {quest.ID} linked to NPC {quest.QuestGiver.ID} ({quest.QuestGiver.Name})");
                    }
                    
                    // Инициализируем условия квеста
                    DebugConsole.Log($"QuestManager.InitializeQuests: Initializing conditions for quest {quest.ID}");
                    quest.InitializeConditions();
                    DebugConsole.Log($"QuestManager.InitializeQuests: Conditions initialized for quest {quest.ID}");
                    
                    // Инициализируем предметы-награды
                    DebugConsole.Log($"QuestManager.InitializeQuests: Initializing rewards for quest {quest.ID}");
                    foreach (var rewardItem in quest.Rewards.Items)
                    {
                        rewardItem.ItemDetails = _worldRepository.ItemByID(rewardItem.ItemID);
                    }
                    DebugConsole.Log($"QuestManager.InitializeQuests: Rewards initialized for quest {quest.ID}");

                    // Добавляем в доступные квесты
                    DebugConsole.Log($"QuestManager.InitializeQuests: About to add quest {quest.ID} to AvailableQuests");
                    _questLog.AddAvailableQuest(quest);
                    DebugConsole.Log($"QuestManager.InitializeQuests: Added quest {quest.ID} to AvailableQuests. Final state: {quest.State}");
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"ERROR: Failed to initialize quest {quest.ID}: {ex.Message}");
                    DebugConsole.Log($"Stack trace: {ex.StackTrace}");
                }
            }
            
            // Валидация всех связей квест-NPC
            DebugConsole.Log("Running quest-NPC validation...");
            try
            {
                Engine.Tools.QuestNPCValidator.ValidateQuestNPCConnections(_questLog, _worldRepository);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"ERROR: Quest-NPC validation failed: {ex.Message}");
            }
            
            DebugConsole.Log($"QuestManager.InitializeQuests: Completed initialization");
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
            DebugConsole.Log($"QuestManager.SetAllQuests: Setting {quests?.Count ?? 0} quests");
            DebugConsole.Log($"QuestManager.SetAllQuests: _questLog is null: {_questLog == null}");
            
            _allQuests = quests ?? new List<EnhancedQuest>();
            DebugConsole.Log($"QuestManager.SetAllQuests: _allQuests count: {_allQuests.Count}");
            
            InitializeQuests();
            DebugConsole.Log($"Установлено {_allQuests.Count} квестов из GameData");
        }

        /// <summary>
        /// Получает менеджер диалогов квестов
        /// </summary>
        public QuestDialogueManager GetQuestDialogueManager()
        {
            DebugConsole.Log($"QuestManager.GetQuestDialogueManager: _questLog is null: {_questLog == null}");
            if (_questLog == null)
            {
                DebugConsole.Log("QuestManager.GetQuestDialogueManager: _questLog is null, returning null");
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
            DebugConsole.Log($"QuestManager.OnQuestStarted: Quest {quest.ID} started");
            
            // Обрабатываем спавн предметов для условий собирания
            var collectConditions = quest.GetRuntimeConditions()
                .OfType<CollectItemsCondition>()
                .Where(c => c.SpawnLocations.Any());
            
            foreach (var condition in collectConditions)
            {
                DebugConsole.Log($"QuestManager.OnQuestStarted: Force spawning items for condition {condition.ID}");
                QuestItemSpawnManager.Instance.ForceSpawnQuestItems(condition);
            }
        }

        /// <summary>
        /// Обрабатывает завершение квеста и очистку предметов
        /// </summary>
        public void OnQuestCompleted(EnhancedQuest quest, Player player)
        {
            DebugConsole.Log($"QuestManager.OnQuestCompleted: Quest {quest.ID} completed");
            
            // Очищаем предметы квеста с локаций и из инвентаря игрока
            var collectConditions = quest.GetRuntimeConditions()
                .OfType<CollectItemsCondition>()
                .Where(c => c.SpawnLocations.Any());
            
            foreach (var condition in collectConditions)
            {
                DebugConsole.Log($"QuestManager.OnQuestCompleted: Cleaning up items for condition {condition.ID}");
                
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
