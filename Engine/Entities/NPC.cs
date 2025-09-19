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
            return $"Добро пожаловать в магазин {Name}!";
        }

        public virtual void Talk(Player player)
        {
            // Используем новую систему диалогов
            try
            {
                var dialogueScreen = new DialogueScreen(this, player, this.Trader);
                ScreenManager.PushScreen(dialogueScreen);
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"NPC.Talk: Не удалось создать DialogueScreen: {ex.Message}");
                // Fallback к простому приветствию
                DebugConsole.Log($"{Name}: {Greeting ?? "Привет!"}");
            }
        }

        /// <summary>
        /// Получает документ диалога для этого NPC
        /// </summary>
        public DialogueDocument GetDialogueDocument()
        {
            return ConvertToDialogueDocument();
        }

        /// <summary>
        /// Конвертирует данные NPC в DialogueDocument
        /// </summary>
        private DialogueDocument ConvertToDialogueDocument()
        {
            try
            {
                if (_worldRepository == null)
                {
                    DebugConsole.Log("NPC.ConvertToDialogueDocument: WorldRepository равен null");
                    return CreateDefaultDialogueDocument();
                }

                var gameData = _worldRepository.GetGameData();
                if (gameData?.Dialogues == null)
                {
                    DebugConsole.Log("NPC.ConvertToDialogueDocument: GameData или Dialogues равен null");
                    return CreateDefaultDialogueDocument();
                }

                // Ищем диалог для этого NPC по DefaultDialogueId
                var dialogueData = gameData.Dialogues.FirstOrDefault(d => d.Id == this.DefaultDialogueId);
                if (dialogueData == null)
                {
                    DebugConsole.Log($"NPC.ConvertToDialogueDocument: Диалог не найден для NPC {this.ID} с ID диалога {this.DefaultDialogueId}");
                    return CreateDefaultDialogueDocument();
                }

                // Конвертируем в новый формат
                var document = new DialogueDocument
                {
                    Id = dialogueData.Id,
                    Name = dialogueData.Name ?? $"Dialogue_{this.ID}",
                    Start = dialogueData.Start ?? "default",
                    Nodes = new List<DialogueNode>()
                };

                // Конвертируем узлы
                if (dialogueData.Nodes != null)
                {
                    foreach (var nodeData in dialogueData.Nodes)
                    {
                        var node = new DialogueNode
                        {
                            Id = nodeData.Id,
                            Text = nodeData.Text,
                            Type = nodeData.Type,
                            Tags = nodeData.Tags?.ToList() ?? new List<string>(),
                            Responses = new List<Response>()
                        };

                        // Конвертируем Choices в Responses
                        if (nodeData.Choices != null)
                        {
                            foreach (var choice in nodeData.Choices)
                            {
                                var response = new Response
                                {
                                    Text = choice.Text,
                                    Target = choice.NextNodeId,
                                    Condition = choice.Condition,
                                    Actions = new List<Engine.Dialogue.DialogueAction>()
                                };

                                // Конвертируем Actions
                                if (choice.Actions != null && choice.Actions.Count > 0)
                                {
                                    foreach (var actionData in choice.Actions)
                                    {
                                        response.Actions.Add(new Engine.Dialogue.DialogueAction
                                        {
                                            Type = GetActionTypeName(actionData.Type),
                                            Param = actionData.Parameter
                                        });
                                    }
                                }
                                // Также обрабатываем одиночное Action для обратной совместимости
                                else if (choice.Action != Engine.Data.DialogueAction.None)
                                {
                                    response.Actions.Add(new Engine.Dialogue.DialogueAction
                                    {
                                        Type = GetActionTypeName(choice.Action),
                                        Param = choice.ActionParameter
                                    });
                                }

                                node.Responses.Add(response);
                            }
                        }

                        document.Nodes.Add(node);
                    }
                }

                DebugConsole.Log($"NPC.ConvertToDialogueDocument: Диалог успешно конвертирован для NPC {this.ID}");
                
                // Инжектируем квестовые узлы через QuestDialogueManager
                try
                {
                    var questManager = GameServices.QuestManager;
                    if (questManager != null)
                    {
                        var questDialogueManager = questManager.GetQuestDialogueManager();
                        if (questDialogueManager != null)
                        {
                            questDialogueManager.InjectQuestNodesForNPC(this.ID, document);
                            DebugConsole.Log($"QuestDialogueManager: Инжектированы квестовые узлы для NPC {this.ID}");
                            
                            // Дополнительная диагностика для квестов
                            var worldRepo = GameServices.WorldRepository;
                            if (worldRepo != null)
                            {
                                var allGameData = worldRepo.GetGameData();
                                if (allGameData?.Quests != null)
                                {
                                    var questsForNPC = allGameData.Quests.Where(q => q.QuestGiverID == this.ID).ToList();
                                    DebugConsole.Log($"NPC {this.ID}: Найдено {questsForNPC.Count} квестов в GameData");
                                    foreach (var quest in questsForNPC)
                                    {
                                        DebugConsole.Log($"  Квест {quest.ID}: {quest.Name}, Состояние: {quest.State}");
                                    }
                                }
                            }
                        }
                        else
                        {
                            DebugConsole.Log($"QuestDialogueManager: GetQuestDialogueManager вернул null для NPC {this.ID}");
                        }
                    }
                    else
                    {
                        DebugConsole.Log($"QuestDialogueManager: QuestManager равен null для NPC {this.ID}");
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"QuestDialogueManager: Ошибка инжекции квестовых узлов для NPC {this.ID}: {ex.Message}");
                }
                
                return document;
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"NPC.ConvertToDialogueDocument не удался: {ex.Message}");
                return CreateDefaultDialogueDocument();
            }
        }

        /// <summary>
        /// Создает диалог по умолчанию
        /// </summary>
        private DialogueDocument CreateDefaultDialogueDocument()
        {
            var document = new DialogueDocument
            {
                Id = $"default_{this.ID}",
                Name = $"Default Dialogue for {this.Name}",
                Start = "greeting",
                Nodes = new List<DialogueNode>()
            };

            var greetingNode = new DialogueNode
            {
                Id = "greeting",
                Text = this.Greeting ?? $"{this.Name}: Привет!",
                Type = "greeting",
                Tags = new List<string> { "default" },
                Responses = new List<Response>()
            };

            // Добавляем базовые опции
            if (this.Trader != null || (this.ItemsForSale != null && this.ItemsForSale.Count > 0))
            {
                greetingNode.Responses.Add(new Response
                {
                    Text = "Покажи мне свои товары...",
                    Actions = new List<Engine.Dialogue.DialogueAction>
                    {
                        new Engine.Dialogue.DialogueAction { Type = "StartTrade" }
                    }
                });
            }

            greetingNode.Responses.Add(new Response
            {
                Text = "Я пойду.",
                Actions = new List<Engine.Dialogue.DialogueAction>
                {
                    new Engine.Dialogue.DialogueAction { Type = "EndDialogue" }
                }
            });

            document.Nodes.Add(greetingNode);
            
            // Инжектируем квестовые узлы через QuestDialogueManager
            try
            {
                var questManager = GameServices.QuestManager;
                if (questManager != null)
                {
                    var questDialogueManager = questManager.GetQuestDialogueManager();
                    if (questDialogueManager != null)
                    {
                        questDialogueManager.InjectQuestNodesForNPC(this.ID, document);
                        DebugConsole.Log($"QuestDialogueManager: Инжектированы квестовые узлы для NPC {this.ID} (диалог по умолчанию)");
                    }
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"QuestDialogueManager: Ошибка инжекции квестовых узлов для NPC {this.ID} (диалог по умолчанию): {ex.Message}");
            }
            
            return document;
        }

        /// <summary>
        /// Конвертирует числовой тип действия в строковое имя
        /// </summary>
        private string GetActionTypeName(Engine.Data.DialogueAction actionType)
        {
            switch (actionType)
            {
                case Engine.Data.DialogueAction.None: return "None";
                case Engine.Data.DialogueAction.StartQuest: return "StartQuest";
                case Engine.Data.DialogueAction.CompleteQuest: return "CompleteQuest";
                case Engine.Data.DialogueAction.GiveGold: return "GiveGold";
                case Engine.Data.DialogueAction.GiveItem: return "GiveItem";
                case Engine.Data.DialogueAction.SetFlag: return "SetFlag";
                case Engine.Data.DialogueAction.StartTrade: return "StartTrade";
                case Engine.Data.DialogueAction.EndDialogue: return "EndDialogue";
                default: return "None";
            }
        }

        // Реализация интерфейса IInteractable
        public List<string> GetAvailableActions(Player player)
        {
            var actions = new List<string> { "Поговорить", "Осмотреть" };
            
            // Добавляем торговлю, если NPC является торговцем
            if (this.Trader != null || (this.ItemsForSale != null && this.ItemsForSale.Count > 0))
            {
                actions.Add("Торговать");
            }
            
            return actions;
        }

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
                    if (this.Trader != null)
                    {
                        var tradeScreen = new TradeScreen(this.Trader, player);
                        ScreenManager.PushScreen(tradeScreen);
                    }
                    break;
                default:
                    DebugConsole.Log($"Неизвестное действие для NPC: {action}");
                    break;
            }
        }

        public virtual void Interact(Player player)
        {
            Talk(player);
        }

        public void Examine(Player player)
        {
            // Создаем экран осмотра NPC
            var examineScreen = new NPCInspectScreen(this, player);
            ScreenManager.PushScreen(examineScreen);
        }
    }
}