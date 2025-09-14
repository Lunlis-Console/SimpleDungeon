using Engine.Core;
using System.Diagnostics;
using Engine.UI;
using Engine.Dialogue;

namespace SimpleDungeon
{
    public static class Program
    {
        private static bool _running = true;
        private static readonly object _renderLock = new object();

        public static void Main(string[] args)
        {
            DebugConsole.Initialize();

            
            Console.Title = "Simple Dungeon";
            // Console.CursorVisible = false;

            try
            {
                DebugConsole.Log("Initializing GameServices...");

                DebugConsole.Log("GameServices initialized successfully");
                // Запускаем главное меню
                ScreenManager.PushScreen(new MainMenuScreen());

                // ЯВНЫЙ ПЕРВЫЙ РЕНДЕР после инициализации
                Thread.Sleep(200); // Увеличиваем задержку
                ScreenManager.ForceRedraw();
                ScreenManager.RenderCurrentScreen();
                //DebugConsole.Log("Initial render completed");

                // Главный игровой цикл
                MainGameLoop();
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Fatal error: {ex.Message}");
                DebugConsole.Log($"Stack trace: {ex.StackTrace}");
                // Console.ReadKey();
            }
            finally
            {
                GameServices.Shutdown();
            }
        }
        private static void MainGameLoop()
        {
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            var lastWindowCheck = stopwatch.ElapsedMilliseconds;

            while (_running)
            {
                try
                {
                    // Проверяем изменение размера окна каждые 500ms
                    if (stopwatch.ElapsedMilliseconds - lastWindowCheck > 500)
                    {
                        if (GameServices.BufferedRenderer.CheckWindowResize())
                        {
                            ScreenManager.RequestFullRedraw();
                            DebugConsole.Log("Window resize handled");
                        }
                        lastWindowCheck = stopwatch.ElapsedMilliseconds;
                    }

                    MessageSystem.UpdateMessages();

                    // Обработка ввода
                    if (Console.KeyAvailable)
                    {
                        var keyInfo = Console.ReadKey(true);

                        if (keyInfo.Key == ConsoleKey.F3)
                        {
                            DebugConsole.Toggle();
                            // Консоль сама запросит нужный тип перерисовки
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


                    // Небольшая задержка для снижения нагрузки
                    Thread.Sleep(16); // ~60 FPS
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"Game loop error: {ex.Message}");
                    ScreenManager.RequestFullRedraw();
                    Thread.Sleep(100);
                }
            }
        }
    }
}