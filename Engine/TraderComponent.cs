using Engine;

public class TraderComponent
{
    public List<InventoryItem> ItemsForSale { get; set; }
    public int Gold { get; set; }
    public int BuyPriceModifier => 80; // 80% от базовой цены
    public int SellPriceModifier => 120; // 120% от базовой цены

    public TraderComponent()
    {
        ItemsForSale = new List<InventoryItem>();
    }

    public void InitializeShop(Player player)
    {
        // Логика инициализации магазина
    }

    public bool CanAfford(Item item, int quantity, Player player)
    {
        return player.Gold >= item.Price * quantity;
    }

    public string GetShopGreeting()
    {
        return "Добро пожаловать в мой магазин!";
    }
}
