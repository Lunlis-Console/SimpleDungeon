using Engine.Core;
using System;
using System.Collections.Generic;

namespace Engine.Combat
{
    public class CombatRenderer
    {
        private readonly EnhancedBufferedRenderer _r;

        public int Left { get; set; } = 2;
        public int RightMargin { get; set; } = 2;

        public CombatRenderer(EnhancedBufferedRenderer renderer)
        {
            _r = renderer ?? throw new ArgumentNullException(nameof(renderer));
        }

        /// <summary>
        /// Рисует кадр боя.
        /// </summary>
        public void RenderCombatFrame(object player, object monster, List<string> combatLog, int currentTurn,
            bool isPlayerTurnReady, bool isEnemyActing, bool isMonsterPreparing = false, int logScrollOffset = 0)
        {
            int width = _r.Width;
            int height = _r.Height;

            // Минимум места
            if (width < 40 || height < 14)
            {
                _r.FillArea(0, 0, width, height, ' ', ConsoleColor.White, ConsoleColor.Black);
                _r.Write(0, 0, "Окно слишком мало для отрисовки боя (min 40x14).", ConsoleColor.Yellow, ConsoleColor.Black);
                return;
            }

            // Верхняя рамка / заголовок
            string top = "┌" + new string('─', Math.Max(0, width - 2)) + "┐";
            string bottomTop = "└" + new string('─', Math.Max(0, width - 2)) + "┘";
            _r.Write(0, 0, top, ConsoleColor.DarkGray, ConsoleColor.Black);

            string title = $"БОЙ";
            if (title.Length > width - 4) title = title.Substring(0, width - 7) + "...";
            int pad = Math.Max(0, (width - 2 - title.Length) / 2);
            string titleLine = "│" + new string(' ', pad) + title + new string(' ', Math.Max(0, width - 2 - title.Length - pad)) + "│";
            _r.Write(0, 1, titleLine, ConsoleColor.Yellow, ConsoleColor.Black);
            _r.Write(0, 2, bottomTop, ConsoleColor.DarkGray, ConsoleColor.Black);

            // Нижняя рамка
            int bottomFrameTop = height - 3;
            int bottomFrameRow = height - 2;
            int bottomFrameBottom = height - 1;
            string bottomTopFrame = "┌" + new string('─', Math.Max(0, width - 2)) + "┐";
            string bottomBottomFrame = "└" + new string('─', Math.Max(0, width - 2)) + "┘";
            _r.Write(0, bottomFrameTop, bottomTopFrame, ConsoleColor.DarkGray, ConsoleColor.Black);

            // Контролы внизу (центруем)
            string controls = "1-Атаковать  2-Заклинание  3-Защита  4-Бежать";
            if (controls.Length > width - 4) controls = "1-атака 2-магия 3-защ 4-бежать";
            int ctlPad = Math.Max(0, (width - 2 - controls.Length) / 2);
            string controlsLine = "│" + new string(' ', ctlPad) + controls + new string(' ', Math.Max(0, width - 2 - controls.Length - ctlPad)) + "│";
            _r.Write(0, bottomFrameRow, controlsLine, ConsoleColor.DarkGray, ConsoleColor.Black);
            _r.Write(0, bottomFrameBottom, bottomBottomFrame, ConsoleColor.DarkGray, ConsoleColor.Black);

            // Контент-область
            int contentTop = 3;
            int contentBottom = bottomFrameTop - 1; // inclusive
            int contentHeight = contentBottom - contentTop + 1;

            // Разметка зон
            int monsterNameRow = contentTop;
            int monsterHpRow = contentTop + 1;
            int monsterSpeedRow = contentTop + 2;

            int playerSpeedRow = contentBottom - 4;  // скорость игрока
            int playerHpTextRow = contentBottom - 3;
            int playerHpBarRow = contentBottom - 2;
            int statusRow = contentBottom - 1; // индикатор чьего хода

            // Очистка рабочей области
            _r.FillArea(1, contentTop, Math.Max(1, width - 2), contentHeight, ' ', ConsoleColor.White, ConsoleColor.Black);

            // --- MONSTER ---
            string monsterName = SafeGetString(monster, new[] { "Name", "name" }) ?? "Монстр";
            int mNameX = Math.Max(1, (width - monsterName.Length) / 2);
            _r.Write(mNameX, monsterNameRow, monsterName, ConsoleColor.Red, ConsoleColor.Black);

            int mCurrent = SafeGetInt(monster, new[] { "CurrentHP", "HP", "Health", "HitPoints" });
            int mMax = Math.Max(1, SafeGetInt(monster, new[] { "MaximumHP", "MaxHP", "MaxHealth", "HitPointsMax" }));
            int barWidth = Math.Min(48, width - Left - RightMargin - 12);
            barWidth = Math.Max(8, barWidth);
            int mBarX = Math.Max(Left, (width - barWidth) / 2);
            DrawBar(mBarX, monsterHpRow, barWidth, mCurrent, mMax, ConsoleColor.DarkMagenta, ConsoleColor.DarkGray);

            string mHpText = $"{mCurrent}/{mMax}";
            int mHpTextX = Math.Min(width - RightMargin - mHpText.Length, mBarX + barWidth + 2);
            _r.Write(mHpTextX, monsterHpRow, mHpText, ConsoleColor.Yellow, ConsoleColor.Black);

            // Monster speed (centered)
            int mSpeed = SafeGetInt(monster, new[] { "CurrentSpeed", "Speed" });
            int mSpeedBarWidth = Math.Max(6, barWidth / 3);
            int mSpeedX = Math.Max(Left, (width - mSpeedBarWidth) / 2);
            DrawSpeedBar(mSpeedX, monsterSpeedRow, mSpeedBarWidth, mSpeed, 100);
            string mSpeedNum = $"{mSpeed}/100";
            _r.Write(Math.Min(width - RightMargin - mSpeedNum.Length, mSpeedX + mSpeedBarWidth + 2), monsterSpeedRow, mSpeedNum, ConsoleColor.Cyan, ConsoleColor.Black);

            // --- PLAYER ---
            int pCurrent = SafeGetInt(player, new[] { "CurrentHP", "HP", "Health", "HitPoints" });
            int pMax = Math.Max(1, SafeGetInt(player, new[] { "MaximumHP", "MaxHP", "MaxHealth", "HitPointsMax" }));
            int pSpeed = SafeGetInt(player, new[] { "CurrentSpeed", "Speed" });

            int pBarWidth = Math.Min(48, width - Left - RightMargin - 12);
            pBarWidth = Math.Max(8, pBarWidth);
            int pBarX = Math.Max(Left, (width - pBarWidth) / 2);

            // Player speed: centered (исправление — раньше использовался pBarX, теперь центрируем по width)
            int pSpeedBarWidth = Math.Max(6, pBarWidth / 3);
            int pSpeedX = Math.Max(Left, (width - pSpeedBarWidth) / 2);
            DrawSpeedBar(pSpeedX, playerSpeedRow, pSpeedBarWidth, pSpeed, 100);
            string pSpeedNum = $"{pSpeed}/100";
            _r.Write(Math.Min(width - RightMargin - pSpeedNum.Length, pSpeedX + pSpeedBarWidth + 2), playerSpeedRow, pSpeedNum, ConsoleColor.Cyan, ConsoleColor.Black);

            string playerName = SafeGetString(player, new[] { "Name", "name" }) ?? "Игрок";
            string pHpLabel = $"{playerName}";
            int pHpLabelX = Math.Max(1, (width - pHpLabel.Length) / 2);
            _r.Write(pHpLabelX, playerHpTextRow, pHpLabel, ConsoleColor.Green, ConsoleColor.Black);

            DrawBar(pBarX, playerHpBarRow, pBarWidth, pCurrent, pMax, ConsoleColor.DarkMagenta, ConsoleColor.DarkGray);
            string pHpText = $"{pCurrent}/{pMax}";
            _r.Write(Math.Min(width - RightMargin - pHpText.Length, pBarX + pBarWidth + 2), playerHpBarRow, pHpText, ConsoleColor.Green, ConsoleColor.Black);

            // --- ИНДИКАТОР ЧЬЕГО ХОДА ---
            string ownership = null;
            ConsoleColor ownershipColor = ConsoleColor.White;

            if (isEnemyActing)
            {
                ownership = $"ХОД: {monsterName}";
                ownershipColor = ConsoleColor.Red;
            }
            else if (isMonsterPreparing)
            {
                ownership = $"Готовится: {monsterName}";
                ownershipColor = ConsoleColor.Cyan;
            }
            else if (isPlayerTurnReady)
            {
                ownership = "ВАШ ХОД";
                ownershipColor = ConsoleColor.Yellow;
            }
            else
            {
                if (mSpeed > pSpeed) { ownership = $"ХОД: {monsterName}"; ownershipColor = ConsoleColor.Red; }
                else if (pSpeed > mSpeed) { ownership = "ВАШ ХОД"; ownershipColor = ConsoleColor.Green; }
                else ownership = null;
            }

            if (!string.IsNullOrEmpty(ownership))
            {
                string outStr = ownership;
                if (outStr.Length > width - 4) outStr = outStr.Substring(0, width - 7) + "...";
                int ox = Math.Max(0, (width - outStr.Length) / 2);
                _r.Write(ox, statusRow, outStr, ownershipColor, ConsoleColor.Black);
            }
            else
            {
                _r.FillArea(1, statusRow, Math.Max(1, width - 2), 1, ' ', ConsoleColor.White, ConsoleColor.Black);
            }

            // --- ЛОГ: поднимаем на одну строку вверх и увеличиваем количество видимых сообщений ---
            int logTop = monsterSpeedRow + 1;     // поднимаем лог на одну строку вверх
            int logBottom = playerSpeedRow - 1;
            if (logBottom < logTop) logBottom = logTop;
            int logHeight = Math.Max(1, logBottom - logTop + 1);

            int maxVisible = logHeight; // используем все доступное пространство для лога

            var filtered = new List<string>();
            if (combatLog != null)
            {
                foreach (var line in combatLog)
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    var t = line.Trim();
                    if (t == "=== ВАШ ХОД ===") continue;
                    if (t.EndsWith("готовится атаковать...")) continue;
                    filtered.Add(line);
                }
            }

