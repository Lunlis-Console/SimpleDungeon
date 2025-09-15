using Engine.Core;
using Engine.Data;
using Engine.Dialogue;
using Engine.Quests;
using Engine.Trading;
using Engine.UI;
using Engine.World;
using System.Linq;

namespace Engine.Entities
{
    public class NPC : IInteractable
    {
        public int ID { get; set; }
        public string Name { get; set; }
        public string Greeting { get; set; }
        public List<int> QuestsToGive { get; set; }
        public virtual List<InventoryItem> ItemsForSale { get; set; }
        public virtual int Gold { get; set; }
        public virtual int BuyPriceModifier => 100; // 100% по умолчанию
        public virtual int SellPriceModifier => 80; // 80% по умолчанию
        public ITrader Trader { get; set; } // Добавляем это свойство
        public DialogueSystem.DialogueNode GreetingDialogue { get; set; }
        public string DefaultDialogueId { get; set; }

        private readonly IWorldRepository _worldRepository;

        public NPC(int id, string name, string greeting = "", IWorldRepository worldRepository = null)
        {
            ID = id;
            Name = name;
            Greeting = greeting;
            QuestsToGive = new List<int>();
            _worldRepository = worldRepository;
        }

        // Новый метод для инициализации магазина
        public virtual void InitializeShop(Player player)
        {
            // Базовая реализация - может быть переопределена
            ItemsForSale = new List<InventoryItem>();
        }

        public virtual bool CanAfford(Item item, int quantity, Player player)
        {
            return player.Gold >= item.Price * BuyPriceModifier / 100 * quantity;
        }

        public virtual string GetShopGreeting()
        {
            return $"{Name}: Что желаете приобрести?";
        }

        // В методе GetAvailableActions добавьте:
        private class MenuOption
        {
            public string DisplayText { get; }
            public Action Action { get; }

            public MenuOption(string displayText, Action action)
            {
                DisplayText = displayText;
                Action = action;
            }
        }

        // Новый метод Talk - просто начинает разговор
        public virtual void Talk(Player player)
        {
            if (GreetingDialogue != null)
            {
                // Создаём DialogueScreen, передаём player и, если есть, заранее подготовленного трейдера.
                // Это соответствует конструктору DialogueScreen(NPC npc, Player player, ITrader traderForDialogue = null)
                var dialogueScreen = new DialogueScreen(this, player, this.Trader);

                // Динамически выбираем правильный узел диалога в зависимости от состояния квестов
                var appropriateNode = GetAppropriateDialogueNode(player);
                if (appropriateNode != null)
                {
                    try
                    {
                        dialogueScreen.SetCurrentNode(appropriateNode);
                        DebugConsole.Log($"Set dialogue node for NPC {ID}: {appropriateNode.Text?.Substring(0, Math.Min(50, appropriateNode.Text?.Length ?? 0))}...");
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log($"NPC.Talk: SetCurrentNode failed: {ex.Message}");
                        // Fallback к оригинальному узлу
                        dialogueScreen.SetCurrentNode(GreetingDialogue);
                    }
                }
                else
                {
                    // Fallback к оригинальному узлу
                    dialogueScreen.SetCurrentNode(GreetingDialogue);
                }

                ScreenManager.PushScreen(dialogueScreen);
            }
            else
            {
                // Fallback к старой системе
                HaveConversation(player);
            }
        }

