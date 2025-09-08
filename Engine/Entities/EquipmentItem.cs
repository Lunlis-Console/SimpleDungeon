namespace Engine.Entities
{
    public class EquipmentItem
    {
        public Equipment Details { get; set; }
        public int Quantity { get; set; }

        public EquipmentItem(Equipment details, int quantity)
        {
            Details = details;
            Quantity = quantity;
        }
    }
}
