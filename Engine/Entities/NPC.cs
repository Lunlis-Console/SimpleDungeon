using Engine.Core;
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

                // Получаем все узлы диалога из GreetingDialogue
                var allNodes = GetAllDialogueNodes(GreetingDialogue);
                var nodeMap = allNodes.ToDictionary(n => GetNodeId(n), n => n);

                DebugConsole.Log($"Found {nodeMap.Count} dialogue nodes for NPC {ID}");

                // Используем QuestDialogueManager для определения правильного узла
                var dialogueDocument = ConvertToDialogueDocument();
                var appropriateNodeId = questDialogueManager.GetDialogueNodeForNPC(ID, dialogueDocument);

                DebugConsole.Log($"QuestDialogueManager selected node ID: '{appropriateNodeId}'");

                if (!string.IsNullOrEmpty(appropriateNodeId) && nodeMap.ContainsKey(appropriateNodeId))
                {
                    var selectedNode = nodeMap[appropriateNodeId];
                    DebugConsole.Log($"Found appropriate node: {appropriateNodeId}");
                    return selectedNode;
                }
                else
                {
                    DebugConsole.Log($"Node '{appropriateNodeId}' not found in dialogue, using original");
                    return GreetingDialogue;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"GetAppropriateDialogueNode failed: {ex.Message}");
                return GreetingDialogue;
            }
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
                Id = "7001", // ID диалога Старосты Федота
                Name = "Диалог Старосты Федота",
                Start = "quest_5001_offer",
                Nodes = new List<DialogueNode>()
            };

            // Добавляем узлы из GreetingDialogue
            var allNodes = GetAllDialogueNodes(GreetingDialogue);
            DebugConsole.Log($"ConvertToDialogueDocument: Found {allNodes.Count} nodes from GreetingDialogue");
            
            foreach (var node in allNodes)
            {
                if (node == null) continue;

                var nodeId = GetNodeId(node);
                DebugConsole.Log($"ConvertToDialogueDocument: Adding node with ID '{nodeId}' and text '{node.Text?.Substring(0, Math.Min(30, node.Text?.Length ?? 0))}...'");

                var dialogueNode = new DialogueNode
                {
                    Id = nodeId,
                    Text = node.Text,
                    Responses = new List<Response>()
                };

                if (node.Options != null)
                {
                    foreach (var option in node.Options)
                    {
                        if (option == null) continue;

                        var response = new Response
                        {
                            Text = option.Text,
                            Target = GetNodeId(option.NextNode),
                            Actions = new List<DialogueAction>()
                        };

                        if (option.Actions != null)
                        {
                            foreach (var action in option.Actions)
                            {
                                if (action == null) continue;

                                response.Actions.Add(new DialogueAction
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