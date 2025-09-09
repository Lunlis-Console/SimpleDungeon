using System;
using System.Windows.Forms;
using Engine.Data;
using System.Text.Json;

namespace JsonEditor
{
    public class EditDialogueForm : Form
    {
        private DialogueData _dialogue;
        private TextBox txtId;
        private TextBox txtName;
        private TextBox txtNodesJson;
        private Button btnOk;
        private Button btnCancel;

        public EditDialogueForm(DialogueData dialogue)
        {
            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            InitializeComponents();
            LoadDataToControls();
        }

        private void InitializeComponents()
        {
            this.Text = "Редактирование диалога";
            this.Width = 800;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblId = new Label { Text = "Id:", Left = 10, Top = 12, Width = 30 };
            txtId = new TextBox { Left = 50, Top = 8, Width = 700 };

            var lblName = new Label { Text = "Название:", Left = 10, Top = 40, Width = 70 };
            txtName = new TextBox { Left = 90, Top = 36, Width = 660 };

            var lblNodes = new Label { Text = "Nodes (JSON):", Left = 10, Top = 70, Width = 120 };
            txtNodesJson = new TextBox { Left = 10, Top = 98, Width = 760, Height = 420, Multiline = true, ScrollBars = ScrollBars.Both, AcceptsReturn = true, AcceptsTab = true };

            btnOk = new Button { Text = "OK", Left = 590, Top = 530, Width = 80 };
            btnCancel = new Button { Text = "Отмена", Left = 690, Top = 530, Width = 80 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblId);
            this.Controls.Add(txtId);
            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblNodes);
            this.Controls.Add(txtNodesJson);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        private void LoadDataToControls()
        {
            txtId.Text = _dialogue.Id;
            txtName.Text = _dialogue.Name;

            var options = new JsonSerializerOptions { WriteIndented = true };
            try
            {
                txtNodesJson.Text = JsonSerializer.Serialize(_dialogue.Nodes ?? new System.Collections.Generic.List<DialogueNodeData>(), options);
            }
            catch
            {
                txtNodesJson.Text = "[]";
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // базовая валидация
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show(this, "Id не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            _dialogue.Id = txtId.Text.Trim();
            _dialogue.Name = txtName.Text.Trim();

            try
            {
                var nodes = JsonSerializer.Deserialize<System.Collections.Generic.List<DialogueNodeData>>(txtNodesJson.Text);
                _dialogue.Nodes = nodes ?? new System.Collections.Generic.List<DialogueNodeData>();
            }
            catch (Exception ex)
            {
                MessageBox.Show(this, "Ошибка парсинга JSON узлов: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}
