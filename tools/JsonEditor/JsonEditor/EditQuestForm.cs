using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class EditQuestForm : Form
    {
        private QuestData _quest;
        private GameData _gameData;

        // Основные элементы управления
        private TextBox txtId;
        private TextBox txtName;
        private TextBox txtDescription;
        private NumericUpDown nudRewardGold;
        private NumericUpDown nudRewardEXP;

        // Элементы квеста
        private ListBox lstQuestItems;
        private Button btnAddQuestItem;
        private Button btnEditQuestItem;
        private Button btnRemoveQuestItem;

        private Button btnOk;
        private Button btnCancel;

        private BindingList<QuestItemData> _questItemsBinding;

        public EditQuestForm(GameData gameData, QuestData quest = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _quest = quest ?? new QuestData();

            InitializeComponent();
            LoadQuestData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование квеста";
            this.Width = 600;
            this.Height = 500;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int leftLabel = 12;
            int leftControl = 120;
            int top = 12;
            int vertGap = 30;

            // ID квеста
            var lblId = new Label { Text = "ID:", Left = leftLabel, Top = top + 4, Width = 100 };
            txtId = new TextBox { Left = leftControl, Top = top, Width = 200 };
            top += vertGap;

            // Название квеста
            var lblName = new Label { Text = "Название:", Left = leftLabel, Top = top + 4, Width = 100 };
            txtName = new TextBox { Left = leftControl, Top = top, Width = 400 };
            top += vertGap;

            // Описание квеста
            var lblDescription = new Label { Text = "Описание:", Left = leftLabel, Top = top + 4, Width = 100 };
            txtDescription = new TextBox
            {
                Left = leftControl,
                Top = top,
                Width = 400,
                Height = 80,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            top += 90;

            // Награда золотом
            var lblRewardGold = new Label { Text = "Награда (золото):", Left = leftLabel, Top = top + 4, Width = 100 };
            nudRewardGold = new NumericUpDown { Left = leftControl, Top = top, Width = 120, Minimum = 0, Maximum = 1000000 };
            top += vertGap;

            // Награда опытом
            var lblRewardEXP = new Label { Text = "Награда (опыт):", Left = leftLabel, Top = top + 4, Width = 100 };
            nudRewardEXP = new NumericUpDown { Left = leftControl, Top = top, Width = 120, Minimum = 0, Maximum = 1000000 };
            top += vertGap + 10;

            // Заголовок для предметов квеста
            var lblQuestItems = new Label { Text = "Предметы квеста:", Left = leftLabel, Top = top + 4, Width = 100 };
            top += vertGap;

            // Список предметов квеста
            lstQuestItems = new ListBox
            {
                Left = leftLabel,
                Top = top,
                Width = 500,
                Height = 150,
                DisplayMember = "DisplayText"
            };
            top += 160;

            // Кнопки управления предметами
            btnAddQuestItem = new Button { Text = "Добавить предмет", Left = leftLabel, Top = top, Width = 120 };
            btnEditQuestItem = new Button { Text = "Редактировать", Left = leftLabel + 130, Top = top, Width = 120 };
            btnRemoveQuestItem = new Button { Text = "Удалить", Left = leftLabel + 260, Top = top, Width = 120 };
            top += 40;

            // Кнопки OK/Cancel
            btnOk = new Button { Text = "OK", Left = leftLabel + 200, Top = top, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Отмена", Left = leftLabel + 290, Top = top, Width = 80, DialogResult = DialogResult.Cancel };

            // Обработчики событий
            btnAddQuestItem.Click += BtnAddQuestItem_Click;
            btnEditQuestItem.Click += BtnEditQuestItem_Click;
            btnRemoveQuestItem.Click += BtnRemoveQuestItem_Click;
            btnOk.Click += BtnOk_Click;

            // Добавление элементов на форму
            this.Controls.AddRange(new Control[]
            {
                lblId, txtId,
                lblName, txtName,
                lblDescription, txtDescription,
                lblRewardGold, nudRewardGold,
                lblRewardEXP, nudRewardEXP,
                lblQuestItems,
                lstQuestItems,
                btnAddQuestItem, btnEditQuestItem, btnRemoveQuestItem,
                btnOk, btnCancel
            });
        }

        private void LoadQuestData()
        {
            txtId.Text = _quest.ID.ToString();
            txtName.Text = _quest.Name ?? "";
            txtDescription.Text = _quest.Description ?? "";
            nudRewardGold.Value = _quest.RewardGold;
            nudRewardEXP.Value = _quest.RewardEXP;

            // Инициализация binding list для предметов квеста
            _questItemsBinding = new BindingList<QuestItemData>(_quest.QuestItems ?? new List<QuestItemData>());
            lstQuestItems.DataSource = _questItemsBinding;

            // Используем событие Format для кастомного отображения
            lstQuestItems.Format += (sender, e) =>
            {
                if (e.ListItem is QuestItemData item)
                {
                    e.Value = GetQuestItemDisplayText(item);
                }
            };

            // Обновляем при изменениях
            _questItemsBinding.ListChanged += (s, e) => lstQuestItems.Invalidate();
        }


        private string GetQuestItemDisplayText(QuestItemData item)
        {
            var itemName = _gameData.Items?.FirstOrDefault(i => i.ID == item.ItemID)?.Name ?? "Неизвестный предмет";
            return $"{itemName} (ID: {item.ItemID}, Количество: {item.Quantity})";
        }

        private void BtnAddQuestItem_Click(object sender, EventArgs e)
        {
            var newQuestItem = new QuestItemData { ItemID = 1, Quantity = 1 };

            using (var form = new EditQuestItemForm(newQuestItem, _gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _questItemsBinding.Add(form.QuestItem);
                }
            }
        }

        private void BtnEditQuestItem_Click(object sender, EventArgs e)
        {
            if (lstQuestItems.SelectedItem is QuestItemData selectedItem)
            {
                using (var form = new EditQuestItemForm(selectedItem, _gameData))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        int index = _questItemsBinding.IndexOf(selectedItem);
                        _questItemsBinding.ResetItem(index);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите предмет для редактирования.");
            }
        }

        private void BtnRemoveQuestItem_Click(object sender, EventArgs e)
        {
            if (lstQuestItems.SelectedItem is QuestItemData selectedItem)
            {
                if (MessageBox.Show("Удалить этот предмет из квеста?", "Подтверждение",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _questItemsBinding.Remove(selectedItem);
                }
            }
            else
            {
                MessageBox.Show("Выберите предмет для удаления.");
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtId.Text) || !int.TryParse(txtId.Text, out int id))
            {
                MessageBox.Show("ID должен быть числом.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название квеста.");
                return;
            }

            // Сохранение данных
            _quest.ID = id;
            _quest.Name = txtName.Text.Trim();
            _quest.Description = txtDescription.Text.Trim();
            _quest.RewardGold = (int)nudRewardGold.Value;
            _quest.RewardEXP = (int)nudRewardEXP.Value;
            _quest.QuestItems = _questItemsBinding.ToList();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public QuestData GetQuest() => _quest;
    }

    public class EditQuestItemForm : Form
    {
        public QuestItemData QuestItem { get; private set; }
        private GameData _gameData;

        private ComboBox cbItem;
        private NumericUpDown nudQuantity;
        private Button btnOk;
        private Button btnCancel;

        public EditQuestItemForm(QuestItemData questItem, GameData gameData)
        {
            QuestItem = questItem ?? throw new ArgumentNullException(nameof(questItem));
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование предмета квеста";
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
                    .FirstOrDefault(i => i.ItemData.ID == QuestItem.ItemID);

                if (selectedItem != null)
                {
                    cbItem.SelectedItem = selectedItem;
                }
                else if (cbItem.Items.Count > 0)
                {
                    cbItem.SelectedIndex = 0;
                }
            }

            nudQuantity.Value = QuestItem.Quantity;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (cbItem.SelectedItem is ItemComboItem selectedItem)
            {
                QuestItem.ItemID = selectedItem.ItemData.ID;
                QuestItem.Quantity = (int)nudQuantity.Value;
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