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
        public int Attack { get; set; }
        public int Defence { get; set; }
        public Location CurrentLocation { get; set; }
        public Equipment EquipmentHelmet { get; set; }
        public Equipment EquipmentArmor { get; set; }
        public Equipment EquipmentGloves { get; set; }
        public Equipment EquipmentBoots { get; set; }
        public Equipment EquipmentWeapon { get; set; }
        public List<InventoryItem> Inventory { get; set; }
        public List<EquipmentItem> EquipmentItems { get; set; }
        public Monster CurrentMonster { get; set; }
        public bool IsInCombat { get; set; }
        public int MonstersKilled { get; set; }
        public int QuestsCompleted { get; set; }
        public QuestLog QuestLog { get; set; }

        public Player(int gold, int currentHP, int maximumHP, int currentEXP, int maximumEXP, int level,
            int attack, int defence) :
            base(currentHP, maximumHP)
        {
            Gold = gold;
            CurrentEXP = currentEXP;
            MaximumEXP = maximumEXP;
            Level = level;
            Attack = attack;
            Defence = defence;

            Inventory = new List<InventoryItem>();

            EquipmentItems = new List<EquipmentItem>();

            QuestLog = new QuestLog();

            EquipmentHelmet = null;
            EquipmentArmor = null;
            EquipmentGloves = null;
            EquipmentBoots = null;

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
                //allItems.AddRange(Inventory.Cast<object>());

                // Добавляем экипированные предметы как специальные объекты
                if (EquipmentHelmet != null) allItems.Add(new EquipmentSlotItem("Надето", EquipmentHelmet));
                if (EquipmentArmor != null) allItems.Add(new EquipmentSlotItem("Надето", EquipmentArmor));
                if (EquipmentGloves != null) allItems.Add(new EquipmentSlotItem("Надето", EquipmentGloves));
                if (EquipmentBoots != null) allItems.Add(new EquipmentSlotItem("Надето", EquipmentBoots));
                if (EquipmentWeapon != null) allItems.Add(new EquipmentSlotItem("Надето", EquipmentWeapon));

                allItems.AddRange(Inventory.Cast<object>());


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
                        "Выберите предмет (TAB - переключить на экипировку)",
                        EquipmentHelmet, EquipmentArmor, EquipmentGloves, EquipmentBoots, EquipmentWeapon,
                        Gold, Defence, Attack, Level, CurrentEXP, MaximumEXP, CurrentHP, MaximumHP);

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
                    UnequipItem(equipment);
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

            CombatLoop();
        }
        public void EquipItem(InventoryItem item)
        {
            // при добавлении тип предмета, нужно редактировать этот элемент!
            if(item.Details.Type == ItemType.Stuff || item.Details.Type == ItemType.Consumable)
            {
                MessageSystem.AddMessage("Это не предмет экипировки!");
                return;
            }

            // Проверяем, есть ли что-то надетое в слоте
            Equipment currentEquipment = GetEquipmentInSlot(item.Details.Type);

            if (currentEquipment != null)
            {
                MessageSystem.AddMessage($"Уже надет предмет {currentEquipment.Name}. Сначала снимите его!");
                return;
            }

            // Надеваем предмет
            switch (item.Details.Type)
            {
                case ItemType.Helmet:
                    EquipmentHelmet = (Equipment)item.Details;
                    break;
                case ItemType.Armor:
                    EquipmentArmor = (Equipment)item.Details;
                    break;
                case ItemType.Gloves:
                    EquipmentGloves = (Equipment)item.Details;
                    break;
                case ItemType.Boots:
                    EquipmentBoots = (Equipment)item.Details;
                    break;
                case ItemType.Sword:
                    EquipmentWeapon = (Equipment)item.Details;
                    break;
                default:
                    MessageSystem.AddMessage("Это нельзя надеть.");
                    return;
            }

            // Добавляем в список экипировки
            EquipmentItems.Add(new EquipmentItem((Equipment)item.Details, 1));

            // Удаляем из инвентаря
            RemoveItemFromInventory(item);

            MessageSystem.AddMessage($"Надето: {item.Details.Name}.");
            UpdateStats();
        }
        public void RemoveItemFromInventory(InventoryItem item)
        {
            item.Quantity--;
            if (item.Quantity <= 0)
            {
                Inventory.Remove(item);
            }
        }

        public void UnequipItem(Equipment equipment)
        {
            if (equipment == null) return;

            // Снимаем предмет
            switch (equipment.Type)
            {
                case ItemType.Helmet:
                    EquipmentHelmet = null;
                    break;
                case ItemType.Armor:
                    EquipmentArmor = null;
                    break;
                case ItemType.Gloves:
                    EquipmentGloves = null;
                    break;
                case ItemType.Boots:
                    EquipmentBoots = null;
                    break;
                case ItemType.Sword:
                    EquipmentWeapon = null;
                    break;
                default:
                    MessageSystem.AddMessage("СИСТЕМА: Неизвестный тип предмета.");
                    return;
            }

            // Удаляем из списка экипировки
            var equipmentItem = EquipmentItems.FirstOrDefault(ei => ei.Details.ID == equipment.ID);
            if (equipmentItem != null)
            {
                EquipmentItems.Remove(equipmentItem);
            }

            // Добавляем в инвентарь
            AddItemToInventory(equipment);

            MessageSystem.AddMessage($"Снято: {equipment.Name}.");
            UpdateStats();
        }

        public void AddItemToInventory(Item item, int quantity = 1)
        {
            // Проверяем, есть ли уже такой предмет в инвентаре
            InventoryItem existingItem = Inventory.FirstOrDefault(ii => ii.Details.ID == item.ID);

            if (existingItem != null)
            {
                existingItem.Quantity += quantity;
            }
            else
            {
                Inventory.Add(new InventoryItem(item, quantity));
            }
        }
        public void UpdateStats()
        {
            Defence =
                (EquipmentHelmet?.DefenceBonus ?? 0) +
                (EquipmentArmor?.DefenceBonus ?? 0) +
                (EquipmentGloves?.DefenceBonus ?? 0) +
                (EquipmentBoots?.DefenceBonus ?? 0);
            Attack = (EquipmentWeapon?.AttackBonus ?? 0);
        }
        public void HelpInventory()
        {
            Console.Clear();

            Console.WriteLine("=================Помощь=================");
            Console.WriteLine("- надеть <имя предмета> - надеть на себя экипировку.");
            Console.WriteLine("- снять <имя предмета> - снять с себя экипировку.");
            Console.WriteLine("- осмотреть <имя предмета> - откроет описание предмета.");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("");
            Console.WriteLine("========================================");

            Console.WriteLine("\nНажмите любую клавишу, чтобы закрыть описание...");
            Console.ReadKey();
            Console.Clear();
        }
        public Equipment GetEquipmentInSlot(ItemType type)
        {
            return type switch
            {
                ItemType.Helmet => EquipmentHelmet,
                ItemType.Armor => EquipmentArmor,
                ItemType.Gloves => EquipmentGloves,
                ItemType.Boots => EquipmentBoots,
                ItemType.Sword => EquipmentWeapon,
                _ => null
            };
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
        public void CombatLoop()
        {
            string playerActionMessage = "";
            string monsterActionMessage = "";

            while (IsInCombat && CurrentHP > 0 && CurrentMonster.CurrentHP > 0)
            {
                Console.Clear();

                Console.WriteLine($"======={CurrentMonster.Name}========");
                Console.WriteLine($"ОЗ: {CurrentMonster.CurrentHP}/{CurrentMonster.MaximumHP} " +
                    $"| АТК: {CurrentMonster.Attack} | ЗЩТ: {CurrentMonster.Defence}");
                Console.WriteLine($"====================================");

                if(!string.IsNullOrEmpty(playerActionMessage))
                {
                    Console.WriteLine(playerActionMessage);
                }
                else
                {
                    Console.WriteLine();
                }

                Console.WriteLine($"------------------------------------");

                if (!string.IsNullOrEmpty(monsterActionMessage))
                {
                    Console.WriteLine(monsterActionMessage);
                }
                else
                {
                    Console.WriteLine();
                }

                Console.WriteLine($"========Игрок========");
                Console.WriteLine($"ОЗ: {CurrentHP}/{MaximumHP} " +
                    $"| АТК: {Attack} | ЗЩТ: {Defence}");

                Console.WriteLine("=========Действия=========");
                Console.WriteLine("| 1 - атаковать | 2 - заклинание | 3 - защищаться | 4 - бежать |");

                playerActionMessage = "";
                monsterActionMessage = "";

                ConsoleKeyInfo key = Console.ReadKey(true);
                Console.WriteLine();

                switch (key.Key)
                {
                    case ConsoleKey.D1:
                    case ConsoleKey.NumPad1:
                        playerActionMessage = AttackToMonster();
                        break;
                    case ConsoleKey.D2:
                    case ConsoleKey.NumPad2:
                        //playerActionMessage = SpellToMonster();
                        break;
                    case ConsoleKey.D3:
                    case ConsoleKey.NumPad3:
                        //playerActionMessage = DefenceToMonster();
                        break;
                    case ConsoleKey.D4:
                    case ConsoleKey.NumPad4:
                        playerActionMessage = TryToEscape();
                        if(playerActionMessage.Contains("Вам удалось сбежать!"))
                        {
                            IsInCombat = false;
                            break;
                        }
                        break;
                }

                if(!IsInCombat)
                {
                    break;
                }

                if (IsInCombat && CurrentMonster.CurrentHP > 0)
                {
                    monsterActionMessage = MonsterAttack();
                }

                if (CurrentHP <= 0)
                {
                    Console.WriteLine("Вы погибли.");
                    IsInCombat = false;
                    Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
                    Console.ReadKey();
                    Console.Clear();
                    break;
                }
                else if(CurrentMonster.CurrentHP <= 0)
                {
                    Console.WriteLine($"{CurrentMonster.Name} побежден!");
                    RecieveReward(CurrentMonster);
                    IsInCombat = false;
                    Console.WriteLine("\nНажмите любую клавишу чтобы продолжить...");
                    Console.ReadKey();
                    Console.Clear();
                }
            }
        }
        public string AttackToMonster()
        {
            int damage = Attack + new Random().Next(1, 6) - CurrentMonster.Defence;

            if(damage < 0)
            {
                damage = 0;
            }

            CurrentMonster.CurrentHP -= damage;
            return $"Вы нанесли {damage} урона по {CurrentMonster.Name}!";
        }
        public void SpellToMonster()
        {

        }
        public void DefenceToMonster()
        {

        }
        public string TryToEscape()
        {
            int escapeChance = 30;
            if(new Random().Next(100) < escapeChance)
            {
                IsInCombat = false;
                Console.Clear();
                return "Вам удалось сбежать!";
            }
            else
            {
                return "Вам не удалось сбежать!";
            }
        }
        public string MonsterAttack()
        {
            int damage = CurrentMonster.Attack + new Random().Next(1, 4) - Defence;

            CurrentHP -= damage;
            return $"{CurrentMonster.Name} наносит вам {damage} урона!";
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
                //int additionalEXP = CurrentEXP - MaximumEXP;

                Level++;
                CurrentEXP -= MaximumEXP;
                MaximumEXP = (int)(MaximumEXP * 1.5);

                MaximumHP += 10;
                CurrentHP = MaximumHP;
                Attack += 2;
                Defence += 2;

                Console.WriteLine($"Поздравляем! Вы достигли {Level} уровня!");
                Console.WriteLine("Ваши параметры увеличились!");
            }
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
