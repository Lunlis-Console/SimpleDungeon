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
            DebugConsole.Log("QuestConditionConverter вызван!");

            var jObject = JObject.Load(reader);

            var id = jObject["id"]?.Value<int>() ?? 0;
            var description = jObject["description"]?.Value<string>() ?? "";
            var requiredAmount = jObject["requiredAmount"]?.Value<int>() ?? 1;
            var currentProgress = jObject["currentProgress"]?.Value<int>() ?? 0;

            QuestCondition condition = null;

            // Определяем тип условия по наличию специфичных полей
            if (jObject["itemID"] != null)
            {
                var itemID = jObject["itemID"].Value<int>();
                condition = new CollectItemsCondition(); // ← Используем конструктор по умолчанию
                ((CollectItemsCondition)condition).ItemID = itemID;
            }
            else if (jObject["monsterID"] != null)
            {
                var monsterID = jObject["monsterID"].Value<int>();
                condition = new KillMonstersCondition(); // ← Используем конструктор по умолчанию
                ((KillMonstersCondition)condition).MonsterID = monsterID;
            }
            else if (jObject["locationID"] != null)
            {
                var locationID = jObject["locationID"].Value<int>();
                condition = new VisitLocationCondition(); // ← Используем конструктор по умолчанию
                ((VisitLocationCondition)condition).LocationID = locationID;
            }
            else if (jObject["npcID"] != null)
            {
                var npcID = jObject["npcID"].Value<int>();
                condition = new TalkToNPCCondition(); // ← Используем конструктор по умолчанию
                ((TalkToNPCCondition)condition).NPCID = npcID;
            }
            else if (jObject["requiredLevel"] != null)
            {
                var requiredLevel = jObject["requiredLevel"].Value<int>();
                condition = new ReachLevelCondition(); // ← Используем конструктор по умолчанию
                ((ReachLevelCondition)condition).RequiredLevel = requiredLevel;
            }

            if (condition != null)
            {
                // Устанавливаем общие свойства
                condition.ID = id;
                condition.Description = description;
                condition.RequiredAmount = requiredAmount;
                condition.CurrentProgress = currentProgress;
                return condition;
            }

            // По умолчанию создаем условие сбора предметов
            var defaultCondition = new CollectItemsCondition();
            defaultCondition.ID = id;
            defaultCondition.Description = description;
            defaultCondition.RequiredAmount = requiredAmount;
            defaultCondition.CurrentProgress = currentProgress;
            return defaultCondition;
        }
    }
}
