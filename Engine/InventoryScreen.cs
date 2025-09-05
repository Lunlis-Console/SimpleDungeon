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

            _renderer.Write(rightColumn, y, "=== СТАТИСТИКА ===", ConsoleColor.Yellow);
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
                InventoryUI.EquipmentSlotItem eqItem => $"[Экипировано] {eqItem}",
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
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_displayItems.Count - 1, _selectedIndex + 1);
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                case ConsoleKey.Spacebar:
                    HandleItemSelection();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void HandleItemSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _displayItems.Count)
                return;

            var selectedItem = _displayItems[_selectedIndex];

            if (selectedItem is InventoryItem inventoryItem)
            {
                // Используем существующую логику контекстного меню
                InventoryUI.ShowItemContextMenu(_player, inventoryItem);
                _displayItems = InventoryUI.PrepareInventoryItems(_player); // Обновляем список
            }
            else if (selectedItem is InventoryUI.EquipmentSlotItem equipmentItem)
            {
                ShowEquipmentContextMenu(equipmentItem.Equipment);
            }
        }

        private void ShowEquipmentContextMenu(Equipment equipment)
        {
            var actions = new List<string> { "Снять", "Осмотреть", "Назад" };

            int menuWidth = 30;
            int menuHeight = actions.Count + 4;
            int left = (Console.WindowWidth - menuWidth) / 2;
            int top = (Console.WindowHeight - menuHeight) / 2;

            // Сохраняем текущий экран
            _renderer.BeginFrame();

            // Рисуем полупрозрачный фон
            for (int y = top; y < top + menuHeight; y++)
            {
                for (int x = left; x < left + menuWidth; x++)
                {
                    if (x >= 0 && x < Console.WindowWidth && y >= 0 && y < Console.WindowHeight)
                    {
                        _renderer.Write(x, y, "░");
                    }
                }
            }

            // Рамка меню
            _renderer.Write(left, top, "╔" + new string('═', menuWidth - 2) + "╗");
            _renderer.Write(left, top + menuHeight - 1, "╚" + new string('═', menuWidth - 2) + "╝");

            for (int y = top + 1; y < top + menuHeight - 1; y++)
            {
                _renderer.Write(left, y, "║");
                _renderer.Write(left + menuWidth - 1, y, "║");
            }

            // Заголовок
            _renderer.Write(left + 2, top + 2, $"Действие: {equipment.Name}");

            // Пункты меню
            for (int i = 0; i < actions.Count; i++)
            {
                _renderer.Write(left + 2, top + 4 + i, $"{i + 1}. {actions[i]}");
            }

            _renderer.EndFrame();

            // Обработка выбора
            var key = Console.ReadKey(true);
            switch (key.Key)
            {
                case ConsoleKey.D1:
                    _player.UnequipItem(equipment);
                    break;
                case ConsoleKey.D2:
                    equipment.Read();
                    break;
            }

            _displayItems = InventoryUI.PrepareInventoryItems(_player);
        }
    }
}