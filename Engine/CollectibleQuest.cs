namespace Engine
{
    public class CollectibleQuest : Quest
    {
        public List<CollectibleSpawn> SpawnLocations { get; set; }
        public bool IsItemsSpawned { get; set; }

        private readonly IWorldRepository _worldRepository;

        public CollectibleQuest(int id, string name, string description, int rewardEXP,
            int rewardGold, NPC questGiver, List<QuestItem> questItems, IWorldRepository worldRepository)
            : base(id, name, description, rewardEXP, rewardGold, questGiver)
        {
            QuestItems = questItems;
            SpawnLocations = new List<CollectibleSpawn>();
            IsItemsSpawned = false;
            _worldRepository = worldRepository;
        }

        public void SpawnCollectibles()
        {
            if (IsItemsSpawned) return;

            foreach (var spawn in SpawnLocations)
            {
                var location = _worldRepository.LocationByID(spawn.LocationID);
                if (location != null)
                {
                    // Очищаем старые предметы этого квеста
                    location.GroundItems.RemoveAll(item =>
                        QuestItems.Any(qi => qi.Details.ID == item.Details.ID));

                    // Добавляем новые
                    location.GroundItems.Add(new InventoryItem(
                        _worldRepository.ItemByID(spawn.ItemID), spawn.Quantity));
                }
            }
            IsItemsSpawned = true;
        }

        public void DespawnCollectibles()
        {
            foreach (var spawn in SpawnLocations)
            {
                var location = _worldRepository.LocationByID(spawn.LocationID);
                if (location != null)
                {
                    location.GroundItems.Add(new InventoryItem(
                        _worldRepository.ItemByID(spawn.ItemID), spawn.Quantity));
                }
            }
            IsItemsSpawned = false;
        }
    }

    public class CollectibleSpawn
    {
        public int LocationID { get; set; }
        public int ItemID { get; set; }
        public int Quantity { get; set; }

        public CollectibleSpawn(int locationID, int itemID, int quantity = 1)
        {
            LocationID = locationID;
            ItemID = itemID;
            Quantity = quantity;
        }
    }
}