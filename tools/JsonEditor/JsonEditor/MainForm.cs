using Engine.Data; // <- должен быть доступен (Engine.dll or project reference)
using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Windows.Forms;
using Engine.Entities;

namespace JsonEditor
{
    public class MainForm : Form
    {
        // Жёсткий путь, как ты просил:
        private readonly string DefaultGameDataPath = @"E:\CSharpProjects\SuperAdventureCSharp\SimpleDungeon\SimpleDungeon\Data\game_data.json";

        private GameData _gameData;
        private string _filePath;

        private Label lblPath;
        private TabControl tabControl;
        private TabPage tabItems;
        private TabPage tabMonsters;

        private ListBox listBoxItems;
        private Button btnAddItem;
        private Button btnRemoveItem;

        private ListBox listBoxMonsters;
        private Button btnAddMonster;
        private Button btnRemoveMonster;

        private Button btnSave;

        public MainForm()
        {
            Text = "JsonEditor";
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            CreateControls();
            InitFileAndLoad();
        }

        private void CreateControls()
        {
            lblPath = new Label { Left = 10, Top = 8, Width = 760, Text = "Файл: (не выбран)" };

            tabControl = new TabControl { Left = 10, Top = 30, Width = 760, Height = 480 };

            // Items tab
            tabItems = new TabPage("Items");
            listBoxItems = new ListBox { Dock = DockStyle.Fill };
            listBoxItems.DoubleClick += listBoxItems_DoubleClick;
            btnAddItem = new Button { Text = "Добавить предмет", Dock = DockStyle.Bottom, Height = 30 };
            btnRemoveItem = new Button { Text = "Удалить предмет", Dock = DockStyle.Bottom, Height = 30 };
            btnAddItem.Click += btnAddItem_Click;
            btnRemoveItem.Click += btnRemoveItem_Click;
            tabItems.Controls.Add(listBoxItems);
            tabItems.Controls.Add(btnRemoveItem);
            tabItems.Controls.Add(btnAddItem);

            // Monsters tab
            tabMonsters = new TabPage("Monsters");
            listBoxMonsters = new ListBox { Dock = DockStyle.Fill };
            listBoxMonsters.DoubleClick += listBoxMonsters_DoubleClick;
            btnAddMonster = new Button { Text = "Добавить монстра", Dock = DockStyle.Bottom, Height = 30 };
            btnRemoveMonster = new Button { Text = "Удалить монстра", Dock = DockStyle.Bottom, Height = 30 };
            btnAddMonster.Click += btnAddMonster_Click;
            btnRemoveMonster.Click += btnRemoveMonster_Click;
            tabMonsters.Controls.Add(listBoxMonsters);
            tabMonsters.Controls.Add(btnRemoveMonster);
            tabMonsters.Controls.Add(btnAddMonster);

            tabControl.TabPages.Add(tabItems);
            tabControl.TabPages.Add(tabMonsters);

            btnSave = new Button { Text = "Сохранить JSON", Left = 10, Top = 520, Width = 760, Height = 30 };
            btnSave.Click += btnSave_Click;

            Controls.Add(lblPath);
            Controls.Add(tabControl);
            Controls.Add(btnSave);
        }

