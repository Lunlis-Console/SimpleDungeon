using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Engine.Data
{
    public class ItemTypeConverter : JsonConverter<Engine.Core.ItemType>
    {
        public override Engine.Core.ItemType Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
        {
            if (reader.TokenType == JsonTokenType.String)
            {
                var s = reader.GetString()?.Trim();
                if (string.IsNullOrEmpty(s)) return Engine.Core.ItemType.Stuff; // fallback

                // Попробуем прямой парсинг (игнор регистра)
                if (Enum.TryParse<Engine.Core.ItemType>(s, true, out var result))
                    return result;

                // Небольшие синонимы/нормализация
                var lowered = s.ToLowerInvariant();
                return lowered switch
                {
                    "helmet" => Engine.Core.ItemType.Helmet,
                    "helm" => Engine.Core.ItemType.Helmet,
                    "armor" => Engine.Core.ItemType.Armor,
                    "armour" => Engine.Core.ItemType.Armor,
                    "onehandedweapon" => Engine.Core.ItemType.OneHandedWeapon,
                    "onehanded" => Engine.Core.ItemType.OneHandedWeapon,
                    "onehand" => Engine.Core.ItemType.OneHandedWeapon,
                    "twohandedweapon" => Engine.Core.ItemType.TwoHandedWeapon,
                    "twohanded" => Engine.Core.ItemType.TwoHandedWeapon,
                    "weapon" => Engine.Core.ItemType.OneHandedWeapon,
                    "consumable" => Engine.Core.ItemType.Consumable,
                    "stuff" => Engine.Core.ItemType.Stuff,
                    "ring" => Engine.Core.ItemType.Ring,
                    "amulet" => Engine.Core.ItemType.Amulet,
                    "lockpick" => Engine.Core.ItemType.Lockpick,
                    "tool" => Engine.Core.ItemType.Tool,
                    "container" => Engine.Core.ItemType.Container,
                    _ => Engine.Core.ItemType.Stuff // безопасный дефолт — подставь свой
                };
            }

            // если приходит не строка — пытаемся прочитать как число
            if (reader.TokenType == JsonTokenType.Number && reader.TryGetInt32(out var v))
            {
                return (Engine.Core.ItemType)v;
            }

            // fallback
            return Engine.Core.ItemType.Stuff;
        }

        public override void Write(Utf8JsonWriter writer, Engine.Core.ItemType value, JsonSerializerOptions options)
        {
            writer.WriteStringValue(value.ToString());
        }
    }
}
