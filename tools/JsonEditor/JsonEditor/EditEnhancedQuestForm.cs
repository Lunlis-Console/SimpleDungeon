using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;
using Engine.Quests;
using Newtonsoft.Json;

namespace JsonEditor
{
    public class EditEnhancedQuestForm : Form
    {
        private EnhancedQuest _quest;
        private GameData _gameData;

        // Основные элементы управления
        private TextBox txtId;
        private TextBox txtName;
        private TextBox txtDescription;
        private TextBox txtDetailedDescription;
        private ComboBox cbQuestGiver;

        // Условия квеста
        private ListBox lstConditions;
        private Button btnAddCondition;
        private Button btnEditCondition;
        private Button btnRemoveCondition;

        // Награды
        private NumericUpDown nudRewardGold;
        private NumericUpDown nudRewardEXP;
        private ListBox lstRewardItems;
        private Button btnAddRewardItem;
        private Button btnRemoveRewardItem;

        // Диалоги
        private TextBox txtOfferNode;
        private TextBox txtInProgressNode;
        private TextBox txtReadyToCompleteNode;
        private TextBox txtCompletedNode;

        // Предварительные условия
        private ListBox lstPrerequisites;
        private Button btnAddPrerequisite;
        private Button btnRemovePrerequisite;

        private Button btnOk;
        private Button btnCancel;

        private BindingList<QuestConditionData> _conditionsBinding;
        private BindingList<QuestRewardItem> _rewardItemsBinding;
        private BindingList<int> _prerequisitesBinding;

        public EditEnhancedQuestForm(GameData gameData, EnhancedQuest quest = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _quest = quest ?? new EnhancedQuest();

            InitializeComponent();
            LoadQuestData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование расширенного квеста";
            this.Width = 800;
            this.Height = 700;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            var tabControl = new TabControl
            {
                Dock = DockStyle.Fill
            };

            // Вкладка "Основное"
            var tabBasic = new TabPage("Основное");
            CreateBasicTab(tabBasic);
            tabControl.TabPages.Add(tabBasic);

            // Вкладка "Условия"
            var tabConditions = new TabPage("Условия");
            CreateConditionsTab(tabConditions);
            tabControl.TabPages.Add(tabConditions);

            // Вкладка "Награды"
            var tabRewards = new TabPage("Награды");
            CreateRewardsTab(tabRewards);
            tabControl.TabPages.Add(tabRewards);

            // Вкладка "Диалоги"
            var tabDialogue = new TabPage("Диалоги");
            CreateDialogueTab(tabDialogue);
            tabControl.TabPages.Add(tabDialogue);

            // Вкладка "Предварительные условия"
            var tabPrerequisites = new TabPage("Предварительные условия");
            CreatePrerequisitesTab(tabPrerequisites);
            tabControl.TabPages.Add(tabPrerequisites);

            this.Controls.Add(tabControl);

            // Кнопки OK/Cancel
            var panelButtons = new Panel
            {
                Dock = DockStyle.Bottom,
                Height = 50
            };

            btnOk = new Button
            {
                Text = "OK",
                Left = 200,
                Top = 10,
                Width = 80,
                DialogResult = DialogResult.OK
            };
            btnCancel = new Button
            {
                Text = "Отмена",
                Left = 290,
                Top = 10,
                Width = 80,
                DialogResult = DialogResult.Cancel
            };

            btnOk.Click += BtnOk_Click;

            panelButtons.Controls.AddRange(new Control[] { btnOk, btnCancel });
            this.Controls.Add(panelButtons);
        }

