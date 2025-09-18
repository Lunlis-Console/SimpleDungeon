using System;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class AddChestItemForm : Form
    {
        private readonly GameData _gameData;
        private ComboBox _cmbItem;
        private NumericUpDown _nudQuantity;
        private Button _btnOk;
        private Button _btnCancel;

        public AddChestItemForm(GameData gameData)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            InitializeComponent();
            LoadItems();
        }

        private void InitializeComponent()
        {
            Text = "Добавить предмет в сундук";
            Size = new System.Drawing.Size(400, 200);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var lblItem = new Label { Text = "Предмет:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(80, 20) };
            _cmbItem = new ComboBox 
            { 
                Location = new System.Drawing.Point(100, 23), 
                Size = new System.Drawing.Size(280, 20), 
                DropDownStyle = ComboBoxStyle.DropDownList 
            };

            var lblQuantity = new Label { Text = "Количество:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(80, 20) };
            _nudQuantity = new NumericUpDown 
            { 
                Location = new System.Drawing.Point(100, 53), 
                Size = new System.Drawing.Size(100, 20), 
                Minimum = 1, 
                Maximum = 999, 
                Value = 1 
            };

            _btnOk = new Button { Text = "OK", Location = new System.Drawing.Point(220, 120), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Отмена", Location = new System.Drawing.Point(310, 120), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { 
                lblItem, _cmbItem, lblQuantity, _nudQuantity, _btnOk, _btnCancel 
            });
        }

        private void LoadItems()
        {
            if (_gameData.Items != null)
            {
                _cmbItem.Items.Clear();
                foreach (var item in _gameData.Items.OrderBy(i => i.Name))
                {
                    _cmbItem.Items.Add(new ItemComboBoxItem(item));
                }
            }
        }

        public InventoryItemData GetItemData()
        {
            if (_cmbItem.SelectedItem is ItemComboBoxItem selectedItem)
            {
                return new InventoryItemData
                {
                    ItemID = selectedItem.Item.ID,
                    Quantity = (int)_nudQuantity.Value
                };
            }
            return null;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (_cmbItem.SelectedItem == null)
                {
                    MessageBox.Show("Выберите предмет.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }

        private class ItemComboBoxItem
        {
            public ItemData Item { get; }

            public ItemComboBoxItem(ItemData item)
            {
                Item = item;
            }

            public override string ToString()
            {
                return $"{Item.ID} - {Item.Name}";
            }
        }
    }
}
