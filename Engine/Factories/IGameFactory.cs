using Engine.Entities;
using Engine.Saving;
using Engine.Data;

namespace Engine.Factories
{
    public interface IGameFactory
    {
        Player CreateNewPlayer();
        Player CreatePlayerFromSave(GameSave save);
        Monster CreateMonster(int monsterId, int level);
        // Добавьте методы фабрики предметов:
        /// <summary>
        /// Создаёт InventoryItem по строковому параметру: поддерживает "1001,5", "rat_meat,5", "itemId:1001;qty:5" и т.п.
        /// Возвращает null, если не удалось.
        /// </summary>
        InventoryItem CreateInventoryItem(string param);

        /// <summary>
        /// Прямое создание по числовому ID (возвращает InventoryItem или null).
        /// </summary>
        InventoryItem CreateInventoryItemById(int id, int qty = 1);

        /// <summary>
        /// Создает сундук из данных
        /// </summary>
        Chest CreateChest(ChestData chestData);

        /// <summary>
        /// Создает сундук по ID
        /// </summary>
        Chest CreateChestById(int chestId);
    }
}
