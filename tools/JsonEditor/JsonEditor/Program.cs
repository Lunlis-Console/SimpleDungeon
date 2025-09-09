using Engine.Data;

namespace JsonEditor
{
    internal static class Program
    {
        /// <summary>
        ///  The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            ApplicationConfiguration.Initialize(); // или Application.EnableVisualStyles(); если .NET старый
            Application.Run(new MainForm());
        }
    }
}