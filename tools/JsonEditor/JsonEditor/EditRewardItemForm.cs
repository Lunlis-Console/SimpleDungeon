using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;
using Engine.Quests;

namespace JsonEditor
{
    public class EditRewardItemForm : Form
    {
        public QuestRewardItem RewardItem { get; private set; }
        private GameData _gameData;

        private ComboBox cbItem;
        private NumericUpDown nudQuantity;
        private Button btnOk;
        private Button btnCancel;

        public EditRewardItemForm(QuestRewardItem rewardItem, GameData gameData)
        {
            RewardItem = rewardItem ?? throw new ArgumentNullException(nameof(rewardItem));
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование предмета-награды";
            this.Width = 400;
            this.Height = 150;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;

            var lblItem = new Label { Text = "Предмет:", Left = 10, Top = 15, Width = 80 };
            cbItem = new ComboBox { Left = 100, Top = 12, Width = 250, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblQuantity = new Label { Text = "Количество:", Left = 10, Top = 45, Width = 80 };
            nudQuantity = new NumericUpDown { Left = 100, Top = 42, Width = 100, Minimum = 1, Maximum = 1000 };

            btnOk = new Button { Text = "OK", Left = 100, Top = 80, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Отмена", Left = 190, Top = 80, Width = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.AddRange(new Control[]
            {
                lblItem, cbItem,
                lblQuantity, nudQuantity,
                btnOk, btnCancel
            });
        }

        private void LoadData()
        {
            // Заполнение списка предметов
            if (_gameData.Items != null)
            {
                foreach (var item in _gameData.Items.OrderBy(i => i.Name))
                {
                    cbItem.Items.Add(new ItemComboItem(item));
                }

                // Выбор текущего предмета
                var selectedItem = cbItem.Items.Cast<ItemComboItem>()
                    .FirstOrDefault(i => i.ItemData.ID == RewardItem.ItemID);

                if (selectedItem != null)
                {
                    cbItem.SelectedItem = selectedItem;
                }
                else if (cbItem.Items.Count > 0)
                {
                    cbItem.SelectedIndex = 0;
                }
            }

            nudQuantity.Value = RewardItem.Quantity;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cbItem.SelectedItem is ItemComboItem selectedItem)
            {
                RewardItem.ItemID = selectedItem.ItemData.ID;
                RewardItem.Quantity = (int)nudQuantity.Value;
            }
            else
            {
                MessageBox.Show("Выберите предмет.");
                this.DialogResult = DialogResult.None;
            }
        }

        private class ItemComboItem
        {
            public ItemData ItemData { get; }

            public ItemComboItem(ItemData itemData)
            {
                ItemData = itemData;
            }

            public override string ToString()
            {
                return $"{ItemData.Name} (ID: {ItemData.ID})";
            }
        }
    }
}
