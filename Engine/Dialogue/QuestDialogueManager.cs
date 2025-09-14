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
            // Проверяем доступные квесты
            var availableQuests = _questLog.GetAvailableQuestsForNPC(npcID);
            if (availableQuests.Any())
            {
                var quest = availableQuests.First();
                if (!string.IsNullOrEmpty(quest.DialogueNodes.OfferNodeID))
                {
                    return quest.DialogueNodes.OfferNodeID;
                }
            }

            // Проверяем активные квесты
            var activeQuests = _questLog.GetActiveQuestsForNPC(npcID);
            if (activeQuests.Any())
            {
                var quest = activeQuests.First();
                
                // Если квест готов к завершению
                if (quest.State == QuestState.ReadyToComplete)
                {
                    if (!string.IsNullOrEmpty(quest.DialogueNodes.ReadyToCompleteNodeID))
                    {
                        return quest.DialogueNodes.ReadyToCompleteNodeID;
                    }
                }
                // Если квест в процессе
                else if (quest.State == QuestState.InProgress)
                {
                    if (!string.IsNullOrEmpty(quest.DialogueNodes.InProgressNodeID))
                    {
                        return quest.DialogueNodes.InProgressNodeID;
                    }
                }
            }

            // Проверяем завершенные квесты
            var completedQuests = _questLog.GetCompletedQuestsForNPC(npcID);
            if (completedQuests.Any())
            {
                var quest = completedQuests.First();
                if (!string.IsNullOrEmpty(quest.DialogueNodes.CompletedNodeID))
                {
                    return quest.DialogueNodes.CompletedNodeID;
                }
            }

            // Возвращаем стандартный стартовый узел
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
                if (!string.IsNullOrEmpty(quest.DialogueNodes.OfferNodeID))
                {
                    AddQuestOfferNode(dialogue, quest);
                }
            }

            foreach (var quest in activeQuests)
            {
                if (quest.State == QuestState.ReadyToComplete && 
                    !string.IsNullOrEmpty(quest.DialogueNodes.ReadyToCompleteNodeID))
                {
                    AddQuestCompleteNode(dialogue, quest);
                }
                else if (quest.State == QuestState.InProgress && 
                         !string.IsNullOrEmpty(quest.DialogueNodes.InProgressNodeID))
                {
                    AddQuestInProgressNode(dialogue, quest);
                }
            }

            foreach (var quest in completedQuests)
            {
                if (!string.IsNullOrEmpty(quest.DialogueNodes.CompletedNodeID))
                {
                    AddQuestCompletedNode(dialogue, quest);
                }
            }
        }

        private void AddQuestOfferNode(DialogueDocument dialogue, EnhancedQuest quest)
        {
            var offerNode = new DialogueNode
            {
                Id = quest.DialogueNodes.OfferNodeID,
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
                Id = quest.DialogueNodes.InProgressNodeID,
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
                Id = quest.DialogueNodes.ReadyToCompleteNodeID,
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
                Id = quest.DialogueNodes.CompletedNodeID,
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
