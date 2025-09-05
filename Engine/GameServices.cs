// GameServices.cs
namespace Engine
{
    public static class GameServices
    {
        private static IWorldRepository _worldRepository;
        private static IOutputService _outputService;
        private static IGameFactory _gameFactory;
        private static Renderer _renderer;

        public static IWorldRepository WorldRepository
        {
            get => _worldRepository ??= new StaticWorldRepository();
            set => _worldRepository = value;
        }

        public static IOutputService OutputService
        {
            get => _outputService ??= new ConsoleOutputService(); // Используем простой ConsoleOutputService
            set => _outputService = value;
        }

        public static IGameFactory GameFactory
        {
            get => _gameFactory ??= new GameFactory(WorldRepository);
            set => _gameFactory = value;
        }

        public static Renderer Renderer
        {
            get => _renderer ??= new Renderer(OutputService);
            set => _renderer = value;
        }

        public static void Initialize()
        {
            WorldRepository = new StaticWorldRepository();
            WorldInitializer.InitializeWithDependencies(WorldRepository);
            GameFactory = new GameFactory(WorldRepository);
            Renderer = new Renderer(OutputService);
        }

        public static void InitializeForTests(IWorldRepository testRepository)
        {
            WorldRepository = testRepository;
        }
    }
}