using Engine.InventorySystem;
using Engine.Core;
using Engine.UI;

namespace Engine.Entities
{
    public class Chest : Item, IInteractable
    {
        public Inventory Inventory { get; private set; }
        public bool IsLocked { get; set; }
        public bool IsTrapped { get; set; }
        public bool RequiresKey { get; set; }
        public int RequiredKeyID { get; set; }
        public List<int> RequiredItemIDs { get; set; }
        public string LockDescription { get; set; } = "Сундук заперт.";
        public int MaxCapacity { get; set; } = 20; // Максимальное количество предметов

        public Chest(int id, string name, string namePlural, int price, string description = "", 
                    bool isLocked = false, bool isTrapped = false, bool requiresKey = false, 
                    int requiredKeyID = 0, List<int> requiredItemIDs = null, int maxCapacity = 20) 
            : base(id, name, namePlural, ItemType.Container, price, description)
        {
            Inventory = new Inventory();
            IsLocked = isLocked;
            IsTrapped = isTrapped;
            RequiresKey = requiresKey;
            RequiredKeyID = requiredKeyID;
            RequiredItemIDs = requiredItemIDs ?? new List<int>();
            MaxCapacity = maxCapacity;
        }

        // Конструктор для создания пустого сундука
        public Chest() : base(0, "Сундук", "Сундуки", ItemType.Container, 0, "")
        {
            Inventory = new Inventory();
            RequiredItemIDs = new List<int>();
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Открыть", "Осмотреть" };
            
            if (IsLocked)
            {
                if (RequiresKey && player.Inventory.HasItem(RequiredKeyID))
                {
                    actions.Add("Открыть ключом");
                }
                else if (RequiredItemIDs.Any(id => player.Inventory.HasItem(id)))
                {
                    actions.Add("Открыть предметом");
                }
                else
                {
                    actions.Add("Взломать");
                }
            }
            
            if (IsTrapped && player.Attributes.Intelligence >= 12)
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
                    OpenChest(player);
                    break;
                case "Открыть ключом":
                    OpenWithKey(player);
                    break;
                case "Открыть предметом":
                    OpenWithItem(player);
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

        private void OpenChest(Player player)
        {
            if (IsLocked)
            {
                DebugConsole.Log($"{LockDescription}");
                return;
            }

            if (IsTrapped)
            {
                TriggerTrap(player);
                return;
            }

            // Открываем сундук - переходим к экрану взаимодействия
            GameServices.CurrentPlayer = player;
            var chestScreen = new ChestInteractionScreen(this, player);
            ScreenManager.PushScreen(chestScreen);
        }

        private void OpenWithKey(Player player)
        {
            if (!RequiresKey || !player.Inventory.HasItem(RequiredKeyID))
            {
                DebugConsole.Log("У вас нет нужного ключа.");
                return;
            }

            // Используем ключ
            player.Inventory.RemoveItem(player.Inventory.Items.First(i => i.Details.ID == RequiredKeyID), 1);
            IsLocked = false;
            DebugConsole.Log($"Вы использовали ключ и открыли {Name}.");
            
            OpenChest(player);
        }

        private void OpenWithItem(Player player)
        {
            var requiredItem = RequiredItemIDs.FirstOrDefault(id => player.Inventory.HasItem(id));
            if (requiredItem == 0)
            {
                DebugConsole.Log("У вас нет нужного предмета.");
                return;
            }

            // Используем предмет
            player.Inventory.RemoveItem(player.Inventory.Items.First(i => i.Details.ID == requiredItem), 1);
            IsLocked = false;
            DebugConsole.Log($"Вы использовали предмет и открыли {Name}.");
            
            OpenChest(player);
        }

        private void Examine(Player player)
        {
            DebugConsole.Log($"============ ОСМОТР: {Name} ============");
            DebugConsole.Log($"Описание: {Description}");
            
            if (IsLocked)
            {
                DebugConsole.Log("Сундук надежно заперт.");
                if (RequiresKey)
                {
                    DebugConsole.Log("Требуется специальный ключ.");
                }
                if (RequiredItemIDs.Any())
                {
                    DebugConsole.Log("Требуется специальный предмет.");
                }
            }

            if (IsTrapped)
            {
                if (player.Attributes.Intelligence >= 15)
                {
                    DebugConsole.Log("Вы замечаете тонкие щели - это ловушка!");
                }
                else
                {
                    DebugConsole.Log("Кажется, сундук в полном порядке.");
                }
            }

            DebugConsole.Log($"Вместимость: {Inventory.Items.Count}/{MaxCapacity}");
            if (Inventory.Items.Any())
            {
                DebugConsole.Log("Содержимое:");
                foreach (var item in Inventory.Items)
                {
                    DebugConsole.Log($"- {item.Details.Name} x{item.Quantity}");
                }
            }
        }

        private void PickLock(Player player)
        {
            // Простая реализация взлома
            int difficulty = IsTrapped ? 15 : 10;
            int playerSkill = player.Attributes.Dexterity + player.Attributes.Intelligence;
            
            if (playerSkill >= difficulty)
            {
                IsLocked = false;
                DebugConsole.Log($"Вы успешно взломали {Name}!");
                OpenChest(player);
            }
            else
            {
                DebugConsole.Log("Взлом не удался. Попробуйте еще раз или найдите ключ.");
            }
        }

        private void DisarmTrap(Player player)
        {
            int difficulty = 15;
            int playerSkill = player.Attributes.Intelligence + player.Attributes.Dexterity;
            
            if (playerSkill >= difficulty)
            {
                IsTrapped = false;
                DebugConsole.Log($"Вы успешно обезвредили ловушку на {Name}!");
            }
            else
            {
                DebugConsole.Log("Обезвреживание не удалось. Будьте осторожны!");
            }
        }

        private void TriggerTrap(Player player)
        {
            DebugConsole.Log("Ловушка сработала!");
            int damage = Random.Shared.Next(5, 15);
            player.CurrentHP = Math.Max(0, player.CurrentHP - damage);
            DebugConsole.Log($"Вы получили {damage} урона!");
            
            if (player.CurrentHP <= 0)
            {
                DebugConsole.Log("Вы погибли от ловушки!");
            }
        }

        // Методы для работы с инвентарем сундука
        public bool CanAddItem(Item item, int quantity = 1)
        {
            if (Inventory.Items.Count >= MaxCapacity)
            {
                return false;
            }
            return true;
        }

        public bool AddItem(Item item, int quantity = 1)
        {
            if (!CanAddItem(item, quantity))
            {
                return false;
            }
            
            Inventory.AddItem(item, quantity);
            return true;
        }

        public bool RemoveItem(Item item, int quantity = 1)
        {
            return Inventory.Items.Any(i => i.Details.ID == item.ID && i.Quantity >= quantity);
        }

        public void TransferToPlayer(Player player, InventoryItem item, int quantity)
        {
            if (item.Quantity >= quantity)
            {
                player.Inventory.AddItem(item.Details, quantity);
                Inventory.RemoveItem(item, quantity);
                DebugConsole.Log($"Вы взяли {item.Details.Name} x{quantity}");
            }
        }

        public void TransferFromPlayer(Player player, InventoryItem item, int quantity)
        {
            if (item.Quantity >= quantity && CanAddItem(item.Details, quantity))
            {
                Inventory.AddItem(item.Details, quantity);
                player.Inventory.RemoveItem(item, quantity);
                DebugConsole.Log($"Вы положили {item.Details.Name} x{quantity}");
            }
            else
            {
                DebugConsole.Log("Недостаточно места в сундуке или предметов в инвентаре.");
            }
        }
    }
}