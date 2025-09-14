// Engine/Dialogue/DialogueSystem.cs
using Engine.Data;
using Engine.Entities;
using Engine.World;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using Engine.Core;

namespace Engine.Dialogue
{
    // Интерфейс для UI диалогов
    public interface IDialogueUI
    {
        /// <summary>
        /// Показывает текущий узел диалога (вся визуализация делегируется UI).
        /// </summary>
        void SetCurrentNode(DialogueSystem.DialogueNode node);

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

            public List<DialogueActionData> Actions { get; set; } = new List<DialogueActionData>();

            // флаг, чтобы регистрация выполнялась только один раз
            private static bool _defaultActionHandlersRegistered = false;


            public DialogueOption(string text, DialogueNode nextNode = null)
            {
                Text = text ?? string.Empty;
                NextNode = nextNode;
            }

            // Вызов при выборе варианта
            // Вставь / замени этот метод в том классе, где реализуется выполнение выбора диалога.
            // Обычно это DialogueSystem.DialogueOption или DialogueActions. Подставь точное имя класса.
            // Метод использует:
            // - DialogueAction (enum) из Engine.Dialogue
            // - IDialogueUI ui (интерфейс диалогового UI; у тебя он уже есть)
            // - ScreenManager (статический менеджер экранов) — уже используется в проекте

            public void ExecuteSelection(Player player, IDialogueUI ui)
            {
                if (player == null) DebugConsole.Log("ExecuteSelection: player is null");
                if (ui == null) DebugConsole.Log("ExecuteSelection: ui is null");

                // Попробуем получить коллекцию действий (Actions) из текущего объекта (динамически, через reflection).
                var actionsObj = this.GetType().GetProperty("Actions")?.GetValue(this)
                               ?? this.GetType().GetField("Actions")?.GetValue(this);

                var actions = actionsObj as IEnumerable;
                if (actions == null)
                {
                    DebugConsole.Log("ExecuteSelection: no Actions collection found on option; handling NextNode/Close only.");
                    HandleNextOrClose(ui);
                    return;
                }

                foreach (var actionObj in actions)
                {
                    if (actionObj == null) continue;

                    try
                    {
                        // Читаем тип действия: поддерживаем "Type", "type", "Action", "action"
                        string typeStr = TryGetStringProperty(actionObj, "Type")
                                         ?? TryGetStringProperty(actionObj, "type")
                                         ?? TryGetStringProperty(actionObj, "Action")
                                         ?? TryGetStringProperty(actionObj, "action");

                        // Читаем параметр: поддерживаем "Param", "param", "Parameter", "parameter"
                        string paramStr = TryGetStringProperty(actionObj, "Param")
                                          ?? TryGetStringProperty(actionObj, "param")
                                          ?? TryGetStringProperty(actionObj, "Parameter")
                                          ?? TryGetStringProperty(actionObj, "parameter");

                        if (string.IsNullOrWhiteSpace(typeStr))
                        {
                            DebugConsole.Log("ExecuteSelection: action.Type is empty -> skipping.");
                            continue;
                        }

                        var t = typeStr.Trim().ToLowerInvariant();

                        if (int.TryParse(t, out var _typeNum))
                        {
                            switch (_typeNum)
                            {
                                case 0: t = "none"; break;
                                case 1: t = "giveitem"; break;
                                case 2: t = "givegold"; break;
                                case 3: t = "startquest"; break;
                                case 4: t = "completequest"; break;
                                case 5: t = "setflag"; break;
                                case 6: t = "starttrade"; break;
                                // если будут другие номера — сюда дописываем
                                default: break;
                            }
                        }

                        switch (t)
                        {
                            case "none":
                                // ничего делать не нужно
                                break;

                            case "starttrade":
                            case "start_trade":
                            case "trade":
                                DebugConsole.Log("DialogueAction: StartTrade invoked");
                                try { ui?.OpenTrade(); } catch (Exception ex) { DebugConsole.Log("StartTrade failed: " + ex.Message); }
                                break;

                            case "enddialogue":
                            case "end_dialogue":
                            case "end":
                                try { ui?.CloseDialogue(); } catch (Exception ex) { DebugConsole.Log("EndDialogue failed: " + ex.Message); }
                                break;

                            case "givegold":
                            case "give_gold":
                            case "gold":
                                {
                                    long amount = ParseLongParam(paramStr);
                                    DebugConsole.Log($"GiveGold: attempt to add {amount} gold to player.");
                                    if (amount == 0)
                                    {
                                        DebugConsole.Log($"GiveGold: param '{paramStr}' parsed as 0 — skipping.");
                                        break;
                                    }
                                    if (TryAddGoldToPlayer(player, amount))
                                    {
                                        DebugConsole.Log($"GiveGold: added {amount} gold to player.");
                                        MessageSystem.AddMessage($"Добавлено золота: {amount}");
                                    }
                                    else
                                    {
                                        DebugConsole.Log($"GiveGold: failed to add gold {amount} to player.");
                                    }
                                }
                                break;


                            case "giveitem":
                            case "give_item":
                            case "item":
                                {
                                    // Поддерживаем форматы param:
                                    // "itemId:iron_sword;qty:2"  или "iron_sword,2" или просто "iron_sword"
                                    var kv = ParseParamString(paramStr);
                                    string itemId = null;
                                    int qty = 1;
                                    if (kv.ContainsKey("itemid")) itemId = kv["itemid"];
                                    else if (kv.ContainsKey("id")) itemId = kv["id"];
                                    else if (!string.IsNullOrEmpty(paramStr))
                                    {
                                        // Разделяем по , ; пробелу и табу
                                        var parts = paramStr.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                                        if (parts.Length > 0) itemId = parts[0].Trim();
                                        if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var q)) qty = q;
                                    }
                                    if (kv.ContainsKey("qty") && int.TryParse(kv["qty"], out var q2)) qty = q2;
                                    if (kv.ContainsKey("quantity") && int.TryParse(kv["quantity"], out var q3)) qty = q3;

                                    if (string.IsNullOrEmpty(itemId))
                                    {
                                        DebugConsole.Log($"GiveItem: param '{paramStr}' did not contain item id.");
                                        break;
                                    }

                                    var invItem = TryCreateInventoryItem(itemId, qty);
                                    if (invItem == null)
                                    {
                                        DebugConsole.Log($"GiveItem: factory failed to create item '{itemId}'.");
                                    }
                                    else
                                    {
                                        if (TryAddInventoryItemToPlayer(player, invItem))
                                        {
                                            DebugConsole.Log($"GiveItem: added {itemId} x{qty} to player.");
                                            var itemName = GetInventoryItemName(invItem);
                                            MessageSystem.AddMessage($"Добавлено: {itemName} x{qty}");
                                        }
                                        else
                                            DebugConsole.Log($"GiveItem: created item but failed to add to player inventory.");
                                    }
                                }
                                break;

                            case "startquest":
                            case "start_quest":
                                {
                                    // param может быть id квеста или имя
                                    if (string.IsNullOrWhiteSpace(paramStr))
                                    {
                                        DebugConsole.Log("StartQuest: no param provided.");
                                        break;
                                    }
                                    var questObj = TryFindQuestByIdOrName(paramStr);
                                    if (questObj == null)
                                    {
                                        DebugConsole.Log($"StartQuest: quest '{paramStr}' not found.");
                                        break;
                                    }
                                    if (!TryAddQuestToPlayer(player, questObj))
                                        DebugConsole.Log($"StartQuest: failed to add quest '{paramStr}' to player.");
                                    else
                                        DebugConsole.Log($"StartQuest: added quest '{paramStr}' to player.");
                                }
                                break;

                            case "completequest":
                            case "complete_quest":
                                {
                                    if (string.IsNullOrWhiteSpace(paramStr))
                                    {
                                        DebugConsole.Log("CompleteQuest: no param provided.");
                                        break;
                                    }
                                    var questObj = TryFindQuestByIdOrName(paramStr);
                                    if (questObj == null)
                                    {
                                        DebugConsole.Log($"CompleteQuest: quest '{paramStr}' not found.");
                                        break;
                                    }
                                    if (!TryCompleteQuestForPlayer(player, questObj))
                                        DebugConsole.Log($"CompleteQuest: failed to complete quest '{paramStr}'.");
                                    else
                                        DebugConsole.Log($"CompleteQuest: quest '{paramStr}' completed for player.");
                                }
                                break;

                            case "setflag":
                            case "set_flag":
                                {
                                    // param: "flagName=true" or "flagName"
                                    var kv = ParseParamString(paramStr);
                                    string flagName = null;
                                    string flagVal = "true";
                                    if (kv.Count == 0)
                                    {
                                        if (!string.IsNullOrWhiteSpace(paramStr)) flagName = paramStr.Trim();
                                    }
                                    else
                                    {
                                        // take first pair
                                        flagName = kv.Keys.FirstOrDefault();
                                        flagVal = kv[flagName];
                                    }

                                    if (string.IsNullOrEmpty(flagName))
                                    {
                                        DebugConsole.Log("SetFlag: no flag name provided.");
                                        break;
                                    }

                                    if (!TrySetGlobalFlag(flagName, flagVal))
                                        DebugConsole.Log($"SetFlag: failed to set {flagName}={flagVal}");
                                    else
                                        DebugConsole.Log($"SetFlag: {flagName}={flagVal}");
                                }
                                break;

                            default:
                                DebugConsole.Log($"ExecuteSelection: unknown action type '{typeStr}'");
                                break;
                        }
                    }
                    catch (Exception exAction)
                    {
                        DebugConsole.Log("ExecuteSelection: action processing error: " + exAction.Message);
                    }
                } // foreach action

