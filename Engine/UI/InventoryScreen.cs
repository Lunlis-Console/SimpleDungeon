using Engine.Core;
using Engine.Entities;
using Engine.InventorySystem;
using Engine.Quests;
using Engine.Trading;

namespace Engine.UI
{
    public class InventoryScreen : BaseScreen
    {
        private readonly Player _player;
        private int _selectedIndex;
        private List<object> _displayItems;
        private object _lastSelectedItem; // Для отслеживания изменений

        public InventoryScreen(Player player)
        {
            _player = player;
            _selectedIndex = 0;
            _displayItems = InventoryUI.PrepareInventoryItems(player);
            _lastSelectedItem = null;
        }

        public class MenuOption
        {
            public string DisplayText { get; set; }
            public Action Action { get; set; }

            public MenuOption(string displayText, Action action)
            {
                DisplayText = displayText;
                Action = action;
            }
        }

        public override void Render()
        {
            ClearScreen();
            RenderHeader("ИНВЕНТАРЬ");
            RenderInventoryItems();
            RenderItemInfo(); // Новая область для информации о предмете
            RenderFooter("W/S - выбор │ E - действие │ Q - назад");
        }

        private void RenderInventoryItems()
        {
            int startY = 4;
            int maxItems = Console.WindowHeight - 10; // Уменьшили высоту списка

            for (int i = 0; i < Math.Min(_displayItems.Count, maxItems); i++)
            {
                bool isSelected = i == _selectedIndex;
                string itemText = GetItemDisplayText(_displayItems[i]);

                if (isSelected)
                {
                    _renderer.Write(2, startY + i, "► ");
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
        private void RenderItemInfo()
        {
            // Область для информации о предмете в нижней части экрана
            int infoAreaY = Console.WindowHeight - 10;
            int infoAreaHeight = 6;

            // Очищаем область
            _renderer.FillArea(0, infoAreaY, Console.WindowWidth, infoAreaHeight, ' ',
                              ConsoleColor.White, ConsoleColor.Black);

            // Разделительная линия
            _renderer.Write(0, infoAreaY, new string('─', Console.WindowWidth), ConsoleColor.DarkGray);

            if (_displayItems.Count == 0 || _selectedIndex >= _displayItems.Count)
            {
                _renderer.Write(2, infoAreaY + 2, "Нет предметов для отображения", ConsoleColor.DarkGray);
                return;
            }

            var selectedItem = _displayItems[_selectedIndex];
            RenderSelectedItemInfo(selectedItem, infoAreaY + 1);
        }
        private void RenderSelectedItemInfo(object item, int startY)
        {
            string itemName = "";
            string itemType = "";
            string itemDescription = "";
            int itemPrice = 0;
            int itemQuantity = 0;
            bool isEquipped = false;

            if (item is InventoryItem inventoryItem)
            {
                itemName = inventoryItem.Details.Name;
                itemType = GetTypeText(inventoryItem.Details.Type);
                itemDescription = inventoryItem.Details.Description;
                itemPrice = inventoryItem.Details.Price;
                itemQuantity = inventoryItem.Quantity;
                isEquipped = IsItemEquipped(inventoryItem.Details);
            }
            else if (item is InventoryUI.EquipmentSlotItem equipmentItem)
            {
                itemName = equipmentItem.Equipment.Name;
                itemType = GetTypeText(equipmentItem.Equipment.Type);
                itemDescription = equipmentItem.Equipment.Description;
                itemPrice = equipmentItem.Equipment.Price;
                itemQuantity = 1;
                isEquipped = true;
            }

            int y = startY;

            // Название и статус экипировки
            string nameLine = itemName;
            if (isEquipped)
            {
                nameLine = "[Надето] " + itemName;
                _renderer.Write(2, y, nameLine, ConsoleColor.Green);
            }
            else
            {
                _renderer.Write(2, y, nameLine, ConsoleColor.White);
            }
            y++;

            // Тип и цена
            _renderer.Write(2, y, $"Тип: {itemType} | Цена: {itemPrice} золота", ConsoleColor.Cyan);
            y++;

            // Количество (если не экипировано)
            if (!isEquipped && itemQuantity > 1)
            {
                _renderer.Write(2, y, $"Количество: {itemQuantity} шт.", ConsoleColor.Yellow);
                y++;
            }
            y++;

            // Описание (первые несколько строк)
            var descriptionLines = WrapText(itemDescription, Console.WindowWidth - 4);
            for (int i = 0; i < Math.Min(2, descriptionLines.Count); i++)
            {
                if (y < Console.WindowHeight - 3) // Не выходим за футер
                {
                    _renderer.Write(2, y, descriptionLines[i], ConsoleColor.Gray);
                    y++;
                }
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
        private bool IsItemEquipped(Item item)
        {
            if (item is Equipment equipment)
            {
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
            HandleCommonInput(keyInfo);

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    _selectedIndex = Math.Max(0, _selectedIndex - 1);
                    RequestPartialRedraw(); // Запрашиваем частичную перерисовку
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    _selectedIndex = Math.Min(_displayItems.Count - 1, _selectedIndex + 1);
                    RequestPartialRedraw(); // Запрашиваем частичную перерисовку
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
        public void RefreshInventoryList()
        {
            _displayItems = InventoryUI.PrepareInventoryItems(_player);
            _selectedIndex = Math.Min(_selectedIndex, _displayItems.Count - 1);
            RequestRedraw();
        }

        // Переопределяем Update для частичной перерисовки при изменении выбора
        public override void Update()
        {
            if (_needsRedraw)
            {
                // Проверяем, изменился ли выбранный предмет
                var currentSelectedItem = _displayItems.Count > 0 && _selectedIndex < _displayItems.Count
                    ? _displayItems[_selectedIndex]
                    : null;

                if (currentSelectedItem != _lastSelectedItem || _needsFullRedraw)
                {
                    // Полная перерисовка при первом показе или изменении экрана
                    Render();
                    _lastSelectedItem = currentSelectedItem;
                }
                else
                {
                    // Частичная перерисовка только области информации о предмете
                    RenderItemInfoPartial();
                }

                _needsRedraw = false;
                _needsFullRedraw = false;
            }
        }
        private void RenderItemInfoPartial()
        {
            // Частичная перерисовка только области информации о предмете
            int infoAreaY = Console.WindowHeight - 10;
            int infoAreaHeight = 6;

            // Очищаем только область информации
            _renderer.FillArea(0, infoAreaY, Console.WindowWidth, infoAreaHeight, ' ',
                              ConsoleColor.White, ConsoleColor.Black);

            // Разделительная линия
            _renderer.Write(0, infoAreaY, new string('─', Console.WindowWidth), ConsoleColor.DarkGray);

            if (_displayItems.Count > 0 && _selectedIndex < _displayItems.Count)
            {
                RenderSelectedItemInfo(_displayItems[_selectedIndex], infoAreaY + 1);
            }
            else
            {
                _renderer.Write(2, infoAreaY + 2, "Нет предметов для отображения", ConsoleColor.DarkGray);
            }
        }
        private void HandleItemSelection()
        {
            if (_selectedIndex < 0 || _selectedIndex >= _displayItems.Count)
                return;

            var selectedItem = _displayItems[_selectedIndex];
            ScreenManager.PushScreen(new InventoryItemActionScreen(_player, selectedItem));
        }

        private void InteractWithNPC(NPC npc)
        {
            var actions = npc.GetAvailableActions(_player);

            string selectedAction = MenuSystem.SelectFromList(
                actions,
                action => action,
                $"Взаимодействие с {npc.Name}",
                "W/S - выбор, E - подтвердить, Q - отмена"
            );

            if (selectedAction != null)
            {
                switch (selectedAction)
                {
                    case "Поговорить":
                        npc.Talk(_player);
                        break;

                    case "Торговать":
                        if (npc.Trader != null)
                        {
                            npc.Trader.InitializeShop(_player);
                            var tradeScreen = new Engine.UI.TradeScreen(npc.Trader, _player);
                            ScreenManager.PushScreen(tradeScreen);
                        }
                        break;


                    case "Задания":
                        ShowQuestMenu(npc, _player);
                        break;

                    case "Осмотреть":
                        npc.Examine(_player);
                        break;
                }

                ScreenManager.RequestPartialRedraw();
            }
        }
        private void ShowQuestMenu(NPC npc, Player player)
        {
            var menuOptions = new List<MenuOption>();

            // Доступные квесты
            if (npc.QuestsToGive != null)
            {
                foreach (var questID in npc.QuestsToGive)
                {
                    var quest = player.QuestLog.GetQuest(questID);
                    if (quest == null) continue;

                    if (!player.QuestLog.ActiveQuests.Any(q => q.ID == questID) &&
                        !player.QuestLog.CompletedQuests.Any(q => q.ID == questID))
                    {
                        menuOptions.Add(new MenuOption($"Принять: {quest.Name}", () =>
                        {
                            player.StartQuest(questID);
                            MessageSystem.AddMessage($"Квест принят: {quest.Name}");
                        }));
                    }
                }
            }

            // Квесты для сдачи
            foreach (var quest in player.QuestLog.ActiveQuests)
            {
                if (quest.State == QuestState.ReadyToComplete)
                {
                    menuOptions.Add(new MenuOption($"Сдать: {quest.Name} ✓", () =>
                    {
                        player.CompleteQuest(quest.ID);
                        MessageSystem.AddMessage($"Квест завершен: {quest.Name}");
                    }));
                }
            }

            menuOptions.Add(new MenuOption("Назад", () => { }));

            var selected = MenuSystem.SelectFromList(
                menuOptions,
                opt => opt.DisplayText,
                $"Задания - {npc.Name}"
            );

            selected?.Action();
        }
    }
}