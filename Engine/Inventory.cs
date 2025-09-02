using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine
{
    public class Inventory
    {
        public List<InventoryItem> Items { get; private set; }
        public List<EquipmentItem> EquippedItems { get; private set; }

        public Equipment Helmet { get; private set; }
        public Equipment Armor { get; private set; }
        public Equipment Gloves { get; private set; }
        public Equipment Boots { get; private set; }
        public Equipment Weapon { get; private set; }

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

            // Проверяем, есть ли что-то надетое в слоте
            Equipment currentEquipment = GetEquipmentInSlot(equipment.Type);
            if (currentEquipment != null)
            {
                return false;
            }

            // Надеваем предмет
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
                case ItemType.Sword:
                    Weapon = equipment;
                    break;
                default:
                    return false;
            }

            EquippedItems.Add(new EquipmentItem(equipment, 1));
            RemoveItem(inventoryItem, 1);

            OnEquipmentChanged?.Invoke();
            return true;
        }

        public bool UnequipItem(Equipment equipment)
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
                case ItemType.Sword:
                    if (Weapon != equipment) return false;
                    Weapon = null;
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

            AddItem(equipment, 1);

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
                ItemType.Sword => Weapon,
                _ => null
            };
        }

        public int CalculateTotalDefence()
        {
            return (Helmet?.DefenceBonus ?? 0) +
                   (Armor?.DefenceBonus ?? 0) +
                   (Gloves?.DefenceBonus ?? 0) +
                   (Boots?.DefenceBonus ?? 0);
        }

        public int CalculateTotalAttack()
        {
            return Weapon?.AttackBonus ?? 0;
        }

        public void Clear()
        {
            Items.Clear();
            EquippedItems.Clear();
            Helmet = null;
            Armor = null;
            Gloves = null;
            Boots = null;
            Weapon = null;

            OnInventoryChanged?.Invoke();
            OnEquipmentChanged?.Invoke();
        }
    }
}