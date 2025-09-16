using Engine.Quests;
using Engine.Data;
using System.Linq;

namespace JsonEditor
{
    /// <summary>
    /// Класс для отображения QuestItemSpawnData в ListBox
    /// </summary>
    public class SpawnLocationDisplayItem
    {
        public QuestItemSpawnData SpawnData { get; }
        private GameData _gameData;
        
        public SpawnLocationDisplayItem(QuestItemSpawnData spawnData, GameData gameData = null)
        {
            SpawnData = spawnData;
            _gameData = gameData;
        }
        
        public override string ToString()
        {
            string locationName = $"Unknown Location (ID: {SpawnData.LocationID})";
            
            // Пытаемся найти локацию в GameData
            if (_gameData?.Locations != null)
            {
                var location = _gameData.Locations.FirstOrDefault(l => l.ID == SpawnData.LocationID);
                if (location != null)
                {
                    locationName = location.Name;
                }
            }

            return $"{locationName} (Шанс: {SpawnData.SpawnChance}%, Кол-во: {SpawnData.Quantity}, Макс: {SpawnData.MaxItemsOnLocation}, Интервал: {SpawnData.SpawnInterval})";
        }
    }
}