        /// <summary>
        /// Получает подходящий узел диалога в зависимости от состояния квестов
        /// </summary>
        private DialogueSystem.DialogueNode GetAppropriateDialogueNode(Player player)
        {
            try
            {
                DebugConsole.Log($"CurrentPlayer is null: {GameServices.CurrentPlayer == null}");
                DebugConsole.Log($"QuestManager is null: {GameServices.QuestManager == null}");
                
                var questDialogueManager = GameServices.QuestManager?.GetQuestDialogueManager();
                if (questDialogueManager == null)
                {
                    DebugConsole.Log("QuestDialogueManager is null, using original dialogue node");
                    return GreetingDialogue;
                }

                // Используем новый API для инъекции квестовых узлов
                var dialogueDocument = ConvertToDialogueDocument();
                var forcedNodeId = questDialogueManager.InjectQuestNodesForNPC(ID, dialogueDocument, autoOverrideStart: false);

                DebugConsole.Log($"InjectQuestNodesForNPC returned forced node: '{forcedNodeId}'");

                // Если есть принудительный старт, используем его
                if (!string.IsNullOrEmpty(forcedNodeId))
                {
                    var forcedNode = CreateDialogueNodeFromId(forcedNodeId);
                    if (forcedNode != null)
                    {
                        DebugConsole.Log($"Using forced start node: {forcedNodeId}");
                        return forcedNode;
                    }
                }

                // Иначе используем дефолтный стартовый узел
                var defaultNodeId = dialogueDocument.Start;
                if (!string.IsNullOrEmpty(defaultNodeId))
                {
                    var defaultNode = CreateDialogueNodeFromId(defaultNodeId);
                    if (defaultNode != null)
                    {
                        DebugConsole.Log($"Using default start node: {defaultNodeId}");
                        return defaultNode;
                    }
                }

                DebugConsole.Log("Failed to create any node, using original");
                return GreetingDialogue;
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"GetAppropriateDialogueNode failed: {ex.Message}");
                return GreetingDialogue;
            }
        }

        /// <summary>
        /// Создает узел диалога на основе ID из game_data.json
        /// </summary>
        private DialogueSystem.DialogueNode CreateDialogueNodeFromId(string nodeId)
        {
            try
            {
                var worldRepo = _worldRepository;
                if (worldRepo != null)
                {
                    var gameData = worldRepo.GetGameData();
                    if (gameData?.Dialogues != null)
                    {
                        var dialogueData = gameData.Dialogues.FirstOrDefault(d => d.Id == "7001");
                        if (dialogueData != null)
                        {
                            var nodeData = dialogueData.Nodes.FirstOrDefault(n => n.Id == nodeId);
                            if (nodeData != null)
                            {
                                DebugConsole.Log($"CreateDialogueNodeFromId: Found node data for ID '{nodeId}'");
                                
                                var node = new DialogueSystem.DialogueNode(nodeData.Id, nodeData.Text ?? string.Empty);
                                
                                if (nodeData.Choices != null)
                                {
                                    foreach (var choice in nodeData.Choices)
                                    {
                                        if (choice == null) continue;

                                        DialogueSystem.DialogueNode nextNode = null;
                                        if (!string.IsNullOrEmpty(choice.NextNodeId))
                                        {
                                            // Создаем следующий узел рекурсивно
                                            nextNode = CreateDialogueNodeFromId(choice.NextNodeId);
                                        }

                                        var option = new DialogueSystem.DialogueOption(choice.Text ?? string.Empty, nextNode);

                                        // Копируем действия
                                        if (choice.Actions != null && choice.Actions.Count > 0)
                                        {
                                            option.Actions = choice.Actions.Select(a => new DialogueActionData
                                            {
                                                Type = a.Type,
                                                Parameter = a.Parameter
                                            }).ToList();
                                        }
                                        // Для обратной совместимости с одиночными действиями
                                        else if (choice.Action != Engine.Data.DialogueAction.None)
                                        {
                                            option.Actions.Add(new DialogueActionData
                                            {
                                                Type = choice.Action,
                                                Parameter = choice.ActionParameter
                                            });
                                        }

                                        node.Options.Add(option);
                                    }
                                }

                                return node;
                            }
                            else
                            {
                                DebugConsole.Log($"CreateDialogueNodeFromId: Node data not found for ID '{nodeId}'");
                            }
                        }
                        else
                        {
                            DebugConsole.Log("CreateDialogueNodeFromId: Dialogue data not found for ID 7001");
                        }
                    }
                    else
                    {
                        DebugConsole.Log("CreateDialogueNodeFromId: GameData or Dialogues is null");
                    }
                }
                else
                {
                    DebugConsole.Log("CreateDialogueNodeFromId: WorldRepository is null");
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"CreateDialogueNodeFromId failed: {ex.Message}");
            }

            return null;
        }

