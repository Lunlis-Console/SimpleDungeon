using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public partial class EditRoomEntranceForm : Form
    {
        private RoomEntranceData _entrance;
        private GameData _gameData;
        private bool _isNew;

        private TextBox _txtId;
        private TextBox _txtName;
        private TextBox _txtDescription;
        private ComboBox _cmbTargetRoom;
        private ComboBox _cmbParentLocation;
        private ComboBox _cmbEntranceType;
        private CheckBox _chkIsLocked;
        private TextBox _txtLockDescription;
        private CheckBox _chkRequiresKey;
        private ComboBox _cmbRequiredKey;
        private CheckedListBox _clbRequiredItems;

        public EditRoomEntranceForm(RoomEntranceData entrance, GameData gameData, bool isNew = false)
        {
            _entrance = entrance ?? new RoomEntranceData();
            _gameData = gameData;
            _isNew = isNew;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            Text = _isNew ? "Новый вход в помещение" : "Редактирование входа в помещение";
            Size = new System.Drawing.Size(500, 600);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            var panel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 2,
                RowCount = 11,
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

            // Target Room
            panel.Controls.Add(new Label { Text = "Целевое помещение:", Anchor = AnchorStyles.Right }, 0, 3);
            _cmbTargetRoom = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbTargetRoom, 1, 3);

            // Parent Location
            panel.Controls.Add(new Label { Text = "Родительская локация:", Anchor = AnchorStyles.Right }, 0, 4);
            _cmbParentLocation = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbParentLocation, 1, 4);

            // Entrance Type
            panel.Controls.Add(new Label { Text = "Тип входа:", Anchor = AnchorStyles.Right }, 0, 5);
            _cmbEntranceType = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbEntranceType, 1, 5);

            // Is Locked
            panel.Controls.Add(new Label { Text = "Заперт:", Anchor = AnchorStyles.Right }, 0, 6);
            _chkIsLocked = new CheckBox { Anchor = AnchorStyles.Left };
            panel.Controls.Add(_chkIsLocked, 1, 6);

            // Lock Description
            panel.Controls.Add(new Label { Text = "Описание замка:", Anchor = AnchorStyles.Right }, 0, 7);
            _txtLockDescription = new TextBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 40, Multiline = true, ScrollBars = ScrollBars.Vertical };
            panel.Controls.Add(_txtLockDescription, 1, 7);

            // Requires Key
            panel.Controls.Add(new Label { Text = "Требует ключ:", Anchor = AnchorStyles.Right }, 0, 8);
            _chkRequiresKey = new CheckBox { Anchor = AnchorStyles.Left };
            panel.Controls.Add(_chkRequiresKey, 1, 8);

            // Required Key
            panel.Controls.Add(new Label { Text = "Требуемый ключ:", Anchor = AnchorStyles.Right }, 0, 9);
            _cmbRequiredKey = new ComboBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, DropDownStyle = ComboBoxStyle.DropDownList };
            panel.Controls.Add(_cmbRequiredKey, 1, 9);

            // Required Items
            panel.Controls.Add(new Label { Text = "Требуемые предметы:", Anchor = AnchorStyles.Right }, 0, 10);
            _clbRequiredItems = new CheckedListBox { Anchor = AnchorStyles.Left | AnchorStyles.Right, Height = 80 };
            panel.Controls.Add(_clbRequiredItems, 1, 10);

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

            // Event handlers
            _chkIsLocked.CheckedChanged += (s, e) => UpdateLockControls();
            _chkRequiresKey.CheckedChanged += (s, e) => UpdateKeyControls();
        }

        private void LoadData()
        {
            // Load entrance types
            _cmbEntranceType.Items.Clear();
            _cmbEntranceType.Items.AddRange(new[] { "entrance", "cave", "dungeon", "city", "building", "tower", "temple", "forest" });

            // Load rooms
            _cmbTargetRoom.Items.Clear();
            foreach (var room in _gameData.Rooms)
            {
                _cmbTargetRoom.Items.Add(new ComboBoxItem { Text = $"{room.Name} (ID: {room.ID})", Value = room.ID });
            }

            // Load locations
            _cmbParentLocation.Items.Clear();
            foreach (var location in _gameData.Locations)
            {
                _cmbParentLocation.Items.Add(new ComboBoxItem { Text = $"{location.Name} (ID: {location.ID})", Value = location.ID });
            }

            // Load items for required key and items
            _cmbRequiredKey.Items.Clear();
            _cmbRequiredKey.Items.Add(new ComboBoxItem { Text = "(Не выбрано)", Value = 0 });
            foreach (var item in _gameData.Items)
            {
                _cmbRequiredKey.Items.Add(new ComboBoxItem { Text = $"{item.Name} (ID: {item.ID})", Value = item.ID });
            }

            _clbRequiredItems.Items.Clear();
            foreach (var item in _gameData.Items)
            {
                _clbRequiredItems.Items.Add(new CheckedListItem { Text = $"{item.Name} (ID: {item.ID})", Value = item.ID });
            }

            // Set current values
            _txtId.Text = _entrance.ID.ToString();
            _txtName.Text = _entrance.Name;
            _txtDescription.Text = _entrance.Description;
            _txtLockDescription.Text = _entrance.LockDescription;
            _chkIsLocked.Checked = _entrance.IsLocked;
            _chkRequiresKey.Checked = _entrance.RequiresKey;

            // Set entrance type
            _cmbEntranceType.Text = _entrance.EntranceType;

            // Set target room
            var targetRoom = _cmbTargetRoom.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == _entrance.TargetRoomID);
            if (targetRoom != null)
                _cmbTargetRoom.SelectedItem = targetRoom;

            // Set parent location
            var parentLocation = _cmbParentLocation.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == _entrance.ParentLocationID);
            if (parentLocation != null)
                _cmbParentLocation.SelectedItem = parentLocation;

            // Set required key
            var requiredKey = _cmbRequiredKey.Items.Cast<ComboBoxItem>().FirstOrDefault(x => x.Value == _entrance.RequiredKeyID);
            if (requiredKey != null)
                _cmbRequiredKey.SelectedItem = requiredKey;

            // Set required items
            foreach (CheckedListItem item in _clbRequiredItems.Items)
            {
                item.Checked = _entrance.RequiredItemIDs.Contains(item.Value);
            }

            UpdateLockControls();
            UpdateKeyControls();
        }

        private void UpdateLockControls()
        {
            bool isLocked = _chkIsLocked.Checked;
            _txtLockDescription.Enabled = isLocked;
        }

        private void UpdateKeyControls()
        {
            bool requiresKey = _chkRequiresKey.Checked;
            _cmbRequiredKey.Enabled = requiresKey;
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
            if (_isNew || _entrance.ID != id)
            {
                if (_gameData.RoomEntrances.Any(r => r.ID == id))
                {
                    MessageBox.Show("Вход с таким ID уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return false;
                }
            }

            if (_cmbTargetRoom.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать целевое помещение.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            if (_cmbParentLocation.SelectedItem == null)
            {
                MessageBox.Show("Необходимо выбрать родительскую локацию.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            return true;
        }

        private void SaveData()
        {
            _entrance.ID = int.Parse(_txtId.Text);
            _entrance.Name = _txtName.Text.Trim();
            _entrance.Description = _txtDescription.Text.Trim();
            _entrance.LockDescription = _txtLockDescription.Text.Trim();
            _entrance.IsLocked = _chkIsLocked.Checked;
            _entrance.RequiresKey = _chkRequiresKey.Checked;
            _entrance.EntranceType = _cmbEntranceType.Text;

            // Target room
            var targetRoom = _cmbTargetRoom.SelectedItem as ComboBoxItem;
            _entrance.TargetRoomID = targetRoom?.Value ?? 0;

            // Parent location
            var parentLocation = _cmbParentLocation.SelectedItem as ComboBoxItem;
            _entrance.ParentLocationID = parentLocation?.Value ?? 0;

            // Required key
            var requiredKey = _cmbRequiredKey.SelectedItem as ComboBoxItem;
            _entrance.RequiredKeyID = requiredKey?.Value ?? 0;

            // Required items
            _entrance.RequiredItemIDs.Clear();
            foreach (CheckedListItem item in _clbRequiredItems.Items)
            {
                if (item.Checked)
                    _entrance.RequiredItemIDs.Add(item.Value);
            }
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
