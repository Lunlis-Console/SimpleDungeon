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
        public int Attack { get; set; }
        public int Defence { get; set; }
        public int LocationID { get; set; }

        // Инвентарь
        public List<InventoryItemData> Inventory { get; set; }

        // Экипировка
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
                Attack = player.Attack,
                Defence = player.Defence,
                LocationID = player.CurrentLocation?.ID ?? World.LOCATION_ID_VILLAGE,

                Inventory = player.Inventory.Select(ii =>
                    new InventoryItemData(ii.Details.ID, ii.Quantity)).ToList(),

                EquippedHelmetID = player.EquipmentHelmet?.ID,
                EquippedArmorID = player.EquipmentArmor?.ID,
                EquippedGlovesID = player.EquipmentGloves?.ID,
                EquippedBootsID = player.EquipmentBoots?.ID,
                EquippedWeaponID = player.EquipmentWeapon?.ID,

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

            // Создаем игрока
            var player = new Player(
                save.Gold, save.CurrentHP, save.MaximumHP,
                save.CurrentEXP, save.MaximumEXP, save.Level,
                save.Attack, save.Defence
            );

            // Загружаем инвентарь
            foreach (var itemData in save.Inventory)
            {
                Item item = World.ItemByID(itemData.ItemID);
                if (item != null)
                {
                    player.AddItemToInventory(item, itemData.Quantity);
                }
            }

            // Загружаем экипировку
            if (save.EquippedHelmetID.HasValue)
                player.EquipmentHelmet = (Equipment)World.ItemByID(save.EquippedHelmetID.Value);
            if (save.EquippedArmorID.HasValue)
                player.EquipmentArmor = (Equipment)World.ItemByID(save.EquippedArmorID.Value);
            if (save.EquippedGlovesID.HasValue)
                player.EquipmentGloves = (Equipment)World.ItemByID(save.EquippedGlovesID.Value);
            if (save.EquippedBootsID.HasValue)
                player.EquipmentBoots = (Equipment)World.ItemByID(save.EquippedBootsID.Value);
            if (save.EquippedWeaponID.HasValue)
                player.EquipmentWeapon = (Equipment)World.ItemByID(save.EquippedWeaponID.Value);

            player.UpdateStats();

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