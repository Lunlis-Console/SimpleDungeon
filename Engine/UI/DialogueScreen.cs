using System;
using System.Linq;
using System.Collections.Generic;
using Engine.Entities;
using Engine.Dialogue;
using Engine.Trading;

namespace Engine.UI
{
    public class DialogueScreen : BaseScreen, Engine.Dialogue.IDialogueUI
    {
        private readonly NPC _npc;
        private readonly Player _player;
        private readonly ITrader _dialogueSuppliedTrader; // опционально: передавать из InteractionScreen
        private DialogueSystem.DialogueNode _currentNode;
        private int _selectedIndex;

        public DialogueSystem.DialogueOption SelectedOption { get; private set; }

        // Конструктор: передаём NPC и Player; опционально ITrader (если InteractionScreen умеет готовить трейдера)
        public DialogueScreen(NPC npc, Player player, ITrader traderForDialogue = null)
        {
            _npc = npc ?? throw new ArgumentNullException(nameof(npc));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _dialogueSuppliedTrader = traderForDialogue;

            _selectedIndex = 0;
            SelectedOption = null;

            // Попробуем инициализировать стартовый узел диалога
            TryInitializeCurrentNodeFromNpc();

            // Гарантируем перерисовку
            RequestFullRedraw();
        }

        #region --- Trade resolution & OpenTrade ---

        // Получаем ITrader: приоритет
        // 1) _dialogueSuppliedTrader (если InteractionScreen передал)
        // 2) NPC.Trader (если задан)
        // 3) Создать Merchant на лету, если у NPC есть ItemsForSale (List<InventoryItem>)
        private ITrader ResolveTraderFromNpc()
        {
            if (_dialogueSuppliedTrader != null) return _dialogueSuppliedTrader;

            try
            {
                if (_npc?.Trader != null) return _npc.Trader;
            }
            catch { /* игнор */ }

            try
            {
                if (_npc?.ItemsForSale != null && _npc.ItemsForSale.Count > 0)
                {
                    var merchant = new Merchant(_npc.Name ?? "Merchant", _npc.Greeting ?? string.Empty, 0)
                    {
                        // Merchant.ItemsForSale у тебя определён как List<InventoryItem>
                        ItemsForSale = new List<InventoryItem>(_npc.ItemsForSale),
                        Gold = 0
                    };
                    return merchant;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"DialogueScreen.ResolveTraderFromNpc: failed to construct Merchant: {ex.Message}");
            }

            return null;
        }

        // Вызывается из DialogueActions (или других мест), чтобы открыть торговлю
        public void OpenTrade()
        {
            var trader = ResolveTraderFromNpc();
            if (trader == null)
            {
                DebugConsole.Log("DialogueScreen.OpenTrade: NPC is not a trader");
                return;
            }

            try
            {
                var tradeScreen = new TradeScreen(trader, _player);
                ScreenManager.PushScreen(tradeScreen);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"DialogueScreen.OpenTrade: failed to open TradeScreen: {ex.Message}");
            }
        }

        #endregion

        #region --- Rendering ---

        public override void Render()
        {
            try { ClearScreen(); } catch { /* если что — молча игнорируем */ }

            RenderHeader(_npc?.Name ?? "NPC");
            RenderDialogueText();
            RenderOptions();
            RenderFooter("W/S - выбор │ E - ответить │ Q - выйти");
        }

        private void RenderDialogueText()
        {
            int y = 4;
            var text = _currentNode?.Text ?? _npc?.Greeting ?? "(пустой диалог)";
            var lines = WrapText(text, Math.Max(20, Width - 4));

            foreach (var line in lines)
            {
                if (y < Height - 10)
                {
                    try { _renderer.Write(2, y, line, ConsoleColor.White); } catch { }
                    y++;
                }
            }

            try { _renderer.Write(0, y + 1, new string('─', Math.Max(1, Width)), ConsoleColor.DarkGray); } catch { }
        }

