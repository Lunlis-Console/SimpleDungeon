using System.IO;
using System.Text.Json;

namespace JsonEditor
{
    public class EditorConfig
    {
        public string JsonPath { get; set; }

        private static string ConfigFile => Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "JsonEditor.config");

        public static EditorConfig Load()
        {
            if (File.Exists(ConfigFile))
            {
                var json = File.ReadAllText(ConfigFile);
                return JsonSerializer.Deserialize<EditorConfig>(json);
            }
            return new EditorConfig();
        }

        public void Save()
        {
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(ConfigFile, json);
        }
    }
}
