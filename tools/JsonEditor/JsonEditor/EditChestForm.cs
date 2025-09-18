using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class EditChestForm : Form
    {
        private readonly GameData _gameData;
        private readonly ChestData _chest;
        private bool _isNew;

        private TextBox _txtId;
        private TextBox _txtName;
        private TextBox _txtNamePlural;
        private TextBox _txtDescription;
        private NumericUpDown _nudPrice;
        private CheckBox _chkIsLocked;
        private CheckBox _chkIsTrapped;
        private CheckBox _chkRequiresKey;
        private NumericUpDown _nudRequiredKeyId;
        private TextBox _txtLockDescription;
        private NumericUpDown _nudMaxCapacity;
        private DataGridView _gridInitialContents;
        private Button _btnAddItem;
        private Button _btnRemoveItem;
        private Button _btnOk;
        private Button _btnCancel;

        public EditChestForm(GameData gameData, ChestData chest = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _chest = chest ?? new ChestData();
            _isNew = chest == null;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = _isNew ? "Новый сундук" : "Редактирование сундука";
            Size = new System.Drawing.Size(800, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            // Основной контейнер с прокруткой
            var mainPanel = new Panel
            {
                Dock = DockStyle.Fill,
                AutoScroll = true
            };

            // Основная информация
            var basicInfoGroup = new GroupBox
            {
                Text = "Основная информация",
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(760, 150)
            };

            var lblId = new Label { Text = "ID:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(80, 20) };
            _txtId = new TextBox { Location = new System.Drawing.Point(100, 23), Size = new System.Drawing.Size(100, 20) };

            var lblName = new Label { Text = "Название:", Location = new System.Drawing.Point(220, 25), Size = new System.Drawing.Size(80, 20) };
            _txtName = new TextBox { Location = new System.Drawing.Point(310, 23), Size = new System.Drawing.Size(200, 20) };

            var lblNamePlural = new Label { Text = "Название (мн.):", Location = new System.Drawing.Point(520, 25), Size = new System.Drawing.Size(100, 20) };
            _txtNamePlural = new TextBox { Location = new System.Drawing.Point(630, 23), Size = new System.Drawing.Size(120, 20) };

            var lblDescription = new Label { Text = "Описание:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(80, 20) };
            _txtDescription = new TextBox { Location = new System.Drawing.Point(100, 53), Size = new System.Drawing.Size(650, 20) };

            var lblPrice = new Label { Text = "Цена:", Location = new System.Drawing.Point(10, 85), Size = new System.Drawing.Size(80, 20) };
            _nudPrice = new NumericUpDown { Location = new System.Drawing.Point(100, 83), Size = new System.Drawing.Size(100, 20), Maximum = 999999, Minimum = 0 };

            var lblMaxCapacity = new Label { Text = "Вместимость:", Location = new System.Drawing.Point(220, 85), Size = new System.Drawing.Size(80, 20) };
            _nudMaxCapacity = new NumericUpDown { Location = new System.Drawing.Point(310, 83), Size = new System.Drawing.Size(100, 20), Maximum = 100, Minimum = 1, Value = 20 };

            basicInfoGroup.Controls.AddRange(new Control[] { 
                lblId, _txtId, lblName, _txtName, lblNamePlural, _txtNamePlural,
                lblDescription, _txtDescription, lblPrice, _nudPrice, lblMaxCapacity, _nudMaxCapacity 
            });

            // Свойства сундука
            var chestPropertiesGroup = new GroupBox
            {
                Text = "Свойства сундука",
                Location = new System.Drawing.Point(10, 170),
                Size = new System.Drawing.Size(760, 120)
            };

            _chkIsLocked = new CheckBox { Text = "Заперт", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(100, 20) };
            _chkIsTrapped = new CheckBox { Text = "Ловушка", Location = new System.Drawing.Point(120, 25), Size = new System.Drawing.Size(100, 20) };
            _chkRequiresKey = new CheckBox { Text = "Требует ключ", Location = new System.Drawing.Point(230, 25), Size = new System.Drawing.Size(120, 20) };

            var lblRequiredKeyId = new Label { Text = "ID ключа:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(80, 20) };
            _nudRequiredKeyId = new NumericUpDown { Location = new System.Drawing.Point(100, 53), Size = new System.Drawing.Size(100, 20), Maximum = 99999, Minimum = 0 };

            var lblLockDescription = new Label { Text = "Описание замка:", Location = new System.Drawing.Point(220, 55), Size = new System.Drawing.Size(100, 20) };
            _txtLockDescription = new TextBox { Location = new System.Drawing.Point(330, 53), Size = new System.Drawing.Size(420, 20) };

            chestPropertiesGroup.Controls.AddRange(new Control[] { 
                _chkIsLocked, _chkIsTrapped, _chkRequiresKey, 
                lblRequiredKeyId, _nudRequiredKeyId, lblLockDescription, _txtLockDescription 
            });

            // Начальное содержимое
            var contentsGroup = new GroupBox
            {
                Text = "Начальное содержимое",
                Location = new System.Drawing.Point(10, 300),
                Size = new System.Drawing.Size(760, 300)
            };

            _gridInitialContents = new DataGridView
            {
                Location = new System.Drawing.Point(10, 25),
                Size = new System.Drawing.Size(740, 200),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false
            };

            // Настройка колонок для содержимого
            _gridInitialContents.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ItemID",
                HeaderText = "ID предмета",
                DataPropertyName = "ItemID",
                Width = 100
            });

            _gridInitialContents.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ItemName",
                HeaderText = "Название предмета",
                Width = 300
            });

            _gridInitialContents.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Quantity",
                HeaderText = "Количество",
                DataPropertyName = "Quantity",
                Width = 100
            });

            _btnAddItem = new Button { Text = "Добавить предмет", Location = new System.Drawing.Point(10, 235), Size = new System.Drawing.Size(120, 30) };
            _btnRemoveItem = new Button { Text = "Удалить предмет", Location = new System.Drawing.Point(140, 235), Size = new System.Drawing.Size(120, 30) };

            _btnAddItem.Click += BtnAddItem_Click;
            _btnRemoveItem.Click += BtnRemoveItem_Click;

            contentsGroup.Controls.AddRange(new Control[] { 
                _gridInitialContents, _btnAddItem, _btnRemoveItem 
            });

            // Кнопки
            _btnOk = new Button { Text = "OK", Location = new System.Drawing.Point(600, 620), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Отмена", Location = new System.Drawing.Point(690, 620), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.Cancel };

            mainPanel.Controls.AddRange(new Control[] { 
                basicInfoGroup, chestPropertiesGroup, contentsGroup, _btnOk, _btnCancel 
            });

            Controls.Add(mainPanel);

            // Обработчики событий
            _chkRequiresKey.CheckedChanged += (s, e) => _nudRequiredKeyId.Enabled = _chkRequiresKey.Checked;
        }

        private void LoadData()
        {
            _txtId.Text = _chest.ID.ToString();
            _txtName.Text = _chest.Name;
            _txtNamePlural.Text = _chest.NamePlural;
            _txtDescription.Text = _chest.Description;
            _nudPrice.Value = _chest.Price;
            _chkIsLocked.Checked = _chest.IsLocked;
            _chkIsTrapped.Checked = _chest.IsTrapped;
            _chkRequiresKey.Checked = _chest.RequiresKey;
            _nudRequiredKeyId.Value = _chest.RequiredKeyID;
            _txtLockDescription.Text = _chest.LockDescription;
            _nudMaxCapacity.Value = _chest.MaxCapacity;

            _nudRequiredKeyId.Enabled = _chkRequiresKey.Checked;

            // Загружаем содержимое
            RefreshContentsGrid();
        }

        private void RefreshContentsGrid()
        {
            _gridInitialContents.DataSource = null;
            _gridInitialContents.DataSource = _chest.InitialContents;

            // Обновляем названия предметов
            foreach (DataGridViewRow row in _gridInitialContents.Rows)
            {
                if (row.DataBoundItem is InventoryItemData itemData)
                {
                    var item = _gameData.Items?.FirstOrDefault(i => i.ID == itemData.ItemID);
                    if (item != null)
                    {
                        row.Cells["ItemName"].Value = item.Name;
                    }
                }
            }
        }

        private void BtnAddItem_Click(object sender, EventArgs e)
        {
            using (var form = new AddChestItemForm(_gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    var itemData = form.GetItemData();
                    _chest.InitialContents.Add(itemData);
                    RefreshContentsGrid();
                }
            }
        }

        private void BtnRemoveItem_Click(object sender, EventArgs e)
        {
            if (_gridInitialContents.SelectedRows.Count > 0)
            {
                var selectedIndex = _gridInitialContents.SelectedRows[0].Index;
                if (selectedIndex >= 0 && selectedIndex < _chest.InitialContents.Count)
                {
                    _chest.InitialContents.RemoveAt(selectedIndex);
                    RefreshContentsGrid();
                }
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (!ValidateInput())
                {
                    e.Cancel = true;
                    return;
                }

                SaveData();
            }

            base.OnFormClosing(e);
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(_txtName.Text))
            {
                MessageBox.Show("Название сундука не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtName.Focus();
                return false;
            }

            if (!int.TryParse(_txtId.Text, out int id) || id <= 0)
            {
                MessageBox.Show("ID должен быть положительным числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                _txtId.Focus();
                return false;
            }

            // Проверяем уникальность ID
            if (_isNew || _chest.ID != id)
            {
                if (_gameData.Chests?.Any(c => c.ID == id) == true)
                {
                    MessageBox.Show("Сундук с таким ID уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    _txtId.Focus();
                    return false;
                }
            }

            return true;
        }

        private void SaveData()
        {
            _chest.ID = int.Parse(_txtId.Text);
            _chest.Name = _txtName.Text;
            _chest.NamePlural = _txtNamePlural.Text;
            _chest.Description = _txtDescription.Text;
            _chest.Price = (int)_nudPrice.Value;
            _chest.IsLocked = _chkIsLocked.Checked;
            _chest.IsTrapped = _chkIsTrapped.Checked;
            _chest.RequiresKey = _chkRequiresKey.Checked;
            _chest.RequiredKeyID = (int)_nudRequiredKeyId.Value;
            _chest.LockDescription = _txtLockDescription.Text;
            _chest.MaxCapacity = (int)_nudMaxCapacity.Value;
        }

        public ChestData GetChestData()
        {
            return _chest;
        }
    }
}
