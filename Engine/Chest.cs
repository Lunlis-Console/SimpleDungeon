namespace Engine
{
    public class Chest : IInteractable
    {
        public string Name { get; set; } = "Сундук";
        public List<InventoryItem> Loot { get; set; }
        public bool IsLocked { get; set; }
        public bool IsTrapped { get; set; }

        public Chest(bool isLocked = false, bool isTrapped = false)
        {
            Loot = new List<InventoryItem>();
            IsLocked = isLocked;
            IsTrapped = isTrapped;
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Открыть", "Осмотреть" };
            if (IsLocked)
            {
                actions.Add("Взломать");
            }
            if (IsTrapped && player.CheckSkill(12, "intelligence"))
            {
                actions.Add("Обезвредить ловушку");
            }
            return actions;
        }

        public void ExecuteAction(Player player, string action)
        {
            switch (action)
            {
                case "Открыть":
                    Open(player);
                    break;
                case "Осмотреть":
                    Examine(player);
                    break;
                case "Взломать":
                    PickLock(player);
                    break;
                case "Обезвредить ловушку":
                    DisarmTrap(player);
                    break;
            }
        }

        private void Open(Player player)
        {
            if (IsLocked)
            {
                Console.WriteLine("Сундук заперт!");
                Console.ReadKey();
                return;
            }

            if (Loot.Count > 0)
            {
                Console.WriteLine("Вы нашли в сундуке:");
                foreach (var item in Loot)
                {
                    Console.WriteLine($"- {item.Details.Name} x{item.Quantity}");
                    player.AddItemToInventory(item.Details, item.Quantity);
                }
                Loot.Clear();
            }
            else
            {
                Console.WriteLine("Сундук пуст.");
            }
            Console.WriteLine("\nНажмите любую клавишу...");
            Console.ReadKey();
        }

        private void Examine(Player player)
        {
            Console.Clear();
            Console.WriteLine($"============ ОСМОТР: {Name} ============");

            if (IsLocked)
            {
                Console.WriteLine("Сундук надежно заперт.");
            }

            if (IsTrapped)
            {
                if (player.CheckSkill(15, "perception"))
                {
                    Console.WriteLine("Вы замечаете тонкие щели - это ловушка!");
                }
                else
                {
                    Console.WriteLine("Кажется, сундук в полном порядке.");
                }
            }
            else
            {
                Console.WriteLine("Выглядит как обычный сундук.");
            }

            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();
        }

        private void PickLock(Player player)
        {
            // Реализация взлома
        }

        private void DisarmTrap(Player player)
        {
            // Реализация обезвреживания ловушки
        }
    }
}