using Engine.Core;
using Engine.Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;

namespace JsonEditor
{
    public class MainForm : Form
    {
        private GameData _gameData;
        private string _currentFilePath;

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem openMenuItem;
        private ToolStripMenuItem saveMenuItem;
        private ToolStripMenuItem saveAsMenuItem;
        private ToolStripMenuItem exitMenuItem;

        private ToolStripMenuItem editMenu;
        private ToolStripMenuItem addItemMenuItem;
        private ToolStripMenuItem addMonsterMenuItem;
        private ToolStripMenuItem addLocationMenuItem;
        private ToolStripMenuItem addQuestMenuItem;

        private TabControl tabControl;
        private TabPage tabItems;
        private TabPage tabMonsters;
        private TabPage tabLocations;
        private TabPage tabQuests;

        private DataGridView gridItems;
        private DataGridView gridMonsters;
        private DataGridView gridLocations;
        private DataGridView gridQuests;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        public MainForm()
        {
            Text = "JsonEditor";
            Width = 1000;
            Height = 700;
            StartPosition = FormStartPosition.CenterScreen;

            InitializeUi();
        }

        private void InitializeUi()
        {
            // Меню
            menuStrip = new MenuStrip();

            // Файл меню
            fileMenu = new ToolStripMenuItem("Файл");
            openMenuItem = new ToolStripMenuItem("Открыть");
            saveMenuItem = new ToolStripMenuItem("Сохранить");
            saveAsMenuItem = new ToolStripMenuItem("Сохранить как...");
            exitMenuItem = new ToolStripMenuItem("Выход");

            openMenuItem.ShortcutKeys = Keys.Control | Keys.O;
            saveMenuItem.ShortcutKeys = Keys.Control | Keys.S;
            saveAsMenuItem.ShortcutKeys = Keys.Control | Keys.Shift | Keys.S;

            openMenuItem.Click += OpenToolStripMenuItem_Click;
            saveMenuItem.Click += SaveToolStripMenuItem_Click;
            saveAsMenuItem.Click += SaveAsToolStripMenuItem_Click;
            exitMenuItem.Click += (s, e) => Close();

            fileMenu.DropDownItems.Add(openMenuItem);
            fileMenu.DropDownItems.Add(saveMenuItem);
            fileMenu.DropDownItems.Add(saveAsMenuItem);
            fileMenu.DropDownItems.Add(new ToolStripSeparator());
            fileMenu.DropDownItems.Add(exitMenuItem);

            // Редактирование меню
            editMenu = new ToolStripMenuItem("Редактирование");
            addItemMenuItem = new ToolStripMenuItem("Добавить предмет");
            addMonsterMenuItem = new ToolStripMenuItem("Добавить монстра");
            addLocationMenuItem = new ToolStripMenuItem("Добавить локацию");
            addQuestMenuItem = new ToolStripMenuItem("Добавить квест");

            addItemMenuItem.Click += (s, e) => AddNewItem();
            addMonsterMenuItem.Click += (s, e) => AddNewMonster();
            addLocationMenuItem.Click += (s, e) => AddNewLocation();
            addQuestMenuItem.Click += (s, e) => AddNewQuest();

            editMenu.DropDownItems.Add(addItemMenuItem);
            editMenu.DropDownItems.Add(addMonsterMenuItem);
            editMenu.DropDownItems.Add(addLocationMenuItem);
            editMenu.DropDownItems.Add(addQuestMenuItem);

            menuStrip.Items.Add(fileMenu);
            menuStrip.Items.Add(editMenu);

            // TabControl
            tabControl = new TabControl { Dock = DockStyle.Fill };

            // Вкладка предметов
            tabItems = new TabPage("Предметы");
            gridItems = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridItems.CellDoubleClick += (s, e) => EditSelectedItem();
            tabItems.Controls.Add(gridItems);

            // Вкладка монстров
            tabMonsters = new TabPage("Монстры");
            gridMonsters = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridMonsters.CellDoubleClick += (s, e) => EditSelectedMonster();
            tabMonsters.Controls.Add(gridMonsters);

            // Вкладка локаций
            tabLocations = new TabPage("Локации");
            gridLocations = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridLocations.CellDoubleClick += (s, e) => EditSelectedLocation();
            tabLocations.Controls.Add(gridLocations);

            // Вкладка квестов
            tabQuests = new TabPage("Квесты");
            gridQuests = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect
            };
            gridQuests.CellDoubleClick += (s, e) => EditSelectedQuest();
            tabQuests.Controls.Add(gridQuests);

            tabControl.TabPages.AddRange(new[] { tabItems, tabMonsters, tabLocations, tabQuests });

            // StatusStrip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Text = "Готов" };
            statusStrip.Items.Add(statusLabel);

