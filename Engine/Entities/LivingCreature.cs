namespace Engine.Entities
{
    public class LivingCreature
    {
        public int CurrentHP { get; set; }
        public int MaximumHP { get; set; }
        public Attributes Attributes { get; set; }
        public int EvasionChance { get; set; } // Шанс уклонения в процентах

        public LivingCreature(int currentHP, int maximumHP, Attributes attributes = null)
        {
            CurrentHP = currentHP;
            MaximumHP = maximumHP;
            Attributes = attributes ?? new Attributes();
            EvasionChance = 0; // По умолчанию 0%
        }
    }
}
