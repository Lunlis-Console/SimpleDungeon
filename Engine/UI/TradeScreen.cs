using Engine.Entities;
using Engine.Trading;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.UI
{
    public class TradeScreen : BaseScreen
    {
        private readonly ITrader _trader;
        private readonly Player _player;
        private bool _viewingTraderItems = true;
        private int _selectedIndex = 0;
        private string _systemMessage = "";

        public TradeScreen(ITrader trader, Player player)
        {
            _trader = trader;
            _player = player;
        }

        public override void Render()
        {
            ClearScreen();

            RenderHeader($"ТОРГОВЛЯ С {_trader.Name.ToUpper()}");
            RenderGoldInfo();
            RenderPlayerItems(); // Теперь сначала отображаем предметы игрока
            RenderTraderItems(); // Затем предметы торговца
            RenderSystemMessage();
            RenderFooter("W/S - Выбор │ TAB - Переключить │ E - Купить/продать │ Q - Выйти");
        }

        private void RenderGoldInfo()
        {
            int y = 4;
            _renderer.Write(2, y, $"Ваше золото: {_player.Gold:N0}", ConsoleColor.Yellow);
            _renderer.Write(40, y, $"Золото торговца: {_trader.Gold:N0}", ConsoleColor.Yellow);
        }

        private void RenderTraderItems()
        {
            int y = 6;
            int x = 50; // Перемещаем предметы торговца в правую часть
            _renderer.Write(x, y, "=== ТОВАРЫ ТОРГОВЦА ===", ConsoleColor.Cyan);
            y += 2;

            if (_trader.ItemsForSale.Count == 0)
            {
                _renderer.Write(x + 2, y, "Товаров нет", ConsoleColor.DarkGray);
                return;
            }

            int maxItemsToShow = Height - y - 6; // Максимальное количество предметов, которые можно отобразить
            int itemsToShow = Math.Min(_trader.ItemsForSale.Count, maxItemsToShow);

            for (int i = 0; i < itemsToShow; i++)
            {
                var item = _trader.ItemsForSale[i];
                int price = CalculateBuyPrice(item.Details);
                bool isSelected = _viewingTraderItems && i == _selectedIndex;

                ConsoleColor color = isSelected ? ConsoleColor.Yellow : ConsoleColor.White;
                string prefix = isSelected ? "► " : "  ";

                string itemText = $"{prefix}{item.Details.Name} x{item.Quantity} - {price} золота";

                _renderer.Write(x + 2, y, itemText, color);

                // Отображаем бонусы для экипировки
                if (item.Details is Equipment equipment)
                {
                    y++;
                    if (y < Height - 6) // Проверяем, не выходим ли за границы экрана
                    {
                        string bonuses = GetEquipmentBonuses(equipment);
                        _renderer.Write(x + 6, y, bonuses, ConsoleColor.DarkGray);
                    }
                }

                y++;
                if (y >= Height - 6) break; // Прерываем, если достигли нижней границы экрана
            }
        }

        private void RenderPlayerItems()
        {
            int y = 6;
            int x = 2; // Перемещаем предметы игрока в левую часть
            _renderer.Write(x, y, "=== ВАШИ ПРЕДМЕТЫ ===", ConsoleColor.Cyan);
            y += 2;

            if (_player.Inventory.Items.Count == 0)
            {
                _renderer.Write(x + 2, y, "Предметов нет", ConsoleColor.DarkGray);
                return;
            }

            int maxItemsToShow = Height - y - 6; // Максимальное количество предметов, которые можно отобразить
            int itemsToShow = Math.Min(_player.Inventory.Items.Count, maxItemsToShow);

            for (int i = 0; i < itemsToShow; i++)
            {
                var item = _player.Inventory.Items[i];
                int price = CalculateSellPrice(item.Details);
                bool isSelected = !_viewingTraderItems && i == _selectedIndex;

                ConsoleColor color = isSelected ? ConsoleColor.Yellow : ConsoleColor.White;
                string prefix = isSelected ? "► " : "  ";

                string itemText = $"{prefix}{item.Details.Name} x{item.Quantity} - {price} золота";

                _renderer.Write(x + 2, y, itemText, color);
                y++;

                if (y >= Height - 6) break; // Прерываем, если достигли нижней границы экрана
            }
        }

        private void RenderSystemMessage()
        {
            if (!string.IsNullOrEmpty(_systemMessage))
            {
                int y = Height - 6;
                _renderer.Write(2, y, _systemMessage, ConsoleColor.Red);
            }
        }

        private string GetEquipmentBonuses(Equipment equipment)
        {
            var bonuses = new List<string>();

            if (equipment.AttackBonus != 0)
                bonuses.Add($"АТК: {equipment.AttackBonus:+#;-#;0}");
            if (equipment.DefenceBonus != 0)
                bonuses.Add($"ЗЩТ: {equipment.DefenceBonus:+#;-#;0}");
            if (equipment.AgilityBonus != 0)
                bonuses.Add($"ЛОВ: {equipment.AgilityBonus:+#;-#;0}");
            if (equipment.HealthBonus != 0)
                bonuses.Add($"ЗДР: {equipment.HealthBonus:+#;-#;0}");

            return bonuses.Count > 0 ? string.Join(" │ ", bonuses) : "Нет бонусов";
        }

        private int CalculateBuyPrice(Item item)
        {
            return item.Price * _trader.BuyPriceModifier / 100;
        }

        private int CalculateSellPrice(Item item)
        {
            return item.Price * _trader.SellPriceModifier / 100;
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            _systemMessage = ""; // Сбрасываем сообщение при новом вводе

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                    MoveSelection(-1);
                    break;

                case ConsoleKey.S:
                    MoveSelection(1);
                    break;

                case ConsoleKey.Tab:
                    SwitchView();
                    break;

                case ConsoleKey.E:
                    PerformTrade();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }

            RequestRedraw();
        }

        private void MoveSelection(int direction)
        {
            if (_viewingTraderItems)
            {
                int maxIndex = Math.Max(0, _trader.ItemsForSale.Count - 1);
                _selectedIndex = Math.Clamp(_selectedIndex + direction, 0, maxIndex);
            }
            else
            {
                int maxIndex = Math.Max(0, _player.Inventory.Items.Count - 1);
                _selectedIndex = Math.Clamp(_selectedIndex + direction, 0, maxIndex);
            }
        }

        private void SwitchView()
        {
            _viewingTraderItems = !_viewingTraderItems;
            _selectedIndex = 0;
        }

        private void PerformTrade()
        {
            if (_viewingTraderItems)
            {
                BuyItem();
            }
            else
            {
                SellItem();
            }
        }

        private void BuyItem()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _trader.ItemsForSale.Count)
            {
                _systemMessage = "Неверный выбор предмета!";
                return;
            }

            var selectedItem = _trader.ItemsForSale[_selectedIndex];
            int price = CalculateBuyPrice(selectedItem.Details);

            if (_player.Gold < price)
            {
                _systemMessage = "Недостаточно золота!";
                return;
            }

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

            _systemMessage = $"Куплено: {selectedItem.Details.Name} за {price} золота!";
        }

        private void SellItem()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _player.Inventory.Items.Count)
            {
                _systemMessage = "Неверный выбор предмета!";
                return;
            }

            var selectedItem = _player.Inventory.Items[_selectedIndex];
            int price = CalculateSellPrice(selectedItem.Details);

            if (_trader.Gold < price)
            {
                _systemMessage = "У торговца недостаточно золота!";
                return;
            }

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

            _systemMessage = $"Продано: {selectedItem.Details.Name} за {price} золота!";
        }
    }
}