// CombatRenderer.cs
using System;
using System.Collections.Generic;
using System.Text;

namespace Engine
{
    public class CombatRenderer
    {
        private readonly DoubleBufferRenderer _buffer;
        private CombatState _previousState;

        // Позиции элементов на экране
        private const int TurnStatusLine = 0;
        private const int TurnNumberLine = 1;
        private const int MonsterNameLine = 3;
        private const int MonsterHealthLine = 4;
        private const int MonsterSpeedLine = 5;
        private const int MonsterStatsLine = 6;
        private const int SeparatorLine = 7;
        private const int CombatLogStartLine = 8;
        private const int PlayerStatsLine = 18;
        private const int PlayerHealthLine = 19;
        private const int PlayerSpeedLine = 20;
        private const int ActionsLine = 22;

        public CombatRenderer(DoubleBufferRenderer buffer)
        {
            _buffer = buffer;
            _previousState = new CombatState();
        }

        public void RenderCombatFrame(Player player, Monster monster, List<string> combatLog,
                                    int currentTurn, int playerSpeed, int monsterSpeed)
        {
            _buffer.BeginFrame();

            var currentState = new CombatState
            {
                PlayerHP = player.CurrentHP,
                PlayerMaxHP = player.TotalMaximumHP,
                PlayerSpeed = playerSpeed,
                MonsterHP = monster.CurrentHP,
                MonsterMaxHP = monster.MaximumHP,
                MonsterSpeed = monsterSpeed,
                CurrentTurn = currentTurn,
                CombatLog = combatLog,
                PlayerAttack = player.Attack,
                PlayerDefence = player.Defence,
                PlayerAgility = player.Agility,
                MonsterAttack = monster.Attack,
                MonsterDefence = monster.Defence,
                MonsterAgility = monster.Agility
            };

            RenderStaticElements();
            RenderDynamicElements(currentState);

            _buffer.EndFrame();
            _previousState = currentState;
        }

        private void RenderStaticElements()
        {
            // Эти элементы рисуем только один раз
            if (!_previousState.StaticElementsRendered)
            {
                // Разделительная линия
                _buffer.Write(0, SeparatorLine, new string('=', Console.WindowWidth));

                // Заголовок действий
                _buffer.Write(0, ActionsLine, "=========Действия========");
                _buffer.Write(0, ActionsLine + 1, "| 1 - атаковать | 2 - заклинание |");
                _buffer.Write(0, ActionsLine + 2, "| 3 - защищаться | 4 - бежать |");

                _previousState.StaticElementsRendered = true;
            }
        }

        private void RenderDynamicElements(CombatState currentState)
        {
            // Статус хода
            RenderTurnStatus(currentState);

            // Информация о монстре
            RenderMonsterInfo(currentState);

            // Лог боя
            RenderCombatLog(currentState.CombatLog);

            // Информация о игроке
            RenderPlayerInfo(currentState);
        }

        private void RenderTurnStatus(CombatState state)
        {
            string status = state.PlayerSpeed >= 100 ? "=== ВАШ ХОД ===" :
                          state.MonsterSpeed >= 100 ? "=== ХОД МОНСТРА ===" :
                          "=== НАПОЛНЕНИЕ ШКАЛЫ СКОРОСТИ ===";

            if (status != _previousState.TurnStatus)
            {
                _buffer.Write(0, TurnStatusLine, status.PadRight(Console.WindowWidth));
                _previousState.TurnStatus = status;
            }

            string turnText = $"================ ХОД {state.CurrentTurn} ================";
            if (turnText != _previousState.TurnText)
            {
                _buffer.Write(0, TurnNumberLine, turnText.PadRight(Console.WindowWidth));
                _previousState.TurnText = turnText;
            }
        }

        private void RenderMonsterInfo(CombatState state)
        {
            // Здоровье монстра
            if (state.MonsterHP != _previousState.MonsterHP ||
                state.MonsterMaxHP != _previousState.MonsterMaxHP)
            {
                RenderHealthBar(0, MonsterHealthLine, state.MonsterHP, state.MonsterMaxHP, "Здоровье");
            }

            // Скорость монстра
            if (state.MonsterSpeed != _previousState.MonsterSpeed)
            {
                RenderSpeedBar(0, MonsterSpeedLine, state.MonsterSpeed, "Скорость");
            }

            // Статы монстра
            if (state.MonsterAttack != _previousState.MonsterAttack ||
                state.MonsterDefence != _previousState.MonsterDefence ||
                state.MonsterAgility != _previousState.MonsterAgility)
            {
                string stats = $"АТК: {state.MonsterAttack} | ЗЩТ: {state.MonsterDefence} | ЛОВ: {state.MonsterAgility}";
                _buffer.Write(0, MonsterStatsLine, stats.PadRight(Console.WindowWidth));
            }
        }