            int visibleCount = Math.Min(maxVisible, filtered.Count);
            
            // Если есть прокрутка, показываем с учетом смещения
            int start;
            if (logScrollOffset > 0)
            {
                start = Math.Max(0, filtered.Count - visibleCount - logScrollOffset);
            }
            else
            {
                start = Math.Max(0, filtered.Count - visibleCount);
            }

            // Показываем строки с учетом прокрутки
            for (int i = 0; i < visibleCount && start + i < filtered.Count; i++)
            {
                string line = filtered[start + i];
                if (line.Length > width - 6) line = line.Substring(0, width - 9) + "...";
                int lx = Math.Max(1, (width - line.Length) / 2);
                // Отображаем снизу вверх: logBottom - visibleCount + 1 + i
                int row = logBottom - visibleCount + 1 + i;
                _r.Write(lx, row, line, ConsoleColor.White, ConsoleColor.Black);
            }

            // Индикатор количества скрытых сообщений — размещаем прямо под скоростью противника
            if (filtered.Count > visibleCount)
            {
                string more = $"... {filtered.Count - visibleCount} пред.";
                int mx = Math.Max(1, (width - more.Length) / 2);
                int my = monsterSpeedRow + 1; // Размещаем прямо под скоростью противника
                if (my >= contentTop && my < logTop) 
                {
                    _r.Write(mx, my, more, ConsoleColor.DarkGray, ConsoleColor.Black);
                }
            }

