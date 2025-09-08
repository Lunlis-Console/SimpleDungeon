using Engine.Entities;

namespace Engine.Trading
{
    public class Merchant : ITrader
    {
        public string Name { get; set; }
        public string ShopGreeting { get; set; }
        public List<InventoryItem> ItemsForSale { get; set; }
        public int Gold { get; set; }
        public int BuyPriceModifier => 100;
        public int SellPriceModifier => 80;

        public Merchant(string name, string shopGreeting, int startingGold = 1000)
        {
            Name = name;
            ShopGreeting = shopGreeting;
            Gold = startingGold;
            ItemsForSale = new List<InventoryItem>();
        }

        public void InitializeShop(Player player)
        {
            // Базовая инициализация магазина
            if (ItemsForSale.Count == 0)
            {
                // Можно добавить базовые товары здесь
            }
        }

        public bool CanAfford(Item item, int quantity, Player player)
        {
            int totalPrice = item.Price * BuyPriceModifier / 100 * quantity;
            return player.Gold >= totalPrice;
        }
    }
}