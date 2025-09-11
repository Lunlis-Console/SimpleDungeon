using System;
using System.Windows.Forms;
using Engine.Data;
using System.Collections.Generic;
using System.Linq;

namespace JsonEditor
{
    using System;
    using System.Windows.Forms;
    using Engine.Data;
    using System.Collections.Generic;
    using System.Linq;

    namespace JsonEditor
    {
        public class EditResponseForm : Form
        {
            private DialogueResponseData _response;
            private List<DialogueNodeData> _allNodes;
            private GameData _gameData;
            private string _currentNodeId;

            private TextBox txtText;
            private ComboBox cmbTargetNodeId;
            private CheckBox chkEndDialogue;
            private CheckBox chkStartTrade;
            private ComboBox cmbAction;
            private TextBox txtActionParameter;
            private NumericUpDown nudGoldReward;
            private NumericUpDown nudExperienceReward;
            private ComboBox cmbQuestId;
            private DataGridView gridItemRewards;
            private Button btnAddItemReward;
            private Button btnEditItemReward;
            private Button btnRemoveItemReward;
            private Button btnOk;
            private Button btnCancel;

            public DialogueResponseData Response => _response;

            public EditResponseForm(DialogueResponseData response, GameData gameData, List<DialogueNodeData> allNodes, string currentNodeId)
            {
                _response = response ?? new DialogueResponseData();
                _gameData = gameData;
                _allNodes = allNodes;
                _currentNodeId = currentNodeId;

                InitializeComponents();
                LoadDataToControls();
            }

