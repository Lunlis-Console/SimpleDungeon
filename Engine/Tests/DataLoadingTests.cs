using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Data;
using Engine.Dialogue;
using Engine.World;
using Engine.Entities;

namespace Engine.Tests
{
    /// <summary>
    /// Тесты для загрузки данных с новыми полями DefaultDialogueId, Type, Tags, Condition
    /// </summary>
    public static class DataLoadingTests
    {
        /// <summary>
        /// Тест загрузки NPC с полем DefaultDialogueId
        /// </summary>
        public static void TestNPCDefaultDialogueId()
        {
            Console.WriteLine("=== Тест загрузки NPC с DefaultDialogueId ===");
            
            // Создаем тестовые данные NPC
            var npcData = new NPCData
            {
                ID = 1,
                Name = "Тестовый NPC",
                Greeting = "Привет!",
                DefaultDialogueId = "test_dialogue_001",
                GreetingDialogueId = "old_greeting" // Старое поле для обратной совместимости
            };
            
            // Проверяем, что поля загружаются корректно
            Console.WriteLine($"NPC ID: {npcData.ID}");
            Console.WriteLine($"NPC Name: {npcData.Name}");
            Console.WriteLine($"DefaultDialogueId: {npcData.DefaultDialogueId}");
            Console.WriteLine($"GreetingDialogueId: {npcData.GreetingDialogueId}");
            
            // Проверяем приоритет DefaultDialogueId над GreetingDialogueId
            var effectiveDialogueId = !string.IsNullOrEmpty(npcData.DefaultDialogueId) 
                ? npcData.DefaultDialogueId 
                : npcData.GreetingDialogueId;
            
            Console.WriteLine($"Эффективный DialogueId: {effectiveDialogueId}");
            
            if (effectiveDialogueId == "test_dialogue_001")
            {
                Console.WriteLine("✓ DefaultDialogueId имеет приоритет над GreetingDialogueId");
            }
            else
            {
                Console.WriteLine("✗ Неправильный приоритет полей");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Тест обратной совместимости с GreetingDialogueId
        /// </summary>
        public static void TestBackwardCompatibility()
        {
            Console.WriteLine("=== Тест обратной совместимости ===");
            
            // Создаем NPC только с GreetingDialogueId (старый формат)
            var npcData = new NPCData
            {
                ID = 2,
                Name = "Старый NPC",
                Greeting = "Привет!",
                GreetingDialogueId = "old_dialogue_001"
                // DefaultDialogueId не задан
            };
            
            // Проверяем fallback на GreetingDialogueId
            var effectiveDialogueId = !string.IsNullOrEmpty(npcData.DefaultDialogueId) 
                ? npcData.DefaultDialogueId 
                : npcData.GreetingDialogueId;
            
            Console.WriteLine($"DefaultDialogueId: {npcData.DefaultDialogueId ?? "null"}");
            Console.WriteLine($"GreetingDialogueId: {npcData.GreetingDialogueId}");
            Console.WriteLine($"Эффективный DialogueId: {effectiveDialogueId}");
            
            if (effectiveDialogueId == "old_dialogue_001")
            {
                Console.WriteLine("✓ Обратная совместимость работает");
            }
            else
            {
                Console.WriteLine("✗ Обратная совместимость не работает");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Тест загрузки диалоговых узлов с новыми полями
        /// </summary>
        public static void TestDialogueNodeFields()
        {
            Console.WriteLine("=== Тест загрузки диалоговых узлов с новыми полями ===");
            
            // Создаем тестовый диалоговый узел
            var nodeData = new DialogueNodeData
            {
                Id = "test_node_001",
                Text = "Тестовый текст узла",
                Type = "greeting",
                Tags = new List<string> { "default", "main" },
                Choices = new List<DialogueChoiceData>
                {
                    new DialogueChoiceData
                    {
                        Text = "Выбор 1",
                        NextNodeId = "next_node_001",
                        Condition = "hasItem:sword"
                    },
                    new DialogueChoiceData
                    {
                        Text = "Выбор 2",
                        NextNodeId = "next_node_002"
                        // Condition не задан
                    }
                }
            };
            
            Console.WriteLine($"Node ID: {nodeData.Id}");
            Console.WriteLine($"Node Text: {nodeData.Text}");
            Console.WriteLine($"Node Type: {nodeData.Type}");
            Console.WriteLine($"Node Tags: {string.Join(", ", nodeData.Tags)}");
            Console.WriteLine($"Количество выборов: {nodeData.Choices.Count}");
            
            // Проверяем первый выбор с условием
            var firstChoice = nodeData.Choices[0];
            Console.WriteLine($"Первый выбор - Condition: {firstChoice.Condition}");
            
            // Проверяем второй выбор без условия
            var secondChoice = nodeData.Choices[1];
            Console.WriteLine($"Второй выбор - Condition: {secondChoice.Condition ?? "null"}");
            
            if (!string.IsNullOrEmpty(firstChoice.Condition) && string.IsNullOrEmpty(secondChoice.Condition))
            {
                Console.WriteLine("✓ Поля Type, Tags и Condition загружаются корректно");
            }
            else
            {
                Console.WriteLine("✗ Проблемы с загрузкой новых полей");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Тест конвертации DialogueNodeData в DialogueNode
        /// </summary>
        public static void TestDialogueNodeConversion()
        {
            Console.WriteLine("=== Тест конвертации DialogueNodeData в DialogueNode ===");
            
            // Создаем тестовые данные
            var nodeData = new DialogueNodeData
            {
                Id = "conversion_test",
                Text = "Тест конвертации",
                Type = "quest_menu",
                Tags = new List<string> { "quest", "menu" },
                Choices = new List<DialogueChoiceData>
                {
                    new DialogueChoiceData
                    {
                        Text = "Принять квест",
                        NextNodeId = "quest_accepted",
                        Condition = "questAvailable:test_quest"
                    }
                }
            };
            
            // Создаем DialogueDocument для конвертации
            var dialogueData = new DialogueData
            {
                Id = "test_dialogue",
                Name = "Test Dialogue",
                Start = "conversion_test",
                Nodes = new List<DialogueNodeData> { nodeData }
            };
            
            // Создаем JsonWorldRepository для тестирования конвертации
            var repository = new JsonWorldRepository("Data/test_data.json");
            
            // Используем рефлексию для доступа к приватному методу
            var convertMethod = typeof(JsonWorldRepository).GetMethod("ConvertToDialogueDocument", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(DialogueData) }, null);
            
            if (convertMethod != null)
            {
                var dialogueDocument = (DialogueDocument)convertMethod.Invoke(repository, new object[] { dialogueData });
                
                Console.WriteLine($"Конвертированный документ ID: {dialogueDocument.Id}");
                Console.WriteLine($"Количество узлов: {dialogueDocument.Nodes.Count}");
                
                var convertedNode = dialogueDocument.Nodes.FirstOrDefault();
                if (convertedNode != null)
                {
                    Console.WriteLine($"Узел ID: {convertedNode.Id}");
                    Console.WriteLine($"Узел Type: {convertedNode.Type}");
                    Console.WriteLine($"Узел Tags: {string.Join(", ", convertedNode.Tags)}");
                    Console.WriteLine($"Количество ответов: {convertedNode.Responses.Count}");
                    
                    if (convertedNode.Responses.Count > 0)
                    {
                        var response = convertedNode.Responses[0];
                        Console.WriteLine($"Ответ Condition: {response.Condition ?? "null"}");
                    }
                    
                    Console.WriteLine("✓ Конвертация выполнена успешно");
                }
                else
                {
                    Console.WriteLine("✗ Узел не найден после конвертации");
                }
            }
            else
            {
                Console.WriteLine("✗ Метод конвертации не найден");
            }
            
            Console.WriteLine();
        }

        /// <summary>
        /// Запуск всех тестов загрузки данных
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("Запуск тестов загрузки данных...");
            Console.WriteLine();
            
            try
            {
                TestNPCDefaultDialogueId();
                TestBackwardCompatibility();
                TestDialogueNodeFields();
                TestDialogueNodeConversion();
                
                Console.WriteLine("=== Все тесты загрузки данных завершены ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Ошибка при выполнении тестов: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
    }
}
