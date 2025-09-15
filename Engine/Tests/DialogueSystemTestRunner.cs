using System;
using Engine.Tests;

namespace Engine.Tests
{
    /// <summary>
    /// Главный класс для запуска всех тестов системы диалогов
    /// </summary>
    public static class DialogueSystemTestRunner
    {
        /// <summary>
        /// Запуск всех тестов системы диалогов
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("==========================================");
            Console.WriteLine("ЗАПУСК ТЕСТОВ СИСТЕМЫ ДИАЛОГОВ");
            Console.WriteLine("==========================================");
            Console.WriteLine();
            
            try
            {
                // Запуск тестов загрузки данных
                Console.WriteLine("1. ТЕСТЫ ЗАГРУЗКИ ДАННЫХ");
                Console.WriteLine("=========================");
                DataLoadingTests.RunAllTests();
                
                Console.WriteLine();
                
                // Запуск тестов QuestDialogueManager
                Console.WriteLine("2. ТЕСТЫ QUESTDIALOGUEMANAGER");
                Console.WriteLine("=============================");
                QuestDialogueManagerTests.RunAllTests();
                
                Console.WriteLine();
                Console.WriteLine("==========================================");
                Console.WriteLine("ВСЕ ТЕСТЫ ЗАВЕРШЕНЫ");
                Console.WriteLine("==========================================");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"КРИТИЧЕСКАЯ ОШИБКА: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Запуск только тестов загрузки данных
        /// </summary>
        public static void RunDataLoadingTests()
        {
            Console.WriteLine("Запуск тестов загрузки данных...");
            DataLoadingTests.RunAllTests();
        }
        
        /// <summary>
        /// Запуск только тестов QuestDialogueManager
        /// </summary>
        public static void RunQuestDialogueManagerTests()
        {
            Console.WriteLine("Запуск тестов QuestDialogueManager...");
            QuestDialogueManagerTests.RunAllTests();
        }
    }
}
