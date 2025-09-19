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
        
        // Параметры новой мини-игры
        private int _sliderPosition = 50; // Позиция ползунка (0-100)
        private int _sliderSpeed = 3; // Скорость движения ползунка
        
        // Невидимая зона взлома
        private int _lockZoneStart = 40; // Начало зоны взлома
        private int _lockZoneEnd = 60; // Конец зоны взлома
        
        // Шкала прогресса взлома
        private float _progressFill = 0f; // Текущий прогресс (0.0-1.0)
        private float _progressSpeed = 0.02f; // Скорость заполнения при правильной позиции
        private float _progressDecaySpeed = 0.05f; // Скорость обнуления при отпускании
        
        // Состояние зажатия пробела
        private bool _isSpacePressed = false;
        
        // Состояние игры
        private bool _gameWon = false;
        private bool _gameLost = false;
        private bool _lockpickBroken = false;
        
        // Параметры поломки отмычки
        private float _lockpickStress = 0f; // Напряжение отмычки (0.0-1.0)
        private float _stressIncreaseRate = 0.01f; // Скорость увеличения напряжения
        private float _stressDecreaseRate = 0.005f; // Скорость уменьшения напряжения
        
        // Новая механика взлома
        private float _progressAcceleration = 0.001f; // Ускорение заполнения прогресса
        private float _currentProgressSpeed = 0f; // Текущая скорость заполнения
        private float _maxProgressSpeed = 0.05f; // Максимальная скорость заполнения
        private float _breakThreshold = 0.10f; // Порог поломки для неправильной позиции (10%)
        
        // Визуальные параметры
        private const int SLIDER_BAR_WIDTH = 60;
        private const int PROGRESS_BAR_WIDTH = 40;

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
            
            // Размер невидимой зоны взлома зависит от сложности замка и навыка игрока
            int baseZoneSize = difficulty switch
            {
                LockDifficulty.Simple => 25,
                LockDifficulty.Average => 18,
                LockDifficulty.Complex => 12,
                LockDifficulty.Master => 8,
                LockDifficulty.Legendary => 5,
                _ => 25
            };
            
            // Увеличиваем размер зоны в зависимости от навыка игрока
            int skillBonus = Math.Min(playerSkill / 15, 8); // Максимум +8 от навыка
            int finalZoneSize = Math.Max(baseZoneSize + skillBonus, 5);
            
            // Устанавливаем зону взлома в случайном месте
            Random random = new Random();
            int zoneCenter = random.Next(15, 85);
            _lockZoneStart = Math.Max(0, zoneCenter - finalZoneSize / 2);
            _lockZoneEnd = Math.Min(100, zoneCenter + finalZoneSize / 2);
            
            // Скорость заполнения прогресса зависит от сложности
            _progressSpeed = difficulty switch
            {
                LockDifficulty.Simple => 0.03f,
                LockDifficulty.Average => 0.025f,
                LockDifficulty.Complex => 0.02f,
                LockDifficulty.Master => 0.015f,
                LockDifficulty.Legendary => 0.01f,
                _ => 0.03f
            };
            
            // Скорость увеличения напряжения отмычки зависит от сложности
            _stressIncreaseRate = difficulty switch
            {
                LockDifficulty.Simple => 0.008f,
                LockDifficulty.Average => 0.01f,
                LockDifficulty.Complex => 0.012f,
                LockDifficulty.Master => 0.015f,
                LockDifficulty.Legendary => 0.018f,
                _ => 0.008f
            };
        }

        public override void Update()
        {
            if (_gameWon || _gameLost) return;
            
            // Если пробел зажат, обновляем прогресс и напряжение отмычки
            if (_isSpacePressed)
            {
                UpdateProgressAndStress();
            }
            else
            {
                // Если пробел отпущен, прогресс обнуляется
                if (_progressFill > 0)
                {
                    _progressFill = Math.Max(0, _progressFill - _progressDecaySpeed);
                }
                
                // Сбрасываем скорость заполнения
                _currentProgressSpeed = 0f;
                
                // Напряжение отмычки постепенно уменьшается
                if (_lockpickStress > 0)
                {
                    _lockpickStress = Math.Max(0, _lockpickStress - _stressDecreaseRate);
                }
            }
            
            // Проверяем условия победы и поражения
            CheckGameConditions();
        }
        
        private void UpdateProgressAndStress()
        {
            bool inLockZone = IsInLockZone();
            bool nearLockZone = IsNearLockZone();
            
            if (inLockZone)
            {
                // В правильной зоне - полоса заполняется до 100%
                _currentProgressSpeed = Math.Min(_maxProgressSpeed, _currentProgressSpeed + _progressAcceleration);
                _progressFill = Math.Min(1.0f, _progressFill + _currentProgressSpeed);
                
                // Минимальное напряжение отмычки
                _lockpickStress = Math.Min(1.0f, _lockpickStress + _stressIncreaseRate * 0.1f);
            }
            else if (nearLockZone)
            {
                // Рядом с зоной - полоса заполняется медленнее, но больше чем на 5%
                _currentProgressSpeed = Math.Min(_maxProgressSpeed * 0.7f, _currentProgressSpeed + _progressAcceleration * 0.5f);
                _progressFill = Math.Min(1.0f, _progressFill + _currentProgressSpeed);
                
                // Умеренное напряжение отмычки
                _lockpickStress = Math.Min(1.0f, _lockpickStress + _stressIncreaseRate * 0.5f);
                
                // Проверяем, не достигли ли порога поломки
                if (_progressFill >= 0.3f) // Рядом с зоной можно заполнить до 30%
                {
                    _lockpickBroken = true;
                    _gameLost = true;
                }
            }
            else
            {
                // Вне зоны - полоса заполняется только на 5%, затем отмычка ломается
                _currentProgressSpeed = Math.Min(_maxProgressSpeed * 0.3f, _currentProgressSpeed + _progressAcceleration * 0.2f);
                _progressFill = Math.Min(_breakThreshold, _progressFill + _currentProgressSpeed);
                
                // Быстрое увеличение напряжения отмычки
                _lockpickStress = Math.Min(1.0f, _lockpickStress + _stressIncreaseRate * 1.5f);
                
                // Проверяем, не достигли ли порога поломки
                if (_progressFill >= _breakThreshold)
                {
                    _lockpickBroken = true;
                    _gameLost = true;
                }
            }
        }
        
        private void CheckGameConditions()
        {
            // Проверяем победу
            if (_progressFill >= 1.0f)
            {
                _gameWon = true;
                return;
            }
            
            // Проверяем поломку отмычки
            if (_lockpickStress >= 1.0f)
            {
                _lockpickBroken = true;
                _gameLost = true;
                BreakLockpick();
                return;
            }
        }

        public override void Render()
        {
            var renderer = GameServices.BufferedRenderer;
            if (renderer == null) return;

            renderer.BeginFrame();

            // Центрируем элементы по горизонтали
            int centerX = Console.WindowWidth / 2;
            int centerY = Console.WindowHeight / 2;
            
            // Основная полоса с ползунком (в центре экрана)
            RenderSliderBar(renderer, centerX, centerY - 1);
            
            // Шкала прогресса взлома (рядом с полосой ползунка)
            RenderProgressBar(renderer, centerX, centerY + 1);
            
            // Статус игры
            if (_gameWon)
            {
                string successText = "УСПЕХ! Замок взломан!";
                string continueText = "Нажмите любую клавишу для продолжения...";
                renderer.Write(centerX - successText.Length / 2, centerY + 3, successText, ConsoleColor.Green);
                renderer.Write(centerX - continueText.Length / 2, centerY + 4, continueText, ConsoleColor.Gray);
            }
            else if (_gameLost)
            {
                if (_lockpickBroken)
                {
                    string brokenText = "ОТМЫЧКА СЛОМАНА!";
                    string continueText = "Нажмите любую клавишу для продолжения...";
                    renderer.Write(centerX - brokenText.Length / 2, centerY + 3, brokenText, ConsoleColor.Red);
                    renderer.Write(centerX - continueText.Length / 2, centerY + 4, continueText, ConsoleColor.Gray);
                }
                else
                {
                    string failText = "НЕУДАЧА!";
                    string continueText = "Нажмите любую клавишу для продолжения...";
                    renderer.Write(centerX - failText.Length / 2, centerY + 3, failText, ConsoleColor.Red);
                    renderer.Write(centerX - continueText.Length / 2, centerY + 4, continueText, ConsoleColor.Gray);
                }
            }
            else
            {
                // Минимальные инструкции
                string instruction1 = "A/D - движение ползунка";
                string instruction2 = "ПРОБЕЛ - начать/остановить взлом";
                string instruction3 = "ESC - отмена";
                renderer.Write(centerX - instruction1.Length / 2, centerY + 3, instruction1, ConsoleColor.Gray);
                renderer.Write(centerX - instruction2.Length / 2, centerY + 4, instruction2, ConsoleColor.Gray);
                renderer.Write(centerX - instruction3.Length / 2, centerY + 5, instruction3, ConsoleColor.Gray);
            }

            renderer.EndFrame();
        }

        private void RenderSliderBar(EnhancedBufferedRenderer renderer, int centerX, int y)
        {
            // Рисуем полосу
            string sliderBar = new string('─', SLIDER_BAR_WIDTH);
            int startX = centerX - SLIDER_BAR_WIDTH / 2;
            renderer.Write(startX, y, sliderBar, ConsoleColor.DarkGray);
            
            // Рисуем ползунок
            int sliderX = startX + (_sliderPosition * SLIDER_BAR_WIDTH / 100);
            renderer.Write(sliderX, y, "▲", ConsoleColor.Blue);
        }

        private void RenderProgressBar(EnhancedBufferedRenderer renderer, int centerX, int y)
        {
            // Рисуем фон шкалы
            string progressBarBg = new string('█', PROGRESS_BAR_WIDTH);
            int startX = centerX - PROGRESS_BAR_WIDTH / 2;
            renderer.Write(startX, y, progressBarBg, ConsoleColor.DarkGray);
            
            // Рисуем заполненную часть
            int filledWidth = (int)(_progressFill * PROGRESS_BAR_WIDTH);
            if (filledWidth > 0)
            {
                string progressBarFill = new string('█', filledWidth);
                ConsoleColor fillColor = _progressFill >= 1.0f ? ConsoleColor.Green : ConsoleColor.Yellow;
                renderer.Write(startX, y, progressBarFill, fillColor);
            }
        }

        private bool IsInLockZone()
        {
            return _sliderPosition >= _lockZoneStart && _sliderPosition <= _lockZoneEnd;
        }
        
        private bool IsNearLockZone()
        {
            int tolerance = 5; // Зона рядом с основной зоной
            return (_sliderPosition >= _lockZoneStart - tolerance && _sliderPosition <= _lockZoneEnd + tolerance) && !IsInLockZone();
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            if (_gameWon)
            {
                // При победе любая клавиша открывает сундук
                CompleteLockpicking();
                return;
            }
            
            if (_gameLost)
            {
                // Игра завершена, любая клавиша закрывает экран
                ScreenManager.PopScreen();
                return;
            }

            switch (keyInfo.Key)
            {
                case ConsoleKey.A:
                    MoveSlider(-_sliderSpeed);
                    break;
                case ConsoleKey.D:
                    MoveSlider(_sliderSpeed);
                    break;
                case ConsoleKey.Spacebar:
                    // Пробел - переключатель взлома
                    _isSpacePressed = !_isSpacePressed;
                    break;
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    return;
            }
        }
        
        private void MoveSlider(int delta)
        {
            _sliderPosition = Math.Max(0, Math.Min(100, _sliderPosition + delta));
        }
        
        private void CompleteLockpicking()
        {
            // Успешный взлом замка
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
            
            // Закрываем экран взлома замков
            ScreenManager.PopScreen();
            
            // Открываем сундук
            _chest.OpenChest(_player);
        }
        
        private void BreakLockpick()
        {
            // Отмычка сломалась
            MessageSystem.AddMessage($"{_lockpickItem.Details.Name} сломалась от напряжения!");
            _player.Inventory.RemoveItem(_lockpickItem, 1);
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
