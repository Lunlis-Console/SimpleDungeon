using Engine.Data;
using Engine.Quests;
using System.Collections.Generic;
using System.Linq;

public static class GameDataValidator
{
    public static List<string> Validate(GameData data)
    {
        var errors = new List<string>();
        var itemIds = new HashSet<int>(data.Items?.Select(i => i.ID) ?? Enumerable.Empty<int>());

        if (data.Items != null)
        {
            var dup = data.Items.GroupBy(i => i.ID).Where(g => g.Count() > 1).Select(g => g.Key);
            foreach (var d in dup) errors.Add($"Duplicate item ID: {d}");
        }

        if (data.Quests != null)
        {
            foreach (var q in data.Quests)
            {
                // Проверяем условия квеста, которые могут требовать предметы
                foreach (var condition in q.Conditions ?? Enumerable.Empty<QuestConditionData>())
                {
                    // Проверяем тип условия и соответствующий TargetID
                    if (condition.Type == "CollectItems")
                    {
                        if (!itemIds.Contains(condition.TargetID))
                            errors.Add($"Quest {q.ID} condition references missing Item {condition.TargetID}");
                    }

                    // Добавьте проверки для других типов условий, если нужно
                    if (condition.Type == "KillMonsters")
                    {
                        // Проверка существования монстра (если есть данные о монстрах)
                        // if (!monsterIds.Contains(condition.TargetID))
                        //     errors.Add($"Quest {q.ID} condition references missing Monster {condition.TargetID}");
                    }
                }

                // Также проверяем награды
                foreach (var rewardItem in q.Rewards?.Items ?? Enumerable.Empty<QuestRewardItem>())
                {
                    if (!itemIds.Contains(rewardItem.ItemID))
                        errors.Add($"Quest {q.ID} reward references missing Item {rewardItem.ItemID}");
                }
            }
        }

        if (data.Locations != null)
        {
            foreach (var loc in data.Locations)
            {
                foreach (var nid in loc.NPCsHere ?? Enumerable.Empty<int>())
                {
                    if (!data.NPCs.Any(n => n.ID == nid))
                        errors.Add($"Location {loc.ID} references missing NPC {nid}");
                }
            }
        }

        return errors;
    }
}