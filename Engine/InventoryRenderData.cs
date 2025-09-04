using Engine;

public class InventoryRenderData
{
    public List<object> Items { get; set; }
    public int SelectedIndex { get; set; }
    public Player Player { get; set; }
    public string Title { get; set; }

    // Вспомогательные свойства для удобства доступа
    public Inventory Inventory => Player.Inventory;
    public int Gold => Player.Gold;
    public int Defence => Player.Defence;
    public int Attack => Player.Attack;
    public int Agility => Player.Agility;
    public int Level => Player.Level;
    public int CurrentEXP => Player.CurrentEXP;
    public int MaximumEXP => Player.MaximumEXP;
    public int CurrentHP => Player.CurrentHP;
    public int TotalMaximumHP => Player.TotalMaximumHP;

    // Экипированные предметы для удобного доступа
    public Equipment MainHand => Inventory.MainHand;
    public Equipment OffHand => Inventory.OffHand;
    public Equipment Helmet => Inventory.Helmet;
    public Equipment Armor => Inventory.Armor;
    public Equipment Gloves => Inventory.Gloves;
    public Equipment Boots => Inventory.Boots;
    public Equipment Amulet => Inventory.Amulet;
    public Equipment Ring1 => Inventory.Ring1;
    public Equipment Ring2 => Inventory.Ring2;
}