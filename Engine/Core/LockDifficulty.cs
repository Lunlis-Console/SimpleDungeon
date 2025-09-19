namespace Engine.Core
{
    public enum LockDifficulty
    {
        Simple = 1,      // Простой замок - базовый уровень
        Average = 2,     // Обычный замок - средний уровень  
        Complex = 3,    // Сложный замок - высокий уровень
        Master = 4,     // Мастерский замок - очень высокий уровень
        Legendary = 5   // Легендарный замок - максимальный уровень
    }

    public static class LockDifficultyHelper
    {
        public static int GetDifficultyValue(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => 8,
                LockDifficulty.Average => 12,
                LockDifficulty.Complex => 16,
                LockDifficulty.Master => 20,
                LockDifficulty.Legendary => 25,
                _ => 10
            };
        }

        public static string GetDifficultyDescription(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => "Простой замок",
                LockDifficulty.Average => "Обычный замок",
                LockDifficulty.Complex => "Сложный замок",
                LockDifficulty.Master => "Мастерский замок",
                LockDifficulty.Legendary => "Легендарный замок",
                _ => "Неизвестный замок"
            };
        }

        public static string GetDifficultyColor(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => "Зеленый",
                LockDifficulty.Average => "Желтый",
                LockDifficulty.Complex => "Оранжевый",
                LockDifficulty.Master => "Красный",
                LockDifficulty.Legendary => "Фиолетовый",
                _ => "Серый"
            };
        }
    }
}
