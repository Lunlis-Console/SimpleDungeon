using System.Text;

namespace Engine
{
    public static class GameServices
    {
        private static IWorldRepository _worldRepository;
        private static IOutputService _outputService;
        private static IGameFactory _gameFactory;
        private static BufferedRenderer _bufferedRenderer;
        private static CombatRenderer _combatRenderer;

        public static BufferedRenderer BufferedRenderer
        {
            get => _bufferedRenderer ??= new BufferedRenderer(OutputService);
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

                DebugConsole.SetEnabled(true);

                WorldRepository = new StaticWorldRepository();
                WorldInitializer.InitializeWithDependencies(WorldRepository);
                GameFactory = new GameFactory(WorldRepository);
                OutputService = new ConsoleOutputService();

                // Инициализируем в правильном порядке
                BufferedRenderer = new BufferedRenderer(OutputService);
                CombatRenderer = new CombatRenderer(BufferedRenderer);

                DebugConsole.Log("Game services initialized successfully");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Failed to initialize game services: {ex.Message}");
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