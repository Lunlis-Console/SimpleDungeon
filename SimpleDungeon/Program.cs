using Engine;
using System.Diagnostics;

namespace SimpleDungeon
{
    public static class Program
    {
        private static bool _running = true;
        private static readonly object _renderLock = new object();

        public static void Main(string[] args)
        {
            Console.Title = "Simple Dungeon";
            Console.CursorVisible = false;

            try
            {
                Console.WriteLine("Initializing GameServices...");
                GameServices.Initialize();
                DebugConsole.Log("GameServices initialized successfully");

                // Запускаем главное меню
                ScreenManager.PushScreen(new MainMenuScreen());

                // ЯВНЫЙ ПЕРВЫЙ РЕНДЕР после инициализации
                Thread.Sleep(200); // Увеличиваем задержку
                ScreenManager.ForceRedraw();
                ScreenManager.RenderCurrentScreen();

                DebugConsole.Log("Initial render completed");

                // Главный игровой цикл
                MainGameLoop();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Fatal error: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                Console.ReadKey();
            }
            finally
            {
                GameServices.Shutdown();
            }
        }
        private static void MainGameLoop()
        {
            int frameCount = 0;
            const int targetFps = 30;
            const int frameDelay = 1000 / targetFps;
            var stopwatch = new Stopwatch();
            var lastWindowCheck = Stopwatch.StartNew();
            stopwatch.Start();

            while (_running)
            {
                frameCount++;
                long startTime = stopwatch.ElapsedMilliseconds;

                try
                {
                    // Проверяем изменение размера окна каждые 500ms
                    if (lastWindowCheck.ElapsedMilliseconds > 500)
                    {
                        if (GameServices.BufferedRenderer.CheckWindowResize())
                        {
                            ScreenManager.RequestFullRedraw();
                            DebugConsole.Log("Window resize handled");
                        }
                        lastWindowCheck.Restart();
                    }

                    // Обработка ввода
                    if (Console.KeyAvailable)
                    {
                        var keyInfo = Console.ReadKey(true);

                        if (keyInfo.Key == ConsoleKey.F3)
                        {
                            DebugConsole.Toggle();
                            ScreenManager.RequestFullRedraw();
                            continue;
                        }

                        if (DebugConsole.IsVisible)
                        {
                            DebugConsole.ProcessInput(keyInfo);
                            continue;
                        }

                        ScreenManager.HandleInput(keyInfo);
                    }

                    // Обновление состояния
                    ScreenManager.Update();

                    // Рендеринг
                    ScreenManager.RenderCurrentScreen();

                    // Консоль отладки поверх всего
                    if (DebugConsole.IsVisible)
                    {
                        DebugConsole.GlobalDraw();
                    }

                    // Контроль FPS
                    long frameTime = stopwatch.ElapsedMilliseconds - startTime;
                    if (frameTime < frameDelay)
                    {
                        Thread.Sleep((int)(frameDelay - frameTime));
                    }

                    // Периодическая полная перерисовка для предотвращения артефактов
                    if (frameCount % 60 == 0)
                    {
                        ScreenManager.RequestFullRedraw();
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"Game loop error: {ex.Message}");
                    // При ошибке запрашиваем полную перерисовку
                    ScreenManager.RequestFullRedraw();
                    Thread.Sleep(100);
                }
            }
        }
        public static void ExitGame()
        {
            _running = false;
        }
    }
}