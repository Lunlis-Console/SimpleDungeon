using Engine.Core;
using Engine.Entities;
using Engine.Quests;
using Newtonsoft.Json;

namespace Engine.Dialogue
{
    /// <summary>
    /// Менеджер для управления диалогами в зависимости от состояния квестов
    /// </summary>
    public class QuestDialogueManager
    {
        private readonly QuestLog _questLog;

        public QuestDialogueManager(QuestLog questLog)
        {
            _questLog = questLog;
        }

        /// <summary>
        /// Получает подходящий узел диалога для NPC в зависимости от состояния квестов
        /// </summary>
        public string GetDialogueNodeForNPC(int npcID, DialogueDocument dialogue)
        {
            DebugConsole.Log($"GetDialogueNodeForNPC called for NPC {npcID}");
            
            // Отладочная информация о состоянии всех квестов
            DebugConsole.Log($"=== QUEST STATE DEBUG for NPC {npcID} ===");
            DebugConsole.Log($"AvailableQuests count: {_questLog.AvailableQuests.Count}");
            foreach (var q in _questLog.AvailableQuests)
            {
                DebugConsole.Log($"  Available: {q.Name} (ID: {q.ID}, State: {q.State}, QuestGiver: {q.QuestGiverID})");
            }
            DebugConsole.Log($"ActiveQuests count: {_questLog.ActiveQuests.Count}");
            foreach (var q in _questLog.ActiveQuests)
            {
                DebugConsole.Log($"  Active: {q.Name} (ID: {q.ID}, State: {q.State}, QuestGiver: {q.QuestGiverID})");
            }
            DebugConsole.Log($"CompletedQuests count: {_questLog.CompletedQuests.Count}");
            foreach (var q in _questLog.CompletedQuests)
            {
                DebugConsole.Log($"  Completed: {q.Name} (ID: {q.ID}, State: {q.State}, QuestGiver: {q.QuestGiverID})");
            }
            DebugConsole.Log($"=== END QUEST STATE DEBUG ===");
            
            // Проверяем доступные квесты
            var availableQuests = _questLog.GetAvailableQuestsForNPC(npcID);
            DebugConsole.Log($"Available quests for NPC {npcID}: {availableQuests.Count}");
            if (availableQuests.Any())
            {
                var quest = availableQuests.First();
                DebugConsole.Log($"First available quest: {quest.Name} (ID: {quest.ID})");
                if (!string.IsNullOrEmpty(quest.DialogueNodes.Offer))
                {
                    DebugConsole.Log($"Returning offer node: {quest.DialogueNodes.Offer}");
                    return quest.DialogueNodes.Offer;
                }
            }

            // Проверяем активные квесты
            var activeQuests = _questLog.GetActiveQuestsForNPC(npcID);
            DebugConsole.Log($"Active quests for NPC {npcID}: {activeQuests.Count}");
            if (activeQuests.Any())
            {
                var quest = activeQuests.First();
                DebugConsole.Log($"First active quest: {quest.Name} (ID: {quest.ID}, State: {quest.State})");
                
                // Если квест готов к завершению
                if (quest.State == QuestState.ReadyToComplete)
                {
                    DebugConsole.Log($"Quest {quest.ID} is ReadyToComplete, looking for node: {quest.DialogueNodes.ReadyToComplete}");
                    if (!string.IsNullOrEmpty(quest.DialogueNodes.ReadyToComplete))
                    {
                        // Проверяем, есть ли этот узел в диалоге
                        var nodeExists = dialogue.Nodes?.Any(n => n.Id == quest.DialogueNodes.ReadyToComplete) ?? false;
                        DebugConsole.Log($"Node {quest.DialogueNodes.ReadyToComplete} exists in dialogue: {nodeExists}");
                        
                        if (nodeExists)
                        {
                            DebugConsole.Log($"Returning ready to complete node: {quest.DialogueNodes.ReadyToComplete}");
                            return quest.DialogueNodes.ReadyToComplete;
                        }
                        else
                        {
                            DebugConsole.Log($"Node {quest.DialogueNodes.ReadyToComplete} not found in dialogue, available nodes: {string.Join(", ", dialogue.Nodes?.Select(n => n.Id) ?? new string[0])}");
                        }
                    }
                    else
                    {
                        DebugConsole.Log($"Quest {quest.ID} ReadyToComplete is null or empty");
                    }
                }
                // Если квест в процессе
                else if (quest.State == QuestState.InProgress)
                {
                    DebugConsole.Log($"Quest {quest.ID} is InProgress, looking for node: {quest.DialogueNodes.InProgress}");
                    if (!string.IsNullOrEmpty(quest.DialogueNodes.InProgress))
                    {
                        // Проверяем, есть ли этот узел в диалоге
                        var nodeExists = dialogue.Nodes?.Any(n => n.Id == quest.DialogueNodes.InProgress) ?? false;
                        DebugConsole.Log($"Node {quest.DialogueNodes.InProgress} exists in dialogue: {nodeExists}");
                        
                        if (nodeExists)
                        {
                            DebugConsole.Log($"Returning in progress node: {quest.DialogueNodes.InProgress}");
                            return quest.DialogueNodes.InProgress;
                        }
                        else
                        {
                            DebugConsole.Log($"Node {quest.DialogueNodes.InProgress} not found in dialogue, available nodes: {string.Join(", ", dialogue.Nodes?.Select(n => n.Id) ?? new string[0])}");
                        }
                    }
                    else
                    {
                        DebugConsole.Log($"Quest {quest.ID} InProgress is null or empty");
                    }
                }
            }

            // Проверяем завершенные квесты
            var completedQuests = _questLog.GetCompletedQuestsForNPC(npcID);
            DebugConsole.Log($"Completed quests for NPC {npcID}: {completedQuests.Count}");
            if (completedQuests.Any())
            {
                var quest = completedQuests.First();
                if (!string.IsNullOrEmpty(quest.DialogueNodes.Completed))
                {
                    DebugConsole.Log($"Returning completed node: {quest.DialogueNodes.Completed}");
                    return quest.DialogueNodes.Completed;
                }
            }

            // Возвращаем первый доступный узел диалога или стартовый узел
            if (dialogue.Nodes != null && dialogue.Nodes.Any())
            {
                // Ищем узел с ID "quest_5001_offer" как fallback
                var fallbackNode = dialogue.Nodes.FirstOrDefault(n => n.Id == "quest_5001_offer");
                if (fallbackNode != null)
                {
                    DebugConsole.Log($"Returning fallback quest offer node: quest_5001_offer");
                    return "quest_5001_offer";
                }
                
                // Если нет, возвращаем первый узел
                var firstNode = dialogue.Nodes.First();
                DebugConsole.Log($"Returning first available node: {firstNode.Id}");
                return firstNode.Id;
            }
            
            DebugConsole.Log($"No nodes available, returning dialogue start: {dialogue.Start}");
            return dialogue.Start;
        }

