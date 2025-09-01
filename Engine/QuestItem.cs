namespace Engine
{
    public class QuestItem
    {
        public Item Details { get; set; }
        public int Quantity { get; set; }

        public QuestItem(Item details, int quantity)
        {
            Details = details;
            Quantity = quantity;
        }
    }
}