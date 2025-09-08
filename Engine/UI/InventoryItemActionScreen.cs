using Engine.Entities;

namespace Engine.UI
{
    public class ItemDetailScreen : BaseScreen
    {
        private readonly Player _player;
        private readonly Item _item;

        public ItemDetailScreen(Player player, Item item)
        {
            _player = player;
            _item = item;
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader($"ОСМОТР : {_item.Name}");
            RenderItemDetails();
            RenderFooter("Q - назад");

            _renderer.EndFrame();
        }

        private void RenderItemDetails()
        {
            int y = 4;
            int dividerX = Console.WindowWidth / 2;
            int rightX = dividerX + 2;

            //// Левая колонка - основная информация
            //_renderer.Write(2, y, "=== ПРЕДМЕТ ===", ConsoleColor.DarkCyan);
            //y += 2;

            //_renderer.Write(2, y, _item.Name, ConsoleColor.White);
            //y++;
            //_renderer.Write(2, y, $"Тип: {GetTypeText(_item.Type)}", ConsoleColor.Cyan);
            //y++;
            //_renderer.Write(2, y, $"Цена: {_item.Price} золота", ConsoleColor.Yellow);
            //y++;

            if (_item is Equipment equipment)
            {
                //y++;
                _renderer.Write(2, y, "=== ЭФФЕКТЫ ===", ConsoleColor.Green);
                y++;

                if (equipment.AttackBonus != 0)
                {
                    _renderer.Write(2, y, $"Атака: +{equipment.AttackBonus}");
                    y++;
                }

                if (equipment.DefenceBonus != 0)
                {
                    _renderer.Write(2, y, $"Защита: +{equipment.DefenceBonus}");
                    y++;
                }

                if (equipment.AgilityBonus != 0)
                {
                    _renderer.Write(2, y, $"Ловкость: +{equipment.AgilityBonus}");
                    y++;
                }

                if (equipment.HealthBonus != 0)
                {
                    _renderer.Write(2, y, $"Здоровье: +{equipment.HealthBonus}");
                    y++;
                }
            }

            // Правая колонка - описание
            y = 4;
            _renderer.Write(rightX, y, "=== ОПИСАНИЕ ===", ConsoleColor.DarkCyan);
            y += 2;

            var descriptionLines = WrapText(_item.Description, Console.WindowWidth - rightX - 2);
            foreach (var line in descriptionLines)
            {
                _renderer.Write(rightX, y, line, ConsoleColor.Gray);
                y++;
            }

            // Вертикальный разделитель
            RenderVerticalDivider(dividerX);
        }

        private void RenderVerticalDivider(int x)
        {
            for (int y = 3; y < Console.WindowHeight - 3; y++)
            {
                _renderer.Write(x, y, "│", ConsoleColor.DarkGray);
            }
        }

        private string GetTypeText(ItemType type)
        {
            return type switch
            {
                ItemType.Consumable => "Расходник",
                ItemType.Helmet => "Шлем",
                ItemType.Armor => "Броня",
                ItemType.Gloves => "Перчатки",
                ItemType.Boots => "Ботинки",
                ItemType.OneHandedWeapon => "Одноручное оружие",
                ItemType.TwoHandedWeapon => "Двуручное оружие",
                ItemType.OffHand => "Щит",
                ItemType.Amulet => "Амулет",
                ItemType.Ring => "Кольцо",
                ItemType.Quest => "Квестовый предмет",
                _ => "Предмет"
            };
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }
    }

    public class DiscardItemScreen : BaseScreen
    {
        private readonly Player _player;
        private readonly InventoryItem _item;
        private int _quantityToDiscard;
        private int _maxQuantity;

        public DiscardItemScreen(Player player, InventoryItem item)
        {
            _player = player;
            _item = item;
            _maxQuantity = item.Quantity;
            _quantityToDiscard = 1;
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader($"ВЫБРОСИТЬ: {_item.Details.Name}");

            int dividerX = Console.WindowWidth / 2;

            RenderQuantitySelector(dividerX);
            RenderItemInfo(dividerX);
            RenderVerticalDivider(dividerX);

            RenderFooter("A/D - изменить количество │ E - подтвердить │ Q - отмена");

            _renderer.EndFrame();
        }

