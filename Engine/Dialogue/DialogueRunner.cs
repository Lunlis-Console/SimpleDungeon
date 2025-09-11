using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace SimpleDungeon.Engine.Dialogue
{
    public class DialogueRunner
    {
        private readonly Dictionary<string, DialogueNode> _nodes = new(StringComparer.OrdinalIgnoreCase);
        public DialogueDocument Document { get; }
        public string CurrentNodeId { get; private set; }

        // Обработчики действий: handler(param, playerContext, ui)
        private readonly Dictionary<string, Action<string, object, IDialogueUI>> _handlers
            = new(StringComparer.OrdinalIgnoreCase);

        // Простой контекст игрока, можно подставить ваш класс player
        public object PlayerContext { get; set; }

        public DialogueRunner(DialogueDocument doc)
        {
            Document = doc ?? throw new ArgumentNullException(nameof(doc));
            if (doc.Nodes != null)
            {
                foreach (var n in doc.Nodes)
                {
                    if (string.IsNullOrWhiteSpace(n.Id)) continue;
                    if (!_nodes.ContainsKey(n.Id)) _nodes.Add(n.Id, n);
                }
            }

            CurrentNodeId = doc.Start;
            RegisterDefaultHandlers();
        }

        // Загрузка/сохранение файла
        public static DialogueDocument LoadFromFile(string path)
        {
            var txt = File.ReadAllText(path);
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver()
            };
            return JsonConvert.DeserializeObject<DialogueDocument>(txt, settings);
        }

        public static void SaveToFile(DialogueDocument doc, string path)
        {
            var settings = new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                ContractResolver = new CamelCasePropertyNamesContractResolver(),
                Formatting = Formatting.None
            };
            File.WriteAllText(path, JsonConvert.SerializeObject(doc, settings));
        }

        public DialogueNode GetCurrentNode()
        {
            if (string.IsNullOrEmpty(CurrentNodeId)) return null;
            return _nodes.TryGetValue(CurrentNodeId, out var n) ? n : null;
        }

        public void RegisterActionHandler(string type, Action<string, object, IDialogueUI> handler)
        {
            if (string.IsNullOrWhiteSpace(type)) throw new ArgumentNullException(nameof(type));
            _handlers[type] = handler;
        }

        public bool HasNode(string id) => !string.IsNullOrEmpty(id) && _nodes.ContainsKey(id);

        // Основной вызов: выбираем ответ по индексу (который пользователь выбрал в UI)
        public void ChooseResponse(int responseIndex, IDialogueUI ui)
        {
            var node = GetCurrentNode();
            if (node == null)
            {
                ui?.CloseDialogue();
                CurrentNodeId = null;
                return;
            }

            if (node.Responses == null || responseIndex < 0 || responseIndex >= node.Responses.Count)
            {
                ui?.Log("Invalid response index.");
                ui?.CloseDialogue();
                CurrentNodeId = null;
                return;
            }

            var resp = node.Responses[responseIndex];

            // Выполнение действий
            if (resp.Actions != null)
            {
                foreach (var act in resp.Actions)
                {
                    if (act == null || string.IsNullOrWhiteSpace(act.Type)) continue;

                    if (_handlers.TryGetValue(act.Type, out var h))
                    {
                        try { h(act.Param, PlayerContext, ui); }
                        catch (Exception ex) { ui?.Log($"Action handler '{act.Type}' error: {ex.Message}"); }
                    }
                    else
                    {
                        // встроенные стандартные действия
                        HandleBuiltIn(act, ui);
                    }
                }
            }

            // Переход
            if (string.IsNullOrEmpty(resp.Target))
            {
                ui?.CloseDialogue();
                CurrentNodeId = null;
            }
            else
            {
                if (!_nodes.ContainsKey(resp.Target))
                {
                    ui?.Log($"Target node '{resp.Target}' not found. Closing dialogue.");
                    ui?.CloseDialogue();
                    CurrentNodeId = null;
                }
                else
                {
                    CurrentNodeId = resp.Target;
                    ui?.ShowNode(GetCurrentNode());
                }
            }
        }

        private void HandleBuiltIn(DialogueAction act, IDialogueUI ui)
        {
            switch (act.Type)
            {
                case "GiveGold":
                    if (int.TryParse(act.Param, out var gold))
                        ui?.Log($"Gave player {gold} gold.");
                    else ui?.Log($"GiveGold: bad param '{act.Param}'");
                    break;
                case "StartTrade":
                    ui?.Log("StartTrade invoked.");
                    break;
                case "StartQuest":
                    ui?.Log($"StartQuest '{act.Param}' invoked.");
                    break;
                case "GiveItem":
                    ui?.Log($"GiveItem '{act.Param}' invoked.");
                    break;
                case "SetFlag":
                    ui?.Log($"SetFlag '{act.Param}' invoked.");
                    break;
                default:
                    ui?.Log($"Unknown action type '{act.Type}'.");
                    break;
            }
        }

        private void RegisterDefaultHandlers()
        {
            // Пример: вы можете зарегистрировать свои обработчики из игровой части:
            // RegisterActionHandler("GiveGold", (param, ctx, ui) => { ((Player)ctx).Gold += int.Parse(param); ui?.Log("Gold given"); });
            // Здесь по умолчанию ничего кроме логов не делаем
        }

        // Валидация документа
        public static (bool ok, string message) Validate(DialogueDocument doc)
        {
            if (doc == null) return (false, "Document is null");
            if (string.IsNullOrWhiteSpace(doc.Start)) return (false, "Start node not set");
            var ids = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            if (doc.Nodes == null || doc.Nodes.Count == 0) return (false, "No nodes");
            foreach (var n in doc.Nodes)
            {
                if (string.IsNullOrWhiteSpace(n.Id)) return (false, "One node has empty id");
                if (!ids.Add(n.Id)) return (false, $"Duplicate node id '{n.Id}'");
            }
            // check targets
            var idSet = new HashSet<string>(ids);
            foreach (var n in doc.Nodes)
            {
                if (n.Responses == null) continue;
                foreach (var r in n.Responses)
                {
                    if (!string.IsNullOrEmpty(r.Target) && !idSet.Contains(r.Target))
                        return (false, $"Response in node '{n.Id}' targets missing node '{r.Target}'");
                }
            }
            if (!idSet.Contains(doc.Start)) return (false, $"Start '{doc.Start}' not present in nodes");
            return (true, "OK");
        }
    }
}
