namespace Engine
{
    public class HealingItem : Item
    {
        public int AmountToHeal { get; set; }

        public HealingItem(int id, string name, string namePlural, ItemType type, int amountToHeal, int price, string desctiption = "") :
            base (id, name, namePlural, type, price, desctiption)
        {
            AmountToHeal = amountToHeal;
        }
    }
}
