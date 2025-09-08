using System;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Engine.Entities; // Item
using Engine.Core;     // ItemType
using Engine.Data;     // ItemData

namespace JsonEditor
{
    public class EditItemForm : Form
    {
        private Item _itemEntity;             // если у тебя используется Engine.Entities.Item
        private ItemData _itemData;           // если у тебя используется Engine.Data.ItemData
        private bool _isEntity = false;       // true -> Item, false -> ItemData

        // Controls
        private TextBox txtID;
        private TextBox txtName;
        private TextBox txtNamePlural;
        private ComboBox cboType;
        private NumericUpDown nudPrice;
        private TextBox txtDescription;
        private Button btnOK;
        private Button btnCancel;

        // Конструктор для Engine.Entities.Item
        public EditItemForm(Item item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _itemEntity = item;
            _isEntity = true;
            InitializeForm();
            LoadFromEntity();
        }

        // Конструктор для Engine.Data.ItemData
        public EditItemForm(ItemData itemData)
        {
            if (itemData == null) throw new ArgumentNullException(nameof(itemData));
            _itemData = itemData;
            _isEntity = false;
            InitializeForm();
            LoadFromData();
        }

        private void InitializeForm()
        {
            Text = "Редактирование предмета";
            Width = 600;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;

            int lblX = 10;
            int tbX = 150;
            int top = 12;
            int vgap = 34;
            int labelWidth = 130;
            int tbWidth = 400;

            // ID
            var lblId = new Label { Left = lblX, Top = top, Width = labelWidth, Text = "ID:" };
            txtID = new TextBox { Left = tbX, Top = top, Width = 120, ReadOnly = true };
            top += vgap;

            // Name
            var lblName = new Label { Left = lblX, Top = top, Width = labelWidth, Text = "Name:" };
            txtName = new TextBox { Left = tbX, Top = top, Width = tbWidth }; top += vgap;

            // NamePlural
            var lblNamePlural = new Label { Left = lblX, Top = top, Width = labelWidth, Text = "NamePlural:" };
            txtNamePlural = new TextBox { Left = tbX, Top = top, Width = tbWidth }; top += vgap;

            // Type (enum)
            var lblType = new Label { Left = lblX, Top = top, Width = labelWidth, Text = "Type:" };
            cboType = new ComboBox { Left = tbX, Top = top, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            var values = Enum.GetValues(typeof(ItemType)).Cast<object>().ToArray();
            cboType.Items.AddRange(values);
            top += vgap;

            // Price
            var lblPrice = new Label { Left = lblX, Top = top, Width = labelWidth, Text = "Price:" };
            nudPrice = new NumericUpDown { Left = tbX, Top = top, Width = 120, Minimum = 0, Maximum = 1_000_000 };
            top += vgap;

            // Description (многострочный)
            var lblDesc = new Label { Left = lblX, Top = top, Width = labelWidth, Text = "Description:" };
            txtDescription = new TextBox { Left = tbX, Top = top, Width = tbWidth, Height = 120, Multiline = true, ScrollBars = ScrollBars.Vertical };
            top += 130;

            // Buttons
            btnOK = new Button { Text = "OK", Left = tbX, Top = top, Width = 120 };
            btnCancel = new Button { Text = "Cancel", Left = tbX + 140, Top = top, Width = 120 };
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] {
                lblId, txtID,
                lblName, txtName,
                lblNamePlural, txtNamePlural,
                lblType, cboType,
                lblPrice, nudPrice,
                lblDesc, txtDescription,
                btnOK, btnCancel
            });
        }

        private void LoadFromEntity()
        {
            txtID.Text = _itemEntity.ID.ToString();
            txtName.Text = _itemEntity.Name ?? "";
            txtNamePlural.Text = _itemEntity.NamePlural ?? "";
            txtDescription.Text = _itemEntity.Description ?? "";
            nudPrice.Value = Math.Max(nudPrice.Minimum, Math.Min(nudPrice.Maximum, _itemEntity.Price));

            cboType.SelectedItem = _itemEntity.Type;
            if (cboType.SelectedItem == null && cboType.Items.Count > 0)
                cboType.SelectedIndex = 0;
        }

