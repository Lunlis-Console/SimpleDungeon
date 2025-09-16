// Engine/Dialogue/DialogueSystem.cs
using Engine.Data;
using Engine.Entities;
using Engine.Quests;
using Engine.World;
using System;
using System.Collections.Generic;
using System.Linq;
using Engine.Core;

namespace Engine.Dialogue
{
    // Интерфейс для UI диалогов
    public interface IDialogueUI
    {
        /// <summary>
        /// Показывает текущий узел диалога (вся визуализация делегируется UI).
        /// </summary>
        void SetCurrentNode(DialogueNode node);

        /// <summary>
        /// Закрыть диалоговое окно / завершить диалог.
        /// </summary>
        void CloseDialogue();

        /// <summary>
        /// Открыть экран торговли для NPC (если текущий UI — экран диалога).
        /// </summary>
        void OpenTrade();
    }

    public class DialogueSystem
    {
            /// <summary>
        /// Проверяет условие диалога
            /// </summary>
        public static bool EvaluateCondition(string condition, Player player, NPC npc = null)
            {
            if (string.IsNullOrWhiteSpace(condition)) return true;

            var parts = condition.Split(new[] { ':' }, 2);
                var key = parts[0];
                var val = parts.Length > 1 ? parts[1] : null;

                try
                {
                    switch (key)
                    {
                        case "HasItem":
                            if (player == null) return false;
                            if (int.TryParse(val, out var itemId))
                                return player.Inventory.HasItem(itemId);
                            return false;

                        case "QuestActive":
                            if (player == null) return false;
                            return player.HasQuest(val);

                        case "FlagSet":
                            return WorldState.Instance.IsFlagSet(val);

                    case "questAvailableForNPC":
                        if (player == null || npc == null || string.IsNullOrEmpty(val)) return false;
                        if (int.TryParse(val, out var availableQuestId))
                        {
                            var quest = player.QuestLog.GetQuest(availableQuestId);
                            var result = quest != null && quest.State == QuestState.NotStarted && quest.QuestGiverID == npc.ID;
                            DebugConsole.Log($"questAvailableForNPC:{availableQuestId} - Quest: {quest?.Name}, State: {quest?.State}, QuestGiver: {quest?.QuestGiverID}, NPC: {npc.ID}, Result: {result}");
                            return result;
                        }
                        return false;

                        case "questInProgressForNPC":
                            if (player == null || npc == null || string.IsNullOrEmpty(val)) return false;
                            if (int.TryParse(val, out var inProgressQuestId))
                            {
                                var quest = player.QuestLog.GetQuest(inProgressQuestId);
                            return quest != null && quest.State == QuestState.InProgress && quest.QuestGiverID == npc.ID;
                            }
                            return false;

                        case "questReadyToCompleteForNPC":
                            if (player == null || npc == null || string.IsNullOrEmpty(val)) return false;
                            if (int.TryParse(val, out var readyQuestId))
                            {
                                var quest = player.QuestLog.GetQuest(readyQuestId);
                            return quest != null && quest.State == QuestState.ReadyToComplete && quest.QuestGiverID == npc.ID;
                            }
                            return false;

                        case "questReadyToComplete":
                            if (player == null || string.IsNullOrEmpty(val)) return false;
                            if (int.TryParse(val, out var readyQuestIdGlobal))
                            {
                                var quest = player.QuestLog.GetQuest(readyQuestIdGlobal);
                                var result = quest != null && quest.State == QuestState.ReadyToComplete;
                                DebugConsole.Log($"questReadyToComplete:{readyQuestIdGlobal} - Quest: {quest?.Name}, State: {quest?.State}, Result: {result}");
                                return result;
                            }
                            return false;

                    default:
                        DebugConsole.Log($"Неизвестное условие диалога: {key}");
                        return true;
                        }
                    }
                    catch (Exception ex)
                    {
                DebugConsole.Log($"Ошибка при проверке условия '{condition}': {ex.Message}");
                return true;
            }
        }

        /// <summary>
        /// Выполняет действие диалога
        /// </summary>
        public static void ExecuteAction(DialogueAction action, Player player, NPC npc = null)
        {
            if (action == null) return;

            try
            {
                switch (action.Type?.ToLower())
                {
                    case "starttrade":
                        // Действие будет обработано в UI
                        break;

                    case "enddialogue":
                        // Действие будет обработано в UI
                        break;

                    case "startquest":
                        if (int.TryParse(action.Param, out var questId))
                        {
                            player.QuestLog.StartQuest(questId);
                            DebugConsole.Log($"Начат квест {questId}");
                        }
                        break;

                    case "completequest":
                        if (int.TryParse(action.Param, out var completeQuestId))
                        {
                            player.QuestLog.CompleteQuest(completeQuestId);
                            DebugConsole.Log($"Завершен квест {completeQuestId}");
                        }
                        break;

                    case "givegold":
                        if (int.TryParse(action.Param, out var goldAmount))
                        {
                            player.Gold += goldAmount;
                            DebugConsole.Log($"Получено золота: {goldAmount}");
                        }
                        break;

                    case "giveitem":
                        if (int.TryParse(action.Param, out var itemId))
                        {
                            var worldRepo = GameServices.WorldRepository;
                            var item = worldRepo?.ItemByID(itemId);
                                if (item != null)
                                {
                                player.Inventory.AddItem(item);
                                DebugConsole.Log($"Получен предмет: {item.Name}");
                            }
                        }
                        break;

                    case "setflag":
                        if (!string.IsNullOrEmpty(action.Param))
                        {
                            WorldState.Instance.SetFlag(action.Param, true);
                            DebugConsole.Log($"Установлен флаг: {action.Param}");
                        }
                        break;

                    default:
                        DebugConsole.Log($"Неизвестное действие диалога: {action.Type}");
                        break;
                }
            }
            catch (Exception ex)
            {
                DebugConsole.Log($"Ошибка при выполнении действия '{action.Type}': {ex.Message}");
        }
    }
}
}