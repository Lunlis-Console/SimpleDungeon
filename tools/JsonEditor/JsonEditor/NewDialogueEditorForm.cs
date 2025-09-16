using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Dialogue;
using Engine.Data;
using Newtonsoft.Json;

namespace JsonEditor
{
    /// <summary>
    /// Редактор диалогов для новой системы диалогов
    /// Поддерживает DialogueDocument, DialogueNode, Response
    /// </summary>
    public class NewDialogueEditorForm : Form
    {
        private Engine.Dialogue.DialogueDocument _document;
        private GameData _gameData;
        private string _currentFilePath;

        // UI элементы
        private TextBox _txtDocumentId;
        private TextBox _txtDocumentName;
        private ComboBox _cmbStartNode;
        private ListBox _lstNodes;
        private TextBox _txtNodeId;
        private TextBox _txtNodeText;
        private ComboBox _cmbNodeType;
        private ListBox _lstResponses;
        private TextBox _txtResponseText;
        private ComboBox _cmbResponseTarget;
        private ComboBox _cmbResponseCondition;
        private ListBox _lstActions;
        private ComboBox _cmbActionType;
        private TextBox _txtActionParam;
        private Label _lblActionParamHint;

        private Button _btnAddNode;
        private Button _btnDeleteNode;
        private Button _btnAddResponse;
        private Button _btnDeleteResponse;
        private Button _btnAddAction;
        private Button _btnDeleteAction;
        private Button _btnSave;
        private Button _btnApply;
        private Button _btnCancel;

        private Engine.Dialogue.DialogueNode _currentNode;
        private Response _currentResponse;

        // Событие для уведомления об изменениях
        public event EventHandler ChangesApplied;

