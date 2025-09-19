using Engine.Core;

namespace Engine.Entities
{
    public class LockpickComponent : IItemComponent
    {
        public string ComponentType => "Lockpick";
        public int LockpickBonus { get; set; }
        public int Durability { get; set; }
        public int MaxDurability { get; set; }
        public int DifficultyReduction { get; set; }
        public bool IsConsumable { get; set; }

        public LockpickComponent(int lockpickBonus = 0, int durability = 10, int difficultyReduction = 0, bool isConsumable = true)
        {
            LockpickBonus = lockpickBonus;
            Durability = durability;
            MaxDurability = durability;
            DifficultyReduction = difficultyReduction;
            IsConsumable = isConsumable;
        }

        // Конструктор без параметров для JSON десериализации
        public LockpickComponent()
        {
            LockpickBonus = 0;
            Durability = 10;
            MaxDurability = 10;
            DifficultyReduction = 0;
            IsConsumable = true;
        }

        public void Use()
        {
            // Убеждаемся, что MaxDurability правильно установлен
            if (MaxDurability == 0 && Durability > 0)
            {
                MaxDurability = Durability;
            }
            
            if (IsConsumable && Durability > 0)
            {
                Durability--;
            }
        }

        public bool IsBroken => Durability <= 0;

        public string GetDescription()
        {
            var desc = $"Бонус к взлому: +{LockpickBonus}";
            if (IsConsumable)
            {
                desc += $"\nПрочность: {Durability}/{MaxDurability}";
            }
            if (DifficultyReduction > 0)
            {
                desc += $"\nСнижает сложность замка на {DifficultyReduction}";
            }
            return desc;
        }
    }
}
