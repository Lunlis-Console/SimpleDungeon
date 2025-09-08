using Engine.Entities;
using Engine.Quests;
using Engine.World;

namespace Engine
{
    public interface IWorldRepository
    {
        // Методы для поиска сущностей по ID
        Item ItemByID(int id);
        Monster MonsterByID(int id);
        Location LocationByID(int id);
        Quest QuestByID(int id);
        NPC NPCByID(int id);
        Title TitleByID(int id);

        // Методы для получения всех сущностей (если нужны)
        List<Item> GetAllItems();
        List<Monster> GetAllMonsters();
        List<Location> GetAllLocations();
        List<Quest> GetAllQuests();
        List<NPC> GetAllNPCs();
        List<Title> GetAllTitles();

        // Метод для инициализации (можно будет убрать позже)
        void Initialize();
    }
}