        /// <summary>
        /// Получает все узлы диалога рекурсивно
        /// </summary>
        private List<DialogueSystem.DialogueNode> GetAllDialogueNodes(DialogueSystem.DialogueNode startNode)
        {
            var nodes = new List<DialogueSystem.DialogueNode>();
            var visited = new HashSet<DialogueSystem.DialogueNode>();

            void CollectNodes(DialogueSystem.DialogueNode node)
            {
                if (node == null || visited.Contains(node)) return;
                
                visited.Add(node);
                nodes.Add(node);

                if (node.Options != null)
                {
                    foreach (var option in node.Options)
                    {
                        if (option?.NextNode != null)
                        {
                            CollectNodes(option.NextNode);
                        }
                    }
                }
            }

            CollectNodes(startNode);
            return nodes;
        }

        /// <summary>
        /// Получает ID узла диалога
        /// </summary>
        private string GetNodeId(DialogueSystem.DialogueNode node)
        {
            if (node == null)
            {
                DebugConsole.Log("GetNodeId: node is null");
                return "null_node";
            }

            // Теперь у узла есть поле Id, просто возвращаем его
            if (string.IsNullOrEmpty(node.Id))
            {
                DebugConsole.Log($"GetNodeId: node has no ID, text: '{node.Text?.Substring(0, Math.Min(50, node.Text?.Length ?? 0))}...'");
                return "unknown";
            }

            return node.Id;
        }

        /// <summary>
        /// Конвертирует диалог NPC в DialogueDocument для QuestDialogueManager
        /// </summary>
        private DialogueDocument ConvertToDialogueDocument()
        {
            var document = new DialogueDocument
            {
                Id = DefaultDialogueId ?? "7001", // Используем DefaultDialogueId или fallback
                Name = $"Диалог {Name}",
                Start = "default_start", // Временный стартовый узел, будет заменен QuestDialogueManager
                Nodes = new List<DialogueNode>()
            };

            // Загружаем все узлы диалога из game_data.json напрямую
            var worldRepo = _worldRepository;
            if (worldRepo != null)
            {
                var gameData = worldRepo.GetGameData();
                if (gameData?.Dialogues != null)
                {
                    var dialogueId = DefaultDialogueId ?? "7001";
                    var dialogueData = gameData.Dialogues.FirstOrDefault(d => d.Id == dialogueId);
                    if (dialogueData != null)
                    {
                        DebugConsole.Log($"ConvertToDialogueDocument: Found dialogue data with {dialogueData.Nodes.Count} nodes");
                        
                        // Устанавливаем правильный стартовый узел
                        document.Start = dialogueData.Start ?? (dialogueData.Nodes.Count > 0 ? dialogueData.Nodes[0].Id : "default_start");
                        
                        foreach (var nodeData in dialogueData.Nodes)
                        {
                            if (nodeData == null) continue;

                            DebugConsole.Log($"ConvertToDialogueDocument: Adding node with ID '{nodeData.Id}' and text '{nodeData.Text?.Substring(0, Math.Min(30, nodeData.Text?.Length ?? 0))}...'");

                            var dialogueNode = new DialogueNode
                            {
                                Id = nodeData.Id,
                                Text = nodeData.Text,
                                Type = nodeData.Type,
                                Tags = nodeData.Tags ?? new List<string>(),
                                Responses = new List<Response>()
                            };

                            if (nodeData.Choices != null)
                            {
                                foreach (var choice in nodeData.Choices)
                                {
                                    if (choice == null) continue;

                                    var response = new Response
                                    {
                                        Text = choice.Text,
                                        Target = choice.NextNodeId,
                                        Condition = choice.Condition,
                                        Actions = new List<Engine.Dialogue.DialogueAction>()
                                    };

                                    if (choice.Actions != null)
                                    {
                                        foreach (var action in choice.Actions)
                                        {
                                            if (action == null) continue;

                                            response.Actions.Add(new Engine.Dialogue.DialogueAction
                                            {
                                                Type = action.Type.ToString(),
                                                Param = action.Parameter
                                            });
                                        }
                                    }

                                    dialogueNode.Responses.Add(response);
                                }
                            }

                            document.Nodes.Add(dialogueNode);
                        }
                    }
                    else
                    {
                        DebugConsole.Log($"ConvertToDialogueDocument: Dialogue data not found for ID {dialogueId}");
                    }
                }
                else
                {
                    DebugConsole.Log("ConvertToDialogueDocument: GameData or Dialogues is null");
                }
            }
            else
            {
                DebugConsole.Log("ConvertToDialogueDocument: WorldRepository is null");
            }

            DebugConsole.Log($"ConvertToDialogueDocument: Final document has {document.Nodes.Count} nodes, start: {document.Start}");
            return document;
        }

        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Поговорить" };

