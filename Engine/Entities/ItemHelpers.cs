using Engine.Core;
using Engine.Entities;
using System.Linq;
using System.Reflection;

public static class ItemHelpers
{
    public static bool IsEquipable(Item item)
    {
        if (item == null) return false;

        // 1) Явный runtime-класс Equipment
        if (item is Equipment) return true;

        // 2) CompositeItem + EquipComponent
        if (item is CompositeItem comp)
        {
            if (comp.Components != null && comp.Components.Any(c => c is EquipComponent))
                return true;
        }

        // 3) По ItemType (если кто-то ставил правильный Type в JSON)
        if (item.Type == ItemType.Helmet ||
            item.Type == ItemType.Armor ||
            item.Type == ItemType.Gloves ||
            item.Type == ItemType.Boots ||
            item.Type == ItemType.OneHandedWeapon ||
            item.Type == ItemType.TwoHandedWeapon ||
            item.Type == ItemType.OffHand ||
            item.Type == ItemType.Amulet ||
            item.Type == ItemType.Ring)
        {
            return true;
        }

        // 4) Защита от случаев, когда runtime-класс не совпал, но свойства есть (reflection fallback)
        //    Если у объекта есть свойства AttackBonus/DefenceBonus/AgilityBonus/HealthBonus и они != 0 -> считаем экипируемым
        var t = item.GetType();
        var pAttack = t.GetProperty("AttackBonus", BindingFlags.Public | BindingFlags.Instance);
        var pDef = t.GetProperty("DefenceBonus", BindingFlags.Public | BindingFlags.Instance);
        var pAgi = t.GetProperty("AgilityBonus", BindingFlags.Public | BindingFlags.Instance);
        var pHealth = t.GetProperty("HealthBonus", BindingFlags.Public | BindingFlags.Instance);

        bool hasNonZeroBonus = false;
        if (pAttack != null && (pAttack.GetValue(item) as int? ?? 0) != 0) hasNonZeroBonus = true;
        if (pDef != null && (pDef.GetValue(item) as int? ?? 0) != 0) hasNonZeroBonus = true;
        if (pAgi != null && (pAgi.GetValue(item) as int? ?? 0) != 0) hasNonZeroBonus = true;
        if (pHealth != null && (pHealth.GetValue(item) as int? ?? 0) != 0) hasNonZeroBonus = true;

        if (hasNonZeroBonus) return true;

        return false;
    }
}
