namespace Engine
{
    public class Inventory
    {
        public List<InventoryItem> Items { get; private set; }
        public List<EquipmentItem> EquippedItems { get; private set; }
        // Броня
        public Equipment Helmet { get; private set; }
        public Equipment Armor { get; private set; }
        public Equipment Gloves { get; private set; }
        public Equipment Boots { get; private set; }
        public Equipment Weapon { get; private set; }
        // Оружие
        public Equipment MainHand { get; private set; }    // Основная рука
        public Equipment OffHand { get; private set; }     // Вторая рука (щит или оружие)

        // Аксессуары
        public Equipment Amulet { get; private set; }
        public Equipment Ring1 { get; private set; }       // Первое кольцо
        public Equipment Ring2 { get; private set; }       // Второе кольцо

        public event Action OnInventoryChanged;
        public event Action OnEquipmentChanged;

        public Inventory()
        {
            Items = new List<InventoryItem>();
            EquippedItems = new List<EquipmentItem>();
        }

        public void AddItem(Item item, int quantity = 1)
        {
            InventoryItem existingItem = Items.FirstOrDefault(ii => ii.Details.ID == item.ID);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Items.Add(new InventoryItem(item, quantity));
            }

            OnInventoryChanged?.Invoke();
        }

        public void RemoveItem(Item item, int quantity = 1)
        {
            InventoryItem existingItem = Items.FirstOrDefault(ii => ii.Details.ID == item.ID);

            if (existingItem != null)
            {
                existingItem.Quantity -= quantity;
                if (existingItem.Quantity <= 0)
                {
                    Items.Remove(existingItem);
                }
            }

            OnInventoryChanged?.Invoke();
        }

        public void RemoveItem(InventoryItem item, int quantity = 1)
        {
            item.Quantity -= quantity;
            if (item.Quantity <= 0)
            {
                Items.Remove(item);
            }

            OnInventoryChanged?.Invoke();
        }

        public bool HasItem(int itemId, int quantity = 1)
        {
            InventoryItem item = Items.FirstOrDefault(ii => ii.Details.ID == itemId);
            return item != null && item.Quantity >= quantity;
        }

        public int GetItemQuantity(int itemId)
        {
            InventoryItem item = Items.FirstOrDefault(ii => ii.Details.ID == itemId);
            return item?.Quantity ?? 0;
        }

        public bool EquipItem(InventoryItem inventoryItem)
        {
            if (inventoryItem.Details.Type == ItemType.Stuff ||
                inventoryItem.Details.Type == ItemType.Consumable)
            {
                return false;
            }

            Equipment equipment = inventoryItem.Details as Equipment;
            if (equipment == null) return false;

            // Сохраняем ссылки на предметы, которые будем снимать
            Equipment itemToUnequip = null;
            string unequipMessage = "";

            // Автоматическое снятие конфликтующей экипировки
            if (equipment.Type == ItemType.TwoHandedWeapon)
            {
                // Если вторая рука занята - автоматически снимаем
                if (OffHand != null)
                {
                    itemToUnequip = OffHand;
                    unequipMessage = $"Снято: {OffHand.Name} (конфликт с двуручным оружием)";
                }
            }
            else if (equipment.Type == ItemType.OffHand)
            {
                // Если надето двуручное оружие - автоматически снимаем
                if (MainHand != null && MainHand.Type == ItemType.TwoHandedWeapon)
                {
                    itemToUnequip = MainHand;
                    unequipMessage = $"Снято: {MainHand.Name} (конфликт со щитом)";
                }
            }
            else if (equipment.Type == ItemType.OneHandedWeapon)
            {
                // Если надето двуручное оружие - автоматически снимаем
                if (MainHand != null && MainHand.Type == ItemType.TwoHandedWeapon)
                {
                    itemToUnequip = MainHand;
                    unequipMessage = $"Снято: {MainHand.Name} (конфликт с одноручным оружием)";
                }
            }

            // Снимаем конфликтующий предмет ДО того, как будем обращаться к его свойствам
            if (itemToUnequip != null)
            {
                UnequipItem(itemToUnequip, true);
                MessageSystem.AddMessage(unequipMessage);
            }

            // Для колец обрабатываем отдельно, так как слотов два
            if (equipment.Type == ItemType.Ring)
            {
                // Если есть свободный слот для кольца, надеваем в него
                if (Ring1 == null)
                {
                    Ring1 = equipment;
                }
                else if (Ring2 == null)
                {
                    Ring2 = equipment;
                }
                else
                {
                    // Если оба слота заняты, предлагаем заменить одно из колец
                    // Для простоты заменяем первое кольцо
                    UnequipItem(Ring1, true);
                    Ring1 = equipment;
                }
            }
            else
            {
                // Для всех остальных типов экипировки
                // Получаем текущий предмет в этом слоте
                Equipment currentEquipment = GetEquipmentInSlot(equipment.Type);

                // Если в слоте уже что-то есть, снимаем это
                if (currentEquipment != null)
                {
                    UnequipItem(currentEquipment, true);
                }

                // Надеваем новый предмет в соответствующий слот
                switch (equipment.Type)
                {
                    case ItemType.Helmet:
                        Helmet = equipment;
                        break;
                    case ItemType.Armor:
                        Armor = equipment;
                        break;
                    case ItemType.Gloves:
                        Gloves = equipment;
                        break;
                    case ItemType.Boots:
                        Boots = equipment;
                        break;
                    case ItemType.OneHandedWeapon:
                    case ItemType.TwoHandedWeapon:
                        MainHand = equipment;
                        break;
                    case ItemType.OffHand:
                        OffHand = equipment;
                        break;
                    case ItemType.Amulet:
                        Amulet = equipment;
                        break;
                    default:
                        return false;
                }
            }

            // Удаляем новый предмет из инвентаря
            RemoveItem(inventoryItem, 1);

            // Добавляем его в список экипированных
            EquippedItems.Add(new EquipmentItem(equipment, 1));

            OnEquipmentChanged?.Invoke();
            return true;
        }
        public bool UnequipItem(Equipment equipment, bool addToInventory = true)
        {
            if (equipment == null) return false;

            // Снимаем предмет
            switch (equipment.Type)
            {
                case ItemType.Helmet:
                    if (Helmet != equipment) return false;
                    Helmet = null;
                    break;
                case ItemType.Armor:
                    if (Armor != equipment) return false;
                    Armor = null;
                    break;
                case ItemType.Gloves:
                    if (Gloves != equipment) return false;
                    Gloves = null;
                    break;
                case ItemType.Boots:
                    if (Boots != equipment) return false;
                    Boots = null;
                    break;
                case ItemType.OneHandedWeapon:
                case ItemType.TwoHandedWeapon:
                    if (MainHand != equipment) return false;
                    MainHand = null;
                    break;
                case ItemType.OffHand:
                    if (OffHand != equipment) return false;
                    OffHand = null;
                    break;
                case ItemType.Amulet:
                    if (Amulet != equipment) return false;
                    Amulet = null;
                    break;
                case ItemType.Ring:
                    if (Ring1 == equipment) Ring1 = null;
                    else if (Ring2 == equipment) Ring2 = null;
                    else return false;
                    break;
                default:
                    return false;
            }

            // Удаляем из списка экипировки
            var equipmentItem = EquippedItems.FirstOrDefault(ei => ei.Details.ID == equipment.ID);
            if (equipmentItem != null)
            {
                EquippedItems.Remove(equipmentItem);
            }

            // Добавляем в инвентарь только если нужно
            if (addToInventory)
            {
                AddItem(equipment, 1);
            }

            OnEquipmentChanged?.Invoke();
            return true;
        }

