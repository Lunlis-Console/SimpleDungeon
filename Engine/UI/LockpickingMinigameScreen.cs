using Engine.Core;
using Engine.Entities;
using System;

namespace Engine.UI
{
    public class LockpickingMinigameScreen : BaseScreen
    {
        private readonly Chest _chest;
        private readonly Player _player;
        private readonly LockpickComponent _lockpickComponent;
        private readonly InventoryItem _lockpickItem;
        
        // Параметры мини-игры
        private int _lockpickPosition = 50; // Позиция отмычки (0-100)
        private int _lockPosition = 50; // Позиция замка (0-100)
        private int _sweetSpotStart = 40; // Начало "сладкой зоны"
        private int _sweetSpotEnd = 60; // Конец "сладкой зоны"
        private int _lockpickSpeed = 2; // Скорость движения отмычки
        private int _lockSpeed = 1; // Скорость движения замка
        
        // Состояние игры
        private int _attempts = 0;
        private int _maxAttempts = 3;
        private bool _gameWon = false;
        private bool _gameLost = false;
        private int _lockDirection = 1; // Направление движения замка (1 или -1)
        private int _lockMoveTimer = 0; // Таймер для движения замка
        
        // Визуальные параметры
        private const int LOCKPICK_WIDTH = 20;
        private const int LOCK_WIDTH = 30;
        private const int SWEET_SPOT_WIDTH = 10;

        public LockpickingMinigameScreen(Chest chest, Player player, LockpickComponent lockpickComponent, InventoryItem lockpickItem)
        {
            _chest = chest ?? throw new ArgumentNullException(nameof(chest));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _lockpickComponent = lockpickComponent ?? throw new ArgumentNullException(nameof(lockpickComponent));
            _lockpickItem = lockpickItem ?? throw new ArgumentNullException(nameof(lockpickItem));
            
            // Рассчитываем сложность на основе уровня замка
            CalculateDifficulty();
        }

        private void CalculateDifficulty()
        {
            var difficulty = _chest.LockDifficulty;
            var playerSkill = _player.Skills.Lockpicking.Level;
            
            // Размер "сладкой зоны" зависит от сложности замка и навыка игрока
            int baseSweetSpotSize = difficulty switch
            {
                LockDifficulty.Simple => 20,
                LockDifficulty.Average => 15,
                LockDifficulty.Complex => 10,
                LockDifficulty.Master => 8,
                LockDifficulty.Legendary => 5,
                _ => 20
            };
            
            // Уменьшаем размер зоны в зависимости от навыка игрока
            int skillBonus = Math.Min(playerSkill / 10, 10); // Максимум +10 от навыка
            int finalSweetSpotSize = Math.Max(baseSweetSpotSize - skillBonus, 5);
            
            // Устанавливаем "сладкую зону" в случайном месте
            Random random = new Random();
            int sweetSpotCenter = random.Next(20, 80);
            _sweetSpotStart = Math.Max(0, sweetSpotCenter - finalSweetSpotSize / 2);
            _sweetSpotEnd = Math.Min(100, sweetSpotCenter + finalSweetSpotSize / 2);
            
            // Скорость замка зависит от сложности
            _lockSpeed = difficulty switch
            {
                LockDifficulty.Simple => 1,
                LockDifficulty.Average => 2,
                LockDifficulty.Complex => 3,
                LockDifficulty.Master => 4,
                LockDifficulty.Legendary => 5,
                _ => 1
            };
            
            // Максимальное количество попыток зависит от навыка
            _maxAttempts = Math.Max(1, 5 - playerSkill / 20);
        }

        public override void Update()
        {
            if (_gameWon || _gameLost) return;
            
            // Автоматическое движение замка
            _lockMoveTimer++;
            if (_lockMoveTimer >= 10) // Движение каждые 10 кадров
            {
                _lockPosition += _lockDirection * _lockSpeed;
                
                // Отражение от краев
                if (_lockPosition <= 0 || _lockPosition >= 100)
                {
                    _lockDirection *= -1;
                    _lockPosition = Math.Max(0, Math.Min(100, _lockPosition));
                }
                
                _lockMoveTimer = 0;
            }
        }

