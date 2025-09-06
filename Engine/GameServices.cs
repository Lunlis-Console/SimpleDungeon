using System.Text;

namespace Engine
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
            get => _worldRepository ??= new StaticWorldRepository();
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