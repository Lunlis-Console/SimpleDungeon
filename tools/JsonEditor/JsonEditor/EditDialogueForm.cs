using System;
using System.Windows.Forms;
using Engine.Data;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Reflection;
using System.Globalization;
using System.Diagnostics;

namespace JsonEditor
{
    public class EditDialogueForm : Form
    {
        private DialogueData _dialogue;
        private TextBox txtId;
        private TextBox txtName;
        private PropertyGrid propertyGrid;
        private TreeView treeNodes;
        private Button btnAddNode;
        private Button btnEditNode;
        private Button btnDeleteNode;
        private Button btnAddResponse;
        private Button btnEditResponse;
        private Button btnDeleteResponse;
        private Button btnOk;
        private Button btnCancel;
        private ContextMenuStrip nodeContextMenu;
        private ContextMenuStrip responseContextMenu;
        private ListBox lstResponses;
        private SplitContainer splitContainer;
        private ToolTip toolTip;
        private TextBox txtSearch;
        private Button btnSearch;
        private Button btnPreview;
        private TreeNode _draggedNode;

        public EditDialogueForm(DialogueData dialogue)
        {
            _dialogue = dialogue ?? throw new ArgumentNullException(nameof(dialogue));
            InitializeComponents();
            LoadDataToControls();
        }

        private void InitializeComponents()
        {
            this.Text = "Редактирование диалога";
            this.Width = 1200;
            this.Height = 750;
            this.StartPosition = FormStartPosition.CenterParent;
            this.Padding = new Padding(10);

            // Основные поля - верхняя строка
            var lblId = new Label { Text = "ID:", Location = new Point(10, 15), Width = 30 };
            txtId = new TextBox { Location = new Point(45, 12), Width = 200 };

            var lblName = new Label { Text = "Название:", Location = new Point(255, 15), Width = 70 };
            txtName = new TextBox { Location = new Point(330, 12), Width = 300 };

            // Поиск - верхняя строка
            txtSearch = new TextBox { Location = new Point(640, 12), Width = 200 };
            try { txtSearch.PlaceholderText = "Поиск узлов..."; } catch { }
            btnSearch = new Button { Text = "Найти", Location = new Point(850, 12), Width = 60 };

            // SplitContainer для дерева и propertyGrid
            splitContainer = new SplitContainer
            {
                Location = new Point(10, 45),
                Size = new Size(1160, 400),
                Orientation = Orientation.Vertical,
                SplitterDistance = 400
            };

            // Дерево узлов
            var lblNodes = new Label { Text = "Узлы диалога:", Location = new Point(10, 25), Width = 120 };
            treeNodes = new TreeView
            {
                Dock = DockStyle.Fill,
                ShowNodeToolTips = true,
                AllowDrop = true
            };

            // PropertyGrid для редактирования
            propertyGrid = new PropertyGrid
            {
                Dock = DockStyle.Fill,
                ToolbarVisible = false,
                HelpVisible = false
            };

            // Кнопки управления узлами - под splitContainer
            btnAddNode = new Button { Text = "Добавить узел", Location = new Point(10, 455), Width = 120 };
            btnEditNode = new Button { Text = "Редактировать", Location = new Point(140, 455), Width = 120 };
            btnDeleteNode = new Button { Text = "Удалить", Location = new Point(270, 455), Width = 120 };

            // Список ответов - под кнопками узлов
            var lblResponses = new Label { Text = "Ответы узла:", Location = new Point(10, 490), Width = 120 };
            lstResponses = new ListBox
            {
                Location = new Point(10, 515),
                Width = 400,
                Height = 100
            };

            // Кнопки управления ответами - под списком ответов
            btnAddResponse = new Button { Text = "Добавить ответ", Location = new Point(10, 625), Width = 120 };
            btnEditResponse = new Button { Text = "Редактировать", Location = new Point(140, 625), Width = 120 };
            btnDeleteResponse = new Button { Text = "Удалить", Location = new Point(270, 625), Width = 120 };

            // Кнопки OK/Отмена/Предпросмотр - справа внизу
            btnPreview = new Button { Text = "Предпросмотр", Location = new Point(900, 625), Width = 100 };
            btnOk = new Button { Text = "OK", Location = new Point(1010, 625), Width = 80 };
            btnCancel = new Button { Text = "Отмена", Location = new Point(1100, 625), Width = 80 };

            // Контекстные меню
            nodeContextMenu = new ContextMenuStrip();
            nodeContextMenu.Items.Add("Добавить дочерний узел", null, (s, e) => AddChildNode());
            nodeContextMenu.Items.Add("Редактировать", null, (s, e) => EditSelectedNode());
            nodeContextMenu.Items.Add("Удалить", null, (s, e) => DeleteSelectedNode());
            treeNodes.ContextMenuStrip = nodeContextMenu;

            responseContextMenu = new ContextMenuStrip();
            responseContextMenu.Items.Add("Добавить ответ", null, (s, e) => AddResponse());
            responseContextMenu.Items.Add("Редактировать", null, (s, e) => EditSelectedResponse());
            responseContextMenu.Items.Add("Удалить", null, (s, e) => DeleteSelectedResponse());
            lstResponses.ContextMenuStrip = responseContextMenu;

            // ToolTip
            toolTip = new ToolTip();
            toolTip.SetToolTip(btnAddNode, "Добавить новый узел диалога");
            toolTip.SetToolTip(btnAddResponse, "Добавить ответ к выбранному узлу");
            toolTip.SetToolTip(btnPreview, "Просмотреть структуру диалога");

            // Добавляем элементы в splitContainer
            splitContainer.Panel1.Controls.Add(treeNodes);
            splitContainer.Panel2.Controls.Add(propertyGrid);

            // События
            btnAddNode.Click += BtnAddNode_Click;
            btnEditNode.Click += BtnEditNode_Click;
            btnDeleteNode.Click += BtnDeleteNode_Click;
            btnAddResponse.Click += BtnAddResponse_Click;
            btnEditResponse.Click += BtnEditResponse_Click;
            btnDeleteResponse.Click += BtnDeleteResponse_Click;
            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };
            btnPreview.Click += BtnPreview_Click;
            btnSearch.Click += BtnSearch_Click;
            txtSearch.KeyPress += TxtSearch_KeyPress;

