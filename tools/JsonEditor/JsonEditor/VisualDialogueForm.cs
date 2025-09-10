//using System;
//using System.Collections.Generic;
//using System.Drawing;
//using System.Linq;
//using System.Windows.Forms;
//using Engine.Data;

//namespace JsonEditor
//{
//    public class VisualDialogueForm : Form
//    {
//        private DialogueData _dialogue;
//        private TreeView _treeView;
//        private PropertyGrid _propertyGrid;
//        private ToolStrip _toolStrip;
//        private Button _btnSave;
//        private Button _btnCancel;
//        private ContextMenuStrip _contextMenu;

//        public VisualDialogueForm(DialogueData dialogue)
//        {
//            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
//            InitializeComponent();
//            LoadDialogue();
//        }

//        private void InitializeComponent()
//        {
//            Text = "Визуальный редактор диалога: " + _dialogue.Name;
//            Size = new Size(1000, 600);
//            StartPosition = FormStartPosition.CenterParent;
//            MinimumSize = new Size(800, 500);

//            // Контекстное меню
//            _contextMenu = new ContextMenuStrip();
//            _contextMenu.Items.Add("Добавить узел", null, (s, e) => AddNode());
//            _contextMenu.Items.Add("Добавить ответ", null, (s, e) => AddResponse());
//            _contextMenu.Items.Add("Удалить", null, (s, e) => DeleteSelected());
//            _contextMenu.Items.Add(new ToolStripSeparator());
//            _contextMenu.Items.Add("Переместить вверх", null, (s, e) => MoveUp());
//            _contextMenu.Items.Add("Переместить вниз", null, (s, e) => MoveDown());

//            // SplitContainer для разделения дерева и свойств
//            var splitContainer = new SplitContainer
//            {
//                Dock = DockStyle.Fill,
//                SplitterDistance = 300
//            };

//            // TreeView для структуры диалога
//            _treeView = new TreeView
//            {
//                Dock = DockStyle.Fill,
//                ShowNodeToolTips = true,
//                ImageList = CreateImageList(),
//                ContextMenuStrip = _contextMenu
//            };
//            _treeView.AfterSelect += TreeView_AfterSelect;
//            _treeView.NodeMouseClick += TreeView_NodeMouseClick;

//            // PropertyGrid для редактирования свойств
//            _propertyGrid = new PropertyGrid
//            {
//                Dock = DockStyle.Fill,
//                HelpVisible = false,
//                ToolbarVisible = false
//            };
//            _propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;

//            // ToolStrip с кнопками
//            _toolStrip = new ToolStrip { Dock = DockStyle.Top };
//            _toolStrip.Items.AddRange(new ToolStripItem[]
//            {
//                new ToolStripButton("Добавить узел", null, (s, e) => AddNode()),
//                new ToolStripButton("Добавить ответ", null, (s, e) => AddResponse()),
//                new ToolStripButton("Удалить", null, (s, e) => DeleteSelected()),
//                new ToolStripSeparator(),
//                new ToolStripButton("Вверх", null, (s, e) => MoveUp()),
//                new ToolStripButton("Вниз", null, (s, e) => MoveDown())
//            });

//            // Кнопки сохранения/отмены
//            _btnSave = new Button
//            {
//                Text = "Сохранить",
//                DialogResult = DialogResult.OK,
//                Dock = DockStyle.Bottom,
//                Height = 35
//            };
//            _btnSave.Click += (s, e) => SaveChanges();

//            _btnCancel = new Button
//            {
//                Text = "Отмена",
//                DialogResult = DialogResult.Cancel,
//                Dock = DockStyle.Bottom,
//                Height = 35
//            };

//            // Размещение элементов
//            splitContainer.Panel1.Controls.Add(_treeView);
//            splitContainer.Panel2.Controls.Add(_propertyGrid);

//            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 35 };
//            buttonPanel.Controls.Add(_btnSave);
//            buttonPanel.Controls.Add(_btnCancel);

