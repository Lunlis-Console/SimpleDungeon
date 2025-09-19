// MonsterInspectScreen.cs — стилизован под TitlesScreen
using Engine.Core;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Text;

namespace Engine.UI
{
    public class MonsterInspectScreen : BaseScreen
    {
        private readonly object _monster;
        private int _selectedLootIndex = 0;
        private bool _lootFocused = true;

        public MonsterInspectScreen(object monster)
        {
            _monster = monster ?? throw new ArgumentNullException(nameof(monster));
        }

        public override void Update()
        {
            // Нет активной анимации — логика ввода в HandleInput
        }

        public override void HandleInput(ConsoleKeyInfo keyInfo)
        {
            var lootCount = GetLootList().Count;

            switch (keyInfo.Key)
            {
                case ConsoleKey.Escape:
                case ConsoleKey.Q:
                    // Закрываем экран осмотра
                    ScreenManager.PopScreen();

                    try
                    {
                        // Даём шанс движку обновиться
                        ScreenManager.RequestPartialRedraw();

                        // Получаем текущий (только что восстановленный) экран
                        var current = typeof(ScreenManager).GetProperty("CurrentScreen", BindingFlags.Public | BindingFlags.Static)?
                                          .GetValue(null);

                        if (current != null)
                        {
                            // 1) Если у экрана есть метод ReopenActionMenuForMonster(object) — вызываем его
                            var reopenMethod = current.GetType().GetMethod("ReopenActionMenuForMonster",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                null, new Type[] { typeof(object) }, null);

                            if (reopenMethod != null)
                            {
                                reopenMethod.Invoke(current, new object[] { _monster });
                                return;
                            }

                            // 2) Если есть метод ShowActionMenu(string, List<string>, Action<string>) — попытаемся вызвать
                            var showMethod = current.GetType().GetMethod("ShowActionMenu",
                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                            if (showMethod != null)
                            {
                                // попытаемся вызвать универсально: формируем заголовок + минимальный набор действий
                                string title = "Взаимодействие";
                                try
                                {
                                    var nameProp = _monster.GetType().GetProperty("Name");
                                    if (nameProp != null) title = $"Взаимодействие с {nameProp.GetValue(_monster)}";
                                }
                                catch { }

                                var actions = new List<string> { "Атаковать", "Осмотреть", "Назад" };
                                // ищем перегрузку с параметрами (string, List<string>, Action<string>) — если есть, вызываем её
                                foreach (var mi in current.GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic))
                                {
                                    var ps = mi.GetParameters();
                                    if (ps.Length == 3
                                        && ps[0].ParameterType == typeof(string)
                                        && (ps[1].ParameterType == typeof(List<string>) || ps[1].ParameterType.IsAssignableFrom(typeof(List<string>)))
                                        && typeof(Action<string>).IsAssignableFrom(ps[2].ParameterType))
                                    {
                                        // подготовим универсальный callback — он будет вызывать метод обработки в current, если такой есть,
                                        // иначе просто логировать выбор.
                                        Action<string> cb = (sel) =>
                                        {
                                            // если current имеет метод HandleInteractionSelection(string, object) — вызовем его
                                            var handle = current.GetType().GetMethod("HandleInteractionSelection",
                                                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic,
                                                null, new Type[] { typeof(string), typeof(object) }, null);
                                            if (handle != null)
                                            {
                                                handle.Invoke(current, new object[] { sel, _monster });
                                            }
                                            else
                                            {
                                                DebugConsole.Log($"[осмотр->меню] выбрано: {sel}");
                                            }
                                        };

                                        mi.Invoke(current, new object[] { title, actions, cb });
                                        return;
                                    }
                                }
                            }
                        }

                        // 3) Запасной вариант: попытка найти тип InteractionScreen и создать новый экран с монстром
                        var asm = Assembly.GetExecutingAssembly();
                        var interactionType = asm.GetType("Engine.InteractionScreen") ?? asm.GetType("SimpleDungeon.InteractionScreen") ?? asm.GetType("InteractionScreen");
                        if (interactionType != null)
                        {
                            // ищем конструктор, принимающий один параметр, совместимый с типом _monster
                            ConstructorInfo ctor = null;
                            foreach (var c in interactionType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                            {
                                var ps = c.GetParameters();
                                if (ps.Length == 1 && ps[0].ParameterType.IsAssignableFrom(_monster.GetType()))
                                {
                                    ctor = c;
                                    break;
                                }
                            }

                            if (ctor == null)
                            {
                                // иначе ищем конструктор с object/без параметров
                                foreach (var c in interactionType.GetConstructors(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance))
                                {
                                    var ps = c.GetParameters();
                                    if (ps.Length == 0 || ps.Length == 1 && ps[0].ParameterType == typeof(object))
                                    {
                                        ctor = c;
                                        break;
                                    }
                                }
                            }

                            if (ctor != null)
                            {
                                object instance = null;
                                var pcount = ctor.GetParameters().Length;
                                if (pcount == 0) instance = ctor.Invoke(null);
                                else instance = ctor.Invoke(new object[] { _monster });

                                if (instance != null)
                                {
                                    ScreenManager.PushScreen((BaseScreen)instance);
                                    ScreenManager.RequestFullRedraw();
                                }
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        DebugConsole.Log($"[осмотр->меню] попытка повторного открытия не удалась: {ex.GetType().Name}: {ex.Message}");
                    }

                    return;
            }

            ScreenManager.RequestPartialRedraw();
            GameServices.BufferedRenderer?.SetNeedsFullRedraw();
        }

        public override void Render()
        {
            // Используем тот же стиль что и TitlesScreen
            ClearScreen();

            RenderHeader("ОСМОТР МОНСТРА");

            // Колонки: левая — информация, правая — лут
            int left = 2;
            int top = 4;
            int mid = Console.WindowWidth / 2;
            int right = mid + 2;

            // Получаем данные
            string name = SafeGetStringProp("Name") ?? $"Монстр#{SafeGetIntProp("ID", 0)}";
            int level = SafeGetIntProp("Level", 0);
            int curHp = SafeGetIntProp("CurrentHP", 0);
            int maxHp = SafeGetIntProp("MaximumHP", 0);
            int rewardExp = SafeGetIntProp("RewardEXP", 0);
            int rewardGold = SafeGetIntProp("RewardGold", 0);
            string description = SafeGetStringProp("Description") ?? $"Это {name}. Уровень {level}.";

            // Левая колонка — заголовок и базовая инфа
            _renderer.Write(left, top, $"=== {name} ===", ConsoleColor.Yellow);
            _renderer.Write(left, top + 2, $"Уровень: {level}");
            _renderer.Write(left, top + 3, $"HP: {curHp}/{maxHp}", ConsoleColor.Cyan);
            _renderer.Write(left, top + 4, $"EXP: {rewardExp}  Gold: {rewardGold}");
            _renderer.Write(left, top + 6, "Атрибуты:", ConsoleColor.Cyan);

            int ay = top + 7;
            foreach (var kv in EnumerateAttributes(GetMonsterProperty("Attributes")))
            {
                _renderer.Write(left + 2, ay, $"{kv.Key}: {kv.Value}");
                ay++;
                if (ay > top + 12) break; // ограничение места
            }

            // Описание (в левой колонке внизу)
            var descLines = WrapText(description, mid - left - 4);
            int dy = top + 14;
            foreach (var line in descLines)
            {
                _renderer.Write(left, dy, line);
                dy++;
                if (dy > Console.WindowHeight - 6) break;
            }

            // Правая колонка — лут
            _renderer.Write(right, top, "Лут:", ConsoleColor.Yellow);

            var loot = GetLootList();
            if (loot.Count == 0)
            {
                _renderer.Write(right + 2, top + 2, "- нет -", ConsoleColor.DarkGray);
            }
            else
            {
                int ly = top + 2;
                for (int i = 0; i < loot.Count && ly < Console.WindowHeight - 4; i++, ly++)
                {
                    var li = loot[i];
                    string qty = li.Quantity > 0 ? $" x{li.Quantity}" : "";
                    string chance = li.DropPercentage >= 0 ? $" ({li.DropPercentage}%)" : "";
                    string text = $"{SafeGetItemName(li.ItemID)}{qty}{chance}";

                    if (i == _selectedLootIndex && _lootFocused)
                    {
                        // стиль выделения как в TitlesScreen: стрелка + зелёный
                        _renderer.Write(right, ly, "► ", ConsoleColor.Green);
                        _renderer.Write(right + 2, ly, text, ConsoleColor.Green);
                    }
                    else
                    {
                        _renderer.Write(right + 2, ly, text);
                    }
                }
            }

            // Footer / подсказка (в стиле TitlesScreen)
            RenderFooter("W/S - выбор │ E - подробности │ Tab - переключение │ Q/Esc - назад");
        }

        #region Helpers (reflection-safe + utils)

        private object GetMonsterProperty(string propertyName)
        {
            try
            {
                var t = _monster.GetType();
                var p = t.GetProperty(propertyName);
                if (p != null) return p.GetValue(_monster);
                var f = t.GetField(propertyName);
                if (f != null) return f.GetValue(_monster);
            }
            catch { }
            return null;
        }

        private int SafeGetIntProp(string propName, int def)
        {
            var v = GetMonsterProperty(propName);
            if (v == null) return def;
            try { return Convert.ToInt32(v); } catch { return def; }
        }

        private string SafeGetStringProp(string propName)
        {
            var v = GetMonsterProperty(propName);
            return v?.ToString();
        }

        private IEnumerable<KeyValuePair<string, object>> EnumerateAttributes(object attributes)
        {
            if (attributes == null) yield break;

            if (attributes is IDictionary dict)
            {
                foreach (DictionaryEntry de in dict)
                    yield return new KeyValuePair<string, object>(de.Key?.ToString() ?? "<key?>", de.Value);
                yield break;
            }

            if (attributes is IEnumerable en)
            {
                foreach (var item in en)
                {
                    if (item == null) continue;
                    var itType = item.GetType();
                    var keyProp = itType.GetProperty("Key") ?? itType.GetProperty("Name") ?? itType.GetProperty("AttributeName");
                    var valProp = itType.GetProperty("Value") ?? itType.GetProperty("Amount") ?? itType.GetProperty("Number");
                    if (keyProp != null && valProp != null)
                    {
                        var k = keyProp.GetValue(item)?.ToString() ?? "<key?>";
                        var v = valProp.GetValue(item);
                        yield return new KeyValuePair<string, object>(k, v);
                        continue;
                    }

                    if (item is object[] arr && arr.Length >= 2)
                    {
                        yield return new KeyValuePair<string, object>(arr[0]?.ToString() ?? "<key?>", arr[1]);
                        continue;
                    }
                }
                yield break;
            }

            yield break;
        }

        private class LootView { public int ItemID; public double DropPercentage = -1; public int Quantity = 0; }

        private List<LootView> GetLootList()
        {
            var outList = new List<LootView>();
            var lootObj = GetMonsterProperty("LootTable") ?? GetMonsterProperty("Loot");
            if (lootObj == null) return outList;
            if (!(lootObj is IEnumerable lootEn)) return outList;

            foreach (var l in lootEn)
            {
                if (l == null) continue;
                try
                {
                    int itemId = 0;
                    double chance = -1;
                    int qty = 0;
                    var lt = l.GetType();

                    var detProp = lt.GetProperty("Details");
                    if (detProp != null)
                    {
                        var det = detProp.GetValue(l);
                        if (det != null)
                        {
                            var idProp = det.GetType().GetProperty("ID");
                            if (idProp != null) itemId = Convert.ToInt32(idProp.GetValue(det));
                        }
                    }

                    if (itemId == 0 && lt.GetProperty("ItemID") != null)
                    {
                        var idv = lt.GetProperty("ItemID").GetValue(l);
                        if (idv != null) itemId = Convert.ToInt32(idv);
                    }

                    var dp = lt.GetProperty("DropPercentage");
                    if (dp != null) { var dv = dp.GetValue(l); if (dv != null) chance = Convert.ToDouble(dv); }

                    var qp = lt.GetProperty("Quantity");
                    if (qp != null) { var qv = qp.GetValue(l); if (qv != null) qty = Convert.ToInt32(qv); }

                    if (itemId != 0) outList.Add(new LootView { ItemID = itemId, DropPercentage = chance, Quantity = qty });
                }
                catch { /* ignore problematic loot entries */ }
            }

            return outList;
        }

        private LootView GetLootAt(int index)
        {
            var list = GetLootList();
            if (index < 0 || index >= list.Count) return null;
            return list[index];
        }

        private string SafeGetItemName(int id)
        {
            try
            {
                var repo = GameServices.WorldRepository;
                if (repo == null) return $"Item#{id}";
                var rt = repo.GetType();
                var getById = rt.GetMethod("GetItemByID");
                if (getById != null)
                {
                    var itemObj = getById.Invoke(repo, new object[] { id });
                    if (itemObj != null)
                    {
                        var nameProp = itemObj.GetType().GetProperty("Name");
                        if (nameProp != null) return nameProp.GetValue(itemObj)?.ToString() ?? $"Item#{id}";
                    }
                }

                var getAll = rt.GetMethod("GetAllItems");
                if (getAll != null)
                {
                    var all = getAll.Invoke(repo, null) as IEnumerable;
                    if (all != null)
                    {
                        foreach (var it in all)
                        {
                            if (it == null) continue;
                            var ip = it.GetType().GetProperty("ID");
                            if (ip == null) continue;
                            var idv = ip.GetValue(it);
                            if (idv == null) continue;
                            if (Convert.ToInt32(idv) == id)
                            {
                                var nameProp = it.GetType().GetProperty("Name");
                                if (nameProp != null) return nameProp.GetValue(it)?.ToString() ?? $"Item#{id}";
                            }
                        }
                    }
                }
            }
            catch { /* ignore */ }
            return $"Item#{id}";
        }

        private List<string> WrapText(string text, int maxWidth)
        {
            var res = new List<string>();
            if (string.IsNullOrEmpty(text)) return res;
            var words = text.Split(' ');
            var sb = new StringBuilder();
            foreach (var w in words)
            {
                if (sb.Length + w.Length + 1 > maxWidth)
                {
                    res.Add(sb.ToString());
                    sb.Clear();
                }
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(w);
            }
            if (sb.Length > 0) res.Add(sb.ToString());
            return res;
        }

        #endregion
    }
}
