using Engine.Entities;

namespace Engine.Quests
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