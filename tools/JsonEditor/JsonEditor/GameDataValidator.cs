using System.Collections.Generic;
using System.Linq;
using Engine.Data;

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
                foreach (var qi in q.QuestItems ?? Enumerable.Empty<QuestItemData>())
                {
                    if (!itemIds.Contains(qi.ItemID)) errors.Add($"Quest {q.ID} references missing Item {qi.ItemID}");
                }
            }
        }

        if (data.Locations != null)
        {
            foreach (var loc in data.Locations)
            {
                foreach (var nid in loc.NPCsHere ?? Enumerable.Empty<int>())
                {
                    if (!data.NPCs.Any(n => n.ID == nid)) errors.Add($"Location {loc.ID} references missing NPC {nid}");
                }
            }
        }

        // другие проверки: monster templates, title requirements и т.д.

        return errors;
    }
}
