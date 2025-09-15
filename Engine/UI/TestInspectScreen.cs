// TestInspectScreen.cs — временный тестовый экран
// ВНИМАНИЕ: Этот экран используется только для отладки и тестирования
// TODO: Удалить после завершения разработки
using Engine.Core;
using System;

namespace Engine.UI
{
    // ВРЕМЕННЫЙ ТЕСТОВЫЙ ЭКРАН - НЕ ИСПОЛЬЗУЕТСЯ В ПРОДАКШЕНЕ
    public class TestInspectScreen : BaseScreen
    {
        private readonly object _monster;
        private int _ticks = 0;

        public TestInspectScreen(object monster)
        {
            _monster = monster;
            DebugConsole.Log("[TestInspectScreen] ctor called for " + (_monster?.GetType().Name ?? "<null>"));
        }

        public override void Update()
        {
            _ticks++;
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Key == ConsoleKey.Escape)
            {
                ScreenManager.PopScreen();
                ScreenManager.RequestFullRedraw();
            }
        }

        public override void Render()
        {
            DebugConsole.Log("[TestInspectScreen] Render called (tick=" + _ticks + ")");
            var r = GameServices.BufferedRenderer;
            if (r != null)
            {
                r.FillArea(2, 2, 80, 10, ' ', ConsoleColor.Gray, ConsoleColor.Black);
                r.Write(4, 3, "=== TEST INSPECT SCREEN ===", ConsoleColor.Yellow, ConsoleColor.Black);
                r.Write(4, 5, "Monster: " + (_monster?.GetType().Name ?? "<null>"), ConsoleColor.Cyan, ConsoleColor.Black);
                r.Write(4, 6, "Press ESC to go back", ConsoleColor.DarkGray, ConsoleColor.Black);
                // просьба перерисовать
                ScreenManager.RequestPartialRedraw();
                r.SetNeedsFullRedraw();
            }
            else
            {
                Console.Clear();
                Console.WriteLine("=== TEST INSPECT SCREEN ===");
                Console.WriteLine("Monster: " + (_monster?.GetType().Name ?? "<null>"));
                Console.WriteLine("Press ESC to go back");
            }
        }
    }
}
