using Engine.Entities;
using Engine.Saving;

namespace Engine.Factories
{
    public interface IGameFactory
    {
        Player CreateNewPlayer();
        Player CreatePlayerFromSave(GameSave save);
        Monster CreateMonster(int monsterId, int level);
        // Добавьте другие методы создания по необходимости
    }
}
