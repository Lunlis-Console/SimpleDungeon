using Engine.Core;
using Engine.Entities;
using Engine.World;
using Engine.Quests;
using Engine.Trading;
using System.Linq;

namespace Engine.UI
{
    public class NPCInspectScreen : BaseScreen
    {
        private readonly NPC _npc;
        private readonly Player _player;

        public NPCInspectScreen(NPC npc, Player player)
        {
            _npc = npc ?? throw new ArgumentNullException(nameof(npc));
            _player = player ?? throw new ArgumentNullException(nameof(player));
        }

        public override void Render()
        {
            ClearScreen();
            RenderHeader($"ОСМОТР: {_npc.Name}");
            RenderNPCInfo();
            RenderFooter("Q - назад");
        }

        private void RenderNPCInfo()
        {
            int y = 4;

            // Основная информация
            _renderer.Write(2, y, $"Имя: {_npc.Name}", ConsoleColor.Cyan);
            y += 2;

            _renderer.Write(2, y, "Описание:", ConsoleColor.Yellow);
            y++;
            var greeting = _npc.Greeting ?? "Обычный житель";
            var lines = WrapText(greeting, Width - 4);
            foreach (var line in lines)
            {
                _renderer.Write(4, y, line);
                y++;
            }
            y++;

            // Квесты
            if (_npc.QuestsToGive != null && _npc.QuestsToGive.Count > 0)
            {
                _renderer.Write(2, y, "Может дать квестов:", ConsoleColor.Yellow);
                y++;
                foreach (var questId in _npc.QuestsToGive)
                {
                    var quest = _player.QuestLog.GetQuest(questId);
                    if (quest != null)
                    {
                        var status = GetQuestStatusText(quest.State);
                        _renderer.Write(4, y, $"• {quest.Name} - {status}");
                        y++;
                    }
                }
                y++;
            }

            // Торговец
            if (_npc.Trader != null)
            {
                _renderer.Write(2, y, "Торговец:", ConsoleColor.Yellow);
                y++;
                _renderer.Write(4, y, "• Может торговать");
                y++;
                
                if (_npc.Trader is Merchant merchant)
                {
                    _renderer.Write(4, y, $"• Золото: {merchant.Gold}");
                    y++;
                    if (merchant.ItemsForSale != null && merchant.ItemsForSale.Count > 0)
                    {
                        _renderer.Write(4, y, $"• Товаров в продаже: {merchant.ItemsForSale.Count}");
                        y++;
                    }
                }
                y++;
            }

            // Доступные действия
            _renderer.Write(2, y, "Доступные действия:", ConsoleColor.Yellow);
            y++;
            var actions = _npc.GetAvailableActions(_player);
            foreach (var action in actions)
            {
                _renderer.Write(4, y, $"• {action}");
                y++;
            }
        }

        private string GetQuestStatusText(QuestState state)
        {
            switch (state)
            {
                case QuestState.NotStarted: return "Не начат";
                case QuestState.InProgress: return "В процессе";
                case QuestState.ReadyToComplete: return "Готов к завершению";
                case QuestState.Completed: return "Завершен";
                default: return "Неизвестно";
            }
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            HandleCommonInput(keyInfo);

            switch (keyInfo.Key)
            {
                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    ScreenManager.PopScreen();
                    break;
            }
        }
    }
}