                // После выполнения всех действий — переходим к NextNode или закрываем диалог (если не открыт TradeScreen)
                HandleNextOrClose(ui);
                return;
            }

            // ---------------------- Вспомогательные методы (локальные / приватные) ----------------------

            private static string GetInventoryItemName(object invItem)
            {
                if (invItem == null) return "неизвестный предмет";

                try
                {
                    // Ограничения для защиты от рекурсий/больших графов
                    const int MaxDepth = 3;
                    var visited = new HashSet<object>();

                    string HumanizeKey(string key)
                    {
                        if (string.IsNullOrWhiteSpace(key)) return key;
                        var tmp = key.Replace('_', ' ').Replace('-', ' ').Trim();
                        var parts = tmp.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)
                                       .Select(p => char.ToUpperInvariant(p[0]) + (p.Length > 1 ? p.Substring(1) : ""));
                        return string.Join(" ", parts);
                    }

                    bool IsSimple(Type t)
                    {
                        if (t == typeof(string)) return true;
                        if (t.IsPrimitive) return true;
                        if (t.IsEnum) return true;
                        if (t == typeof(decimal) || t == typeof(DateTime) || t == typeof(TimeSpan)) return true;
                        return false;
                    }

                    string TryExtractNameRecursive(object obj, int depth)
                    {
                        if (obj == null) return null;
                        if (depth > MaxDepth) return null;
                        if (visited.Contains(obj)) return null;
                        visited.Add(obj);

                        var t = obj.GetType();

                        // 1) Сначала — прямые свойства/методы для имени
                        var nameProps = new[] { "Name", "Title", "DisplayName", "LocalizedName", "Label" };
                        foreach (var pn in nameProps)
                        {
                            try
                            {
                                var p = t.GetProperty(pn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (p != null)
                                {
                                    var v = p.GetValue(obj);
                                    if (v != null)
                                    {
                                        if (v is string s && !string.IsNullOrWhiteSpace(s)) return s;
                                        // если свойство само не строка — рекурсивно попробуем
                                        if (!IsSimple(v.GetType()))
                                        {
                                            var sub = TryExtractNameRecursive(v, depth + 1);
                                            if (!string.IsNullOrWhiteSpace(sub)) return sub;
                                        }
                                    }
                                }
                            }
                            catch { /* ignore */ }
                        }

                        // 2) Попробуем методы без параметров, возвращающие string
                        var methodNames = new[] { "GetDisplayName", "GetName", "GetLabel", "GetTitle", "ToString" };
                        foreach (var mn in methodNames)
                        {
                            try
                            {
                                var m = t.GetMethod(mn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (m != null && m.GetParameters().Length == 0)
                                {
                                    var mv = m.Invoke(obj, null);
                                    if (mv is string ms && !string.IsNullOrWhiteSpace(ms))
                                    {
                                        // игнорируем стандартный ToString возвращающий тип вида "Namespace.Type"
                                        if (!mn.Equals("ToString", StringComparison.OrdinalIgnoreCase) || !ms.Equals(t.FullName, StringComparison.OrdinalIgnoreCase))
                                            return ms;
                                    }
                                }
                            }
                            catch { }
                        }

                        // 3) Ищем внутренние объекты в свойствах (public/nonpublic)
                        var props = t.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var p in props)
                        {
                            // пропускаем простые свойства
                            if (p.GetIndexParameters().Length > 0) continue;
                            try
                            {
                                var val = p.GetValue(obj);
                                if (val == null) continue;
                                var vt = val.GetType();

                                if (IsSimple(vt)) continue;
                                // пропускаем коллекции (IEnumerable но не string)
                                if (typeof(IEnumerable).IsAssignableFrom(vt) && vt != typeof(string)) continue;

                                var sub = TryExtractNameRecursive(val, depth + 1);
                                if (!string.IsNullOrWhiteSpace(sub)) return sub;
                            }
                            catch { /* ignore property access exceptions */ }
                        }

                        // 4) Ищем внутренние объекты в полях (public/nonpublic)
                        var fields = t.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        foreach (var f in fields)
                        {
                            try
                            {
                                var val = f.GetValue(obj);
                                if (val == null) continue;
                                var vt = val.GetType();
                                if (IsSimple(vt)) continue;
                                if (typeof(IEnumerable).IsAssignableFrom(vt) && vt != typeof(string)) continue;

                                var sub = TryExtractNameRecursive(val, depth + 1);
                                if (!string.IsNullOrWhiteSpace(sub)) return sub;
                            }
                            catch { }
                        }

                        // 5) Если ничего не найдено, попробуем ToString (если даёт информативный результат)
                        try
                        {
                            var s = obj.ToString();
                            if (!string.IsNullOrWhiteSpace(s) && !s.Equals(t.FullName, StringComparison.OrdinalIgnoreCase))
                                return s;
                        }
                        catch { }

                        return null;
                    }

                    var found = TryExtractNameRecursive(invItem, 0);
                    if (!string.IsNullOrWhiteSpace(found))
                    {
                        // Если имя похоже на ключ типа "rat_meat" — гуманизируем
                        if (found.IndexOf('_') >= 0 || found.IndexOf('-') >= 0)
                            return HumanizeKey(found);
                        return found;
                    }

                    // fallback — просто имя типа без namespace
                    var typeName = invItem.GetType().Name;
                    return typeName ?? "неизвестный предмет";
                }
                catch
                {
                    return "неизвестный предмет";
                }
            }


            private void HandleNextOrClose(IDialogueUI ui)
            {
                try
                {
                    var nextNodeProp = this.GetType().GetProperty("NextNode");
                    var nextNode = nextNodeProp?.GetValue(this) as DialogueSystem.DialogueNode;
                    if (nextNode != null)
                    {
                        ui?.SetCurrentNode(nextNode);
                        return;
                    }

                    // Если торговый экран сейчас открыт — не закрываем диалог (trade screen должен оставаться поверх)
                    try
                    {
                        if (ScreenManager.CurrentScreen is Engine.UI.TradeScreen)
                        {
                            DebugConsole.Log("ExecuteSelection: TradeScreen is active — skipping CloseDialogue.");
                            return;
                        }
                    }
                    catch { /* ignore */ }

                    ui?.CloseDialogue();
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("HandleNextOrClose failed: " + ex.Message);
                }
            }

            private static string TryGetStringProperty(object obj, string propName)
            {
                try
                {
                    var p = obj.GetType().GetProperty(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (p != null)
                    {
                        var v = p.GetValue(obj);
                        return v?.ToString();
                    }
                    var f = obj.GetType().GetField(propName, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                    if (f != null)
                    {
                        var v = f.GetValue(obj);
                        return v?.ToString();
                    }
                }
                catch { }
                return null;
            }

            private static long ParseLongParam(string param)
            {
                if (string.IsNullOrWhiteSpace(param)) return 0;
                if (long.TryParse(param.Trim(), out var v)) return v;

                // если формат key:value
                var kv = ParseParamString(param);
                if (kv.ContainsKey("amount") && long.TryParse(kv["amount"], out var v2)) return v2;
                if (kv.ContainsKey("value") && long.TryParse(kv["value"], out var v3)) return v3;
                if (kv.ContainsKey("gold") && long.TryParse(kv["gold"], out var v4)) return v4;
                return 0;
            }

            // Разбирает строку параметров вида "key:val;key2=val2" или "val1,val2" -> словарь
            private static Dictionary<string, string> ParseParamString(string param)
            {
                var res = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                if (string.IsNullOrWhiteSpace(param)) return res;

                // поддерживаем разделители ; , |
                var parts = param.Split(new[] { ';', ',', '|' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var p in parts)
                {
                    var kv = p.Split(new[] { ':', '=' }, 2);
                    if (kv.Length == 2)
                    {
                        var k = kv[0].Trim();
                        var v = kv[1].Trim();
                        if (!res.ContainsKey(k)) res[k] = v;
                    }
                    else
                    {
                        var v = p.Trim();
                        if (!res.ContainsKey(v)) res[v] = "true";
                    }
                }
                return res;
            }

            private static bool TryAddGoldToPlayer(Player player, long amount)
            {
                if (player == null)
                {
                    DebugConsole.Log("TryAddGoldToPlayer: player is null");
                    return false;
                }

                try
                {
                    var pType = player.GetType();

                    // 1) Ищем public свойство (Gold/Money/Coins/Balance)
                    var propNames = new[] { "Gold", "Money", "Coins", "Balance", "Cash", "Amount" };
                    foreach (var pname in propNames)
                    {
                        var prop = pType.GetProperty(pname, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (prop != null && prop.CanRead && prop.CanWrite)
                        {
                            try
                            {
                                var cur = prop.GetValue(player);
                                long curVal = 0;
                                if (cur != null)
                                {
                                    if (cur is long) curVal = (long)cur;
                                    else if (cur is int) curVal = (int)cur;
                                    else if (!long.TryParse(cur.ToString(), out curVal)) curVal = 0;
                                }
                                var newVal = Convert.ChangeType(curVal + amount, prop.PropertyType);
                                prop.SetValue(player, newVal);
                                DebugConsole.Log($"TryAddGoldToPlayer: property '{pname}' found and updated from {curVal} to {curVal + amount}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.Log($"TryAddGoldToPlayer: failed to set property '{pname}': {ex.Message}");
                                // продолжим искать
                            }
                        }
                    }

                    // 2) Ищем публичные поля (включая приватные поля, часто _gold)
                    var fieldNames = new[] { "Gold", "_gold", "money", "_money", "coins", "_coins", "balance", "_balance" };
                    foreach (var fname in fieldNames)
                    {
                        var fld = pType.GetField(fname, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (fld != null)
                        {
                            try
                            {
                                var cur = fld.GetValue(player);
                                long curVal = 0;
                                if (cur != null)
                                {
                                    if (cur is long) curVal = (long)cur;
                                    else if (cur is int) curVal = (int)cur;
                                    else if (!long.TryParse(cur.ToString(), out curVal)) curVal = 0;
                                }

                                object newVal = Convert.ChangeType(curVal + amount, fld.FieldType);
                                fld.SetValue(player, newVal);
                                DebugConsole.Log($"TryAddGoldToPlayer: field '{fname}' found and updated from {curVal} to {curVal + amount}");
                                return true;
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.Log($"TryAddGoldToPlayer: failed to set field '{fname}': {ex.Message}");
                            }
                        }
                    }

                    // 3) Ищем методы: AddGold/AddMoney/GiveGold/ChangeMoney/AddCurrency/ModifyBalance
                    var methodNames = new[] { "AddGold", "AddMoney", "GiveGold", "GiveMoney", "ChangeMoney", "AddCurrency", "ModifyBalance", "IncreaseGold", "ReceiveMoney" };
                    foreach (var mname in methodNames)
                    {
                        var methods = pType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic)
                            .Where(m => string.Equals(m.Name, mname, StringComparison.OrdinalIgnoreCase)).ToArray();
                        foreach (var m in methods)
                        {
                            var ps = m.GetParameters();
                            try
                            {
                                if (ps.Length == 1)
                                {
                                    var pt = ps[0].ParameterType;
                                    if (pt == typeof(long) || pt == typeof(int) || pt == typeof(short) || pt == typeof(decimal) || pt == typeof(double))
                                    {
                                        m.Invoke(player, new object[] { Convert.ChangeType(amount, pt) });
                                        DebugConsole.Log($"TryAddGoldToPlayer: invoked '{mname}(amount)'.");
                                        return true;
                                    }
                                    // try object param
                                    if (pt == typeof(object))
                                    {
                                        m.Invoke(player, new object[] { amount });
                                        DebugConsole.Log($"TryAddGoldToPlayer: invoked '{mname}(object)'.");
                                        return true;
                                    }
                                }
                                else if (ps.Length == 2)
                                {
                                    // try signatures like (int amount, object something) or (object, int)
                                    object arg0 = null, arg1 = null;
                                    if (ps[0].ParameterType == typeof(string) && ps[1].ParameterType == typeof(long))
                                    {
                                        arg0 = "dialog"; arg1 = Convert.ChangeType(amount, ps[1].ParameterType);
                                    }
                                    else if ((ps[0].ParameterType == typeof(long) || ps[0].ParameterType == typeof(int)) && ps[1].ParameterType == typeof(string))
                                    {
                                        arg0 = Convert.ChangeType(amount, ps[0].ParameterType); arg1 = "dialog";
                                    }
                                    else
                                    {
                                        // если первый параметр подходит к amount
                                        if (ps[0].ParameterType == typeof(long) || ps[0].ParameterType == typeof(int))
                                        {
                                            arg0 = Convert.ChangeType(amount, ps[0].ParameterType);
                                            arg1 = Type.Missing;
                                        }
                                    }

                                    try
                                    {
                                        if (arg0 != null)
                                        {
                                            m.Invoke(player, new object[] { arg0, arg1 });
                                            DebugConsole.Log($"TryAddGoldToPlayer: invoked '{mname}' with 2 params.");
                                            return true;
                                        }
                                    }
                                    catch { /* try next */ }
                                }
                            }
                            catch (TargetParameterCountException tpc)
                            {
                                DebugConsole.Log($"TryAddGoldToPlayer: method '{m.Name}' parameter mismatch: {tpc.Message}");
                            }
                            catch (Exception ex)
                            {
                                DebugConsole.Log($"TryAddGoldToPlayer: invoking '{m.Name}' failed: {ex.Message}");
                            }
                        }
                    }

                    // 4) Попробуем найти вложенные объекты (Wallet/Account/WalletComponent/Finance/Inventory) и вызвать у них методы/свойства
                    var walletNames = new[] { "Wallet", "Account", "Finance", "Money", "WalletComponent", "Currency", "Economy" };
                    foreach (var wn in walletNames)
                    {
                        var wprop = pType.GetProperty(wn, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (wprop != null)
                        {
                            var walletObj = wprop.GetValue(player);
                            if (walletObj != null)
                            {
                                // рекурсивно попробуем применить то же самое к walletObj (пробуем найти у него AddMoney)
                                var wType = walletObj.GetType();
                                var addM = wType.GetMethod("AddMoney", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                            ?? wType.GetMethod("AddGold", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                            ?? wType.GetMethod("ChangeBalance", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (addM != null)
                                {
                                    var ps = addM.GetParameters();
                                    try
                                    {
                                        if (ps.Length == 1)
                                        {
                                            addM.Invoke(walletObj, new object[] { Convert.ChangeType(amount, ps[0].ParameterType) });
                                            DebugConsole.Log($"TryAddGoldToPlayer: invoked wallet.{addM.Name}()");
                                            return true;
                                        }
                                    }
                                    catch (Exception ex)
                                    {
                                        DebugConsole.Log($"TryAddGoldToPlayer: invoking wallet.{addM.Name} failed: {ex.Message}");
                                    }
                                }
                            }
                        }
                    }

                    // 5) Попытка: если в player есть Inventory с полем/свойством Gold/Money — записать туда
                    try
                    {
                        var invProp = pType.GetProperty("Inventory", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (invProp != null)
                        {
                            var invObj = invProp.GetValue(player);
                            if (invObj != null)
                            {
                                var invType = invObj.GetType();
                                var gprop = invType.GetProperty("Gold", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase)
                                            ?? invType.GetProperty("Money", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                                if (gprop != null && gprop.CanRead && gprop.CanWrite)
                                {
                                    var cur = gprop.GetValue(invObj);
                                    long curVal = 0;
                                    if (cur != null)
                                    {
                                        if (cur is long) curVal = (long)cur;
                                        else if (cur is int) curVal = (int)cur;
                                        else if (!long.TryParse(cur.ToString(), out curVal)) curVal = 0;
                                    }
                                    gprop.SetValue(invObj, Convert.ChangeType(curVal + amount, gprop.PropertyType));
                                    DebugConsole.Log("TryAddGoldToPlayer: added to player.Inventory." + gprop.Name);
                                    return true;
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log("TryAddGoldToPlayer: inventory money set attempt failed: " + ex.Message);
                    }

                    DebugConsole.Log("TryAddGoldToPlayer: no suitable property/field/method found to add gold.");
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryAddGoldToPlayer failed: " + ex.Message);
                }

                return false;
            }

            private static object TryCreateInventoryItem(string itemId, int qty)
            {
                try
                {
                    // сначала — доверяем GameFactory (интегрированная фабрика)
                    var gf = Engine.Core.GameServices.GameFactory;
                    if (gf != null)
                    {
                        var createdFromFactory = gf.CreateInventoryItem(itemId + "," + qty);
                        if (createdFromFactory != null) return createdFromFactory;
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log($"TryCreateInventoryItem: GameFactory.CreateInventoryItem error: {ex.Message}");
                }


                if (string.IsNullOrWhiteSpace(itemId)) return null;
                try
                {
                    // Ищем тип InventoryItem в загруженных сборках
                    var asmCandidates = AppDomain.CurrentDomain.GetAssemblies();
                    Type invItemType = null;
                    foreach (var asm in asmCandidates)
                    {
                        invItemType = asm.GetTypes().FirstOrDefault(t => t.Name.Equals("InventoryItem", StringComparison.OrdinalIgnoreCase));
                        if (invItemType != null) break;
                    }

                    // Попробуем найти фабрику ItemFactory с методом CreateInventoryItem(string id) или Create(string id)
                    foreach (var asm in asmCandidates)
                    {
                        var factoryType = asm.GetTypes().FirstOrDefault(t => t.Name.IndexOf("ItemFactory", StringComparison.OrdinalIgnoreCase) >= 0
                                                                            || t.Name.IndexOf("ItemRepository", StringComparison.OrdinalIgnoreCase) >= 0
                                                                            || t.Name.IndexOf("ItemDatabase", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (factoryType == null) continue;

                        var createMethod = factoryType.GetMethod("CreateInventoryItem", BindingFlags.Public | BindingFlags.Static)
                                          ?? factoryType.GetMethod("Create", BindingFlags.Public | BindingFlags.Static)
                                          ?? factoryType.GetMethod("GetItemById", BindingFlags.Public | BindingFlags.Static)
                                          ?? factoryType.GetMethod("Get", BindingFlags.Public | BindingFlags.Static);
                        if (createMethod == null) continue;

                        try
                        {
                            object created = null;
                            // Попробуем разные сигнатуры
                            var parameters = createMethod.GetParameters();
                            if (parameters.Length == 1)
                            {
                                created = createMethod.Invoke(null, new object[] { itemId });
                            }
                            else if (parameters.Length == 2)
                            {
                                created = createMethod.Invoke(null, new object[] { itemId, qty });
                            }
                            else
                            {
                                created = createMethod.Invoke(null, new object[] { itemId });
                            }

                            if (created != null)
                            {
                                // Если фабрика вернула InventoryItem, установим количество и вернём
                                if (invItemType != null && invItemType.IsInstanceOfType(created))
                                {
                                    var qtyProp = invItemType.GetProperty("Quantity");
                                    if (qtyProp != null && qtyProp.CanWrite)
                                        qtyProp.SetValue(created, qty);
                                    return created;
                                }

                                // Если фабрика вернула Item (не InventoryItem) — попробуем создать InventoryItem на его основе
                                var itemType = created.GetType();
                                var inventoryCtor = invItemType?.GetConstructors()
                                    .FirstOrDefault(ci =>
                                    {
                                        var ps = ci.GetParameters();
                                        return ps.Length == 2 || ps.Length == 1;
                                    });

                                if (inventoryCtor != null && invItemType != null)
                                {
                                    try
                                    {
                                        object invCreated = null;
                                        var ctorParams = inventoryCtor.GetParameters();
                                        if (ctorParams.Length == 2)
                                            invCreated = inventoryCtor.Invoke(new object[] { created, qty });
                                        else if (ctorParams.Length == 1)
                                            invCreated = inventoryCtor.Invoke(new object[] { created });
                                        if (invCreated != null)
                                        {
                                            var qtyProp2 = invCreated.GetType().GetProperty("Quantity");
                                            if (qtyProp2 != null && qtyProp2.CanWrite) qtyProp2.SetValue(invCreated, qty);
                                            return invCreated;
                                        }
                                    }
                                    catch { /* ignore constructor failures */ }
                                }

                                // Если фабрика вернула что-то пригодное в качестве InventoryItem — возвращаем
                                if (invItemType != null && invItemType.IsInstanceOfType(created)) return created;
                            }
                        }
                        catch { /* ignore factory call errors */ }
                    }

                    // Если фабрики не нашлось, попытаемся напрямую найти тип Item по id в репозитории
                    // (игнорируем — возвращаем null)
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryCreateInventoryItem failed: " + ex.Message);
                }

                return null;
            }

            private static bool TryAddInventoryItemToPlayer(Player player, object inventoryItem)
            {
                if (player == null || inventoryItem == null) return false;
                try
                {
                    // Вспомогательные локальные функции
                    object GetUnderlyingItem(object inv)
                    {
                        if (inv == null) return null;
                        var t = inv.GetType();
                        // Если это уже Item-like (имеет ID/Name), возвращаем сам объект
                        if (t.GetProperty("ID") != null || t.GetProperty("Name") != null) return inv;

                        // Ищем свойства, которые обычно хранят "вложенный" Item
                        var candProps = new[] { "Item", "BaseItem", "Definition", "Template", "Data" };
                        foreach (var pn in candProps)
                        {
                            var p = t.GetProperty(pn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            if (p != null)
                            {
                                try
                                {
                                    var val = p.GetValue(inv);
                                    if (val != null) return val;
                                }
                                catch { }
                            }
                        }
                        return null;
                    }

                    int GetQuantity(object inv)
                    {
                        if (inv == null) return 1;
                        var t = inv.GetType();
                        var qtyProps = new[] { "Quantity", "Qty", "Count", "Amount" };
                        foreach (var qn in qtyProps)
                        {
                            var qp = t.GetProperty(qn, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            if (qp != null)
                            {
                                try
                                {
                                    var val = qp.GetValue(inv);
                                    if (val != null)
                                    {
                                        if (val is int) return (int)val;
                                        if (val is long) return Convert.ToInt32(val);
                                        if (int.TryParse(val.ToString(), out var r)) return r;
                                    }
                                }
                                catch { }
                            }
                        }
                        return 1;
                    }

                    // Попытка №1: использовать player.Inventory и его методы
                    var invProp = player.GetType().GetProperty("Inventory", BindingFlags.Public | BindingFlags.Instance);
                    if (invProp != null)
                    {
                        var invObj = invProp.GetValue(player);
                        if (invObj != null)
                        {
                            // собираем кандидатов-методов с именами AddItem/Add/AddToInventory и т.д.
                            var methodNames = new[] { "AddItem", "Add", "AddToInventory", "AddInventoryItem", "GiveItem", "PutItem" };
                            var methods = invObj.GetType()
                                .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                .Where(m => methodNames.Contains(m.Name, StringComparer.OrdinalIgnoreCase))
                                .OrderBy(m => m.GetParameters().Length) // сначала простые сигнатуры
                                .ToArray();

                            // если нет явных методов — попробуем любые public методы "Add*"
                            if (methods.Length == 0)
                            {
                                methods = invObj.GetType()
                                    .GetMethods(BindingFlags.Public | BindingFlags.Instance)
                                    .Where(m => m.Name.StartsWith("Add", StringComparison.OrdinalIgnoreCase))
                                    .OrderBy(m => m.GetParameters().Length)
                                    .ToArray();
                            }

                            // Для диагностики — логируем найденные методы (однократно)
                            foreach (var m in methods)
                            {
                                var ps = m.GetParameters();
                                DebugConsole.Log($"TryAddInventory: found inv method '{m.Name}' with {ps.Length} param(s): ({string.Join(", ", ps.Select(p => p.ParameterType.Name + " " + p.Name))})");
                            }

                            var underlyingItem = GetUnderlyingItem(inventoryItem);
                            var qty = GetQuantity(inventoryItem);

                            foreach (var m in methods)
                            {
                                var ps = m.GetParameters();
                                try
                                {
                                    // Case: one-parameter methods
                                    if (ps.Length == 1)
                                    {
                                        var pType = ps[0].ParameterType;
                                        // 1) if it accepts InventoryItem directly
                                        if (pType.IsInstanceOfType(inventoryItem))
                                        {
                                            m.Invoke(invObj, new object[] { inventoryItem });
                                            return true;
                                        }
                                        // 2) if it accepts underlying Item
                                        if (underlyingItem != null && pType.IsInstanceOfType(underlyingItem))
                                        {
                                            m.Invoke(invObj, new object[] { underlyingItem });
                                            return true;
                                        }
                                        // 3) try convert via ChangeType if simple type (unlikely)
                                    }

                                    // Case: two-parameter methods
                                    if (ps.Length == 2)
                                    {
                                        var p0 = ps[0].ParameterType;
                                        var p1 = ps[1].ParameterType;

                                        // (Item, int) or (object, int)
                                        if (underlyingItem != null && (p0.IsInstanceOfType(underlyingItem) || p0 == typeof(object)))
                                        {
                                            if (p1 == typeof(int) || p1 == typeof(long) || p1 == typeof(short) || p1 == typeof(byte) || p1 == typeof(uint))
                                            {
                                                var arg1 = Convert.ChangeType(qty, p1);
                                                m.Invoke(invObj, new object[] { underlyingItem, arg1 });
                                                return true;
                                            }

                                            // (Item, Player) — rare: try (underlyingItem, player)
                                            if (p1.IsInstanceOfType(player))
                                            {
                                                m.Invoke(invObj, new object[] { underlyingItem, player });
                                                return true;
                                            }
                                        }

                                        // (InventoryItem, int) — if p0 accepts inventoryItem
                                        if (p0.IsInstanceOfType(inventoryItem))
                                        {
                                            if (p1 == typeof(int) || p1 == typeof(long) || p1 == typeof(short) || p1 == typeof(byte) || p1 == typeof(uint))
                                            {
                                                var arg1 = Convert.ChangeType(qty, p1);
                                                m.Invoke(invObj, new object[] { inventoryItem, arg1 });
                                                return true;
                                            }
                                        }

                                        // (object, object) — fallback: try to pass inv and qty as boxed types (best effort)
                                        if (p0 == typeof(object) && (p1 == typeof(object) || p1 == typeof(int)))
                                        {
                                            object second = p1 == typeof(int) ? (object)qty : (object)player;
                                            try { m.Invoke(invObj, new object[] { underlyingItem ?? inventoryItem, second }); return true; } catch { }
                                        }
                                    }

                                    // Case: three-parameter methods (rare) — try common patterns
                                    if (ps.Length == 3)
                                    {
                                        // (Item, int, bool) etc. Try to match (underlyingItem, qty, false)
                                        if (underlyingItem != null && (ps[0].ParameterType.IsInstanceOfType(underlyingItem) || ps[0].ParameterType == typeof(object)))
                                        {
                                            var args = new object[3];
                                            args[0] = underlyingItem;
                                            args[1] = Convert.ChangeType(qty, ps[1].ParameterType);
                                            // third — try default false/0/null
                                            if (ps[2].ParameterType == typeof(bool)) args[2] = false;
                                            else if (ps[2].ParameterType.IsValueType) args[2] = Activator.CreateInstance(ps[2].ParameterType);
                                            else args[2] = null;

                                            try { m.Invoke(invObj, args); return true; } catch { }
                                        }
                                    }
                                }
                                catch (TargetParameterCountException tpc)
                                {
                                    DebugConsole.Log($"TryAddInventory: method '{m.Name}' parameter count mismatch — continuing. ({tpc.Message})");
                                    // пробуем следующий метод
                                }
                                catch (Exception exInvoke)
                                {
                                    DebugConsole.Log($"TryAddInventory: invoking method '{m.Name}' failed: {exInvoke.Message}");
                                    // пробуем следующий метод
                                }
                            } // foreach method
                        }
                    }

                    // Попытка №2: найти у Player методы типа AddItem/AddToInventory/AddItemToInventory и вызвать
                    var playerMethodNames = new[] { "AddItemToInventory", "AddToInventory", "AddItem", "AddInventoryItem", "GiveItem" };
                    foreach (var name in playerMethodNames)
                    {
                        var pm = player.GetType().GetMethod(name, BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                        if (pm == null) continue;
                        var ps = pm.GetParameters();
                        DebugConsole.Log($"TryAddInventory: found player method '{pm.Name}' with {ps.Length} param(s).");
                        try
                        {
                            var underlyingItem = GetUnderlyingItem(inventoryItem);
                            var qty = GetQuantity(inventoryItem);

                            if (ps.Length == 1)
                            {
                                if (ps[0].ParameterType.IsInstanceOfType(inventoryItem))
                                {
                                    pm.Invoke(player, new object[] { inventoryItem });
                                    return true;
                                }
                                if (underlyingItem != null && ps[0].ParameterType.IsInstanceOfType(underlyingItem))
                                {
                                    pm.Invoke(player, new object[] { underlyingItem });
                                    return true;
                                }
                            }
                            else if (ps.Length == 2)
                            {
                                // (Item, int)
                                if (underlyingItem != null && (ps[0].ParameterType.IsInstanceOfType(underlyingItem) || ps[0].ParameterType == typeof(object)))
                                {
                                    if (ps[1].ParameterType == typeof(int) || ps[1].ParameterType == typeof(long))
                                    {
                                        pm.Invoke(player, new object[] { underlyingItem, Convert.ChangeType(qty, ps[1].ParameterType) });
                                        return true;
                                    }
                                }

                                // (InventoryItem, Player) etc. — try a few combos
                                if (ps[0].ParameterType.IsInstanceOfType(inventoryItem) && ps[1].ParameterType.IsInstanceOfType(player))
                                {
                                    pm.Invoke(player, new object[] { inventoryItem, player });
                                    return true;
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.Log($"TryAddInventory: invoking player method '{pm.Name}' failed: {ex.Message}");
                        }
                    }

                    // Попытка №3: если invProp/Methods не сработали, попробовать добавить в IList Items (если есть)
                    if (invProp != null)
                    {
                        var invObj = invProp.GetValue(player);
                        if (invObj != null)
                        {
                            var itemsProp = invObj.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase);
                            if (itemsProp != null)
                            {
                                var list = itemsProp.GetValue(invObj) as IList;
                                if (list != null)
                                {
                                    list.Add(inventoryItem);
                                    return true;
                                }
                            }
                        }
                    }

                    DebugConsole.Log("TryAddInventoryItemToPlayer: no suitable method found to add item to player's inventory.");
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryAddInventoryItemToPlayer failed: " + ex.Message);
                }
                return false;
            }

            private static object TryFindQuestByIdOrName(string qparam)
            {
                if (string.IsNullOrWhiteSpace(qparam)) return null;
                try
                {
                    // Попробуем найти статические репозитории/фабрики квестов
                    var asms = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var asm in asms)
                    {
                        var repoType = asm.GetTypes().FirstOrDefault(t => t.Name.IndexOf("QuestRepository", StringComparison.OrdinalIgnoreCase) >= 0
                                                                      || t.Name.IndexOf("QuestDatabase", StringComparison.OrdinalIgnoreCase) >= 0
                                                                      || t.Name.IndexOf("QuestManager", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (repoType == null) continue;

                        var getById = repoType.GetMethod("GetById", BindingFlags.Public | BindingFlags.Static)
                                     ?? repoType.GetMethod("GetQuestById", BindingFlags.Public | BindingFlags.Static)
                                     ?? repoType.GetMethod("Find", BindingFlags.Public | BindingFlags.Static);
                        if (getById != null)
                        {
                            try
                            {
                                if (int.TryParse(qparam, out var qid))
                                {
                                    var q = getById.Invoke(null, new object[] { qid });
                                    if (q != null) return q;
                                }
                                var qName = qparam;
                                var q2 = getById.Invoke(null, new object[] { qName });
                                if (q2 != null) return q2;
                            }
                            catch { }
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryFindQuestByIdOrName failed: " + ex.Message);
                }
                return null;
            }

            private static bool TryAddQuestToPlayer(Player player, object questObj)
            {
                if (player == null || questObj == null) return false;
                try
                {
                    // Получаем ID квеста из объекта
                    var idProperty = questObj.GetType().GetProperty("ID");
                    if (idProperty != null)
                    {
                        var questID = (int)idProperty.GetValue(questObj);
                        player.StartQuest(questID);
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryAddQuestToPlayer failed: " + ex.Message);
                }
                return false;
            }

            private static bool TryCompleteQuestForPlayer(Player player, object questObj)
            {
                if (player == null || questObj == null) return false;
                try
                {
                    var qlogProp = player.GetType().GetProperty("QuestLog", BindingFlags.Public | BindingFlags.Instance);
                    var qlog = qlogProp?.GetValue(player);
                    if (qlog != null)
                    {
                        var completeMethod = qlog.GetType().GetMethod("CompleteQuest", BindingFlags.Public | BindingFlags.Instance);
                        if (completeMethod != null)
                        {
                            // CompleteQuest(Quest, Player) или CompleteQuest(Quest)
                            var ps = completeMethod.GetParameters().Length;
                            if (ps == 2) completeMethod.Invoke(qlog, new object[] { questObj, player });
                            else completeMethod.Invoke(qlog, new object[] { questObj });
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryCompleteQuestForPlayer failed: " + ex.Message);
                }
                return false;
            }

            private static bool TrySetGlobalFlag(string name, string value)
            {
                if (string.IsNullOrWhiteSpace(name)) return false;
                try
                {
                    var asms = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (var asm in asms)
                    {
                        var flagsType = asm.GetTypes().FirstOrDefault(t => t.Name.IndexOf("WorldState", StringComparison.OrdinalIgnoreCase) >= 0
                                                                        || t.Name.IndexOf("GameState", StringComparison.OrdinalIgnoreCase) >= 0
                                                                        || t.Name.IndexOf("FlagsRepository", StringComparison.OrdinalIgnoreCase) >= 0);
                        if (flagsType == null) continue;

                        // Ищем статический метод SetFlag(string, object) или Set(string, object)
                        var setMethod = flagsType.GetMethod("SetFlag", BindingFlags.Public | BindingFlags.Static)
                                     ?? flagsType.GetMethod("Set", BindingFlags.Public | BindingFlags.Static);
                        if (setMethod != null)
                        {
                            object v = value;
                            if (bool.TryParse(value, out var bv)) v = bv;
                            else if (int.TryParse(value, out var iv)) v = iv;
                            setMethod.Invoke(null, new object[] { name, v });
                            return true;
                        }
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TrySetGlobalFlag failed: " + ex.Message);
                }
                return false;
            }            // Простейшая проверка условий видимости
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

            // вставьте этот public static метод в класс DialogueSystem (Engine/Dialogue/DialogueSystem.cs)
            public static void RegisterDefaultActionHandlers()
            {
                if (_defaultActionHandlersRegistered) return;
                _defaultActionHandlersRegistered = true;

                // GiveGold
                DialogueActions.RegisterHandler("GiveGold", (parameter, player, ui) =>
                {
                    try
                    {
                        var amount = ParseLongParam(parameter);
                        if (amount == 0)
                        {
                            DebugConsole.Log($"GiveGold handler: param '{parameter}' parsed as 0 — skipping.");
                            return;
                        }
                        if (player == null)
                        {
                            DebugConsole.Log("GiveGold handler: player is null");
                            return;
                        }

                        if (TryAddGoldToPlayer(player, amount))
                        {
                            DebugConsole.Log($"GiveGold handler: added {amount} gold to player.");
                            MessageSystem.AddMessage($"Добавлено золота: {amount}");
                        }
                        else
                        {
                            DebugConsole.Log($"GiveGold handler: failed to add {amount} gold to player.");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log("GiveGold handler error: " + ex.Message);
                    }
                });

                // GiveItem
                DialogueActions.RegisterHandler("GiveItem", (parameter, player, ui) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(parameter))
                        {
                            DebugConsole.Log("GiveItem handler: no parameter provided.");
                            return;
                        }
                        if (player == null)
                        {
                            DebugConsole.Log("GiveItem handler: player is null");
                            return;
                        }

                        // Разбираем параметр: поддерживаем те же правила, что и ранее в опции
                        var kv = ParseParamString(parameter);
                        string itemId = null;
                        int qty = 1;
                        if (kv.ContainsKey("itemid")) itemId = kv["itemid"];
                        else if (kv.ContainsKey("id")) itemId = kv["id"];
                        else
                        {
                            var parts = parameter.Split(new[] { ',', ';', ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
                            if (parts.Length > 0) itemId = parts[0].Trim();
                            if (parts.Length > 1 && int.TryParse(parts[1].Trim(), out var q)) qty = q;
                        }
                        if (kv.ContainsKey("qty") && int.TryParse(kv["qty"], out var q2)) qty = q2;
                        if (kv.ContainsKey("quantity") && int.TryParse(kv["quantity"], out var q3)) qty = q3;

                        if (string.IsNullOrEmpty(itemId))
                        {
                            DebugConsole.Log($"GiveItem handler: parameter '{parameter}' does not contain item id.");
                            return;
                        }

                        var invItem = TryCreateInventoryItem(itemId, qty);
                        if (invItem == null)
                        {
                            DebugConsole.Log($"GiveItem handler: factory failed to create item '{itemId}'.");
                            return;
                        }

                        if (TryAddInventoryItemToPlayer(player, invItem))
                        {
                            DebugConsole.Log($"GiveItem handler: added {itemId} x{qty} to player.");
                            var itemName = GetInventoryItemName(invItem);
                            MessageSystem.AddMessage($"Добавлено: {itemName} x{qty}");
                        }
                        else
                        {
                            DebugConsole.Log($"GiveItem handler: created item but failed to add to player inventory.");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log("GiveItem handler error: " + ex.Message);
                    }
                });

                // StartTrade
                DialogueActions.RegisterHandler("StartTrade", (parameter, player, ui) =>
                {
                    try
                    {
                        DebugConsole.Log("StartTrade handler invoked");
                        ui?.OpenTrade();
                        // не закрываем диалог — UI/ScreenManager управляет состоянием
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log("StartTrade handler error: " + ex.Message);
                    }
                });

                // StartQuest (использует TryFindQuestByIdOrName и TryAddQuestToPlayer)
                DialogueActions.RegisterHandler("StartQuest", (parameter, player, ui) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(parameter))
                        {
                            DebugConsole.Log("StartQuest handler: no parameter provided.");
                            return;
                        }
                        var questObj = TryFindQuestByIdOrName(parameter);
                        if (questObj == null)
                        {
                            DebugConsole.Log($"StartQuest handler: quest '{parameter}' not found.");
                            return;
                        }
                        if (TryAddQuestToPlayer(player, questObj))
                        {
                            DebugConsole.Log($"StartQuest handler: added quest '{parameter}' to player.");
                            MessageSystem.AddMessage($"Получено задание: {parameter}");
                        }
                        else
                        {
                            DebugConsole.Log($"StartQuest handler: failed to add quest '{parameter}'.");
                        }
                        ui?.CloseDialogue();
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log("StartQuest handler error: " + ex.Message);
                    }
                });

                // SetFlag
                DialogueActions.RegisterHandler("SetFlag", (parameter, player, ui) =>
                {
                    try
                    {
                        if (string.IsNullOrWhiteSpace(parameter))
                        {
                            DebugConsole.Log("SetFlag handler: no parameter provided.");
                            return;
                        }
                        var kv = ParseParamString(parameter);
                        string flagName = null;
                        string flagVal = "true";
                        if (kv.Count == 0)
                        {
                            flagName = parameter.Trim();
                        }
                        else
                        {
                            flagName = kv.Keys.FirstOrDefault();
                            flagVal = kv[flagName];
                        }
                        if (string.IsNullOrWhiteSpace(flagName))
                        {
                            DebugConsole.Log("SetFlag handler: no valid flag name.");
                            return;
                        }
                        if (TrySetGlobalFlag(flagName, flagVal))
                        {
                            DebugConsole.Log($"SetFlag handler: set {flagName}={flagVal}");
                        }
                        else
                        {
                            DebugConsole.Log($"SetFlag handler: failed to set {flagName}={flagVal}");
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log("SetFlag handler error: " + ex.Message);
                    }
                });
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
                                    player.StartQuest(quest.ID);
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
                                    player.StartQuest(quest.ID);
                                    DebugConsole.Log($"DialogueAction: started quest '{parameter}'");
                                }
                            }
                        }
                        ui?.CloseDialogue();
                        break;

                    case "StartTrade":
                        // Открываем торговый экран через UI; диалог не закрываем автоматически.
                        try
                        {
                            ui?.OpenTrade();
                        }
                        catch (Exception ex)
                        {
                            DebugConsole.Log($"DialogueAction: StartTrade failed: {ex.Message}");
                        }
                        break;

                    case "None":
                        // Ничего не делаем — намеренное отсутствие действия.
                        break;

                    default:
                        DebugConsole.Log($"DialogueActions: no handler for '{action}' (param='{parameter}')");
                        break;
                }
            }
        }
    }
}
