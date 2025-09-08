using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
// Используй namespace DTO из твоего Engine; заменяй если нужно:
using Engine.Data; // <- проверь фактический namespace в твоем Engine

public static class SerializerHelper
{
    public static readonly JsonSerializerOptions JsonOptions = new JsonSerializerOptions
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    public static GameData LoadGameData(string path)
    {
        var json = File.ReadAllText(path);
        return JsonSerializer.Deserialize<GameData>(json, JsonOptions) ?? new GameData();
    }

    public static void SaveGameData(GameData data, string path)
    {
        // бэкап
        if (File.Exists(path))
        {
            var bak = path + ".bak";
            File.Copy(path, bak, overwrite: true);
        }

        var json = JsonSerializer.Serialize(data, JsonOptions);
        File.WriteAllText(path, json);
    }
}
