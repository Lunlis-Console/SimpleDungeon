using Engine.Data; // MonsterData, LootItemData, GameData
using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using Engine.Entities;
using System.Drawing;

namespace JsonEditor
{
    public class EditMonsterForm : Form
    {
        private readonly MonsterData _monster;
        private readonly GameData? _gameData;

        // Controls
        private Label lblName, lblLevel, lblCurrentHP, lblMaximumHP, lblRewardEXP, lblRewardGold;
        private TextBox txtName, txtLevel, txtCurrentHP, txtMaximumHP, txtRewardEXP, txtRewardGold;

        private GroupBox grpAttributes;
        private Label lStr, lCon, lDex, lInt, lWis, lCha;
        private TextBox txtStrength, txtConstitution, txtDexterity, txtIntelligence, txtWisdom, txtCharisma;

        private Label lblLoot;
        private ListBox listBoxLoot;
        private Button btnAddLoot, btnEditLoot, btnRemoveLoot;

        private Button btnSave, btnCancel;

        private BindingList<LootItemData> lootBinding = new BindingList<LootItemData>();

        public EditMonsterForm(MonsterData monster, GameData? gameData = null)
        {
            _monster = monster ?? throw new ArgumentNullException(nameof(monster));
            _gameData = gameData;

            Text = $"Edit Monster #{_monster.ID}";
            StartPosition = FormStartPosition.CenterParent;

            // Компактный размер формы — стандартный экран без прокрутки
            Width = 720;
            Height = 680;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            InitializeControls();
            LoadMonsterToControls();

            AcceptButton = btnSave;
            CancelButton = btnCancel;
        }

