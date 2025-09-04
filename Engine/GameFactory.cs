using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class GameFactory : IGameFactory
    {
        private readonly IWorldRepository _worldRepository;

        public GameFactory(IWorldRepository worldRepository)
        {
            _worldRepository = worldRepository;
        }

        public Player CreateNewPlayer()
        {
            return new Player(0, 100, 100, 0, 100, 1, 0, 0, 10, _worldRepository);
        }

        public Player CreatePlayerFromSave(GameSave save)
        {
            var player = new Player(
                save.Gold, save.CurrentHP, save.MaximumHP,
                save.CurrentEXP, save.MaximumEXP, save.Level,
                save.BaseAttack, save.BaseDefence, save.BaseAgility,
                _worldRepository
            );

            // ... остальная логика загрузки
            return player;
        }

        public Monster CreateMonster(int monsterId, int level)
        {
            var baseMonster = _worldRepository.MonsterByID(monsterId);
            return new Monster(baseMonster, level);
        }
    }
}
