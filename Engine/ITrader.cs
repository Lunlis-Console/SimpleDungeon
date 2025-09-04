namespace Engine
{
    public interface ITrader
    {
        string Name { get; }
        string ShopGreeting { get; }
        List<InventoryItem> ItemsForSale { get; set; }
        int Gold { get; set; }
        int BuyPriceModifier { get; }
        int SellPriceModifier { get; }

        void InitializeShop(Player player);
        bool CanAfford(Item item, int quantity, Player player);
    }
}