            private void InitializeComponents()
            {
                this.Text = "Редактирование ответа";
                this.Width = 600;
                this.Height = 650;
                this.StartPosition = FormStartPosition.CenterParent;

                // Основные элементы
                var lblText = new Label { Text = "Текст ответа:", Left = 10, Top = 15, Width = 120 };
                txtText = new TextBox { Left = 140, Top = 12, Width = 430, Height = 60, Multiline = true };

                var lblTarget = new Label { Text = "Целевой узел:", Left = 10, Top = 85, Width = 120 };
                cmbTargetNodeId = new ComboBox
                {
                    Left = 140,
                    Top = 82,
                    Width = 430,
                    DropDownStyle = ComboBoxStyle.DropDown,
                    AutoCompleteMode = AutoCompleteMode.SuggestAppend
                };

                // Флаги
                chkEndDialogue = new CheckBox { Text = "Завершить диалог", Left = 10, Top = 120, Width = 150 };
                chkStartTrade = new CheckBox { Text = "Начать торговлю", Left = 170, Top = 120, Width = 150 };

                // Действия
                var lblAction = new Label { Text = "Действие:", Left = 10, Top = 155, Width = 120 };
                cmbAction = new ComboBox
                {
                    Left = 140,
                    Top = 152,
                    Width = 200,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };
                cmbAction.Items.AddRange(Enum.GetValues(typeof(DialogueAction)).Cast<object>().ToArray());

                var lblActionParam = new Label { Text = "Параметр:", Left = 10, Top = 185, Width = 120 };
                txtActionParameter = new TextBox { Left = 140, Top = 182, Width = 430 };

                // Награды
                var lblGold = new Label { Text = "Золото:", Left = 10, Top = 215, Width = 120 };
                nudGoldReward = new NumericUpDown { Left = 140, Top = 212, Width = 100, Minimum = 0, Maximum = 1000000 };

                var lblExp = new Label { Text = "Опыт:", Left = 250, Top = 215, Width = 120 };
                nudExperienceReward = new NumericUpDown { Left = 300, Top = 212, Width = 100, Minimum = 0, Maximum = 1000000 };

                var lblQuest = new Label { Text = "Квест:", Left = 10, Top = 245, Width = 120 };
                cmbQuestId = new ComboBox
                {
                    Left = 140,
                    Top = 242,
                    Width = 430,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                // Предметы
                var lblItems = new Label { Text = "Предметы:", Left = 10, Top = 275, Width = 120 };
                gridItemRewards = new DataGridView
                {
                    Left = 10,
                    Top = 300,
                    Width = 560,
                    Height = 150,
                    AllowUserToAddRows = false,
                    AllowUserToDeleteRows = false,
                    ReadOnly = true,
                    SelectionMode = DataGridViewSelectionMode.FullRowSelect
                };
                gridItemRewards.Columns.Add("ItemId", "ID предмета");
                gridItemRewards.Columns.Add("ItemName", "Название");
                gridItemRewards.Columns.Add("Quantity", "Количество");

                btnAddItemReward = new Button { Text = "Добавить", Left = 10, Top = 460, Width = 80 };
                btnEditItemReward = new Button { Text = "Редактировать", Left = 100, Top = 460, Width = 100 };
                btnRemoveItemReward = new Button { Text = "Удалить", Left = 210, Top = 460, Width = 80 };

                // Кнопки
                btnOk = new Button { Text = "OK", Left = 400, Top = 500, Width = 80, DialogResult = DialogResult.OK };
                btnCancel = new Button { Text = "Отмена", Left = 490, Top = 500, Width = 80, DialogResult = DialogResult.Cancel };

                // События
                btnAddItemReward.Click += BtnAddItemReward_Click;
                btnEditItemReward.Click += BtnEditItemReward_Click;
                btnRemoveItemReward.Click += BtnRemoveItemReward_Click;
                btnOk.Click += BtnOk_Click;
                chkEndDialogue.CheckedChanged += ChkEndDialogue_CheckedChanged;
                cmbAction.SelectedIndexChanged += CmbAction_SelectedIndexChanged;

                // Добавление элементов
                this.Controls.AddRange(new Control[]
                {
                lblText, txtText,
                lblTarget, cmbTargetNodeId,
                chkEndDialogue, chkStartTrade,
                lblAction, cmbAction,
                lblActionParam, txtActionParameter,
                lblGold, nudGoldReward,
                lblExp, nudExperienceReward,
                lblQuest, cmbQuestId,
                lblItems, gridItemRewards,
                btnAddItemReward, btnEditItemReward, btnRemoveItemReward,
                btnOk, btnCancel
                });
            }

            private void LoadDataToControls()
            {
                txtText.Text = _response.Text;

                // Заполнение целевых узлов
                cmbTargetNodeId.Items.Clear();
                cmbTargetNodeId.Items.Add(""); // Пустой элемент

                var availableNodes = _allNodes
                    .Where(n => n.Id != _currentNodeId)
                    .Select(n => new { Id = n.Id, Display = $"{n.Id}: {TruncateText(n.Text, 30)}" })
                    .ToList();

                foreach (var node in availableNodes)
                {
                    cmbTargetNodeId.Items.Add(node);
                }

                if (!string.IsNullOrEmpty(_response.TargetNodeId))
                {
                    var selectedItem = availableNodes.FirstOrDefault(n => n.Id == _response.TargetNodeId);
                    if (selectedItem != null)
                        cmbTargetNodeId.SelectedItem = selectedItem;
                    else
                        cmbTargetNodeId.Text = _response.TargetNodeId;
                }

                // Флаги
                chkEndDialogue.Checked = _response.EndDialogue;
                chkStartTrade.Checked = _response.StartTrade;

                // Действия
                cmbAction.SelectedItem = _response.Action;
                txtActionParameter.Text = _response.ActionParameter;

                // Награды
                nudGoldReward.Value = _response.GoldReward;
                nudExperienceReward.Value = _response.ExperienceReward;

                // Квесты
                cmbQuestId.Items.Clear();
                cmbQuestId.Items.Add(""); // Пустой элемент
                if (_gameData?.Quests != null)
                {
                    foreach (var quest in _gameData.Quests)
                    {
                        cmbQuestId.Items.Add(new { Id = quest.ID.ToString(), Display = $"{quest.ID}: {quest.Name}" });
                    }
                }

                if (!string.IsNullOrEmpty(_response.QuestId))
                {
                    var selectedQuest = cmbQuestId.Items.Cast<object>()
                        .FirstOrDefault(item => item.ToString().Contains(_response.QuestId));
                    if (selectedQuest != null)
                        cmbQuestId.SelectedItem = selectedQuest;
                }

                // Предметы
                RefreshItemRewardsGrid();

                UpdateControlsState();
            }

            private void RefreshItemRewardsGrid()
            {
                gridItemRewards.Rows.Clear();
                foreach (var reward in _response.ItemRewards)
                {
                    var itemName = _gameData?.Items?.FirstOrDefault(i => i.ID == reward.ItemId)?.Name ?? "Неизвестный предмет";
                    gridItemRewards.Rows.Add(reward.ItemId, itemName, reward.Quantity);
                }
            }

            private void UpdateControlsState()
            {
                bool hasTarget = !string.IsNullOrEmpty(_response.TargetNodeId);
                bool endDialogue = chkEndDialogue.Checked;

                cmbTargetNodeId.Enabled = !endDialogue;
                cmbAction.Enabled = !hasTarget && !endDialogue;
                txtActionParameter.Enabled = !hasTarget && !endDialogue;
                nudGoldReward.Enabled = !hasTarget && !endDialogue;
                nudExperienceReward.Enabled = !hasTarget && !endDialogue;
                cmbQuestId.Enabled = !hasTarget && !endDialogue;
                btnAddItemReward.Enabled = !hasTarget && !endDialogue;
                btnEditItemReward.Enabled = !hasTarget && !endDialogue;
                btnRemoveItemReward.Enabled = !hasTarget && !endDialogue;
            }

            private void ChkEndDialogue_CheckedChanged(object sender, EventArgs e)
            {
                UpdateControlsState();
            }

            private void CmbAction_SelectedIndexChanged(object sender, EventArgs e)
            {
                var action = (DialogueAction)cmbAction.SelectedItem;
                txtActionParameter.Enabled = action == DialogueAction.GiveQuest || action == DialogueAction.CompleteQuest;
            }

            private void BtnAddItemReward_Click(object sender, EventArgs e)
            {
                using (var form = new EditItemRewardForm(null, _gameData))
                {
                    if (form.ShowDialog() == DialogResult.OK)
                    {
                        _response.ItemRewards.Add(form.ItemReward);
                        RefreshItemRewardsGrid();
                    }
                }
            }

            private void BtnEditItemReward_Click(object sender, EventArgs e)
            {
                if (gridItemRewards.SelectedRows.Count > 0)
                {
                    int index = gridItemRewards.SelectedRows[0].Index;
                    if (index < _response.ItemRewards.Count)
                    {
                        var reward = _response.ItemRewards[index];
                        using (var form = new EditItemRewardForm(reward, _gameData))
                        {
                            if (form.ShowDialog() == DialogResult.OK)
                            {
                                RefreshItemRewardsGrid();
                            }
                        }
                    }
                }
            }

            private void BtnRemoveItemReward_Click(object sender, EventArgs e)
            {
                if (gridItemRewards.SelectedRows.Count > 0)
                {
                    int index = gridItemRewards.SelectedRows[0].Index;
                    if (index < _response.ItemRewards.Count)
                    {
                        _response.ItemRewards.RemoveAt(index);
                        RefreshItemRewardsGrid();
                    }
                }
            }

            private void BtnOk_Click(object sender, EventArgs e)
            {
                if (string.IsNullOrWhiteSpace(txtText.Text))
                {
                    MessageBox.Show("Текст ответа не может быть пустым", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                _response.Text = txtText.Text.Trim();
                _response.TargetNodeId = chkEndDialogue.Checked ? null : (cmbTargetNodeId.SelectedItem?.ToString() ?? cmbTargetNodeId.Text);
                _response.EndDialogue = chkEndDialogue.Checked;
                _response.StartTrade = chkStartTrade.Checked;
                _response.Action = (DialogueAction)cmbAction.SelectedItem;
                _response.ActionParameter = txtActionParameter.Text.Trim();
                _response.GoldReward = (int)nudGoldReward.Value;
                _response.ExperienceReward = (int)nudExperienceReward.Value;
                _response.QuestId = cmbQuestId.SelectedItem?.ToString();

                DialogResult = DialogResult.OK;
                Close();
            }

            private string TruncateText(string text, int maxLength)
            {
                if (string.IsNullOrEmpty(text)) return "(без текста)";
                return text.Length <= maxLength ? text : text.Substring(0, maxLength) + "...";
            }
        }

        public class EditItemRewardForm : Form
        {
            private ItemReward _itemReward;
            private GameData _gameData;

            private ComboBox cmbItem;
            private NumericUpDown nudQuantity;
            private Button btnOk;
            private Button btnCancel;

            public ItemReward ItemReward => _itemReward;

            public EditItemRewardForm(ItemReward itemReward, GameData gameData)
            {
                _itemReward = itemReward ?? new ItemReward();
                _gameData = gameData;

                InitializeComponents();
                LoadData();
            }

            private void InitializeComponents()
            {
                this.Text = "Добавление предмета";
                this.Width = 300;
                this.Height = 150;
                this.StartPosition = FormStartPosition.CenterParent;

                var lblItem = new Label { Text = "Предмет:", Left = 10, Top = 15, Width = 80 };
                cmbItem = new ComboBox
                {
                    Left = 100,
                    Top = 12,
                    Width = 180,
                    DropDownStyle = ComboBoxStyle.DropDownList
                };

                var lblQuantity = new Label { Text = "Количество:", Left = 10, Top = 45, Width = 80 };
                nudQuantity = new NumericUpDown
                {
                    Left = 100,
                    Top = 42,
                    Width = 100,
                    Minimum = 1,
                    Maximum = 1000
                };

                btnOk = new Button { Text = "OK", Left = 120, Top = 80, Width = 80, DialogResult = DialogResult.OK };
                btnCancel = new Button { Text = "Отмена", Left = 210, Top = 80, Width = 80, DialogResult = DialogResult.Cancel };

                btnOk.Click += BtnOk_Click;

                this.Controls.AddRange(new Control[]
                {
                lblItem, cmbItem,
                lblQuantity, nudQuantity,
                btnOk, btnCancel
                });
            }

            private void LoadData()
            {
                // Заполнение списка предметов
                cmbItem.Items.Clear();
                if (_gameData?.Items != null)
                {
                    foreach (var item in _gameData.Items.OrderBy(i => i.Name))
                    {
                        cmbItem.Items.Add(new { Id = item.ID, Name = $"{item.Name} (ID: {item.ID})" });
                    }
                }

                // Установка текущих значений
                if (_itemReward.ItemId > 0)
                {
                    var selectedItem = cmbItem.Items.Cast<object>()
                        .FirstOrDefault(item => ((dynamic)item).Id == _itemReward.ItemId);
                    if (selectedItem != null)
                        cmbItem.SelectedItem = selectedItem;
                }
                else if (cmbItem.Items.Count > 0)
                {
                    cmbItem.SelectedIndex = 0;
                }

                nudQuantity.Value = _itemReward.Quantity > 0 ? _itemReward.Quantity : 1;
            }

            private void BtnOk_Click(object sender, EventArgs e)
            {
                if (cmbItem.SelectedItem == null)
                {
                    MessageBox.Show("Выберите предмет", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    DialogResult = DialogResult.None;
                    return;
                }

                dynamic selectedItem = cmbItem.SelectedItem;
                _itemReward.ItemId = selectedItem.Id;
                _itemReward.Quantity = (int)nudQuantity.Value;

                DialogResult = DialogResult.OK;
                Close();
            }
        }
    }
}