using Engine.Core;

namespace Engine.Entities
{
    public class Item
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string NamePlural { get; set; }
        public ItemType Type { get; set; }
        public int Price { get; set; }
        public string Description { get; set; }

        public Item(int id, string name, string namePlural, ItemType type, int price, string description = "")
        {
            ID = id;
            Name = name;
            NamePlural = namePlural;
            Type = type;
            Price = price;
            Description = description;
        }

        public void Read()
        {
            if(File.Exists(Description))
            {
                string text = File.ReadAllText(Description);

                Console.Clear();
                Console.WriteLine(text);
                Console.WriteLine("\nНажмите любую клавишу, чтобы закрить описание...");
                Console.ReadKey();
                Console.Clear();
            }
            else
            {
                Console.Clear();
                Console.WriteLine("Описание отсутствует.");
                Console.WriteLine("\nНажмите любую клавишу, чтобы закрить описание...");
                Console.ReadKey();
                Console.Clear();
            }
        }



    }
}
