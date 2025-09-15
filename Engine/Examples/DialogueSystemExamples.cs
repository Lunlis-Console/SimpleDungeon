using System;
using System.Collections.Generic;
using Engine.Dialogue;
using Engine.Data;
using Engine.Quests;
using Engine.Entities;
using Engine.World;

namespace Engine.Examples
{
    /// <summary>
    /// Примеры использования новой системы диалогов с дефолтными диалогами и квестовой инъекцией
    /// </summary>
    public static class DialogueSystemExamples
    {
        /// <summary>
        /// Пример создания NPC с дефолтным диалогом
        /// </summary>
        public static void CreateNPCWithDefaultDialogue()
        {
            Console.WriteLine("=== Пример создания NPC с дефолтным диалогом ===");
            
            // Создаем данные NPC
            var npcData = new NPCData
            {
                ID = 1,
                Name = "Торговец Гарольд",
                Greeting = "Добро пожаловать в мой магазин!",
                DefaultDialogueId = "merchant_harold_default"
            };
            
            Console.WriteLine($"Создан NPC: {npcData.Name}");
            Console.WriteLine($"DefaultDialogueId: {npcData.DefaultDialogueId}");
            
            // Создаем дефолтный диалог для торговца
            var defaultDialogue = new DialogueData
            {
                Id = "merchant_harold_default",
                Name = "Дефолтный диалог торговца",
                Start = "greeting",
                Nodes = new List<DialogueNodeData>
                {
                    new DialogueNodeData
                    {
                        Id = "greeting",
                        Text = "Добро пожаловать в мой магазин! Чем могу помочь?",
                        Type = "greeting",
                        Tags = new List<string> { "default", "main" },
                        Choices = new List<DialogueChoiceData>
                        {
                            new DialogueChoiceData
                            {
                                Text = "Посмотреть товары",
                                NextNodeId = "shop_menu"
                            },
                            new DialogueChoiceData
                            {
                                Text = "Есть ли у вас задания?",
                                NextNodeId = "quests_menu",
                                Condition = "hasAvailableQuests"
                            },
                            new DialogueChoiceData
                            {
                                Text = "Просто поболтать",
                                NextNodeId = "small_talk"
                            },
                            new DialogueChoiceData
                            {
                                Text = "До свидания",
                                NextNodeId = null // Завершение диалога
                            }
                        }
                    },
                    new DialogueNodeData
                    {
                        Id = "shop_menu",
                        Text = "Вот мои товары. Что вас интересует?",
                        Type = "shop",
                        Tags = new List<string> { "service" },
                        Choices = new List<DialogueChoiceData>
                        {
                            new DialogueChoiceData
                            {
                                Text = "Назад",
                                NextNodeId = "greeting"
                            }
                        }
                    },
                    new DialogueNodeData
                    {
                        Id = "small_talk",
                        Text = "Погода сегодня прекрасная, не правда ли?",
                        Type = "small_talk",
                        Tags = new List<string> { "casual" },
                        Choices = new List<DialogueChoiceData>
                        {
                            new DialogueChoiceData
                            {
                                Text = "Да, согласен",
                                NextNodeId = "greeting"
                            },
                            new DialogueChoiceData
                            {
                                Text = "Назад",
                                NextNodeId = "greeting"
                            }
                        }
                    }
                }
            };
            
            Console.WriteLine($"Создан дефолтный диалог с {defaultDialogue.Nodes.Count} узлами");
            Console.WriteLine($"Стартовый узел: {defaultDialogue.Start}");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Пример создания квеста с диалоговыми узлами
        /// </summary>
        public static void CreateQuestWithDialogueNodes()
        {
            Console.WriteLine("=== Пример создания квеста с диалоговыми узлами ===");
            
            // Создаем квест
            var quest = new EnhancedQuest
            {
                ID = 1,
                Name = "Доставка посылки",
                Description = "Отнесите посылку торговцу в соседнюю деревню",
                State = QuestState.NotStarted,
                DialogueNodes = new QuestDialogueNodes
                {
                    Offer = "quest_delivery_offer",
                    InProgress = "quest_delivery_progress",
                    ReadyToComplete = "quest_delivery_complete",
                    Completed = "quest_delivery_completed"
                }
            };
            
            Console.WriteLine($"Создан квест: {quest.Name}");
            Console.WriteLine($"Состояние: {quest.State}");
            Console.WriteLine($"Узел предложения: {quest.DialogueNodes.Offer}");
            Console.WriteLine($"Узел завершения: {quest.DialogueNodes.ReadyToComplete}");
            
            // Создаем диалоговые узлы для квеста
            var questDialogueNodes = new List<DialogueNodeData>
            {
                new DialogueNodeData
                {
                    Id = "quest_delivery_offer",
                    Text = "У меня есть для вас работа! Нужно отнести посылку торговцу в соседнюю деревню. Согласны?",
                    Type = "quest_offer",
                    Tags = new List<string> { "quest", "offer" },
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData
                        {
                            Text = "Согласен!",
                            NextNodeId = "quest_accepted",
                            Actions = new List<DialogueActionData>
                            {
                                new DialogueActionData
                                {
                                    Type = Engine.Data.DialogueAction.StartQuest,
                                    Parameter = "1"
                                }
                            }
                        },
                        new DialogueChoiceData
                        {
                            Text = "Может быть позже",
                            NextNodeId = "greeting"
                        }
                    }
                },
                new DialogueNodeData
                {
                    Id = "quest_delivery_progress",
                    Text = "Как дела с доставкой посылки?",
                    Type = "quest_progress",
                    Tags = new List<string> { "quest", "progress" },
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData
                        {
                            Text = "Еще в пути",
                            NextNodeId = "greeting"
                        }
                    }
                },
                new DialogueNodeData
                {
                    Id = "quest_delivery_complete",
                    Text = "Отлично! Вы доставили посылку. Вот ваша награда!",
                    Type = "quest_complete",
                    Tags = new List<string> { "quest", "complete" },
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData
                        {
                            Text = "Спасибо!",
                            NextNodeId = "quest_completed",
                            Actions = new List<DialogueActionData>
                            {
                                new DialogueActionData
                                {
                                    Type = Engine.Data.DialogueAction.CompleteQuest,
                                    Parameter = "1"
                                }
                            }
                        }
                    }
                },
                new DialogueNodeData
                {
                    Id = "quest_delivery_completed",
                    Text = "Спасибо за помощь! Если будут еще задания, я дам знать.",
                    Type = "quest_completed",
                    Tags = new List<string> { "quest", "completed" },
                    Choices = new List<DialogueChoiceData>
                    {
                        new DialogueChoiceData
                        {
                            Text = "Хорошо",
                            NextNodeId = "greeting"
                        }
                    }
                }
            };
            