        private void RenderPlayerInfo(CombatState state)
        {
            // Здоровье игрока
            if (state.PlayerHP != _previousState.PlayerHP ||
                state.PlayerMaxHP != _previousState.PlayerMaxHP)
            {
                RenderHealthBar(0, PlayerHealthLine, state.PlayerHP, state.PlayerMaxHP, "Здоровье");
            }

            // Скорость игрока
            if (state.PlayerSpeed != _previousState.PlayerSpeed)
            {
                RenderSpeedBar(0, PlayerSpeedLine, state.PlayerSpeed, "Скорость");
            }

            // Статы игрока
            if (state.PlayerAttack != _previousState.PlayerAttack ||
                state.PlayerDefence != _previousState.PlayerDefence ||
                state.PlayerAgility != _previousState.PlayerAgility)
            {
                string stats = $"АТК: {state.PlayerAttack} | ЗЩТ: {state.PlayerDefence} | ЛОВ: {state.PlayerAgility}";
                _buffer.Write(0, PlayerStatsLine, stats.PadRight(Console.WindowWidth));
            }
        }

        private void RenderCombatLog(List<string> combatLog)
        {
            if (combatLog == null) return;

            int y = CombatLogStartLine;
            int maxLines = PlayerStatsLine - CombatLogStartLine - 1;

            // Очищаем область лога
            for (int i = 0; i < maxLines; i++)
            {
                _buffer.Write(0, y + i, new string(' ', Console.WindowWidth));
            }

            // Рисуем новые сообщения
            y = CombatLogStartLine;
            int linesToShow = Math.Min(combatLog.Count, maxLines);
            int startIndex = Math.Max(0, combatLog.Count - maxLines);

            for (int i = startIndex; i < combatLog.Count && y < PlayerStatsLine; i++)
            {
                string message = combatLog[i];
                ConsoleColor color = GetMessageColor(message);

                // Сохраняем текущий цвет
                ConsoleColor previousColor = Console.ForegroundColor;
                Console.ForegroundColor = color;

                _buffer.Write(0, y, message.PadRight(Console.WindowWidth));

                // Восстанавливаем цвет
                Console.ForegroundColor = previousColor;

                y++;
            }
        }

        private void RenderHealthBar(int x, int y, int current, int max, string label)
        {
            current = Math.Max(current, 0);
            max = Math.Max(max, 1);

            float percentage = (float)current / max;
            int bars = (int)(20 * percentage);
            bars = Math.Clamp(bars, 0, 20);
            int emptyBars = 20 - bars;

            string healthText = $"{current}/{max}";
            string bar = $"{label}: [";

            // Выбираем цвет
            ConsoleColor color = percentage > 0.5f ? ConsoleColor.Green :
                                percentage > 0.25f ? ConsoleColor.Yellow :
                                ConsoleColor.Red;

            bar += new string('█', bars);
            bar += new string('░', emptyBars);
            bar += $"] {healthText}";

            // Сохраняем цвет
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = color;

            _buffer.Write(x, y, bar.PadRight(Console.WindowWidth));

            // Восстанавливаем цвет
            Console.ForegroundColor = previousColor;
        }

        private void RenderSpeedBar(int x, int y, int current, string label)
        {
            float percentage = (float)current / 100;
            int bars = (int)(20 * percentage);
            bars = Math.Clamp(bars, 0, 20);
            int emptyBars = 20 - bars;

            string bar = $"{label}: [";
            bar += new string('█', bars);
            bar += new string('░', emptyBars);
            bar += $"] {current}%";

            // Сохраняем цвет
            ConsoleColor previousColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Cyan;

            _buffer.Write(x, y, bar.PadRight(Console.WindowWidth));

            // Восстанавливаем цвет
            Console.ForegroundColor = previousColor;
        }

        private ConsoleColor GetMessageColor(string message)
        {
            if (string.IsNullOrEmpty(message)) return ConsoleColor.Gray;

            return message switch
            {
                string s when s.Contains("Вы ") || s.Contains("Вы достигли") => ConsoleColor.Green,
                string s when s.Contains("КРИТИЧЕСКИЙ УДАР") => ConsoleColor.Red,
                string s when s.Contains("ХОД") || s.Contains("====") => ConsoleColor.Yellow,
                string s when s.Contains("Добыча") || s.Contains("Получено") => ConsoleColor.Cyan,
                string s when s.Contains("побежден") || s.Contains("побеждён") => ConsoleColor.Magenta,
                _ => ConsoleColor.Gray
            };
        }

        public void SetNeedsFullRedraw()
        {
            _previousState = new CombatState();
        }
    }

    // Класс для хранения состояния боя
    public class CombatState
    {
        public int PlayerHP { get; set; }
        public int PlayerMaxHP { get; set; }
        public int PlayerSpeed { get; set; }
        public int MonsterHP { get; set; }
        public int MonsterMaxHP { get; set; }
        public int MonsterSpeed { get; set; }
        public int CurrentTurn { get; set; }
        public List<string> CombatLog { get; set; }
        public int PlayerAttack { get; set; }
        public int PlayerDefence { get; set; }
        public int PlayerAgility { get; set; }
        public int MonsterAttack { get; set; }
        public int MonsterDefence { get; set; }
        public int MonsterAgility { get; set; }
        public string TurnStatus { get; set; }
        public string TurnText { get; set; }
        public bool StaticElementsRendered { get; set; }
    }
}