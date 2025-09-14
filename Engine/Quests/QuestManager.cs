using Engine.Core;
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
        /// Загружает квесты из JSON файла
        /// </summary>
        public void LoadQuests(string questsFilePath)
        {
            try
            {
                if (!File.Exists(questsFilePath))
                {
                    DebugConsole.Log($"Файл квестов не найден: {questsFilePath}");
                    return;
                }

                var json = File.ReadAllText(questsFilePath);
                var questsData = JsonConvert.DeserializeObject<QuestsData>(json);

                if (questsData?.Quests != null)
                {
                    _allQuests = questsData.Quests;
                    InitializeQuests();
                    DebugConsole.Log($"Загружено {_allQuests.Count} квестов");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Ошибка загрузки квестов: {ex.Message}");
            }
        }

        /// <summary>
        /// Инициализирует квесты после загрузки
        /// </summary>
        private void InitializeQuests()
        {
            foreach (var quest in _allQuests)
            {
                // Связываем квестодателя
                quest.QuestGiver = _worldRepository.NPCByID(quest.QuestGiverID);
                
                // Инициализируем предметы-награды
                foreach (var rewardItem in quest.Rewards.Items)
                {
                    rewardItem.ItemDetails = _worldRepository.ItemByID(rewardItem.ItemID);
                }

                // Добавляем в доступные квесты
                _questLog.AddAvailableQuest(quest);
            }
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
        /// Получает квест по ID
        /// </summary>
        public EnhancedQuest GetQuest(int questID)
        {
            return _allQuests.FirstOrDefault(q => q.ID == questID);
        }

        /// <summary>
        /// Получает все квесты
        /// </summary>
        public List<EnhancedQuest> GetAllQuests()
        {
            return _allQuests.ToList();
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
