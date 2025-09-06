namespace Engine
{
    public class InventoryScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;
        private List<object> _displayItems;

        public InventoryScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;
            _displayItems = InventoryUI.PrepareInventoryItems(player);
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader("ИНВЕНТАРЬ");
            RenderInventoryItems();
            RenderEquipmentInfo();
            RenderPlayerStats();
            RenderFooter("W/S - выбор │ E - действие │ Q - назад");

            _renderer.EndFrame();
        }

        private void RenderInventoryItems()
        {
            int startY = 4;
            int maxItems = Console.WindowHeight - 15;

            for (int i = 0; i < Math.Min(_displayItems.Count, maxItems); i++)
            {
                bool isSelected = i == _selectedIndex;
                string itemText = GetItemDisplayText(_displayItems[i]);

                if (isSelected)
                {
                    _renderer.Write(2, startY + i, "> ");
                    _renderer.Write(4, startY + i, itemText, ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, startY + i, itemText);
                }
            }

            // Scroll indicator
            if (_displayItems.Count > maxItems)
            {
                _renderer.Write(Console.WindowWidth - 3, startY, "↑", ConsoleColor.DarkGray);
                _renderer.Write(Console.WindowWidth - 3, startY + maxItems - 1, "↓", ConsoleColor.DarkGray);
            }
        }

        private void RenderEquipmentInfo()
        {
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 4;

            _renderer.Write(rightColumn, y, "=== ЭКИПИРОВКА ===", ConsoleColor.Yellow);
            y += 2;

            RenderEquipmentSlot(rightColumn, ref y, "Оружие:", _player.Inventory.MainHand);
            RenderEquipmentSlot(rightColumn, ref y, "Щит:", _player.Inventory.OffHand);
            RenderEquipmentSlot(rightColumn, ref y, "Шлем:", _player.Inventory.Helmet);
            RenderEquipmentSlot(rightColumn, ref y, "Броня:", _player.Inventory.Armor);
            RenderEquipmentSlot(rightColumn, ref y, "Перчатки:", _player.Inventory.Gloves);
            RenderEquipmentSlot(rightColumn, ref y, "Ботинки:", _player.Inventory.Boots);
            RenderEquipmentSlot(rightColumn, ref y, "Амулет:", _player.Inventory.Amulet);
            RenderEquipmentSlot(rightColumn, ref y, "Кольцо 1:", _player.Inventory.Ring1);
            RenderEquipmentSlot(rightColumn, ref y, "Кольцо 2:", _player.Inventory.Ring2);
        }

        private void RenderEquipmentSlot(int x, ref int y, string slotName, Equipment equipment)
        {
            string equipmentName = equipment?.Name ?? "Пусто";
            _renderer.Write(x, y, $"{slotName,-10} {equipmentName}");
            y++;
        }

        private void RenderPlayerStats()
        {
            int rightColumn = Console.WindowWidth / 2 + 2;
            int y = 18;

            _renderer.Write(rightColumn, y, "=== ПАРАМЕТРЫ ===", ConsoleColor.Yellow);
            y += 2;

            _renderer.Write(rightColumn, y, $"Здоровье: {_player.CurrentHP}/{_player.TotalMaximumHP}");
            y++;
            _renderer.Write(rightColumn, y, $"Атака: {_player.Attack}");
            y++;
            _renderer.Write(rightColumn, y, $"Защита: {_player.Defence}");
            y++;
            _renderer.Write(rightColumn, y, $"Ловкость: {_player.Agility}");
            y++;
            _renderer.Write(rightColumn, y, $"Золото: {_player.Gold}");
            y++;
            _renderer.Write(rightColumn, y, $"Уровень: {_player.Level}");
        }

        private string GetItemDisplayText(object item)
        {
            return item switch
            {
                InventoryItem invItem => $"{invItem.Details.Name} x{invItem.Quantity}",
                InventoryUI.EquipmentSlotItem eqItem => $"[Надето] {eqItem}",
                _ => item?.ToString() ?? "Неизвестный предмет"
            };
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw(); // ← ДОБАВЛЯЕМ ЭТУ СТРОЧКУ
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_displayItems.Count - 1, _selectedIndex + 1);
                    RequestPartialRedraw(); // ← ДОБАВЛЯЕМ ЭТУ СТРОЧКУ
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    HandleItemSelection();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                case ConsoleKey.I:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void HandleItemSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _displayItems.Count)
                return;

            var selectedItem = _displayItems[_selectedIndex];

            // Вместо показа контекстного меню, переходим на новый экран действий
            ScreenManager.PushScreen(new InventoryItemActionScreen(_player, selectedItem));
        }

        public void RefreshInventoryList()
        {
            _displayItems = InventoryUI.PrepareInventoryItems(_player);
            _selectedIndex = Math.Min(_selectedIndex, _displayItems.Count - 1);
            RequestRedraw();
        }
    }
}