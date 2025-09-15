using Engine.Audio;
using Engine.Combat;
using Engine.Data;
using Engine.Entities;
using Engine.Factories;
using Engine.Quests;
using Engine.Saving;
using Engine.World;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;


namespace Engine.Core
{
    public static class GameServices
    {
        private static IWorldRepository _worldRepository;
        private static IOutputService _outputService;
        private static IGameFactory _gameFactory;
        private static EnhancedBufferedRenderer _bufferedRenderer;
        private static CombatRenderer _combatRenderer;
        private static QuestManager _questManager;
        public static Player CurrentPlayer { get; set; }

        public static EnhancedBufferedRenderer BufferedRenderer
        {
            get => _bufferedRenderer ??= new EnhancedBufferedRenderer();
            set => _bufferedRenderer = value;
        }

        public static CombatRenderer CombatRenderer
        {
            get
            {
                if (_combatRenderer == null)
                {
                    // Сначала убедимся, что BufferedRenderer создан
                    var renderer = BufferedRenderer;
                    _combatRenderer = new CombatRenderer(renderer);
                }
                return _combatRenderer;
            }
            set => _combatRenderer = value;
        }

        public static IWorldRepository WorldRepository
        {
            get
            {
                if (_worldRepository == null)
                {
                    string jsonPath = Path.Combine("Data", "game_data.json");
                    string fullPath = Path.GetFullPath(jsonPath);

                    DebugConsole.Log($"Путь к JSON: {fullPath}");
                    DebugConsole.Log($"Файл существует: {File.Exists(fullPath)}");
                    DebugConsole.Log($"Директория существует: {Directory.Exists(Path.GetDirectoryName(fullPath))}");

                    if (!File.Exists(fullPath))
                    {
                        throw new FileNotFoundException($"JSON файл не найден: {fullPath}");
                    }

                    _worldRepository = new JsonWorldRepository(fullPath);
                    DebugConsole.Log($"JsonWorldRepository instance type: {_worldRepository.GetType().FullName}");
                }

                return _worldRepository;
            }
            set => _worldRepository = value;
        }

        // Форматирование mapping вида Dictionary<string, Dictionary<int,int>>
        private static string FormatMapping(Dictionary<string, Dictionary<int, int>> mapping)
        {
            if (mapping == null || mapping.Count == 0) return "(no mapping)";
            var sb = new StringBuilder();
            foreach (var typeKv in mapping)
            {
                sb.AppendLine(typeKv.Key + ":");
                if (typeKv.Value == null || typeKv.Value.Count == 0)
                {
                    sb.AppendLine("  (no changes)");
                    continue;
                }
                foreach (var kv in typeKv.Value)
                    sb.AppendLine($"  {kv.Key} -> {kv.Value}");
            }
            return sb.ToString();
        }

        private static void LogDuplicatesVerbose(GameData data)
        {
            if (data == null) return;

            void LogFor<T>(IEnumerable<T> list, string typeName)
            {
                if (list == null) return;
                var idProp = typeof(T).GetProperty("ID") ?? typeof(T).GetProperty("Id");
                var nameProp = typeof(T).GetProperty("Name") ?? typeof(T).GetProperty("Title") ?? typeof(T).GetProperty("Id");
                if (idProp == null) return;

                var groups = list.Cast<object>()
                                 .GroupBy(x => (int)idProp.GetValue(x))
                                 .Where(g => g.Count() > 1);

                foreach (var g in groups)
                {
                    var id = g.Key;
                    var names = g.Select(x =>
                    {
                        if (nameProp != null) return nameProp.GetValue(x)?.ToString() ?? "<no-name>";
                        return x.ToString();
                    });
                    DebugConsole.Log($"Duplicate {typeName} ID={id}: {string.Join(", ", names)}");
                }
            }

            LogFor(data.Items, "Item");
            LogFor(data.Monsters, "Monster");
            LogFor(data.NPCs, "NPC");
            LogFor(data.Locations, "Location");
            LogFor(data.Quests, "Quest");
            LogFor(data.Titles, "Title");
        }

        // Простейшая запись GameData в файл с бэкапом. Не полагаемся на SaveManager, чтобы избежать ошибок.
        private static void SaveGameData(GameData data, string path)
        {
            try
            {
                // бэкап
                if (File.Exists(path))
                {
                    var bak = path + ".bak";
                    File.Copy(path, bak, overwrite: true);
                    DebugConsole.Log($"Создан бэкап {bak}");
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    WriteIndented = true,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
                };
                options.Converters.Add(new JsonStringEnumConverter());

                var json = JsonSerializer.Serialize(data, options);
                File.WriteAllText(path, json);
                DebugConsole.Log($"Сохранён исправленный JSON в {path}");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Ошибка при сохранении исправленного JSON: {ex.Message}");
                // не ломаем процесс — просто логгируем
            }
        }


        public static IGameFactory GameFactory
        {
            get => _gameFactory ??= new GameFactory(WorldRepository);
            set => _gameFactory = value;
        }

        public static QuestManager QuestManager
        {
            get
            {
                if (_questManager == null && CurrentPlayer != null)
                {
                    _questManager = new QuestManager(WorldRepository, CurrentPlayer.QuestLog);
                    // Загружаем квесты из game_data.json
                    var gameData = WorldRepository.GetGameData();
                    if (gameData?.Quests != null)
                    {
                        // Добавляем квесты в QuestManager для правильной инициализации
                        _questManager.SetAllQuests(gameData.Quests);
                        
                        DebugConsole.Log($"Загружено {gameData.Quests.Count} квестов из game_data.json");
                        
                        // Отладочные сообщения для проверки DialogueNodes
                        foreach (var quest in gameData.Quests)
                        {
                            DebugConsole.Log($"Quest {quest.ID} DialogueNodes: {quest.DialogueNodes?.ToString() ?? "null"}");
                        }
                    }
                }
                return _questManager;
            }
            set => _questManager = value;
        }


        public static void Shutdown()
        {
            try
            {
                _bufferedRenderer?.Dispose();
                _bufferedRenderer = null; // Важно: обнуляем ссылку

                if (_outputService is IDisposable disposable)
                {
                    disposable.Dispose();
                }

                Console.ResetColor();
                Console.CursorVisible = true;
                Console.Clear();

                DebugConsole.Log("Game services shutdown completed");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Error during shutdown: {ex.Message}");
            }
        }
    }
}