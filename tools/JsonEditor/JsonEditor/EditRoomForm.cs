using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public partial class EditRoomForm : Form
    {
        private RoomData _room;
        private GameData _gameData;
        private bool _isNew;

        private TextBox _txtId;
        private TextBox _txtName;
        private TextBox _txtDescription;
        private ComboBox _cmbParentLocation;
        private CheckedListBox _clbNPCs;
        private CheckedListBox _clbMonsterTemplates;
        private CheckedListBox _clbGroundItems;
        private CheckBox _chkScaleMonsters;
        private ComboBox _cmbRoomNorth;
        private ComboBox _cmbRoomEast;
        private ComboBox _cmbRoomSouth;
        private ComboBox _cmbRoomWest;

        public EditRoomForm(RoomData room, GameData gameData, bool isNew = false)
        {
            _room = room ?? new RoomData();
            _gameData = gameData;
            _isNew = isNew;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = _isNew ? "Новое помещение" : "Редактирование помещения";
            Size = new System.Drawing.Size(600, 700);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 12,
                Padding = new Padding(10)
            };

            // ID
            panel.Controls.Add(new Label { Text = "ID:", Anchor = AnchorStyles.Right }, 0, 0);
            _txtId = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            panel.Controls.Add(_txtId, 1, 0);

            // Name
            panel.Controls.Add(new Label { Text = "Название:", Anchor = AnchorStyles.Right }, 0, 1);
            _txtName = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right };
            panel.Controls.Add(_txtName, 1, 1);

            // Description
            panel.Controls.Add(new Label { Text = "Описание:", Anchor = AnchorStyles.Right }, 0, 2);
            _txtDescription = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 60, Multiline = true, ScrollBars = ScrollBars.Vertical };
            panel.Controls.Add(_txtDescription, 1, 2);

            // Parent Location
            panel.Controls.Add(new Label { Text = "Родительская локация:", Anchor = AnchorStyles.Right }, 0, 3);
            _cmbParentLocation = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbParentLocation, 1, 3);

            // NPCs
            panel.Controls.Add(new Label { Text = "NPC в помещении:", Anchor = AnchorStyles.Right }, 0, 4);
            _clbNPCs = new CheckedListBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 80 };
            panel.Controls.Add(_clbNPCs, 1, 4);

            // Monster Templates
            panel.Controls.Add(new Label { Text = "Шаблоны монстров:", Anchor = AnchorStyles.Right }, 0, 5);
            _clbMonsterTemplates = new CheckedListBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 80 };
            panel.Controls.Add(_clbMonsterTemplates, 1, 5);

            // Ground Items
            panel.Controls.Add(new Label { Text = "Предметы на земле:", Anchor = AnchorStyles.Right }, 0, 6);
            _clbGroundItems = new CheckedListBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 80 };
            panel.Controls.Add(_clbGroundItems, 1, 6);

            // Scale Monsters
            panel.Controls.Add(new Label { Text = "Масштабировать монстров:", Anchor = AnchorStyles.Right }, 0, 7);
            _chkScaleMonsters = new CheckBox { Anchor = AnchorStyles.Left };
            panel.Controls.Add(_chkScaleMonsters, 1, 7);

            // Room Connections
            panel.Controls.Add(new Label { Text = "Помещение на севере:", Anchor = AnchorStyles.Right }, 0, 8);
            _cmbRoomNorth = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbRoomNorth, 1, 8);

            panel.Controls.Add(new Label { Text = "Помещение на востоке:", Anchor = AnchorStyles.Right }, 0, 9);
            _cmbRoomEast = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbRoomEast, 1, 9);

            panel.Controls.Add(new Label { Text = "Помещение на юге:", Anchor = AnchorStyles.Right }, 0, 10);
            _cmbRoomSouth = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbRoomSouth, 1, 10);

            panel.Controls.Add(new Label { Text = "Помещение на западе:", Anchor = AnchorStyles.Right }, 0, 11);
            _cmbRoomWest = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbRoomWest, 1, 11);

            // Buttons
            var buttonPanel = new FlowLayoutPanel
            {
                Dock = DockStyle.Bottom,
                FlowDirection = FlowDirection.RightToLeft,
                Padding = new Padding(10)
            };

            var btnCancel = new Button { Text = "Отмена", DialogResult = DialogResult.Cancel, Size = new System.Drawing.Size(75, 23) };
            var btnOK = new Button { Text = "OK", DialogResult = DialogResult.OK, Size = new System.Drawing.Size(75, 23) };

            buttonPanel.Controls.Add(btnCancel);
            buttonPanel.Controls.Add(btnOK);

            Controls.Add(panel);
            Controls.Add(buttonPanel);

            AcceptButton = btnOK;
            CancelButton = btnCancel;
        }

        private void LoadData()
        {
            // Load locations
            _cmbParentLocation.Items.Clear();
            _cmbParentLocation.Items.Add(new ComboBoxItem { Text = "(Не выбрано)", Value = 0 });
            foreach (var location in _gameData.Locations)
            {
                _cmbParentLocation.Items.Add(new ComboBoxItem { Text = $"{location.Name} (ID: {location.ID})", Value = location.ID });
            }

            // Load NPCs
            _clbNPCs.Items.Clear();
            foreach (var npc in _gameData.NPCs)
            {
                _clbNPCs.Items.Add(new CheckedListItem { Text = $"{npc.Name} (ID: {npc.ID})", Value = npc.ID });
            }

            // Load monster templates
            _clbMonsterTemplates.Items.Clear();
            foreach (var monster in _gameData.Monsters)
            {
                _clbMonsterTemplates.Items.Add(new CheckedListItem { Text = $"{monster.Name} (ID: {monster.ID})", Value = monster.ID });
            }

            // Load items for ground items
            _clbGroundItems.Items.Clear();
            foreach (var item in _gameData.Items)
            {
                _clbGroundItems.Items.Add(new CheckedListItem { Text = $"{item.Name} (ID: {item.ID})", Value = item.ID });
            }

            // Load rooms for connections
            var roomComboBoxes = new[] { _cmbRoomNorth, _cmbRoomEast, _cmbRoomSouth, _cmbRoomWest };
            foreach (var cmb in roomComboBoxes)
            {
                cmb.Items.Clear();
                cmb.Items.Add(new ComboBoxItem { Text = "(Нет)", Value = 0 });
                foreach (var room in _gameData.Rooms)
                {
                    cmb.Items.Add(new ComboBoxItem { Text = $"{room.Name} (ID: {room.ID})", Value = room.ID });
                }
            }

            // Set current values
            _txtId.Text = _room.ID.ToString();
            _txtName.Text = _room.Name;
            _txtDescription.Text = _room.Description;
            _chkScaleMonsters.Checked = _room.ScaleMonstersToPlayerLevel;

            // Set parent location
            var parentLocation = _cmbParentLocation.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == _room.ParentLocationID);
            if (parentLocation != null)
                _cmbParentLocation.SelectedItem = parentLocation;

            // Set NPCs
            foreach (CheckedListItem item in _clbNPCs.Items)
            {
                item.Checked = _room.NPCsHere.Contains(item.Value);
            }

            // Set monster templates
            foreach (CheckedListItem item in _clbMonsterTemplates.Items)
            {
                item.Checked = _room.MonsterTemplates.Contains(item.Value);
            }

            // Set ground items (simplified - just show which items are available)
            // Note: Ground items have quantity, so this is a simplified view

            // Set room connections
            SetRoomConnection(_cmbRoomNorth, _room.RoomToNorth);
            SetRoomConnection(_cmbRoomEast, _room.RoomToEast);
            SetRoomConnection(_cmbRoomSouth, _room.RoomToSouth);
            SetRoomConnection(_cmbRoomWest, _room.RoomToWest);
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
            if (_isNew || _room.ID != id)
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
            _room.ID = int.Parse(_txtId.Text);
            _room.Name = _txtName.Text.Trim();
            _room.Description = _txtDescription.Text.Trim();
            _room.ScaleMonstersToPlayerLevel = _chkScaleMonsters.Checked;

            // Parent location
            var parentLocation = _cmbParentLocation.SelectedItem as ComboBoxItem;
            _room.ParentLocationID = parentLocation?.Value ?? 0;

            // NPCs
            _room.NPCsHere.Clear();
            foreach (CheckedListItem item in _clbNPCs.Items)
            {
                if (item.Checked)
                    _room.NPCsHere.Add(item.Value);
            }

            // Monster templates
            _room.MonsterTemplates.Clear();
            foreach (CheckedListItem item in _clbMonsterTemplates.Items)
            {
                if (item.Checked)
                    _room.MonsterTemplates.Add(item.Value);
            }

            // Room connections
            _room.RoomToNorth = GetRoomConnection(_cmbRoomNorth);
            _room.RoomToEast = GetRoomConnection(_cmbRoomEast);
            _room.RoomToSouth = GetRoomConnection(_cmbRoomSouth);
            _room.RoomToWest = GetRoomConnection(_cmbRoomWest);
        }

        private int? GetRoomConnection(ComboBox cmb)
        {
            var item = cmb.SelectedItem as ComboBoxItem;
            return item?.Value > 0 ? item.Value : null;
        }

        // Helper classes
        private class ComboBoxItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public override string ToString() => Text;
        }

        private class CheckedListItem
        {
            public string Text { get; set; }
            public int Value { get; set; }
            public bool Checked { get; set; }
            public override string ToString() => Text;
        }
    }
}
