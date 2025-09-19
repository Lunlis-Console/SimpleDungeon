using Engine.Core;
using Engine.Entities;
using Engine.InventorySystem;

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
            ClearScreen();

            RenderHeader($"ОСМОТР : {_item.Name}");
            RenderItemDetails();
            RenderFooter("Q - назад");
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
            ClearScreen();

            RenderHeader($"ВЫБРОСИТЬ: {_item.Details.Name}");

            int dividerX = Console.WindowWidth / 2;

            RenderQuantitySelector(dividerX);
            RenderItemInfo(dividerX);
            RenderVerticalDivider(dividerX);

            RenderFooter("A/D - изменить количество │ E - подтвердить │ Q - отмена");
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
            if (item == null) return false;

            // Старый путь — сравнение по ссылке для Equipment (оставим для совместимости),
            // но добавим сравнение по ID на всякий случай.
            if (item is Equipment equipment)
            {
                if (_player.Inventory.Helmet?.ID == equipment.ID) return true;
                if (_player.Inventory.Armor?.ID == equipment.ID) return true;
                if (_player.Inventory.Gloves?.ID == equipment.ID) return true;
                if (_player.Inventory.Boots?.ID == equipment.ID) return true;
                if (_player.Inventory.MainHand?.ID == equipment.ID) return true;
                if (_player.Inventory.OffHand?.ID == equipment.ID) return true;
                if (_player.Inventory.Amulet?.ID == equipment.ID) return true;
                if (_player.Inventory.Ring1?.ID == equipment.ID) return true;
                if (_player.Inventory.Ring2?.ID == equipment.ID) return true;

                return false;
            }

            // Новый путь — CompositeItem + EquipComponent: сравниваем по ID
            if (item is CompositeItem compItem)
            {
                var eq = compItem.Components?.OfType<EquipComponent>().FirstOrDefault();
                if (eq != null)
                {
                    // Нормализуем имя слота — используем простое сравнение строк (имена слотов должны совпадать с ItemType names или с именами, которые используешь)
                    string slot = (eq.Slot ?? string.Empty).ToLowerInvariant();

                    switch (slot)
                    {
                        case "helmet":
                            return _player.Inventory.Helmet?.ID == compItem.ID;
                        case "armor":
                            return _player.Inventory.Armor?.ID == compItem.ID;
                        case "gloves":
                            return _player.Inventory.Gloves?.ID == compItem.ID;
                        case "boots":
                            return _player.Inventory.Boots?.ID == compItem.ID;
                        case "onehandedweapon":
                        case "twohandedweapon":
                        case "weapon":
                            return _player.Inventory.MainHand?.ID == compItem.ID || _player.Inventory.OffHand?.ID == compItem.ID;
                        case "offhand":
                        case "shield":
                            return _player.Inventory.OffHand?.ID == compItem.ID;
                        case "amulet":
                            return _player.Inventory.Amulet?.ID == compItem.ID;
                        case "ring":
                            return _player.Inventory.Ring1?.ID == compItem.ID || _player.Inventory.Ring2?.ID == compItem.ID;
                        default:
                            // Если слот не задан или неизвестен — считаем, что не экипировано
                            return false;
                    }
                }
            }

            // По умолчанию — не экипировано
            return false;
        }


        private void InitializeAvailableActions()
        {
            _availableActions = new List<string>();

            if (_selectedItem is InventoryItem inventoryItem)
            {
                // вычисляем флаги
                bool isConsumable = inventoryItem.Details.Type == ItemType.Consumable;
                bool isEquipment = ItemHelpers.IsEquipable(inventoryItem.Details);
                _isAlreadyEquipped = IsItemEquipped(inventoryItem.Details);

                // DIAGNOSTIC: подробный лог (runtime-type, id, ItemType)
                var runtimeType = inventoryItem.Details?.GetType().FullName ?? "<null>";
                var id = inventoryItem.Details?.ID ?? -1;
                var itemType = inventoryItem.Details?.Type.ToString() ?? "<null>";
                DebugConsole.Log($"ОТЛАДКА: isConsumable={isConsumable}, isEquipment={isEquipment}, alreadyEquipped={_isAlreadyEquipped} | runtimeType={runtimeType} | ID={id} | Type={itemType}");

                // Действия
                if (isConsumable)
                {
                    _availableActions.Add("Использовать");
                }

                if (isEquipment && !_isAlreadyEquipped)
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
            ClearScreen();

            RenderHeader($"ДЕЙСТВИЯ : {_itemName}");
            RenderItemInfoAndActions();
            RenderFooter("W/S - выбор │ E - выполнить │ Q - назад");
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
            HandleCommonInput(keyInfo);

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
                    _player.UseItem(inventoryItem);
                    //_player.UseItemToHeal(inventoryItem);
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
                            //MessageSystem.AddMessage($"Надето: {inventoryItem.Details.Name}");
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