        public override void Render()
        {
            var renderer = GameServices.BufferedRenderer;
            if (renderer == null) return;

            renderer.BeginFrame();

            // Заголовок
            renderer.Write(2, 1, "=== ВЗЛОМ ЗАМКА ===", ConsoleColor.Yellow);
            renderer.Write(2, 2, $"Сундук: {_chest.Name}", ConsoleColor.Gray);
            renderer.Write(2, 3, $"Сложность: {LockDifficultyHelper.GetDifficultyDescription(_chest.LockDifficulty)}", ConsoleColor.Gray);
            renderer.Write(2, 4, $"Попытки: {_attempts}/{_maxAttempts}", ConsoleColor.Gray);

            // Рисуем замок
            RenderLock(renderer, 2, 6);
            
            // Рисуем отмычку
            RenderLockpick(renderer, 2, 8);
            
            // Статус игры
            if (_gameWon)
            {
                renderer.Write(2, 12, "✓ УСПЕХ! Замок взломан!", ConsoleColor.Green);
                renderer.Write(2, 13, "Нажмите любую клавишу для продолжения...", ConsoleColor.Gray);
            }
            else if (_gameLost)
            {
                renderer.Write(2, 12, "✗ НЕУДАЧА! Попытки исчерпаны.", ConsoleColor.Red);
                renderer.Write(2, 13, "Нажмите любую клавишу для продолжения...", ConsoleColor.Gray);
            }
            else
            {
                // Инструкции
                renderer.Write(2, 12, "Управление:", ConsoleColor.Cyan);
                renderer.Write(2, 13, "A/D - движение отмычки", ConsoleColor.Gray);
                renderer.Write(2, 14, "ПРОБЕЛ - попытка взлома", ConsoleColor.Gray);
                renderer.Write(2, 15, "ESC - отмена", ConsoleColor.Gray);
                
                // Статус
                if (IsInSweetSpot())
                {
                    renderer.Write(2, 17, "✓ Отмычка в правильной позиции!", ConsoleColor.Green);
                    renderer.Write(2, 18, "Нажмите ПРОБЕЛ для попытки взлома", ConsoleColor.Yellow);
                }
                else
                {
                    renderer.Write(2, 17, "✗ Отмычка не в правильной позиции", ConsoleColor.Red);
                    int distance = Math.Min(Math.Abs(_lockpickPosition - _sweetSpotStart), Math.Abs(_lockpickPosition - _sweetSpotEnd));
                    renderer.Write(2, 18, $"Расстояние до зоны: {distance}", ConsoleColor.Gray);
                }
                
                // Информация о замке
                renderer.Write(2, 20, $"Позиция замка: {_lockPosition}", ConsoleColor.Gray);
                renderer.Write(2, 21, $"Сладкая зона: {_sweetSpotStart}-{_sweetSpotEnd}", ConsoleColor.Gray);
            }

            renderer.EndFrame();
        }

        private void RenderLock(EnhancedBufferedRenderer renderer, int x, int y)
        {
            // Рисуем замок
            renderer.Write(x, y, "Замок:", ConsoleColor.Cyan);
            y++;
            
            // Рисуем шкалу замка
            string lockBar = new string('█', LOCK_WIDTH);
            renderer.Write(x, y, lockBar, ConsoleColor.DarkGray);
            
            // Рисуем "сладкую зону"
            int sweetSpotX = x + (_sweetSpotStart * LOCK_WIDTH / 100);
            int sweetSpotLength = ((_sweetSpotEnd - _sweetSpotStart) * LOCK_WIDTH / 100);
            if (sweetSpotLength > 0)
            {
                string sweetSpot = new string('█', sweetSpotLength);
                renderer.Write(sweetSpotX, y, sweetSpot, ConsoleColor.Green);
            }
            
            // Рисуем позицию замка
            int lockX = x + (_lockPosition * LOCK_WIDTH / 100);
            renderer.Write(lockX, y, "▲", ConsoleColor.Yellow);
        }

