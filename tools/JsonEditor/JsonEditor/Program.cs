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
            ApplicationConfiguration.Initialize(); // ��� Application.EnableVisualStyles(); ���� .NET ������
            Application.Run(new MainForm());
        }
    }
}