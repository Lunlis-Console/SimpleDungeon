using Engine.Core;
using Engine.Entities;
using System.Reflection;

namespace Engine.InventorySystem
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

        private readonly Player _player;

        public Inventory()
        {
            Items = new List<InventoryItem>();
            EquippedItems = new List<EquipmentItem>();
        }

        public Inventory(Player player)
        {
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public void AddItem(Item item, int quantity = 1)
        {

            if (item == null)
            {
                var stackTrace = new System.Diagnostics.StackTrace();
                DebugConsole.Log($"Попытка добавить null предмет. Вызов из: {stackTrace.ToString()}");
                return;
            }

            if (Items == null)
            {
                DebugConsole.Log("Items list was null, initializing");
                Items = new List<InventoryItem>();
            }

            InventoryItem existingItem = Items
                .Where(ii => ii != null && ii.Details != null)
                .FirstOrDefault(ii => ii.Details.ID == item.ID);

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
            DebugConsole.Log($"[RemoveItem] Attempting to remove item: {item?.Details?.Name ?? "null"}, quantity: {quantity}");
            DebugConsole.Log($"[RemoveItem] Current inventory items count: {Items.Count}");
            
            // Проверяем, что предмет существует в инвентаре
            if (item == null || !Items.Contains(item))
            {
                DebugConsole.Log($"[RemoveItem] Item not found in inventory. Item: {item?.Details?.Name ?? "null"}");
                return;
            }

            DebugConsole.Log($"[RemoveItem] Item found in inventory, current quantity: {item.Quantity}");
            
            item.Quantity -= quantity;
            DebugConsole.Log($"[RemoveItem] Item quantity after reduction: {item.Quantity}");
            
            if (item.Quantity <= 0)
            {
                DebugConsole.Log($"[RemoveItem] Removing item from inventory completely");
                Items.Remove(item);
            }

            DebugConsole.Log($"[RemoveItem] Final inventory items count: {Items.Count}");
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
            DebugConsole.Log($"DEBUG: Inventory.EquipItem called for ID={(inventoryItem?.Details?.ID ?? -1)}, Name={(inventoryItem?.Details?.Name ?? "<null>")}, Type={(inventoryItem?.Details?.Type.ToString() ?? "<null>")}");

            if (inventoryItem == null || inventoryItem.Details == null)
            {
                DebugConsole.Log("DEBUG: Equip failed — null item/details.");
                return false;
            }

            // запретим экипировку для мусора/расходников
            if (inventoryItem.Details.Type == ItemType.Stuff ||
                inventoryItem.Details.Type == ItemType.Consumable)
            {
                DebugConsole.Log("DEBUG: Item type is Stuff/Consumable — cannot equip.");
                return false;
            }

            Equipment equipment = null;

            // 1) Если уже Equipment — используем напрямую
            if (inventoryItem.Details is Equipment existingEq)
            {
                equipment = existingEq;
                DebugConsole.Log("DEBUG: Detected runtime Equipment instance — using it.");
            }
            else
            {
                // 2) Если CompositeItem — пытаемся получить EquipComponent
                if (inventoryItem.Details is CompositeItem comp)
                {
                    var equipComp = comp.Components?.OfType<EquipComponent>().FirstOrDefault();
                    if (equipComp != null)
                    {
                        DebugConsole.Log("DEBUG: CompositeItem has EquipComponent — constructing Equipment from component.");
                        equipment = new Equipment(
                            comp.ID,
                            string.IsNullOrWhiteSpace(comp.NamePlural) ? comp.Name : comp.NamePlural,
                            equipComp.AttackBonus,
                            equipComp.DefenceBonus,
                            equipComp.AgilityBonus,
                            equipComp.HealthBonus,
                            comp.Type,
                            comp.Price,
                            comp.Name,
                            comp.Description
                        );
                    }
                    else
                    {
                        DebugConsole.Log("DEBUG: CompositeItem has NO EquipComponent — attempting reflection fallback.");
                        // reflection fallback — ищем бонусные свойства прямо на объекте
                        var t = comp.GetType();
                        int attack = GetIntPropertySafe(comp, "AttackBonus");
                        int def = GetIntPropertySafe(comp, "DefenceBonus");
                        int agi = GetIntPropertySafe(comp, "AgilityBonus");
                        int hp = GetIntPropertySafe(comp, "HealthBonus");

                        if ((attack | def | agi | hp) != 0)
                        {
                            DebugConsole.Log("DEBUG: Reflection found non-zero bonus properties — constructing Equipment from these values.");
                            equipment = new Equipment(
                                comp.ID,
                                string.IsNullOrWhiteSpace(comp.NamePlural) ? comp.Name : comp.NamePlural,
                                attack,
                                def,
                                agi,
                                hp,
                                comp.Type,
                                comp.Price,
                                comp.Name,
                                comp.Description
                            );
                        }
                        else
                        {
                            DebugConsole.Log("DEBUG: Reflection fallback found no bonus properties on CompositeItem.");
                        }
                    }
                }
                else
                {
                    // 3) Не CompositeItem — пробуем рефлексией получить бонусы (старый ItemData-like)
                    var it = inventoryItem.Details;
                    int attack = GetIntPropertySafe(it, "AttackBonus");
                    int def = GetIntPropertySafe(it, "DefenceBonus");
                    int agi = GetIntPropertySafe(it, "AgilityBonus");
                    int hp = GetIntPropertySafe(it, "HealthBonus");

                    if ((attack | def | agi | hp) != 0 || IsItemTypeEquipable(it.Type))
                    {
                        DebugConsole.Log("DEBUG: Non-composite item has bonuses or equippable ItemType — constructing Equipment via fallback.");
                        equipment = new Equipment(
                            it.ID,
                            string.IsNullOrWhiteSpace(it.NamePlural) ? it.Name : it.NamePlural,
                            attack,
                            def,
                            agi,
                            hp,
                            it.Type,
                            it.Price,
                            it.Name,
                            it.Description
                        );
                    }
                }
            }

            if (equipment == null)
            {
                DebugConsole.Log("DEBUG: Could not determine Equipment instance to equip — aborting.");
                return false;
            }

            // --- Далее оригинальная логика с обработкой конфликтов/слотов ---
            Equipment itemToUnequip = null;
            string unequipMessage = "";

            if (equipment.Type == ItemType.TwoHandedWeapon)
            {
                if (OffHand != null)
                {
                    itemToUnequip = OffHand;
                    unequipMessage = $"Снято: {OffHand.Name} (конфликт с двуручным оружием)";
                }
            }
            else if (equipment.Type == ItemType.OffHand)
            {
                if (MainHand != null && MainHand.Type == ItemType.TwoHandedWeapon)
                {
                    itemToUnequip = MainHand;
                    unequipMessage = $"Снято: {MainHand.Name} (конфликт со щитом)";
                }
            }
            else if (equipment.Type == ItemType.OneHandedWeapon)
            {
                if (MainHand != null && MainHand.Type == ItemType.TwoHandedWeapon)
                {
                    itemToUnequip = MainHand;
                    unequipMessage = $"Снято: {MainHand.Name} (конфликт с одноручным оружием)";
                }
            }

            if (itemToUnequip != null)
            {
                UnequipItem(itemToUnequip, true);
                DebugConsole.Log(unequipMessage);
            }

            // Ринг - отдельная обработка
            if (equipment.Type == ItemType.Ring)
            {
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
                    UnequipItem(Ring1, true);
                    Ring1 = equipment;
                }
            }
            else
            {
                Equipment currentEquipment = GetEquipmentInSlot(equipment.Type);

                if (currentEquipment != null)
                {
                    UnequipItem(currentEquipment, true);
                }

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
                        DebugConsole.Log($"DEBUG: Unknown equipment type {equipment.Type} — cannot equip.");
                        return false;
                }
            }

            // Удаляем предмет из инвентаря (уменьшаем количество)
            RemoveItem(inventoryItem, 1);

            // Добавляем в список экипированных
            EquippedItems.Add(new EquipmentItem(equipment, 1));

            OnEquipmentChanged?.Invoke();

            DebugConsole.Log($"DEBUG: Successfully equipped ID={equipment.ID}, Name={equipment.Name}, Slot={equipment.Type}");
            return true;
        }

        // Вспомогательные методы (можно поместить рядом, приватными)
        private static int GetIntPropertySafe(object obj, string propName)
        {
            try
            {
                var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance);
                if (p == null) return 0;
                var v = p.GetValue(obj);
                if (v is int i) return i;
                if (v is int?) return (int?)(v) ?? 0;
                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private static bool IsItemTypeEquipable(ItemType t)
        {
            return t == ItemType.Helmet ||
                   t == ItemType.Armor ||
                   t == ItemType.Gloves ||
                   t == ItemType.Boots ||
                   t == ItemType.OneHandedWeapon ||
                   t == ItemType.TwoHandedWeapon ||
                   t == ItemType.OffHand ||
                   t == ItemType.Amulet ||
                   t == ItemType.Ring;
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
            int baseDef = (Helmet?.DefenceBonus ?? 0) +
                          (Armor?.DefenceBonus ?? 0) +
                          (Gloves?.DefenceBonus ?? 0) +
                          (Boots?.DefenceBonus ?? 0) +
                          (OffHand?.DefenceBonus ?? 0) +
                          (Amulet?.DefenceBonus ?? 0) +
                          (Ring1?.DefenceBonus ?? 0) +
                          (Ring2?.DefenceBonus ?? 0);

            // учесть временный бафф
            return baseDef + (_player?.TemporaryDefenceBuff ?? 0);
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