        private void RenderOptions()
        {
            int startY = Math.Max(8, Height - 8);
            var options = _currentNode?.Options ?? new List<DialogueSystem.DialogueOption>();

            // Обновляем доступность опций через EvaluateCondition (safety)
            foreach (var opt in options)
            {
                try { opt.IsAvailable = opt.EvaluateCondition(_player); } catch { opt.IsAvailable = true; }
            }

            var availableOptions = options.Where(o => o.IsAvailable).ToList();

            if (availableOptions.Count == 0)
            {
                try { _renderer.Write(2, startY, "(Нет доступных ответов)", ConsoleColor.DarkGray); } catch { }
                return;
            }

            try { _renderer.Write(2, startY - 2, "ВЫБЕРИТЕ ОТВЕТ:", ConsoleColor.Cyan); } catch { }

            for (int i = 0; i < availableOptions.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                bool isVisited = availableOptions[i].IsVisited;
                int y = startY + i;
                var text = availableOptions[i].Text ?? "(пустой ответ)";

                try
                {
                    if (isSelected)
                    {
                        _renderer.Write(2, y, "►");
                        _renderer.Write(4, y, text);
                    }
                    else
                    {
                        if (isVisited)
                            _renderer.Write(4, y, text, ConsoleColor.DarkGray);
                        else
                            _renderer.Write(4, y, text, ConsoleColor.Gray);
                    }
                }
                catch { /* игнорируем ошибки отрисовки */ }
            }
        }

        private void RenderHeader(string title)
        {
            try
            {
                _renderer.Write(2, 1, title, ConsoleColor.Yellow);
                _renderer.Write(0, 2, new string('=', Math.Max(1, Width)), ConsoleColor.DarkGray);
            }
            catch { }
        }

        #endregion

        #region --- Input handling ---

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            var options = _currentNode?.Options ?? new List<DialogueSystem.DialogueOption>();
            var availableOptions = options.Where(o => o.IsAvailable).ToList();

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    if (availableOptions.Count > 0)
                    {
                        _selectedIndex = Math.Max(0, _selectedIndex - 1);
                        try { RequestPartialRedraw(); } catch { RequestFullRedraw(); }
                    }
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (availableOptions.Count > 0)
                    {
                        _selectedIndex = Math.Min(availableOptions.Count - 1, _selectedIndex + 1);
                        try { RequestPartialRedraw(); } catch { RequestFullRedraw(); }
                    }
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    if (availableOptions.Count > 0)
                    {
                        var selectedOption = availableOptions[_selectedIndex];
                        SelectedOption = selectedOption;
                        selectedOption.IsVisited = true;

                        try
                        {
                            // ExecuteSelection использует DialogueActions.Execute(...)
                            selectedOption.ExecuteSelection(_player, this);
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.Log("Ошибка при выполнении опции диалога: " + ex.Message);

                            if (selectedOption.NextNode != null)
                                SetCurrentNode(selectedOption.NextNode);
                            else
                                CloseDialogue();
                        }

                        // Если выбор открыл TradeScreen — не закрываем диалог и выходим
                        if (ScreenManager.CurrentScreen is TradeScreen)
                        {
                            try { RequestFullRedraw(); } catch { }
                            break;
                        }

                        // В противном случае — просто перерисуем диалог
                        try { RequestFullRedraw(); } catch { }
                    }
                    break;

                case ConsoleKey.Q:
                case ConsoleKey.Escape:
                    CloseDialogue();
                    break;
            }
        }

        #endregion

        #region --- IDialogueUI implementation ---

        public void SetCurrentNode(DialogueSystem.DialogueNode node)
        {
            if (node == null) return;
            _currentNode = node;
            _selectedIndex = 0;
            try { _currentNode.OnEnter?.Invoke(); } catch { }
            try { RequestFullRedraw(); } catch { }
        }

        public void CloseDialogue()
        {
            try { ScreenManager.PopScreen(); } catch { }
        }

        #endregion

        #region --- Utils: init current node from NPC if available ---

        private void TryInitializeCurrentNodeFromNpc()
        {
            try
            {
                // В проекте у NPC есть GreetingDialogue и/или Trader и т.д.
                // Попробуем взять GreetingDialogue, если он задан.
                if (_npc == null) { _currentNode = null; return; }

                if (_npc.GreetingDialogue != null)
                {
                    _currentNode = _npc.GreetingDialogue;
                    return;
                }

                // Если NPC.Trader предоставляет Greeting через GetShopGreeting, можно сделать заглушку:
                if (_npc.Trader != null)
                {
                    // Создаём простой узел с приветствием продавца
                    var text = _npc.Trader.ShopGreeting ?? _npc.Greeting ?? $"{_npc.Name}: Привет.";
                    _currentNode = new DialogueSystem.DialogueNode(text, null)
                    {
                        Options = new List<DialogueSystem.DialogueOption>()
                    };
                    return;
                }

                // fallback: пустой узел
                _currentNode = new DialogueSystem.DialogueNode(_npc.Greeting ?? "(пусто)", null)
                {
                    Options = new List<DialogueSystem.DialogueOption>()
                };
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"DialogueScreen.TryInitializeCurrentNodeFromNpc: {ex.Message}");
                _currentNode = null;
            }
        }

        #endregion
    }
}
