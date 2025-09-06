using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine
{
    public class GameSave
    {
        public string SaveName { get; set; }
        public DateTime SaveDate { get; set; }

        // Данные игрока

        public string Name { get; set; }
        public int Gold { get; set; }
        public int CurrentHP { get; set; }
        public int MaximumHP { get; set; }
        public int CurrentEXP { get; set; }
        public int MaximumEXP { get; set; }
        public int Level { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefence { get; set; }
        public int BaseAgility { get; set; }
        public int LocationID { get; set; }

        // Инвентарь
        public List<InventoryItemData> Inventory { get; set; }
        public List<EquipmentItemData> EquippedItems { get; set; }

        // Прогресс
        public List<int> CompletedQuests { get; set; }
        public List<int> ActiveQuests { get; set; }
        public int MonstersKilled { get; set; }
        public int QuestsCompleted { get; set; }

        public GameSave()
        {
            Inventory = new List<InventoryItemData>();
            EquippedItems = new List<EquipmentItemData>();
            CompletedQuests = new List<int>();
            ActiveQuests = new List<int>();
        }
    }

    public class InventoryItemData
    {
        public int ItemID { get; set; }
        public int Quantity { get; set; }

        public InventoryItemData() { }

        public InventoryItemData(int itemID, int quantity)
        {
            ItemID = itemID;
            Quantity = quantity;
        }
    }

    public class EquipmentItemData
    {
        public int ItemID { get; set; }
        public ItemType SlotType { get; set; }

        public EquipmentItemData() { }

        public EquipmentItemData(int itemID, ItemType slotType)
        {
            ItemID = itemID;
            SlotType = slotType;
        }
    }

    public static class SaveManager
    {
        private static readonly string SavesDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Saves");

        static SaveManager()
        {
            if (!Directory.Exists(SavesDirectory))
            {
                Directory.CreateDirectory(SavesDirectory);
            }
        }

        public static void SaveGame(Player player, string saveName = "quicksave")
        {
            var save = new GameSave
            {
                SaveName = saveName,
                SaveDate = DateTime.Now,

                Name = player.Name,
                Gold = player.Gold,
                CurrentHP = player.CurrentHP,
                MaximumHP = player.TotalMaximumHP,
                CurrentEXP = player.CurrentEXP,
                MaximumEXP = player.MaximumEXP,
                Level = player.Level,
                BaseAttack = player.BaseAttack,
                BaseDefence = player.BaseDefence,
                BaseAgility = player.BaseAgility,
                LocationID = player.CurrentLocation?.ID ?? Constants.LOCATION_ID_VILLAGE,

                Inventory = player.Inventory.Items.Select(ii =>
                    new InventoryItemData(ii.Details.ID, ii.Quantity)).ToList(),

                EquippedItems = player.Inventory.EquippedItems.Select(ei =>
                    new EquipmentItemData(ei.Details.ID, ei.Details.Type)).ToList(),

                CompletedQuests = player.QuestLog.CompletedQuests.Select(q => q.ID).ToList(),
                ActiveQuests = player.QuestLog.ActiveQuests.Select(q => q.ID).ToList(),
                MonstersKilled = player.MonstersKilled,
                QuestsCompleted = player.QuestsCompleted
            };

            string json = JsonSerializer.Serialize(save, new JsonSerializerOptions
            {
                WriteIndented = true,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            });
            string filePath = Path.Combine(SavesDirectory, $"{saveName}.json");

            File.WriteAllText(filePath, json);

            MessageSystem.AddMessage($"Игра сохранена: {saveName}");
        }

        public static Player LoadGame(string saveName, IWorldRepository worldRepository)
        {
            string filePath = Path.Combine(SavesDirectory, $"{saveName}.json");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Сохранение не найдено");
            }

            string json = File.ReadAllText(filePath);
            GameSave save = JsonSerializer.Deserialize<GameSave>(json);

            // Создаем игрока с передачей репозитория
            var player = new Player(
                save.Name, save.Gold, save.CurrentHP, save.MaximumHP,
                save.CurrentEXP, save.MaximumEXP, save.Level,
                save.BaseAttack, save.BaseDefence, save.BaseAgility,
                GameServices.WorldRepository // Добавляем репозиторий
            );

            // В цикле загрузки инвентаря:
            foreach (var itemData in save.Inventory)
            {
                Item item = worldRepository.ItemByID(itemData.ItemID);
                if (item != null)
                {
                    player.Inventory.AddItem(item, itemData.Quantity);
                }
            }

            // Загружаем экипировку (новый способ)
            foreach (var equippedItem in save.EquippedItems)
            {
                Equipment equipment = worldRepository.ItemByID(equippedItem.ItemID) as Equipment;
                if (equipment != null)
                {
                    // Находим соответствующий предмет в инвентаре
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == equippedItem.ItemID);

                    if (inventoryItem != null)
                    {
                        // Экипируем предмет
                        if (player.Inventory.EquipItem(inventoryItem))
                        {
                            // Успешно экипировано, удаляем из инвентаря
                            player.Inventory.RemoveItem(inventoryItem, 1);
                        }
                    }
                    else
                    {
                        // Если предмета нет в инвентаре, создаем его и экипируем
                        var tempItem = new InventoryItem(equipment, 1);
                        if (player.Inventory.EquipItem(tempItem))
                        {
                            // Предмет успешно экипирован
                        }
                    }
                }
            }

            // Загружаем квесты
            foreach (int questID in save.ActiveQuests)
            {
                Quest quest = worldRepository.QuestByID(questID);
                if (quest != null)
                {
                    player.QuestLog.ActiveQuests.Add(quest);
                }
            }

            foreach (int questID in save.CompletedQuests)
            {
                Quest quest = worldRepository.QuestByID(questID);
                if (quest != null)
                {
                    player.QuestLog.CompletedQuests.Add(quest);
                }
            }

            // Загружаем статистику
            player.MonstersKilled = save.MonstersKilled;
            player.QuestsCompleted = save.QuestsCompleted;

            // Загружаем локацию
            player.CurrentLocation = worldRepository.LocationByID(save.LocationID);

            return player;
        }

        public static List<string> GetAvailableSaves()
        {
            if (!Directory.Exists(SavesDirectory))
                return new List<string>();

            return Directory.GetFiles(SavesDirectory, "*.json")
                .Select(Path.GetFileNameWithoutExtension)
                .ToList();
        }

        public static void DeleteSave(string saveName)
        {
            string filePath = Path.Combine(SavesDirectory, $"{saveName}.json");
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }
    }
}