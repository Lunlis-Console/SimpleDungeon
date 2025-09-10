using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;

namespace JsonEditor
{
    public class EditLocationForm : Form
    {
        private readonly GameData _gameData;
        private readonly LocationData _location;

        private NumericUpDown numID;
        private TextBox txtName;
        private TextBox txtDescription;

        private CheckedListBox listNPCs;
        private CheckedListBox listMonsters;

        private ComboBox comboNorth;
        private ComboBox comboEast;
        private ComboBox comboSouth;
        private ComboBox comboWest;

        private Button btnOk;
        private Button btnCancel;

        public EditLocationForm(GameData gameData, LocationData location = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            _location = location ?? new LocationData();

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование локации";
            this.Width = 700;
            this.Height = 600;
            this.StartPosition = FormStartPosition.CenterParent;

            var lblID = new Label { Text = "ID:", Left = 10, Top = 14, Width = 60 };
            numID = new NumericUpDown { Left = 80, Top = 10, Width = 100, Maximum = 9999 };

            var lblName = new Label { Text = "Имя:", Left = 200, Top = 14, Width = 60 };
            txtName = new TextBox { Left = 260, Top = 10, Width = 400 };

            var lblDesc = new Label { Text = "Описание:", Left = 10, Top = 46, Width = 80 };
            txtDescription = new TextBox { Left = 100, Top = 42, Width = 560, Height = 60, Multiline = true };

            var lblNPCs = new Label { Text = "NPC:", Left = 10, Top = 120, Width = 80 };
            listNPCs = new CheckedListBox { Left = 100, Top = 120, Width = 560, Height = 120 };

            var lblMonsters = new Label { Text = "Монстры:", Left = 10, Top = 250, Width = 80 };
            listMonsters = new CheckedListBox { Left = 100, Top = 250, Width = 560, Height = 120 };

            var lblNorth = new Label { Text = "Север:", Left = 10, Top = 380, Width = 80 };
            comboNorth = new ComboBox { Left = 100, Top = 380, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblEast = new Label { Text = "Восток:", Left = 320, Top = 380, Width = 80 };
            comboEast = new ComboBox { Left = 400, Top = 380, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblSouth = new Label { Text = "Юг:", Left = 10, Top = 420, Width = 80 };
            comboSouth = new ComboBox { Left = 100, Top = 420, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblWest = new Label { Text = "Запад:", Left = 320, Top = 420, Width = 80 };
            comboWest = new ComboBox { Left = 400, Top = 420, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            btnOk = new Button { Text = "OK", Left = 480, Top = 500, Width = 80 };
            btnCancel = new Button { Text = "Отмена", Left = 580, Top = 500, Width = 80 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblID);
            this.Controls.Add(numID);
            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblDesc);
            this.Controls.Add(txtDescription);
            this.Controls.Add(lblNPCs);
            this.Controls.Add(listNPCs);
            this.Controls.Add(lblMonsters);
            this.Controls.Add(listMonsters);
            this.Controls.Add(lblNorth);
            this.Controls.Add(comboNorth);
            this.Controls.Add(lblEast);
            this.Controls.Add(comboEast);
            this.Controls.Add(lblSouth);
            this.Controls.Add(comboSouth);
            this.Controls.Add(lblWest);
            this.Controls.Add(comboWest);
            this.Controls.Add(btnOk);
            this.Controls.Add(btnCancel);
        }

        private void LoadData()
        {
            numID.Value = _location.ID;
            txtName.Text = _location.Name;
            txtDescription.Text = _location.Description;

            // NPC
            listNPCs.Items.Clear();
            foreach (var npc in _gameData.NPCs)
            {
                int index = listNPCs.Items.Add($"{npc.ID} - {npc.Name}", _location.NPCsHere.Contains(npc.ID));
            }

            // Monsters
            listMonsters.Items.Clear();
            foreach (var monster in _gameData.Monsters)
            {
                int index = listMonsters.Items.Add($"{monster.ID} - {monster.Name}", _location.MonsterTemplates.Contains(monster.ID));
            }

            // Локации для переходов
            var locationOptions = new List<(int?, string)> { (null, "(нет)") };
            locationOptions.AddRange(
                                        _gameData.Locations.Select(l => ((int?)l.ID, $"{l.ID} - {l.Name}"))
                                    );

            //void BindCombo(ComboBox combo, int? selected)
            //{
            //    combo.DataSource = new List<(int?, string)>(locationOptions);
            //    combo.DisplayMember = "Item2";
            //    combo.ValueMember = "Item1";
            //    combo.SelectedValue = selected;
            //}

            BindCombo(comboNorth, _location.LocationToNorth);
            BindCombo(comboEast, _location.LocationToEast);
            BindCombo(comboSouth, _location.LocationToSouth);
            BindCombo(comboWest, _location.LocationToWest);
        }

        private void BindCombo(ComboBox combo, int? selected)
        {
            var locationOptions = new List<object> { new { Value = (int?)null, DisplayText = "(нет)" } };
            locationOptions.AddRange(
                _gameData.Locations.Select(l => new { Value = (int?)l.ID, DisplayText = $"{l.ID} - {l.Name}" })
            );

            combo.DataSource = locationOptions;
            combo.DisplayMember = "DisplayText";
            combo.ValueMember = "Value";

            if (selected.HasValue)
            {
                var selectedOption = locationOptions.Cast<dynamic>().FirstOrDefault(opt => opt.Value == selected.Value);
                if (selectedOption != null)
                {
                    combo.SelectedItem = selectedOption;
                }
                else
                {
                    combo.SelectedIndex = 0;
                }
            }
            else
            {
                combo.SelectedIndex = 0;
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            _location.ID = (int)numID.Value;
            _location.Name = txtName.Text.Trim();
            _location.Description = txtDescription.Text.Trim();

            _location.NPCsHere = listNPCs.CheckedItems
                .Cast<string>()
                .Select(item => int.Parse(item.Split('-')[0].Trim()))
                .ToList();

            _location.MonsterTemplates = listMonsters.CheckedItems
                .Cast<string>()
                .Select(item => int.Parse(item.Split('-')[0].Trim()))
                .ToList();

            _location.LocationToNorth = comboNorth.SelectedValue as int?;
            _location.LocationToEast = comboEast.SelectedValue as int?;
            _location.LocationToSouth = comboSouth.SelectedValue as int?;
            _location.LocationToWest = comboWest.SelectedValue as int?;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        public LocationData GetLocation() => _location;
    }
}
