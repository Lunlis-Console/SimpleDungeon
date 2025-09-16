using Engine.Core;
using Engine.Entities;
using Engine.World;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.Quests
{
    /// <summary>
    /// Менеджер для управления спавном предметов квестов на локациях
    /// </summary>
    public class QuestItemSpawnManager
    {
        private static QuestItemSpawnManager _instance;
        public static QuestItemSpawnManager Instance => _instance ??= new QuestItemSpawnManager();
        
        private QuestItemSpawnManager() { }

        /// <summary>
        /// Инициализирует спавн предметов для активных квестов
        /// </summary>
        public void InitializeQuestItemSpawns(List<EnhancedQuest> activeQuests)
        {
            DebugConsole.Log($"QuestItemSpawnManager.InitializeQuestItemSpawns: Processing {activeQuests.Count} active quests");
            
            foreach (var quest in activeQuests)
            {
                if (quest.State != QuestState.InProgress) continue;
                
                var collectConditions = quest.GetRuntimeConditions()
                    .OfType<CollectItemsCondition>()
                    .Where(c => c.SpawnLocations.Any());
                
                foreach (var condition in collectConditions)
                {
                    ProcessQuestItemSpawn(condition);
                }
            }
        }

        /// <summary>
        /// Обрабатывает спавн предметов для конкретного условия квеста
        /// </summary>
        public void ProcessQuestItemSpawn(CollectItemsCondition condition)
        {
            if (condition.SpawnLocations == null || !condition.SpawnLocations.Any())
                return;

            DebugConsole.Log($"QuestItemSpawnManager.ProcessQuestItemSpawn: Processing condition {condition.ID} for item {condition.ItemID}");

            foreach (var spawnData in condition.SpawnLocations)
            {
                // Проверяем интервал спавна
                spawnData.SpawnCounter++;
                if (spawnData.SpawnCounter < spawnData.SpawnInterval)
                    continue;

                // Сбрасываем счетчик
                spawnData.SpawnCounter = 0;

                // Проверяем шанс спавна
                var random = new Random();
                if (random.Next(1, 101) > spawnData.SpawnChance)
                    continue;

                // Получаем локацию
                var location = GameServices.WorldRepository.LocationByID(spawnData.LocationID);
                if (location == null)
                {
                    DebugConsole.Log($"QuestItemSpawnManager.ProcessQuestItemSpawn: Location {spawnData.LocationID} not found");
                    continue;
                }

                // Проверяем максимальное количество предметов на локации
                var currentItemCount = location.GroundItems
                    .Where(item => item.Details.ID == condition.ItemID)
                    .Sum(item => item.Quantity);

                if (currentItemCount >= spawnData.MaxItemsOnLocation)
                {
                    DebugConsole.Log($"QuestItemSpawnManager.ProcessQuestItemSpawn: Max items ({spawnData.MaxItemsOnLocation}) already on location {spawnData.LocationID}");
                    continue;
                }

                // Спавним предмет
                SpawnQuestItemOnLocation(condition.ItemID, spawnData.Quantity, location);
                DebugConsole.Log($"QuestItemSpawnManager.ProcessQuestItemSpawn: Spawned {spawnData.Quantity} items {condition.ItemID} on location {spawnData.LocationID}");
            }
        }

        /// <summary>
        /// Спавнит предмет квеста на указанной локации
        /// </summary>
        private void SpawnQuestItemOnLocation(int itemID, int quantity, Location location)
        {
            var item = GameServices.WorldRepository.ItemByID(itemID);
            if (item == null)
            {
                DebugConsole.Log($"QuestItemSpawnManager.SpawnQuestItemOnLocation: Item {itemID} not found");
                return;
            }

            // Проверяем, есть ли уже такой предмет на локации
            var existingItem = location.GroundItems.FirstOrDefault(gi => gi.Details.ID == itemID);
            
            if (existingItem != null)
            {
                // Увеличиваем количество существующего предмета
                existingItem.Quantity += quantity;
            }
            else
            {
                // Создаем новый предмет на земле
                var newItem = new InventoryItem(item, quantity);
                location.GroundItems.Add(newItem);
            }

            DebugConsole.Log($"QuestItemSpawnManager.SpawnQuestItemOnLocation: Added {quantity} {item.Name} to location {location.Name}");
        }

        /// <summary>
        /// Очищает предметы квеста с локаций при завершении квеста
        /// </summary>
        public void CleanupQuestItems(CollectItemsCondition condition)
        {
            if (condition.SpawnLocations == null || !condition.SpawnLocations.Any())
                return;

            DebugConsole.Log($"QuestItemSpawnManager.CleanupQuestItems: Cleaning up items for condition {condition.ID}");

            foreach (var spawnData in condition.SpawnLocations)
            {
                var location = GameServices.WorldRepository.LocationByID(spawnData.LocationID);
                if (location == null) continue;

                // Удаляем предметы квеста с локации
                location.GroundItems.RemoveAll(item => item.Details.ID == condition.ItemID);
                
                DebugConsole.Log($"QuestItemSpawnManager.CleanupQuestItems: Removed quest items from location {spawnData.LocationID}");
            }
        }

        /// <summary>
        /// Удаляет предметы квеста из инвентаря игрока при завершении квеста
        /// </summary>
        public void RemoveQuestItemsFromPlayer(CollectItemsCondition condition, Player player)
        {
            if (player?.Inventory == null) return;

            DebugConsole.Log($"QuestItemSpawnManager.RemoveQuestItemsFromPlayer: Removing quest items {condition.ItemID} from player inventory");

            // Получаем количество предметов квеста в инвентаре игрока
            var questItemQuantity = player.Inventory.GetItemQuantity(condition.ItemID);
            
            if (questItemQuantity > 0)
            {
                // Удаляем только необходимое количество предметов для квеста
                var itemsToRemove = Math.Min(questItemQuantity, condition.RequiredAmount);
                
                // Удаляем предметы квеста из инвентаря
                var questItem = GameServices.WorldRepository.ItemByID(condition.ItemID);
                if (questItem != null)
                {
                    player.Inventory.RemoveItem(questItem, itemsToRemove);
                    DebugConsole.Log($"QuestItemSpawnManager.RemoveQuestItemsFromPlayer: Removed {itemsToRemove} {questItem.Name} from player inventory (quest required {condition.RequiredAmount}, player had {questItemQuantity})");
                }
            }
        }

        /// <summary>
        /// Принудительно спавнит предметы квеста на всех указанных локациях
        /// </summary>
        public void ForceSpawnQuestItems(CollectItemsCondition condition)
        {
            if (condition.SpawnLocations == null || !condition.SpawnLocations.Any())
                return;

            DebugConsole.Log($"QuestItemSpawnManager.ForceSpawnQuestItems: Force spawning items for condition {condition.ID}");

            foreach (var spawnData in condition.SpawnLocations)
            {
                var location = GameServices.WorldRepository.LocationByID(spawnData.LocationID);
                if (location == null) continue;

                // Проверяем, не превышаем ли максимальное количество
                var currentItemCount = location.GroundItems
                    .Where(item => item.Details.ID == condition.ItemID)
                    .Sum(item => item.Quantity);

                if (currentItemCount >= spawnData.MaxItemsOnLocation)
                    continue;

                // Спавним предмет
                SpawnQuestItemOnLocation(condition.ItemID, spawnData.Quantity, location);
            }
        }

        /// <summary>
        /// Получает информацию о предметах квеста на локации
        /// </summary>
        public string GetQuestItemsInfo(int locationID, int questItemID)
        {
            var location = GameServices.WorldRepository.LocationByID(locationID);
            if (location == null) return "";

            var questItems = location.GroundItems
                .Where(item => item.Details.ID == questItemID)
                .ToList();

            if (!questItems.Any()) return "";

            var totalQuantity = questItems.Sum(item => item.Quantity);
            var itemName = questItems.First().Details.Name;
            
            return $"На земле: {itemName} x{totalQuantity}";
        }
    }
}
