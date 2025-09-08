using Engine.Core;
using Engine.Quests;
using Engine.World;
using Engine.Combat;
using Engine.InventorySystem;
using Engine.Titles;

namespace Engine.Entities
{
    public class Player : LivingCreature
    {
        public string Name { get; set; }
        public int Gold { get; set; }
        public int CurrentEXP { get; set; }
        public int MaximumEXP { get; set; }
        public int Level { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefence { get; set; }
        public int BaseAgility { get; set; }
        public int BaseMaximumHP { get; set; }
        public int TotalMaximumHP => BaseMaximumHP + Inventory.CalculateTotalHealth();
        public int Attack => BaseAttack + Inventory.CalculateTotalAttack() + Attributes.Strength / 2;
        public int Defence => BaseDefence + Inventory.CalculateTotalDefence() + Attributes.Constitution / 2;
        public int Agility => BaseAgility + Inventory.CalculateTotalAgility() + Attributes.Dexterity;

        // Temporary defence buff: value and remaining turns.
        // Вставь прямо в класс Player рядом с другими полями.
        public int TemporaryDefenceBuff { get; set; } = 0;

        /// <summary>
        /// Сколько ходов ещё действует временный бафф защиты (0 = не действует)
        /// </summary>
        public int TemporaryDefenceBuffTurnsRemaining { get; set; } = 0;

        /// <summary>
        /// Применяет временную прибавку к защите на указанное количество ходов.
        /// Если уже есть бафф — суммирует значение и продлевает время (можно изменить логику).
        /// </summary>

        public int CurrentSpeed { get; set; }
        public Location CurrentLocation { get; set; }
        public Inventory Inventory { get; private set; }
        public Monster CurrentMonster { get; set; }
        public bool IsInCombat { get; set; }
        public int MonstersKilled { get; set; }
        public int QuestsCompleted { get; set; }
        public QuestLog QuestLog { get; set; }

        // Добавляем новые свойства
        public List<Title> UnlockedTitles { get; set; }
        public Title ActiveTitle { get; set; }
        public Dictionary<string, int> MonstersKilledByType { get; set; }

        private readonly IWorldRepository _worldRepository;
        private static bool _needsRedraw = true;

        public Player(string name, int gold, int currentHP, int maximumHP, int currentEXP, int maximumEXP, int level,
            int baseAttack, int baseDefence, int agility, IWorldRepository worldRepository, Attributes attributes = null) :
            base(currentHP, maximumHP)
        {
            Name = name;
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
            QuestLog = new QuestLog(this);
            UnlockedTitles = new List<Title>();
            MonstersKilledByType = new Dictionary<string, int>();
            ActiveTitle = null;

            EvasionChance = 5 + Attributes.Dexterity / 2;

            Inventory.OnEquipmentChanged += OnEquipmentChanged;

            _worldRepository = worldRepository;
        }

        public void ApplyTemporaryDefenceBuff(int amount, int turns)
        {
            if (amount <= 0 || turns <= 0) return;
            TemporaryDefenceBuff += amount;
            // Решение: берем максимум оставшихся ходов и новых, чтобы не уменьшать случайно
            TemporaryDefenceBuffTurnsRemaining = Math.Max(TemporaryDefenceBuffTurnsRemaining, turns);
        }

        /// <summary>
        /// Вычисляет итоговую защиту с учётом экипировки и временных эффектов.
        /// Заменяй в проекте вызов CalculateTotalDefence() или GetTotalDefence() на эту функцию,
        /// либо используй её внутри существующего метода расчёта защиты.
        /// </summary>
        public int GetEffectiveDefence()
        {
            // Пример: если у тебя есть метод CalculateTotalDefence() — можно вызывать его.
            int baseDef = 0;
            // Ниже — пример суммирования экипировки. Если у тебя уже есть CalculateTotalDefence, 
            // возвращай CalculateTotalDefence() + TemporaryDefenceBuff;
            // Я даю безопасный вариант, чтобы не сломать существующие вызовы:
            try
            {
                // если есть метод CalculateTotalDefence
                var mi = GetType().GetMethod("CalculateTotalDefence", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.NonPublic);
                if (mi != null)
                {
                    var baseVal = mi.Invoke(this, null);
                    if (baseVal is int bi) baseDef = bi;
                }
            }
            catch
            {
                // если нет CalculateTotalDefence, оставить baseDef = 0
            }

            return baseDef + TemporaryDefenceBuff;
        }

        public void MoveTo(Location newLocation)
        {
            if (newLocation != null)
            {
                newLocation.SpawnMonsters(Level);

                foreach (var quest in QuestLog.ActiveQuests.OfType<CollectibleQuest>())
                {
                    if (!quest.IsItemsSpawned)
                    {
                        quest.SpawnCollectibles();
                    }
                }

                MessageSystem.ClearMessages();
                MessageSystem.AddMessage($"Вы переместились в {newLocation.Name}");
            }

            CurrentLocation = newLocation;
            // НЕ устанавливаем флаг перерисовки здесь - это сделает HandleInput
        }

        // При завершении квеста убираем предметы
        public void CompleteQuest(Quest quest)
        {
            if (quest is CollectibleQuest collectibleQuest)
            {
                collectibleQuest.DespawnCollectibles();
            }
            // ... остальная логика завершения квеста
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
        private List<object> PrepareInventoryItems()
        {
            var allItems = new List<object>();

            // Добавляем экипированные предметы как EquipmentSlotItem
            if (Inventory.Helmet != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Helmet));
            if (Inventory.Armor != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Armor));
            if (Inventory.Gloves != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Gloves));
            if (Inventory.Boots != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Boots));
            if (Inventory.MainHand != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.MainHand));
            if (Inventory.OffHand != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.OffHand));
            if (Inventory.Amulet != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Amulet));
            if (Inventory.Ring1 != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Ring1));
            if (Inventory.Ring2 != null)
                allItems.Add(new InventoryUI.EquipmentSlotItem(Inventory.Ring2));

            // Добавляем предметы из инвентаря
            allItems.AddRange(Inventory.Items.Cast<object>());

            return allItems;
        }

        private void HandleSelectedItem(object selectedItem)
        {
            if (selectedItem is InventoryItem inventoryItem)
            {
                InventoryUI.ShowItemContextMenu(this, inventoryItem);
            }
            else if (selectedItem is InventoryUI.EquipmentSlotItem equipmentItem)
            {
                ShowEquipmentContextMenu(equipmentItem.Equipment);
            }
        }        // Новый метод для контекстного меню экипированных предметов
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

            // Пушим экран боя вместо синхронного CombatLoop
            var combatScreen = new CombatScreen(this, monster);
            ScreenManager.PushScreen(combatScreen);

            // Обновить рендеринг (если у вас есть такие helper'ы)
            ScreenManager.RequestFullRedraw();
        }

        public bool EquipItem(InventoryItem item)
        {
            if (Inventory.EquipItem(item))
            {
                MessageSystem.AddMessage($"Надето: {item.Details.Name}.");
                return true;
            }
            else
            {
                MessageSystem.AddMessage("Это не предмет экипировки или слот занят!");
                return false;
            }
        }

        public void RemoveItemFromInventory(InventoryItem item, int quantity = 1)
        {
            Inventory.RemoveItem(item, quantity);
        }

        public bool UnequipItem(Equipment equipment)
        {
            if (Inventory.UnequipItem(equipment))
            {
                MessageSystem.AddMessage($"Снято: {equipment.Name}.");
                return true;
            }
            else
            {
                MessageSystem.AddMessage("Не удалось снять предмет.");
                return false;
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
                healedAmount -= CurrentHP - MaximumHP;

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
        public bool CheckSkill(int difficulty, string attribute, int bonus = 0)
        {
            int attributeValue = attribute.ToLower() switch
            {
                "strength" => Attributes.Strength,
                "constitution" => Attributes.Constitution,
                "dexterity" => Attributes.Dexterity,
                "intelligence" => Attributes.Intelligence,
                "wisdom" => Attributes.Wisdom,
                "charisma" => Attributes.Charisma,
                _ => 10
            };

            Random random = new Random();
            int roll = random.Next(1, 21) + attributeValue + bonus;
            return roll >= difficulty;
        }

        // Метод для получения количества убитых монстров по типу
        public int GetMonstersKilled(string monsterType)
        {
            if (MonstersKilledByType.ContainsKey(monsterType))
                return MonstersKilledByType[monsterType];
            return 0;
        }

        // Метод для увеличения счетчика убийств монстров
        public void AddMonsterKill(string monsterType)
        {
            if (MonstersKilledByType.ContainsKey(monsterType))
                MonstersKilledByType[monsterType]++;
            else
                MonstersKilledByType[monsterType] = 1;

            CheckTitleUnlocks();
        }

        // Проверка разблокировки титулов
        public void CheckTitleUnlocks()
        {
            foreach (var title in GameServices.WorldRepository.GetAllTitles())
            {
                if (!title.IsUnlocked && title.CheckRequirements(this))
                {
                    title.IsUnlocked = true;
                    UnlockedTitles.Add(title);
                    MessageSystem.AddMessage($"Разблокирован новый титул: {title.Name}!");
                }
            }
        }

        // Активация титула
        public void ActivateTitle(Title title)
        {
            if (UnlockedTitles.Contains(title))
            {
                ActiveTitle = title;
                title.IsActive = true;
                MessageSystem.AddMessage($"Активирован титул: {title.Name}");
            }
        }

        // Деактивация титула
        public void DeactivateTitle()
        {
            if (ActiveTitle != null)
            {
                ActiveTitle.IsActive = false;
                MessageSystem.AddMessage($"Титул {ActiveTitle.Name} деактивирован");
                ActiveTitle = null;
            }
        }

        // Получение бонуса против конкретного типа монстра
        public int GetBonusAgainstMonster(Monster monster)
        {
            if (ActiveTitle != null &&
                !string.IsNullOrEmpty(ActiveTitle.BonusAgainstType) &&
                monster.Name.Contains(ActiveTitle.BonusAgainstType))
            {
                return ActiveTitle.BonusAgainstAmount;
            }
            return 0;
        }

        // Модифицируем метод получения урона для учета бонусов титула
        public int GetTotalAttack(Monster targetMonster = null)
        {
            int bonus = 0;

            if (ActiveTitle != null)
            {
                bonus += ActiveTitle.AttackBonus;

                // Бонус против конкретного типа монстра
                if (targetMonster != null && !string.IsNullOrEmpty(ActiveTitle.BonusAgainstType) &&
                    targetMonster.Name.Contains(ActiveTitle.BonusAgainstType))
                {
                    bonus += (int)(Attack * ActiveTitle.BonusAgainstAmount / 100f);
                }
            }

            return Attack + bonus;
        }
    }
}
