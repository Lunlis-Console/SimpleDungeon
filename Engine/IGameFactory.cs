using Engine.Entities;

namespace Engine
{
    public interface IGameFactory
    {
        Player CreateNewPlayer();
        Player CreatePlayerFromSave(GameSave save);
        Monster CreateMonster(int monsterId, int level);
        // Добавьте другие методы создания по необходимости
    }
}
