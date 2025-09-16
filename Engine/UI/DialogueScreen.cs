using System;
using System.Linq;
using System.Collections.Generic;
using Engine.Entities;
using Engine.Dialogue;
using Engine.Trading;
using Engine.Core;
using Engine.World;
using Engine.Quests;
using Engine.Data;

namespace Engine.UI
{
    public class DialogueScreen : BaseScreen, Engine.Dialogue.IDialogueUI
    {
        private readonly NPC _npc;
        private readonly Player _player;
        private readonly ITrader _dialogueSuppliedTrader; // опционально: передавать из InteractionScreen
        private DialogueDocument _dialogueDocument;
        private DialogueNode _currentNode;
        private int _selectedIndex;

        public Response SelectedOption { get; private set; }

        // Конструктор: передаём NPC и Player; опционально ITrader (если InteractionScreen умеет готовить трейдера)
        public DialogueScreen(NPC npc, Player player, ITrader traderForDialogue = null)
        {
            _npc = npc ?? throw new ArgumentNullException(nameof(npc));
            _player = player ?? throw new ArgumentNullException(nameof(player));
            _dialogueSuppliedTrader = traderForDialogue;

            _selectedIndex = 0;
            SelectedOption = null;

            // Включаем режим диалога для MessageSystem
            MessageSystem.EnterDialogueMode();

            // Уведомляем QuestManager о начале разговора с NPC
            var questManager = GameServices.QuestManager;
            if (questManager != null)
            {
                questManager.OnNPCTalked(_npc, _player);
            }

            // Попробуем инициализировать диалог из NPC
            TryInitializeDialogueFromNpc();

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
            var responses = _currentNode?.Responses ?? new List<Response>();

            // Фильтруем опции по условиям видимости
            var availableResponses = new List<Response>();
            foreach (var response in responses)
            {
                if (EvaluateResponseCondition(response))
                {
                    availableResponses.Add(response);
                }
            }

            if (availableResponses.Count == 0)
            {
                try { _renderer.Write(2, startY, "(Нет доступных ответов)", ConsoleColor.DarkGray); } catch { }
                return;
            }

            // Простой заголовок для всех опций
            try { _renderer.Write(2, startY - 2, "ВЫБЕРИТЕ ОТВЕТ:", ConsoleColor.Cyan); } catch { }

            int currentY = startY;

            // Показываем все опции в едином списке
            for (int i = 0; i < availableResponses.Count; i++)
            {
                bool isSelected = i == _selectedIndex;
                var text = availableResponses[i].Text ?? "(пустой ответ)";

                try
                {
                    if (isSelected)
                    {
                        _renderer.Write(2, currentY, "►");
                        _renderer.Write(4, currentY, text, ConsoleColor.White);
                    }
                    else
                    {
                        _renderer.Write(4, currentY, text, ConsoleColor.Gray);
                    }
                }
                catch { /* игнорируем ошибки отрисовки */ }
                currentY++;
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
            HandleCommonInput(keyInfo);

            var responses = _currentNode?.Responses ?? new List<Response>();
            var availableResponses = responses.Where(r => EvaluateResponseCondition(r)).ToList();

            switch (keyInfo.Key)
            {
                case ConsoleKey.W:
                case ConsoleKey.UpArrow:
                    if (availableResponses.Count > 0)
                    {
                        _selectedIndex = Math.Max(0, _selectedIndex - 1);
                        try { RequestPartialRedraw(); } catch { RequestFullRedraw(); }
                    }
                    break;

                case ConsoleKey.S:
                case ConsoleKey.DownArrow:
                    if (availableResponses.Count > 0)
                    {
                        _selectedIndex = Math.Min(availableResponses.Count - 1, _selectedIndex + 1);
                        try { RequestPartialRedraw(); } catch { RequestFullRedraw(); }
                    }
                    break;

                case ConsoleKey.E:
                case ConsoleKey.Enter:
                    if (availableResponses.Count > 0)
                    {
                        var selectedResponse = availableResponses[_selectedIndex];
                        SelectedOption = selectedResponse;

                        try
                        {
                            // Выполняем действия ответа
                            ExecuteResponseActions(selectedResponse);

                            // Переходим к следующему узлу, если указан
                            if (!string.IsNullOrEmpty(selectedResponse.Target))
                            {
                                var nextNode = _dialogueDocument?.Nodes?.FirstOrDefault(n => n.Id == selectedResponse.Target);
                                if (nextNode != null)
                                {
                                    SetCurrentNode(nextNode);
                                }
                                else
                                {
                                    DebugConsole.Log($"Узел диалога '{selectedResponse.Target}' не найден");
                                    CloseDialogue();
                                }
                            }
                            else
                            {
                                // Если нет следующего узла, закрываем диалог
                                CloseDialogue();
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.Log("Ошибка при выполнении ответа диалога: " + ex.Message);
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

        public void SetCurrentNode(DialogueNode node)
        {
            if (node == null) return;
            _currentNode = node;
            _selectedIndex = 0;
            try { RequestFullRedraw(); } catch { }
        }

        public void CloseDialogue()
        {
            // Выходим из режима диалога для MessageSystem
            MessageSystem.ExitDialogueMode();
            
            try { ScreenManager.PopScreen(); } catch { }
        }

        #endregion

        #region --- Utils: init dialogue from NPC if available ---

        private void TryInitializeDialogueFromNpc()
        {
            try
            {
                if (_npc == null) 
                { 
                    _currentNode = null; 
                    return; 
                }

                // Получаем диалог из NPC
                _dialogueDocument = _npc.GetDialogueDocument();
                
                if (_dialogueDocument != null && _dialogueDocument.Nodes != null)
                {
                    // Находим стартовый узел
                    var startNode = _dialogueDocument.Nodes.FirstOrDefault(n => n.Id == _dialogueDocument.Start);
                    if (startNode != null)
                    {
                        _currentNode = startNode;
                        DebugConsole.Log($"DialogueScreen: Инициализирован диалог '{_dialogueDocument.Name}' с узлом '{startNode.Id}'");
                        return;
                    }
                }

                // Если нет диалога, создаем простой узел с приветствием
                _currentNode = new DialogueNode
                {
                    Id = "default_greeting",
                    Text = _npc.Greeting ?? $"{_npc.Name}: Привет.",
                    Responses = new List<Response>()
                };

                // Добавляем базовые опции
                if (_npc.Trader != null || (_npc.ItemsForSale != null && _npc.ItemsForSale.Count > 0))
                {
                    _currentNode.Responses.Add(new Response
                    {
                        Text = "Покажи мне свои товары...",
                        Actions = new List<Engine.Dialogue.DialogueAction>
                        {
                            new Engine.Dialogue.DialogueAction { Type = "StartTrade" }
                        }
                    });
                }

                _currentNode.Responses.Add(new Response
                {
                    Text = "Я пойду.",
                    Actions = new List<Engine.Dialogue.DialogueAction>
                    {
                        new Engine.Dialogue.DialogueAction { Type = "EndDialogue" }
                    }
                });

                DebugConsole.Log("DialogueScreen: Создан простой диалог с приветствием");
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"DialogueScreen.TryInitializeDialogueFromNpc: {ex.Message}");
                _currentNode = null;
            }
        }

        #endregion

        #region --- Helper methods ---

        /// <summary>
        /// Проверяет условие видимости для ответа диалога
        /// </summary>
        private bool EvaluateResponseCondition(Response response)
        {
            if (string.IsNullOrWhiteSpace(response.Condition)) return true;
            var result = DialogueSystem.EvaluateCondition(response.Condition, _player, _npc);
            DebugConsole.Log($"EvaluateResponseCondition: '{response.Condition}' = {result} for NPC {_npc.ID} ({_npc.Name})");
            return result;
        }

        /// <summary>
        /// Выполняет действия ответа диалога
        /// </summary>
        private void ExecuteResponseActions(Response response)
        {
            if (response.Actions == null) return;

            foreach (var action in response.Actions)
            {
                // Специальные действия UI обрабатываем здесь
                switch (action.Type?.ToLower())
                {
                    case "starttrade":
                        OpenTrade();
                        break;
                    case "enddialogue":
                        CloseDialogue();
                        break;
                    default:
                        // Остальные действия обрабатываем в DialogueSystem
                        DialogueSystem.ExecuteAction(action, _player, _npc);
                        break;
                }
            }
        }

        #endregion

        #region --- MessageSystem positioning ---

        /// <summary>
        /// Переопределяем RenderOverlay для позиционирования MessageSystem ниже текста НПС
        /// </summary>
        public override void RenderOverlay()
        {
            int messageSystemStartY = CalculateMessageSystemPosition();
            RenderOverlay(messageSystemStartY);
        }

        /// <summary>
        /// Вычисляет позицию для MessageSystem на основе текущего содержимого диалога
        /// </summary>
        private int CalculateMessageSystemPosition()
        {
            // Начинаем с позиции после заголовка
            int y = 4; // Позиция после заголовка (headerHeight + 1)
            
            // Добавляем высоту текста диалога
            var text = _currentNode?.Text ?? _npc?.Greeting ?? "(пустой диалог)";
            var lines = WrapText(text, Math.Max(20, Width - 4));
            
            foreach (var line in lines)
            {
                if (y < Height - 10) // Проверяем границы экрана
                {
                    y++;
                }
            }
            
            // Добавляем отступ после текста диалога
            y += 2;
            
            // Добавляем высоту разделительной линии
            y += 1;
            
            // Добавляем высоту заголовка опций ("ВЫБЕРИТЕ ОТВЕТ:")
            y += 1;
            
            // Добавляем высоту всех доступных опций
            var responses = _currentNode?.Responses ?? new List<Response>();
            var availableResponses = responses.Where(r => EvaluateResponseCondition(r)).ToList();
            y += availableResponses.Count;
            
            // Добавляем небольшой отступ перед MessageSystem
            y += 1;
            
            // Ограничиваем позицию границами экрана
            return Math.Min(y, Height - 6); // Оставляем место для футера
        }

        #endregion
    }
}