        private void RenderQuantitySelector(int dividerX)
        {
            int centerX = dividerX / 2;
            int centerY = Console.WindowHeight / 2 - 2;

            _renderer.Write(centerX - 15, centerY - 2, "Сколько выбросить?", ConsoleColor.Yellow);

            // Графический индикатор количества
            string quantityBar = new string('█', _quantityToDiscard) +
                               new string('░', _maxQuantity - _quantityToDiscard);

            _renderer.Write(centerX - _maxQuantity / 2, centerY, quantityBar, ConsoleColor.Green);

            // Числовое значение
            string quantityText = $"{_quantityToDiscard} / {_maxQuantity}";
            _renderer.Write(centerX - quantityText.Length / 2, centerY + 2, quantityText, ConsoleColor.White);

            // Кнопки управления
            _renderer.Write(centerX - 20, centerY + 4, "[A] Меньше   [D] Больше", ConsoleColor.DarkGray);
        }

        private void RenderItemInfo(int dividerX)
        {
            int rightX = dividerX + 2;
            int y = Console.WindowHeight / 2 - 4;

            _renderer.Write(rightX, y, "=== ПРЕДМЕТ ===", ConsoleColor.DarkCyan);
            y += 2;

            _renderer.Write(rightX, y, _item.Details.Name, ConsoleColor.White);
            y++;
            _renderer.Write(rightX, y, $"Тип: {GetTypeText(_item.Details.Type)}", ConsoleColor.Cyan);
            y++;
            _renderer.Write(rightX, y, $"Цена: {_item.Details.Price} золота", ConsoleColor.Yellow);
            y++;
            _renderer.Write(rightX, y, $"Всего: {_maxQuantity} шт.", ConsoleColor.Gray);
        }

        private void RenderVerticalDivider(int x)
        {
            for (int y = 3; y < Console.WindowHeight - 3; y++)
            {
                _renderer.Write(x, y, "│", ConsoleColor.DarkGray);
            }
        }

