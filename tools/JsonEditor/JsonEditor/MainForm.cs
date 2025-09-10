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
        private TabPage tabDialogues;

        private DataGridView gridNPCs;
        private DataGridView gridItems;
        private DataGridView gridMonsters;
        private DataGridView gridLocations;
        private DataGridView gridQuests;
        private DataGridView gridDialogues;

        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        private Panel panelDialoguesButtons;

        private Button btnAddDialogue;
        private Button btnEditDialogue;
        private Button btnDeleteDialogue;

        private Button btnAddNPC;
        private Button btnEditNPC;
        private Button btnDeleteNPC;

        private Button btnAddLocation;
        private Button btnEditLocation;
        private Button btnDeleteLocation;

        private ComboBox comboGreetingDialogue;

        
        




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

            tabControl.TabPages.AddRange(new[] { tabItems, tabMonsters, tabQuests });

            // === Dialogues tab initialization ===
            tabDialogues = new TabPage("Диалоги");
            gridDialogues = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            panelDialoguesButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 36
            };

            btnAddDialogue = new Button { Text = "Добавить", Width = 90, Left = 8, Top = 5 };
            btnEditDialogue = new Button { Text = "Редактировать", Width = 110, Left = 110, Top = 5 };
            btnDeleteDialogue = new Button { Text = "Удалить", Width = 90, Left = 230, Top = 5 };

            btnAddDialogue.Click += (s, e) => AddDialogue();
            btnEditDialogue.Click += (s, e) => EditSelectedDialogue();
            btnDeleteDialogue.Click += (s, e) => DeleteSelectedDialogue();
            gridDialogues.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelectedDialogue(); };

            panelDialoguesButtons.Controls.Add(btnAddDialogue);
            panelDialoguesButtons.Controls.Add(btnEditDialogue);
            panelDialoguesButtons.Controls.Add(btnDeleteDialogue);

            tabDialogues.Controls.Add(gridDialogues);
            tabDialogues.Controls.Add(panelDialoguesButtons);

            // Добавим вкладку в главный TabControl (название у тебя может отличаться, заменяй на свой)
            tabControl.TabPages.Add(tabDialogues);

            // После создания интерфейса — отрисуем данные
            RefreshDialoguesGrid();

            // Создаём вкладку NPC
            var tabNPCs = new TabPage("NPC");
            gridNPCs = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 400,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            var panelNPCButtons = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            btnAddNPC = new Button { Text = "Добавить NPC", Left = 8, Width = 110, Top = 4 };
            btnEditNPC = new Button { Text = "Редактировать NPC", Left = 130, Width = 130, Top = 4 };
            btnDeleteNPC = new Button { Text = "Удалить NPC", Left = 270, Width = 110, Top = 4 };

            btnAddNPC.Click += (s, e) => OpenEditNPCForm(null);
            btnEditNPC.Click += (s, e) =>
            {
                var npc = GetSelectedNPCData();
                if (npc != null) OpenEditNPCForm(npc);
            };
            btnDeleteNPC.Click += (s, e) => DeleteSelectedNPC();

            gridNPCs.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var npc = GetSelectedNPCData();
                    if (npc != null) OpenEditNPCForm(npc);
                }
            };

            panelNPCButtons.Controls.Add(btnAddNPC);
            panelNPCButtons.Controls.Add(btnEditNPC);
            panelNPCButtons.Controls.Add(btnDeleteNPC);

            tabNPCs.Controls.Add(gridNPCs);
            tabNPCs.Controls.Add(panelNPCButtons);

            tabControl.TabPages.Add(tabNPCs);

            // В конце инициализации — отображаем NPC
            RefreshNPCGrid();

            // Создаём вкладку Локации
            tabLocations = new TabPage("Locations");

            gridLocations = new DataGridView
            {
                Dock = DockStyle.Top,
                Height = 400,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };

            var panelLocationButtons = new Panel { Dock = DockStyle.Bottom, Height = 36 };
            btnAddLocation = new Button { Text = "Добавить", Left = 8, Width = 100, Top = 4 };
            btnEditLocation = new Button { Text = "Редактировать", Left = 120, Width = 120, Top = 4 };
            btnDeleteLocation = new Button { Text = "Удалить", Left = 250, Width = 100, Top = 4 };

            btnAddLocation.Click += (s, e) => OpenEditLocationForm(null);
            btnEditLocation.Click += (s, e) =>
            {
                var location = GetSelectedLocation();
                if (location != null) OpenEditLocationForm(location);
            };
            btnDeleteLocation.Click += (s, e) => DeleteSelectedLocation();

            gridLocations.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var location = GetSelectedLocation();
                    if (location != null) OpenEditLocationForm(location);
                }
            };

            panelLocationButtons.Controls.Add(btnAddLocation);
            panelLocationButtons.Controls.Add(btnEditLocation);
            panelLocationButtons.Controls.Add(btnDeleteLocation);

            tabLocations.Controls.Add(gridLocations);
            tabLocations.Controls.Add(panelLocationButtons);

            tabControl.TabPages.Add(tabLocations);

            RefreshLocationGrid();





            // StatusStrip
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel { Text = "Готов" };
            statusStrip.Items.Add(statusLabel);

            // Панель кнопок
            var buttonPanel = new Panel { Dock = DockStyle.Top, Height = 40 };
            var btnAddItem = new Button { Text = "Добавить предмет", Left = 10, Top = 8, Width = 120 };
            var btnAddMonster = new Button { Text = "Добавить монстра", Left = 140, Top = 8, Width = 120 };
            btnAddLocation = new Button { Text = "Добавить локацию", Left = 270, Top = 8, Width = 120 };
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
                var item = GetSelectedItem();
                if (item != null) EditItem(item);
            }
        }

        private void EditSelectedMonster()
        {
            if (gridMonsters.SelectedRows.Count > 0)
            {
                var selectedId = (int)gridMonsters.SelectedRows[0].Cells["ID"].Value;
                var monster = GetSelectedMonster();
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
                var quest = GetSelectedQuest();
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

        private void RefreshDialoguesGrid()
        {
            if (_gameData == null) return;

            // Отображаем ограниченное представление: Id + Name + NodesCount
            var list = _gameData.Dialogues
                .Select(d => new
                {
                    Id = d.Id,
                    Name = d.Name,
                    Nodes = d.Nodes?.Count ?? 0
                })
                .OrderBy(x => x.Name)
                .ToList();

            gridDialogues.DataSource = null;
            gridDialogues.DataSource = list;

            // Корректные заголовки
            if (gridDialogues.Columns["Id"] != null) gridDialogues.Columns["Id"].HeaderText = "Id";
            if (gridDialogues.Columns["Name"] != null) gridDialogues.Columns["Name"].HeaderText = "Название";
            if (gridDialogues.Columns["Nodes"] != null) gridDialogues.Columns["Nodes"].HeaderText = "Узлов";
        }

        private void AddDialogue()
        {
            var newDialogue = new DialogueData
            {
                Id = Guid.NewGuid().ToString(),
                Name = "Новый диалог",
                Nodes = new List<DialogueNodeData>()
            };

            // Заменяем вызов старой формы на новую
            using (var form = new VisualDialogueForm(newDialogue))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Dialogues.Add(newDialogue);
                    RefreshDialoguesGrid();
                    statusLabel.Text = "Изменения не сохранены";
                }
            }
        }
        private void EditSelectedDialogue()
        {
            if (gridDialogues.CurrentRow == null) return;
            var id = gridDialogues.CurrentRow.Cells["Id"].Value as string;
            if (string.IsNullOrEmpty(id)) return;

            var dialog = _gameData.Dialogues.FirstOrDefault(d => d.Id == id);
            if (dialog == null) return;

            // Заменяем вызов старой формы на новую
            using (var form = new VisualDialogueForm(dialog))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDialoguesGrid();
                    statusLabel.Text = "Изменения не сохранены";
                }
            }
        }
        private void DeleteSelectedDialogue()
        {
            if (gridDialogues.CurrentRow == null) return;
            var id = gridDialogues.CurrentRow.Cells["Id"].Value as string;
            if (string.IsNullOrEmpty(id)) return;
            var dialog = _gameData.Dialogues.FirstOrDefault(d => d.Id == id);
            if (dialog == null) return;

            var confirm = MessageBox.Show(this, $"Удалить диалог '{dialog.Name}' (Id: {dialog.Id})?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (confirm == DialogResult.Yes)
            {
                _gameData.Dialogues.Remove(dialog);
                RefreshDialoguesGrid();
                statusLabel.Text = "Изменения не сохранены";
            }
        }

        private void RefreshNPCGrid()
        {
            if (_gameData == null) return;

            var list = _gameData.NPCs
                .Select(n => new
                {
                    ID = n.ID,
                    Name = n.Name,
                    Greeting = n.Greeting,
                    GreetingDialogue = n.GreetingDialogueId ?? ""
                })
                .OrderBy(n => n.ID)
                .ToList();

            gridNPCs.DataSource = null;
            gridNPCs.DataSource = list;

            if (gridNPCs.Columns["ID"] != null) gridNPCs.Columns["ID"].HeaderText = "ID";
            if (gridNPCs.Columns["Name"] != null) gridNPCs.Columns["Name"].HeaderText = "Имя";
            if (gridNPCs.Columns["Greeting"] != null) gridNPCs.Columns["Greeting"].HeaderText = "Greeting";
            if (gridNPCs.Columns["GreetingDialogue"] != null) gridNPCs.Columns["GreetingDialogue"].HeaderText = "GreetingDialogueId";
        }

        private NPCData? GetSelectedNPCData()
        {
            if (gridNPCs.CurrentRow == null) return null;
            var idObj = gridNPCs.CurrentRow.Cells["ID"].Value;
            if (idObj == null) return null;
            int id = Convert.ToInt32(idObj);
            return _gameData?.NPCs?.FirstOrDefault(n => n.ID == id);
        }

        private void OpenEditNPCForm(NPCData npc)
        {
            // Передаём существующий объект (если редактируем) или null (для создания)
            NPCData editing = npc;
            var dialogNpc = editing ?? new NPCData();

            using (var form = new EditNPCForm(_gameData, dialogNpc))
            {
                var res = form.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    var updated = form.GetNPCData();
                    // Если мы редактировали существующий объект и передали ссылку — список уже изменён.
                    if (editing == null)
                    {
                        // Добавляем новый NPC в gameData (убираем возможный дубль ID)
                        // Убедимся, что ID уникален — если совпадает с существующим, найдём свободный
                        if (_gameData.NPCs.Any(n => n.ID == updated.ID))
                        {
                            // Найдём max id + 1
                            var next = (_gameData.NPCs.Count > 0) ? _gameData.NPCs.Max(n => n.ID) + 1 : 1;
                            updated.ID = next;
                        }
                        _gameData.NPCs.Add(updated);
                    }
                    // Обновляем таблицу
                    RefreshNPCGrid();
                    statusLabel.Text = "Изменения не сохранены";
                }
            }
        }

        private void DeleteSelectedNPC()
        {
            var sel = GetSelectedNPCData();
            if (sel == null) return;
            var ans = MessageBox.Show(this, $"Удалить NPC '{sel.Name}' (ID: {sel.ID})?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ans == DialogResult.Yes)
            {
                _gameData.NPCs.Remove(sel);
                RefreshNPCGrid();
                statusLabel.Text = "Изменения не сохранены";
            }
        }

        private void RefreshLocationGrid()
        {
            if (_gameData == null) return;

            var list = _gameData.Locations
                .Select(l => new
                {
                    l.ID,
                    l.Name,
                    NPCs = string.Join(", ", l.NPCsHere ?? new List<int>()),
                    Monsters = string.Join(", ", l.MonsterTemplates ?? new List<int>()),
                    North = l.LocationToNorth,
                    East = l.LocationToEast,
                    South = l.LocationToSouth,
                    West = l.LocationToWest
                })
                .OrderBy(l => l.ID)
                .ToList();

            gridLocations.DataSource = null;
            gridLocations.DataSource = list;
        }

        private LocationData? GetSelectedLocation()
        {
            if (gridLocations.CurrentRow == null) return null;
            var idObj = gridLocations.CurrentRow.Cells["ID"].Value;
            if (idObj == null) return null;
            int id = Convert.ToInt32(idObj);
            return _gameData?.Locations?.FirstOrDefault(l => l.ID == id);
        }

        private void OpenEditLocationForm(LocationData loc)
        {
            LocationData editing = loc;
            var dialogLoc = editing ?? new LocationData();

            using (var form = new EditLocationForm(_gameData, dialogLoc))
            {
                var res = form.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    var updated = form.GetLocation();
                    if (editing == null)
                    {
                        if (_gameData.Locations.Any(l => l.ID == updated.ID))
                        {
                            MessageBox.Show("Локация с таким ID уже существует.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
                        _gameData.Locations.Add(updated);
                    }
                    RefreshLocationGrid();
                    statusLabel.Text = "Изменения не сохранены";
                }
            }
        }

        private void DeleteSelectedLocation()
        {
            var sel = GetSelectedLocation();
            if (sel == null) return;
            var ans = MessageBox.Show(this, $"Удалить локацию '{sel.Name}'?", "Подтверждение", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
            if (ans == DialogResult.Yes)
            {
                _gameData.Locations.Remove(sel);
                RefreshLocationGrid();
                statusLabel.Text = "Изменения не сохранены";
            }
        }

        private ItemData? GetSelectedItem()
        {
            if (gridItems.CurrentRow == null) return null;
            var idObj = gridItems.CurrentRow.Cells["ID"].Value;
            if (idObj == null) return null;
            int id = Convert.ToInt32(idObj);
            return _gameData?.Items?.FirstOrDefault(i => i.ID == id);
        }

        private MonsterData? GetSelectedMonster()
        {
            if (gridMonsters.CurrentRow == null) return null;
            var idObj = gridMonsters.CurrentRow.Cells["ID"].Value;
            if (idObj == null) return null;
            int id = Convert.ToInt32(idObj);
            return _gameData?.Monsters?.FirstOrDefault(m => m.ID == id);
        }

        private QuestData? GetSelectedQuest()
        {
            if (gridQuests.CurrentRow == null) return null;
            var idObj = gridQuests.CurrentRow.Cells["ID"].Value;
            if (idObj == null) return null;
            int id = Convert.ToInt32(idObj);
            return _gameData?.Quests?.FirstOrDefault(q => q.ID == id);
        }

        

    }
}