            // Панель кнопок
            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            var btnAddItem = new Button { Text = "Добавить предмет", Left = 10, Top = 8, Width = 120 };
            var btnAddMonster = new Button { Text = "Добавить монстра", Left = 140, Top = 8, Width = 120 };
            var btnAddLocation = new Button { Text = "Добавить локацию", Left = 270, Top = 8, Width = 120 };
            var btnAddQuest = new Button { Text = "Добавить квест", Left = 400, Top = 8, Width = 120 };

            btnAddItem.Click += (s, e) => AddNewItem();
            btnAddMonster.Click += (s, e) => AddNewMonster();
            btnAddLocation.Click += (s, e) => AddNewLocation();
            btnAddQuest.Click += (s, e) => AddNewQuest();

            buttonPanel.Controls.AddRange(new Control[] { btnAddItem, btnAddMonster, btnAddLocation, btnAddQuest });

            // Размещение элементов
            Controls.Add(tabControl);
            Controls.Add(buttonPanel);
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);

            MainMenuStrip = menuStrip;

            // Контекстное меню для DataGridView
            var contextMenu = new ContextMenuStrip();
            var editMenuItem = new ToolStripMenuItem("Редактировать");
            var deleteMenuItem = new ToolStripMenuItem("Удалить");

            editMenuItem.Click += (s, e) => EditSelectedItem();
            deleteMenuItem.Click += (s, e) => DeleteSelectedItem();

            contextMenu.Items.Add(editMenuItem);
            contextMenu.Items.Add(deleteMenuItem);

            gridItems.ContextMenuStrip = contextMenu;
            gridMonsters.ContextMenuStrip = contextMenu;
            gridLocations.ContextMenuStrip = contextMenu;
            gridQuests.ContextMenuStrip = contextMenu;

