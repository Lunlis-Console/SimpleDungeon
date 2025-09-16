using Engine.Quests;

namespace JsonEditor
{
    // Расширение класса QuestItemSpawnData для отображения в списке
    public static class QuestItemSpawnDataExtensions
    {
        public static string DisplayText(this QuestItemSpawnData spawnData)
        {
            var locationName = GetLocationName(spawnData.LocationID);
            return $"{locationName} - Шанс: {spawnData.SpawnChance}%, Количество: {spawnData.Quantity}, Макс: {spawnData.MaxItemsOnLocation}, Интервал: {spawnData.SpawnInterval}";
        }

        private static string GetLocationName(int locationID)
        {
            // Здесь можно добавить логику получения имени локации по ID
            // Пока возвращаем просто ID
            return $"Локация {locationID}";
        }
    }
}