        private string GetTypeText(ItemType type)
        {
            return type switch
            {
                ItemType.Consumable => "Расходник",
                ItemType.Helmet => "Шлем",
                ItemType.Armor => "Броня",
                ItemType.Gloves => "Перчатки",
                ItemType.Boots => "Ботинки",
                ItemType.OneHandedWeapon => "Одноручное оружие",
                ItemType.TwoHandedWeapon => "Двуручное оружие",
                ItemType.OffHand => "Щит",
                ItemType.Amulet => "Амулет",
                ItemType.Ring => "Кольцо",
                _ => "Предмет"
            };
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.A:
                case ConsoleKey.LeftArrow:
                    _quantityToDiscard = Math.Max(1, _quantityToDiscard - 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.D:
                case ConsoleKey.RightArrow:
                    _quantityToDiscard = Math.Min(_maxQuantity, _quantityToDiscard + 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    DiscardItems();
                    ScreenManager.PopScreen();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void DiscardItems()
        {
            _player.Inventory.RemoveItem(_item, _quantityToDiscard);
            MessageSystem.AddMessage($"Выброшено {_quantityToDiscard} шт. предмета {_item.Details.Name}");
        }
    }

    public class InventoryItemActionScreen : BaseScreen
    {
        private readonly Player _player;
        private readonly object _selectedItem;
        private int _selectedActionIndex;
        private List<string> _availableActions;
        private string _itemName;
        private string _itemDescription;
        private bool _isAlreadyEquipped;

        public InventoryItemActionScreen(Player player, object selectedItem)
        {
            _player = player;
            _selectedItem = selectedItem;
            _selectedActionIndex = 0;

            InitializeItemInfo();
            InitializeAvailableActions();
        }

        private void InitializeItemInfo()
        {
            if (_selectedItem is InventoryItem inventoryItem)
            {
                _itemName = inventoryItem.Details.Name;
                _itemDescription = inventoryItem.Details.Description;
                _isAlreadyEquipped = IsItemEquipped(inventoryItem.Details);
            }
            else if (_selectedItem is InventoryUI.EquipmentSlotItem equipmentItem)
            {
                _itemName = equipmentItem.Equipment.Name;
                _itemDescription = equipmentItem.Equipment.Description;
                _isAlreadyEquipped = true;
            }
            else
            {
                _itemName = "Неизвестный предмет";
                _itemDescription = "Описание отсутствует";
                _isAlreadyEquipped = false;
            }
        }

        private bool IsItemEquipped(Item item)
        {
            if (item is Equipment equipment)
            {
                // Проверяем все слоты экипировки
                return _player.Inventory.Helmet == equipment ||
                       _player.Inventory.Armor == equipment ||
                       _player.Inventory.Gloves == equipment ||
                       _player.Inventory.Boots == equipment ||
                       _player.Inventory.MainHand == equipment ||
                       _player.Inventory.OffHand == equipment ||
                       _player.Inventory.Amulet == equipment ||
                       _player.Inventory.Ring1 == equipment ||
                       _player.Inventory.Ring2 == equipment;
            }
            return false;
        }

        private void InitializeAvailableActions()
        {
            _availableActions = new List<string>();

            if (_selectedItem is InventoryItem inventoryItem)
            {
                // Показываем "Надеть" только если предмет не экипирован и это экипировка
                if (inventoryItem.Details.Type == ItemType.Consumable)
                {
                    _availableActions.Add("Использовать");
                }
                else if (inventoryItem.Details is Equipment && !_isAlreadyEquipped)
                {
                    _availableActions.Add("Надеть");
                }

                _availableActions.Add("Осмотреть");
                _availableActions.Add("Выбросить");
            }
            else if (_selectedItem is InventoryUI.EquipmentSlotItem equipmentItem)
            {
                // Для экипированных предметов показываем "Снять"
                _availableActions.Add("Снять");
                _availableActions.Add("Осмотреть");
            }

            _availableActions.Add("Назад");
        }

        public override void Render()
        {
            _renderer.BeginFrame();
            ClearScreen();

            RenderHeader($"ДЕЙСТВИЯ : {_itemName}");
            RenderItemInfoAndActions();
            RenderFooter("W/S - выбор │ E - выполнить │ Q - назад");

            _renderer.EndFrame();
        }

        private void RenderItemInfoAndActions()
        {
            int dividerX = Console.WindowWidth / 2;
            int y = 4;

            for (int i = 0; i < _availableActions.Count; i++)
            {
                bool isSelected = i == _selectedActionIndex;

                if (isSelected)
                {
                    _renderer.Write(2, y, "► ");
                    _renderer.Write(4, y, _availableActions[i], ConsoleColor.Green);
                }
                else
                {
                    _renderer.Write(4, y, _availableActions[i]);
                }
                y++;
            }

            //// ПРАВАЯ ЧАСТЬ - ИНФОРМАЦИЯ О ПРЕДМЕТЕ
            //y = 4;
            //int rightX = dividerX + 2;

            //// Заголовок ПРЕДМЕТ
            //_renderer.Write(rightX, y, "=== ПРЕДМЕТ ===", ConsoleColor.DarkCyan);
            //y += 1;

            //// Название предмета
            //_renderer.Write(rightX, y, _itemName, ConsoleColor.White);
            //y++;

            //// Статус экипировки
            //if (_isAlreadyEquipped)
            //{
            //    _renderer.Write(rightX, y, "Статус: Экипировано", ConsoleColor.Green);
            //    y++;
            //}

            //// Тип предмета
            //string itemType = GetItemTypeText();
            //_renderer.Write(rightX, y, $"Тип: {itemType}", ConsoleColor.Cyan);
            //y++;

            //// Количество (для предметов инвентаря)
            //if (_selectedItem is InventoryItem inventoryItem)
            //{
            //    _renderer.Write(rightX, y, $"Количество: {inventoryItem.Quantity}");
            //    y++;
            //}

            //// Цена
            //int price = GetItemPrice();
            //if (price > 0)
            //{
            //    _renderer.Write(rightX, y, $"Цена: {price} золота", ConsoleColor.Yellow);
            //    y++;
            //}

            //// Разделитель перед описанием
            //y++;
            //_renderer.Write(rightX, y, "=== ОПИСАНИЕ ===", ConsoleColor.DarkGray);
            //y += 2;

            //// Описание предмета
            //var descriptionLines = WrapText(_itemDescription, Console.WindowWidth - rightX - 2);
            //foreach (var line in descriptionLines)
            //{
            //    if (y >= Console.WindowHeight - 4) break;

            //    _renderer.Write(rightX, y, line, ConsoleColor.Gray);
            //    y++;
            //}

            //// Вертикальный разделитель между секциями
            //RenderVerticalDivider(dividerX);
        }

        private void RenderVerticalDivider(int x)
        {
            for (int y = 3; y < Console.WindowHeight - 3; y++)
            {
                _renderer.Write(x, y, "│", ConsoleColor.DarkGray);
            }
        }

        private int GetItemPrice()
        {
            return _selectedItem switch
            {
                InventoryItem inventoryItem => inventoryItem.Details.Price,
                InventoryUI.EquipmentSlotItem equipmentItem => equipmentItem.Equipment.Price,
                _ => 0
            };
        }

        private string GetItemTypeText()
        {
            return _selectedItem switch
            {
                InventoryItem { Details.Type: ItemType.Consumable } => "Расходник",
                InventoryItem { Details.Type: ItemType.Helmet } => "Шлем",
                InventoryItem { Details.Type: ItemType.Armor } => "Броня",
                InventoryItem { Details.Type: ItemType.Gloves } => "Перчатки",
                InventoryItem { Details.Type: ItemType.Boots } => "Ботинки",
                InventoryItem { Details.Type: ItemType.OneHandedWeapon } => "Одноручное оружие",
                InventoryItem { Details.Type: ItemType.TwoHandedWeapon } => "Двуручное оружие",
                InventoryItem { Details.Type: ItemType.OffHand } => "Щит",
                InventoryItem { Details.Type: ItemType.Amulet } => "Амулет",
                InventoryItem { Details.Type: ItemType.Ring } => "Кольцо",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.Helmet } => "Шлем",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.Armor } => "Броня",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.Gloves } => "Перчатки",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.Boots } => "Ботинки",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.OneHandedWeapon } => "Оружие",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.TwoHandedWeapon } => "Двуручное оружие",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.OffHand } => "Щит",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.Amulet } => "Амулет",
                InventoryUI.EquipmentSlotItem { Equipment.Type: ItemType.Ring } => "Кольцо",
                _ => "Предмет"
            };
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedActionIndex = Math.Max(0, _selectedActionIndex - 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedActionIndex = Math.Min(_availableActions.Count - 1, _selectedActionIndex + 1);
                    RequestPartialRedraw();
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    ExecuteSelectedAction();
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void ExecuteSelectedAction()
        {
            string action = _availableActions[_selectedActionIndex];

            if (action == "Назад")
            {
                ScreenManager.PopScreen();
                return;
            }

            if (_selectedItem is InventoryItem inventoryItem)
            {
                HandleInventoryItemAction(inventoryItem, action);
            }
            else if (_selectedItem is InventoryUI.EquipmentSlotItem equipmentItem)
            {
                HandleEquipmentItemAction(equipmentItem.Equipment, action);
            }

            // После выполнения действия возвращаемся в инвентарь
            //ScreenManager.PopScreen();
        }

        private void HandleInventoryItemAction(InventoryItem inventoryItem, string action)
        {
            switch (action)
            {
                case "Использовать":
                    _player.UseItemToHeal(inventoryItem);
                    MessageSystem.AddMessage($"Использовано: {inventoryItem.Details.Name}");
                    RefreshParentInventory();
                    ScreenManager.PopScreen();
                    break;

                case "Надеть":
                    if (!_isAlreadyEquipped)
                    {
                        bool success = _player.EquipItem(inventoryItem);
                        if (success)
                        {
                            MessageSystem.AddMessage($"Надето: {inventoryItem.Details.Name}");
                            RefreshParentInventory();
                            ScreenManager.PopScreen();
                        }
                    }
                    else
                    {
                        MessageSystem.AddMessage("Предмет уже экипирован!");
                    }
                    break;
                case "Осмотреть":
                    ScreenManager.PushScreen(new ItemDetailScreen(_player, inventoryItem.Details));
                    break;

                case "Выбросить":
                    HandleItemDiscard(inventoryItem);
                    RefreshParentInventory();
                    ScreenManager.PopScreen();
                    break;
            }
        }

        private void HandleEquipmentItemAction(Equipment equipment, string action)
        {
            switch (action)
            {
                case "Снять":
                    bool success = _player.UnequipItem(equipment);
                    if (success)
                    {
                        MessageSystem.AddMessage($"Снято: {equipment.Name}");
                        RefreshParentInventory();
                        ScreenManager.PopScreen();
                    }
                    break;

                case "Осмотреть":
                    ScreenManager.PushScreen(new ItemDetailScreen(_player, equipment));
                    break;
            }
        }
        private void HandleItemDiscard(InventoryItem item)
        {
            if (item.Quantity > 1)
            {
                ScreenManager.PushScreen(new DiscardItemScreen(_player, item));
            }
            else
            {
                _player.Inventory.RemoveItem(item);
                MessageSystem.AddMessage($"Предмет {item.Details.Name} выброшен");
            }
        }

        // Новый метод для обновления родительского экрана инвентаря
        private void RefreshParentInventory()
        {
            var inventoryScreen = ScreenManager.GetScreen<InventoryScreen>();
            inventoryScreen?.RefreshInventoryList();
        }
    }
}