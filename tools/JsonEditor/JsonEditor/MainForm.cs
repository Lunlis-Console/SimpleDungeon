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
        private ToolStripMenuItem addDialogueMenuItem;
        private ToolStripMenuItem addNPCMenuItem;

        private TabControl tabControl;
        private TabPage tabItems;
        private TabPage tabMonsters;
        private TabPage tabLocations;
        private TabPage tabQuests;
        private TabPage tabDialogues;
        private TabPage tabNPCs;

        private DataGridView gridNPCs;
        private DataGridView gridItems;
        private DataGridView gridMonsters;
        private DataGridView gridLocations;
        private DataGridView gridQuests;
        private DataGridView gridDialogues;

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
            addDialogueMenuItem = new ToolStripMenuItem("Добавить диалог");
            addNPCMenuItem = new ToolStripMenuItem("Добавить NPC");

            addItemMenuItem.Click += (s, e) => AddNewItem();
            addMonsterMenuItem.Click += (s, e) => AddNewMonster();
            addLocationMenuItem.Click += (s, e) => AddNewLocation();
            addQuestMenuItem.Click += (s, e) => AddNewQuest();
            addDialogueMenuItem.Click += (s, e) => AddDialogue();
            addNPCMenuItem.Click += (s, e) => OpenEditNPCForm(null);

            editMenu.DropDownItems.Add(addItemMenuItem);
            editMenu.DropDownItems.Add(addMonsterMenuItem);
            editMenu.DropDownItems.Add(addLocationMenuItem);
            editMenu.DropDownItems.Add(addQuestMenuItem);
            editMenu.DropDownItems.Add(addDialogueMenuItem);
            editMenu.DropDownItems.Add(addNPCMenuItem);

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

            // Вкладка диалогов
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
            gridDialogues.CellDoubleClick += (s, e) => { if (e.RowIndex >= 0) EditSelectedDialogue(); };
            tabDialogues.Controls.Add(gridDialogues);

            // Вкладка NPC
            tabNPCs = new TabPage("NPC");
            gridNPCs = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            gridNPCs.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var npc = GetSelectedNPCData();
                    if (npc != null) OpenEditNPCForm(npc);
                }
            };
            tabNPCs.Controls.Add(gridNPCs);

            // Вкладка локаций
            tabLocations = new TabPage("Локации");
            gridLocations = new DataGridView
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                MultiSelect = false
            };
            gridLocations.CellDoubleClick += (s, e) =>
            {
                if (e.RowIndex >= 0)
                {
                    var location = GetSelectedLocation();
                    if (location != null) OpenEditLocationForm(location);
                }
            };
            tabLocations.Controls.Add(gridLocations);

            tabControl.TabPages.AddRange(new[] { tabItems, tabMonsters, tabQuests, tabDialogues, tabNPCs, tabLocations });

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
            var btnAddDialogue = new Button { Text = "Добавить диалог", Left = 530, Top = 8, Width = 120 };
            var btnAddNPC = new Button { Text = "Добавить NPC", Left = 660, Top = 8, Width = 120 };

            btnAddItem.Click += (s, e) => AddNewItem();
            btnAddMonster.Click += (s, e) => AddNewMonster();
            btnAddLocation.Click += (s, e) => AddNewLocation();
            btnAddQuest.Click += (s, e) => AddNewQuest();
            btnAddDialogue.Click += (s, e) => AddDialogue();
            btnAddNPC.Click += (s, e) => OpenEditNPCForm(null);

            buttonPanel.Controls.AddRange(new Control[] { btnAddItem, btnAddMonster, btnAddLocation, btnAddQuest, btnAddDialogue, btnAddNPC });

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
            gridDialogues.ContextMenuStrip = contextMenu;
            gridNPCs.ContextMenuStrip = contextMenu;

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
                                          $"{_gameData.Monsters?.Count ?? 0} монстров, " +
                                          $"{_gameData.Dialogues?.Count ?? 0} диалогов, " +
                                          $"{_gameData.NPCs?.Count ?? 0} NPC";

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
                RewardEXP = q.RewardEXP,
                QuestItems = q.QuestItems?.Count ?? 0
            }).ToList();

            // Диалоги
            RefreshDialoguesGrid();

            // NPC
            RefreshNPCGrid();

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
                        else if (currentGrid == gridDialogues)
                        {
                            var id = gridDialogues.SelectedRows[0].Cells["Id"].Value as string;
                            _gameData.Dialogues?.RemoveAll(d => d.Id == id);
                        }
                        else if (currentGrid == gridNPCs)
                        {
                            var id = (int)gridNPCs.SelectedRows[0].Cells["ID"].Value;
                            _gameData.NPCs?.RemoveAll(n => n.ID == id);
                        }

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

        private void EditQuest(QuestData quest)
        {
            using (var form = new EditQuestForm(_gameData, quest))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDataGrids();
                    statusLabel.Text = "Квест обновлен";
                }
            }
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
            var newLocation = new LocationData
            {
                ID = GetNextAvailableId(_gameData.Locations),
                Name = "Новая локация"
            };

            using (var form = new EditLocationForm(_gameData, newLocation))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Locations ??= new List<LocationData>();
                    _gameData.Locations.Add(form.GetLocation());
                    RefreshDataGrids();
                    statusLabel.Text = "Новая локация добавлена";
                    tabControl.SelectedTab = tabLocations;
                }
            }
        }

        private void AddNewQuest()
        {
            var newQuest = new QuestData
            {
                ID = GetNextAvailableId(_gameData.Quests),
                Name = "Новый квест",
                Description = "",
                RewardGold = 0,
                RewardEXP = 0,
                QuestItems = new List<QuestItemData>()
            };

            using (var form = new EditQuestForm(_gameData, newQuest))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Quests ??= new List<QuestData>();
                    _gameData.Quests.Add(form.GetQuest());
                    RefreshDataGrids();
                    statusLabel.Text = "Новый квест добавлен";
                    tabControl.SelectedTab = tabQuests;
                }
            }
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

            // ЗАМЕНА: используем EditDialogueForm вместо VisualDialogueForm
            using (var form = new EditDialogueForm(newDialogue))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _gameData.Dialogues.Add(newDialogue);
                    RefreshDataGrids();
                    statusLabel.Text = "Новый диалог добавлен";
                    tabControl.SelectedTab = tabDialogues;
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

            // ЗАМЕНА: используем EditDialogueForm вместо VisualDialogueForm
            using (var form = new EditDialogueForm(dialog))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    RefreshDataGrids();
                    statusLabel.Text = "Диалог обновлён";
                }
            }
        }

        // Метод для глубокого копирования диалога

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
            if (gridNPCs.Columns["Greeting"] != null) gridNPCs.Columns["Greeting"].HeaderText = "Приветствие";
            if (gridNPCs.Columns["GreetingDialogue"] != null) gridNPCs.Columns["GreetingDialogue"].HeaderText = "ID диалога";
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
            NPCData editing = npc;
            var dialogNpc = editing ?? new NPCData();

            using (var form = new EditNPCForm(_gameData, dialogNpc))
            {
                var res = form.ShowDialog(this);
                if (res == DialogResult.OK)
                {
                    var updated = form.GetNPCData();
                    if (editing == null)
                    {
                        if (_gameData.NPCs.Any(n => n.ID == updated.ID))
                        {
                            var next = (_gameData.NPCs.Count > 0) ? _gameData.NPCs.Max(n => n.ID) + 1 : 1;
                            updated.ID = next;
                        }
                        _gameData.NPCs.Add(updated);
                        statusLabel.Text = "Новый NPC добавлен";
                        tabControl.SelectedTab = tabNPCs;
                    }
                    else
                    {
                        statusLabel.Text = "NPC обновлён";
                    }
                    RefreshDataGrids();
                }
            }
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
                        statusLabel.Text = "Новая локация добавлена";
                        tabControl.SelectedTab = tabLocations;
                    }
                    else
                    {
                        statusLabel.Text = "Локация обновлена";
                    }
                    RefreshDataGrids();
                }
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

        private QuestData GetSelectedQuest()
        {
            if (gridQuests.CurrentRow == null) return null;
            var idObj = gridQuests.CurrentRow.Cells["ID"].Value;
            if (idObj == null) return null;
            int id = Convert.ToInt32(idObj);
            return _gameData?.Quests?.FirstOrDefault(q => q.ID == id);
        }
    }
}
