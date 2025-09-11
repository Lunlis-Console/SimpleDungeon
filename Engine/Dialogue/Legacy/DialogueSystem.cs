// Engine/Dialogue/DialogueSystem.cs
using System;
using System.Collections.Generic;
using Engine.Entities;
using Engine.World;

namespace Engine.Dialogue.Legacy
{
    // Интерфейс для UI диалогов
    public interface IDialogueUI
    {
        void SetCurrentNode(DialogueSystem.DialogueNode node);
        void CloseDialogue();
    }

    public class DialogueSystem
    {
        public class DialogueNode
        {
            public string Text { get; set; }
            public List<DialogueOption> Options { get; set; }
            public Action OnEnter { get; set; }

            public DialogueNode(string text, Action onEnter = null)
            {
                Text = text;
                Options = new List<DialogueOption>();
                OnEnter = onEnter;
            }
        }

        public class DialogueOption
        {
            public string Text { get; set; }
            public DialogueNode NextNode { get; set; }

            // поля из JSON
            public string Action { get; set; }
            public string Parameter { get; set; }
            public string Condition { get; set; }

            public bool IsAvailable { get; set; } = true;
            public bool IsVisited { get; set; } = false;

            public DialogueOption(string text, DialogueNode nextNode = null)
            {
                Text = text ?? string.Empty;
                NextNode = nextNode;
            }

            // Вызов при выборе варианта
            public void ExecuteSelection(Player player, IDialogueUI ui)
            {
                if (!string.IsNullOrWhiteSpace(Action))
                {
                    try
                    {
                        DialogueActions.Execute(Action, Parameter, player, ui);
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log($"DialogueOption.ExecuteSelection: action '{Action}' failed: {ex.Message}");
                    }
                }

                if (NextNode != null)
                {
                    ui?.SetCurrentNode(NextNode);
                }
                else
                {
                    ui?.CloseDialogue();
                }

                IsVisited = true;
            }

            // Простейшая проверка условий видимости
            public bool EvaluateCondition(Player player)
            {
                if (string.IsNullOrWhiteSpace(Condition)) return true;

                var parts = Condition.Split(new[] { ':' }, 2);
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

                        default:
                            return false;
                    }
                }
                catch
                {
                    return false;
                }
            }
        }

        // Централизованный обработчик действий
        public static class DialogueActions
        {
            private static readonly Dictionary<string, Action<string, Player, IDialogueUI>> _handlers
                = new Dictionary<string, Action<string, Player, IDialogueUI>>(StringComparer.OrdinalIgnoreCase);

            public static void RegisterHandler(string actionName, Action<string, Player, IDialogueUI> handler)
            {
                if (string.IsNullOrWhiteSpace(actionName) || handler == null) return;
                _handlers[actionName] = handler;
            }

            public static bool UnregisterHandler(string actionName) => _handlers.Remove(actionName);

            public static void Execute(string action, string parameter, Player player, IDialogueUI ui)
            {
                if (string.IsNullOrWhiteSpace(action)) return;

                if (_handlers.TryGetValue(action, out var handler))
                {
                    try { handler(parameter, player, ui); }
                    catch (Exception ex) { DebugConsole.Log($"DialogueActions handler '{action}' threw: {ex.Message}"); }
                    return;
                }

                // Fallback-реализация ("из коробки")
                switch (action)
                {
                    case "EndDialogue":
                        ui?.CloseDialogue();
                        break;

                    case "GiveGold":
                        if (player != null && int.TryParse(parameter, out var gold))
                        {
                            player.Gold += gold;
                            DebugConsole.Log($"DialogueAction: gave {gold} gold to player");
                        }
                        break;

                    case "GiveItem":
                        if (player != null && !string.IsNullOrWhiteSpace(parameter))
                        {
                            var p = parameter.Split(',');
                            if (int.TryParse(p[0], out var itemId))
                            {
                                var qty = 1;
                                if (p.Length > 1) int.TryParse(p[1], out qty);
                                var item = JsonWorldRepository.Instance?.ItemByID(itemId);
                                if (item != null)
                                {
                                    // используем существующий метод игрока
                                    player.AddItemToInventory(item, qty);
                                    DebugConsole.Log($"DialogueAction: gave item {itemId} x{qty} to player");
                                }
                            }
                        }
                        break;

                    case "StartQuest":
                        if (player != null && !string.IsNullOrWhiteSpace(parameter))
                        {
                            // сначала попробуем числовой ID
                            if (int.TryParse(parameter, out var qid))
                            {
                                var quest = JsonWorldRepository.Instance?.QuestByID(qid);
                                if (quest != null)
                                {
                                    player.AddQuest(quest);
                                    DebugConsole.Log($"DialogueAction: started quest id={qid}");
                                }
                            }
                            else
                            {
                                // попытка по имени либо по ID->ToString
                                var quest = JsonWorldRepository.Instance?.GetAllQuests()
                                    .Find(q => string.Equals(q.Name, parameter, StringComparison.OrdinalIgnoreCase)
                                            || q.ID.ToString() == parameter);
                                if (quest != null)
                                {
                                    player.AddQuest(quest);
                                    DebugConsole.Log($"DialogueAction: started quest '{parameter}'");
                                }
                            }
                        }
                        ui?.CloseDialogue();
                        break;

                    default:
                        DebugConsole.Log($"DialogueActions: no handler for '{action}' (param='{parameter}')");
                        break;
                }
            }
        }
    }
}