        private void LoadFromData()
        {
            // Используем reflection — чтобы форма работала независимо от точных полей ItemData
            var t = _itemData.GetType();

            var prop = t.GetProperty("ID");
            if (prop != null) txtID.Text = prop.GetValue(_itemData)?.ToString() ?? "";

            prop = t.GetProperty("Name");
            if (prop != null) txtName.Text = prop.GetValue(_itemData)?.ToString() ?? "";

            prop = t.GetProperty("NamePlural");
            if (prop != null) txtNamePlural.Text = prop.GetValue(_itemData)?.ToString() ?? "";

            prop = t.GetProperty("Description");
            if (prop != null) txtDescription.Text = prop.GetValue(_itemData)?.ToString() ?? "";

            prop = t.GetProperty("Price");
            if (prop != null)
            {
                if (int.TryParse(prop.GetValue(_itemData)?.ToString(), out int price))
                {
                    nudPrice.Value = Math.Max(nudPrice.Minimum, Math.Min(nudPrice.Maximum, price));
                }
            }

            // Type может быть enum или int — пробуем
            prop = t.GetProperty("Type");
            if (prop != null)
            {
                var val = prop.GetValue(_itemData);
                if (val != null)
                {
                    if (val is ItemType it) cboType.SelectedItem = it;
                    else
                    {
                        // пробуем распарсить строку/число
                        var s = val.ToString();
                        if (Enum.TryParse<ItemType>(s, out var parsed)) cboType.SelectedItem = parsed;
                        else if (int.TryParse(s, out var ival))
                        {
                            var enumVals = Enum.GetValues(typeof(ItemType)).Cast<int>().ToArray();
                            if (enumVals.Contains(ival))
                            {
                                cboType.SelectedItem = Enum.ToObject(typeof(ItemType), ival);
                            }
                        }
                    }
                }
            }

            if (cboType.SelectedItem == null && cboType.Items.Count > 0) cboType.SelectedIndex = 0;
        }

        private void BtnOK_Click(object? sender, EventArgs e)
        {
            // basic validation
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Имя не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (_isEntity)
            {
                _itemEntity.Name = txtName.Text.Trim();
                _itemEntity.NamePlural = txtNamePlural.Text.Trim();
                _itemEntity.Description = txtDescription.Text;
                _itemEntity.Price = (int)nudPrice.Value;

                if (cboType.SelectedItem is ItemType it) _itemEntity.Type = it;
                else if (Enum.TryParse<ItemType>(cboType.Text, out var parsed)) _itemEntity.Type = parsed;
            }
            else
            {
                var t = _itemData.GetType();

                var prop = t.GetProperty("Name");
                prop?.SetValue(_itemData, txtName.Text.Trim());

                prop = t.GetProperty("NamePlural");
                prop?.SetValue(_itemData, txtNamePlural.Text.Trim());

                prop = t.GetProperty("Description");
                prop?.SetValue(_itemData, txtDescription.Text);

                prop = t.GetProperty("Price");
                prop?.SetValue(_itemData, (int)nudPrice.Value);

                prop = t.GetProperty("Type");
                if (prop != null)
                {
                    if (prop.PropertyType == typeof(ItemType))
                    {
                        if (cboType.SelectedItem is ItemType it2) prop.SetValue(_itemData, it2);
                    }
                    else if (prop.PropertyType == typeof(int))
                    {
                        // int-backed enum
                        if (cboType.SelectedItem is ItemType it3) prop.SetValue(_itemData, Convert.ToInt32(it3));
                    }
                    else
                    {
                        // fallback: string
                        prop.SetValue(_itemData, cboType.SelectedItem?.ToString());
                    }
                }
            }

            DialogResult = DialogResult.OK;
            Close();
        }
    }
}
