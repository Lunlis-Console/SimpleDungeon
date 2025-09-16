using Engine.Core;
using Engine.Entities;
using Engine.UI;
using System;
using System.Collections.Generic;

namespace Engine.Combat
{
    public class CombatScreen : BaseScreen
    {
        private readonly CombatEngine _engine;
        private int _logScrollOffset = 0;
        private bool _showingRewards = false;
        private List<string> _rewardInfo = new List<string>();

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
            ClearScreen();

            string headerText = _showingRewards ? 
                $"БОЙ ЗАВЕРШЕН: {SafeGetString(_engine.Monster, new[] { "Name", "name" }) ?? "Монстр"}  —  {SafeGetString(_engine.Player, new[] { "Name", "name" }) ?? "Игрок"}" :
                $"БОЙ: {SafeGetString(_engine.Monster, new[] { "Name", "name" }) ?? "Монстр"}  —  {SafeGetString(_engine.Player, new[] { "Name", "name" }) ?? "Игрок"}";
            
            RenderHeader(headerText);

            // Делегируем отрисовку игрового содержимого CombatRenderer'у
            GameServices.CombatRenderer.RenderCombatFrame(
                _engine.Player,
                _engine.Monster,
                _engine.GetCombatLog(),
                _engine.CurrentTurn,
                _engine.IsPlayerTurnReady,
                _engine.IsEnemyActing,
                false, // isMonsterPreparing
                _logScrollOffset // logScrollOffset
            );

            // Если бой завершен, показываем информацию о наградах справа
            if (_showingRewards)
            {
                RenderRewardsInfo();
            }
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

