using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;
using Engine.Quests;

namespace JsonEditor
{
    public class EditQuestItemSpawnForm : Form
    {
        public List<QuestItemSpawnData> SpawnLocations { get; private set; }
        private GameData _gameData;

        private ListBox lstSpawnLocations;
        private Button btnAddSpawn;
        private Button btnEditSpawn;
        private Button btnRemoveSpawn;
        private Button btnOk;
        private Button btnCancel;

        private BindingList<SpawnLocationDisplayItem> _spawnBinding;

        public EditQuestItemSpawnForm(GameData gameData, List<QuestItemSpawnData> existingSpawns = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            SpawnLocations = existingSpawns ?? new List<QuestItemSpawnData>();

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Настройка спавна предметов квеста";
            this.Width = 600;
            this.Height = 500;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            // Заголовок
            var lblTitle = new Label
            {
                Text = "Локации для спавна предметов квеста",
                Left = 12,
                Top = 12,
                Width = 400,
                Font = new Font("Microsoft Sans Serif", 10, FontStyle.Bold)
            };

            // Список локаций спавна
            var lblSpawnLocations = new Label
            {
                Text = "Локации спавна:",
                Left = 12,
                Top = 45,
                Width = 120
            };

            lstSpawnLocations = new ListBox
            {
                Left = 12,
                Top = 70,
                Width = 550,
                Height = 300
            };

            // Кнопки управления
            btnAddSpawn = new Button { Text = "Добавить", Left = 12, Top = 380, Width = 100 };
            btnEditSpawn = new Button { Text = "Редактировать", Left = 120, Top = 380, Width = 100 };
            btnRemoveSpawn = new Button { Text = "Удалить", Left = 228, Top = 380, Width = 100 };

            btnAddSpawn.Click += BtnAddSpawn_Click;
            btnEditSpawn.Click += BtnEditSpawn_Click;
            btnRemoveSpawn.Click += BtnRemoveSpawn_Click;

            // Кнопки OK/Отмена
            btnOk = new Button { Text = "OK", Left = 400, Top = 420, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Отмена", Left = 490, Top = 420, Width = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.AddRange(new Control[]
            {
                lblTitle, lblSpawnLocations, lstSpawnLocations,
                btnAddSpawn, btnEditSpawn, btnRemoveSpawn,
                btnOk, btnCancel
            });
        }

        private void LoadData()
        {
            // Создаем список элементов для отображения
            var displayItems = SpawnLocations.Select(s => new SpawnLocationDisplayItem(s, _gameData)).ToList();
            _spawnBinding = new BindingList<SpawnLocationDisplayItem>(displayItems);
            lstSpawnLocations.DataSource = _spawnBinding;
        }

        private void BtnAddSpawn_Click(object sender, EventArgs e)
        {
            using (var form = new EditSpawnLocationForm(_gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _spawnBinding.Add(new SpawnLocationDisplayItem(form.SpawnData, _gameData));
                }
            }
        }

        private void BtnEditSpawn_Click(object sender, EventArgs e)
        {
            if (lstSpawnLocations.SelectedItem is SpawnLocationDisplayItem selectedSpawn)
            {
                using (var form = new EditSpawnLocationForm(_gameData, selectedSpawn.SpawnData))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        int index = _spawnBinding.IndexOf(selectedSpawn);
                        _spawnBinding.RemoveAt(index);
                        _spawnBinding.Insert(index, new SpawnLocationDisplayItem(form.SpawnData, _gameData));
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите локацию для редактирования.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnRemoveSpawn_Click(object sender, EventArgs e)
        {
            if (lstSpawnLocations.SelectedItem is SpawnLocationDisplayItem selectedSpawn)
            {
                if (MessageBox.Show("Удалить выбранную локацию спавна?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    _spawnBinding.Remove(selectedSpawn);
                }
            }
            else
            {
                MessageBox.Show("Выберите локацию для удаления.", "Предупреждение", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            SpawnLocations = _spawnBinding.Select(item => item.SpawnData).ToList();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

}