            // Автоматически добавляем торговлю если NPC - торговец
            if (Trader != null)
            {
                actions.Add("Торговать");
            }

            // Автоматически добавляем квесты если они доступны
            if (HasAvailableQuests(player) || HasCompletableQuests(player))
            {
                actions.Add("Задания");
            }

            actions.Add("Осмотреть");
            return actions;
        }
        private bool HasAvailableQuests(Player player)
        {
            return QuestsToGive?.Any(questID =>
                !player.QuestLog.ActiveQuests.Any(q => q.ID == questID) &&
                !player.QuestLog.CompletedQuests.Any(q => q.ID == questID)) ?? false;
        }

        private bool HasCompletableQuests(Player player)
        {
            return QuestsToGive?.Any(questID =>
                player.QuestLog.ActiveQuests.Any(q => q.ID == questID && q.State == QuestState.ReadyToComplete)) ?? false;
        }

        protected virtual void HaveConversation(Player player)
        {
            Console.Clear();
            Console.WriteLine($"╔{new string('═', Name.Length + 4)}╗");
            Console.WriteLine($"║  {Name}  ║");
            Console.WriteLine($"╚{new string('═', Name.Length + 4)}╝");
            Console.WriteLine();
            Console.WriteLine(Greeting);
            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            Console.ReadKey(true);
        }


        public void AddQuest(int questID)
        {
            if (QuestsToGive == null)
            {
                QuestsToGive = new List<int>();
            }

            if (!QuestsToGive.Contains(questID))
            {
                QuestsToGive.Add(questID);
            }
        }

        // В методе ExecuteAction добавьте:
        public void ExecuteAction(Player player, string action)
        {
            switch (action)
            {
                case "Поговорить":
                    Talk(player);
                    break;
                case "Осмотреть":
                    Examine(player);
                    break;
                case "Торговать":
                    if (Trader != null)
                    {
                        // подготовка ассортимента (как было)
                        Trader.InitializeShop(player);

                        // Открываем новый экран торговли через ScreenManager
                        var tradeScreen = new Engine.UI.TradeScreen(Trader, player);
                        ScreenManager.PushScreen(tradeScreen);
                        // ScreenManager.PushScreen уже вызывает RequestFullRedraw() внутри себя,
                        // поэтому дополнительный вызов чаще не обязателен.
                    }
                    break;
                case "Квесты":
                    ShowQuestMenu(player);
                    break;
            }
        }

        private void ShowQuestMenu(Player player)
        {
            // Логика меню квестов (перенести из старого Talk метода)
        }

        // Новый метод для осмотра NPC
        public virtual void Examine(Player player)
        {
            Console.Clear();
            Console.WriteLine($"============ ОСМОТР: {Name} ============");
            Console.WriteLine("Это житель деревни.");

            // Информация о квестах
            if (QuestsToGive?.Count > 0)
            {
                Console.WriteLine();
                Console.WriteLine("Возможные задания:");

                foreach (var questID in QuestsToGive)
                {
                    var quest = player.QuestLog.GetQuest(questID);
                    if (quest == null) continue;

                    string status = "❓"; // Доступен
                    if (player.QuestLog.ActiveQuests.Any(q => q.ID == questID))
                        status = quest.State == QuestState.ReadyToComplete ? "✅" : "⏳";
                    else if (player.QuestLog.CompletedQuests.Any(q => q.ID == questID))
                        status = "✔️";

                    Console.WriteLine($" {status} {quest.Name}");
                }
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу чтобы продолжить...");
            Console.ReadKey(true);
        }
    }
}