        private void InitFileAndLoad()
        {
            // Сначала пробуем жёсткий путь
            if (File.Exists(DefaultGameDataPath))
            {
                _filePath = DefaultGameDataPath;
            }
            else
            {
                // Если жёсткий путь не найден — предложим выбрать
                var dr = MessageBox.Show(
                    $"Файл по жёсткому пути не найден:\n{DefaultGameDataPath}\n\nВы хотите выбрать файл вручную?",
                    "Файл не найден", MessageBoxButtons.YesNo, MessageBoxIcon.Question);

                if (dr == DialogResult.Yes)
                {
                    using var ofd = new OpenFileDialog
                    {
                        Title = "Выберите game_data.json",
                        Filter = "JSON Files (*.json)|*.json|All Files (*.*)|*.*",
                        InitialDirectory = Path.GetDirectoryName(DefaultGameDataPath)
                    };
                    if (ofd.ShowDialog() == DialogResult.OK)
                        _filePath = ofd.FileName;
                    else
                    {
                        MessageBox.Show("Файл не выбран — редактор закроется.", "Отмена", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        Close();
                        return;
                    }
                }
                else
                {
                    MessageBox.Show("Редактор закрывается.", "Отсутствует файл", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    Close();
                    return;
                }
            }

            lblPath.Text = $"Файл: {_filePath}";
            LoadJson();
        }

        private void LoadJson()
        {
            try
            {
                var json = File.ReadAllText(_filePath);
                _gameData = JsonSerializer.Deserialize<GameData>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                if (_gameData == null)
                    _gameData = new GameData();

                RefreshUI();
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка загрузки JSON: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveJson()
        {
            try
            {
                var json = JsonSerializer.Serialize(_gameData, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_filePath, json);
                MessageBox.Show("Файл сохранен.", "Сохранение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка сохранения: " + ex.Message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void RefreshUI()
        {
            // Items
            listBoxItems.DataSource = null;
            if (_gameData.Items != null)
            {
                listBoxItems.DataSource = _gameData.Items;
                listBoxItems.DisplayMember = "Name";
            }

            // Monsters
            listBoxMonsters.DataSource = null;
            if (_gameData.Monsters != null)
            {
                listBoxMonsters.DataSource = _gameData.Monsters;
                listBoxMonsters.DisplayMember = "Name";
            }
        }

        // ===== Items =====
        private void btnAddItem_Click(object sender, EventArgs e)
        {
            var nextId = (_gameData.Items != null && _gameData.Items.Any()) ? _gameData.Items.Max(i => i.ID) + 1 : 1000;
            var newItem = new ItemData { ID = nextId, Name = "Новый предмет", Description = "" };
            _gameData.Items.Add(newItem);
            RefreshUI();
        }

        private void btnRemoveItem_Click(object sender, EventArgs e)
        {
            if (listBoxItems.SelectedIndex >= 0)
            {
                var idx = listBoxItems.SelectedIndex;
                _gameData.Items.RemoveAt(idx);
                RefreshUI();
            }
        }

        private void listBoxItems_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxItems.SelectedItem == null) return;

            var sel = listBoxItems.SelectedItem;

            // Сначала попробуем сущность Item (Engine.Entities.Item)
            if (sel is Engine.Entities.Item ent)
            {
                using var f = new EditItemForm(ent);
                if (f.ShowDialog() == DialogResult.OK)
                {
                    RefreshUI();
                    SaveJson();
                }
                return;
            }

            // Если это DTO / data-класс (Engine.Data.ItemData)
            if (sel is Engine.Data.ItemData data)
            {
                using var f = new EditItemForm(data); // добавим этот конструктор в EditItemForm
                if (f.ShowDialog() == DialogResult.OK)
                {
                    RefreshUI();
                    SaveJson();
                }
                return;
            }

            // На всякий случай — показать тип если неизвестен
            MessageBox.Show($"Неизвестный тип элемента: {sel.GetType().FullName}");
        }



        // ===== Monsters =====
        private void btnAddMonster_Click(object sender, EventArgs e)
        {
            var nextId = (_gameData.Monsters != null && _gameData.Monsters.Any()) ? _gameData.Monsters.Max(m => m.ID) + 1 : 2000;
            var newMonster = new MonsterData
            {
                ID = nextId,
                Name = "Новый монстр",
                Level = 1,
                MaximumHP = 10,
                CurrentHP = 10,
                RewardEXP = 0,
                RewardGold = 0,
                Attributes = new Attributes()
            };
            _gameData.Monsters.Add(newMonster);
            RefreshUI();
        }

        private void btnRemoveMonster_Click(object sender, EventArgs e)
        {
            if (listBoxMonsters.SelectedIndex >= 0)
            {
                var idx = listBoxMonsters.SelectedIndex;
                _gameData.Monsters.RemoveAt(idx);
                RefreshUI();
            }
        }

        private void listBoxMonsters_DoubleClick(object sender, EventArgs e)
        {
            if (listBoxMonsters.SelectedIndex < 0) return;
            var monster = (MonsterData)listBoxMonsters.SelectedItem;
            using (var f = new EditMonsterForm(monster, _gameData))
            {
                if (f.ShowDialog() == DialogResult.OK)
                {
                    RefreshUI();
                    SaveJson();
                }
            }

        }

        // ===== Save button =====
        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveJson();
        }
    }
}
