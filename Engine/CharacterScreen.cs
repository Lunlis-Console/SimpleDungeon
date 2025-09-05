﻿using System;
using System.Collections.Generic;

namespace Engine
{
    public static class CharacterScreen
    {
        public static void Show(Player player)
        {
            bool viewing = true;

            while (viewing)
            {
                GameServices.OutputService.Clear();
                //DisplayCharacterDetails(player);
                GameServices.Renderer.RenderCharacterScreen(player);


                var key = Console.ReadKey(true).Key;

                switch (key)
                {
                    case ConsoleKey.Q:
                    case ConsoleKey.Escape:
                        viewing = false;
                        break;

                    case ConsoleKey.I:
                        player.DisplayInventory();
                        break;

                    case ConsoleKey.S:
                        // Можно добавить меню навыков позже
                        MessageSystem.AddMessage("Система навыков в разработке!");
                        break;

                    case ConsoleKey.T:
                        ShowTitlesMenu(player);
                        break;
                }
            }
        }

        private static void ShowTitlesMenu(Player player)
        {
            var titles = player.UnlockedTitles;

            if (titles.Count == 0)
            {
                GameServices.OutputService.Clear();
                GameServices.OutputService.WriteLine("У вас нет разблокированных титулов!");
                GameServices.OutputService.WriteLine("\nНажмите любую клавишу...");
                Console.ReadKey();
                return;
            }

            var selectedTitle = MenuSystem.SelectFromList(
                titles,
                title => $"{title.Name} {(title.IsActive ? "[АКТИВЕН]" : "")}",
                "====== ВАШИ ТИТУЛЫ ======",
                "Клавиши 'W' 'S' для выбора, 'E' - активировать/деактивировать, 'Q' - назад"
            );

            if (selectedTitle != null)
            {
                if (selectedTitle.IsActive)
                {
                    player.DeactivateTitle();
                }
                else
                {
                    player.ActivateTitle(selectedTitle);
                }

                // Показываем информацию о титуле
                GameServices.OutputService.Clear();
                GameServices.OutputService.WriteLine($"====== {selectedTitle.Name} ======");
                GameServices.OutputService.WriteLine($"Описание: {selectedTitle.Description}");
                GameServices.OutputService.WriteLine($"Бонусы: {selectedTitle.GetBonusDescription()}");
                GameServices.OutputService.WriteLine($"Требование: {selectedTitle.RequirementAmount} {GetRequirementDescription(selectedTitle)}");
                GameServices.OutputService.WriteLine("\nНажмите любую клавишу...");
                Console.ReadKey();
            }
        }

        private static string GetRequirementDescription(Title title)
        {
            switch (title.RequirementType)
            {
                case "MonsterKill": return $"убийств {title.RequirementTarget}";
                case "QuestComplete": return "выполненных квестов";
                case "TotalMonstersKilled": return "убитых монстров";
                default: return "";
            }
        }

    }
}
