// Engine/Data/ItemComponentConverter.cs
using System;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Entities;

namespace Engine.Data
{
    public class ItemComponentConverter : JsonConverter<IItemComponent>
    {
        public override IItemComponent Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            using var doc = JsonDocument.ParseValue(ref reader);
            var root = doc.RootElement;

            if (!root.TryGetProperty("ComponentType", out var ctProp))
                throw new JsonException("Component JSON must contain 'ComponentType'.");

            var type = ctProp.GetString();
            return type switch
            {
                "Heal" => JsonSerializer.Deserialize<HealComponent>(root.GetRawText(), options)!,
                "Damage" => JsonSerializer.Deserialize<DamageComponent>(root.GetRawText(), options)!,
                "Buff" => JsonSerializer.Deserialize<BuffComponent>(root.GetRawText(), options)!,
                "Equip" => JsonSerializer.Deserialize<EquipComponent>(root.GetRawText(), options)!,
                "Lockpick" => JsonSerializer.Deserialize<LockpickComponent>(root.GetRawText(), options)!,
                _ => throw new NotSupportedException($"Unknown component type: {type}")
            };
        }

        public override void Write(Utf8JsonWriter writer, IItemComponent value, JsonSerializerOptions options)
        {
            switch (value)
            {
                case HealComponent h: JsonSerializer.Serialize(writer, h, options); break;
                case DamageComponent d: JsonSerializer.Serialize(writer, d, options); break;
                case BuffComponent b: JsonSerializer.Serialize(writer, b, options); break;
                case EquipComponent eq: JsonSerializer.Serialize(writer, eq, options); break;
                case LockpickComponent lp: JsonSerializer.Serialize(writer, lp, options); break;

                default: throw new NotSupportedException($"Cannot serialize component type {value.GetType().Name}");
            }
        }
    }
}
