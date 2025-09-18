using Engine.Core;
using Engine.Entities;

namespace Engine.InventorySystem
{
    public static class ItemCategoryHelper
    {
        public static string GetCategoryDisplayName(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.All => "Все",
                ItemCategory.Weapons => "Оружие",
                ItemCategory.Armor => "Броня",
                ItemCategory.Books => "Книги",
                ItemCategory.QuestItems => "Квестовые предметы",
                ItemCategory.Other => "Прочее",
                _ => "Неизвестно"
            };
        }

        public static ItemCategory GetItemCategory(Item item)
        {
            return item.Type switch
            {
                ItemType.OneHandedWeapon or ItemType.TwoHandedWeapon => ItemCategory.Weapons,
                ItemType.Helmet or ItemType.Armor or ItemType.Gloves or ItemType.Boots or 
                ItemType.OffHand or ItemType.Ring or ItemType.Amulet => ItemCategory.Armor,
                ItemType.Quest => ItemCategory.QuestItems,
                ItemType.Consumable => ItemCategory.Other, // Пока что расходники в "Прочее"
                ItemType.Stuff => ItemCategory.Other,
                _ => ItemCategory.Other
            };
        }

        public static ItemCategory GetItemCategory(object item)
        {
            return item switch
            {
                InventoryItem invItem => GetItemCategory(invItem.Details),
                InventoryUI.EquipmentSlotItem eqItem => GetItemCategory(eqItem.Equipment),
                _ => ItemCategory.Other
            };
        }

        public static bool ItemMatchesCategory(object item, ItemCategory category)
        {
            if (category == ItemCategory.All)
                return true;

            return GetItemCategory(item) == category;
        }

        public static List<object> FilterItemsByCategory(List<object> items, ItemCategory category)
        {
            if (category == ItemCategory.All)
                return items;

            return items.Where(item => ItemMatchesCategory(item, category)).ToList();
        }
    }
}