//            Controls.Add(splitContainer);
//            Controls.Add(_toolStrip);
//            Controls.Add(buttonPanel);
//        }

//        private ImageList CreateImageList()
//        {
//            var imageList = new ImageList();
//            imageList.Images.Add("Node", SystemIcons.Information);
//            imageList.Images.Add("Response", SystemIcons.Exclamation);
//            return imageList;
//        }

//        private void LoadDialogue()
//        {
//            _treeView.BeginUpdate();
//            _treeView.Nodes.Clear();

//            // Добавляем корневой узел диалога
//            var dialogueNode = new TreeNode(_dialogue.Name)
//            {
//                Tag = _dialogue,
//                ImageKey = "Node",
//                SelectedImageKey = "Node"
//            };

//            // Добавляем все узлы диалога
//            if (_dialogue.Nodes != null)
//            {
//                foreach (var node in _dialogue.Nodes)
//                {
//                    var nodeTreeNode = CreateNodeTreeNode(node);
//                    dialogueNode.Nodes.Add(nodeTreeNode);
//                }
//            }

//            _treeView.Nodes.Add(dialogueNode);
//            dialogueNode.Expand();
//            _treeView.EndUpdate();
//        }

//        private TreeNode CreateNodeTreeNode(DialogueNodeData node)
//        {
//            var nodeText = string.IsNullOrEmpty(node.Text) ?
//                "Пустой узел" :
//                $"Узел: {TruncateText(node.Text, 30)}";

//            var treeNode = new TreeNode(nodeText)
//            {
//                Tag = node,
//                ImageKey = "Node",
//                SelectedImageKey = "Node",
//                ToolTipText = node.Text
//            };

//            // Добавляем ответы к узлу
//            if (node.Responses != null)
//            {
//                foreach (var response in node.Responses)
//                {
//                    var responseTreeNode = CreateResponseTreeNode(response);
//                    treeNode.Nodes.Add(responseTreeNode);
//                }
//            }

//            return treeNode;
//        }

//        private TreeNode CreateResponseTreeNode(DialogueResponseData response)
//        {
//            var responseText = string.IsNullOrEmpty(response.Text) ?
//                "Пустой ответ" :
//                $"Ответ: {TruncateText(response.Text, 30)}";

//            return new TreeNode(responseText)
//            {
//                Tag = response,
//                ImageKey = "Response",
//                SelectedImageKey = "Response",
//                ToolTipText = response.Text
//            };
//        }

//        private string TruncateText(string text, int maxLength)
//        {
//            if (string.IsNullOrEmpty(text)) return "";
//            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
//        }

//        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
//        {
//            _propertyGrid.SelectedObject = e.Node.Tag;
//            UpdateToolbarState();
//        }

//        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
//        {
//            _treeView.SelectedNode = e.Node;
//            if (e.Button == MouseButtons.Right)
//            {
//                _contextMenu.Show(_treeView, e.Location);
//            }
//        }

//        private void UpdateToolbarState()
//        {
//            bool canAddResponse = _treeView.SelectedNode?.Tag is DialogueNodeData;
//            bool canDelete = _treeView.SelectedNode != null && _treeView.SelectedNode != _treeView.Nodes[0];
//            bool canMoveUp = canDelete && _treeView.SelectedNode.Index > 0;
//            bool canMoveDown = canDelete && _treeView.SelectedNode.Index < _treeView.SelectedNode.Parent.Nodes.Count - 1;

//            // Обновляем состояние кнопок
//            foreach (ToolStripItem item in _toolStrip.Items)
//            {
//                if (item is ToolStripButton button)
//                {
//                    if (button.Text.Contains("ответ"))
//                        button.Enabled = canAddResponse;
//                    else if (button.Text.Contains("Удалить"))
//                        button.Enabled = canDelete;
//                    else if (button.Text.Contains("Вверх"))
//                        button.Enabled = canMoveUp;
//                    else if (button.Text.Contains("Вниз"))
//                        button.Enabled = canMoveDown;
//                }
//            }
//        }

