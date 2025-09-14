using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Engine.Quests
{
    /// <summary>
    /// Конвертер для десериализации различных типов условий квестов
    /// </summary>
    public class QuestConditionConverter : JsonConverter<QuestCondition>
    {
        public override void WriteJson(JsonWriter writer, QuestCondition value, JsonSerializer serializer)
        {
            var jObject = new JObject();
            jObject["id"] = value.ID;
            jObject["description"] = value.Description;
            jObject["requiredAmount"] = value.RequiredAmount;
            jObject["currentProgress"] = value.CurrentProgress;

            // Добавляем специфичные поля в зависимости от типа
            switch (value)
            {
                case CollectItemsCondition collectCondition:
                    jObject["itemID"] = collectCondition.ItemID;
                    break;
                case KillMonstersCondition killCondition:
                    jObject["monsterID"] = killCondition.MonsterID;
                    break;
                case VisitLocationCondition visitCondition:
                    jObject["locationID"] = visitCondition.LocationID;
                    break;
                case TalkToNPCCondition talkCondition:
                    jObject["npcID"] = talkCondition.NPCID;
                    break;
                case ReachLevelCondition levelCondition:
                    jObject["requiredLevel"] = levelCondition.RequiredLevel;
                    break;
            }

            jObject.WriteTo(writer);
        }

        public override QuestCondition ReadJson(JsonReader reader, Type objectType, QuestCondition existingValue, bool hasExistingValue, JsonSerializer serializer)
        {
            var jObject = JObject.Load(reader);

            var id = jObject["id"]?.Value<int>() ?? 0;
            var description = jObject["description"]?.Value<string>() ?? "";
            var requiredAmount = jObject["requiredAmount"]?.Value<int>() ?? 1;
            var currentProgress = jObject["currentProgress"]?.Value<int>() ?? 0;

            // Определяем тип условия по наличию специфичных полей
            if (jObject["itemID"] != null)
            {
                var itemID = jObject["itemID"].Value<int>();
                var condition = new CollectItemsCondition(id, description, itemID, requiredAmount);
                condition.CurrentProgress = currentProgress;
                return condition;
            }
            else if (jObject["monsterID"] != null)
            {
                var monsterID = jObject["monsterID"].Value<int>();
                var condition = new KillMonstersCondition(id, description, monsterID, requiredAmount);
                condition.CurrentProgress = currentProgress;
                return condition;
            }
            else if (jObject["locationID"] != null)
            {
                var locationID = jObject["locationID"].Value<int>();
                var condition = new VisitLocationCondition(id, description, locationID);
                condition.CurrentProgress = currentProgress;
                return condition;
            }
            else if (jObject["npcID"] != null)
            {
                var npcID = jObject["npcID"].Value<int>();
                var condition = new TalkToNPCCondition(id, description, npcID);
                condition.CurrentProgress = currentProgress;
                return condition;
            }
            else if (jObject["requiredLevel"] != null)
            {
                var requiredLevel = jObject["requiredLevel"].Value<int>();
                var condition = new ReachLevelCondition(id, description, requiredLevel);
                condition.CurrentProgress = currentProgress;
                return condition;
            }

            // По умолчанию создаем условие сбора предметов
            return new CollectItemsCondition(id, description, 0, requiredAmount);
        }
    }
}
