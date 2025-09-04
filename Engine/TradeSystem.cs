namespace Engine
{
    public class TradeSystem
    {
        private readonly ITrader _trader;
        private readonly Player _player;
        private bool _viewingTraderItems = true;
        private int _selectedIndex = 0;

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
                        if (_viewingTraderItems)
                            _selectedIndex = Math.Max(0, _selectedIndex - 1);
                        else
                            _selectedIndex = Math.Max(0, _selectedIndex - 1);
                        break;
                    case ConsoleKey.S:
                        if (_viewingTraderItems)
                            _selectedIndex = Math.Min(_trader.ItemsForSale.Count - 1, _selectedIndex + 1);
                        else
                            _selectedIndex = Math.Min(_player.Inventory.Items.Count - 1, _selectedIndex + 1);
                        break;
                    //case ConsoleKey.A:
                    case ConsoleKey.Tab:
                        _viewingTraderItems = !_viewingTraderItems;
                        _selectedIndex = 0;
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

            // Товары торговца
            Console.WriteLine("=======ТОВАРЫ ТОРГОВЦА=======КУПИТЬ===");
            DisplayItems(_trader.ItemsForSale, _viewingTraderItems);

            Console.WriteLine("==============================");

            // Предметы игрока
            Console.WriteLine("========ВАШИ ПРЕДМЕТЫ=======ПРОДАТЬ===");
            DisplayItems(_player.Inventory.Items, !_viewingTraderItems);

            Console.WriteLine("\nУправление:");
            Console.WriteLine("W/S - Выбор предмета");
            Console.WriteLine("TAB - Переключить между товарами");
            Console.WriteLine("E - Купить/продать выбранный предмет");
            Console.WriteLine("Q - Выйти из торговли");
        }

        private void DisplayItems(List<InventoryItem> items, bool isSelectedSection)
        {
            if (items.Count == 0)
            {
                Console.WriteLine("     Товаров нет.");
                return;
            }

            for (int i = 0; i < items.Count; i++)
            {
                if (isSelectedSection && i == _selectedIndex)
                {
                    Console.ForegroundColor = ConsoleColor.Yellow;
                    Console.Write("> ");
                }
                else
                {
                    Console.Write("  ");
                }

                int price = isSelectedSection ?
                    CalculateBuyPrice(items[i].Details) :
                    CalculateSellPrice(items[i].Details);

                Console.WriteLine($"{items[i].Details.Name} x{items[i].Quantity} - {price} золота");
                Console.ResetColor();
            }
        }

        private int CalculateBuyPrice(Item item)
        {
            return (item.Price * _trader.BuyPriceModifier) / 100;
        }

        private int CalculateSellPrice(Item item)
        {
            return (item.Price * _trader.SellPriceModifier) / 100;
        }

        private string BuySelectedItem()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _trader.ItemsForSale.Count)
                return "Неверный выбор предмета!";

            var selectedItem = _trader.ItemsForSale[_selectedIndex];
            int price = CalculateBuyPrice(selectedItem.Details);

            if (_player.Gold < price)
                return "Недостаточно золота!";

            if (!_trader.CanAfford(selectedItem.Details, 1, _player))
                return "Торговец не может продать этот предмет!";

            // Покупка
            _player.Gold -= price;
            _trader.Gold += price;

            _player.AddItemToInventory(selectedItem.Details, 1);
            selectedItem.Quantity--;

            if (selectedItem.Quantity <= 0)
            {
                _trader.ItemsForSale.RemoveAt(_selectedIndex);
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            }

            return $"Куплено: {selectedItem.Details.Name} за {price} золота!";
        }

        private string SellSelectedItem()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _player.Inventory.Items.Count)
                return "Неверный выбор предмета!";

            var selectedItem = _player.Inventory.Items[_selectedIndex];
            int price = CalculateSellPrice(selectedItem.Details);

            if (_trader.Gold < price)
                return "У торговца недостаточно золота!";

            // Продажа
            _player.Gold += price;
            _trader.Gold -= price;

            // Добавляем предмет торговцу или увеличиваем количество
            var traderItem = _trader.ItemsForSale.FirstOrDefault(i => i.Details.ID == selectedItem.Details.ID);
            if (traderItem != null)
            {
                traderItem.Quantity++;
            }
            else
            {
                _trader.ItemsForSale.Add(new InventoryItem(selectedItem.Details, 1));
            }

            _player.RemoveItemFromInventory(selectedItem, 1);

            return $"Продано: {selectedItem.Details.Name} за {price} золота!";
        }
    }
}