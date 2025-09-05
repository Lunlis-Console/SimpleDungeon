namespace Engine
{
    public class LootItem
    {
        public Item Details { get; set; }
        public int DropPercentage {get; set; }
        public bool IsUnique { get; set; }

        public LootItem(Item details, int dropPercentage, bool isUnique)
        {
            Details = details;
            DropPercentage = dropPercentage;
            IsUnique = isUnique;
        }
    }
}