        private void CreateBasicTab(TabPage tab)
        {
            int leftLabel = 12;
            int leftControl = 150;
            int top = 12;
            int vertGap = 30;

            // ID квеста
            var lblId = new Label { Text = "ID:", Left = leftLabel, Top = top + 4, Width = 130 };
            txtId = new TextBox { Left = leftControl, Top = top, Width = 200 };
            top += vertGap;

            // Название квеста
            var lblName = new Label { Text = "Название:", Left = leftLabel, Top = top + 4, Width = 130 };
            txtName = new TextBox { Left = leftControl, Top = top, Width = 400 };
            top += vertGap;

            // Описание квеста
            var lblDescription = new Label { Text = "Описание:", Left = leftLabel, Top = top + 4, Width = 130 };
            txtDescription = new TextBox
            {
                Left = leftControl,
                Top = top,
                Width = 400,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            top += 70;

            // Подробное описание
            var lblDetailedDescription = new Label { Text = "Подробное описание:", Left = leftLabel, Top = top + 4, Width = 130 };
            txtDetailedDescription = new TextBox
            {
                Left = leftControl,
                Top = top,
                Width = 400,
                Height = 60,
                Multiline = true,
                ScrollBars = ScrollBars.Vertical
            };
            top += 70;

            // Квестодатель
            var lblQuestGiver = new Label { Text = "Квестодатель:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbQuestGiver = new ComboBox { Left = leftControl, Top = top, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };
            top += vertGap;

            tab.Controls.AddRange(new Control[]
            {
                lblId, txtId,
                lblName, txtName,
                lblDescription, txtDescription,
                lblDetailedDescription, txtDetailedDescription,
                lblQuestGiver, cbQuestGiver
            });
        }

        private void CreateConditionsTab(TabPage tab)
        {
            var lblConditions = new Label { Text = "Условия квеста:", Left = 12, Top = 12, Width = 100 };
            lstConditions = new ListBox
            {
                Left = 12,
                Top = 35,
                Width = 500,
                Height = 200
            };

            btnAddCondition = new Button { Text = "Добавить условие", Left = 12, Top = 245, Width = 120 };
            btnEditCondition = new Button { Text = "Редактировать", Left = 140, Top = 245, Width = 120 };
            btnRemoveCondition = new Button { Text = "Удалить", Left = 268, Top = 245, Width = 120 };

            btnAddCondition.Click += BtnAddCondition_Click;
            btnEditCondition.Click += BtnEditCondition_Click;
            btnRemoveCondition.Click += BtnRemoveCondition_Click;

            tab.Controls.AddRange(new Control[]
            {
                lblConditions, lstConditions,
                btnAddCondition, btnEditCondition, btnRemoveCondition
            });
        }

        private void CreateRewardsTab(TabPage tab)
        {
            int leftLabel = 12;
            int leftControl = 150;
            int top = 12;
            int vertGap = 30;

            // Награда золотом
            var lblRewardGold = new Label { Text = "Награда (золото):", Left = leftLabel, Top = top + 4, Width = 130 };
            nudRewardGold = new NumericUpDown { Left = leftControl, Top = top, Width = 120, Minimum = 0, Maximum = 1000000 };
            top += vertGap;

            // Награда опытом
            var lblRewardEXP = new Label { Text = "Награда (опыт):", Left = leftLabel, Top = top + 4, Width = 130 };
            nudRewardEXP = new NumericUpDown { Left = leftControl, Top = top, Width = 120, Minimum = 0, Maximum = 1000000 };
            top += vertGap + 10;

            // Предметы-награды
            var lblRewardItems = new Label { Text = "Предметы-награды:", Left = leftLabel, Top = top + 4, Width = 130 };
            lstRewardItems = new ListBox
            {
                Left = leftLabel,
                Top = top + 25,
                Width = 400,
                Height = 150
            };
            top += 185;

            btnAddRewardItem = new Button { Text = "Добавить предмет", Left = leftLabel, Top = top, Width = 120 };
            btnRemoveRewardItem = new Button { Text = "Удалить", Left = leftLabel + 130, Top = top, Width = 120 };

            btnAddRewardItem.Click += BtnAddRewardItem_Click;
            btnRemoveRewardItem.Click += BtnRemoveRewardItem_Click;

            tab.Controls.AddRange(new Control[]
            {
                lblRewardGold, nudRewardGold,
                lblRewardEXP, nudRewardEXP,
                lblRewardItems, lstRewardItems,
                btnAddRewardItem, btnRemoveRewardItem
            });
        }

        private void CreateDialogueTab(TabPage tab)
        {
            int leftLabel = 12;
            int leftControl = 200;
            int top = 12;
            int vertGap = 30;

            // Добавляем подсказку
            var lblHint = new Label 
            { 
                Text = "ID узлов диалогов для разных состояний квеста. Эти узлы будут созданы автоматически QuestDialogueManager.", 
                Left = leftLabel, 
                Top = top, 
                Width = 500, 
                Height = 40,
                ForeColor = Color.Blue
            };
            top += 50;

            var lblOfferNode = new Label { Text = "Узел предложения квеста:", Left = leftLabel, Top = top + 4, Width = 180 };
            txtOfferNode = new TextBox { Left = leftControl, Top = top, Width = 200 };
            top += vertGap;

            var lblInProgressNode = new Label { Text = "Узел квеста в процессе:", Left = leftLabel, Top = top + 4, Width = 180 };
            txtInProgressNode = new TextBox { Left = leftControl, Top = top, Width = 200 };
            top += vertGap;

            var lblReadyToCompleteNode = new Label { Text = "Узел готовности к завершению:", Left = leftLabel, Top = top + 4, Width = 180 };
            txtReadyToCompleteNode = new TextBox { Left = leftControl, Top = top, Width = 200 };
            top += vertGap;

            var lblCompletedNode = new Label { Text = "Узел завершенного квеста:", Left = leftLabel, Top = top + 4, Width = 180 };
            txtCompletedNode = new TextBox { Left = leftControl, Top = top, Width = 200 };
            top += vertGap;

            // Добавляем примеры
            var lblExamples = new Label 
            { 
                Text = "Примеры ID: quest_offer_5001, quest_progress_5001, quest_complete_5001, quest_done_5001", 
                Left = leftLabel, 
                Top = top, 
                Width = 500, 
                Height = 20,
                ForeColor = Color.Gray
            };

            tab.Controls.AddRange(new Control[]
            {
                lblHint,
                lblOfferNode, txtOfferNode,
                lblInProgressNode, txtInProgressNode,
                lblReadyToCompleteNode, txtReadyToCompleteNode,
                lblCompletedNode, txtCompletedNode,
                lblExamples
            });
        }

        private void CreatePrerequisitesTab(TabPage tab)
        {
            var lblPrerequisites = new Label { Text = "Предварительные условия:", Left = 12, Top = 12, Width = 150 };
            lstPrerequisites = new ListBox
            {
                Left = 12,
                Top = 35,
                Width = 400,
                Height = 200
            };

            btnAddPrerequisite = new Button { Text = "Добавить условие", Left = 12, Top = 245, Width = 120 };
            btnRemovePrerequisite = new Button { Text = "Удалить", Left = 140, Top = 245, Width = 120 };

            btnAddPrerequisite.Click += BtnAddPrerequisite_Click;
            btnRemovePrerequisite.Click += BtnRemovePrerequisite_Click;

            tab.Controls.AddRange(new Control[]
            {
                lblPrerequisites, lstPrerequisites,
                btnAddPrerequisite, btnRemovePrerequisite
            });
        }

        private void LoadQuestData()
        {
            txtId.Text = _quest.ID.ToString();
            txtName.Text = _quest.Name ?? "";
            txtDescription.Text = _quest.Description ?? "";
            txtDetailedDescription.Text = _quest.DetailedDescription ?? "";
            nudRewardGold.Value = _quest.Rewards?.Gold ?? 0;
            nudRewardEXP.Value = _quest.Rewards?.Experience ?? 0;

            // Заполнение списка NPC
            if (_gameData.NPCs != null)
            {
                foreach (var npc in _gameData.NPCs.OrderBy(n => n.Name))
                {
                    cbQuestGiver.Items.Add(new NPCComboItem(npc));
                }

                var selectedNPC = cbQuestGiver.Items.Cast<NPCComboItem>()
                    .FirstOrDefault(n => n.NPCData.ID == _quest.QuestGiverID);

                if (selectedNPC != null)
                {
                    cbQuestGiver.SelectedItem = selectedNPC;
                }
            }

            // Инициализация условий
            _conditionsBinding = new BindingList<QuestConditionData>(_quest.Conditions ?? new List<QuestConditionData>());
            lstConditions.DataSource = _conditionsBinding;
            lstConditions.DisplayMember = "Description";

            // Инициализация предметов-наград
            _rewardItemsBinding = new BindingList<QuestRewardItem>(_quest.Rewards?.Items ?? new List<QuestRewardItem>());
            lstRewardItems.DataSource = _rewardItemsBinding;
            lstRewardItems.DisplayMember = "ItemDetails.Name";

            // Инициализация предварительных условий
            _prerequisitesBinding = new BindingList<int>(_quest.PrerequisiteQuestIDs ?? new List<int>());
            lstPrerequisites.DataSource = _prerequisitesBinding;

            // Загрузка узлов диалогов
            if (_quest.DialogueNodes != null)
            {
                txtOfferNode.Text = _quest.DialogueNodes.Offer ?? "";
                txtInProgressNode.Text = _quest.DialogueNodes.InProgress ?? "";
                txtReadyToCompleteNode.Text = _quest.DialogueNodes.ReadyToComplete ?? "";
                txtCompletedNode.Text = _quest.DialogueNodes.Completed ?? "";
            }
        }

        private void BtnAddCondition_Click(object sender, EventArgs e)
        {
            using (var form = new EditQuestConditionForm(_gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    // Преобразуем QuestCondition в QuestConditionData
                    var conditionData = ConvertToData(form.QuestCondition);
                    _conditionsBinding.Add(conditionData);
                }
            }
        }

        private void BtnEditCondition_Click(object sender, EventArgs e)
        {
            if (lstConditions.SelectedItem is QuestConditionData selectedCondition)
            {
                // Преобразуем QuestConditionData в QuestCondition для редактирования
                var condition = selectedCondition.ToQuestCondition();

                using (var form = new EditQuestConditionForm(_gameData, condition))
                {
                    if (form.ShowDialog(this) == DialogResult.OK)
                    {
                        // Преобразуем обратно в QuestConditionData
                        var updatedData = ConvertToData(form.QuestCondition);
                        int index = _conditionsBinding.IndexOf(selectedCondition);
                        _conditionsBinding.RemoveAt(index);
                        _conditionsBinding.Insert(index, updatedData);
                    }
                }
            }
            else
            {
                MessageBox.Show("Выберите условие для редактирования.");
            }
        }

        private void BtnRemoveCondition_Click(object sender, EventArgs e)
        {
            if (lstConditions.SelectedItem is QuestConditionData selectedCondition)
            {
                if (MessageBox.Show("Удалить это условие?", "Подтверждение",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _conditionsBinding.Remove(selectedCondition);
                }
            }
            else
            {
                MessageBox.Show("Выберите условие для удаления.");
            }
        }

        private void BtnAddRewardItem_Click(object sender, EventArgs e)
        {
            var newRewardItem = new QuestRewardItem { ItemID = 1, Quantity = 1 };

            using (var form = new EditRewardItemForm(newRewardItem, _gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _rewardItemsBinding.Add(form.RewardItem);
                }
            }
        }

        private void BtnRemoveRewardItem_Click(object sender, EventArgs e)
        {
            if (lstRewardItems.SelectedItem is QuestRewardItem selectedItem)
            {
                if (MessageBox.Show("Удалить этот предмет-награду?", "Подтверждение",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _rewardItemsBinding.Remove(selectedItem);
                }
            }
            else
            {
                MessageBox.Show("Выберите предмет-награду для удаления.");
            }
        }

        private void BtnAddPrerequisite_Click(object sender, EventArgs e)
        {
            using (var form = new SelectQuestForm(_gameData))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _prerequisitesBinding.Add(form.SelectedQuestID);
                }
            }
        }

        private void BtnRemovePrerequisite_Click(object sender, EventArgs e)
        {
            if (lstPrerequisites.SelectedItem is int selectedQuestID)
            {
                if (MessageBox.Show("Удалить это предварительное условие?", "Подтверждение",
                    MessageBoxButtons.YesNo) == DialogResult.Yes)
                {
                    _prerequisitesBinding.Remove(selectedQuestID);
                }
            }
            else
            {
                MessageBox.Show("Выберите предварительное условие для удаления.");
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtId.Text) || !int.TryParse(txtId.Text, out int id))
            {
                MessageBox.Show("ID должен быть числом.");
                return;
            }

            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show("Введите название квеста.");
                return;
            }

            if (cbQuestGiver.SelectedItem == null)
            {
                MessageBox.Show("Выберите квестодателя.");
                return;
            }

            // Сохранение данных
            _quest.ID = id;
            _quest.Name = txtName.Text.Trim();
            _quest.Description = txtDescription.Text.Trim();
            _quest.DetailedDescription = txtDetailedDescription.Text.Trim();
            _quest.QuestGiverID = ((NPCComboItem)cbQuestGiver.SelectedItem).NPCData.ID;

            // Награды
            if (_quest.Rewards == null)
                _quest.Rewards = new QuestRewards();

            _quest.Rewards.Gold = (int)nudRewardGold.Value;
            _quest.Rewards.Experience = (int)nudRewardEXP.Value;
            _quest.Rewards.Items = _rewardItemsBinding.ToList();

            // Условия
            _quest.Conditions = _conditionsBinding.ToList();

            // Предварительные условия
            _quest.PrerequisiteQuestIDs = _prerequisitesBinding.ToList();

            // Узлы диалогов
            if (_quest.DialogueNodes == null)
                _quest.DialogueNodes = new QuestDialogueNodes();

            _quest.DialogueNodes.Offer = txtOfferNode.Text.Trim();
            _quest.DialogueNodes.InProgress = txtInProgressNode.Text.Trim();
            _quest.DialogueNodes.ReadyToComplete = txtReadyToCompleteNode.Text.Trim();
            _quest.DialogueNodes.Completed = txtCompletedNode.Text.Trim();

            // Валидация узлов диалогов
            if (string.IsNullOrEmpty(_quest.DialogueNodes.Offer))
            {
                MessageBox.Show("Необходимо указать ID узла предложения квеста.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private QuestConditionData ConvertToData(QuestCondition condition)
        {
            return condition switch
            {
                CollectItemsCondition collect => new QuestConditionData(collect),
                KillMonstersCondition kill => new QuestConditionData(kill),
                VisitLocationCondition visit => new QuestConditionData(visit),
                TalkToNPCCondition talk => new QuestConditionData(talk),
                ReachLevelCondition level => new QuestConditionData(level),
                _ => throw new ArgumentException($"Unknown condition type: {condition.GetType().Name}")
            };
        }

        public EnhancedQuest GetQuest() => _quest;

        private class NPCComboItem
        {
            public NPCData NPCData { get; }

            public NPCComboItem(NPCData npcData)
            {
                NPCData = npcData;
            }

            public override string ToString()
            {
                return $"{NPCData.Name} (ID: {NPCData.ID})";
            }
        }
    }
}