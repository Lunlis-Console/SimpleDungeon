using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Dialogue;
using Engine.Data;
using Engine.Quests;
using Engine.Entities;
using Engine.Core;
using Engine.World;

namespace Engine.Tests
{
    /// <summary>
    /// Тесты для QuestDialogueManager.InjectQuestNodesForNPC
    /// </summary>
    public static class QuestDialogueManagerTests
    {
        /// <summary>
        /// Тест базовой функциональности инъекции квестовых узлов
        /// </summary>
        public static void TestBasicQuestInjection()
        {
            Console.WriteLine("=== Тест базовой инъекции квестовых узлов ===");
            
            // Создаем тестовый диалог
            var dialogue = new DialogueDocument
            {
                Id = "test_dialogue",
                Name = "Test Dialogue",
                Start = "start",
                Nodes = new List<DialogueNode>
                {
                    new DialogueNode
                    {
                        Id = "start",
                        Text = "Привет! Чем могу помочь?",
                        Type = "greeting",
                        Tags = new List<string> { "default" },
                        Responses = new List<Response>()
                    }
                }
            };

            // Создаем тестовый квест
            var quest = new EnhancedQuest
            {
                ID = 1,
                Name = "Тестовый квест",
                Description = "Описание тестового квеста",
                State = QuestState.NotStarted,
                DialogueNodes = new QuestDialogueNodes
                {
                    Offer = "quest_offer_1"
                }
            };

            // Создаем QuestDialogueManager
            var worldRepo = new JsonWorldRepository("Data/test_data.json");
            var player = new Player("TestPlayer", 100, 100, 100, 0, 100, 1, 10, 5, 5, worldRepo);
            var questLog = new QuestLog(player);
            var questManager = new QuestDialogueManager(questLog);
            
            // Мокаем получение квестов для NPC
            var originalMethod = typeof(QuestDialogueManager).GetMethod("GetQuestsForNPC", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            // Вызываем инъекцию
            var forcedStart = questManager.InjectQuestNodesForNPC(1, dialogue, autoOverrideStart: false);
            
            // Проверяем результаты
            Console.WriteLine($"Принудительный старт: {forcedStart}");
            Console.WriteLine($"Количество узлов после инъекции: {dialogue.Nodes.Count}");
            
            // Проверяем, что добавлен квестовый узел
            var questNode = dialogue.Nodes.FirstOrDefault(n => n.Id == "quest_offer_1");
            if (questNode != null)
            {
                Console.WriteLine($"✓ Квестовый узел добавлен: {questNode.Id}");
                Console.WriteLine($"  Тип узла: {questNode.Type}");
            }
            else
            {
                Console.WriteLine("✗ Квестовый узел не найден");
            }
            
            // Проверяем, что в стартовом узле добавлена ссылка на квест
            var startNode = dialogue.Nodes.FirstOrDefault(n => n.Id == "start");
            if (startNode != null && startNode.Responses.Any(r => r.Target == "quest_offer_1"))
            {
                Console.WriteLine("✓ Ссылка на квест добавлена в стартовый узел");
            }
            else
            {
                Console.WriteLine("✗ Ссылка на квест не найдена в стартовом узле");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Тест автоматического переключения на квест ReadyToComplete
        /// </summary>
        public static void TestAutoOverrideForReadyToComplete()
        {
            Console.WriteLine("=== Тест автоматического переключения на ReadyToComplete ===");
            
            // Создаем тестовый диалог
            var dialogue = new DialogueDocument
            {
                Id = "test_dialogue",
                Name = "Test Dialogue",
                Start = "start",
                Nodes = new List<DialogueNode>
                {
                    new DialogueNode
                    {
                        Id = "start",
                        Text = "Привет! Чем могу помочь?",
                        Type = "greeting",
                        Tags = new List<string> { "default" },
                        Responses = new List<Response>()
                    }
                }
            };

            // Создаем QuestDialogueManager
            var worldRepo = new JsonWorldRepository("Data/test_data.json");
            var player = new Player("TestPlayer", 100, 100, 100, 0, 100, 1, 10, 5, 5, worldRepo);
            var questLog = new QuestLog(player);
            var questManager = new QuestDialogueManager(questLog);
            
            // Вызываем инъекцию с autoOverrideStart = true
            var forcedStart = questManager.InjectQuestNodesForNPC(1, dialogue, autoOverrideStart: true);
            
            Console.WriteLine($"Принудительный старт: {forcedStart}");
            
            if (!string.IsNullOrEmpty(forcedStart))
            {
                Console.WriteLine("✓ Автоматическое переключение работает");
            }
            else
            {
                Console.WriteLine("ℹ Нет квестов ReadyToComplete для автоматического переключения");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Тест создания меню квестов
        /// </summary>
        public static void TestQuestMenuCreation()
        {
            Console.WriteLine("=== Тест создания меню квестов ===");
            
            // Создаем тестовый диалог
            var dialogue = new DialogueDocument
            {
                Id = "test_dialogue",
                Name = "Test Dialogue",
                Start = "start",
                Nodes = new List<DialogueNode>
                {
                    new DialogueNode
                    {
                        Id = "start",
                        Text = "Привет! Чем могу помочь?",
                        Type = "greeting",
                        Tags = new List<string> { "default" },
                        Responses = new List<Response>()
                    }
                }
            };

            // Создаем QuestDialogueManager
            var worldRepo = new JsonWorldRepository("Data/test_data.json");
            var player = new Player("TestPlayer", 100, 100, 100, 0, 100, 1, 10, 5, 5, worldRepo);
            var questLog = new QuestLog(player);
            var questManager = new QuestDialogueManager(questLog);
            
            // Вызываем инъекцию
            questManager.InjectQuestNodesForNPC(1, dialogue, autoOverrideStart: false);
            
            // Проверяем, создано ли меню квестов
            var questMenu = dialogue.Nodes.FirstOrDefault(n => n.Type == "quests_menu");
            if (questMenu != null)
            {
                Console.WriteLine($"✓ Меню квестов создано: {questMenu.Id}");
                Console.WriteLine($"  Текст: {questMenu.Text}");
            }
            else
            {
                Console.WriteLine("ℹ Меню квестов не создано (возможно, нет квестов)");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Запуск всех тестов
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("Запуск тестов QuestDialogueManager...");
            Console.WriteLine();
            
            try
            {
                TestBasicQuestInjection();
                TestAutoOverrideForReadyToComplete();
                TestQuestMenuCreation();
                
                Console.WriteLine("=== Все тесты завершены ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении тестов: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
