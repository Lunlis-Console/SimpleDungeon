using Engine.Data;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JsonEditor.Legacy
{
    public class EditDialogueForm : Form
    {
        private DialogueData _dialogue;
        private GameData _gameData;
        private TextBox txtId;
        private TextBox txtName;
        private TreeView treeNodes;
        private Button btnAddNode;
        private Button btnEditNode;
        private Button btnDeleteNode;
        private Button btnAddOption;
        private Button btnEditOption;
        private Button btnDeleteOption;
        private Button btnOk;
        private Button btnCancel;
        private ListBox lstOptions;
        private PropertyGrid propertyGrid;

        public EditDialogueForm(DialogueData dialogue, GameData gameData)
        {
            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            _gameData = gameData;
            InitializeComponents();
            LoadDataToControls();
        }

        private void InitializeComponents()
        {
            Text = "Редактирование диалога";
            Size = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;

            // Основные поля
            var lblId = new Label { Text = "ID:", Location = new Point(10, 15), Width = 30 };
            txtId = new TextBox { Location = new Point(45, 12), Width = 200 };

            var lblName = new Label { Text = "Название:", Location = new Point(255, 15), Width = 70 };
            txtName = new TextBox { Location = new Point(330, 12), Width = 300 };

            // Дерево узлов
            treeNodes = new TreeView
            {
                Location = new Point(10, 45),
                Size = new Size(400, 400),
                ShowNodeToolTips = true
            };

            // PropertyGrid для редактирования
            propertyGrid = new PropertyGrid
            {
                Location = new Point(420, 45),
                Size = new Size(550, 400),
                ToolbarVisible = false
            };

            // Кнопки управления узлами
            btnAddNode = new Button { Text = "Добавить узел", Location = new Point(10, 455), Width = 120 };
            btnEditNode = new Button { Text = "Редактировать", Location = new Point(140, 455), Width = 120 };
            btnDeleteNode = new Button { Text = "Удалить", Location = new Point(270, 455), Width = 120 };

            // Список опций
            var lblOptions = new Label { Text = "Опции узла:", Location = new Point(10, 490), Width = 120 };
            lstOptions = new ListBox
            {
                Location = new Point(10, 515),
                Size = new Size(400, 100)
            };

            // Кнопки управления опциями
            btnAddOption = new Button { Text = "Добавить опцию", Location = new Point(10, 625), Width = 120 };
            btnEditOption = new Button { Text = "Редактировать", Location = new Point(140, 625), Width = 120 };
            btnDeleteOption = new Button { Text = "Удалить", Location = new Point(270, 625), Width = 120 };

            // Кнопки OK/Отмена
            btnOk = new Button { Text = "OK", Location = new Point(800, 625), Width = 80 };
            btnCancel = new Button { Text = "Отмена", Location = new Point(890, 625), Width = 80 };

            // События
            btnAddNode.Click += (s, e) => AddNewNode(null);
            btnEditNode.Click += (s, e) => EditSelectedNode();
            btnDeleteNode.Click += (s, e) => DeleteSelectedNode();
            btnAddOption.Click += (s, e) => AddOption();
            btnEditOption.Click += (s, e) => EditSelectedOption();
            btnDeleteOption.Click += (s, e) => DeleteSelectedOption();
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            treeNodes.AfterSelect += (s, e) =>
            {
                if (e.Node?.Tag is DialogueNodeData node)
                {
                    propertyGrid.SelectedObject = node;
                    LoadOptionsForNode(node);
                }
            };

            lstOptions.SelectedIndexChanged += (s, e) =>
            {
                if (treeNodes.SelectedNode?.Tag is DialogueNodeData node &&
                    lstOptions.SelectedIndex >= 0 &&
                    node.Choices != null &&
                    lstOptions.SelectedIndex < node.Choices.Count)
                {
                    propertyGrid.SelectedObject = node.Choices[lstOptions.SelectedIndex];
                }
            };

            Controls.AddRange(new Control[] {
                lblId, txtId, lblName, txtName,
                treeNodes, propertyGrid,
                btnAddNode, btnEditNode, btnDeleteNode,
                lblOptions, lstOptions,
                btnAddOption, btnEditOption, btnDeleteOption,
                btnOk, btnCancel
            });
        }

        private void LoadDataToControls()
        {
            txtId.Text = _dialogue.Id;
            txtName.Text = _dialogue.Name;
            BuildTreeView();
        }

        private void BuildTreeView()
        {
            treeNodes.Nodes.Clear();
            lstOptions.Items.Clear();

            if (_dialogue.Nodes == null)
                _dialogue.Nodes = new List<DialogueNodeData>();

            foreach (var node in _dialogue.Nodes.Where(n => string.IsNullOrEmpty(n.ParentId)))
            {
                var treeNode = new TreeNode($"{node.Id}: {TruncateText(node.Text, 30)}") { Tag = node };
                treeNodes.Nodes.Add(treeNode);
                AddChildNodes(treeNode, node.Id);
            }

            treeNodes.ExpandAll();
        }

        private void AddChildNodes(TreeNode parentTreeNode, string parentId)
        {
            foreach (var childNode in _dialogue.Nodes.Where(n => n.ParentId == parentId))
            {
                var childTreeNode = new TreeNode($"{childNode.Id}: {TruncateText(childNode.Text, 30)}") { Tag = childNode };
                parentTreeNode.Nodes.Add(childTreeNode);
                AddChildNodes(childTreeNode, childNode.Id);
            }
        }

        private string TruncateText(string text, int maxLength)
        {
            return string.IsNullOrEmpty(text) ? "(без текста)" :
                text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private void AddNewNode(string parentId)
        {
            var newNode = new DialogueNodeData
            {
                Id = GenerateUniqueId(),
                ParentId = parentId,
                Text = "Новый узел диалога",
                Choices = new List<DialogueChoiceData>()
            };

            using (var form = new EditNodeForm(newNode, _dialogue.Nodes))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _dialogue.Nodes.Add(form.Node);
                    BuildTreeView();
                }
            }
        }

        private string GenerateUniqueId()
        {
            int counter = 1;
            while (_dialogue.Nodes.Any(n => n.Id == $"node_{counter}")) counter++;
            return $"node_{counter}";
        }

        private void EditSelectedNode()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode)
            {
                using (var form = new EditNodeForm(selectedNode, _dialogue.Nodes))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        BuildTreeView();
                        LoadOptionsForNode(selectedNode);
                    }
                }
            }
        }

        private void DeleteSelectedNode()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode &&
                MessageBox.Show("Удалить этот узел?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _dialogue.Nodes.RemoveAll(n => n.Id == selectedNode.Id);
                BuildTreeView();
            }
        }

        private void LoadOptionsForNode(DialogueNodeData node)
        {
            lstOptions.Items.Clear();
            if (node.Choices != null)
            {
                foreach (var response in node.Choices)
                {
                    string displayText = $"{response.NextNodeId}: {TruncateText(response.Text, 30)}";
                    if (response.EndDialogue) displayText += " [КОНЕЦ]";
                    if (response.StartTrade) displayText += " [ТОРГОВЛЯ]";
                    lstOptions.Items.Add(displayText);
                }
            }
        }

        private void AddOption()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode)
            {
                var newResponse = new DialogueChoiceData
                {
                    Text = "Новый ответ",
                    NextNodeId = null
                };

                if (selectedNode.Choices == null) selectedNode.Choices = new List<DialogueChoiceData>();
                selectedNode.Choices.Add(newResponse);
                LoadOptionsForNode(selectedNode);
                lstOptions.SelectedIndex = lstOptions.Items.Count - 1;
            }
        }

        private void EditSelectedOption()
        {
            // Редактирование через PropertyGrid
        }

        private void DeleteSelectedOption()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode &&
                lstOptions.SelectedIndex >= 0 &&
                selectedNode.Choices != null &&
                lstOptions.SelectedIndex < selectedNode.Choices.Count &&
                MessageBox.Show("Удалить этот ответ?", "Подтверждение", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                selectedNode.Choices.RemoveAt(lstOptions.SelectedIndex);
                LoadOptionsForNode(selectedNode);
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show("ID не может быть пустым");
                return;
            }

            _dialogue.Id = txtId.Text.Trim();
            _dialogue.Name = txtName.Text.Trim();
            DialogResult = DialogResult.OK;
            Close();
        }
    }

    public class EditNodeForm : Form
    {
        public DialogueNodeData Node { get; private set; }
        private TextBox txtId;
        private TextBox txtText;
        private ComboBox cmbParentId;

        public EditNodeForm(DialogueNodeData node, List<DialogueNodeData> allNodes)
        {
            Node = node;
            InitializeComponents(allNodes);
        }

        private void InitializeComponents(List<DialogueNodeData> allNodes)
        {
            Text = "Редактирование узла";
            Size = new Size(400, 200);
            StartPosition = FormStartPosition.CenterParent;

            var lblId = new Label { Text = "ID:", Left = 10, Top = 15, Width = 80 };
            txtId = new TextBox { Left = 100, Top = 12, Width = 280, Text = Node.Id };

            var lblText = new Label { Text = "Текст:", Left = 10, Top = 45, Width = 80 };
            txtText = new TextBox { Left = 100, Top = 42, Width = 280, Text = Node.Text, Multiline = true, Height = 60 };

            var lblParent = new Label { Text = "Родитель:", Left = 10, Top = 115, Width = 80 };
            cmbParentId = new ComboBox { Left = 100, Top = 112, Width = 280 };
            cmbParentId.Items.Add("");
            cmbParentId.Items.AddRange(allNodes.Where(n => n.Id != Node.Id).Select(n => n.Id).ToArray());
            cmbParentId.Text = Node.ParentId;

            var btnOk = new Button { Text = "OK", Left = 220, Top = 140, Width = 80 };
            var btnCancel = new Button { Text = "Отмена", Left = 310, Top = 140, Width = 80 };

            btnOk.Click += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(txtId.Text))
                {
                    MessageBox.Show("ID не может быть пустым");
                    return;
                }

                Node.Id = txtId.Text;
                Node.Text = txtText.Text;
                Node.ParentId = cmbParentId.Text;
                DialogResult = DialogResult.OK;
                Close();
            };

            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] { lblId, txtId, lblText, txtText, lblParent, cmbParentId, btnOk, btnCancel });
        }
    }
}