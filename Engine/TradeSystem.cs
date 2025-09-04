namespace Engine
{
    public class TradeSystem
    {
        private readonly ITrader _trader;
        private readonly Player _player;
        private bool _viewingTraderItems = true;

        public TradeSystem(ITrader trader, Player player)
        {
            _trader = trader;
            _player = player;
        }

        public void StartTrade()
        {
            bool trading = true;
            string systemMessage = "";

            while (trading)
            {
                Console.Clear();

                if (!string.IsNullOrEmpty(systemMessage))
                {
                    Console.WriteLine($"СИСТЕМА: {systemMessage}");
                    systemMessage = "";
                }

                DisplayTradeInterface();

                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.W:
                    case ConsoleKey.S:
                        _viewingTraderItems = !_viewingTraderItems;
                        break;
                    case ConsoleKey.E:
                        if (_viewingTraderItems)
                            systemMessage = BuySelectedItem();
                        else
                            systemMessage = SellSelectedItem();
                        break;
                    case ConsoleKey.Q:
                        trading = false;
                        break;
                }
            }
        }

        private void DisplayTradeInterface()
        {
            Console.WriteLine($"======{_trader.Name}======");
            Console.WriteLine($"{_trader.ShopGreeting}");
            Console.WriteLine($"\nВаше золото: {_player.Gold}");
            Console.WriteLine($"Золото торговца: {_trader.Gold}\n");

            if (_viewingTraderItems)
            {
                Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======");
                DisplayItems(_trader.ItemsForSale, true);
                Console.WriteLine("========ВАШИ ПРЕДМЕТЫ========");
                DisplayItems(_player.Inventory.Items, false);
            }
            else
            {
                Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======");
                DisplayItems(_trader.ItemsForSale, false);
                Console.WriteLine("========ВАШИ ПРЕДМЕТЫ========");
                DisplayItems(_player.Inventory.Items, true);
            }

            Console.WriteLine("'W' и 'S' - Переключить панель торговли");
            Console.WriteLine("'E' - купить/продать 'Q' - уйти");
        }

        private void DisplayItems(List<InventoryItem> items, bool highlight)
        {
            if (items.Count == 0)
            {
                Console.WriteLine("     Товаров нет.");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (highlight)
                    Console.ForegroundColor = ConsoleColor.Yellow;

                int price = _viewingTraderItems ?
                    items[i].Details.Price :
                    (int)(items[i].Details.Price * 0.8);

                Console.WriteLine($"    {i + 1}. {items[i].Details.Name} x{items[i].Quantity} - {price} золота");
                Console.ResetColor();
            }
        }

        private string BuySelectedItem()
        {
            // Реализация покупки (адаптировать из старого Trader)
            return "Покупка...";
        }

        private string SellSelectedItem()
        {
            // Реализация продажи (адаптировать из старого Trader)
            return "Продажа...";
        }
    }
}