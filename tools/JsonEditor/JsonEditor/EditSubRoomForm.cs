using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public partial class EditSubRoomForm : Form
    {
        private RoomData _parentRoom;
        private RoomData _subRoom;
        private GameData _gameData;
        private bool _isNew;

        private TextBox _txtId;
        private TextBox _txtName;
        private TextBox _txtDescription;
        private DataGridView _gridNPCs;
        private DataGridView _gridMonsterTemplates;
        private DataGridView _gridGroundItems;
        private CheckBox _chkScaleMonsters;
        private ComboBox _cmbRoomNorth;
        private ComboBox _cmbRoomEast;
        private ComboBox _cmbRoomSouth;
        private ComboBox _cmbRoomWest;

        public EditSubRoomForm(RoomData parentRoom, RoomData subRoom, GameData gameData, bool isNew = false)
        {
            _parentRoom = parentRoom ?? throw new ArgumentNullException(nameof(parentRoom));
            _subRoom = subRoom ?? new RoomData();
            _gameData = gameData;
            _isNew = isNew;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = _isNew ? $"Новое под-помещение в {_parentRoom.Name}" : $"Редактирование под-помещения в {_parentRoom.Name}";
            Size = new System.Drawing.Size(900, 1000); // Увеличиваем размер
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
                Size = new System.Drawing.Size(860, 120)
            };

            var lblId = new Label { Text = "ID:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(80, 20) };
            _txtId = new TextBox { Location = new System.Drawing.Point(100, 23), Size = new System.Drawing.Size(100, 20) };

            var lblName = new Label { Text = "Название:", Location = new System.Drawing.Point(220, 25), Size = new System.Drawing.Size(80, 20) };
            _txtName = new TextBox { Location = new System.Drawing.Point(310, 23), Size = new System.Drawing.Size(300, 20) };

            var lblDescription = new Label { Text = "Описание:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(80, 20) };
            _txtDescription = new TextBox { Location = new System.Drawing.Point(100, 53), Size = new System.Drawing.Size(750, 20), Multiline = true, Height = 50, ScrollBars = ScrollBars.Vertical };

            var lblParentInfo = new Label { Text = $"Родительское помещение: {_parentRoom.Name} (ID: {_parentRoom.ID})", Location = new System.Drawing.Point(10, 85), Size = new System.Drawing.Size(400, 20), ForeColor = System.Drawing.Color.Blue };

            basicInfoGroup.Controls.AddRange(new Control[] { lblId, _txtId, lblName, _txtName, lblDescription, _txtDescription, lblParentInfo });

            // NPCs
            var npcGroup = new GroupBox
            {
                Text = "NPC в под-помещении",
                Location = new System.Drawing.Point(10, 140),
                Size = new System.Drawing.Size(420, 200)
            };

            _gridNPCs = new DataGridView
            {
                Location = new System.Drawing.Point(10, 20),
                Size = new System.Drawing.Size(400, 170),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            npcGroup.Controls.Add(_gridNPCs);

            // Monster Templates
            var monsterGroup = new GroupBox
            {
                Text = "Шаблоны монстров",
                Location = new System.Drawing.Point(440, 140),
                Size = new System.Drawing.Size(430, 200)
            };

            _gridMonsterTemplates = new DataGridView
            {
                Location = new System.Drawing.Point(10, 20),
                Size = new System.Drawing.Size(410, 170),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            monsterGroup.Controls.Add(_gridMonsterTemplates);

            // Ground Items
            var itemsGroup = new GroupBox
            {
                Text = "Предметы на земле",
                Location = new System.Drawing.Point(10, 350),
                Size = new System.Drawing.Size(860, 200)
            };

            _gridGroundItems = new DataGridView
            {
                Location = new System.Drawing.Point(10, 20),
                Size = new System.Drawing.Size(840, 170),
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            itemsGroup.Controls.Add(_gridGroundItems);

            // Scale Monsters
            var scaleGroup = new GroupBox
            {
                Text = "Настройки монстров",
                Location = new System.Drawing.Point(10, 560),
                Size = new System.Drawing.Size(860, 60)
            };

            _chkScaleMonsters = new CheckBox { Text = "Масштабировать монстров под уровень игрока", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(300, 20) };
            scaleGroup.Controls.Add(_chkScaleMonsters);

            // Room Connections
            var connectionsGroup = new GroupBox
            {
                Text = "Связи с другими под-помещениями",
                Location = new System.Drawing.Point(10, 630),
                Size = new System.Drawing.Size(860, 120)
            };

            var lblNorth = new Label { Text = "Север:", Location = new System.Drawing.Point(10, 25), Size = new System.Drawing.Size(60, 20) };
            _cmbRoomNorth = new ComboBox { Location = new System.Drawing.Point(80, 23), Size = new System.Drawing.Size(180, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblEast = new Label { Text = "Восток:", Location = new System.Drawing.Point(280, 25), Size = new System.Drawing.Size(60, 20) };
            _cmbRoomEast = new ComboBox { Location = new System.Drawing.Point(350, 23), Size = new System.Drawing.Size(180, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblSouth = new Label { Text = "Юг:", Location = new System.Drawing.Point(10, 55), Size = new System.Drawing.Size(60, 20) };
            _cmbRoomSouth = new ComboBox { Location = new System.Drawing.Point(80, 53), Size = new System.Drawing.Size(180, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            var lblWest = new Label { Text = "Запад:", Location = new System.Drawing.Point(280, 55), Size = new System.Drawing.Size(60, 20) };
            _cmbRoomWest = new ComboBox { Location = new System.Drawing.Point(350, 53), Size = new System.Drawing.Size(180, 20), DropDownStyle = ComboBoxStyle.DropDownList };

            connectionsGroup.Controls.AddRange(new Control[] { lblNorth, _cmbRoomNorth, lblEast, _cmbRoomEast, lblSouth, _cmbRoomSouth, lblWest, _cmbRoomWest });

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10),
                Height = 50
            };

            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOK);

            // Добавляем все группы в основной панель
            mainPanel.Controls.AddRange(new Control[] { 
                basicInfoGroup, npcGroup, monsterGroup, itemsGroup, scaleGroup, connectionsGroup 
            });

            Controls.Add(mainPanel);
            Controls.Add(buttonPanel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void LoadData()
        {
            // Initialize grids
            InitializeNPCsGrid();
            InitializeMonsterTemplatesGrid();
            InitializeGroundItemsGrid();

            // Load sub-rooms for connections (only rooms that belong to the same parent)
            var subRooms = GetSubRoomsForParent(_parentRoom.ID);
            var roomComboBoxes = new[] { _cmbRoomNorth, _cmbRoomEast, _cmbRoomSouth, _cmbRoomWest };
            foreach (var cmb in roomComboBoxes)
            {
                cmb.Items.Clear();
                cmb.Items.Add(new ComboBoxItem { Text = "(Нет)", Value = 0 });
                foreach (var room in subRooms)
                {
                    cmb.Items.Add(new ComboBoxItem { Text = $"{room.Name} (ID: {room.ID})", Value = room.ID });
                }
            }

            // Set current values
            _txtId.Text = _subRoom.ID.ToString();
            _txtName.Text = _subRoom.Name;
            _txtDescription.Text = _subRoom.Description;
            _chkScaleMonsters.Checked = _subRoom.ScaleMonstersToPlayerLevel;

            // Load data into grids
            LoadNPCsData();
            LoadMonsterTemplatesData();
            LoadGroundItemsData();

            // Set room connections
            SetRoomConnection(_cmbRoomNorth, _subRoom.RoomToNorth);
            SetRoomConnection(_cmbRoomEast, _subRoom.RoomToEast);
            SetRoomConnection(_cmbRoomSouth, _subRoom.RoomToSouth);
            SetRoomConnection(_cmbRoomWest, _subRoom.RoomToWest);
        }

        private List<RoomData> GetSubRoomsForParent(int parentRoomId)
        {
            // Возвращаем все помещения, которые принадлежат к тому же родительскому помещению
            // Для простоты пока возвращаем все помещения, но можно добавить специальное поле ParentRoomID
            return _gameData.Rooms.Where(r => r.ID != _subRoom.ID).ToList();
        }

        private void InitializeNPCsGrid()
        {
            _gridNPCs.Columns.Clear();
            var colSelected = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Вкл.", Width = 50 };
            var colId = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", ReadOnly = true, Width = 50 };
            var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Имя", ReadOnly = true, FillWeight = 60 };
            var colCount = new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Кол-во", Width = 80 };
            _gridNPCs.Columns.AddRange(new DataGridViewColumn[] { colSelected, colId, colName, colCount });
        }

        private void InitializeMonsterTemplatesGrid()
        {
            _gridMonsterTemplates.Columns.Clear();
            var colSelected = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Вкл.", Width = 50 };
            var colId = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", ReadOnly = true, Width = 50 };
            var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Имя", ReadOnly = true, FillWeight = 60 };
            var colCount = new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Кол-во", Width = 80 };
            _gridMonsterTemplates.Columns.AddRange(new DataGridViewColumn[] { colSelected, colId, colName, colCount });
        }

        private void InitializeGroundItemsGrid()
        {
            _gridGroundItems.Columns.Clear();
            var colSelected = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Вкл.", Width = 50 };
            var colId = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", ReadOnly = true, Width = 50 };
            var colName = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Имя", ReadOnly = true, FillWeight = 60 };
            var colCount = new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Кол-во", Width = 80 };
            _gridGroundItems.Columns.AddRange(new DataGridViewColumn[] { colSelected, colId, colName, colCount });
        }

        private void LoadNPCsData()
        {
            _gridNPCs.Rows.Clear();
            foreach (var npc in _gameData.NPCs)
            {
                bool selected = _subRoom.NPCsHere.Contains(npc.ID);
                int count = 1; // По умолчанию 1 NPC
                _gridNPCs.Rows.Add(selected, npc.ID, npc.Name, count);
            }
        }

        private void LoadMonsterTemplatesData()
        {
            _gridMonsterTemplates.Rows.Clear();
            foreach (var monster in _gameData.Monsters)
            {
                bool selected = _subRoom.MonsterTemplates.Contains(monster.ID);
                int count = 1; // По умолчанию 1 монстр
                _gridMonsterTemplates.Rows.Add(selected, monster.ID, monster.Name, count);
            }
        }

        private void LoadGroundItemsData()
        {
            _gridGroundItems.Rows.Clear();
            foreach (var item in _gameData.Items)
            {
                bool selected = false;
                int count = 1;

                // Проверяем, есть ли этот предмет в GroundItems
                var groundItem = _subRoom.GroundItems?.FirstOrDefault(gi => gi.ItemID == item.ID);
                if (groundItem != null)
                {
                    selected = true;
                    count = groundItem.Quantity;
                }

                _gridGroundItems.Rows.Add(selected, item.ID, item.Name, count);
            }
        }

        private void SetRoomConnection(ComboBox cmb, int? roomId)
        {
            if (roomId.HasValue)
            {
                var room = cmb.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == roomId.Value);
                if (room != null)
                    cmb.SelectedItem = room;
            }
            else
            {
                cmb.SelectedIndex = 0; // "(Нет)"
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
                MessageBox.Show("Название не может быть пустым.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (!int.TryParse(_txtId.Text, out int id) || id <= 0)
            {
                MessageBox.Show("ID должен быть положительным числом.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            // Check for duplicate ID
            if (_isNew || _subRoom.ID != id)
            {
                if (_gameData.Rooms.Any(r => r.ID == id))
                {
                    MessageBox.Show("Помещение с таким ID уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            return true;
        }

        private void SaveData()
        {
            _subRoom.ID = int.Parse(_txtId.Text);
            _subRoom.Name = _txtName.Text.Trim();
            _subRoom.Description = _txtDescription.Text.Trim();
            _subRoom.ScaleMonstersToPlayerLevel = _chkScaleMonsters.Checked;
            _subRoom.ParentLocationID = _parentRoom.ParentLocationID; // Наследуем родительскую локацию

            // NPCs - сохраняем только выбранные
            _subRoom.NPCsHere.Clear();
            foreach (DataGridViewRow row in _gridNPCs.Rows)
            {
                try
                {
                    bool selected = Convert.ToBoolean(row.Cells["Selected"].Value);
                    if (!selected) continue;
                }
                catch
                {
                    continue;
                }

                if (!int.TryParse(Convert.ToString(row.Cells["ID"].Value), out int id)) continue;
                _subRoom.NPCsHere.Add(id);
            }

            // Monster templates - сохраняем только выбранные
            _subRoom.MonsterTemplates.Clear();
            foreach (DataGridViewRow row in _gridMonsterTemplates.Rows)
            {
                try
                {
                    bool selected = Convert.ToBoolean(row.Cells["Selected"].Value);
                    if (!selected) continue;
                }
                catch
                {
                    continue;
                }

                if (!int.TryParse(Convert.ToString(row.Cells["ID"].Value), out int id)) continue;
                _subRoom.MonsterTemplates.Add(id);
            }

            // Ground Items - сохраняем выбранные с количеством
            _subRoom.GroundItems.Clear();
            foreach (DataGridViewRow row in _gridGroundItems.Rows)
            {
                try
                {
                    bool selected = Convert.ToBoolean(row.Cells["Selected"].Value);
                    if (!selected) continue;
                }
                catch
                {
                    continue;
                }

                if (!int.TryParse(Convert.ToString(row.Cells["ID"].Value), out int id)) continue;
                int count = 1;
                int.TryParse(Convert.ToString(row.Cells["Count"].Value), out count);
                count = Math.Max(1, count);
                _subRoom.GroundItems.Add(new InventoryItemData(id, count));
            }

            // Room connections
            _subRoom.RoomToNorth = GetRoomConnection(_cmbRoomNorth);
            _subRoom.RoomToEast = GetRoomConnection(_cmbRoomEast);
            _subRoom.RoomToSouth = GetRoomConnection(_cmbRoomSouth);
            _subRoom.RoomToWest = GetRoomConnection(_cmbRoomWest);
        }

        private int? GetRoomConnection(ComboBox cmb)
        {
            var item = cmb.SelectedItem as ComboBoxItem;
            return item?.Value > 0 ? item.Value : null;
        }

        public RoomData GetSubRoom() => _subRoom;

        // Helper classes
        private class ComboBoxItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public override string ToString() => Text;
        }
    }
}