        /// <summary>
        /// Обрабатывает действия диалога, связанные с квестами
        /// </summary>
        public bool ProcessQuestAction(DialogueAction action, Player player)
        {
            switch (action.Type?.ToLower())
            {
                case "startquest":
                    return StartQuest(action.Param, player);
                
                case "completequest":
                    return CompleteQuest(action.Param, player);
                
                default:
                    return false;
            }
        }

        /// <summary>
        /// Начинает квест
        /// </summary>
        private bool StartQuest(string param, Player player)
        {
            if (int.TryParse(param, out int questID))
            {
                return _questLog.StartQuest(questID);
            }
            return false;
        }

        /// <summary>
        /// Завершает квест
        /// </summary>
        private bool CompleteQuest(string param, Player player)
        {
            if (int.TryParse(param, out int questID))
            {
                return _questLog.CompleteQuest(questID);
            }
            return false;
        }

        /// <summary>
        /// Создает динамический диалог для NPC с учетом состояния квестов
        /// </summary>
        public DialogueDocument CreateDynamicDialogue(int npcID, DialogueDocument baseDialogue)
        {
            var dynamicDialogue = JsonConvert.DeserializeObject<DialogueDocument>(
                JsonConvert.SerializeObject(baseDialogue));

            // Добавляем узлы для квестов, если их нет
            AddQuestDialogueNodes(npcID, dynamicDialogue);

            return dynamicDialogue;
        }

