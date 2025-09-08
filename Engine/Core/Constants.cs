namespace Engine.Core
{
    public class Constants
    {
        // ========== ДИАПАЗОНЫ ID ==========
        private const int ITEMS_BASE = 1000;
        private const int MONSTERS_BASE = 2000;
        private const int LOCATIONS_BASE = 3000;
        private const int NPCS_BASE = 4000;
        private const int QUESTS_BASE = 5000;
        private const int TITLES_BASE = 6000;

        // ========== ПРЕДМЕТЫ (1000-1999) ==========
        // Расходники (1000-1099)
        public const int ITEM_ID_RATS_MEAT = ITEMS_BASE + 1;
        public const int ITEM_ID_WEAK_HEALING_POTION = ITEMS_BASE + 2;

        // Оружие (1100-1199)
        public const int ITEM_ID_RUSTY_SWORD = ITEMS_BASE + 101;
        public const int ITEM_ID_IRON_SWORD = ITEMS_BASE + 102;
        public const int ITEM_ID_IRON_GREATSWORD = ITEMS_BASE + 103;

        // Броня (1200-1299)
        public const int ITEM_ID_LEATHER_HELMET = ITEMS_BASE + 201;
        public const int ITEM_ID_LEATHER_ARMOR = ITEMS_BASE + 202;
        public const int ITEM_ID_LEATHER_GLOVES = ITEMS_BASE + 203;
        public const int ITEM_ID_LEATHER_BOOTS = ITEMS_BASE + 204;
        public const int ITEM_ID_IRON_HELMET = ITEMS_BASE + 205;
        public const int ITEM_ID_IRON_ARMOR = ITEMS_BASE + 206;
        public const int ITEM_ID_IRON_GLOVES = ITEMS_BASE + 207;
        public const int ITEM_ID_IRON_BOOTS = ITEMS_BASE + 208;

        // Щиты (1300-1399)
        public const int ITEM_ID_IRON_SHIELD = ITEMS_BASE + 301;

        // Аксессуары (1400-1499)
        public const int ITEM_ID_FAMILY_RING = ITEMS_BASE + 401;
        public const int ITEM_ID_GOLD_RING = ITEMS_BASE + 402;
        public const int ITEM_ID_RICH_AMULET = ITEMS_BASE + 403;

        // Ресурсы (1500-1599)
        public const int ITEM_ID_SPIDER_SILK = ITEMS_BASE + 501;

        // Квестовые предметы (1600-1699)
        public const int ITEM_ID_CRATE = ITEMS_BASE + 601;
        public const int ITEM_ID_CRATE2 = ITEMS_BASE + 602;
        public const int ITEM_ID_CRATE3 = ITEMS_BASE + 603;
        public const int ITEM_ID_CRATE4 = ITEMS_BASE + 604;

        // ========== МОНСТРЫ (2000-2999) ==========
        public const int MONSTER_ID_RAT = MONSTERS_BASE + 1;
        public const int MONSTER_ID_SPIDER = MONSTERS_BASE + 2;
        public const int MONSTER_ID_OLDER_RAT = MONSTERS_BASE + 3;
        public const int MONSTER_ID_OLDER_SPIDER = MONSTERS_BASE + 4;

        // ========== ЛОКАЦИИ (3000-3999) ==========
        public const int LOCATION_ID_VILLAGE = LOCATIONS_BASE + 1;
        public const int LOCATION_ID_FIELD_OF_NORTH = LOCATIONS_BASE + 2;
        public const int LOCATION_ID_FIELD_OF_SOUTH = LOCATIONS_BASE + 3;
        public const int LOCATION_ID_FIELD_OF_EAST = LOCATIONS_BASE + 4;
        public const int LOCATION_ID_FIELD_OF_WEST = LOCATIONS_BASE + 5;

        // ========== NPC (4000-4999) ==========
        public const int NPC_ID_VILLAGE_TRADER = NPCS_BASE + 1;
        public const int NPC_ID_VILLAGE_ELDER = NPCS_BASE + 2;
        public const int NPC_ID_VILLAGE_CRAFTSMAN = NPCS_BASE + 3;
        public const int NPC_ID_VILLAGE_HUNTER = NPCS_BASE + 4;

        // ========== КВЕСТЫ (5000-5999) ==========
        public const int QUEST_ID_RAT_HUNT = QUESTS_BASE + 1;
        public const int QUEST_ID_SPIDER_SILK = QUESTS_BASE + 2;
        public const int QUEST_ID_LOST_CRATES = QUESTS_BASE + 3;

        // ========== ТИТУЛЫ (6000-6999) ==========
        public const int TITLE_ID_RAT_SLAYER = TITLES_BASE + 1;
        public const int TITLE_ID_SPIDER_HUNTER = TITLES_BASE + 2;
        public const int TITLE_ID_EXPERIENCED_ADVENTURER = TITLES_BASE + 3;
    }
}