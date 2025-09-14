using Engine.Data;
using Engine.Quests;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Engine.World
{
    public static class UniqueIdHelper
    {
        /// <summary>
        /// Проверяет все коллекции GameData на уникальность ID.
        /// Возвращает список ошибок и логирует найденные дубли.
        /// </summary>
        public static List<string> ValidateUniqueIds(GameData data)
        {
            var errors = new List<string>();

            if (data == null)
            {
                errors.Add("GameData is null");
                return errors;
            }

            // Проверка верхнего уровня (ID)
            CheckDuplicates("Item", data.Items?.Select(i => i.ID), errors);
            CheckDuplicates("Monster", data.Monsters?.Select(m => m.ID), errors);
            CheckDuplicates("NPC", data.NPCs?.Select(n => n.ID), errors);
            CheckDuplicates("Location", data.Locations?.Select(l => l.ID), errors);
            CheckDuplicates("Quest", data.Quests?.Select(q => q.ID), errors);
            CheckDuplicates("Title", data.Titles?.Select(t => t.ID), errors);

            // Дополнительные проверки на внутренние дубликаты
            ValidateMonsterLootTables(data, errors);
            ValidateLocationSpawns(data, errors);
            ValidateQuestItems(data, errors);

            return errors;
        }

        public static void AutoFixInternalDuplicates(GameData data)
        {
            if (data == null) return;

            DebugConsole.Log("[AutoFixInternalDuplicates] Запуск автофикса...");

            // Фикс LootTable у монстров
            if (data.Monsters != null)
            {
                foreach (var monster in data.Monsters)
                {
                    if (monster.LootTable == null) continue;

                    var seen = new HashSet<int>();
                    var toRemove = new List<LootItemData>();

                    foreach (var loot in monster.LootTable)
                    {
                        if (!seen.Add(loot.ItemID))
                        {
                            DebugConsole.Log($"[AutoFixInternalDuplicates] Удалён дубль ItemID={loot.ItemID} в LootTable монстра {monster.ID} \"{monster.Name}\"");
                            toRemove.Add(loot);
                        }
                    }

                    foreach (var r in toRemove)
                        monster.LootTable.Remove(r);
                }
            }

            // Фикс MonsterSpawns у локаций
            if (data.Locations != null)
            {
                foreach (var loc in data.Locations)
                {
                    if (loc.MonsterSpawns == null) continue;

                    var seen = new HashSet<(int monsterId, int level, int weight)>();
                    var toRemove = new List<MonsterSpawnData>();

                    foreach (var spawn in loc.MonsterSpawns)
                    {
                        var key = (spawn.MonsterTemplateID, spawn.Level, spawn.SpawnWeight);
                        if (!seen.Add(key))
                        {
                            DebugConsole.Log($"[AutoFixInternalDuplicates] Удалён дубль MonsterTemplateID={spawn.MonsterTemplateID} (Level={spawn.Level}, SpawnWeight={spawn.SpawnWeight}) в Location {loc.ID} \"{loc.Name}\"");
                            toRemove.Add(spawn);
                        }
                    }

                    foreach (var r in toRemove)
                        loc.MonsterSpawns.Remove(r);
                }
            }

            // Фикс предметов в квестах (CompletionItems и RewardItems → InventoryItemData)
            if (data.Quests != null)
            {
                foreach (var quest in data.Quests)
                {
                    // Проверка наград
                    if (quest.Rewards?.Items != null)
                    {
                        var seen = new HashSet<int>();
                        var toRemove = new List<QuestRewardItem>(); // Изменён тип

                        foreach (var reward in quest.Rewards.Items)
                        {
                            if (!seen.Add(reward.ItemID))
                            {
                                DebugConsole.Log($"[AutoFixInternalDuplicates] Удалён дубль ItemID={reward.ItemID} в Rewards.Items квеста {quest.ID} \"{quest.Name}\"");
                                toRemove.Add(reward); // Теперь типы совпадают
                            }
                        }

                        foreach (var r in toRemove)
                            quest.Rewards.Items.Remove(r); // Теперь типы совпадают
                    }
                }
            }

            DebugConsole.Log("[AutoFixInternalDuplicates] Автофикс завершён");
        }


        private static void ValidateQuestItems(GameData data, List<string> errors)
        {
            if (data.Quests == null) return;

            foreach (var quest in data.Quests)
            {
                // Проверяем условия квеста на предмет дубликатов ItemID (только CollectItemsCondition)
                if (quest.Conditions != null)
                {
                    var itemConditions = quest.Conditions.OfType<CollectItemsCondition>();
                    var dupItems = itemConditions
                        .GroupBy(c => c.ItemID)
                        .Where(g => g.Count() > 1);

                    foreach (var dup in dupItems)
                    {
                        string msg = $"Quest {quest.ID} \"{quest.Name}\" содержит дубликаты ItemID={dup.Key} в условиях (count={dup.Count()})";
                        errors.Add(msg);
                        DebugConsole.Log("[ValidateUniqueIds] " + msg);
                    }
                }

                // Проверяем награды на предмет дубликатов ItemID
                if (quest.Rewards?.Items != null)
                {
                    var dupRewards = quest.Rewards.Items
                        .GroupBy(r => r.ItemID)
                        .Where(g => g.Count() > 1);

                    foreach (var dup in dupRewards)
                    {
                        string msg = $"Quest {quest.ID} \"{quest.Name}\" содержит дубликаты ItemID={dup.Key} в Rewards.Items (count={dup.Count()})";
                        errors.Add(msg);
                        DebugConsole.Log("[ValidateUniqueIds] " + msg);
                    }
                }
            }
        }
        private static void ValidateLocationSpawns(GameData data, List<string> errors)
        {
            if (data.Locations == null) return;

            foreach (var loc in data.Locations)
            {
                if (loc.MonsterSpawns == null) continue;

                var duplicates = loc.MonsterSpawns
                    .GroupBy(s => new { s.MonsterTemplateID, s.Level, s.SpawnWeight })
                    .Where(g => g.Count() > 1);

                foreach (var dup in duplicates)
                {
                    string msg = $"Location {loc.ID} \"{loc.Name}\" содержит дубликаты MonsterTemplateID={dup.Key.MonsterTemplateID} (Level={dup.Key.Level}, SpawnWeight={dup.Key.SpawnWeight}) — count={dup.Count()}";
                    errors.Add(msg);
                    DebugConsole.Log("[ValidateUniqueIds] " + msg);
                }
            }
        }

        private static void ValidateMonsterLootTables(GameData data, List<string> errors)
        {
            if (data.Monsters == null) return;

            foreach (var monster in data.Monsters)
            {
                if (monster.LootTable == null) continue;

                var duplicates = monster.LootTable
                    .GroupBy(l => l.ItemID)
                    .Where(g => g.Count() > 1);

                foreach (var dup in duplicates)
                {
                    string msg = $"Monster {monster.ID} \"{monster.Name}\" содержит дубликаты ItemID={dup.Key} в LootTable (count={dup.Count()})";
                    errors.Add(msg);
                    DebugConsole.Log("[ValidateUniqueIds] " + msg);
                }
            }
        }

        /// <summary>
        /// Универсальная проверка коллекции ID.
        /// </summary>
        private static void CheckDuplicates(string category, IEnumerable<int> ids, List<string> errors)
        {
            if (ids == null) return;

            var idList = ids.ToList();
            if (!idList.Any()) return;

            var groups = idList
                .GroupBy(id => id)
                .Where(g => g.Count() > 1);

            foreach (var g in groups)
            {
                string msg = $"Duplicate {category} ID={g.Key} встречается {g.Count()} раз";
                errors.Add(msg);
                DebugConsole.Log($"[ValidateUniqueIds] {msg}");
            }

            DebugConsole.Log($"[ValidateUniqueIds] {category}: total={idList.Count}, unique={idList.Distinct().Count()}");
        }

        /// <summary>
        /// Автоматически исправляет дубликаты, выдавая новые ID.
        /// Возвращает карту старых->новых ID по типам.
        /// </summary>
        public static Dictionary<string, Dictionary<int, int>> FixDuplicateIds(GameData data)
        {
            var result = new Dictionary<string, Dictionary<int, int>>();

            if (data == null) return result;

            result["Item"] = FixDuplicatesForList(data.Items, NextFreeId);
            result["Monster"] = FixDuplicatesForList(data.Monsters, NextFreeId);
            result["NPC"] = FixDuplicatesForList(data.NPCs, NextFreeId);
            result["Location"] = FixDuplicatesForList(data.Locations, NextFreeId);
            result["Quest"] = FixDuplicatesForList(data.Quests, NextFreeId);
            result["Title"] = FixDuplicatesForList(data.Titles, NextFreeId);

            return result;
        }

        /// <summary>
        /// Обновляет дублирующиеся ID в списке.
        /// </summary>
        private static Dictionary<int, int> FixDuplicatesForList<T>(IList<T> list, Func<IEnumerable<int>, int> nextFreeIdFunc)
        {
            var map = new Dictionary<int, int>();
            if (list == null || !list.Any()) return map;

            var idProp = typeof(T).GetProperty("ID") ?? typeof(T).GetProperty("Id");
            if (idProp == null) return map;

            var usedIds = new HashSet<int>();
            foreach (var item in list)
            {
                int id = (int)idProp.GetValue(item);

                if (usedIds.Contains(id))
                {
                    int newId = nextFreeIdFunc(usedIds);
                    idProp.SetValue(item, newId);
                    map[id] = newId;
                    usedIds.Add(newId);
                    DebugConsole.Log($"[FixDuplicateIds] {typeof(T).Name}: заменён ID {id} → {newId}");
                }
                else
                {
                    usedIds.Add(id);
                }
            }

            return map;
        }

        /// <summary>
        /// Находит следующий свободный ID.
        /// </summary>
        private static int NextFreeId(IEnumerable<int> existingIds)
        {
            int candidate = 1;
            var set = new HashSet<int>(existingIds);
            while (set.Contains(candidate)) candidate++;
            return candidate;
        }
    }
}
