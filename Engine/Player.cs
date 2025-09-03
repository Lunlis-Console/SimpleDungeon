using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class Player : LivingCreature
    {
        public int Gold { get; set; }
        public int CurrentEXP { get; set; }
        public int MaximumEXP { get; set; }
        public int Level { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefence { get; set; }
        public int BaseAgility { get; set; }
        public int BaseMaximumHP { get; set; }
        public int TotalMaximumHP => BaseMaximumHP + Inventory.CalculateTotalHealth();
        public int Attack => BaseAttack + Inventory.CalculateTotalAttack();
        public int Defence => BaseDefence + Inventory.CalculateTotalDefence();
        public int Agility => BaseAgility + Inventory.CalculateTotalAgility();
        
        public int CurrentSpeed { get; set; }
        public Location CurrentLocation { get; set; }
        public Inventory Inventory { get; private set; }
        public Monster CurrentMonster { get; set; }
        public bool IsInCombat { get; set; }
        public int MonstersKilled { get; set; }
        public int QuestsCompleted { get; set; }
        public QuestLog QuestLog { get; set; }

        public Player(int gold, int currentHP, int maximumHP, int currentEXP, int maximumEXP, int level,
            int baseAttack, int baseDefence, int agility) :
            base(currentHP, maximumHP)
        {
            Gold = gold;
            CurrentEXP = currentEXP;
            MaximumEXP = maximumEXP;
            Level = level;
            BaseMaximumHP = maximumHP;
            BaseAttack = baseAttack;
            BaseDefence = baseDefence;
            BaseAgility = agility;
            CurrentSpeed = 0;
            Inventory = new Inventory();
            QuestLog = new QuestLog();

            Inventory.OnEquipmentChanged += OnEquipmentChanged;
        }

        public void MoveTo(Location newLocation)
        {
            if(newLocation != null)
            {
                newLocation.SpawnMonsters(Level);
                MessageSystem.ClearMessages();
            }

            CurrentLocation = newLocation;
        }
        public void MoveNorth()
        {
            if (CurrentLocation.LocationToNorth != null)
            {
                MoveTo(CurrentLocation.LocationToNorth);
            }
        }
        public void MoveEast()
        {
            if (CurrentLocation.LocationToEast != null)
            {
                MoveTo(CurrentLocation.LocationToEast);
            }
        }
        public void MoveWest()
        {
            if (CurrentLocation.LocationToWest != null)
            {
                MoveTo(CurrentLocation.LocationToWest);
            }
        }
        public void MoveSouth()
        {
            if (CurrentLocation.LocationToSouth != null)
            {
                MoveTo(CurrentLocation.LocationToSouth);
            }
        }
        public void DisplayInventory()
        {
            while (true)
            {
                Console.Clear();
                MessageSystem.DisplayMessages();

                // Создаем объединенный список предметов: инвентарь + экипировка
                var allItems = new List<object>();

                // Добавляем экипированные предметы
                if (Inventory.Helmet != null) allItems.Add(new EquipmentSlotItem("Шлем", Inventory.Helmet));
                if (Inventory.Armor != null) allItems.Add(new EquipmentSlotItem("Броня", Inventory.Armor));
                if (Inventory.Gloves != null) allItems.Add(new EquipmentSlotItem("Перчатки", Inventory.Gloves));
                if (Inventory.Boots != null) allItems.Add(new EquipmentSlotItem("Ботинки", Inventory.Boots));
                if (Inventory.MainHand != null) allItems.Add(new EquipmentSlotItem("Основная рука", Inventory.MainHand));
                if (Inventory.OffHand != null) allItems.Add(new EquipmentSlotItem("Вторая рука", Inventory.OffHand));
                if (Inventory.Amulet != null) allItems.Add(new EquipmentSlotItem("Амулет", Inventory.Amulet));
                if (Inventory.Ring1 != null) allItems.Add(new EquipmentSlotItem("Кольцо 1", Inventory.Ring1));
                if (Inventory.Ring2 != null) allItems.Add(new EquipmentSlotItem("Кольцо 2", Inventory.Ring2));

                allItems.AddRange(Inventory.Items.Cast<object>());


                if (allItems.Count == 0)
                {
                    Console.WriteLine("Пусто");
                    Console.WriteLine("\nНажмите любую клавишу чтобы вернуться...");
                    Console.ReadKey();
                    break;
                }
                else
                {
                    // Используем новый метод для выбора из объединенного списка
                    var selectedItem = InventoryUI.SelectItemFromCombinedList(
                        allItems,
                        "",
                        Inventory.MainHand, 
                        Inventory.OffHand,
                        Inventory.Helmet,    // Было: Inventory.Amulet
                        Inventory.Armor,     // Было: Inventory.Ring1
                        Inventory.Gloves,    // Было: Inventory.Ring2
                        Inventory.Boots,     // Было: Inventory.Helmet
                        Inventory.Weapon,    // Было: Inventory.Armor
                        Inventory.Amulet,    // Было: Inventory.Gloves
                        Inventory.Ring1,     // Было: Inventory.Boots
                        Inventory.Ring2,     // Было: Inventory.Weapon
                        Gold, Defence, Attack, Agility, Level, CurrentEXP, MaximumEXP, CurrentHP, TotalMaximumHP);

                    if (selectedItem == null)
                    {
                        break;
                    }

                    // Обрабатываем выбранный предмет
                    if (selectedItem is InventoryItem inventoryItem)
                    {
                        InventoryUI.ShowItemContextMenu(this, inventoryItem);
                    }
                    else if (selectedItem is EquipmentSlotItem equipmentItem)
                    {
                        ShowEquipmentContextMenu(equipmentItem.Equipment);
                    }
                }
            }
            MessageSystem.ClearMessages();
        }

        // Новый метод для контекстного меню экипированных предметов
        private void ShowEquipmentContextMenu(Equipment equipment)
        {
            var actions = new List<string> { "Снять", "Осмотреть", "Назад" };
            string selectedAction = InventoryUI.SelectActionFromList(actions, $"Экипировка: {equipment.Name}");

            switch (selectedAction)
            {
                case "Снять":
                    UnequipItem(equipment); // Теперь используется значение по умолчанию addToInventory = true
                    break;
                case "Осмотреть":
                    equipment.Read();
                    break;
            }
        }

        // Вспомогательный класс для представления экипированных предметов
        public class EquipmentSlotItem
        {
            public string SlotName { get; }
            public Equipment Equipment { get; }

            public EquipmentSlotItem(string slotName, Equipment equipment)
            {
                SlotName = slotName;
                Equipment = equipment;
            }

            public override string ToString()
            {
                return $"{SlotName}: {Equipment.Name}";
            }
        }

        public void StartCombat(Monster monster)
        {
            CurrentMonster = monster;
            IsInCombat = true;
            Console.WriteLine($"Внимание! Сражение с {monster.Name} [{monster.Level}]!");
            Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
            Console.ReadKey();

            CombatEngine combatEngine = new CombatEngine(this, monster);
            combatEngine.CombatLoop();

            if (!IsInCombat)
            {
                CurrentMonster = null;
            }
        }

        public void EquipItem(InventoryItem item)
        {
            if (Inventory.EquipItem(item))
            {
                MessageSystem.AddMessage($"Надето: {item.Details.Name}.");
            }
            else
            {
                MessageSystem.AddMessage("Это не предмет экипировки или слот занят!");
            }
        }

        public void RemoveItemFromInventory(InventoryItem item, int quantity = 1)
        {
            Inventory.RemoveItem(item, quantity);
        }

        public void UnequipItem(Equipment equipment)
        {
            if (Inventory.UnequipItem(equipment))
            {
                MessageSystem.AddMessage($"Снято: {equipment.Name}.");
            }
            else
            {
                MessageSystem.AddMessage("Не удалось снять предмет.");
            }
        }

        public void AddItemToInventory(Item item, int quantity = 1)
        {
            Inventory.AddItem(item, quantity);
        }
                
        public void UseItemToHeal(InventoryItem item)
        {
            // Пока проработаны только расходники лечения
            if (item.Details.Type != ItemType.Consumable)
            {
                MessageSystem.AddMessage("Это нельзя использовать!");
                return;
            }

            HealingItem toHeal = item.Details as HealingItem;
            if (toHeal == null)
            {
                MessageSystem.AddMessage("Этот предмет нельзя использовать для лечения!");
                return;
            }

            if (CurrentHP >= MaximumHP)
            {
                MessageSystem.AddMessage("Лечение не требуется!");
                return;
            }

            int healedAmount = toHeal.AmountToHeal;
            CurrentHP += healedAmount;


            if (CurrentHP > MaximumHP)
            {
                healedAmount -= (CurrentHP - MaximumHP);

                CurrentHP = MaximumHP;
            }

            RemoveItemFromInventory(item);
            MessageSystem.AddMessage($"Использовано: {item.Details.Name}, восстановлено {healedAmount} ед. здоровья!");
        }
        public void RecieveReward(Monster monster)
        {
            Gold += monster.RewardGold;
            CurrentEXP += monster.RewardEXP;
            MonstersKilled++;

            Console.WriteLine($"Вы получаете {monster.RewardGold} золота и {monster.RewardEXP} опыта!");

            List<Item> loot = monster.GetLoot();

            if(loot.Count > 0)
            {
                Console.WriteLine("Добыча: ");

                foreach( Item item in loot )
                {
                    AddItemToInventory( item );
                    Console.WriteLine($"- {item.Name}");
                }
            }
            else
            {
                Console.WriteLine("Добыча: ");
            }

            CheckLevelUp();
        }
        public void CheckLevelUp()
        {
            if(CurrentEXP >= MaximumEXP)
            {
                Level++;
                CurrentEXP -= MaximumEXP;
                MaximumEXP = (int)(MaximumEXP * 1.5);

                BaseMaximumHP += 10;
                CurrentHP = TotalMaximumHP;
                BaseAttack += 2;
                BaseDefence += 2;

                Console.WriteLine($"Поздравляем! Вы достигли {Level} уровня!");
                Console.WriteLine("Ваши параметры увеличились!");
            }
        }

        // Добавляем метод для корректировки CurrentHP при изменении MaximumHP
        public void AdjustHPAfterEquipmentChange()
        {
            if (CurrentHP > TotalMaximumHP)
            {
                CurrentHP = TotalMaximumHP;
            }
        }

        private void OnEquipmentChanged()
        {
            // Корректируем HP при изменении экипировки
            AdjustHPAfterEquipmentChange();
        }

        public void LookAround()
        {
            Console.WriteLine("Вы осматриваетесь вокруг...");
            CurrentLocation.CleanDeadMonster();
        }
        public void TalkTo(string npcName)
        {
            NPC npcToTalk = CurrentLocation.NPCsHere.FirstOrDefault(n =>
                n.Name.ToLower().Contains(npcName.ToLower()));

            if(npcToTalk != null)
            {
                npcToTalk.Talk(this);
            }
            else
            {
                MessageSystem.AddMessage("СИСТЕМА: Здесь нет такого человека.");
            }
        }


    }
}
