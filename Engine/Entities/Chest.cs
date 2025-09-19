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
        public LockDifficulty LockDifficulty { get; set; } = LockDifficulty.Simple;

        public Chest(int id, string name, string namePlural, int price, string description = "", 
                    bool isLocked = false, bool isTrapped = false, bool requiresKey = false, 
                    int requiredKeyID = 0, List<int> requiredItemIDs = null, int maxCapacity = 20,
                    LockDifficulty lockDifficulty = LockDifficulty.Simple) 
            : base(id, name, namePlural, ItemType.Container, price, description)
        {
            Inventory = new Inventory();
            IsLocked = isLocked;
            IsTrapped = isTrapped;
            RequiresKey = requiresKey;
            RequiredKeyID = requiredKeyID;
            RequiredItemIDs = requiredItemIDs ?? new List<int>();
            MaxCapacity = maxCapacity;
            LockDifficulty = lockDifficulty;
        }

        // Конструктор для создания пустого сундука
        public Chest() : base(0, "Сундук", "Сундуки", ItemType.Container, 0, "")
        {
            Inventory = new Inventory();
            RequiredItemIDs = new List<int>();
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string>();
            
            if (IsLocked)
            {
                // Для запертых сундуков проверяем способы открытия
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
                    // Если нет ключа/предмета, предлагаем взлом
                    actions.Add("Взломать");
                }
            }
            else
            {
                // Для незапертых сундуков только открытие
                actions.Add("Открыть");
            }
            
            // Осмотр всегда доступен
            actions.Add("Осмотреть");
            
            // Обезвреживание ловушки доступно только для запертых сундуков с ловушкой
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

        public void OpenChest(Player player)
        {
            if (IsLocked)
            {
                MessageSystem.AddMessage($"{LockDescription}");
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
                MessageSystem.AddMessage("У вас нет нужного ключа.");
                return;
            }

            // Используем ключ
            player.Inventory.RemoveItem(player.Inventory.Items.First(i => i.Details.ID == RequiredKeyID), 1);
            IsLocked = false;
            MessageSystem.AddMessage($"Вы использовали ключ и открыли {Name}.");
            
            OpenChest(player);
        }

        private void OpenWithItem(Player player)
        {
            var requiredItem = RequiredItemIDs.FirstOrDefault(id => player.Inventory.HasItem(id));
            if (requiredItem == 0)
            {
                MessageSystem.AddMessage("У вас нет нужного предмета.");
                return;
            }

            // Используем предмет
            player.Inventory.RemoveItem(player.Inventory.Items.First(i => i.Details.ID == requiredItem), 1);
            IsLocked = false;
            MessageSystem.AddMessage($"Вы использовали предмет и открыли {Name}.");
            
            OpenChest(player);
        }

        private void Examine(Player player)
        {
            DebugConsole.Log($"============ ОСМОТР: {Name} ============");
            DebugConsole.Log($"Описание: {Description}");
            
            if (IsLocked)
            {
                DebugConsole.Log("Сундук надежно заперт.");
                DebugConsole.Log($"Сложность замка: {LockDifficultyHelper.GetDifficultyDescription(LockDifficulty)}");
                
                var requiredLevelDescription = GetRequiredLevelDescription(LockDifficulty);
                DebugConsole.Log($"Требуется уровень навыка взлома: {requiredLevelDescription}");
                
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
            DebugConsole.Log($"[PickLock] Starting lockpicking attempt for chest: {Name}");
            
            // Проверяем уровень навыка взлома игрока
            var lockpickingSkill = player.Skills.Lockpicking;
            var requiredMinLevel = GetRequiredMinLevel(LockDifficulty);
            
            DebugConsole.Log($"[PickLock] Player lockpicking skill: {lockpickingSkill.Level}, Required: {requiredMinLevel}");
            
            if (lockpickingSkill.Level < requiredMinLevel)
            {
                string levelName = player.Skills.GetSkillLevelName("lockpicking");
                string requiredLevelName = GetRequiredLevelDescription(LockDifficulty);
                MessageSystem.AddMessage($"Ваш уровень навыка взлома ({levelName}) недостаточен для этого замка!");
                MessageSystem.AddMessage($"Требуется уровень: {requiredLevelName}");
                return;
            }

            // Проверяем, есть ли у игрока отмычка
            DebugConsole.Log($"[PickLock] Checking player inventory for lockpick items...");
            DebugConsole.Log($"[PickLock] Player inventory items count: {player.Inventory.Items.Count}");
            
            foreach (var item in player.Inventory.Items)
            {
                DebugConsole.Log($"[PickLock] Inventory item: {item.Details.Name} (ID: {item.Details.ID}, Type: {item.Details.Type})");
            }
            
            var lockpickItem = player.Inventory.Items
                .FirstOrDefault(item => item.Details.Type == ItemType.Lockpick);

            DebugConsole.Log($"[PickLock] Lockpick item found: {lockpickItem != null}");
            if (lockpickItem != null)
            {
                DebugConsole.Log($"[PickLock] Lockpick item details: {lockpickItem.Details.Name} (ID: {lockpickItem.Details.ID})");
                DebugConsole.Log($"[PickLock] Lockpick item components count: {lockpickItem.Details.Components.Count}");
                
                foreach (var component in lockpickItem.Details.Components)
                {
                    DebugConsole.Log($"[PickLock] Component: {component.GetType().Name} - {component.ComponentType}");
                }
            }

            if (lockpickItem == null)
            {
                MessageSystem.AddMessage("У вас нет отмычки для взлома замков!");
                return;
            }

            // Получаем компонент отмычки
            LockpickComponent lockpickComponent = null;
            
            // Если это CompositeItem, проверяем его компоненты
            if (lockpickItem.Details is CompositeItem compositeItem)
            {
                DebugConsole.Log($"[PickLock] Item is CompositeItem, checking components...");
                lockpickComponent = compositeItem.Components
                    .OfType<LockpickComponent>()
                    .FirstOrDefault();
            }
            else
            {
                DebugConsole.Log($"[PickLock] Item is regular Item, checking base components...");
                // Для обычных Item проверяем базовые компоненты
                lockpickComponent = lockpickItem.Details.Components
                    .OfType<LockpickComponent>()
                    .FirstOrDefault();
            }

            DebugConsole.Log($"[PickLock] LockpickComponent found: {lockpickComponent != null}");
            if (lockpickComponent != null)
            {
                DebugConsole.Log($"[PickLock] LockpickComponent details: Bonus={lockpickComponent.LockpickBonus}, Durability={lockpickComponent.Durability}/{lockpickComponent.MaxDurability}, IsBroken={lockpickComponent.IsBroken}");
            }

            if (lockpickComponent == null)
            {
                MessageSystem.AddMessage("Отмычка повреждена и не может быть использована!");
                // Удаляем поврежденную отмычку из инвентаря
                player.Inventory.RemoveItem(lockpickItem, 1);
                return;
            }

            // Проверяем, не сломана ли отмычка
            if (lockpickComponent.IsBroken)
            {
                MessageSystem.AddMessage("Отмычка сломана и не может быть использована!");
                // Удаляем сломанную отмычку из инвентаря
                player.Inventory.RemoveItem(lockpickItem, 1);
                return;
            }

            DebugConsole.Log($"[PickLock] All checks passed, starting minigame...");
            
            // Запускаем мини-игру взлома замка
            var minigameScreen = new LockpickingMinigameScreen(this, player, lockpickComponent, lockpickItem);
            ScreenManager.PushScreen(minigameScreen);
        }

        private int CalculateExperienceGain(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => 1,
                LockDifficulty.Average => 2,
                LockDifficulty.Complex => 3,
                LockDifficulty.Master => 4,
                LockDifficulty.Legendary => 5,
                _ => 1
            };
        }

        private int GetRequiredMinLevel(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => 1,      // Новичок
                LockDifficulty.Average => 25,    // Ученик
                LockDifficulty.Complex => 50,    // Адепт
                LockDifficulty.Master => 75,     // Эксперт
                LockDifficulty.Legendary => 100, // Мастер
                _ => 1
            };
        }

        private string GetRequiredLevelDescription(LockDifficulty difficulty)
        {
            return difficulty switch
            {
                LockDifficulty.Simple => "Новичок (1+ уровень)",
                LockDifficulty.Average => "Ученик (25+ уровень)",
                LockDifficulty.Complex => "Адепт (50+ уровень)",
                LockDifficulty.Master => "Эксперт (75+ уровень)",
                LockDifficulty.Legendary => "Мастер (100+ уровень)",
                _ => "Новичок (1+ уровень)"
            };
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