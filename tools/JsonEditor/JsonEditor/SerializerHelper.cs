// SerializerHelper.cs
// Замените существующий файл этим кодом.
// Требует Newtonsoft.Json (пакет Newtonsoft.Json)
using System;
using System.IO;
using System.Linq;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Engine.Data;

namespace JsonEditor
{
    /// <summary>
    /// SerializerHelper — безопасная реализация:
    /// - при загрузке: сохраняет "сырые" компоненты предметов (Items[].Components) в памяти и
    ///   удаляет их из JSON перед десериализацией в GameData, чтобы не пытаться инстанцировать интерфейсы;
    /// - при сохранении: берет GameData -> JObject, нормализует диалоги (Options -> Responses),
    ///   затем вставляет обратно сохранённые ранее "сырые" Components по ID предметов и записывает файл атомарно.
    /// 
    /// Дополнительно: добавлена нормализация спаунов локаций:
    /// - если в Location есть старое поле NPCsHere (массив ID), а нет NPCSpawns, то автоматически создаём NPCSpawns = [{ NPCID, Count:1 }, ...]
    /// - в существующих MonsterSpawns, если отсутствует поле Count, добавляем Count = 1
    /// Это делает переход на формат с количеством обратнo-совместимым.
    /// </summary>
    public static class SerializerHelper
    {
        // Хранилище "сырых" компонент для каждого открытого файла (ключ = полный путь к файлу)
        // Внутри: mapping itemId -> JToken (обычно JArray) с оригинальными компонентами
        private static readonly ConcurrentDictionary<string, Dictionary<int, JToken>> _rawItemComponentsByFile
            = new ConcurrentDictionary<string, Dictionary<int, JToken>>(StringComparer.OrdinalIgnoreCase);

        private static JsonSerializerSettings CreateLoadSettings()
        {
            return new JsonSerializerSettings
            {
                MissingMemberHandling = MissingMemberHandling.Ignore,
                NullValueHandling = NullValueHandling.Include,
                Formatting = Formatting.Indented,
                DateParseHandling = DateParseHandling.None
            };
        }

        private static JsonSerializerSettings CreateSaveSettings()
        {
            return new JsonSerializerSettings
            {
                NullValueHandling = NullValueHandling.Ignore,
                Formatting = Formatting.Indented
            };
        }

        /// <summary>
        /// Загружает GameData из файла.
        /// Не пытается инстанцировать IItemComponent — вместо этого сохраняет "сырой" JToken компонентов в памяти.
        /// </summary>
        public static GameData LoadGameData(string path)
        {
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));
            if (!File.Exists(path)) throw new FileNotFoundException("File not found", path);

            string text = File.ReadAllText(path);
            JToken rootToken;
            try
            {
                rootToken = JToken.Parse(text);
            }
            catch (Exception ex)
            {
                throw new Exception($"Ошибка парсинга JSON: {ex.Message}", ex);
            }

            if (!(rootToken is JObject rootObj))
            {
                // если файл — не объект (маловероятно для game_data.json), попытаемся десериализовать напрямую
                try
                {
                    var gd = JsonConvert.DeserializeObject<GameData>(text, CreateLoadSettings());
                    // очищаем любые ранее сохранённые raw-components для этого файла
                    _rawItemComponentsByFile.TryRemove(Path.GetFullPath(path), out _);
                    return gd;
                }
                catch (JsonException jex)
                {
                    throw new Exception($"Ошибка десериализации GameData: {jex.Message}", jex);
                }
            }

            // 1) Сохраняем original Items[].Components в памяти (по ID предметов), а затем удаляем поле Components
            SaveAndStripItemComponents(rootObj, path);

            // 2) Нормализация диалогов: Options -> Responses единый формат
            TryNormalizeDialoguesJsonToResponses(rootObj);

            // 3) Нормализация спаунов локаций (обратная совместимость): NPCsHere -> NPCSpawns, добавить Count в MonsterSpawns
            TryNormalizeLocationSpawns(rootObj);

