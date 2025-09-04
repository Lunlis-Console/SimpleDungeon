using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
