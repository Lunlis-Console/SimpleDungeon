using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Engine.Data;

namespace JsonEditor
{
    public static class SerializerHelper
    {
        private static JsonSerializerOptions CreateOptions()
        {
            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true,
                WriteIndented = true
            };

            // Подключаем конвертеры
            options.Converters.Add(new ItemComponentConverter());
            options.Converters.Add(new JsonStringEnumConverter());

            return options;
        }

        public static GameData LoadGameData(string path)
        {
            string json = File.ReadAllText(path);
            var options = CreateOptions();
            return JsonSerializer.Deserialize<GameData>(json, options);
        }

        public static void SaveGameData(GameData gameData, string path)
        {
            var options = CreateOptions();
            string json = JsonSerializer.Serialize(gameData, options);
            File.WriteAllText(path, json);
        }
    }
}
