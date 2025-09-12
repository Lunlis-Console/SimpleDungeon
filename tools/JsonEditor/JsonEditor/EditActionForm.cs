using System;
using System.Windows.Forms;
using DialogueAction = Engine.Dialogue.DialogueAction;

namespace SimpleDungeon.Tools.DialogueEditor
{
    public class EditActionForm : Form
    {
        private TextBox _typeBox;
        private TextBox _paramBox;
        private Button _ok;
        private Button _cancel;

        public DialogueAction Result { get; private set; }

        public EditActionForm(DialogueAction action)
        {
            Text = action == null ? "New Action" : "Edit Action";
            Width = 400;
            Height = 180;
            StartPosition = FormStartPosition.CenterParent;

            var lbType = new Label { Left = 10, Top = 10, Text = "Type (e.g. GiveGold, StartTrade)", Width = 360 };
            Controls.Add(lbType);
            _typeBox = new TextBox { Left = 10, Top = 30, Width = 360 };
            Controls.Add(_typeBox);

            var lbParam = new Label { Left = 10, Top = 60, Text = "Param (optional)", Width = 360 };
            Controls.Add(lbParam);
            _paramBox = new TextBox { Left = 10, Top = 80, Width = 360 };
            Controls.Add(_paramBox);

            _ok = new Button { Left = 200, Top = 110, Width = 80, Text = "OK" };
            _ok.Click += (s, e) => { SaveAndClose(); };
            Controls.Add(_ok);

            _cancel = new Button { Left = 290, Top = 110, Width = 80, Text = "Cancel" };
            _cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_cancel);

            if (action != null)
            {
                _typeBox.Text = action.Type;
                _paramBox.Text = action.Param;
            }
        }

        private void SaveAndClose()
        {
            if (string.IsNullOrWhiteSpace(_typeBox.Text))
            {
                MessageBox.Show("Type required (e.g. GiveGold, StartQuest)");
                return;
            }
            Result = new DialogueAction { Type = _typeBox.Text.Trim(), Param = string.IsNullOrWhiteSpace(_paramBox.Text) ? null : _paramBox.Text.Trim() };
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
