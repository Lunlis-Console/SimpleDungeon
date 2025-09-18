namespace Engine.Core
{
    public enum ItemType
    {
        Consumable,
        OneHandedWeapon,
        TwoHandedWeapon,
        Helmet,
        Armor,
        Gloves,
        Boots,
        OffHand,
        Ring,
        Amulet,
        Stuff,
        Quest,
        Container
    }

    public enum ItemCategory
    {
        All,           // Все
        Weapons,       // Оружие
        Armor,         // Броня (включая амулеты и кольца)
        Books,         // Книги
        QuestItems,    // Квестовые предметы
        Other          // Прочее
    }
}

