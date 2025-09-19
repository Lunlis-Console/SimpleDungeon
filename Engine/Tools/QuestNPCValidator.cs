using Engine.Core;
using Engine.Quests;
using Engine.World;
using System;
using System.Linq;
using Engine.Entities;

namespace Engine.Tools
{
    /// <summary>
    /// Утилита для проверки и валидации связей между квестами и NPC
    /// </summary>
    public static class QuestNPCValidator
    {
        /// <summary>
        /// Проверяет все связи между квестами и NPC
        /// </summary>
        public static void ValidateQuestNPCConnections(QuestLog questLog, IWorldRepository worldRepository)
        {
            DebugConsole.Log("=== ВАЛИДАЦИЯ КВЕСТ-NPC ===");
            
            // Проверяем все доступные квесты
            DebugConsole.Log($"Доступные квесты ({questLog.AvailableQuests.Count}):");
            foreach (var quest in questLog.AvailableQuests)
            {
                ValidateQuestNPCConnection(quest, worldRepository);
            }
            
            // Проверяем все активные квесты
            DebugConsole.Log($"Активные квесты ({questLog.ActiveQuests.Count}):");
            foreach (var quest in questLog.ActiveQuests)
            {
                ValidateQuestNPCConnection(quest, worldRepository);
            }
            
            // Проверяем все завершенные квесты
            DebugConsole.Log($"Завершенные квесты ({questLog.CompletedQuests.Count}):");
            foreach (var quest in questLog.CompletedQuests)
            {
                ValidateQuestNPCConnection(quest, worldRepository);
            }
            
            DebugConsole.Log("=== END QUEST-NPC VALIDATION ===");
        }
        
        /// <summary>
        /// Проверяет связь конкретного квеста с NPC
        /// </summary>
        private static void ValidateQuestNPCConnection(EnhancedQuest quest, IWorldRepository worldRepository)
        {
            var npc = worldRepository.NPCByID(quest.QuestGiverID);
            
            if (npc == null)
            {
                DebugConsole.Log($"❌ Quest {quest.ID} ({quest.Name}) -> NPC {quest.QuestGiverID} NOT FOUND");
                return;
            }
            
            // Проверяем, есть ли у NPC диалог (приоритет DefaultDialogueId)
            var hasDialogue = !string.IsNullOrEmpty(npc.DefaultDialogueId);
            
            if (hasDialogue)
            {
                var dialogueId = npc.DefaultDialogueId; // Обратная совместимость
                DebugConsole.Log($"✅ Quest {quest.ID} ({quest.Name}) -> NPC {quest.QuestGiverID} ({npc.Name}) [Dialogue: {dialogueId}]");
            }
            else
            {
                DebugConsole.Log($"⚠️ Quest {quest.ID} ({quest.Name}) -> NPC {quest.QuestGiverID} ({npc.Name}) [NO DIALOGUE]");
            }
        }
        
        /// <summary>
        /// Проверяет условия диалога для конкретного NPC
        /// </summary>
        public static void ValidateDialogueConditionsForNPC(NPC npc, Player player)
        {
            DebugConsole.Log($"=== DIALOGUE CONDITIONS VALIDATION for NPC {npc.ID} ({npc.Name}) ===");
            
            // Проверяем доступные квесты
            var availableQuests = player.QuestLog.GetAvailableQuestsForNPC(npc.ID);
            DebugConsole.Log($"Available quests for NPC {npc.ID}: {availableQuests.Count}");
            foreach (var quest in availableQuests)
            {
                DebugConsole.Log($"  - {quest.Name} (ID: {quest.ID}, State: {quest.State})");
            }
            
            // Проверяем активные квесты
            var activeQuests = player.QuestLog.GetActiveQuestsForNPC(npc.ID);
            DebugConsole.Log($"Active quests for NPC {npc.ID}: {activeQuests.Count}");
            foreach (var quest in activeQuests)
            {
                DebugConsole.Log($"  - {quest.Name} (ID: {quest.ID}, State: {quest.State})");
            }
            
            // Проверяем завершенные квесты
            var completedQuests = player.QuestLog.GetCompletedQuestsForNPC(npc.ID);
            DebugConsole.Log($"Completed quests for NPC {npc.ID}: {completedQuests.Count}");
            foreach (var quest in completedQuests)
            {
                DebugConsole.Log($"  - {quest.Name} (ID: {quest.ID}, State: {quest.State})");
            }
            
            DebugConsole.Log("=== END DIALOGUE CONDITIONS VALIDATION ===");
        }
    }
}