            Console.WriteLine($"Создано {questDialogueNodes.Count} диалоговых узлов для квеста");
            
            Console.WriteLine();
        }

        /// <summary>
        /// Пример использования QuestDialogueManager для инъекции квестовых узлов
        /// </summary>
        public static void ExampleQuestInjection()
        {
            Console.WriteLine("=== Пример инъекции квестовых узлов ===");
            
            // Создаем базовый диалог
            var baseDialogue = new DialogueDocument
            {
                Id = "merchant_base",
                Name = "Базовый диалог торговца",
                Start = "greeting",
                Nodes = new List<DialogueNode>
                {
                    new DialogueNode
                    {
                        Id = "greeting",
                        Text = "Добро пожаловать! Чем могу помочь?",
                        Type = "greeting",
                        Tags = new List<string> { "default" },
                        Responses = new List<Response>
                        {
                            new Response
                            {
                                Text = "Посмотреть товары",
                                Target = "shop_menu"
                            },
                            new Response
                            {
                                Text = "Поболтать",
                                Target = "small_talk"
                            }
                        }
                    },
                    new DialogueNode
                    {
                        Id = "shop_menu",
                        Text = "Вот мои товары",
                        Type = "shop",
                        Tags = new List<string> { "service" },
                        Responses = new List<Response>
                        {
                            new Response
                            {
                                Text = "Назад",
                                Target = "greeting"
                            }
                        }
                    },
                    new DialogueNode
                    {
                        Id = "small_talk",
                        Text = "Погода хорошая сегодня",
                        Type = "small_talk",
                        Tags = new List<string> { "casual" },
                        Responses = new List<Response>
                        {
                            new Response
                            {
                                Text = "Назад",
                                Target = "greeting"
                            }
                        }
                    }
                }
            };
            
            Console.WriteLine($"Базовый диалог содержит {baseDialogue.Nodes.Count} узлов");
            
            // Создаем QuestDialogueManager
            var worldRepo = new JsonWorldRepository("Data/test_data.json");
            var player = new Player("TestPlayer", 100, 100, 100, 0, 100, 1, 10, 5, 5, worldRepo);
            var questLog = new QuestLog(player);
            var questManager = new QuestDialogueManager(questLog);
            
            // Инъекция квестовых узлов
            var forcedStart = questManager.InjectQuestNodesForNPC(1, baseDialogue, autoOverrideStart: false);
            
            Console.WriteLine($"После инъекции диалог содержит {baseDialogue.Nodes.Count} узлов");
            Console.WriteLine($"Принудительный старт: {forcedStart ?? "нет"}");
            
            // Показываем обновленный стартовый узел
            var startNode = baseDialogue.Nodes.FirstOrDefault(n => n.Id == "greeting");
            if (startNode != null)
            {
                Console.WriteLine($"Стартовый узел теперь содержит {startNode.Responses.Count} ответов:");
                foreach (var response in startNode.Responses)
                {
                    Console.WriteLine($"  - {response.Text} -> {response.Target}");
                }
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Запуск всех примеров
        /// </summary>
        public static void RunAllExamples()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("ПРИМЕРЫ ИСПОЛЬЗОВАНИЯ СИСТЕМЫ ДИАЛОГОВ");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            
            try
            {
                CreateNPCWithDefaultDialogue();
                CreateQuestWithDialogueNodes();
                ExampleQuestInjection();
                
                Console.WriteLine("==========================================");
                Console.WriteLine("ВСЕ ПРИМЕРЫ ЗАВЕРШЕНЫ");
                Console.WriteLine("==========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении примеров: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
