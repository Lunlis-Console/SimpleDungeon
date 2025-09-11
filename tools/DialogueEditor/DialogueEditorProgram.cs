using System;
using System.Windows.Forms;

namespace SimpleDungeon.Tools.DialogueEditor
{
    static class DialogueEditorProgram
    {
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new DialogueEditorForm());
        }
    }
}
