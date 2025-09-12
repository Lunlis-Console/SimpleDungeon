using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using DialogueAction = Engine.Dialogue.DialogueAction;
using DialogueDocument = Engine.Dialogue.DialogueDocument;
using Response = Engine.Dialogue.Response;

namespace SimpleDungeon.Tools.DialogueEditor
{
    public class EditResponseForm : Form
    {
        private readonly DialogueDocument _doc;
        private readonly Response _editing;
        private TextBox _text;
        private ComboBox _targetCombo;
        private ListView _actionsList;
        private Button _addActionBtn;
        private Button _editActionBtn;
        private Button _delActionBtn;
        private Button _ok;
        private Button _cancel;

        public Response Result { get; private set; }

        public EditResponseForm(DialogueDocument doc, Response response)
        {
            _doc = doc;
            _editing = response;
            Initialize();
            LoadData();
        }

        private void Initialize()
        {
            Text = "Edit Response";
            Width = 600;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            var lbText = new Label { Left = 10, Top = 10, Text = "Text", Width = 80 };
            Controls.Add(lbText);
            _text = new TextBox { Left = 10, Top = 30, Width = 560 };
            Controls.Add(_text);

            var lbTarget = new Label { Left = 10, Top = 60, Text = "Target (node id or empty)", Width = 200 };
            Controls.Add(lbTarget);
            _targetCombo = new ComboBox { Left = 10, Top = 80, Width = 400, DropDownStyle = ComboBoxStyle.DropDown };
            Controls.Add(_targetCombo);

            _actionsList = new ListView { Left = 10, Top = 120, Width = 560, Height = 200, View = View.Details, FullRowSelect = true };
            _actionsList.Columns.Add("Type", 200);
            _actionsList.Columns.Add("Param", 300);
            Controls.Add(_actionsList);

            _addActionBtn = new Button { Left = 10, Top = 330, Width = 80, Text = "Add" };
            _addActionBtn.Click += (s, e) => AddAction();
            Controls.Add(_addActionBtn);

            _editActionBtn = new Button { Left = 100, Top = 330, Width = 80, Text = "Edit" };
            _editActionBtn.Click += (s, e) => EditAction();
            Controls.Add(_editActionBtn);

            _delActionBtn = new Button { Left = 190, Top = 330, Width = 80, Text = "Del" };
            _delActionBtn.Click += (s, e) => DeleteAction();
            Controls.Add(_delActionBtn);

            _ok = new Button { Left = 350, Top = 330, Width = 100, Text = "OK" };
            _ok.Click += (s, e) => { SaveAndClose(); };
            Controls.Add(_ok);

            _cancel = new Button { Left = 460, Top = 330, Width = 100, Text = "Cancel" };
            _cancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };
            Controls.Add(_cancel);
        }

        private void LoadData()
        {
            _targetCombo.Items.Clear();
            _targetCombo.Items.Add("(null)");
            foreach (var n in _doc.Nodes) _targetCombo.Items.Add(n.Id);

            if (_editing != null)
            {
                _text.Text = _editing.Text;
                _targetCombo.SelectedItem = _editing.Target ?? "(null)";
                if (_editing.Actions != null)
                {
                    foreach (var a in _editing.Actions)
                    {
                        var it = new ListViewItem(a.Type ?? "(none)");
                        it.SubItems.Add(a.Param ?? "");
                        it.Tag = a;
                        _actionsList.Items.Add(it);
                    }
                }
            }
            else
            {
                _targetCombo.SelectedIndex = 0;
            }
        }

        private void AddAction()
        {
            var form = new EditActionForm(null);
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                var a = form.Result;
                var it = new ListViewItem(a.Type ?? "(none)");
                it.SubItems.Add(a.Param ?? "");
                it.Tag = a;
                _actionsList.Items.Add(it);
            }
        }

        private void EditAction()
        {
            if (_actionsList.SelectedItems.Count == 0) return;
            var it = _actionsList.SelectedItems[0];
            var a = it.Tag as DialogueAction;
            var form = new EditActionForm(new DialogueAction { Type = a.Type, Param = a.Param });
            if (form.ShowDialog(this) == DialogResult.OK)
            {
                var na = form.Result;
                it.Text = na.Type ?? "(none)";
                it.SubItems[0].Text = na.Param ?? "";
                it.Tag = na;
            }
        }

        private void DeleteAction()
        {
            if (_actionsList.SelectedItems.Count == 0) return;
            var it = _actionsList.SelectedItems[0];
            _actionsList.Items.Remove(it);
        }

        private void SaveAndClose()
        {
            var resp = _editing ?? new Response();
            resp.Text = _text.Text;
            var sel = _targetCombo.SelectedItem?.ToString();
            resp.Target = (sel == null || sel == "(null)") ? null : sel;
            resp.Actions = new List<DialogueAction>();
            foreach (ListViewItem it in _actionsList.Items)
            {
                if (it.Tag is DialogueAction da)
                    resp.Actions.Add(da);
            }
            Result = resp;
            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