        private void InitializeControls()
        {
            // Compact layout constants
            int padLeft = 12;
            int labelW = 160;
            int tbLeft = 200;
            int tbW = 500 - 30; // немного уменьшим ширину чтобы поместились кнопки
            int top = 12;
            int vgap = 30; // уменьшённый вертикальный шаг

            // Basic fields
            lblName = new Label { Left = padLeft, Top = top + 2, Width = labelW, Text = "Name:" };
            txtName = new TextBox { Left = tbLeft, Top = top, Width = tbW }; top += vgap;

            lblLevel = new Label { Left = padLeft, Top = top + 2, Width = labelW, Text = "Level:" };
            txtLevel = new TextBox { Left = tbLeft, Top = top, Width = 120 }; top += vgap;

            lblCurrentHP = new Label { Left = padLeft, Top = top + 2, Width = labelW, Text = "Current HP:" };
            txtCurrentHP = new TextBox { Left = tbLeft, Top = top, Width = 120 }; top += vgap;

            lblMaximumHP = new Label { Left = padLeft, Top = top + 2, Width = labelW, Text = "Maximum HP:" };
            txtMaximumHP = new TextBox { Left = tbLeft, Top = top, Width = 120 }; top += vgap;

            lblRewardEXP = new Label { Left = padLeft, Top = top + 2, Width = labelW, Text = "Reward EXP:" };
            txtRewardEXP = new TextBox { Left = tbLeft, Top = top, Width = 120 }; top += vgap;

            lblRewardGold = new Label { Left = padLeft, Top = top + 2, Width = labelW, Text = "Reward Gold:" };
            txtRewardGold = new TextBox { Left = tbLeft, Top = top, Width = 120 }; top += vgap + 4;

            // Attributes group — компактная высота
            int grpWidth = tbW + labelW - 8;
            int grpHeight = 170; // уменьшено
            grpAttributes = new GroupBox { Left = padLeft, Top = top, Width = grpWidth, Height = grpHeight, Text = "Attributes" };
            int aLeftLabel = 10, aLeftTextbox = 160, aTop = 20, aV = 28;

            lStr = new Label { Left = aLeftLabel, Top = aTop + 4, Width = 140, Text = "Strength:" };
            txtStrength = new TextBox { Left = aLeftTextbox, Top = aTop, Width = 80 }; aTop += aV;

            lCon = new Label { Left = aLeftLabel, Top = aTop + 4, Width = 140, Text = "Constitution:" };
            txtConstitution = new TextBox { Left = aLeftTextbox, Top = aTop, Width = 80 }; aTop += aV;

            lDex = new Label { Left = aLeftLabel, Top = aTop + 4, Width = 140, Text = "Dexterity:" };
            txtDexterity = new TextBox { Left = aLeftTextbox, Top = aTop, Width = 80 }; aTop += aV;

            lInt = new Label { Left = aLeftLabel, Top = aTop + 4, Width = 140, Text = "Intelligence:" };
            txtIntelligence = new TextBox { Left = aLeftTextbox, Top = aTop, Width = 80 }; aTop += aV;

            lWis = new Label { Left = aLeftLabel + 260, Top = 20 + 4, Width = 140, Text = "Wisdom:" };
            txtWisdom = new TextBox { Left = aLeftTextbox + 260, Top = 20, Width = 80 };

            lCha = new Label { Left = aLeftLabel + 260, Top = 20 + aV + 4, Width = 140, Text = "Charisma:" };
            txtCharisma = new TextBox { Left = aLeftTextbox + 260, Top = 20 + aV, Width = 80 };

            // Add attribute controls (compact two-column)
            grpAttributes.Controls.AddRange(new Control[] {
                lStr, txtStrength,
                lCon, txtConstitution,
                lDex, txtDexterity,
                lInt, txtIntelligence,
                lWis, txtWisdom,
                lCha, txtCharisma
            });

            top += grpAttributes.Height + 8;

            // Loot label + list (уменьшенная высота)
            lblLoot = new Label { Left = padLeft, Top = top + 6, Width = labelW, Text = "LootTable:" };
            int listHeight = 160; // уменьшено
            listBoxLoot = new ListBox { Left = tbLeft, Top = top, Width = tbW, Height = listHeight, FormattingEnabled = true, IntegralHeight = false };
            listBoxLoot.DoubleClick += (s, e) => EditSelectedLoot();

            int lootTop = top + listHeight + 8;

            // Buttons for loot
            btnAddLoot = new Button { Left = tbLeft, Top = lootTop, Width = 110, Text = "Добавить" };
            btnEditLoot = new Button { Left = tbLeft + 120, Top = lootTop, Width = 130, Text = "Редактировать" };
            btnRemoveLoot = new Button { Left = tbLeft + 260, Top = lootTop, Width = 110, Text = "Удалить" };

            btnAddLoot.Click += (s, e) => AddNewLoot();
            btnEditLoot.Click += (s, e) => EditSelectedLoot();
            btnRemoveLoot.Click += (s, e) =>
            {
                if (listBoxLoot.SelectedItem is LootItemData li) lootBinding.Remove(li);
            };

            // Save / Cancel at bottom — разместим компактно
            int btnBottomTop = lootTop + 56;
            btnSave = new Button { Left = tbLeft, Top = btnBottomTop, Width = 160, Height = 36, Text = "Save", DialogResult = DialogResult.OK };
            btnCancel = new Button { Left = tbLeft + 180, Top = btnBottomTop, Width = 160, Height = 36, Text = "Cancel", DialogResult = DialogResult.Cancel };
            btnSave.Click += BtnSave_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            // Add all controls to form
            Controls.AddRange(new Control[] {
                lblName, txtName,
                lblLevel, txtLevel,
                lblCurrentHP, txtCurrentHP,
                lblMaximumHP, txtMaximumHP,
                lblRewardEXP, txtRewardEXP,
                lblRewardGold, txtRewardGold,
                grpAttributes,
                lblLoot, listBoxLoot,
                btnAddLoot, btnEditLoot, btnRemoveLoot,
                btnSave, btnCancel
            });

            // Ensure nothing is outside client area — small safety: if form border changed, we cap positions
            var maxBottom = Controls.Cast<Control>().Max(c => c.Bottom);
            int clientAvailable = this.ClientSize.Height;
            if (maxBottom + 12 > clientAvailable)
            {
                // If still too tall (rare), slightly reduce spacing between blocks
                int shrink = (maxBottom + 12) - clientAvailable;
                // Try to reduce group box and list heights a bit more
                if (grpAttributes.Height - shrink / 2 >= 120) grpAttributes.Height -= shrink / 2;
                if (listBoxLoot.Height - shrink / 2 >= 100) listBoxLoot.Height -= shrink / 2;
            }
        }

