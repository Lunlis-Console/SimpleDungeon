using Engine;

public class Monster : LivingCreature
{
    public int ID { get; set; }
    public string Name { get; set; }
    public int Level { get; set; }
    public int RewardEXP { get; set; }
    public int RewardGold { get; set; }
    public List<LootItem> LootTable { get; set; }
    public int CurrentSpeed { get; set; }

    // Вычисляемые свойства на основе атрибутов
    public int Attack => (Attributes.Strength / 2) + (Level / 2);
    public int Defence => (Attributes.Constitution / 2) + (Level / 3);
    public int Agility => Attributes.Dexterity + (Level / 4);

    public Monster(int id, string name, int level, int currentHP, int maximumHP,
                  int rewardEXP, int rewardGold, Attributes attributes = null) :
            base(currentHP, maximumHP, attributes)
    {
        ID = id;
        Name = name;
        Level = level;
        RewardEXP = rewardEXP;
        RewardGold = rewardGold;
        CurrentSpeed = 0;
        LootTable = new List<LootItem>();
    }

    public Monster(Monster baseMonster, int newLevel) :
        base(CalculateScaledHP(baseMonster, newLevel), CalculateScaledHP(baseMonster, newLevel),
             baseMonster.Attributes) // Передаем атрибуты базового монстра
    {
        ID = baseMonster.ID;
        Name = baseMonster.Name;
        Level = newLevel;
        RewardEXP = (int)(baseMonster.RewardEXP * (1 + (newLevel - baseMonster.Level) * 0.5));
        RewardGold = (int)(baseMonster.RewardGold * (1 + (newLevel - baseMonster.Level) * 0.3));
        CurrentSpeed = 0;
        LootTable = new List<LootItem>(baseMonster.LootTable);
    }

    private static int CalculateScaledHP(Monster baseMonster, int newLevel)
    {
        double levelRatio = (double)newLevel / baseMonster.Level;
        return (int)(baseMonster.MaximumHP * levelRatio * 1.1);
    }

    public List<Item> GetLoot()
    {
        List<Item> loot = new List<Item>();
        Random random = new Random();

        foreach (LootItem lootItem in LootTable)
        {
            if (random.Next(100) < lootItem.DropPercentage)
            {
                loot.Add(lootItem.Details);
            }

            if (lootItem.IsUnique)
            {
                LootTable.Remove(lootItem);
            }
        }
        return loot;
    }
}