//        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
//        {
//            // Обновляем текст узла при изменении свойств
//            if (_treeView.SelectedNode?.Tag is DialogueNodeData node)
//            {
//                _treeView.SelectedNode.Text = $"Узел: {TruncateText(node.Text, 30)}";
//                _treeView.SelectedNode.ToolTipText = node.Text;
//            }
//            else if (_treeView.SelectedNode?.Tag is DialogueResponseData response)
//            {
//                _treeView.SelectedNode.Text = $"Ответ: {TruncateText(response.Text, 30)}";
//                _treeView.SelectedNode.ToolTipText = response.Text;
//            }
//        }

//        private void AddNode()
//        {
//            var newNode = new DialogueNodeData
//            {
//                Id = Guid.NewGuid().ToString(),
//                Text = "Новый узел диалога",
//                Responses = new List<DialogueResponseData>()
//            };

//            _dialogue.Nodes ??= new List<DialogueNodeData>();
//            _dialogue.Nodes.Add(newNode);

//            var treeNode = CreateNodeTreeNode(newNode);
//            _treeView.Nodes[0].Nodes.Add(treeNode);
//            _treeView.SelectedNode = treeNode;
//        }

//        private void AddResponse()
//        {
//            if (_treeView.SelectedNode?.Tag is DialogueNodeData node)
//            {
//                var newResponse = new DialogueResponseData
//                {
//                    Text = "Новый ответ",
//                    TargetNodeId = null
//                };

//                node.Responses ??= new List<DialogueResponseData>();
//                node.Responses.Add(newResponse);

//                var treeNode = CreateResponseTreeNode(newResponse);
//                _treeView.SelectedNode.Nodes.Add(treeNode);
//                _treeView.SelectedNode = treeNode;
//            }
//        }

//        private void DeleteSelected()
//        {
//            if (_treeView.SelectedNode?.Tag is DialogueNodeData node)
//            {
//                _dialogue.Nodes.Remove(node);
//                _treeView.SelectedNode.Remove();
//            }
//            else if (_treeView.SelectedNode?.Tag is DialogueResponseData response)
//            {
//                var parentNode = _treeView.SelectedNode.Parent;
//                if (parentNode?.Tag is DialogueNodeData parentDialogueNode)
//                {
//                    parentDialogueNode.Responses.Remove(response);
//                    _treeView.SelectedNode.Remove();
//                }
//            }
//        }

//        private void MoveUp()
//        {
//            var selectedNode = _treeView.SelectedNode;
//            if (selectedNode != null && selectedNode.PrevNode != null)
//            {
//                MoveNode(selectedNode, -1);
//            }
//        }

//        private void MoveDown()
//        {
//            var selectedNode = _treeView.SelectedNode;
//            if (selectedNode != null && selectedNode.NextNode != null)
//            {
//                MoveNode(selectedNode, 1);
//            }
//        }

//        private void MoveNode(TreeNode node, int direction)
//        {
//            var parent = node.Parent;
//            var index = node.Index;
//            var newIndex = index + direction;

//            if (parent != null && newIndex >= 0 && newIndex < parent.Nodes.Count)
//            {
//                parent.Nodes.RemoveAt(index);
//                parent.Nodes.Insert(newIndex, node);
//                _treeView.SelectedNode = node;

//                // Также перемещаем данные в коллекции
//                if (parent.Tag is DialogueData dialogue && node.Tag is DialogueNodeData dialogueNode)
//                {
//                    dialogue.Nodes.RemoveAt(index);
//                    dialogue.Nodes.Insert(newIndex, dialogueNode);
//                }
//                else if (parent.Tag is DialogueNodeData parentNode && node.Tag is DialogueResponseData response)
//                {
//                    parentNode.Responses.RemoveAt(index);
//                    parentNode.Responses.Insert(newIndex, response);
//                }
//            }
//        }

//        private void SaveChanges()
//        {
//            DialogResult = DialogResult.OK;
//            Close();
//        }
//    }
//}