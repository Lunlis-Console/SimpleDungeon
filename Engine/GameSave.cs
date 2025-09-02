// GameSave.cs
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine
{
    public class GameSave
    {
        public string SaveName { get; set; }
        public DateTime SaveDate { get; set; }

        // Данные игрока
        public int Gold { get; set; }
        public int CurrentHP { get; set; }
        public int MaximumHP { get; set; }
        public int CurrentEXP { get; set; }
        public int MaximumEXP { get; set; }
        public int Level { get; set; }
        public int BaseAttack { get; set; }
        public int BaseDefence { get; set; }
        public int LocationID { get; set; }

        // Инвентарь
        public List<InventoryItemData> Inventory { get; set; }
        public List<EquipmentItemData> EquippedItems { get; set; }

        // Экипировка (для обратной совместимости)
        public int? EquippedHelmetID { get; set; }
        public int? EquippedArmorID { get; set; }
        public int? EquippedGlovesID { get; set; }
        public int? EquippedBootsID { get; set; }
        public int? EquippedWeaponID { get; set; }

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

                Gold = player.Gold,
                CurrentHP = player.CurrentHP,
                MaximumHP = player.MaximumHP,
                CurrentEXP = player.CurrentEXP,
                MaximumEXP = player.MaximumEXP,
                Level = player.Level,
                BaseAttack = player.BaseAttack,
                BaseDefence = player.BaseDefence,
                LocationID = player.CurrentLocation?.ID ?? World.LOCATION_ID_VILLAGE,

                Inventory = player.Inventory.Items.Select(ii =>
                    new InventoryItemData(ii.Details.ID, ii.Quantity)).ToList(),

                EquippedItems = player.Inventory.EquippedItems.Select(ei =>
                    new EquipmentItemData(ei.Details.ID, ei.Details.Type)).ToList(),

                // Сохраняем также старые поля для обратной совместимости
                EquippedHelmetID = player.Inventory.Helmet?.ID,
                EquippedArmorID = player.Inventory.Armor?.ID,
                EquippedGlovesID = player.Inventory.Gloves?.ID,
                EquippedBootsID = player.Inventory.Boots?.ID,
                EquippedWeaponID = player.Inventory.Weapon?.ID,

                CompletedQuests = player.QuestLog.CompletedQuests.Select(q => q.ID).ToList(),
                ActiveQuests = player.QuestLog.ActiveQuests.Select(q => q.ID).ToList(),
                MonstersKilled = player.MonstersKilled,
                QuestsCompleted = player.QuestsCompleted
            };

            string json = JsonSerializer.Serialize(save, new JsonSerializerOptions { WriteIndented = true });
            string filePath = Path.Combine(SavesDirectory, $"{saveName}.json");

            File.WriteAllText(filePath, json);

            MessageSystem.AddMessage($"Игра сохранена: {saveName}");
        }

        public static Player LoadGame(string saveName)
        {
            string filePath = Path.Combine(SavesDirectory, $"{saveName}.json");

            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException("Сохранение не найдено");
            }

            string json = File.ReadAllText(filePath);
            GameSave save = JsonSerializer.Deserialize<GameSave>(json);

            // Создаем игрока с базовыми характеристиками
            var player = new Player(
                save.Gold, save.CurrentHP, save.MaximumHP,
                save.CurrentEXP, save.MaximumEXP, save.Level,
                save.BaseAttack, save.BaseDefence
            );

            // Загружаем инвентарь
            foreach (var itemData in save.Inventory)
            {
                Item item = World.ItemByID(itemData.ItemID);
                if (item != null)
                {
                    player.Inventory.AddItem(item, itemData.Quantity);
                }
            }

            // Загружаем экипировку (новый способ)
            foreach (var equippedItem in save.EquippedItems)
            {
                Equipment equipment = World.ItemByID(equippedItem.ItemID) as Equipment;
                if (equipment != null)
                {
                    // Находим соответствующий предмет в инвентаре
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == equippedItem.ItemID);

                    if (inventoryItem != null)
                    {
                        player.Inventory.EquipItem(inventoryItem);
                    }
                }
            }

            // Загружаем экипировку (старый способ для обратной совместимости)
            if (save.EquippedHelmetID.HasValue)
            {
                Equipment helmet = World.ItemByID(save.EquippedHelmetID.Value) as Equipment;
                if (helmet != null)
                {
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == save.EquippedHelmetID.Value);
                    if (inventoryItem != null)
                    {
                        player.Inventory.EquipItem(inventoryItem);
                    }
                }
            }

            // Повторите для других слотов экипировки (Armor, Gloves, Boots, Weapon)
            if (save.EquippedArmorID.HasValue)
            {
                Equipment armor = World.ItemByID(save.EquippedArmorID.Value) as Equipment;
                if (armor != null)
                {
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == save.EquippedArmorID.Value);
                    if (inventoryItem != null)
                    {
                        player.Inventory.EquipItem(inventoryItem);
                    }
                }
            }

            if (save.EquippedGlovesID.HasValue)
            {
                Equipment gloves = World.ItemByID(save.EquippedGlovesID.Value) as Equipment;
                if (gloves != null)
                {
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == save.EquippedGlovesID.Value);
                    if (inventoryItem != null)
                    {
                        player.Inventory.EquipItem(inventoryItem);
                    }
                }
            }

            if (save.EquippedBootsID.HasValue)
            {
                Equipment boots = World.ItemByID(save.EquippedBootsID.Value) as Equipment;
                if (boots != null)
                {
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == save.EquippedBootsID.Value);
                    if (inventoryItem != null)
                    {
                        player.Inventory.EquipItem(inventoryItem);
                    }
                }
            }

            if (save.EquippedWeaponID.HasValue)
            {
                Equipment weapon = World.ItemByID(save.EquippedWeaponID.Value) as Equipment;
                if (weapon != null)
                {
                    var inventoryItem = player.Inventory.Items
                        .FirstOrDefault(ii => ii.Details.ID == save.EquippedWeaponID.Value);
                    if (inventoryItem != null)
                    {
                        player.Inventory.EquipItem(inventoryItem);
                    }
                }
            }

            // Загружаем квесты
            foreach (int questID in save.ActiveQuests)
            {
                Quest quest = World.QuestByID(questID);
                if (quest != null)
                {
                    player.QuestLog.ActiveQuests.Add(quest);
                }
            }

            foreach (int questID in save.CompletedQuests)
            {
                Quest quest = World.QuestByID(questID);
                if (quest != null)
                {
                    player.QuestLog.CompletedQuests.Add(quest);
                }
            }

            // Загружаем статистику
            player.MonstersKilled = save.MonstersKilled;
            player.QuestsCompleted = save.QuestsCompleted;

            // Загружаем локацию
            player.CurrentLocation = World.LocationByID(save.LocationID);

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