        private int GetIntSafe(object obj, string[] names)
        {
            if (obj == null) return 0;
            var t = obj.GetType();
            foreach (var name in names)
            {
                var p = t.GetProperty(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (p != null && p.PropertyType == typeof(int))
                    return (int)p.GetValue(obj);
                var f = t.GetField(name, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
                if (f != null && f.FieldType == typeof(int))
                    return (int)f.GetValue(obj);
            }
            return 0;
        }

        private void CollectRewardInfo()
        {
            _rewardInfo.Clear();
            
            if (_engine.Monster != null)
            {
                int gold = GetIntSafe(_engine.Monster, new[] { "RewardGold" });
                int exp = GetIntSafe(_engine.Monster, new[] { "RewardEXP" });
                
                _rewardInfo.Add("НАГРАДЫ:");
                _rewardInfo.Add($"Золото: {gold}");
                _rewardInfo.Add($"Опыт: {exp}");
                _rewardInfo.Add("");
                
                // Получаем информацию о дропе
                var loot = _engine.Monster.GetLoot();
                if (loot.Count > 0)
                {
                    _rewardInfo.Add("ДОБЫЧА:");
                    foreach (var item in loot)
                    {
                        _rewardInfo.Add($"• {item.Name}");
                    }
                }
                else
                {
                    _rewardInfo.Add("ДОБЫЧА:");
                    _rewardInfo.Add("Нет предметов");
                }
            }
        }

        private void RenderRewardsInfo()
        {
            int width = GameServices.BufferedRenderer.Width;
            int height = GameServices.BufferedRenderer.Height;
            
            // Размещаем информацию о наградах справа
            int startX = Math.Max(width * 2 / 3, width - 30);
            int startY = 3;
            
            // Рисуем рамку для информации о наградах
            int boxWidth = width - startX - 1;
            int boxHeight = Math.Min(_rewardInfo.Count + 2, height - 4);
            
            // Верхняя и нижняя границы
            string topBorder = "┌" + new string('─', boxWidth - 2) + "┐";
            string bottomBorder = "└" + new string('─', boxWidth - 2) + "┘";
            
            GameServices.BufferedRenderer.Write(startX, startY, topBorder, ConsoleColor.DarkGray, ConsoleColor.Black);
            GameServices.BufferedRenderer.Write(startX, startY + boxHeight - 1, bottomBorder, ConsoleColor.DarkGray, ConsoleColor.Black);
            
            // Боковые границы
            for (int i = 1; i < boxHeight - 1; i++)
            {
                GameServices.BufferedRenderer.Write(startX, startY + i, "│", ConsoleColor.DarkGray, ConsoleColor.Black);
                GameServices.BufferedRenderer.Write(startX + boxWidth - 1, startY + i, "│", ConsoleColor.DarkGray, ConsoleColor.Black);
            }
            
            // Содержимое
            int contentLines = Math.Min(_rewardInfo.Count, boxHeight - 4); // Оставляем место для подсказок
            for (int i = 0; i < contentLines; i++)
            {
                string line = _rewardInfo[i];
                if (line.Length > boxWidth - 4)
                {
                    line = line.Substring(0, boxWidth - 7) + "...";
                }
                
                ConsoleColor color = ConsoleColor.White;
                if (line.StartsWith("НАГРАДЫ:") || line.StartsWith("ДОБЫЧА:"))
                    color = ConsoleColor.Cyan;
                else if (line.StartsWith("•"))
                    color = ConsoleColor.Green;
                else if (line.Contains("Золото:") || line.Contains("Опыт:"))
                    color = ConsoleColor.Yellow;
                
                GameServices.BufferedRenderer.Write(startX + 2, startY + 1 + i, line, color, ConsoleColor.Black);
            }
            
            // Подсказки управления
            int hintY = startY + boxHeight - 3;
            GameServices.BufferedRenderer.Write(startX + 2, hintY, "W/S: прокрутка лога", ConsoleColor.DarkGray, ConsoleColor.Black);
            GameServices.BufferedRenderer.Write(startX + 2, hintY + 1, "E: принять", ConsoleColor.DarkGray, ConsoleColor.Black);
        }

        public override void Update()
        {
            _engine.UpdateFrame();

            if (_engine.IsCombatOver && !_showingRewards)
            {
                // Проверяем, победил ли игрок (монстр мертв, а игрок жив)
                if (_engine.Player != null && _engine.Monster != null)
                {
                    int playerHP = GetIntSafe(_engine.Player, new[] { "CurrentHP" });
                    int monsterHP = GetIntSafe(_engine.Monster, new[] { "CurrentHP" });
                    
                    // Если игрок жив, а монстр мертв - выдаем награду и собираем информацию
                    if (playerHP > 0 && monsterHP <= 0)
                    {
                        CollectRewardInfo();
                        _engine.Player.RecieveReward(_engine.Monster);
                    }
                }

                // Очистка игровых ссылок на монстра (на всякий случай)
                if (_engine.Player != null)
                {
                    _engine.Player.CurrentMonster = null;
                    _engine.Player.IsInCombat = false;
                }

                _showingRewards = true;
                RequestFullRedraw();
                return;
            }

            RequestPartialRedraw();
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            // Если бой завершен, обрабатываем только навигацию и принятие результатов
            if (_showingRewards)
            {
                switch (keyInfo.Key)
                {
                    case ConsoleKey.E:
                        // Принимаем результаты и возвращаемся на экран мира
                        ScreenManager.PopUntil<GameWorldScreen>();
                        ScreenManager.RequestFullRedraw();
                        break;
                        
                    case ConsoleKey.W:
                        // Прокрутка лога вверх
                        if (_logScrollOffset > 0)
                        {
                            _logScrollOffset--;
                            RequestFullRedraw();
                        }
                        break;
                        
                    case ConsoleKey.S:
                        // Прокрутка лога вниз
                        var combatLog = _engine.GetCombatLog();
                        int maxScroll = Math.Max(0, combatLog.Count - 10); // Показываем 10 строк
                        if (_logScrollOffset < maxScroll)
                        {
                            _logScrollOffset++;
                            RequestFullRedraw();
                        }
                        break;
                }
                return;
            }

            // Во время боя нельзя выйти на Q - только сбежать или победить
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
