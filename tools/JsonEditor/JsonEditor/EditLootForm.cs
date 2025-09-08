using System;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class EditLootForm : Form
    {
        private readonly LootItemData _loot;
        private readonly GameData? _gameData;

        private NumericUpDown nudItemID;
        private NumericUpDown nudDrop;
        private CheckBox chkUnique;
        private ComboBox cboItems;
        private Button btnOK, btnCancel;

        public EditLootForm(LootItemData loot, GameData? gameData = null)
        {
            _loot = loot ?? throw new ArgumentNullException(nameof(loot));
            _gameData = gameData;
            Text = "Edit Loot";
            Width = 520;
            Height = 180;
            StartPosition = FormStartPosition.CenterParent;
            Build();
            LoadValues();
        }

        private void Build()
        {
            Controls.Add(new Label { Left = 12, Top = 12, Width = 120, Text = "Item ID:" });
            nudItemID = new NumericUpDown { Left = 140, Top = 10, Width = 120, Minimum = 0, Maximum = 1_000_000 };

            Controls.Add(new Label { Left = 12, Top = 44, Width = 120, Text = "Drop %:" });
            nudDrop = new NumericUpDown { Left = 140, Top = 42, Width = 120, Minimum = 0, Maximum = 100 };

            chkUnique = new CheckBox { Left = 280, Top = 12, Width = 160, Text = "Is Unique" };

            cboItems = new ComboBox { Left = 280, Top = 42, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            if (_gameData?.Items != null)
            {
                foreach (var it in _gameData.Items)
                    cboItems.Items.Add(new ComboItem(it.ID, it.Name));
                cboItems.SelectedIndexChanged += (s, e) =>
                {
                    if (cboItems.SelectedItem is ComboItem ci) nudItemID.Value = ci.ID;
                };
            }

            btnOK = new Button { Left = 120, Top = 100, Width = 120, Text = "OK" };
            btnCancel = new Button { Left = 260, Top = 100, Width = 120, Text = "Cancel" };
            btnOK.Click += BtnOK_Click;
            btnCancel.Click += (s, e) => DialogResult = DialogResult.Cancel;

            Controls.AddRange(new Control[] { nudItemID, nudDrop, chkUnique, cboItems, btnOK, btnCancel });
        }

        private void LoadValues()
        {
            nudItemID.Value = _loot.ItemID;
            nudDrop.Value = Math.Clamp(_loot.DropPercentage, 0, 100);
            chkUnique.Checked = _loot.IsUnique;

            if (_gameData?.Items != null)
            {
                var idx = _gameData.Items.Select((it, i) => (it.ID, i)).FirstOrDefault(x => x.ID == _loot.ItemID).i;
                if (idx >= 0 && idx < cboItems.Items.Count) cboItems.SelectedIndex = idx;
            }
        }

        private void BtnOK_Click(object sender, EventArgs e)
        {
            _loot.ItemID = (int)nudItemID.Value;
            _loot.DropPercentage = (int)nudDrop.Value;
            _loot.IsUnique = chkUnique.Checked;
            DialogResult = DialogResult.OK;
        }

        private class ComboItem
        {
            public int ID { get; }
            public string Name { get; }
            public ComboItem(int id, string name) { ID = id; Name = name; }
            public override string ToString() => $"{Name} ({ID})";
        }
    }
}
