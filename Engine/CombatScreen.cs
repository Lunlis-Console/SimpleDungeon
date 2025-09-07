using System;
using System.Collections.Generic;

namespace Engine
{
    public class CombatScreen : BaseScreen
    {
        private readonly CombatEngine _engine;

        public CombatScreen(Player player, Monster monster)
        {
            // Создаём движок боя (он теперь — неблокирующий)
            _engine = new CombatEngine(player, monster);
            // Просим полный рендер при первом показе
            GameServices.BufferedRenderer.SetNeedsFullRedraw();
            RequestFullRedraw();
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader($"БОЙ: {SafeGetString(_engine.Monster, new[] { "Name", "name" }) ?? "Монстр"}  —  {SafeGetString(_engine.Player, new[] { "Name", "name" }) ?? "Игрок"}");

            // Делегируем отрисовку игрового содержимого CombatRenderer'у
            GameServices.CombatRenderer.RenderCombatFrame(
                _engine.Player,
                _engine.Monster,
                _engine.GetCombatLog(),
                _engine.CurrentTurn,
                _engine.IsPlayerTurnReady,
                // новый флаг - сообщаем рендереру, выполняется ли сейчас действие монстра
                // (если в вашей реализации флаг называется иначе, замените на соответствующее свойство)
                _engine.IsEnemyActing
            );

            _renderer.EndFrame();
        }

        // ... остальной код без изменений ...
        private string BuildBarString(int width, int value, int maxValue)
        {
            if (width < 4) width = 4;
            int inner = width - 2;
            int filled = (int)Math.Round((double)inner * value / Math.Max(1, maxValue));
            filled = Math.Clamp(filled, 0, inner);
            string fill = new string('█', filled);
            string empty = new string('░', inner - filled);
            return "[" + fill + empty + "]";
        }

        private int SafeGetInt(object obj, string[] names)
        {
            if (obj == null) return 0;
            var t = obj.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null)
                {
                    var v = p.GetValue(obj);
                    if (v is int ii) return ii;
                    if (v is long ll) return (int)ll;
                    if (v != null) { try { return Convert.ToInt32(v); } catch { } }
                }
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (f != null)
                {
                    var v = f.GetValue(obj);
                    if (v is int ii) return ii;
                    if (v is long ll) return (int)ll;
                    if (v != null) { try { return Convert.ToInt32(v); } catch { } }
                }
            }
            // fallback methods omitted for brevity (leave as in your original)
            return 0;
        }

        private string SafeGetString(object obj, string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(string))
                    return p.GetValue(obj) as string;
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(string))
                    return f.GetValue(obj) as string;
            }
            return null;
        }

        public override void Update()
        {
            _engine.UpdateFrame();

            if (_engine.IsCombatOver)
            {
                // Очистка игровых ссылок на монстра (на всякий случай)
                if (_engine.Player != null)
                {
                    _engine.Player.CurrentMonster = null;
                    _engine.Player.IsInCombat = false;
                }

                // Попадаем на экран мира — замените WorldScreen на фактическое имя вашего экрана карты
                ScreenManager.PopUntil<GameWorldScreen>();

                // Гарантируем перерисовку
                ScreenManager.RequestFullRedraw();
                return;
            }

            RequestPartialRedraw();
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (keyInfo.Key == ConsoleKey.Q || keyInfo.Key == ConsoleKey.Escape)
            {
                ScreenManager.PopScreen();
                ScreenManager.RequestFullRedraw();
                return;
            }

            // Обработаем действия игрока — только если игрок готов и если монстр не действует
            if (!_engine.IsPlayerTurnReady || _engine.IsEnemyActing) return;

            switch (keyInfo.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    _engine.ProcessPlayerAction_Attack();
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    _engine.ProcessPlayerAction_Spell();
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    _engine.ProcessPlayerAction_Defend();
                    break;
                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    _engine.ProcessPlayerAction_Flee();
                    break;
            }

            RequestPartialRedraw();
        }
    }
}
