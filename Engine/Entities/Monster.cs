using Engine.Core;
using Engine.Entities;

public class Monster : LivingCreature, IInteractable
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
        // РАСЧЕТ ШАНСА УКЛОНЕНИЯ ДЛЯ МОНСТРА:
        // База (5%) + 1% за каждый уровень + 1% за каждые 3 ед. Ловкости
        EvasionChance = 5 + Level + (Attributes.Dexterity / 3);
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

        // РАСЧЕТ ШАНСА УКЛОНЕНИЯ ДЛЯ МАСШТАБИРОВАННОГО МОНСТРА
        EvasionChance = 5 + newLevel + (baseMonster.Attributes.Dexterity / 3);
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

    // ... остальной код класса Monster ...

    // Реализация расширенного интерфейса IInteractable
    public List<string> GetAvailableActions(Player player)
    {
        var actions = new List<string> { "Атаковать", "Осмотреть" };
        // В будущем можно добавить другие действия, например:
        // if (player.HasSkill("Stealth")) { actions.Add("Подкрасться"); }
        return actions;
    }

    public void ExecuteAction(Player player, string action)
    {
        switch (action)
        {
            case "Атаковать":
                player.StartCombat(this);
                break;
            case "Осмотреть":
                Examine(player);
                break;
            default:
                MessageSystem.AddMessage("Неизвестное действие.");
                break;
        }
    }

    // Новый метод для осмотра монстра
    // Новый метод для осмотра монстра
    public void Examine(Player player)
    {
        // Создаем цикл, который будет показывать информацию и затем СНОВА меню действий
        bool isExamining = true;

        while (isExamining)
        {
            Console.Clear();
            Console.WriteLine($"============ ОСМОТР: {Name} ============");
            Console.WriteLine($"Уровень: {Level}");
            Console.WriteLine($"Здоровье: {CurrentHP}/{MaximumHP}");
            Console.WriteLine($"Атака: ~{Attack}");
            Console.WriteLine($"Защита: ~{Defence}");
            Console.WriteLine($"Ловкость: ~{Agility}");
            Console.WriteLine($"Награда: {RewardEXP} опыта, {RewardGold} золота");

            Console.WriteLine("\n[Нажмите любую клавишу чтобы вернуться к выбору действия...]");
            Console.ReadKey();

            // После просмотра информации снова показываем меню действий для этого монстра
            // Для этого нам нужно выйти из цикла осмотра и позволить методу ExecuteAction завершиться,
            // но так как он вызывается извне, нам нужно изменить саму логику вызова.

            // Вместо этого, лучший способ - изменить метод ExecuteAction в коде, который его вызывает.
            // Но так как мы не можем изменить вызывающий код прямо здесь, мы изменим логику:
            // Мы просто выходим из цикла, и управление возвращается в меню выбора действия.
            isExamining = false;
        }
    }
}