using Engine.Combat;
using System.Text;

namespace Engine.Core
{
    public static class GameServices
    {
        private static IWorldRepository _worldRepository;
        private static IOutputService _outputService;
        private static IGameFactory _gameFactory;
        private static EnhancedBufferedRenderer _bufferedRenderer;
        private static CombatRenderer _combatRenderer;

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
                    DebugConsole.Log($"Файл существует: {File.Exists(jsonPath)}");
                    DebugConsole.Log($"Директория существует: {Directory.Exists(Path.GetDirectoryName(jsonPath))}");

                    if (!File.Exists(jsonPath))
                    {
                        throw new FileNotFoundException($"JSON файл не найден: {fullPath}");
                    }

                    // Пытаемся загрузить из JSON
                    if (File.Exists(jsonPath))
                    {
                        try
                        {
                            _worldRepository = new JsonWorldRepository(jsonPath);
                            DebugConsole.Log("Данные загружены из JSON");
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.Log($"❌ Ошибка загрузки JSON: {ex.Message}");
                            // Fallback больше не нужен!
                            throw new Exception("Не удалось загрузить игровые данные из JSON");
                        }
                    }
                    else
                    {
                        throw new Exception($"JSON файл не найден: {jsonPath}");
                    }
                }
                return _worldRepository;
            }
            set => _worldRepository = value;
        }
        public static IOutputService OutputService
        {
            get => _outputService ??= new ConsoleOutputService();
            set => _outputService = value;
        }

        public static IGameFactory GameFactory
        {
            get => _gameFactory ??= new GameFactory(WorldRepository);
            set => _gameFactory = value;
        }

        public static void Initialize()
        {
            try
            {
                Console.OutputEncoding = Encoding.UTF8;
                Console.CursorVisible = false;

                // Инициализируем рендерер первым
                BufferedRenderer = new EnhancedBufferedRenderer();

                // Остальная инициализация...
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Failed to initialize: {ex.Message}");
                throw;
            }
        }

        public static void InitializeForTests(IWorldRepository testRepository)
        {
            WorldRepository = testRepository;
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