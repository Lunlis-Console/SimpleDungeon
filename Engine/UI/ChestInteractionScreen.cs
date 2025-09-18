using Engine.Core;
using Engine.Entities;
using Engine.InventorySystem;

namespace Engine.UI
{
    public class ChestInteractionScreen : BaseScreen
    {
        private readonly Chest _chest;
        private readonly Player _player;
        private int _selectedIndex = 0;
        private bool _isPlayerInventory = true; // true = игрок, false = сундук
        private int _transferQuantity = 1;

        public ChestInteractionScreen(Chest chest, Player player)
        {
            _chest = chest ?? throw new ArgumentNullException(nameof(chest));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public override void Render()
        {
            var renderer = GameServices.BufferedRenderer;
            if (renderer == null) return;

            renderer.BeginFrame();

            // Заголовок
            renderer.Write(2, 1, $"=== {_chest.Name} ===", ConsoleColor.Yellow);
            renderer.Write(2, 2, $"Вместимость: {_chest.Inventory.Items.Count}/{_chest.MaxCapacity}", ConsoleColor.Gray);

            // Разделитель
            int centerX = renderer.Width / 2;
            for (int y = 4; y < renderer.Height - 2; y++)
            {
                renderer.Write(centerX, y, "│", ConsoleColor.DarkGray);
            }

            // Левая панель - инвентарь игрока
            RenderPlayerInventory(renderer, 2, 4, centerX - 2, renderer.Height - 6);

            // Правая панель - содержимое сундука
            RenderChestInventory(renderer, centerX + 2, 4, centerX - 2, renderer.Height - 6);

            // Подсказки
            RenderHelp(renderer);

            renderer.EndFrame();
        }

        private void RenderPlayerInventory(EnhancedBufferedRenderer renderer, int x, int y, int width, int height)
        {
            renderer.Write(x, y, "Ваш инвентарь:", ConsoleColor.Cyan);
            
            var items = _player.Inventory.Items;
            int maxItems = Math.Min(items.Count, height - 2);
            
            for (int i = 0; i < maxItems; i++)
            {
                var item = items[i];
                var color = (_isPlayerInventory && _selectedIndex == i) ? ConsoleColor.Green : ConsoleColor.White;
                var marker = (_isPlayerInventory && _selectedIndex == i) ? "► " : "  ";
                
                string itemText = $"{marker}{item.Details.Name} x{item.Quantity}";
                if (itemText.Length > width - 2)
                {
                    itemText = itemText.Substring(0, width - 5) + "...";
                }
                
                renderer.Write(x, y + 2 + i, itemText, color);
            }

            if (items.Count == 0)
            {
                renderer.Write(x, y + 2, "Инвентарь пуст", ConsoleColor.DarkGray);
            }
        }

        private void RenderChestInventory(EnhancedBufferedRenderer renderer, int x, int y, int width, int height)
        {
            renderer.Write(x, y, $"Содержимое {_chest.Name}:", ConsoleColor.Cyan);
            
            var items = _chest.Inventory.Items;
            int maxItems = Math.Min(items.Count, height - 2);
            
            for (int i = 0; i < maxItems; i++)
            {
                var item = items[i];
                var color = (!_isPlayerInventory && _selectedIndex == i) ? ConsoleColor.Green : ConsoleColor.White;
                var marker = (!_isPlayerInventory && _selectedIndex == i) ? "► " : "  ";
                
                string itemText = $"{marker}{item.Details.Name} x{item.Quantity}";
                if (itemText.Length > width - 2)
                {
                    itemText = itemText.Substring(0, width - 5) + "...";
                }
                
                renderer.Write(x, y + 2 + i, itemText, color);
            }

            if (items.Count == 0)
            {
                renderer.Write(x, y + 2, "Сундук пуст", ConsoleColor.DarkGray);
            }
        }

        private void RenderHelp(EnhancedBufferedRenderer renderer)
        {
            int y = renderer.Height - 3;
            
            renderer.Write(2, y, "Tab - переключить панель", ConsoleColor.DarkGray);
            renderer.Write(2, y + 1, "Enter - взять/положить предмет", ConsoleColor.DarkGray);
            renderer.Write(2, y + 2, "Escape - закрыть сундук", ConsoleColor.DarkGray);
            
            int rightX = renderer.Width - 30;
            renderer.Write(rightX, y, "+/- - изменить количество", ConsoleColor.DarkGray);
            renderer.Write(rightX, y + 1, $"Количество: {_transferQuantity}", ConsoleColor.Yellow);
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Tab:
                    SwitchPanel();
                    break;
                    
                case ConsoleKey.UpArrow:
                case ConsoleKey.W:
                    MoveSelection(-1);
                    break;
                    
                case ConsoleKey.DownArrow:
                case ConsoleKey.S:
                    MoveSelection(1);
                    break;
                    
                case ConsoleKey.Enter:
                case ConsoleKey.E:
                    TransferItem();
                    break;
                    
                case ConsoleKey.Add:
                case ConsoleKey.OemPlus:
                    IncreaseQuantity();
                    break;
                    
                case ConsoleKey.Subtract:
                case ConsoleKey.OemMinus:
                    DecreaseQuantity();
                    break;
                    
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void SwitchPanel()
        {
            _isPlayerInventory = !_isPlayerInventory;
            _selectedIndex = 0;
        }

        private void MoveSelection(int direction)
        {
            var currentItems = _isPlayerInventory ? _player.Inventory.Items : _chest.Inventory.Items;
            
            if (currentItems.Count == 0) return;
            
            _selectedIndex += direction;
            
            if (_selectedIndex < 0)
                _selectedIndex = currentItems.Count - 1;
            else if (_selectedIndex >= currentItems.Count)
                _selectedIndex = 0;
        }

        private void TransferItem()
        {
            var currentItems = _isPlayerInventory ? _player.Inventory.Items : _chest.Inventory.Items;
            
            if (currentItems.Count == 0) return;
            
            var selectedItem = currentItems[_selectedIndex];
            
            if (_isPlayerInventory)
            {
                // Передаем из инвентаря игрока в сундук
                _chest.TransferFromPlayer(_player, selectedItem, _transferQuantity);
            }
            else
            {
                // Передаем из сундука в инвентарь игрока
                _chest.TransferToPlayer(_player, selectedItem, _transferQuantity);
            }
            
            // Сбрасываем выделение если предмет закончился
            if (selectedItem.Quantity <= 0)
            {
                _selectedIndex = Math.Max(0, _selectedIndex - 1);
            }
        }

        private void IncreaseQuantity()
        {
            var currentItems = _isPlayerInventory ? _player.Inventory.Items : _chest.Inventory.Items;
            
            if (currentItems.Count == 0) return;
            
            var selectedItem = currentItems[_selectedIndex];
            _transferQuantity = Math.Min(_transferQuantity + 1, selectedItem.Quantity);
        }

        private void DecreaseQuantity()
        {
            _transferQuantity = Math.Max(1, _transferQuantity - 1);
        }

        public override void Update()
        {
            // Обновление не требуется
        }
    }
}
