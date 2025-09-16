using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;
using Engine.Quests;

namespace JsonEditor
{
    public class EditQuestConditionForm : Form
    {
        public QuestCondition QuestCondition { get; private set; }
        private GameData _gameData;

        private ComboBox cbConditionType;
        private TextBox txtDescription;
        private NumericUpDown nudRequiredAmount;
        private ComboBox cbTargetItem;
        private ComboBox cbTargetMonster;
        private ComboBox cbTargetLocation;
        private ComboBox cbTargetNPC;
        private NumericUpDown nudRequiredLevel;
        private Button btnConfigureSpawn;

        private Button btnOk;
        private Button btnCancel;

        private List<QuestItemSpawnData> _currentSpawnLocations = new List<QuestItemSpawnData>();

        public EditQuestConditionForm(GameData gameData, QuestCondition existingCondition = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            QuestCondition = existingCondition;

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование условия квеста";
            this.Width = 500;
            this.Height = 400;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int leftLabel = 12;
            int leftControl = 150;
            int top = 12;
            int vertGap = 30;

            // Тип условия
            var lblConditionType = new Label { Text = "Тип условия:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbConditionType = new ComboBox
            {
                Left = leftControl,
                Top = top,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            cbConditionType.Items.AddRange(new object[]
            {
                "Собрать предметы",
                "Убить монстров",
                "Посетить локацию",
                "Поговорить с NPC",
                "Достичь уровня"
            });
            cbConditionType.SelectedIndexChanged += CbConditionType_SelectedIndexChanged;
            top += vertGap;

            // Описание
            var lblDescription = new Label { Text = "Описание:", Left = leftLabel, Top = top + 4, Width = 130 };
            txtDescription = new TextBox { Left = leftControl, Top = top, Width = 300 };
            top += vertGap;

            // Требуемое количество
            var lblRequiredAmount = new Label { Text = "Требуемое количество:", Left = leftLabel, Top = top + 4, Width = 130 };
            nudRequiredAmount = new NumericUpDown { Left = leftControl, Top = top, Width = 100, Minimum = 1, Maximum = 10000 };
            top += vertGap;

            // Целевой предмет
            var lblTargetItem = new Label { Text = "Целевой предмет:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbTargetItem = new ComboBox
            {
                Left = leftControl,
                Top = top,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            top += vertGap;

            // Целевой монстр
            var lblTargetMonster = new Label { Text = "Целевой монстр:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbTargetMonster = new ComboBox
            {
                Left = leftControl,
                Top = top,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            top += vertGap;

            // Целевая локация
            var lblTargetLocation = new Label { Text = "Целевая локация:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbTargetLocation = new ComboBox
            {
                Left = leftControl,
                Top = top,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            top += vertGap;

            // Целевой NPC
            var lblTargetNPC = new Label { Text = "Целевой NPC:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbTargetNPC = new ComboBox
            {
                Left = leftControl,
                Top = top,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            top += vertGap;

            // Требуемый уровень
            var lblRequiredLevel = new Label { Text = "Требуемый уровень:", Left = leftLabel, Top = top + 4, Width = 130 };
            nudRequiredLevel = new NumericUpDown { Left = leftControl, Top = top, Width = 100, Minimum = 1, Maximum = 100 };
            top += vertGap;

            // Кнопка настройки спавна предметов
            btnConfigureSpawn = new Button 
            { 
                Text = "Настроить спавн предметов", 
                Left = leftLabel, 
                Top = top, 
                Width = 200,
                Enabled = false
            };
            btnConfigureSpawn.Click += BtnConfigureSpawn_Click;
            top += vertGap + 10;

            // Кнопки
            btnOk = new Button { Text = "OK", Left = 150, Top = top + 20, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Отмена", Left = 240, Top = top + 20, Width = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.AddRange(new Control[]
            {
                lblConditionType, cbConditionType,
                lblDescription, txtDescription,
                lblRequiredAmount, nudRequiredAmount,
                lblTargetItem, cbTargetItem,
                lblTargetMonster, cbTargetMonster,
                lblTargetLocation, cbTargetLocation,
                lblTargetNPC, cbTargetNPC,
                lblRequiredLevel, nudRequiredLevel,
                btnConfigureSpawn,
                btnOk, btnCancel
            });

            // Инициализация видимости элементов
            UpdateControlVisibility();
        }

        private void LoadData()
        {
            // Заполнение списков
            LoadItems();
            LoadMonsters();
            LoadLocations();
            LoadNPCs();

            // Загрузка данных существующего условия
            if (QuestCondition != null)
            {
                txtDescription.Text = QuestCondition.Description;
                nudRequiredAmount.Value = QuestCondition.RequiredAmount;

                switch (QuestCondition)
                {
                    case CollectItemsCondition collectCondition:
                        cbConditionType.SelectedIndex = 0;
                        SelectItem(collectCondition.ItemID);
                        // Загружаем данные спавна если они есть
                        if (collectCondition.SpawnLocations != null && collectCondition.SpawnLocations.Any())
                        {
                            _currentSpawnLocations = collectCondition.SpawnLocations.ToList();
                        }
                        break;
                    case KillMonstersCondition killCondition:
                        cbConditionType.SelectedIndex = 1;
                        SelectMonster(killCondition.MonsterID);
                        break;
                    case VisitLocationCondition visitCondition:
                        cbConditionType.SelectedIndex = 2;
                        SelectLocation(visitCondition.LocationID);
                        nudRequiredAmount.Value = 1;
                        break;
                    case TalkToNPCCondition talkCondition:
                        cbConditionType.SelectedIndex = 3;
                        SelectNPC(talkCondition.NPCID);
                        nudRequiredAmount.Value = 1;
                        break;
                    case ReachLevelCondition levelCondition:
                        cbConditionType.SelectedIndex = 4;
                        nudRequiredLevel.Value = levelCondition.RequiredLevel;
                        break;
                }
            }
            else
            {
                cbConditionType.SelectedIndex = 0;
            }

            UpdateControlVisibility();
        }

        private void LoadItems()
        {
            if (_gameData.Items != null)
            {
                foreach (var item in _gameData.Items.OrderBy(i => i.Name))
                {
                    cbTargetItem.Items.Add(new ItemComboItem(item));
                }
            }
        }

        private void LoadMonsters()
        {
            if (_gameData.Monsters != null)
            {
                foreach (var monster in _gameData.Monsters.OrderBy(m => m.Name))
                {
                    cbTargetMonster.Items.Add(new MonsterComboItem(monster));
                }
            }
        }

        private void LoadLocations()
        {
            if (_gameData.Locations != null)
            {
                foreach (var location in _gameData.Locations.OrderBy(l => l.Name))
                {
                    cbTargetLocation.Items.Add(new LocationComboItem(location));
                }
            }
        }

        private void LoadNPCs()
        {
            if (_gameData.NPCs != null)
            {
                foreach (var npc in _gameData.NPCs.OrderBy(n => n.Name))
                {
                    cbTargetNPC.Items.Add(new NPCComboItem(npc));
                }
            }
        }

        private void SelectItem(int itemID)
        {
            var selectedItem = cbTargetItem.Items.Cast<ItemComboItem>()
                .FirstOrDefault(i => i.ItemData.ID == itemID);
            if (selectedItem != null)
                cbTargetItem.SelectedItem = selectedItem;
        }

        private void SelectMonster(int monsterID)
        {
            var selectedMonster = cbTargetMonster.Items.Cast<MonsterComboItem>()
                .FirstOrDefault(m => m.MonsterData.ID == monsterID);
            if (selectedMonster != null)
                cbTargetMonster.SelectedItem = selectedMonster;
        }

        private void SelectLocation(int locationID)
        {
            var selectedLocation = cbTargetLocation.Items.Cast<LocationComboItem>()
                .FirstOrDefault(l => l.LocationData.ID == locationID);
            if (selectedLocation != null)
                cbTargetLocation.SelectedItem = selectedLocation;
        }

        private void SelectNPC(int npcID)
        {
            var selectedNPC = cbTargetNPC.Items.Cast<NPCComboItem>()
                .FirstOrDefault(n => n.NPCData.ID == npcID);
            if (selectedNPC != null)
                cbTargetNPC.SelectedItem = selectedNPC;
        }

        private void CbConditionType_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateControlVisibility();
        }

        private void UpdateControlVisibility()
        {
            // Скрываем все целевые элементы
            cbTargetItem.Visible = false;
            cbTargetMonster.Visible = false;
            cbTargetLocation.Visible = false;
            cbTargetNPC.Visible = false;
            nudRequiredLevel.Visible = false;
            btnConfigureSpawn.Visible = false;

            // Показываем соответствующие элементы в зависимости от типа
            switch (cbConditionType.SelectedIndex)
            {
                case 0: // Собрать предметы
                    cbTargetItem.Visible = true;
                    nudRequiredAmount.Visible = true;
                    btnConfigureSpawn.Visible = true;
                    btnConfigureSpawn.Enabled = true;
                    break;
                case 1: // Убить монстров
                    cbTargetMonster.Visible = true;
                    nudRequiredAmount.Visible = true;
                    break;
                case 2: // Посетить локацию
                    cbTargetLocation.Visible = true;
                    nudRequiredAmount.Visible = false;
                    break;
                case 3: // Поговорить с NPC
                    cbTargetNPC.Visible = true;
                    nudRequiredAmount.Visible = false;
                    break;
                case 4: // Достичь уровня
                    nudRequiredLevel.Visible = true;
                    nudRequiredAmount.Visible = false;
                    break;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Валидация
            if (string.IsNullOrWhiteSpace(txtDescription.Text))
            {
                MessageBox.Show("Введите описание условия.");
                return;
            }

            // Создание условия в зависимости от типа
            int id = QuestCondition?.ID ?? 0;
            string description = txtDescription.Text.Trim();

            switch (cbConditionType.SelectedIndex)
            {
                case 0: // Собрать предметы
                    if (cbTargetItem.SelectedItem is ItemComboItem selectedItem)
                    {
                        var collectCondition = new CollectItemsCondition(id, description, selectedItem.ItemData.ID, (int)nudRequiredAmount.Value);
                        collectCondition.SpawnLocations = _currentSpawnLocations;
                        QuestCondition = collectCondition;
                    }
                    else
                    {
                        MessageBox.Show("Выберите предмет.");
                        return;
                    }
                    break;

                case 1: // Убить монстров
                    if (cbTargetMonster.SelectedItem is MonsterComboItem selectedMonster)
                    {
                        QuestCondition = new KillMonstersCondition(id, description, selectedMonster.MonsterData.ID, (int)nudRequiredAmount.Value);
                    }
                    else
                    {
                        MessageBox.Show("Выберите монстра.");
                        return;
                    }
                    break;

                case 2: // Посетить локацию
                    if (cbTargetLocation.SelectedItem is LocationComboItem selectedLocation)
                    {
                        QuestCondition = new VisitLocationCondition(id, description, selectedLocation.LocationData.ID);
                    }
                    else
                    {
                        MessageBox.Show("Выберите локацию.");
                        return;
                    }
                    break;

                case 3: // Поговорить с NPC
                    if (cbTargetNPC.SelectedItem is NPCComboItem selectedNPC)
                    {
                        QuestCondition = new TalkToNPCCondition(id, description, selectedNPC.NPCData.ID);
                    }
                    else
                    {
                        MessageBox.Show("Выберите NPC.");
                        return;
                    }
                    break;

                case 4: // Достичь уровня
                    QuestCondition = new ReachLevelCondition(id, description, (int)nudRequiredLevel.Value);
                    break;

                default:
                    MessageBox.Show("Выберите тип условия.");
                    return;
            }

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void BtnConfigureSpawn_Click(object sender, EventArgs e)
        {
            using (var form = new EditQuestItemSpawnForm(_gameData, _currentSpawnLocations))
            {
                if (form.ShowDialog(this) == DialogResult.OK)
                {
                    _currentSpawnLocations = form.SpawnLocations;
                }
            }
        }

        // Вспомогательные классы для ComboBox
        private class ItemComboItem
        {
            public ItemData ItemData { get; }
            public ItemComboItem(ItemData itemData) { ItemData = itemData; }
            public override string ToString() => $"{ItemData.Name} (ID: {ItemData.ID})";
        }

        private class MonsterComboItem
        {
            public MonsterData MonsterData { get; }
            public MonsterComboItem(MonsterData monsterData) { MonsterData = monsterData; }
            public override string ToString() => $"{MonsterData.Name} (ID: {MonsterData.ID})";
        }

        private class LocationComboItem
        {
            public LocationData LocationData { get; }
            public LocationComboItem(LocationData locationData) { LocationData = locationData; }
            public override string ToString() => $"{LocationData.Name} (ID: {LocationData.ID})";
        }

        private class NPCComboItem
        {
            public NPCData NPCData { get; }
            public NPCComboItem(NPCData npcData) { NPCData = npcData; }
            public override string ToString() => $"{NPCData.Name} (ID: {NPCData.ID})";
        }
    }
}
