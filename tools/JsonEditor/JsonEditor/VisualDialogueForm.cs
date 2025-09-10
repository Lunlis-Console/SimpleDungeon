using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class VisualDialogueForm : Form
    {
        private DialogueData _dialogue;
        private TreeView _treeView;
        private PropertyGrid _propertyGrid;
        private ToolStrip _toolStrip;
        private Button _btnSave;
        private Button _btnCancel;

        public VisualDialogueForm(DialogueData dialogue)
        {
            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            InitializeComponent();
            LoadDialogue();
        }

        private void InitializeComponent()
        {
            Text = "Визуальный редактор диалога: " + _dialogue.Name;
            Size = new Size(1000, 600);
            StartPosition = FormStartPosition.CenterParent;
            MinimumSize = new Size(800, 500);

            // SplitContainer для разделения дерева и свойств
            var splitContainer = new SplitContainer
            {
                Dock = DockStyle.Fill,
                SplitterDistance = 300
            };

            // TreeView для структуры диалога
            _treeView = new TreeView
            {
                Dock = DockStyle.Fill,
                ShowNodeToolTips = true,
                ImageList = CreateImageList()
            };
            _treeView.AfterSelect += TreeView_AfterSelect;
            _treeView.NodeMouseClick += TreeView_NodeMouseClick;

            // PropertyGrid для редактирования свойств
            _propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                HelpVisible = false,
                ToolbarVisible = false
            };
            _propertyGrid.PropertyValueChanged += PropertyGrid_PropertyValueChanged;

            // ToolStrip с кнопками
            _toolStrip = new ToolStrip { Dock = DockStyle.Top };
            _toolStrip.Items.AddRange(new ToolStripItem[]
            {
                new ToolStripButton("Добавить узел", null, (s, e) => AddNode()),
                new ToolStripButton("Добавить ответ", null, (s, e) => AddResponse()),
                new ToolStripButton("Удалить", null, (s, e) => DeleteSelected()),
                new ToolStripSeparator(),
                new ToolStripButton("Вверх", null, (s, e) => MoveUp()),
                new ToolStripButton("Вниз", null, (s, e) => MoveDown())
            });

            // Кнопки сохранения/отмены
            _btnSave = new Button
            {
                Text = "Сохранить",
                DialogResult = DialogResult.OK,
                Dock = DockStyle.Bottom,
                Height = 35
            };
            _btnSave.Click += (s, e) => SaveChanges();

            _btnCancel = new Button
            {
                Text = "Отмена",
                DialogResult = DialogResult.Cancel,
                Dock = DockStyle.Bottom,
                Height = 35
            };

            // Размещение элементов
            splitContainer.Panel1.Controls.Add(_treeView);
            splitContainer.Panel2.Controls.Add(_propertyGrid);

            var buttonPanel = new Panel { Dock = DockStyle.Bottom, Height = 35 };
            buttonPanel.Controls.Add(_btnSave);
            buttonPanel.Controls.Add(_btnCancel);

            Controls.Add(splitContainer);
            Controls.Add(_toolStrip);
            Controls.Add(buttonPanel);
        }

        private ImageList CreateImageList()
        {
            var imageList = new ImageList();
            imageList.Images.Add("Node", SystemIcons.Information);
            imageList.Images.Add("Response", SystemIcons.Exclamation);
            return imageList;
        }

        private void LoadDialogue()
        {
            _treeView.BeginUpdate();
            _treeView.Nodes.Clear();

            // Добавляем корневой узел диалога
            var dialogueNode = new TreeNode(_dialogue.Name)
            {
                Tag = _dialogue,
                ImageKey = "Node",
                SelectedImageKey = "Node"
            };

            // Добавляем все узлы диалога
            foreach (var node in _dialogue.Nodes)
            {
                var nodeTreeNode = new TreeNode($"Узел: {node.Text?.Substring(0, Math.Min(30, node.Text?.Length ?? 0))}...")
                {
                    Tag = node,
                    ImageKey = "Node",
                    SelectedImageKey = "Node"
                };

                // Добавляем ответы к узлу
                foreach (var response in node.Responses)
                {
                    var responseTreeNode = new TreeNode($"Ответ: {response.Text?.Substring(0, Math.Min(30, response.Text?.Length ?? 0))}...")
                    {
                        Tag = response,
                        ImageKey = "Response",
                        SelectedImageKey = "Response"
                    };
                    nodeTreeNode.Nodes.Add(responseTreeNode);
                }

                dialogueNode.Nodes.Add(nodeTreeNode);
            }

            _treeView.Nodes.Add(dialogueNode);
            dialogueNode.Expand();
            _treeView.EndUpdate();
        }

        private void TreeView_AfterSelect(object sender, TreeViewEventArgs e)
        {
            _propertyGrid.SelectedObject = e.Node.Tag;
        }

        private void TreeView_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            _treeView.SelectedNode = e.Node;
        }

        private void PropertyGrid_PropertyValueChanged(object s, PropertyValueChangedEventArgs e)
        {
            // Обновляем текст узла при изменении свойств
            if (_treeView.SelectedNode?.Tag is DialogueNodeData node)
            {
                _treeView.SelectedNode.Text = $"Узел: {node.Text?.Substring(0, Math.Min(30, node.Text?.Length ?? 0))}...";
            }
            else if (_treeView.SelectedNode?.Tag is DialogueResponseData response)
            {
                _treeView.SelectedNode.Text = $"Ответ: {response.Text?.Substring(0, Math.Min(30, response.Text?.Length ?? 0))}...";
            }
        }

        private void AddNode()
        {
            var newNode = new DialogueNodeData
            {
                Id = Guid.NewGuid().ToString(),
                Text = "Новый узел диалога",
                Responses = new List<DialogueResponseData>()
            };

            _dialogue.Nodes.Add(newNode);

            var treeNode = new TreeNode($"Узел: {newNode.Text}")
            {
                Tag = newNode,
                ImageKey = "Node",
                SelectedImageKey = "Node"
            };

            _treeView.Nodes[0].Nodes.Add(treeNode);
            _treeView.SelectedNode = treeNode;
        }

        private void AddResponse()
        {
            if (_treeView.SelectedNode?.Tag is DialogueNodeData node)
            {
                var newResponse = new DialogueResponseData
                {
                    Text = "Новый ответ",
                    TargetNodeId = null
                };

                node.Responses.Add(newResponse);

                var treeNode = new TreeNode($"Ответ: {newResponse.Text}")
                {
                    Tag = newResponse,
                    ImageKey = "Response",
                    SelectedImageKey = "Response"
                };

                _treeView.SelectedNode.Nodes.Add(treeNode);
                _treeView.SelectedNode = treeNode;
            }
        }

        private void DeleteSelected()
        {
            if (_treeView.SelectedNode?.Tag is DialogueNodeData node)
            {
                _dialogue.Nodes.Remove(node);
                _treeView.SelectedNode.Remove();
            }
            else if (_treeView.SelectedNode?.Tag is DialogueResponseData response)
            {
                var parentNode = _treeView.SelectedNode.Parent;
                if (parentNode?.Tag is DialogueNodeData parentDialogueNode)
                {
                    parentDialogueNode.Responses.Remove(response);
                    _treeView.SelectedNode.Remove();
                }
            }
        }

        private void MoveUp()
        {
            // Реализация перемещения вверх
            var selectedNode = _treeView.SelectedNode;
            if (selectedNode != null && selectedNode.PrevNode != null)
            {
                var index = selectedNode.Index;
                SwapNodes(index, index - 1);
            }
        }

        private void MoveDown()
        {
            // Реализация перемещения вниз
            var selectedNode = _treeView.SelectedNode;
            if (selectedNode != null && selectedNode.NextNode != null)
            {
                var index = selectedNode.Index;
                SwapNodes(index, index + 1);
            }
        }

        private void SwapNodes(int index1, int index2)
        {
            // Реализация обмена узлов
            if (_treeView.SelectedNode?.Parent?.Nodes.Count > Math.Max(index1, index2))
            {
                var node1 = _treeView.SelectedNode.Parent.Nodes[index1];
                var node2 = _treeView.SelectedNode.Parent.Nodes[index2];

                _treeView.SelectedNode.Parent.Nodes.RemoveAt(index1);
                _treeView.SelectedNode.Parent.Nodes.Insert(index2, node1);

                _treeView.SelectedNode.Parent.Nodes.RemoveAt(index1 > index2 ? index2 : index1);
                _treeView.SelectedNode.Parent.Nodes.Insert(index1 > index2 ? index1 : index2, node2);

                _treeView.SelectedNode = index1 > index2 ? node1 : node2;
            }
        }

        private void SaveChanges()
        {
            // Сохранение изменений уже происходит в реальном времени,
            // так как мы работаем напрямую с объектом DialogueData
            DialogResult = DialogResult.OK;
        }
    }
}