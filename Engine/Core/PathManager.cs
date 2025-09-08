// PathManager.cs
namespace Engine.Core
{
    public static class PathManager
    {
        public static string GameDataPath =>
            Path.Combine(Directory.GetCurrentDirectory(), "Data", "game_data.json");

        public static string SavesDirectory =>
            Path.Combine(Directory.GetCurrentDirectory(), "Saves");

        public static void EnsureDirectoriesExist()
        {
            Directory.CreateDirectory(Path.GetDirectoryName(GameDataPath));
            Directory.CreateDirectory(SavesDirectory);
        }
    }
}