            // 4) Теперь десериализуем модифицированный JSON в GameData (без попытки инстанцировать компонентов)
            try
            {
                var gd = JsonConvert.DeserializeObject<GameData>(rootObj.ToString(Formatting.Indented), CreateLoadSettings());
                return gd;
            }
            catch (JsonException jex)
            {
                throw new Exception($"Ошибка десериализации GameData: {jex.Message}", jex);
            }
        }

        /// <summary>
        /// Сохраняет GameData в файл. Перед записью:
        /// - конвертирует Options->Responses,
        /// - вставляет обратно сохранённые raw Components (Items[].Components) по ID,
        /// - очищает null/пустые значения,
        /// - пишет атомарно с .bak.
        /// </summary>
        public static void SaveGameData(GameData gameData, string path)
        {
            if (gameData == null) throw new ArgumentNullException(nameof(gameData));
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException(nameof(path));

            // Сериализуем GameData в JObject
            var serializer = JsonSerializer.Create(CreateSaveSettings());
            var root = JObject.FromObject(gameData, serializer);

            // Нормализуем диалоги в единый формат Responses
            TryNormalizeDialoguesJsonToResponses(root);

            // Восстанавливаем raw Components (если были сохранены при загрузке)
            RestoreRawItemComponentsIntoRoot(root, path);

            // Нормализуем локации перед сохранением (убедиться, что Count присутствует в MonsterSpawns при необходимости)
            TryNormalizeLocationSpawns(root);

            // Очистка пустых/null полей
            CleanJToken(root);

            // Атомарная запись
            var tmpPath = path + ".tmp";
            var bakPath = path + ".bak";
            try
            {
                File.WriteAllText(tmpPath, root.ToString(Formatting.Indented));

                if (File.Exists(path))
                {
                    if (File.Exists(bakPath)) File.Delete(bakPath);
                    try
                    {
                        File.Replace(tmpPath, path, bakPath);
                    }
                    catch (PlatformNotSupportedException)
                    {
                        // fallback для платформ без File.Replace
                        File.Copy(path, bakPath, true);
                        File.Copy(tmpPath, path, true);
                        File.Delete(tmpPath);
                    }
                }
                else
                {
                    File.Move(tmpPath, path);
                }
            }
            catch
            {
                if (File.Exists(tmpPath))
                {
                    try { File.Delete(tmpPath); } catch { }
                }
                throw;
            }
        }

        // -------------------------
        // Вспомогательные: сохранение/восстановление компонентов предметов
        // -------------------------

        private static void SaveAndStripItemComponents(JObject rootObj, string path)
        {
            try
            {
                var itemsProp = rootObj.Properties().FirstOrDefault(p => string.Equals(p.Name, "Items", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);
                if (itemsProp == null)
                {
                    // нет предметов — удаляем возможные записи
                    _rawItemComponentsByFile.TryRemove(Path.GetFullPath(path), out _);
                    return;
                }

                var itemsArray = (JArray)itemsProp.Value;
                var dict = new Dictionary<int, JToken>();

                foreach (var itemToken in itemsArray.OfType<JObject>())
                {
                    // ищем ID (регистронезависимо)
                    var idProp = itemToken.Properties().FirstOrDefault(p => string.Equals(p.Name, "ID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
                    if (idProp == null) continue;

                    if (!int.TryParse(idProp.Value.ToString(), out int itemId)) continue;

                    // ищем Components
                    var compProp = itemToken.Properties().FirstOrDefault(p => string.Equals(p.Name, "Components", StringComparison.OrdinalIgnoreCase));
                    if (compProp != null && compProp.Value != null && compProp.Value.Type != JTokenType.Null)
                    {
                        // сохраняем исходный JToken (DeepClone чтобы не ссылаться на оригинал)
                        dict[itemId] = compProp.Value.DeepClone();

                        // затем удаляем свойство Components из JSON, чтобы десериализация в GameData не пыталась инстанцировать интерфейсы
                        compProp.Remove();
                    }
                }

                // сохраняем map (перезаписываем предыдущий, если был)
                var fullPath = Path.GetFullPath(path);
                if (dict.Count > 0)
                    _rawItemComponentsByFile[fullPath] = dict;
                else
                    _rawItemComponentsByFile.TryRemove(fullPath, out _);
            }
            catch
            {
                // В случае неожиданных ошибок — не прерываем загрузку, просто не сохраняем компоненты
                try { _rawItemComponentsByFile.TryRemove(Path.GetFullPath(path), out _); } catch { }
            }
        }

        private static void RestoreRawItemComponentsIntoRoot(JObject rootObj, string path)
        {
            try
            {
                var fullPath = Path.GetFullPath(path);
                if (!_rawItemComponentsByFile.TryGetValue(fullPath, out var dict) || dict == null || dict.Count == 0) return;

                var itemsProp = rootObj.Properties().FirstOrDefault(p => string.Equals(p.Name, "Items", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);
                if (itemsProp == null) return;
                var itemsArray = (JArray)itemsProp.Value;

                foreach (var itemToken in itemsArray.OfType<JObject>())
                {
                    var idProp = itemToken.Properties().FirstOrDefault(p => string.Equals(p.Name, "ID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
                    if (idProp == null) continue;
                    if (!int.TryParse(idProp.Value.ToString(), out int itemId)) continue;

                    if (dict.TryGetValue(itemId, out var rawComponents))
                    {
                        // вставляем rawComponents (DeepClone)
                        itemToken["Components"] = rawComponents.DeepClone();
                    }
                }
            }
            catch
            {
                // ignore on failure — лучше записать что-то, чем упасть
            }
        }

        // -------------------------
        // Нормализация диалогов (Options -> Responses) — единый формат Responses в JSON
        // -------------------------
        private static void TryNormalizeDialoguesJsonToResponses(JObject root)
        {
            if (root == null) return;

            JArray dialoguesArray = null;

            // находим свойство-массив, которое похоже на Dialogues
            foreach (var p in root.Properties())
            {
                if (p.Value.Type != JTokenType.Array) continue;
                var arr = (JArray)p.Value;
                if (!arr.Any()) continue;
                var first = arr.OfType<JObject>().FirstOrDefault();
                if (first == null) continue;
                if (first.Properties().Any(pp => string.Equals(pp.Name, "Nodes", StringComparison.OrdinalIgnoreCase)))
                {
                    dialoguesArray = arr;
                    break;
                }
            }

            if (dialoguesArray == null) return;

            foreach (var dlg in dialoguesArray.OfType<JObject>())
            {
                var nodesProp = dlg.Properties().FirstOrDefault(pp => string.Equals(pp.Name, "Nodes", StringComparison.OrdinalIgnoreCase));
                if (nodesProp == null || nodesProp.Value.Type != JTokenType.Array) continue;
                var nodesArray = (JArray)nodesProp.Value;

                foreach (var node in nodesArray.OfType<JObject>())
                {
                    var responsesProp = node.Properties().FirstOrDefault(pp => string.Equals(pp.Name, "Responses", StringComparison.OrdinalIgnoreCase));
                    var optionsProp = node.Properties().FirstOrDefault(pp => string.Equals(pp.Name, "Options", StringComparison.OrdinalIgnoreCase));

                    JArray responsesArray = responsesProp?.Value as JArray;
                    JArray optionsArray = optionsProp?.Value as JArray;

                    // если нет options — ничего не делаем.
                    if (optionsArray == null || optionsArray.Count == 0)
                    {
                        // при этом, если Responses существует и пустой или null — удаляем его (чтобы не сохранять null)
                        if (responsesProp != null && (responsesArray == null || responsesArray.Count == 0))
                        {
                            responsesProp.Remove();
                        }
                        continue;
                    }

                    // Создаём новый список ответов: сначала старые Responses (если были), затем опции
                    var merged = new JArray();

                    if (responsesArray != null)
                    {
                        foreach (var r in responsesArray.OfType<JObject>())
                        {
                            merged.Add(NormalizeResponseObject(r));
                        }
                    }

                    foreach (var opt in optionsArray.OfType<JObject>())
                    {
                        var resp = ConvertOptionToResponse(opt);
                        if (resp != null) merged.Add(resp);
                    }

                    if (merged.Count > 0)
                    {
                        // удаляем старое Responses (разных регистров)
                        var oldRespProp = node.Properties().FirstOrDefault(pp => string.Equals(pp.Name, "Responses", StringComparison.OrdinalIgnoreCase));
                        oldRespProp?.Remove();

                        node["Responses"] = merged;
                    }

                    // Удаляем Options (мы договорились хранить единый формат)
                    optionsProp?.Remove();
                }
            }
        }

        private static JObject NormalizeResponseObject(JObject r)
        {
            var res = new JObject();

            var textProp = r.Properties().FirstOrDefault(p => string.Equals(p.Name, "Text", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "text", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "ResponseText", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Title", StringComparison.OrdinalIgnoreCase));
            if (textProp != null && textProp.Value != null && textProp.Value.Type != JTokenType.Null) res["text"] = textProp.Value;

            var targetProp = r.Properties().FirstOrDefault(p => string.Equals(p.Name, "Target", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Next", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "NextNodeId", StringComparison.OrdinalIgnoreCase));
            if (targetProp != null && targetProp.Value != null && targetProp.Value.Type != JTokenType.Null) res["target"] = targetProp.Value;

            var actionsProp = r.Properties().FirstOrDefault(p => string.Equals(p.Name, "Actions", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Action", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Param", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Parameter", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Value", StringComparison.OrdinalIgnoreCase));
            if (actionsProp != null && actionsProp.Value != null && actionsProp.Value.Type != JTokenType.Null)
            {
                if (actionsProp.Value.Type == JTokenType.Array) res["actions"] = actionsProp.Value;
                else res["actions"] = new JArray(actionsProp.Value);
            }

            var extras = new[] { "Id", "Value", "Condition", "Parameter", "Param", "Type" };
            foreach (var ex in extras)
            {
                var exProp = r.Properties().FirstOrDefault(p => string.Equals(p.Name, ex, StringComparison.OrdinalIgnoreCase));
                if (exProp != null && exProp.Value != null && exProp.Value.Type != JTokenType.Null)
                {
                    var key = ex.ToLowerInvariant();
                    if (!res.ContainsKey(key)) res[key] = exProp.Value;
                }
            }

            return res;
        }

        private static JObject ConvertOptionToResponse(JObject opt)
        {
            if (opt == null) return null;
            var res = new JObject();

            var textProp = opt.Properties().FirstOrDefault(p => string.Equals(p.Name, "Text", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Title", StringComparison.OrdinalIgnoreCase));
            if (textProp != null && textProp.Value != null && textProp.Value.Type != JTokenType.Null) res["text"] = textProp.Value;

            var targetProp = opt.Properties().FirstOrDefault(p => string.Equals(p.Name, "NextNodeId", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Next", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Target", StringComparison.OrdinalIgnoreCase));
            if (targetProp != null && targetProp.Value != null && targetProp.Value.Type != JTokenType.Null) res["target"] = targetProp.Value;

            var actionsProp = opt.Properties().FirstOrDefault(p => string.Equals(p.Name, "Actions", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Action", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Param", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Parameter", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Value", StringComparison.OrdinalIgnoreCase));
            if (actionsProp != null && actionsProp.Value != null && actionsProp.Value.Type != JTokenType.Null)
            {
                if (actionsProp.Value.Type == JTokenType.Array) res["actions"] = actionsProp.Value;
                else res["actions"] = new JArray(actionsProp.Value);
            }

            var extras = new[] { "Id", "Value", "Condition", "Parameter", "Param", "Type" };
            foreach (var ex in extras)
            {
                var exProp = opt.Properties().FirstOrDefault(p => string.Equals(p.Name, ex, StringComparison.OrdinalIgnoreCase));
                if (exProp != null && exProp.Value != null && exProp.Value.Type != JTokenType.Null)
                {
                    var key = ex.ToLowerInvariant();
                    if (!res.ContainsKey(key)) res[key] = exProp.Value;
                }
            }

            if (res.Properties().Any()) return res;
            return null;
        }

        // -------------------------
        // Нормализация локаций (обратная совместимость со старым форматом)
        // -------------------------
        /// <summary>
        /// Если у локации есть NPCsHere (список int) и нет NPCSpawns — создаём NPCSpawns с Count = 1.
        /// В существующих MonsterSpawns добавляем Count = 1, если поле отсутствует.
        /// </summary>
        private static void TryNormalizeLocationSpawns(JObject root)
        {
            if (root == null) return;

            var locationsProp = root.Properties().FirstOrDefault(p => string.Equals(p.Name, "Locations", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);
            if (locationsProp == null) return;

            var locationsArray = (JArray)locationsProp.Value;
            foreach (var locToken in locationsArray.OfType<JObject>())
            {
                try
                {
                    // --- NPCsHere -> NPCSpawns ---
                    var hasNpcSpawns = locToken.Properties().Any(p => string.Equals(p.Name, "NPCSpawns", StringComparison.OrdinalIgnoreCase));
                    var npcHereProp = locToken.Properties().FirstOrDefault(p => string.Equals(p.Name, "NPCsHere", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);

                    if (!hasNpcSpawns && npcHereProp != null)
                    {
                        var arr = (JArray)npcHereProp.Value;
                        var spawns = new JArray();
                        foreach (var idToken in arr)
                        {
                            if (idToken == null) continue;
                            if (!int.TryParse(idToken.ToString(), out int id)) continue;
                            var spawnObj = new JObject
                            {
                                ["NPCID"] = id,
                                ["Count"] = 1
                            };
                            spawns.Add(spawnObj);
                        }
                        if (spawns.Count > 0)
                        {
                            locToken["NPCSpawns"] = spawns;
                        }
                    }

                    // --- MonsterSpawns: добавить Count=1 если отсутствует ---
                    var mspProp = locToken.Properties().FirstOrDefault(p => string.Equals(p.Name, "MonsterSpawns", StringComparison.OrdinalIgnoreCase) && p.Value.Type == JTokenType.Array);
                    if (mspProp != null)
                    {
                        var marr = (JArray)mspProp.Value;
                        foreach (var spawn in marr.OfType<JObject>())
                        {
                            if (!spawn.Properties().Any(p => string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase)))
                            {
                                spawn["Count"] = 1;
                            }
                        }
                    }
                }
                catch
                {
                    // в нормализации локаций ошибки не должны ломать загрузку — игнорируем отдельные проблемы
                    continue;
                }
            }
        }

        // -------------------------
        // Очистка JSON от null/пустых значений
        // -------------------------
        private static void CleanJToken(JToken token)
        {
            if (token is JObject obj)
            {
                var props = obj.Properties().ToList();
                foreach (var p in props)
                {
                    CleanJToken(p.Value);

                    if (p.Value.Type == JTokenType.Null)
                    {
                        p.Remove();
                        continue;
                    }
                    if (p.Value.Type == JTokenType.Array && !p.Value.HasValues)
                    {
                        p.Remove();
                        continue;
                    }
                    if (p.Value.Type == JTokenType.Object && !p.Value.HasValues)
                    {
                        p.Remove();
                        continue;
                    }
                    if (p.Value.Type == JTokenType.String && string.IsNullOrWhiteSpace(p.Value.ToString()))
                    {
                        // по умолчанию удаляем пустые строки; если нужно сохранить — удалите эту ветку
                        p.Remove();
                        continue;
                    }
                }
            }
            else if (token is JArray arr)
            {
                foreach (var item in arr.ToList())
                {
                    CleanJToken(item);
                    if (item.Type == JTokenType.Null) arr.Remove(item);
                    else if (item.Type == JTokenType.Object && !item.HasValues) arr.Remove(item);
                    else if (item.Type == JTokenType.Array && !item.HasValues) arr.Remove(item);
                    else if (item.Type == JTokenType.String && string.IsNullOrWhiteSpace(item.ToString())) arr.Remove(item);
                }
            }
        }
    }
}