            treeNodes.AfterSelect += TreeNodes_AfterSelect;
            treeNodes.NodeMouseClick += TreeNodes_NodeMouseClick;
            treeNodes.ItemDrag += TreeNodes_ItemDrag;
            treeNodes.DragEnter += TreeNodes_DragEnter;
            treeNodes.DragDrop += TreeNodes_DragDrop;

            lstResponses.SelectedIndexChanged += LstResponses_SelectedIndexChanged;
            lstResponses.MouseClick += LstResponses_MouseClick;

            // Добавление элементов на форму в правильном порядке
            this.Controls.AddRange(new Control[] {
                lblId, txtId,
                lblName, txtName,
                txtSearch, btnSearch,
                lblNodes,
                splitContainer,
                btnAddNode, btnEditNode, btnDeleteNode,
                lblResponses, lstResponses,
                btnAddResponse, btnEditResponse, btnDeleteResponse,
                btnPreview, btnOk, btnCancel
            });
        }

        #region Tree drag/drop and helpers

        private void TreeNodes_ItemDrag(object sender, ItemDragEventArgs e)
        {
            if (e.Item is TreeNode node)
            {
                _draggedNode = node;
                DoDragDrop(e.Item, DragDropEffects.Move);
            }
        }

        private void TreeNodes_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Move;
        }

        private void TreeNodes_DragDrop(object sender, DragEventArgs e)
        {
            if (_draggedNode == null) return;

            Point targetPoint = treeNodes.PointToClient(new Point(e.X, e.Y));
            TreeNode targetNode = treeNodes.GetNodeAt(targetPoint);

            if (targetNode != null && targetNode != _draggedNode &&
                !IsDescendant(_draggedNode, targetNode))
            {
                if (_draggedNode.Tag is DialogueNodeData draggedData && targetNode.Tag is DialogueNodeData targetData)
                {
                    draggedData.ParentId = targetData.Id;

                    // Перестраиваем дерево
                    BuildTreeView();

                    // Выделяем перемещенный узел
                    treeNodes.SelectedNode = FindTreeNode(draggedData.Id);
                }
            }
        }

        private bool IsDescendant(TreeNode parent, TreeNode child)
        {
            while (child != null)
            {
                if (child.Parent == parent) return true;
                child = child.Parent;
            }
            return false;
        }

        #endregion

        #region Search / Preview / Events

        private void BtnSearch_Click(object sender, EventArgs e) => SearchNodes(txtSearch.Text);

        private void TxtSearch_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Enter)
            {
                SearchNodes(txtSearch.Text);
                e.Handled = true;
            }
        }

        private void SearchNodes(string searchText)
        {
            if (string.IsNullOrEmpty(searchText)) return;

            foreach (TreeNode node in treeNodes.Nodes)
            {
                var foundNode = SearchNodeRecursive(node, searchText);
                if (foundNode != null)
                {
                    treeNodes.SelectedNode = foundNode;
                    treeNodes.Focus();
                    return;
                }
            }

            MessageBox.Show("Узел не найден");
        }

        private TreeNode SearchNodeRecursive(TreeNode parentNode, string searchText)
        {
            if (parentNode.Tag is DialogueNodeData nodeData)
            {
                if ((!string.IsNullOrEmpty(nodeData.Id) && nodeData.Id.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    (!string.IsNullOrEmpty(nodeData.Text) && nodeData.Text.Contains(searchText, StringComparison.OrdinalIgnoreCase)))
                {
                    return parentNode;
                }
            }

            foreach (TreeNode childNode in parentNode.Nodes)
            {
                var found = SearchNodeRecursive(childNode, searchText);
                if (found != null) return found;
            }

            return null;
        }

        private void BtnPreview_Click(object sender, EventArgs e)
        {
            using (var previewForm = new DialoguePreviewForm(_dialogue))
            {
                previewForm.ShowDialog();
            }
        }

        private void TreeNodes_NodeMouseClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                treeNodes.SelectedNode = e.Node;
            }
        }

        private void LstResponses_MouseClick(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                int index = lstResponses.IndexFromPoint(e.Location);
                if (index != ListBox.NoMatches)
                {
                    lstResponses.SelectedIndex = index;
                }
            }
        }

        #endregion

        private void LoadDataToControls()
        {
            txtId.Text = _dialogue.Id;
            txtName.Text = _dialogue.Name;
            BuildTreeView();
        }

        private void BuildTreeView()
        {
            treeNodes.Nodes.Clear();
            lstResponses.Items.Clear();

            if (_dialogue.Nodes == null)
                _dialogue.Nodes = new List<DialogueNodeData>();

            // Находим корневые узлы (те, у которых нет родителя)
            var rootNodes = _dialogue.Nodes.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();

            foreach (var node in rootNodes)
            {
                var treeNode = CreateTreeNode(node);
                treeNodes.Nodes.Add(treeNode);
                AddChildNodes(treeNode, node.Id);
            }

            treeNodes.ExpandAll();
            HighlightBrokenLinks();
        }

        private void HighlightBrokenLinks()
        {
            var nodeIds = _dialogue.Nodes.Select(n => n.Id).ToHashSet();

            foreach (TreeNode treeNode in treeNodes.Nodes)
            {
                HighlightNodeRecursive(treeNode, nodeIds);
            }
        }

        private void HighlightNodeRecursive(TreeNode treeNode, HashSet<string> validNodeIds)
        {
            if (treeNode.Tag is DialogueNodeData nodeData)
            {
                // Проверяем родительскую ссылку
                if (!string.IsNullOrEmpty(nodeData.ParentId) && !validNodeIds.Contains(nodeData.ParentId))
                {
                    treeNode.BackColor = Color.LightPink;
                    treeNode.ToolTipText = "Неверная ссылка на родительский узел";
                }

                // Проверяем новый формат Options (через reflection)
                var ndType = nodeData.GetType();
                var optProp = ndType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);
                bool foundBroken = false;
                if (optProp != null)
                {
                    var opts = optProp.GetValue(nodeData) as System.Collections.IEnumerable;
                    if (opts != null)
                    {
                        foreach (var o in opts)
                        {
                            var oType = o.GetType();
                            var nextProp = oType.GetProperty("NextNodeId") ?? oType.GetProperty("TargetNodeId") ?? oType.GetProperty("Next") ?? oType.GetProperty("Target");
                            var nextId = nextProp?.GetValue(o)?.ToString();
                            if (!string.IsNullOrEmpty(nextId) && !validNodeIds.Contains(nextId))
                            {
                                treeNode.BackColor = Color.LightYellow;
                                treeNode.ToolTipText = "Есть ответы с неверными ссылками";
                                foundBroken = true;
                                break;
                            }
                        }
                    }
                }

                if (!foundBroken && nodeData.Responses != null)
                {
                    foreach (var response in nodeData.Responses)
                    {
                        if (!string.IsNullOrEmpty(response.TargetNodeId) &&
                            !validNodeIds.Contains(response.TargetNodeId))
                        {
                            treeNode.BackColor = Color.LightYellow;
                            treeNode.ToolTipText = "Есть ответы с неверными ссылками";
                            break;
                        }
                    }
                }
            }

            foreach (TreeNode childNode in treeNode.Nodes)
            {
                HighlightNodeRecursive(childNode, validNodeIds);
            }
        }

        private TreeNode CreateTreeNode(DialogueNodeData node)
        {
            return new TreeNode($"{node.Id}: {TruncateText(node.Text, 30)}")
            {
                Tag = node,
                ToolTipText = node.Text
            };
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "(без текста)";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private void AddChildNodes(TreeNode parentTreeNode, string parentId)
        {
            var childNodes = _dialogue.Nodes.Where(n => n.ParentId == parentId).ToList();

            foreach (var childNode in childNodes)
            {
                var childTreeNode = CreateTreeNode(childNode);
                parentTreeNode.Nodes.Add(childTreeNode);
                AddChildNodes(childTreeNode, childNode.Id);
            }
        }

        private void BtnAddNode_Click(object sender, EventArgs e)
        {
            AddNewNode(null); // Добавляем корневой узел
        }

        private void AddChildNode()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode)
            {
                AddNewNode(selectedNode.Id);
            }
            else
            {
                MessageBox.Show("Выберите узел для добавления дочернего элемента");
            }
        }

        private void AddNewNode(string parentId)
        {
            var newNode = new DialogueNodeData
            {
                Id = GenerateUniqueId(),
                ParentId = parentId,
                Text = "Новый узел диалога",
                Responses = new List<DialogueResponseData>()
            };

            using (var form = new EditNodeForm(newNode, _dialogue.Nodes))
            {
                if (form.ShowDialog() == DialogResult.OK)
                {
                    _dialogue.Nodes.Add(form.Node);
                    BuildTreeView();
                    // Выделяем новый узел
                    var newTreeNode = FindTreeNode(form.Node.Id);
                    if (newTreeNode != null)
                    {
                        treeNodes.SelectedNode = newTreeNode;
                    }
                }
            }
        }

        private TreeNode FindTreeNode(string nodeId)
        {
            foreach (TreeNode node in treeNodes.Nodes)
            {
                var result = FindTreeNodeRecursive(node, nodeId);
                if (result != null) return result;
            }
            return null;
        }

        private TreeNode FindTreeNodeRecursive(TreeNode parentNode, string nodeId)
        {
            if (parentNode.Tag is DialogueNodeData nodeData && nodeData.Id == nodeId)
                return parentNode;

            foreach (TreeNode childNode in parentNode.Nodes)
            {
                var result = FindTreeNodeRecursive(childNode, nodeId);
                if (result != null) return result;
            }

            return null;
        }

        private string GenerateUniqueId()
        {
            int counter = 1;
            string baseId = "node_";

            while (_dialogue.Nodes.Any(n => n.Id == baseId + counter))
            {
                counter++;
            }

            return baseId + counter;
        }

        private void BtnEditNode_Click(object sender, EventArgs e) => EditSelectedNode();

        private void EditSelectedNode()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode)
            {
                using (var form = new EditNodeForm(selectedNode, _dialogue.Nodes))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        BuildTreeView();
                        // Обновляем список ответов, если редактировался текущий выбранный узел
                        if (treeNodes.SelectedNode?.Tag is DialogueNodeData currentNode && currentNode.Id == selectedNode.Id)
                        {
                            LoadResponsesForNode(currentNode);
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите узел для редактирования");
            }
        }

        private void BtnDeleteNode_Click(object sender, EventArgs e) => DeleteSelectedNode();

        private void DeleteSelectedNode()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode)
            {
                if (MessageBox.Show("Удалить этот узел и все дочерние узлы?", "Подтверждение",
                    MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                {
                    RemoveNodeAndChildren(selectedNode.Id);
                    BuildTreeView();
                }
            }
            else
            {
                MessageBox.Show("Выберите узел для удаления");
            }
        }

        private void RemoveNodeAndChildren(string nodeId)
        {
            // Рекурсивное удаление узла и всех его потомков
            var nodesToRemove = new List<string> { nodeId };
            var children = _dialogue.Nodes.Where(n => n.ParentId == nodeId).ToList();

            foreach (var child in children)
            {
                nodesToRemove.AddRange(GetAllDescendantIds(child.Id));
            }

            _dialogue.Nodes.RemoveAll(n => nodesToRemove.Contains(n.Id));
        }

        private List<string> GetAllDescendantIds(string parentId)
        {
            var result = new List<string>();
            var children = _dialogue.Nodes.Where(n => n.ParentId == parentId).ToList();

            foreach (var child in children)
            {
                result.Add(child.Id);
                result.AddRange(GetAllDescendantIds(child.Id));
            }

            return result;
        }

        private void TreeNodes_AfterSelect(object sender, TreeViewEventArgs e)
        {
            if (e.Node?.Tag is DialogueNodeData node)
            {
                propertyGrid.SelectedObject = node;
                LoadResponsesForNode(node);
            }
            else
            {
                propertyGrid.SelectedObject = null;
                lstResponses.Items.Clear();
            }
        }

        private void LoadResponsesForNode(DialogueNodeData node)
        {
            lstResponses.Items.Clear();

            // Если есть Options (новый формат) — показываем их (через reflection)
            var ndType = node.GetType();
            var optProp = ndType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);
            if (optProp != null)
            {
                var opts = optProp.GetValue(node) as System.Collections.IEnumerable;
                if (opts != null)
                {
                    foreach (var opt in opts)
                    {
                        var optType = opt.GetType();
                        var textProp = optType.GetProperty("Text");
                        var nextProp = optType.GetProperty("NextNodeId") ?? optType.GetProperty("TargetNodeId") ?? optType.GetProperty("Next") ?? optType.GetProperty("Target");
                        var text = textProp?.GetValue(opt)?.ToString() ?? "(без текста)";
                        var nextId = nextProp?.GetValue(opt)?.ToString();
                        lstResponses.Items.Add($"{nextId}: {TruncateText(text, 50)}");
                    }
                    return;
                }
            }

            // fallback — старый формат Responses
            if (node.Responses != null)
            {
                foreach (var response in node.Responses)
                {
                    lstResponses.Items.Add($"{response.TargetNodeId}: {TruncateText(response.Text, 50)}");
                }
            }
        }

        private void LstResponses_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode &&
                lstResponses.SelectedIndex >= 0)
            {
                // Проверяем новый формат Options
                var ndType = selectedNode.GetType();
                var optProp = ndType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);

                if (optProp != null)
                {
                    var opts = optProp.GetValue(selectedNode) as System.Collections.IList;
                    if (opts != null && lstResponses.SelectedIndex < opts.Count)
                    {
                        propertyGrid.SelectedObject = opts[lstResponses.SelectedIndex];
                        return;
                    }
                }

                // Fallback на старый формат Responses
                if (selectedNode.Responses != null && lstResponses.SelectedIndex < selectedNode.Responses.Count)
                {
                    propertyGrid.SelectedObject = selectedNode.Responses[lstResponses.SelectedIndex];
                    return;
                }
            }

            propertyGrid.SelectedObject = null;
        }

        private void BtnAddResponse_Click(object sender, EventArgs e) => AddResponse();

        private void AddResponse()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode)
            {
                var newResponse = new DialogueResponseData
                {
                    Text = "Новый ответ",
                    TargetNodeId = null
                };

                using (var form = new EditResponseForm(newResponse, _dialogue.Nodes, selectedNode.Id))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        if (selectedNode.Responses == null)
                            selectedNode.Responses = new List<DialogueResponseData>();

                        // Приводим объект обратно к DialogueResponseData
                        if (form.Response is DialogueResponseData responseData)
                        {
                            selectedNode.Responses.Add(responseData);
                            LoadResponsesForNode(selectedNode);
                            if (lstResponses.Items.Count > 0)
                                lstResponses.SelectedIndex = lstResponses.Items.Count - 1;
                        }
                        else
                        {
                            MessageBox.Show("Ошибка: неверный тип ответа");
                        }
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите узел для добавления ответа");
            }
        }
        private void BtnEditResponse_Click(object sender, EventArgs e) => EditSelectedResponse();

        private void EditSelectedResponse()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode &&
                lstResponses.SelectedIndex >= 0)
            {
                // Проверяем новый формат Options
                var ndType = selectedNode.GetType();
                var optProp = ndType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);

                if (optProp != null)
                {
                    var opts = optProp.GetValue(selectedNode) as System.Collections.IList;
                    if (opts != null && lstResponses.SelectedIndex < opts.Count)
                    {
                        var responseToEdit = opts[lstResponses.SelectedIndex];
                        using (var form = new EditResponseForm(responseToEdit, _dialogue.Nodes, selectedNode.Id))
                        {
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                LoadResponsesForNode(selectedNode);
                                lstResponses.SelectedIndex = lstResponses.SelectedIndex;
                            }
                        }
                        return;
                    }
                }

                // Fallback на старый формат Responses
                if (selectedNode.Responses != null && lstResponses.SelectedIndex < selectedNode.Responses.Count)
                {
                    var responseToEdit = selectedNode.Responses[lstResponses.SelectedIndex];
                    using (var form = new EditResponseForm(responseToEdit, _dialogue.Nodes, selectedNode.Id))
                    {
                        if (form.ShowDialog() == DialogResult.OK)
                        {
                            LoadResponsesForNode(selectedNode);
                            lstResponses.SelectedIndex = lstResponses.SelectedIndex;
                        }
                    }
                    return;
                }
            }

            MessageBox.Show("Выберите ответ для редактирования");
        }

        private void BtnDeleteResponse_Click(object sender, EventArgs e) => DeleteSelectedResponse();

        private void DeleteSelectedResponse()
        {
            if (treeNodes.SelectedNode?.Tag is DialogueNodeData selectedNode &&
                lstResponses.SelectedIndex >= 0)
            {
                // Проверяем новый формат Options
                var ndType = selectedNode.GetType();
                var optProp = ndType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);

                if (optProp != null)
                {
                    var opts = optProp.GetValue(selectedNode) as System.Collections.IList;
                    if (opts != null && lstResponses.SelectedIndex < opts.Count)
                    {
                        if (MessageBox.Show("Удалить этот ответ?", "Подтверждение",
                            MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                        {
                            opts.RemoveAt(lstResponses.SelectedIndex);
                            LoadResponsesForNode(selectedNode);
                        }
                        return;
                    }
                }

                // Fallback на старый формат Responses
                if (selectedNode.Responses != null && lstResponses.SelectedIndex < selectedNode.Responses.Count)
                {
                    if (MessageBox.Show("Удалить этот ответ?", "Подтверждение",
                        MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                    {
                        selectedNode.Responses.RemoveAt(lstResponses.SelectedIndex);
                        LoadResponsesForNode(selectedNode);
                    }
                    return;
                }
            }

            MessageBox.Show("Выберите ответ для удаления");
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Попытка миграции Responses -> Options перед валидацией/сохранением
            try
            {
                ConvertResponsesToOptionsReflection(_dialogue);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"ConvertResponsesToOptionsReflection failed: {ex.Message}");
            }

            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show("ID не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Проверка целостности диалога
            if (!ValidateDialogueStructure())
            {
                return;
            }

            _dialogue.Id = txtId.Text.Trim();
            _dialogue.Name = txtName.Text.Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private bool ValidateDialogueStructure()
        {
            // Проверяем, что все ParentId ссылаются на существующие узлы
            var nodeIds = _dialogue.Nodes.Select(n => n.Id).ToHashSet();

            foreach (var node in _dialogue.Nodes)
            {
                if (!string.IsNullOrEmpty(node.ParentId) && !nodeIds.Contains(node.ParentId))
                {
                    MessageBox.Show($"Узел '{node.Id}' ссылается на несуществующий родительский узел '{node.ParentId}'",
                        "Ошибка структуры", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
            }

            // Проверяем, что TargetNodeId в ответах ссылаются на существующие узлы (если указаны)
            foreach (var node in _dialogue.Nodes)
            {
                if (node.Responses != null)
                {
                    foreach (var response in node.Responses)
                    {
                        if (!string.IsNullOrEmpty(response.TargetNodeId) && !nodeIds.Contains(response.TargetNodeId))
                        {
                            MessageBox.Show($"Ответ в узле '{node.Id}' ссылается на несуществующий целевой узел '{response.TargetNodeId}'",
                                "Ошибка структуры", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return false;
                        }
                    }
                }
            }

            // Проверяем новый формат Options (через reflection) на корректные ссылки
            foreach (var node in _dialogue.Nodes)
            {
                var ndType = node.GetType();
                var optProp = ndType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);
                if (optProp != null)
                {
                    var opts = optProp.GetValue(node) as System.Collections.IEnumerable;
                    if (opts != null)
                    {
                        foreach (var o in opts)
                        {
                            var oType = o.GetType();
                            var nextProp = oType.GetProperty("NextNodeId") ?? oType.GetProperty("TargetNodeId") ?? oType.GetProperty("Next") ?? oType.GetProperty("Target");
                            var nextId = nextProp?.GetValue(o)?.ToString();
                            if (!string.IsNullOrEmpty(nextId) && !nodeIds.Contains(nextId))
                            {
                                MessageBox.Show($"Опция в узле '{node.Id}' ссылается на несуществующий целевой узел '{nextId}'",
                                    "Ошибка структуры", MessageBoxButtons.OK, MessageBoxIcon.Error);
                                return false;
                            }
                        }
                    }
                }
            }

            // Проверяем, что есть хотя бы один корневой узел
            if (!_dialogue.Nodes.Any(n => string.IsNullOrEmpty(n.ParentId)))
            {
                MessageBox.Show("Должен быть хотя бы один корневой узел (без родителя)",
                    "Ошибка структуры", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            return true;
        }

        #region Responses -> Options migration & helpers

        private void ConvertResponsesToOptionsReflection(DialogueData dialog)
        {
            if (dialog?.Nodes == null) return;

            foreach (var node in dialog.Nodes)
            {
                ConvertNodeResponsesToOptionsReflection(node);
            }
        }

        private void ConvertNodeResponsesToOptionsReflection(object node)
        {
            if (node == null) return;

            var nodeType = node.GetType();
            var responsesProp = nodeType.GetProperty("Responses", BindingFlags.Public | BindingFlags.Instance);
            var optionsProp = nodeType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);

            if (responsesProp == null || optionsProp == null) return;

            var responsesValue = responsesProp.GetValue(node) as System.Collections.IEnumerable;
            if (responsesValue == null) return;

            var optionsListType = optionsProp.PropertyType;
            Type elemType = null;
            if (optionsListType.IsGenericType)
            {
                elemType = optionsListType.GetGenericArguments()[0];
            }
            if (elemType == null) return;

            var optionsListObj = Activator.CreateInstance(optionsListType) as System.Collections.IList;
            if (optionsListObj == null) return;

            foreach (var resp in responsesValue)
            {
                try
                {
                    var optObj = Activator.CreateInstance(elemType);
                    var respType = resp.GetType();

                    var respTextProp = respType.GetProperty("Text") ?? respType.GetProperty("ResponseText");
                    var dstTextProp = elemType.GetProperty("Text");
                    if (respTextProp != null && dstTextProp != null && dstTextProp.CanWrite)
                    {
                        var t = respTextProp.GetValue(resp);
                        TrySetPropertyValue(optObj, dstTextProp, t);
                    }

                    var respTargetProp = respType.GetProperty("TargetNodeId") ?? respType.GetProperty("NextNodeId") ?? respType.GetProperty("Target") ?? respType.GetProperty("Next");
                    var dstNextProp = elemType.GetProperty("NextNodeId") ?? elemType.GetProperty("TargetNodeId") ?? elemType.GetProperty("Next") ?? elemType.GetProperty("Target");
                    if (respTargetProp != null && dstNextProp != null && dstNextProp.CanWrite)
                    {
                        var v = respTargetProp.GetValue(resp);

                        if (v is string vs && string.IsNullOrWhiteSpace(vs))
                        {
                            // пустая строка -> null для nullable/sref или пропустить
                            if (!dstNextProp.PropertyType.IsValueType || Nullable.GetUnderlyingType(dstNextProp.PropertyType) != null)
                                TrySetPropertyValue(optObj, dstNextProp, null);
                        }
                        else
                        {
                            TrySetPropertyValue(optObj, dstNextProp, v);
                        }
                    }

                    var possibleExtras = new[] { "Action", "Parameter", "Condition", "Value", "Id" };
                    foreach (var name in possibleExtras)
                    {
                        var s = respType.GetProperty(name);
                        var d = elemType.GetProperty(name);
                        if (s != null && d != null && d.CanWrite)
                        {
                            var vv = s.GetValue(resp);
                            if (vv != null) TrySetPropertyValue(optObj, d, vv);
                        }
                    }

                    optionsListObj.Add(optObj);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"ConvertNodeResponsesToOptionsReflection: skipped response due to {ex.Message}");
                }
            }

            try { optionsProp.SetValue(node, optionsListObj); } catch { }
            try { if (responsesProp.CanWrite) responsesProp.SetValue(node, null); } catch { }
        }

        /// <summary>
        /// Безопасная установка свойства с попыткой конвертации типов.
        /// </summary>
        private bool TrySetPropertyValue(object targetObj, PropertyInfo prop, object value)
        {
            if (prop == null || !prop.CanWrite) return false;
            if (targetObj == null) return false;

            if (value == null)
            {
                try
                {
                    prop.SetValue(targetObj, null);
                    return true;
                }
                catch
                {
                    return false;
                }
            }

            var targetType = Nullable.GetUnderlyingType(prop.PropertyType) ?? prop.PropertyType;

            try
            {
                // если значение уже того типа
                if (value != null && targetType.IsInstanceOfType(value))
                {
                    prop.SetValue(targetObj, value);
                    return true;
                }

                // если строка — пробуем парсить
                if (value is string s)
                {
                    s = s.Trim();
                    if (string.IsNullOrEmpty(s))
                    {
                        // пустая строка -> null если возможно
                        if (!prop.PropertyType.IsValueType || Nullable.GetUnderlyingType(prop.PropertyType) != null)
                        {
                            prop.SetValue(targetObj, null);
                            return true;
                        }
                        return false;
                    }

                    if (targetType.IsEnum)
                    {
                        if (Enum.TryParse(targetType, s, true, out var enumVal))
                        {
                            prop.SetValue(targetObj, enumVal);
                            return true;
                        }
                    }

                    if (targetType == typeof(Guid) && Guid.TryParse(s, out var g))
                    {
                        prop.SetValue(targetObj, g);
                        return true;
                    }

                    if (targetType == typeof(int) && int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var i))
                    {
                        prop.SetValue(targetObj, i); return true;
                    }
                    if (targetType == typeof(long) && long.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var l))
                    {
                        prop.SetValue(targetObj, l); return true;
                    }
                    if (targetType == typeof(short) && short.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var sh))
                    {
                        prop.SetValue(targetObj, sh); return true;
                    }
                    if (targetType == typeof(byte) && byte.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var by))
                    {
                        prop.SetValue(targetObj, by); return true;
                    }
                    if (targetType == typeof(bool) && bool.TryParse(s, out var b))
                    {
                        prop.SetValue(targetObj, b); return true;
                    }
                    if (targetType == typeof(double) && double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var d))
                    {
                        prop.SetValue(targetObj, d); return true;
                    }
                    if (targetType == typeof(float) && float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out var f))
                    {
                        prop.SetValue(targetObj, f); return true;
                    }

                    // последняя попытка
                    var converted = Convert.ChangeType(s, targetType, CultureInfo.InvariantCulture);
                    prop.SetValue(targetObj, converted);
                    return true;
                }
                else
                {
                    // value не строка — попытка Convert.ChangeType
                    if (value is IConvertible)
                    {
                        var converted = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                        prop.SetValue(targetObj, converted);
                        return true;
                    }

                    // пробуем прямую установку через ChangeType как последнее средство
                    var conv2 = Convert.ChangeType(value, targetType, CultureInfo.InvariantCulture);
                    prop.SetValue(targetObj, conv2);
                    return true;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"TrySetPropertyValue failed for '{prop.Name}' -> {ex.Message}");
                return false;
            }
        }

        #endregion
    }

    // Форма для предпросмотра диалога
    public class DialoguePreviewForm : Form
    {
        private DialogueData _dialogue;
        private RichTextBox txtPreview;

        public DialoguePreviewForm(DialogueData dialogue)
        {
            _dialogue = dialogue;
            InitializeComponents();

            if (_dialogue?.Nodes != null)
            {
                var rootNodes = _dialogue.Nodes.Where(n => string.IsNullOrEmpty(n.ParentId)).ToList();
                var visited = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                foreach (var root in rootNodes)
                {
                    AddNodeToPreview(root, 0, visited);
                }
            }
        }

        private void InitializeComponents()
        {
            this.Text = "Предпросмотр диалога";
            this.Width = 600;
            this.Height = 500;
            this.StartPosition = FormStartPosition.CenterParent;

            txtPreview = new RichTextBox { Dock = DockStyle.Fill, ReadOnly = true };
            this.Controls.Add(txtPreview);
        }

        private void AddNodeToPreview(DialogueNodeData node, int indentLevel, HashSet<string> visited)
        {
            if (node == null) return;

            if (!string.IsNullOrEmpty(node.Id))
            {
                if (visited.Contains(node.Id)) return;
                visited.Add(node.Id);
            }

            string indent = new string(' ', indentLevel * 4);
            txtPreview.AppendText($"{indent}[{node.Id}] {node.Text}\n");

            // Сначала пробуем Options (новый формат) через reflection
            var nodeType = node.GetType();
            var optionsProp = nodeType.GetProperty("Options", BindingFlags.Public | BindingFlags.Instance);
            if (optionsProp != null)
            {
                var optionsVal = optionsProp.GetValue(node) as System.Collections.IEnumerable;
                if (optionsVal != null)
                {
                    foreach (var opt in optionsVal)
                    {
                        var optType = opt.GetType();
                        var textProp = optType.GetProperty("Text");
                        var nextProp = optType.GetProperty("NextNodeId") ?? optType.GetProperty("TargetNodeId") ?? optType.GetProperty("Next") ?? optType.GetProperty("Target");
                        var text = textProp?.GetValue(opt)?.ToString() ?? "(без текста)";
                        var targetId = nextProp?.GetValue(opt)?.ToString();

                        txtPreview.AppendText($"{indent}  → {text}");

                        if (!string.IsNullOrWhiteSpace(targetId))
                        {
                            var targetNode = _dialogue.Nodes.FirstOrDefault(n => n.Id == targetId);
                            if (targetNode != null)
                            {
                                txtPreview.AppendText($" → [{targetNode.Id}]\n");
                                AddNodeToPreview(targetNode, indentLevel + 2, visited);
                            }
                            else
                            {
                                txtPreview.AppendText($" [НЕТ УЗЛА {targetId}]\n");
                            }
                        }
                        else
                        {
                            txtPreview.AppendText(" [КОНЕЦ]\n");
                        }
                    }
                    return;
                }
            }

            // Fallback на старый формат Responses
            if (node.Responses != null)
            {
                foreach (var response in node.Responses)
                {
                    txtPreview.AppendText($"{indent}  → {response.Text}");

                    if (!string.IsNullOrEmpty(response.TargetNodeId))
                    {
                        var targetNode = _dialogue.Nodes.FirstOrDefault(n => n.Id == response.TargetNodeId);
                        if (targetNode != null)
                        {
                            txtPreview.AppendText($" → [{targetNode.Id}]\n");
                            AddNodeToPreview(targetNode, indentLevel + 2, visited);
                        }
                        else
                        {
                            txtPreview.AppendText($" [НЕТ УЗЛА {response.TargetNodeId}]\n");
                        }
                    }
                    else
                    {
                        txtPreview.AppendText(" [КОНЕЦ]\n");
                    }
                }
            }
        }
    }

    // Вспомогательная форма для редактирования отдельного узла
    public class EditNodeForm : Form
    {
        public DialogueNodeData Node { get; private set; }
        private List<DialogueNodeData> _allNodes;
        private TextBox txtId;
        private TextBox txtText;
        private ComboBox cmbParentId;
        private Button btnOk;
        private Button btnCancel;

        public EditNodeForm(DialogueNodeData node, List<DialogueNodeData> allNodes)
        {
            Node = node;
            _allNodes = allNodes;
            InitializeComponents();
            LoadDataToControls();
        }

        private void InitializeComponents()
        {
            this.Text = "Редактирование узла диалога";
            this.Width = 400;
            this.Height = 250;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblId = new Label { Text = "ID узла:", Left = 10, Top = 15, Width = 80 };
            txtId = new TextBox { Left = 100, Top = 12, Width = 280 };

            var lblText = new Label { Text = "Текст:", Left = 10, Top = 45, Width = 80 };
            txtText = new TextBox { Left = 100, Top = 42, Width = 280, Height = 60, Multiline = true };

            var lblParent = new Label { Text = "Родительский ID:", Left = 10, Top = 115, Width = 80 };
            cmbParentId = new ComboBox { Left = 100, Top = 112, Width = 280, DropDownStyle = ComboBoxStyle.DropDown };

            btnOk = new Button { Text = "OK", Left = 220, Top = 170, Width = 80 };
            btnCancel = new Button { Text = "Отмена", Left = 310, Top = 170, Width = 80 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblId, txtId,
                lblText, txtText,
                lblParent, cmbParentId,
                btnOk, btnCancel
            });
        }

        private void LoadDataToControls()
        {
            txtId.Text = Node.Id;
            txtText.Text = Node.Text;

            // Заполняем комбобокс доступными ID узлов (исключая текущий узел и его потомков)
            cmbParentId.Items.Clear();
            cmbParentId.Items.Add(""); // Пустой элемент для корневого узла

            var availableNodes = _allNodes
                .Where(n => n.Id != Node.Id && !IsDescendant(Node.Id, n.Id))
                .Select(n => n.Id)
                .ToList();

            foreach (var nodeId in availableNodes)
            {
                cmbParentId.Items.Add(nodeId);
            }

            cmbParentId.Text = Node.ParentId;
        }

        private bool IsDescendant(string potentialAncestorId, string potentialDescendantId)
        {
            var currentNode = _allNodes.FirstOrDefault(n => n.Id == potentialDescendantId);
            while (currentNode != null && !string.IsNullOrEmpty(currentNode.ParentId))
            {
                if (currentNode.ParentId == potentialAncestorId)
                    return true;
                currentNode = _allNodes.FirstOrDefault(n => n.Id == currentNode.ParentId);
            }
            return false;
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtId.Text))
            {
                MessageBox.Show("ID узла не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Проверяем уникальность ID (кроме текущего узла)
            if (_allNodes.Any(n => n.Id == txtId.Text.Trim() && n != Node))
            {
                MessageBox.Show("Узел с таким ID уже существует", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Node.Id = txtId.Text.Trim();
            Node.Text = txtText.Text.Trim();
            Node.ParentId = (cmbParentId.Text ?? "").Trim();

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }

    // Вспомогательная форма для редактирования ответа
    public class EditResponseForm : Form
    {
        public object Response { get; private set; }
        private List<DialogueNodeData> _allNodes;
        private string _currentNodeId;
        private TextBox txtText;
        private ComboBox cmbTargetNodeId;
        private Button btnOk;
        private Button btnCancel;

        public EditResponseForm(object response, List<DialogueNodeData> allNodes, string currentNodeId)
        {
            Response = response;
            _allNodes = allNodes;
            _currentNodeId = currentNodeId;
            InitializeComponents();
            LoadDataToControls();
        }

        private void InitializeComponents()
        {
            this.Text = "Редактирование ответа";
            this.Width = 400;
            this.Height = 220;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblText = new Label { Text = "Текст ответа:", Left = 10, Top = 15, Width = 100 };
            txtText = new TextBox { Left = 120, Top = 12, Width = 260, Height = 60, Multiline = true };

            var lblTarget = new Label { Text = "Целевой узел:", Left = 10, Top = 85, Width = 100 };
            cmbTargetNodeId = new ComboBox
            {
                Left = 120,
                Top = 82,
                Width = 260,
                DropDownStyle = ComboBoxStyle.DropDown,
                AutoCompleteMode = AutoCompleteMode.SuggestAppend,
                AutoCompleteSource = AutoCompleteSource.ListItems
            };

            btnOk = new Button { Text = "OK", Left = 220, Top = 130, Width = 80 };
            btnCancel = new Button { Text = "Отмена", Left = 310, Top = 130, Width = 80 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.AddRange(new Control[] {
                lblText, txtText,
                lblTarget, cmbTargetNodeId,
                btnOk, btnCancel
            });
        }

        private void LoadDataToControls()
        {
            // Получаем текст ответа через reflection
            var responseType = Response.GetType();
            var textProp = responseType.GetProperty("Text") ?? responseType.GetProperty("ResponseText");
            if (textProp != null)
            {
                txtText.Text = textProp.GetValue(Response)?.ToString() ?? "";
            }

            // Получаем целевой узел через reflection
            var targetProp = responseType.GetProperty("TargetNodeId") ?? responseType.GetProperty("NextNodeId") ?? responseType.GetProperty("Target") ?? responseType.GetProperty("Next");
            string currentTargetId = null;
            if (targetProp != null)
            {
                currentTargetId = targetProp.GetValue(Response)?.ToString();
            }

            // Автодополнение для целевых узлов
            cmbTargetNodeId.Items.Clear();
            cmbTargetNodeId.Items.Add(""); // Пустой элемент для завершения диалога

            var availableNodes = _allNodes
                .Where(n => n.Id != _currentNodeId)
                .Select(n => new { Id = n.Id, Display = $"{n.Id}: {TruncateText(n.Text, 30)}" })
                .ToList();

            cmbTargetNodeId.DisplayMember = "Display";
            cmbTargetNodeId.ValueMember = "Id";

            foreach (var node in availableNodes)
            {
                cmbTargetNodeId.Items.Add(node);
            }

            // Устанавливаем значение
            if (!string.IsNullOrEmpty(currentTargetId))
            {
                var selectedItem = availableNodes.FirstOrDefault(n => n.Id == currentTargetId);
                if (selectedItem != null)
                    cmbTargetNodeId.SelectedItem = selectedItem;
                else
                    cmbTargetNodeId.Text = currentTargetId;
            }
            else
            {
                cmbTargetNodeId.Text = "";
            }
        }

        private string TruncateText(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "(без текста)";
            return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtText.Text))
            {
                MessageBox.Show("Текст ответа не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Устанавливаем текст ответа через reflection
            var responseType = Response.GetType();
            var textProp = responseType.GetProperty("Text") ?? responseType.GetProperty("ResponseText");
            if (textProp != null && textProp.CanWrite)
            {
                textProp.SetValue(Response, txtText.Text.Trim());
            }

            // Устанавливаем целевой узел через reflection
            var targetProp = responseType.GetProperty("TargetNodeId") ?? responseType.GetProperty("NextNodeId") ?? responseType.GetProperty("Target") ?? responseType.GetProperty("Next");
            if (targetProp != null && targetProp.CanWrite)
            {
                if (cmbTargetNodeId.SelectedItem != null)
                {
                    dynamic selectedItem = cmbTargetNodeId.SelectedItem;
                    targetProp.SetValue(Response, selectedItem?.Id?.ToString());
                }
                else
                {
                    var txt = (cmbTargetNodeId.Text ?? "").Trim();
                    targetProp.SetValue(Response, string.IsNullOrEmpty(txt) ? null : txt);
                }
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }
    }
}