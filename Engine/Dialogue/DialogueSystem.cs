// Engine/Dialogue/DialogueSystem.cs
using Engine.Data;
using Engine.Entities;
using Engine.World;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

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
                        // Читаем string Type и string Param (в соответствии с твоей моделью DialogueModels.DialogueAction)
                        string typeStr = TryGetStringProperty(actionObj, "Type") ?? TryGetStringProperty(actionObj, "type");
                        string paramStr = TryGetStringProperty(actionObj, "Param") ?? TryGetStringProperty(actionObj, "param");

                        if (string.IsNullOrWhiteSpace(typeStr))
                        {
                            DebugConsole.Log("ExecuteSelection: action.Type is empty -> skipping.");
                            continue;
                        }

                        var t = typeStr.Trim().ToLowerInvariant();

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
                                    if (amount == 0)
                                    {
                                        DebugConsole.Log($"GiveGold: param '{paramStr}' parsed as 0 — skipping.");
                                        break;
                                    }
                                    if (!TryAddGoldToPlayer(player, amount))
                                        DebugConsole.Log($"GiveGold: failed to add gold {amount} to player.");
                                    else
                                        DebugConsole.Log($"GiveGold: added {amount} gold to player.");
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
                                        var parts = paramStr.Split(new[] { ',', ';' }, StringSplitOptions.RemoveEmptyEntries);
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
                                            DebugConsole.Log($"GiveItem: added {itemId} x{qty} to player.");
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
                try
                {
                    // Ищем свойство Gold
                    var goldProp = player.GetType().GetProperty("Gold", BindingFlags.Public | BindingFlags.Instance);
                    if (goldProp != null && goldProp.CanRead && goldProp.CanWrite)
                    {
                        var cur = Convert.ToInt64(goldProp.GetValue(player) ?? 0);
                        goldProp.SetValue(player, cur + amount);
                        return true;
                    }

                    // Ищем метод AddGold(long) или AddMoney(int/long)
                    var addMethod = player.GetType().GetMethod("AddGold", BindingFlags.Public | BindingFlags.Instance)
                                    ?? player.GetType().GetMethod("AddMoney", BindingFlags.Public | BindingFlags.Instance)
                                    ?? player.GetType().GetMethod("GiveGold", BindingFlags.Public | BindingFlags.Instance);
                    if (addMethod != null)
                    {
                        var pCount = addMethod.GetParameters().Length;
                        if (pCount == 1) addMethod.Invoke(player, new object[] { Convert.ChangeType(amount, addMethod.GetParameters()[0].ParameterType) });
                        else addMethod.Invoke(player, new object[] { amount, null });
                        return true;
                    }
                }
                catch (Exception ex)
                {
                    DebugConsole.Log("TryAddGoldToPlayer failed: " + ex.Message);
                }
                return false;
            }

            private static object TryCreateInventoryItem(string itemId, int qty)
            {
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
                    // Ищем player.Inventory и методы добавления
                    var invProp = player.GetType().GetProperty("Inventory", BindingFlags.Public | BindingFlags.Instance);
                    if (invProp != null)
                    {
                        var invObj = invProp.GetValue(player);
                        if (invObj != null)
                        {
                            var addMethod = invObj.GetType().GetMethod("AddItem", BindingFlags.Public | BindingFlags.Instance)
                                            ?? invObj.GetType().GetMethod("Add", BindingFlags.Public | BindingFlags.Instance);
                            if (addMethod != null)
                            {
                                addMethod.Invoke(invObj, new object[] { inventoryItem });
                                return true;
                            }

                            var itemsProp = invObj.GetType().GetProperty("Items", BindingFlags.Public | BindingFlags.Instance);
                            if (itemsProp != null)
                            {
                                var list = itemsProp.GetValue(invObj) as IList;
                                list?.Add(inventoryItem);
                                return true;
                            }
                        }
                    }

                    // Попробуем найти метод у Player: AddToInventory(InventoryItem)
                    var addToInv = player.GetType().GetMethod("AddToInventory", BindingFlags.Public | BindingFlags.Instance)
                                    ?? player.GetType().GetMethod("AddItem", BindingFlags.Public | BindingFlags.Instance);
                    if (addToInv != null)
                    {
                        addToInv.Invoke(player, new object[] { inventoryItem });
                        return true;
                    }
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
                    var qlogProp = player.GetType().GetProperty("QuestLog", BindingFlags.Public | BindingFlags.Instance);
                    var qlog = qlogProp?.GetValue(player);
                    if (qlog != null)
                    {
                        var addMethod = qlog.GetType().GetMethod("AddQuest", BindingFlags.Public | BindingFlags.Instance);
                        if (addMethod != null)
                        {
                            addMethod.Invoke(qlog, new object[] { questObj });
                            return true;
                        }
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
