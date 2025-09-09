using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class MainForm : Form
    {
        private GameData _gameData;

        private MenuStrip menuStrip;
        private ToolStripMenuItem fileMenu;
        private ToolStripMenuItem openMenuItem;
        private ToolStripMenuItem saveMenuItem;

        private TreeView treeView;

        public MainForm()
        {
            Text = "JsonEditor";
            Width = 800;
            Height = 600;
            StartPosition = FormStartPosition.CenterScreen;

            InitializeUi();
        }

        private void InitializeUi()
        {
            // Меню
            menuStrip = new MenuStrip();

            fileMenu = new ToolStripMenuItem("Файл");
            openMenuItem = new ToolStripMenuItem("Открыть");
            saveMenuItem = new ToolStripMenuItem("Сохранить");

            openMenuItem.Click += openToolStripMenuItem_Click;
            saveMenuItem.Click += saveToolStripMenuItem_Click;

            fileMenu.DropDownItems.Add(openMenuItem);
            fileMenu.DropDownItems.Add(saveMenuItem);

            menuStrip.Items.Add(fileMenu);

            Controls.Add(menuStrip);

            // TreeView
            treeView = new TreeView
            {
                Dock = DockStyle.Fill
            };
            Controls.Add(treeView);

            MainMenuStrip = menuStrip;
        }

        private void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            using (var ofd = new OpenFileDialog())
            {
                ofd.Filter = "JSON files (*.json)|*.json|All files (*.*)|*.*";
                if (ofd.ShowDialog(this) == DialogResult.OK)
                {
                    try
                    {
                        _gameData = SerializerHelper.LoadGameData(ofd.FileName);
                        RefreshTreeView();
                        Text = $"JsonEditor - {Path.GetFileName(ofd.FileName)}";
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка загрузки файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void saveToolStripMenuItem_Click(object sender, EventArgs e)
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
                        MessageBox.Show("Файл сохранён успешно.", "Успех",
                            MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Ошибка сохранения файла: {ex.Message}", "Ошибка",
                            MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
        }

        private void RefreshTreeView()
        {
            treeView.Nodes.Clear();

            if (_gameData == null) return;

            // Items
            var itemsNode = new TreeNode("Items");
            foreach (var item in _gameData.Items ?? new List<ItemData>())
            {
                itemsNode.Nodes.Add(new TreeNode($"{item.ID}: {item.Name} [{item.Type}]"));
            }
            treeView.Nodes.Add(itemsNode);

            // Monsters
            var monstersNode = new TreeNode("Monsters");
            foreach (var monster in _gameData.Monsters ?? new List<MonsterData>())
            {
                monstersNode.Nodes.Add(new TreeNode($"{monster.ID}: {monster.Name} (Lvl {monster.Level})"));
            }
            treeView.Nodes.Add(monstersNode);

            // Locations
            var locNode = new TreeNode("Locations");
            foreach (var loc in _gameData.Locations ?? new List<LocationData>())
            {
                locNode.Nodes.Add(new TreeNode($"{loc.ID}: {loc.Name}"));
            }
            treeView.Nodes.Add(locNode);

            // Quests
            var questNode = new TreeNode("Quests");
            foreach (var q in _gameData.Quests ?? new List<QuestData>())
            {
                questNode.Nodes.Add(new TreeNode($"{q.ID}: {q.Name}"));
            }
            treeView.Nodes.Add(questNode);

            treeView.ExpandAll();
        }
    }
}