        private void LoadMonsterToControls()
        {
            txtName.Text = _monster.Name ?? string.Empty;
            txtLevel.Text = _monster.Level.ToString();
            txtCurrentHP.Text = _monster.CurrentHP.ToString();
            txtMaximumHP.Text = _monster.MaximumHP.ToString();
            txtRewardEXP.Text = _monster.RewardEXP.ToString();
            txtRewardGold.Text = _monster.RewardGold.ToString();

            txtStrength.Text = _monster.Attributes?.Strength.ToString() ?? "0";
            txtConstitution.Text = _monster.Attributes?.Constitution.ToString() ?? "0";
            txtDexterity.Text = _monster.Attributes?.Dexterity.ToString() ?? "0";
            txtIntelligence.Text = _monster.Attributes?.Intelligence.ToString() ?? "0";
            txtWisdom.Text = _monster.Attributes?.Wisdom.ToString() ?? "0";
            txtCharisma.Text = _monster.Attributes?.Charisma.ToString() ?? "0";

            var list = _monster.LootTable ?? new System.Collections.Generic.List<LootItemData>();
            lootBinding = new BindingList<LootItemData>(list);
            listBoxLoot.DataSource = lootBinding;

            listBoxLoot.Format += (s, e) =>
            {
                if (e.ListItem is LootItemData li)
                {
                    var name = _gameData?.Items?.FirstOrDefault(it => it.ID == li.ItemID)?.Name;
                    e.Value = name != null ? $"{li.ItemID} - {name} ({li.DropPercentage}%)" : $"{li.ItemID} ({li.DropPercentage}%)";
                }
            };
            listBoxLoot.DisplayMember = ""; // используем Format
        }

        private void AddNewLoot()
        {
            var initialId = (_gameData?.Items?.FirstOrDefault()?.ID) ?? 1000;
            var li = new LootItemData { ItemID = initialId, DropPercentage = 10, IsUnique = false };
            using var dlg = new EditLootForm(li, _gameData);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                lootBinding.Add(li);
            }
        }

        private void EditSelectedLoot()
        {
            if (!(listBoxLoot.SelectedItem is LootItemData li)) return;
            using var dlg = new EditLootForm(li, _gameData);
            if (dlg.ShowDialog() == DialogResult.OK)
            {
                var idx = lootBinding.IndexOf(li);
                if (idx >= 0) lootBinding.ResetItem(idx);
            }
        }

        private void BtnSave_Click(object? sender, EventArgs e)
        {
            _monster.Name = txtName.Text.Trim();

            if (int.TryParse(txtLevel.Text, out var lvl)) _monster.Level = lvl;
            if (int.TryParse(txtCurrentHP.Text, out var chp)) _monster.CurrentHP = chp;
            if (int.TryParse(txtMaximumHP.Text, out var maxhp)) _monster.MaximumHP = maxhp;
            if (int.TryParse(txtRewardEXP.Text, out var exp)) _monster.RewardEXP = exp;
            if (int.TryParse(txtRewardGold.Text, out var gold)) _monster.RewardGold = gold;

            if (_monster.Attributes == null) _monster.Attributes = new Attributes();

            if (int.TryParse(txtStrength.Text, out var s)) _monster.Attributes.Strength = s;
            if (int.TryParse(txtConstitution.Text, out var c)) _monster.Attributes.Constitution = c;
            if (int.TryParse(txtDexterity.Text, out var d)) _monster.Attributes.Dexterity = d;
            if (int.TryParse(txtIntelligence.Text, out var i)) _monster.Attributes.Intelligence = i;
            if (int.TryParse(txtWisdom.Text, out var w)) _monster.Attributes.Wisdom = w;
            if (int.TryParse(txtCharisma.Text, out var ch)) _monster.Attributes.Charisma = ch;

            _monster.LootTable = lootBinding.ToList();

            DialogResult = DialogResult.OK;
        }
    }
}
