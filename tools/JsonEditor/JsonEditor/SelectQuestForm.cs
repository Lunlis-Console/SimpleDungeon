using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class SelectQuestForm : Form
    {
        public int SelectedQuestID { get; private set; }
        private GameData _gameData;

        private ListBox lstQuests;
        private Button btnOk;
        private Button btnCancel;

        public SelectQuestForm(GameData gameData)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Выбор квеста";
            this.Width = 400;
            this.Height = 300;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            var lblQuests = new Label { Text = "Выберите квест:", Left = 10, Top = 10, Width = 100 };
            lstQuests = new ListBox
            {
                Left = 10,
                Top = 35,
                Width = 360,
                Height = 180
            };

            btnOk = new Button { Text = "OK", Left = 100, Top = 230, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Отмена", Left = 190, Top = 230, Width = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.AddRange(new Control[]
            {
                lblQuests, lstQuests,
                btnOk, btnCancel
            });
        }

        private void LoadData()
        {
            if (_gameData.Quests != null)
            {
                foreach (var quest in _gameData.Quests.OrderBy(q => q.Name))
                {
                    lstQuests.Items.Add(new QuestComboItem(quest));
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (lstQuests.SelectedItem is QuestComboItem selectedQuest)
            {
                SelectedQuestID = selectedQuest.QuestData.ID;
            }
            else
            {
                MessageBox.Show("Выберите квест.");
                this.DialogResult = DialogResult.None;
            }
        }

        private class QuestComboItem
        {
            public QuestData QuestData { get; }

            public QuestComboItem(QuestData questData)
            {
                QuestData = questData;
            }

            public override string ToString()
            {
                return $"{QuestData.Name} (ID: {QuestData.ID})";
            }
        }
    }
}
