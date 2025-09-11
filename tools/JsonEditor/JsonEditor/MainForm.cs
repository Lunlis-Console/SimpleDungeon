using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class MainForm : Form
    {
        private GameData _gameData;
        private string _currentFilePath;

        private MenuStrip menuStrip;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusLabel;

        private TabControl tabControl;
        private DataGridView gridItems, gridMonsters, gridQuests, gridDialogues, gridNPCs, gridLocations;

        public MainForm()
        {
            Text = "JsonEditor";
            Width = 1200;
            Height = 800;
            StartPosition = FormStartPosition.CenterScreen;

            InitializeUi();
        }

        private void InitializeUi()
        {
            // === Menu ===
            menuStrip = new MenuStrip();
            var fileMenu = new ToolStripMenuItem("Файл");
            var openItem = new ToolStripMenuItem("Открыть", null, OpenFile);
            var saveItem = new ToolStripMenuItem("Сохранить", null, SaveFile);
            var saveAsItem = new ToolStripMenuItem("Сохранить как...", null, SaveFileAs);
            var exitItem = new ToolStripMenuItem("Выход", null, (s, e) => Close());
            fileMenu.DropDownItems.AddRange(new ToolStripItem[] { openItem, saveItem, saveAsItem, new ToolStripSeparator(), exitItem });
            menuStrip.Items.Add(fileMenu);

            // === Status ===
            statusStrip = new StatusStrip();
            statusLabel = new ToolStripStatusLabel("Готово");
            statusStrip.Items.Add(statusLabel);

            // === Tabs ===
            tabControl = new TabControl
            {
                Dock = DockStyle.Fill,
                Appearance = TabAppearance.Normal,
                ItemSize = new System.Drawing.Size(100, 25),
                SizeMode = TabSizeMode.Normal
            };

            tabControl.TabPages.Add("Предметы");
            tabControl.TabPages.Add("Монстры");
            tabControl.TabPages.Add("Квесты");
            tabControl.TabPages.Add("Диалоги");
            tabControl.TabPages.Add("NPC");
            tabControl.TabPages.Add("Локации");

            gridItems = NewGrid();
            gridMonsters = NewGrid();
            gridQuests = NewGrid();
            gridDialogues = NewGrid();
            gridNPCs = NewGrid();
            gridLocations = NewGrid();

            tabControl.TabPages[0].Controls.Add(gridItems);
            tabControl.TabPages[1].Controls.Add(gridMonsters);
            tabControl.TabPages[2].Controls.Add(gridQuests);
            tabControl.TabPages[3].Controls.Add(gridDialogues);
            tabControl.TabPages[4].Controls.Add(gridNPCs);
            tabControl.TabPages[5].Controls.Add(gridLocations);

            // === Правильный порядок добавления контролов ===
            // Сначала добавляем контролы с DockStyle.None или те, что должны быть "под" другими
            Controls.Add(tabControl);

            // Затем добавляем контролы с докингом (они будут поверх)
            Controls.Add(menuStrip);
            Controls.Add(statusStrip);

            // Устанавливаем докинг после добавления
            menuStrip.Dock = DockStyle.Top;
            statusStrip.Dock = DockStyle.Bottom;

            // Устанавливаем отступы для tabControl, чтобы не перекрывался меню и статусной строкой
            tabControl.Location = new System.Drawing.Point(0, menuStrip.Height);
            tabControl.Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height - menuStrip.Height - statusStrip.Height);

            _gameData = new GameData();
        }

        protected override void OnResize(EventArgs e)
        {
            base.OnResize(e);
            // Обновляем размер и положение tabControl при изменении размера формы
            if (tabControl != null && menuStrip != null && statusStrip != null)
            {
                tabControl.Location = new System.Drawing.Point(0, menuStrip.Height);
                tabControl.Size = new System.Drawing.Size(ClientSize.Width, ClientSize.Height - menuStrip.Height - statusStrip.Height);
            }
        }

        private DataGridView NewGrid() => new DataGridView
        {
            Dock = DockStyle.Fill,
            ReadOnly = true,
            AllowUserToAddRows = false,
            AllowUserToDeleteRows = false,
            AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill,
            SelectionMode = DataGridViewSelectionMode.FullRowSelect,
            MultiSelect = false
        };

        private void OpenFile(object sender, EventArgs e)
        {
            using var ofd = new OpenFileDialog { Filter = "JSON (*.json)|*.json" };
            if (ofd.ShowDialog(this) != DialogResult.OK) return;

            _currentFilePath = ofd.FileName;
            _gameData = SerializerHelper.LoadGameData(_currentFilePath);
            RefreshGrids();
            Text = $"JsonEditor - {Path.GetFileName(_currentFilePath)}";
        }

        private void SaveFile(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(_currentFilePath))
            {
                SaveFileAs(sender, e);
                return;
            }
            SerializerHelper.SaveGameData(_gameData, _currentFilePath);
            statusLabel.Text = "Сохранено";
        }

        private void SaveFileAs(object sender, EventArgs e)
        {
            using var sfd = new SaveFileDialog { Filter = "JSON (*.json)|*.json" };
            if (sfd.ShowDialog(this) != DialogResult.OK) return;

            _currentFilePath = sfd.FileName;
            SerializerHelper.SaveGameData(_gameData, _currentFilePath);
            statusLabel.Text = "Сохранено как";
        }

        private void RefreshGrids()
        {
            gridItems.DataSource = _gameData.Items?.Select(i => new { i.ID, i.Name, i.Description, i.Price }).ToList();
            gridMonsters.DataSource = _gameData.Monsters?.Select(m => new { m.ID, m.Name, m.Level, m.CurrentHP, m.MaximumHP }).ToList();
            gridQuests.DataSource = _gameData.Quests?.Select(q => new { q.ID, q.Name, q.Description, q.RewardGold, q.RewardEXP }).ToList();
            gridDialogues.DataSource = _gameData.Dialogues?.Select(d => new { d.Id, d.Name, Nodes = d.Nodes?.Count ?? 0 }).ToList();
            gridNPCs.DataSource = _gameData.NPCs?.Select(n => new { n.ID, n.Name, n.Greeting }).ToList();
            gridLocations.DataSource = _gameData.Locations?.Select(l => new { l.ID, l.Name, NPCs = l.NPCsHere?.Count ?? 0 }).ToList();
        }
    }
}