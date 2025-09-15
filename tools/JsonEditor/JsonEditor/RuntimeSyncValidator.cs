using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;
using Engine.Dialogue;
using Engine.Quests;

namespace JsonEditor
{
    /// <summary>
    /// Валидатор для проверки синхронизации между JsonEditor и runtime системой
    /// </summary>
    public static class RuntimeSyncValidator
    {
        /// <summary>
        /// Проверяет синхронизацию между диалогами и квестами
        /// </summary>
        public static ValidationResult ValidateDialogueQuestSync(GameData gameData)
        {
            var result = new ValidationResult();
            
            if (gameData?.Dialogues == null || gameData?.Quests == null)
            {
                result.AddError("GameData не содержит диалоги или квесты");
                return result;
            }

            // Проверяем, что все квесты имеют корректные ID узлов диалогов
            foreach (var quest in gameData.Quests)
            {
                if (quest.DialogueNodes == null)
                {
                    result.AddWarning($"Квест {quest.ID} ({quest.Name}) не имеет DialogueNodes");
                    continue;
                }

                // Проверяем, что квестодатель существует
                var questGiver = gameData.NPCs?.FirstOrDefault(npc => npc.ID == quest.QuestGiverID);
                if (questGiver == null)
                {
                    result.AddError($"Квест {quest.ID} ({quest.Name}) ссылается на несуществующего NPC {quest.QuestGiverID}");
                }
                else
                {
                    // Проверяем, что у NPC есть диалог
                    if (string.IsNullOrEmpty(questGiver.DefaultDialogueId))
                    {
                        result.AddWarning($"NPC {questGiver.ID} ({questGiver.Name}) не имеет DefaultDialogueId");
                    }
                    else
                    {
                        // Проверяем, что диалог существует
                        var dialogue = gameData.Dialogues?.FirstOrDefault(d => d.Id == questGiver.DefaultDialogueId);
                        if (dialogue == null)
                        {
                            result.AddError($"NPC {questGiver.ID} ссылается на несуществующий диалог {questGiver.DefaultDialogueId}");
                        }
                    }
                }

                // Проверяем ID узлов диалогов квеста
                if (!string.IsNullOrEmpty(quest.DialogueNodes.Offer))
                {
                    result.AddInfo($"Квест {quest.ID} использует узел предложения: {quest.DialogueNodes.Offer}");
                }
                if (!string.IsNullOrEmpty(quest.DialogueNodes.InProgress))
                {
                    result.AddInfo($"Квест {quest.ID} использует узел в процессе: {quest.DialogueNodes.InProgress}");
                }
                if (!string.IsNullOrEmpty(quest.DialogueNodes.ReadyToComplete))
                {
                    result.AddInfo($"Квест {quest.ID} использует узел готовности: {quest.DialogueNodes.ReadyToComplete}");
                }
                if (!string.IsNullOrEmpty(quest.DialogueNodes.Completed))
                {
                    result.AddInfo($"Квест {quest.ID} использует узел завершения: {quest.DialogueNodes.Completed}");
                }
            }

            // Проверяем, что все диалоги имеют корректную структуру
            foreach (var dialogue in gameData.Dialogues)
            {
                if (dialogue.Nodes == null || dialogue.Nodes.Count == 0)
                {
                    result.AddWarning($"Диалог {dialogue.Id} не содержит узлов");
                    continue;
                }

                // Проверяем, что стартовый узел существует
                var startNode = dialogue.Nodes.FirstOrDefault(n => n.Id == dialogue.Start);
                if (startNode == null)
                {
                    result.AddError($"Диалог {dialogue.Id} ссылается на несуществующий стартовый узел {dialogue.Start}");
                }

                // Проверяем все узлы диалога
                foreach (var node in dialogue.Nodes)
                {
                    if (node.Choices != null)
                    {
                        foreach (var choice in node.Choices)
                        {
                            // Проверяем целевые узлы
                            if (!string.IsNullOrEmpty(choice.NextNodeId))
                            {
                                var targetNode = dialogue.Nodes.FirstOrDefault(n => n.Id == choice.NextNodeId);
                                if (targetNode == null)
                                {
                                    result.AddWarning($"Узел {node.Id} в диалоге {dialogue.Id} ссылается на несуществующий целевой узел {choice.NextNodeId}");
                                }
                            }

                            // Проверяем условия
                            if (!string.IsNullOrEmpty(choice.Condition))
                            {
                                result.AddInfo($"Узел {node.Id} содержит условие: {choice.Condition}");
                            }

                            // Проверяем действия
                            if (choice.Actions != null && choice.Actions.Count > 0)
                            {
                                foreach (var action in choice.Actions)
                                {
                                    result.AddInfo($"Узел {node.Id} содержит действие: {action.Type}({action.Parameter})");
                                }
                            }
                        }
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Проверяет, что все NPC имеют корректные настройки торговли
        /// </summary>
        public static ValidationResult ValidateNPCTrading(GameData gameData)
        {
            var result = new ValidationResult();

            if (gameData?.NPCs == null)
            {
                result.AddError("GameData не содержит NPC");
                return result;
            }

            foreach (var npc in gameData.NPCs)
            {
                // Проверяем торговцев
                if (npc.Merchant != null)
                {
                    if (npc.Merchant.ItemsForSale == null || npc.Merchant.ItemsForSale.Count == 0)
                    {
                        result.AddWarning($"Торговец {npc.ID} ({npc.Name}) не имеет товаров для продажи");
                    }
                    else
                    {
                        // Проверяем, что все товары существуют
                        foreach (var itemForSale in npc.Merchant.ItemsForSale)
                        {
                            var item = gameData.Items?.FirstOrDefault(i => i.ID == itemForSale.ItemID);
                            if (item == null)
                            {
                                result.AddError($"Торговец {npc.ID} продает несуществующий предмет {itemForSale.ItemID}");
                            }
                        }
                    }
                }
                // Проверяем старую систему торговли (если есть)
                // В новой системе торговля идет через Merchant.ItemsForSale
            }

            return result;
        }

        /// <summary>
        /// Показывает результаты валидации в диалоговом окне
        /// </summary>
        public static void ShowValidationResults(ValidationResult result)
        {
            var message = "Результаты валидации:\n\n";
            
            if (result.Errors.Count > 0)
            {
                message += "ОШИБКИ:\n";
                foreach (var error in result.Errors)
                {
                    message += $"❌ {error}\n";
                }
                message += "\n";
            }

            if (result.Warnings.Count > 0)
            {
                message += "ПРЕДУПРЕЖДЕНИЯ:\n";
                foreach (var warning in result.Warnings)
                {
                    message += $"⚠️ {warning}\n";
                }
                message += "\n";
            }

            if (result.Infos.Count > 0)
            {
                message += "ИНФОРМАЦИЯ:\n";
                foreach (var info in result.Infos)
                {
                    message += $"ℹ️ {info}\n";
                }
            }

            if (result.Errors.Count == 0 && result.Warnings.Count == 0)
            {
                message += "✅ Все проверки пройдены успешно!";
            }

            MessageBox.Show(message, "Результаты валидации", MessageBoxButtons.OK, 
                result.Errors.Count > 0 ? MessageBoxIcon.Error : 
                result.Warnings.Count > 0 ? MessageBoxIcon.Warning : MessageBoxIcon.Information);
        }
    }

    /// <summary>
    /// Результат валидации
    /// </summary>
    public class ValidationResult
    {
        public List<string> Errors { get; } = new List<string>();
        public List<string> Warnings { get; } = new List<string>();
        public List<string> Infos { get; } = new List<string>();

        public void AddError(string message) => Errors.Add(message);
        public void AddWarning(string message) => Warnings.Add(message);
        public void AddInfo(string message) => Infos.Add(message);

        public bool HasErrors => Errors.Count > 0;
        public bool HasWarnings => Warnings.Count > 0;
        public bool IsValid => !HasErrors;
    }
}