        public NewDialogueEditorForm(GameData gameData, Engine.Dialogue.DialogueDocument document = null)
        {
            _gameData = gameData;
            _document = document ?? new Engine.Dialogue.DialogueDocument
            {
                Id = "new_dialogue",
                Name = "Новый диалог",
                Start = "greeting",
                Nodes = new List<Engine.Dialogue.DialogueNode>()
            };

            InitializeComponent();
            LoadDocument();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактор диалогов";
            this.Size = new Size(1000, 700);
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;

            // Создаем основной layout
            var mainPanel = new TableLayoutPanel
            {
                Dock = DockStyle.Fill,
                ColumnCount = 3,
                RowCount = 2
            };

            // Настройка колонок
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Документ и узлы
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40)); // Редактирование узла
            mainPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 30)); // Ответы и действия

            // Настройка строк
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            mainPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 50)); // Кнопки

            // Левая панель - документ и узлы
            var leftPanel = CreateLeftPanel();
            mainPanel.Controls.Add(leftPanel, 0, 0);

            // Средняя панель - редактирование узла
            var centerPanel = CreateCenterPanel();
            mainPanel.Controls.Add(centerPanel, 1, 0);

            // Правая панель - ответы и действия
            var rightPanel = CreateRightPanel();
            mainPanel.Controls.Add(rightPanel, 2, 0);

            // Панель кнопок
            var buttonPanel = CreateButtonPanel();
            mainPanel.Controls.Add(buttonPanel, 0, 1);
            mainPanel.SetColumnSpan(buttonPanel, 3);

            this.Controls.Add(mainPanel);
        }

        private Panel CreateLeftPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(new Label { Text = "Документ диалога", Location = new Point(10, 10), Size = new Size(200, 20) });

            // ID документа
            panel.Controls.Add(new Label { Text = "ID:", Location = new Point(10, 40), Size = new Size(50, 20) });
            _txtDocumentId = new TextBox { Location = new Point(70, 38), Size = new Size(150, 20) };
            panel.Controls.Add(_txtDocumentId);

            // Имя документа
            panel.Controls.Add(new Label { Text = "Имя:", Location = new Point(10, 70), Size = new Size(50, 20) });
            _txtDocumentName = new TextBox { Location = new Point(70, 68), Size = new Size(150, 20) };
            panel.Controls.Add(_txtDocumentName);

            // Стартовый узел
            panel.Controls.Add(new Label { Text = "Старт:", Location = new Point(10, 100), Size = new Size(50, 20) });
            _cmbStartNode = new ComboBox { Location = new Point(70, 98), Size = new Size(150, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbStartNode.SelectedIndexChanged += CmbStartNode_SelectedIndexChanged;
            panel.Controls.Add(_cmbStartNode);

            // Список узлов
            panel.Controls.Add(new Label { Text = "Узлы диалога:", Location = new Point(10, 130), Size = new Size(200, 20) });
            _lstNodes = new ListBox { Location = new Point(10, 150), Size = new Size(250, 200) };
            _lstNodes.SelectedIndexChanged += LstNodes_SelectedIndexChanged;
            panel.Controls.Add(_lstNodes);

            // Кнопки для узлов
            _btnAddNode = new Button { Text = "Добавить узел", Location = new Point(10, 360), Size = new Size(100, 30) };
            _btnAddNode.Click += BtnAddNode_Click;
            panel.Controls.Add(_btnAddNode);

            _btnDeleteNode = new Button { Text = "Удалить узел", Location = new Point(120, 360), Size = new Size(100, 30) };
            _btnDeleteNode.Click += BtnDeleteNode_Click;
            panel.Controls.Add(_btnDeleteNode);

            return panel;
        }

        private Panel CreateCenterPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(new Label { Text = "Редактирование узла", Location = new Point(10, 10), Size = new Size(200, 20) });

            // ID узла
            panel.Controls.Add(new Label { Text = "ID узла:", Location = new Point(10, 40), Size = new Size(80, 20) });
            _txtNodeId = new TextBox { Location = new Point(100, 38), Size = new Size(150, 20) };
            panel.Controls.Add(_txtNodeId);

            // Текст узла
            panel.Controls.Add(new Label { Text = "Текст:", Location = new Point(10, 70), Size = new Size(80, 20) });
            _txtNodeText = new TextBox { Location = new Point(10, 90), Size = new Size(350, 100), Multiline = true, ScrollBars = ScrollBars.Vertical };
            panel.Controls.Add(_txtNodeText);

            // Тип узла
            panel.Controls.Add(new Label { Text = "Тип:", Location = new Point(10, 200), Size = new Size(80, 20) });
            _cmbNodeType = new ComboBox { Location = new Point(100, 198), Size = new Size(150, 20) };
            _cmbNodeType.Items.AddRange(new[] { "greeting", "quest_offer", "quest_progress", "quest_complete", "trade", "default" });
            panel.Controls.Add(_cmbNodeType);

            return panel;
        }

        private Panel CreateRightPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            panel.Controls.Add(new Label { Text = "Ответы и действия", Location = new Point(10, 10), Size = new Size(200, 20) });

            // Список ответов
            panel.Controls.Add(new Label { Text = "Ответы:", Location = new Point(10, 40), Size = new Size(100, 20) });
            _lstResponses = new ListBox { Location = new Point(10, 60), Size = new Size(250, 100) };
            _lstResponses.SelectedIndexChanged += LstResponses_SelectedIndexChanged;
            panel.Controls.Add(_lstResponses);

            // Кнопки для ответов
            _btnAddResponse = new Button { Text = "Добавить ответ", Location = new Point(10, 170), Size = new Size(100, 30) };
            _btnAddResponse.Click += BtnAddResponse_Click;
            panel.Controls.Add(_btnAddResponse);

            _btnDeleteResponse = new Button { Text = "Удалить ответ", Location = new Point(120, 170), Size = new Size(100, 30) };
            _btnDeleteResponse.Click += BtnDeleteResponse_Click;
            panel.Controls.Add(_btnDeleteResponse);

            // Редактирование ответа
            panel.Controls.Add(new Label { Text = "Текст ответа:", Location = new Point(10, 210), Size = new Size(100, 20) });
            _txtResponseText = new TextBox { Location = new Point(10, 230), Size = new Size(250, 20) };
            panel.Controls.Add(_txtResponseText);

            panel.Controls.Add(new Label { Text = "Целевой узел:", Location = new Point(10, 260), Size = new Size(100, 20) });
            _cmbResponseTarget = new ComboBox { Location = new Point(10, 280), Size = new Size(150, 20), DropDownStyle = ComboBoxStyle.DropDown };
            _cmbResponseTarget.SelectedIndexChanged += CmbResponseTarget_SelectedIndexChanged;
            panel.Controls.Add(_cmbResponseTarget);

            panel.Controls.Add(new Label { Text = "Условие:", Location = new Point(10, 300), Size = new Size(100, 20) });
            _cmbResponseCondition = new ComboBox { Location = new Point(10, 320), Size = new Size(250, 20), DropDownStyle = ComboBoxStyle.DropDown };
            _cmbResponseCondition.Items.AddRange(new[] { 
                "", 
                "questAvailableForNPC:5001 - Квест доступен для NPC",
                "questInProgressForNPC:5001 - Квест в процессе у NPC", 
                "questReadyToCompleteForNPC:5001 - Квест готов к завершению",
                "HasItem:1002 - У игрока есть предмет",
                "FlagSet:flag_name - Установлен флаг",
                "QuestActive:5001 - Квест активен",
                "PlayerLevel:5 - Уровень игрока >= 5"
            });
            _cmbResponseCondition.SelectedIndexChanged += CmbResponseCondition_SelectedIndexChanged;
            panel.Controls.Add(_cmbResponseCondition);

            // Подсказка по условиям
            var lblConditionHint = new Label 
            { 
                Text = "Примеры: questAvailableForNPC:5001, HasItem:1002, FlagSet:quest_done", 
                Location = new Point(10, 345), 
                Size = new Size(250, 30), 
                ForeColor = Color.Gray, 
                Font = new Font("Arial", 8)
            };
            panel.Controls.Add(lblConditionHint);

            // Список действий
            panel.Controls.Add(new Label { Text = "Действия:", Location = new Point(10, 380), Size = new Size(100, 20) });
            _lstActions = new ListBox { Location = new Point(10, 400), Size = new Size(250, 80) };
            panel.Controls.Add(_lstActions);

            // Редактирование действий
            _cmbActionType = new ComboBox { Location = new Point(10, 490), Size = new Size(120, 20), DropDownStyle = ComboBoxStyle.DropDownList };
            _cmbActionType.Items.AddRange(new[] { "StartQuest", "CompleteQuest", "StartTrade", "EndDialogue", "GiveGold", "GiveItem", "SetFlag" });
            _cmbActionType.SelectedIndexChanged += CmbActionType_SelectedIndexChanged;
            panel.Controls.Add(_cmbActionType);

            _txtActionParam = new TextBox { Location = new Point(140, 490), Size = new Size(80, 20) };
            panel.Controls.Add(_txtActionParam);

            _lblActionParamHint = new Label { Location = new Point(10, 515), Size = new Size(250, 40), ForeColor = Color.Blue, Font = new Font("Arial", 8) };
            panel.Controls.Add(_lblActionParamHint);

            _btnAddAction = new Button { Text = "Добавить", Location = new Point(10, 560), Size = new Size(60, 25) };
            _btnAddAction.Click += BtnAddAction_Click;
            panel.Controls.Add(_btnAddAction);

            _btnDeleteAction = new Button { Text = "Удалить", Location = new Point(80, 560), Size = new Size(60, 25) };
            _btnDeleteAction.Click += BtnDeleteAction_Click;
            panel.Controls.Add(_btnDeleteAction);

            return panel;
        }

        private Panel CreateButtonPanel()
        {
            var panel = new Panel { Dock = DockStyle.Fill };
            
            // Подсказки по условиям
            var lblConditionsHint = new Label 
            { 
                Text = "Условия: questAvailableForNPC:ID - квест доступен, questInProgressForNPC:ID - квест в процессе, questReadyToCompleteForNPC:ID - готов к завершению", 
                Location = new Point(10, 5), 
                Size = new Size(600, 15), 
                ForeColor = Color.DarkBlue,
                Font = new Font("Arial", 8)
            };
            panel.Controls.Add(lblConditionsHint);
            
            var lblMoreConditionsHint = new Label 
            { 
                Text = "HasItem:ID - есть предмет, FlagSet:имя - установлен флаг, QuestActive:ID - квест активен, PlayerLevel:число - уровень игрока", 
                Location = new Point(10, 20), 
                Size = new Size(600, 15), 
                ForeColor = Color.DarkBlue,
                Font = new Font("Arial", 8)
            };
            panel.Controls.Add(lblMoreConditionsHint);

            _btnSave = new Button { Text = "Сохранить", Location = new Point(700, 5), Size = new Size(80, 30) };
            _btnSave.Click += BtnSave_Click;
            panel.Controls.Add(_btnSave);

            _btnApply = new Button { Text = "Применить", Location = new Point(790, 5), Size = new Size(80, 30) };
            _btnApply.Click += BtnApply_Click;
            panel.Controls.Add(_btnApply);

            _btnCancel = new Button { Text = "Отмена", Location = new Point(880, 5), Size = new Size(80, 30) };
            _btnCancel.Click += BtnCancel_Click;
            panel.Controls.Add(_btnCancel);

            return panel;
        }

        private void LoadDocument()
        {
            _txtDocumentId.Text = _document.Id;
            _txtDocumentName.Text = _document.Name;
            RefreshNodesList();
            RefreshStartNodeComboBox();
            if (!string.IsNullOrEmpty(_document.Start))
            {
                _cmbStartNode.Text = _document.Start;
            }
        }

        private void RefreshNodesList()
        {
            _lstNodes.Items.Clear();
            foreach (var node in _document.Nodes)
            {
                _lstNodes.Items.Add($"{node.Id} - {node.Text?.Substring(0, Math.Min(30, node.Text?.Length ?? 0))}...");
            }
        }

        private void RefreshResponsesList()
        {
            _lstResponses.Items.Clear();
            if (_currentNode?.Responses != null)
            {
                foreach (var response in _currentNode.Responses)
                {
                    _lstResponses.Items.Add($"{response.Text} -> {response.Target}");
                }
            }
        }

        private void RefreshActionsList()
        {
            _lstActions.Items.Clear();
            if (_currentResponse?.Actions != null)
            {
                foreach (var action in _currentResponse.Actions)
                {
                    _lstActions.Items.Add($"{action.Type}({action.Param})");
                }
            }
        }

        private void RefreshStartNodeComboBox()
        {
            _cmbStartNode.Items.Clear();
            foreach (var node in _document.Nodes)
            {
                _cmbStartNode.Items.Add(node.Id);
            }
        }

        private void RefreshResponseTargetComboBox()
        {
            _cmbResponseTarget.Items.Clear();
            _cmbResponseTarget.Items.Add(""); // Пустой вариант для завершения диалога
            foreach (var node in _document.Nodes)
            {
                _cmbResponseTarget.Items.Add(node.Id);
            }
        }

        private void UpdateActionParamHint()
        {
            var actionType = _cmbActionType.Text;
            var hint = actionType switch
            {
                "StartQuest" => "ID квеста (например: 5001)",
                "CompleteQuest" => "ID квеста (например: 5001)",
                "GiveGold" => "Количество золота (например: 100)",
                "GiveItem" => "ID предмета (например: 1002)",
                "SetFlag" => "Имя флага (например: quest_completed)",
                "StartTrade" => "Без параметров",
                "EndDialogue" => "Без параметров",
                _ => "Выберите тип действия"
            };
            _lblActionParamHint.Text = hint;
        }

        private void CmbStartNode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_cmbStartNode.SelectedItem != null)
            {
                _document.Start = _cmbStartNode.SelectedItem.ToString();
            }
        }

        private void CmbResponseTarget_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_currentResponse != null && _cmbResponseTarget.SelectedItem != null)
            {
                _currentResponse.Target = _cmbResponseTarget.SelectedItem.ToString();
            }
        }

        private void CmbResponseCondition_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_currentResponse != null && _cmbResponseCondition.SelectedItem != null)
            {
                var selectedText = _cmbResponseCondition.SelectedItem.ToString();
                // Извлекаем только часть до дефиса (само условие)
                var condition = selectedText.Contains(" - ") ? selectedText.Split(" - ")[0] : selectedText;
                _currentResponse.Condition = condition;
            }
        }

        private void CmbActionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateActionParamHint();
        }

        // Обработчики событий
        private void LstNodes_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_lstNodes.SelectedIndex >= 0 && _lstNodes.SelectedIndex < _document.Nodes.Count)
            {
                _currentNode = _document.Nodes[_lstNodes.SelectedIndex];
                LoadNodeData();
            }
        }

        private void LstResponses_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (_currentNode?.Responses != null && _lstResponses.SelectedIndex >= 0 && _lstResponses.SelectedIndex < _currentNode.Responses.Count)
            {
                _currentResponse = _currentNode.Responses[_lstResponses.SelectedIndex];
                LoadResponseData();
            }
        }

        private void LoadNodeData()
        {
            if (_currentNode != null)
            {
                _txtNodeId.Text = _currentNode.Id;
                _txtNodeText.Text = _currentNode.Text;
                _cmbNodeType.Text = _currentNode.Type;
                RefreshResponsesList();
                RefreshResponseTargetComboBox();
            }
        }

        private void LoadResponseData()
        {
            if (_currentResponse != null)
            {
                _txtResponseText.Text = _currentResponse.Text;
                _cmbResponseTarget.Text = _currentResponse.Target;
                
                // Находим соответствующий элемент в ComboBox с комментарием
                var conditionText = _currentResponse.Condition;
                var matchingItem = _cmbResponseCondition.Items.Cast<string>()
                    .FirstOrDefault(item => item.StartsWith(conditionText + " - ") || item == conditionText);
                
                if (matchingItem != null)
                {
                    _cmbResponseCondition.Text = matchingItem;
                }
                else
                {
                    _cmbResponseCondition.Text = conditionText;
                }
                
                RefreshActionsList();
            }
        }

        private void BtnAddNode_Click(object sender, EventArgs e)
        {
            var newNode = new Engine.Dialogue.DialogueNode
            {
                Id = $"node_{_document.Nodes.Count + 1}",
                Text = "Новый узел",
                Type = "default",
                Responses = new List<Response>()
            };
            _document.Nodes.Add(newNode);
            RefreshNodesList();
            RefreshStartNodeComboBox();
            RefreshResponseTargetComboBox();
            _lstNodes.SelectedIndex = _lstNodes.Items.Count - 1;
        }

        private void BtnDeleteNode_Click(object sender, EventArgs e)
        {
            if (_lstNodes.SelectedIndex >= 0)
            {
                _document.Nodes.RemoveAt(_lstNodes.SelectedIndex);
                RefreshNodesList();
                RefreshStartNodeComboBox();
                RefreshResponseTargetComboBox();
            }
        }

        private void BtnAddResponse_Click(object sender, EventArgs e)
        {
            if (_currentNode != null)
            {
                var newResponse = new Response
                {
                    Text = "Новый ответ",
                    Target = "",
                    Condition = "",
                    Actions = new List<Engine.Dialogue.DialogueAction>()
                };
                _currentNode.Responses.Add(newResponse);
                RefreshResponsesList();
                _lstResponses.SelectedIndex = _lstResponses.Items.Count - 1;
            }
        }

        private void BtnDeleteResponse_Click(object sender, EventArgs e)
        {
            if (_currentNode?.Responses != null && _lstResponses.SelectedIndex >= 0)
            {
                _currentNode.Responses.RemoveAt(_lstResponses.SelectedIndex);
                RefreshResponsesList();
            }
        }

        private void BtnAddAction_Click(object sender, EventArgs e)
        {
            if (_currentResponse != null && !string.IsNullOrEmpty(_cmbActionType.Text))
            {
                var newAction = new Engine.Dialogue.DialogueAction
                {
                    Type = _cmbActionType.Text,
                    Param = _txtActionParam.Text
                };
                _currentResponse.Actions.Add(newAction);
                RefreshActionsList();
            }
        }

        private void BtnDeleteAction_Click(object sender, EventArgs e)
        {
            if (_currentResponse?.Actions != null && _lstActions.SelectedIndex >= 0)
            {
                _currentResponse.Actions.RemoveAt(_lstActions.SelectedIndex);
                RefreshActionsList();
            }
        }

        private void BtnSave_Click(object sender, EventArgs e)
        {
            // Сохраняем изменения и закрываем форму
            SaveChanges();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnApply_Click(object sender, EventArgs e)
        {
            // Сохраняем изменения, но не закрываем форму
            SaveChanges();
            MessageBox.Show("Изменения применены!", "Применение", MessageBoxButtons.OK, MessageBoxIcon.Information);
            
            // Уведомляем о применении изменений
            ChangesApplied?.Invoke(this, EventArgs.Empty);
        }

        private void SaveChanges()
        {
            // Сохраняем изменения
            _document.Id = _txtDocumentId.Text;
            _document.Name = _txtDocumentName.Text;
            _document.Start = _cmbStartNode.Text;

            if (_currentNode != null)
            {
                _currentNode.Id = _txtNodeId.Text;
                _currentNode.Text = _txtNodeText.Text;
                _currentNode.Type = _cmbNodeType.Text;
            }

            if (_currentResponse != null)
            {
                _currentResponse.Text = _txtResponseText.Text;
                _currentResponse.Target = _cmbResponseTarget.Text;
                
                // Извлекаем только условие без комментария
                var conditionText = _cmbResponseCondition.Text;
                var condition = conditionText.Contains(" - ") ? conditionText.Split(" - ")[0] : conditionText;
                _currentResponse.Condition = condition;
            }
        }

        private void BtnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        public Engine.Dialogue.DialogueDocument GetDocument()
        {
            return _document;
        }
    }
}