        private void RenderLockpick(EnhancedBufferedRenderer renderer, int x, int y)
        {
            // Рисуем отмычку
            renderer.Write(x, y, "Отмычка:", ConsoleColor.Cyan);
            y++;
            
            // Рисуем шкалу отмычки
            string lockpickBar = new string('█', LOCKPICK_WIDTH);
            renderer.Write(x, y, lockpickBar, ConsoleColor.DarkGray);
            
            // Рисуем позицию отмычки
            int lockpickX = x + (_lockpickPosition * LOCKPICK_WIDTH / 100);
            renderer.Write(lockpickX, y, "▼", ConsoleColor.Blue);
        }

        private bool IsInSweetSpot()
        {
            return _lockpickPosition >= _sweetSpotStart && _lockpickPosition <= _sweetSpotEnd;
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (_gameWon || _gameLost)
            {
                // Игра завершена, любая клавиша закрывает экран
                ScreenManager.PopScreen();
                return;
            }

            switch (keyInfo.Key)
            {
                case ConsoleKey.A:
                    MoveLockpick(-_lockpickSpeed);
                    break;
                case ConsoleKey.D:
                    MoveLockpick(_lockpickSpeed);
                    break;
                case ConsoleKey.Spacebar:
                    AttemptPick();
                    break;
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    return;
            }
        }

        private void MoveLockpick(int delta)
        {
            _lockpickPosition = Math.Max(0, Math.Min(100, _lockpickPosition + delta));
        }

        private void AttemptPick()
        {
            _attempts++;
            
            // Проверяем, находится ли отмычка в "сладкой зоне"
            bool inSweetSpot = IsInSweetSpot();
            
            if (inSweetSpot)
            {
                // Успех!
                _gameWon = true;
                _chest.IsLocked = false;
                
                // Используем отмычку
                _lockpickComponent.Use();
                
                // Даем опыт
                int experienceGained = CalculateExperienceGain(_chest.LockDifficulty);
                _player.Skills.GainExperience("lockpicking", experienceGained);
                
                MessageSystem.AddMessage($"Вы успешно взломали {_chest.Name}!");
                MessageSystem.AddMessage($"Получено опыта взлома: {experienceGained}");
                
                // Проверяем, сломалась ли отмычка
                if (_lockpickComponent.IsBroken)
                {
                    MessageSystem.AddMessage($"{_lockpickItem.Details.Name} сломалась!");
                    _player.Inventory.RemoveItem(_lockpickItem, 1);
                }
                
                // Открываем сундук
                _chest.OpenChest(_player);
            }
            else
            {
                // Неудача
                MessageSystem.AddMessage($"Попытка неудачна! Осталось попыток: {_maxAttempts - _attempts}");
                
                // Используем отмычку
                _lockpickComponent.Use();
                
                // Даем небольшой опыт
                _player.Skills.GainExperience("lockpicking", 1);
                
                // Проверяем, сломалась ли отмычка
                if (_lockpickComponent.IsBroken)
                {
                    MessageSystem.AddMessage($"{_lockpickItem.Details.Name} сломалась!");
                    _player.Inventory.RemoveItem(_lockpickItem, 1);
                    _gameLost = true;
                    return;
                }
                
                if (_attempts >= _maxAttempts)
                {
                    _gameLost = true;
                    MessageSystem.AddMessage($"Взлом не удался. Попытки исчерпаны.");
                }
                else
                {
                    // Случайно меняем позицию замка и "сладкой зоны"
                    Random random = new Random();
                    _lockPosition = random.Next(0, 100);
                    
                    // Немного смещаем "сладкую зону"
                    int sweetSpotCenter = random.Next(20, 80);
                    int sweetSpotSize = _sweetSpotEnd - _sweetSpotStart;
                    _sweetSpotStart = Math.Max(0, sweetSpotCenter - sweetSpotSize / 2);
                    _sweetSpotEnd = Math.Min(100, sweetSpotCenter + sweetSpotSize / 2);
                }
            }
        }

        private int CalculateExperienceGain(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => 1,
                LockDifficulty.Average => 2,
                LockDifficulty.Complex => 3,
                LockDifficulty.Master => 4,
                LockDifficulty.Legendary => 5,
                _ => 1
            };
        }
    }
}
