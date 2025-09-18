using System;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class ChestSelectionForm : Form
    {
        private readonly GameData _gameData;
        private DataGridView _gridChests;
        private Button _btnOk;
        private Button _btnCancel;
        private int _selectedChestId = 0;

        public ChestSelectionForm(GameData gameData)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            InitializeComponent();
            LoadChests();
        }

        private void InitializeComponent()
        {
            Text = "Выбор сундука";
            Size = new System.Drawing.Size(600, 400);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _gridChests = new DataGridView
            {
                Location = new System.Drawing.Point(10, 10),
                Size = new System.Drawing.Size(570, 300),
                ReadOnly = true,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false,
                AutoGenerateColumns = false
            };

            // Настройка колонок
            _gridChests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "ID",
                HeaderText = "ID",
                DataPropertyName = "ID",
                Width = 60
            });

            _gridChests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Name",
                HeaderText = "Название",
                DataPropertyName = "Name",
                Width = 200
            });

            _gridChests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Description",
                HeaderText = "Описание",
                DataPropertyName = "Description",
                Width = 250
            });

            _gridChests.Columns.Add(new DataGridViewTextBoxColumn
            {
                Name = "Status",
                HeaderText = "Статус",
                Width = 100
            });

            _btnOk = new Button { Text = "OK", Location = new System.Drawing.Point(420, 320), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.OK };
            _btnCancel = new Button { Text = "Отмена", Location = new System.Drawing.Point(510, 320), Size = new System.Drawing.Size(80, 30), DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { _gridChests, _btnOk, _btnCancel });

            // Обработчики событий
            _gridChests.SelectionChanged += (s, e) => UpdateSelectedChest();
            _gridChests.CellDoubleClick += (s, e) => { DialogResult = DialogResult.OK; Close(); };
        }

        private void LoadChests()
        {
            if (_gameData.Chests != null)
            {
                _gridChests.DataSource = _gameData.Chests.ToList();

                // Обновляем статус для каждого сундука
                foreach (DataGridViewRow row in _gridChests.Rows)
                {
                    if (row.DataBoundItem is ChestData chest)
                    {
                        string status = "";
                        if (chest.IsLocked) status += "ЗАПЕРТ ";
                        if (chest.IsTrapped) status += "ЛОВУШКА ";
                        if (string.IsNullOrEmpty(status)) status = "ОТКРЫТ";
                        
                        row.Cells["Status"].Value = status.Trim();
                    }
                }
            }
        }

        private void UpdateSelectedChest()
        {
            if (_gridChests.SelectedRows.Count > 0)
            {
                var selectedRow = _gridChests.SelectedRows[0];
                if (selectedRow.DataBoundItem is ChestData chest)
                {
                    _selectedChestId = chest.ID;
                }
            }
        }

        public int GetSelectedChestId()
        {
            return _selectedChestId;
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            if (DialogResult == DialogResult.OK)
            {
                if (_selectedChestId == 0)
                {
                    MessageBox.Show("Выберите сундук.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    e.Cancel = true;
                    return;
                }
            }

            base.OnFormClosing(e);
        }
    }
}
