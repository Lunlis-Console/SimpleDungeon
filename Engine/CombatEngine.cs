using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Engine
{
    public class CombatEngine
    {
        public Player Player { get; set; }
        public Monster Monster { get; set; }

        // Сообщения о действиях для отображения в UI
        public string PlayerActionMessage { get; private set; }
        public string MonsterActionMessage { get; private set; }

        public CombatEngine(Player player, Monster monster)
        {
            Player = player;
            Monster = monster;
            Player.IsInCombat = true;
            Player.CurrentMonster = monster;

            PlayerActionMessage = "";
            MonsterActionMessage = "";
        }

        public void CombatLoop()
        {
            // Сброс сообщений в начале каждого раунда
            PlayerActionMessage = "";
            MonsterActionMessage = "";

            while (Player.IsInCombat && Player.CurrentHP > 0 && Monster.CurrentHP > 0)
            {
                // Отрисовка состояния боя (это можно тоже вынести в отдельный метод)
                RenderCombatState();

                // Обработка действия игрока
                ProcessPlayerInput();

                // Если игрок сбежал или монстр убит - выходим из цикла
                if (!Player.IsInCombat || Monster.CurrentHP <= 0)
                {
                    break;
                }

                // Ход монстра
                MonsterTurn();

                // Проверка на смерть игрока
                if (Player.CurrentHP <= 0)
                {
                    PlayerActionMessage = "Вы погибли.";
                    Player.IsInCombat = false;
                    break;
                }
            }

            // Завершение боя
            if (Monster.CurrentHP <= 0)
            {
                //PlayerActionMessage = $"{Monster.Name} побежден!";
                Player.RecieveReward(Monster); // Этот метод остаётся в Player, так как награда влияет на его состояние.
                MessageSystem.AddMessage($"{Monster.Name} побежден!");
                Player.IsInCombat = false;

                Console.WriteLine("Нажмите любую клавишу, чтобы продолжить...");
                Console.ReadKey(true);

            }
        }

        private void RenderCombatState()
        {
            Console.Clear();
            Console.WriteLine($"======={Monster.Name}========");
            Console.WriteLine($"ОЗ: {Monster.CurrentHP}/{Monster.MaximumHP} | АТК: {Monster.Attack} | ЗЩТ: {Monster.Defence}");
            Console.WriteLine($"====================================");
            Console.WriteLine(string.IsNullOrEmpty(PlayerActionMessage) ? "" : PlayerActionMessage);
            Console.WriteLine($"------------------------------------");
            Console.WriteLine(string.IsNullOrEmpty(MonsterActionMessage) ? "" : MonsterActionMessage);
            Console.WriteLine($"========Игрок========");
            Console.WriteLine($"ОЗ: {Player.CurrentHP}/{Player.MaximumHP} | АТК: {Player.Attack} | ЗЩТ: {Player.Defence}");
            Console.WriteLine("=========Действия=========");
            Console.WriteLine("| 1 - атаковать | 2 - заклинание | 3 - защищаться | 4 - бежать |");
        }

        private void ProcessPlayerInput()
        {
            ConsoleKeyInfo key = Console.ReadKey(true);

            switch (key.Key)
            {
                case ConsoleKey.D1:
                case ConsoleKey.NumPad1:
                    PlayerActionMessage = PlayerAttack();
                    break;
                case ConsoleKey.D2:
                case ConsoleKey.NumPad2:
                    //PlayerActionMessage = PlayerSpell();
                    PlayerActionMessage = "Заклинания пока не реализованы!";
                    break;
                case ConsoleKey.D3:
                case ConsoleKey.NumPad3:
                    //PlayerActionMessage = PlayerDefend();
                    PlayerActionMessage = "Защита пока не реализована!";
                    break;
                case ConsoleKey.D4:
                case ConsoleKey.NumPad4:
                    PlayerActionMessage = TryToEscape();
                    break;
                default:
                    PlayerActionMessage = "Неизвестная команда!";
                    break;
            }
        }

        private string PlayerAttack()
        {
            int damage = Player.Attack + new Random().Next(1, 6) - Monster.Defence;
            damage = Math.Max(damage, 0); // Урон не может быть отрицательным

            Monster.CurrentHP -= damage;
            return $"Вы нанесли {damage} урона по {Monster.Name}!";
        }

        private string TryToEscape()
        {
            int escapeChance = 30;
            if (new Random().Next(100) < escapeChance)
            {
                Player.IsInCombat = false;
                return "Вам удалось сбежать!";
            }
            else
            {
                return "Вам не удалось сбежать!";
            }
        }

        private void MonsterTurn()
        {
            MonsterActionMessage = MonsterAttack();
        }

        private string MonsterAttack()
        {
            int damage = Monster.Attack + new Random().Next(1, 4) - Player.Defence;
            damage = Math.Max(damage, 0); // Урон не может быть отрицательным

            Player.CurrentHP -= damage;
            return $"{Monster.Name} наносит вам {damage} урона!";
        }

    }
}