        public Equipment GetEquipmentInSlot(ItemType type)
        {
            return type switch
            {
                ItemType.Helmet => Helmet,
                ItemType.Armor => Armor,
                ItemType.Gloves => Gloves,
                ItemType.Boots => Boots,
                ItemType.OneHandedWeapon => MainHand,
                ItemType.TwoHandedWeapon => MainHand,
                ItemType.OffHand => OffHand,
                ItemType.Amulet => Amulet,
                ItemType.Ring => null, // Для колец возвращаем null, так как их два
                _ => null
            };
        }


        public int CalculateTotalDefence()
        {
            return (Helmet?.DefenceBonus ?? 0) +
                   (Armor?.DefenceBonus ?? 0) +
                   (Gloves?.DefenceBonus ?? 0) +
                   (Boots?.DefenceBonus ?? 0) +
                   (OffHand?.DefenceBonus ?? 0) +
                   (Amulet?.DefenceBonus ?? 0) +
                   (Ring1?.DefenceBonus ?? 0) +
                   (Ring2?.DefenceBonus ?? 0);
        }

        public int CalculateTotalAttack()
        {
            return (MainHand?.AttackBonus ?? 0) +
                   (OffHand?.AttackBonus ?? 0) +
                   (Amulet?.AttackBonus ?? 0) +
                   (Ring1?.AttackBonus ?? 0) +
                   (Ring2?.AttackBonus ?? 0);
        }

        public int CalculateTotalAgility()
        {
            return (Helmet?.AgilityBonus ?? 0) +
                   (Armor?.AgilityBonus ?? 0) +
                   (Gloves?.AgilityBonus ?? 0) +
                   (Boots?.AgilityBonus ?? 0) +
                   (MainHand?.AgilityBonus ?? 0) +
                   (OffHand?.AgilityBonus ?? 0) +
                   (Amulet?.AgilityBonus ?? 0) +
                   (Ring1?.AgilityBonus ?? 0) +
                   (Ring2?.AgilityBonus ?? 0);
        }

        public int CalculateTotalHealth()
        {
            return (Helmet?.HealthBonus ?? 0) +
                   (Armor?.HealthBonus ?? 0) +
                   (Amulet?.HealthBonus ?? 0) +
                   (Ring1?.HealthBonus ?? 0) +
                   (Ring2?.HealthBonus ?? 0);
        }

        public void Clear()
        {
            Items.Clear();
            EquippedItems.Clear();
            Helmet = null;
            Armor = null;
            Gloves = null;
            Boots = null;
            MainHand = null;
            OffHand = null;
            Amulet = null;
            Ring1 = null;
            Ring2 = null;

            OnInventoryChanged?.Invoke();
            OnEquipmentChanged?.Invoke();
        }
                
    }
}