        /// <summary>
        /// Добавляет узлы диалогов для квестов
        /// </summary>
        private void AddQuestDialogueNodes(int npcID, DialogueDocument dialogue)
        {
            var availableQuests = _questLog.GetAvailableQuestsForNPC(npcID);
            var activeQuests = _questLog.GetActiveQuestsForNPC(npcID);
            var completedQuests = _questLog.GetCompletedQuestsForNPC(npcID);

            foreach (var quest in availableQuests)
            {
                if (!string.IsNullOrEmpty(quest.DialogueNodes.Offer))
                {
                    AddQuestOfferNode(dialogue, quest);
                }
            }

            foreach (var quest in activeQuests)
            {
                if (quest.State == QuestState.ReadyToComplete && 
                    !string.IsNullOrEmpty(quest.DialogueNodes.ReadyToComplete))
                {
                    AddQuestCompleteNode(dialogue, quest);
                }
                else if (quest.State == QuestState.InProgress && 
                         !string.IsNullOrEmpty(quest.DialogueNodes.InProgress))
                {
                    AddQuestInProgressNode(dialogue, quest);
                }
            }

            foreach (var quest in completedQuests)
            {
                if (!string.IsNullOrEmpty(quest.DialogueNodes.Completed))
                {
                    AddQuestCompletedNode(dialogue, quest);
                }
            }
        }

        private void AddQuestOfferNode(DialogueDocument dialogue, EnhancedQuest quest)
        {
            var offerNode = new DialogueNode
            {
                Id = quest.DialogueNodes.Offer,
                Text = $"У меня есть для тебя задание: {quest.Name}\n\n{quest.Description}",
                Responses = new List<Response>
                {
                    new Response
                    {
                        Text = "Принимаю задание!",
                        Target = "quest_accepted",
                        Actions = new List<DialogueAction>
                        {
                            new DialogueAction { Type = "StartQuest", Param = quest.ID.ToString() }
                        }
                    },
                    new Response
                    {
                        Text = "Мне нужно подумать...",
                        Target = dialogue.Start
                    }
                }
            };

            dialogue.Nodes.Add(offerNode);
        }

        private void AddQuestInProgressNode(DialogueDocument dialogue, EnhancedQuest quest)
        {
            var inProgressNode = new DialogueNode
            {
                Id = quest.DialogueNodes.InProgress,
                Text = $"Как дела с заданием '{quest.Name}'?\n\n{quest.GetProgressText()}",
                Responses = new List<Response>
                {
                    new Response
                    {
                        Text = "Я еще работаю над этим.",
                        Target = dialogue.Start
                    }
                }
            };

            dialogue.Nodes.Add(inProgressNode);
        }

        private void AddQuestCompleteNode(DialogueDocument dialogue, EnhancedQuest quest)
        {
            var completeNode = new DialogueNode
            {
                Id = quest.DialogueNodes.ReadyToComplete,
                Text = $"Отлично! Ты выполнил задание '{quest.Name}'!\n\nВот твоя награда:",
                Responses = new List<Response>
                {
                    new Response
                    {
                        Text = "Спасибо!",
                        Target = "quest_completed",
                        Actions = new List<DialogueAction>
                        {
                            new DialogueAction { Type = "CompleteQuest", Param = quest.ID.ToString() }
                        }
                    }
                }
            };

            dialogue.Nodes.Add(completeNode);
        }

        private void AddQuestCompletedNode(DialogueDocument dialogue, EnhancedQuest quest)
        {
            var completedNode = new DialogueNode
            {
                Id = quest.DialogueNodes.Completed,
                Text = $"Спасибо за выполнение задания '{quest.Name}'! Ты отлично справился.",
                Responses = new List<Response>
                {
                    new Response
                    {
                        Text = "Пожалуйста!",
                        Target = dialogue.Start
                    }
                }
            };

            dialogue.Nodes.Add(completedNode);
        }
    }
}