            UpdateMenuState();
        }

        private void UpdateMenuState()
        {
            bool hasData = _gameData != null;
            saveMenuItem.Enabled = hasData;
            saveAsMenuItem.Enabled = hasData;
            editMenu.Enabled = hasData;
        }

        private void OpenToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _gameData = SerializerHelper.LoadGameData(ofd.FileName);
                        _currentFilePath = ofd.FileName;
                        RefreshDataGrids();
                        Text = $"JsonEditor - {Path.GetFileName(ofd.FileName)}";
                        statusLabel.Text = $"Загружено: {_gameData.Items?.Count ?? 0} предметов, " +
                                          $"{_gameData.Monsters?.Count ?? 0} монстров";

                        UpdateMenuState();
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void SaveToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_gameData == null)
            {
                MessageBox.Show("Нет данных для сохранения.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveAsToolStripMenuItem_Click(sender, e);
                return;
            }

            try
            {
                SerializerHelper.SaveGameData(_gameData, _currentFilePath);
                MessageBox.Show("Файл сохранён успешно.", "Успех",
                    MessageBoxButtons.OK, MessageBoxIcon.Information);
                statusLabel.Text = $"Сохранено: {Path.GetFileName(_currentFilePath)}";
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка",
                    MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void SaveAsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (_gameData == null)
            {
                MessageBox.Show("Нет данных для сохранения.", "Внимание",
                    MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (var sfd = new SaveFileDialog())
            {
                sfd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (sfd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        SerializerHelper.SaveGameData(_gameData, sfd.FileName);
                        _currentFilePath = sfd.FileName;
                        Text = $"JsonEditor - {Path.GetFileName(sfd.FileName)}";
                        MessageBox.Show("Файл сохранён успешно.", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                        statusLabel.Text = $"Сохранено как: {Path.GetFileName(_currentFilePath)}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RefreshDataGrids()
        {
            // Предметы
            gridItems.DataSource = _gameData?.Items?.Select(i => new
            {
                i.ID,
                i.Name,
                i.Type,
                i.Price,
                Description = i.Description?.Length > 50 ? i.Description.Substring(0, 50) + "..." : i.Description
            }).ToList();

            // Монстры
            gridMonsters.DataSource = _gameData?.Monsters?.Select(m => new
            {
                m.ID,
                m.Name,
                m.Level,
                m.CurrentHP,
                m.MaximumHP,
                m.RewardEXP,
                m.RewardGold
            }).ToList();

            // Локации
            gridLocations.DataSource = _gameData?.Locations?.Select(l => new
            {
                l.ID,
                l.Name,
                NPCs = l.NPCsHere?.Count ?? 0,
            }).ToList();

            // Квесты
            gridQuests.DataSource = _gameData?.Quests?.Select(q => new
            {
                q.ID,
                q.Name,
                RewardGold = q.RewardGold,
                QuestItems = q.QuestItems?.Count ?? 0
            }).ToList();

            UpdateMenuState();
        }

        private void EditSelectedItem()
        {
            if (gridItems.SelectedRows.Count > 0)
            {
                var selectedId = (int)gridItems.SelectedRows[0].Cells["ID"].Value;
                var item = _gameData.Items?.FirstOrDefault(i => i.ID == selectedId);
                if (item != null) EditItem(item);
            }
        }

        private void EditSelectedMonster()
        {
            if (gridMonsters.SelectedRows.Count > 0)
            {
                var selectedId = (int)gridMonsters.SelectedRows[0].Cells["ID"].Value;
                var monster = _gameData.Monsters?.FirstOrDefault(m => m.ID == selectedId);
                if (monster != null) EditMonster(monster);
            }
        }

        private void EditSelectedLocation()
        {
            if (gridLocations.SelectedRows.Count > 0)
            {
                var selectedId = (int)gridLocations.SelectedRows[0].Cells["ID"].Value;
                var location = _gameData.Locations?.FirstOrDefault(l => l.ID == selectedId);
                if (location != null) EditLocation(location);
            }
        }

        private void EditSelectedQuest()
        {
            if (gridQuests.SelectedRows.Count > 0)
            {
                var selectedId = (int)gridQuests.SelectedRows[0].Cells["ID"].Value;
                var quest = _gameData.Quests?.FirstOrDefault(q => q.ID == selectedId);
                if (quest != null) EditQuest(quest);
            }
        }

        private void DeleteSelectedItem()
        {
            var currentGrid = GetCurrentDataGridView();
            if (currentGrid?.SelectedRows.Count > 0)
            {
                var selectedId = (int)currentGrid.SelectedRows[0].Cells["ID"].Value;
                var result = MessageBox.Show("Вы уверены, что хотите удалить этот элемент?",
                    "Подтверждение удаления", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);

                if (result == DialogResult.Yes)
                {
                    try
                    {
                        if (currentGrid == gridItems)
                            _gameData.Items?.RemoveAll(i => i.ID == selectedId);
                        else if (currentGrid == gridMonsters)
                            _gameData.Monsters?.RemoveAll(m => m.ID == selectedId);
                        else if (currentGrid == gridLocations)
                            _gameData.Locations?.RemoveAll(l => l.ID == selectedId);
                        else if (currentGrid == gridQuests)
                            _gameData.Quests?.RemoveAll(q => q.ID == selectedId);

                        RefreshDataGrids();
                        statusLabel.Text = "Элемент удалён";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка удаления: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private DataGridView GetCurrentDataGridView()
        {
            return tabControl.SelectedTab?.Controls.OfType<DataGridView>().FirstOrDefault();
        }

        private void EditItem(ItemData item)
        {
            using (var form = new EditItemForm(item))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDataGrids();
                    statusLabel.Text = "Предмет обновлён";
                }
            }
        }

        private void EditMonster(MonsterData monster)
        {
            using (var form = new EditMonsterForm(monster, _gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDataGrids();
                    statusLabel.Text = "Монстр обновлён";
                }
            }
        }

        private void EditLocation(LocationData location)
        {
            MessageBox.Show("Редактирование локаций пока не реализовано");
        }

        private void EditQuest(QuestData quest)
        {
            MessageBox.Show("Редактирование квестов пока не реализовано");
        }

        private void AddNewItem()
        {
            var newItem = new ItemData
            {
                ID = GetNextAvailableId(_gameData.Items),
                Name = "Новый предмет",
                NamePlural = "Новые предметы",
                Type = ItemType.Stuff,
                Price = 0,
                Description = ""
            };

            using (var form = new EditItemForm(newItem))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Items ??= new List<ItemData>();
                    _gameData.Items.Add(form.EditedItemData);
                    RefreshDataGrids();
                    statusLabel.Text = "Новый предмет добавлен";
                    tabControl.SelectedTab = tabItems;
                }
            }
        }

        private void AddNewMonster()
        {
            var newMonster = new MonsterData
            {
                ID = GetNextAvailableId(_gameData.Monsters),
                Name = "Новый монстр",
                Level = 1,
                CurrentHP = 10,
                MaximumHP = 10,
                RewardEXP = 10,
                RewardGold = 5
            };

            using (var form = new EditMonsterForm(newMonster, _gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Monsters ??= new List<MonsterData>();
                    _gameData.Monsters.Add(newMonster);
                    RefreshDataGrids();
                    statusLabel.Text = "Новый монстр добавлен";
                    tabControl.SelectedTab = tabMonsters;
                }
            }
        }

        private void AddNewLocation()
        {
            MessageBox.Show("Добавление локаций пока не реализовано");
        }

        private void AddNewQuest()
        {
            MessageBox.Show("Добавление квестов пока не реализовано");
        }

        private int GetNextAvailableId<T>(List<T> list) where T : class
        {
            if (list == null || list.Count == 0) return 1;

            int maxId = 0;
            foreach (var item in list)
            {
                int id = item switch
                {
                    ItemData i => i.ID,
                    MonsterData m => m.ID,
                    LocationData l => l.ID,
                    QuestData q => q.ID,
                    _ => 0
                };
                maxId = Math.Max(maxId, id);
            }
            return maxId + 1;
        }
    }
}