            // Готово
        }

        // Рисует HP-полосу (рамка [ ] + цветная заливка)
        private void DrawBar(int left, int top, int width, int value, int maxValue, ConsoleColor filledBg, ConsoleColor emptyBg)
        {
            if (width < 4) width = 4;
            int inner = Math.Max(1, width - 2);
            int filled = (int)Math.Round((double)inner * value / Math.Max(1, maxValue));
            filled = Math.Clamp(filled, 0, inner);

            _r.Write(left, top, "[", ConsoleColor.Gray, ConsoleColor.Black);
            if (filled > 0)
                _r.FillArea(left + 1, top, filled, 1, ' ', filledBg, filledBg);

            int emptyLen = inner - filled;
            if (emptyLen > 0)
                _r.FillArea(left + 1 + filled, top, emptyLen, 1, ' ', ConsoleColor.White, emptyBg);

            _r.Write(left + width - 1, top, "]", ConsoleColor.Gray, ConsoleColor.Black);
        }

        // Рисует speed bar как '_' внутри скобок
        private void DrawSpeedBar(int left, int top, int width, int value, int maxValue)
        {
            if (width < 6) width = 6;
            int inner = Math.Max(1, width - 2);
            int filled = (int)Math.Round((double)inner * value / Math.Max(1, maxValue));
            filled = Math.Clamp(filled, 0, inner);

            _r.Write(left, top, "[", ConsoleColor.Gray, ConsoleColor.Black);
            if (filled > 0)
                _r.FillArea(left + 1, top, filled, 1, '_', ConsoleColor.Cyan, ConsoleColor.Black);
            int rest = inner - filled;
            if (rest > 0)
                _r.FillArea(left + 1 + filled, top, rest, 1, '_', ConsoleColor.DarkGray, ConsoleColor.Black);
            _r.Write(left + width - 1, top, "]", ConsoleColor.Gray, ConsoleColor.Black);
        }

        #region Safe getters

        private int SafeGetInt(object obj, string[] names)
        {
            if (obj == null) return 0;
            var t = obj.GetType();
            foreach (var name in names)
            {
                try
                {
                    var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        if (v is int i) return i;
                        if (v is long l) return (int)l;
                        if (v != null) { try { return Convert.ToInt32(v); } catch { } }
                    }
                    var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (f != null)
                    {
                        var v = f.GetValue(obj);
                        if (v is int i) return i;
                        if (v is long l) return (int)l;
                        if (v != null) { try { return Convert.ToInt32(v); } catch { } }
                    }
                }
                catch { }
            }
            return 0;
        }

        private string SafeGetString(object obj, string[] names)
        {
            if (obj == null) return null;
            var t = obj.GetType();
            foreach (var name in names)
            {
                try
                {
                    var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (p != null && p.PropertyType == typeof(string))
                        return p.GetValue(obj) as string;
                    var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                    if (f != null && f.FieldType == typeof(string))
                        return f.GetValue(obj) as string;
                }
                catch { }
            }
            return null;
        }

        #endregion
    }
}
