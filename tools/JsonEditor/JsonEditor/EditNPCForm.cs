using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class EditNPCForm : Form
    {
        private NPCData _npcData;
        private GameData _gameData;
        private TextBox txtId;
        private TextBox txtName;
        private TextBox txtGreeting;
        private ComboBox comboGreetingDialogue;
        private ComboBox comboDefaultDialogue;
        private Button btnOk;
        private Button btnCancel;

        public EditNPCForm(GameData gameData, NPCData npcData = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _npcData = npcData ?? new NPCData(); // если null — создаём новый

            InitializeComponent();
            LoadDataToControls();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование NPC";
            this.Width = 600;
            this.Height = 300;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var lblId = new Label { Text = "ID:", Left = 10, Top = 14, Width = 60 };
            txtId = new TextBox { Left = 80, Top = 10, Width = 460 };

            var lblName = new Label { Text = "Имя:", Left = 10, Top = 46, Width = 60 };
            txtName = new TextBox { Left = 80, Top = 42, Width = 460 };

            var lblGreeting = new Label { Text = "Greeting (текст):", Left = 10, Top = 78, Width = 120 };
            txtGreeting = new TextBox { Left = 135, Top = 74, Width = 405 };

            var lblDialogue = new Label { Text = "GreetingDialogue:", Left = 10, Top = 110, Width = 120 };
            comboGreetingDialogue = new ComboBox
            {
                Left = 135,
                Top = 106,
                Width = 405,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            var lblDefaultDialogue = new Label { Text = "DefaultDialogue:", Left = 10, Top = 142, Width = 120 };
            comboDefaultDialogue = new ComboBox
            {
                Left = 135,
                Top = 138,
                Width = 405,
                DropDownStyle = ComboBoxStyle.DropDownList
            };

            btnOk = new Button { Text = "OK", Left = 340, Top = 190, Width = 75 };
            btnCancel = new Button { Text = "Отмена", Left = 435, Top = 190, Width = 75 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblId);
            this.Controls.Add(txtId);
            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblGreeting);
            this.Controls.Add(txtGreeting);
            this.Controls.Add(lblDialogue);
            this.Controls.Add(comboGreetingDialogue);
            this.Controls.Add(lblDefaultDialogue);
            this.Controls.Add(comboDefaultDialogue);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        private void LoadDataToControls()
        {
            // Заполняем поля
            txtId.Text = _npcData.ID.ToString();
            txtName.Text = _npcData.Name ?? string.Empty;
            txtGreeting.Text = _npcData.Greeting ?? string.Empty;

            // Заполняем combo с диалогами (Id - Name)
            var items = new List<KeyValuePair<string, string>>
            {
                new KeyValuePair<string,string>("", "(нет)")
            };

            if (_gameData?.Dialogues != null)
            {
                items.AddRange(_gameData.Dialogues.Select(d => new KeyValuePair<string, string>(d.Id, $"{d.Id} - {d.Name}")));
            }

            comboGreetingDialogue.DisplayMember = "Value";
            comboGreetingDialogue.ValueMember = "Key";
            comboGreetingDialogue.DataSource = new BindingSource(items, null);

            comboDefaultDialogue.DisplayMember = "Value";
            comboDefaultDialogue.ValueMember = "Key";
            comboDefaultDialogue.DataSource = new BindingSource(items, null);

            // Выбор текущих значений
            comboGreetingDialogue.SelectedValue = _npcData.GreetingDialogueId ?? "";
            comboDefaultDialogue.SelectedValue = _npcData.DefaultDialogueId ?? "";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Валидация ID (целое число)
            if (!int.TryParse(txtId.Text.Trim(), out var id))
            {
                MessageBox.Show(this, "ID должен быть целым числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Основные поля
            _npcData.ID = id;
            _npcData.Name = txtName.Text.Trim();
            _npcData.Greeting = txtGreeting.Text;

            var sel = comboGreetingDialogue.SelectedValue as string;
            _npcData.GreetingDialogueId = string.IsNullOrEmpty(sel) ? null : sel;

            var selDefault = comboDefaultDialogue.SelectedValue as string;
            _npcData.DefaultDialogueId = string.IsNullOrEmpty(selDefault) ? null : selDefault;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Возвращаем изменённый объект (при DialogResult.OK)
        public NPCData GetNPCData() => _npcData;
    }
}
