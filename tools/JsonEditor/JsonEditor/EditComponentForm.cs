using System;
using System.Windows.Forms;
using Engine.Entities;

namespace JsonEditor
{
    public class EditComponentForm : Form
    {
        private IItemComponent _component;
        public IItemComponent Component => _component;

        private TableLayoutPanel _panel;
        private Button _btnOk, _btnCancel;

        public EditComponentForm(IItemComponent component)
        {
            _component = component ?? throw new ArgumentNullException(nameof(component));

            Text = $"Редактирование: {component.GetType().Name}";
            Width = 420;
            Height = 220;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _panel = new TableLayoutPanel { Dock = DockStyle.Top, AutoSize = true, ColumnCount = 2, Padding = new Padding(8) };
            _panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40));
            _panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 60));

            Controls.Add(_panel);

            BuildFieldsForComponent(component);

            _btnOk = new Button { Left = 220, Top = 140, Width = 80, Text = "OK", DialogResult = DialogResult.OK };
            _btnCancel = new Button { Left = 310, Top = 140, Width = 80, Text = "Отмена", DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { _btnOk, _btnCancel });

            _btnOk.Click += (s, e) => { SaveFieldsToComponent(); Close(); };
            _btnCancel.Click += (s, e) => { Close(); };
        }

        private void BuildFieldsForComponent(IItemComponent comp)
        {
            if (comp is EquipComponent eq)
            {
                AddLabeledControl("Slot:", new TextBox { Text = eq.Slot, Tag = "Slot" });
                AddLabeledControl("AttackBonus:", new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = eq.AttackBonus });
                AddLabeledControl("DefenceBonus:", new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = eq.DefenceBonus });
                AddLabeledControl("AgilityBonus:", new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = eq.AgilityBonus });
                AddLabeledControl("HealthBonus:", new NumericUpDown { Minimum = -1000, Maximum = 1000, Value = eq.HealthBonus });
            }
            else if (comp is HealComponent h)
            {
                AddLabeledControl("HealAmount:", new NumericUpDown { Minimum = 0, Maximum = 100000, Value = h.HealAmount });
            }
            else if (comp is DamageComponent d)
            {
                AddLabeledControl("Damage:", new NumericUpDown { Minimum = -10000, Maximum = 10000, Value = d.Damage });
                AddLabeledControl("Range:", new NumericUpDown { Minimum = 1, Maximum = 100, Value = d.Range });
            }
            else if (comp is BuffComponent b)
            {
                AddLabeledControl("Attribute:", new TextBox { Text = b.Attribute, Tag = "Attribute" });
                AddLabeledControl("Amount:", new NumericUpDown { Minimum = -10000, Maximum = 10000, Value = b.Amount });
                AddLabeledControl("DurationTurns:", new NumericUpDown { Minimum = 0, Maximum = 1000, Value = b.DurationTurns });
            }
            else
            {
                AddLabeledControl("Info:", new Label { Text = comp.GetType().Name });
            }
        }

        private void AddLabeledControl(string label, Control ctrl)
        {
            var lbl = new Label { Text = label, AutoSize = true, Anchor = AnchorStyles.Left };
            ctrl.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            _panel.RowCount += 1;
            _panel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            _panel.Controls.Add(lbl, 0, _panel.RowCount - 1);
            _panel.Controls.Add(ctrl, 1, _panel.RowCount - 1);
        }

        private void SaveFieldsToComponent()
        {
            if (_component is EquipComponent eq)
            {
                eq.Slot = GetTextBoxValue("Slot") ?? eq.Slot;
                eq.AttackBonus = (int)GetNumericValue("AttackBonus", eq.AttackBonus);
                eq.DefenceBonus = (int)GetNumericValue("DefenceBonus", eq.DefenceBonus);
                eq.AgilityBonus = (int)GetNumericValue("AgilityBonus", eq.AgilityBonus);
                eq.HealthBonus = (int)GetNumericValue("HealthBonus", eq.HealthBonus);
            }
            else if (_component is HealComponent h)
            {
                h.HealAmount = (int)GetNumericValue("HealAmount", h.HealAmount);
            }
            else if (_component is DamageComponent d)
            {
                d.Damage = (int)GetNumericValue("Damage", d.Damage);
                d.Range = (int)GetNumericValue("Range", d.Range);
            }
            else if (_component is BuffComponent b)
            {
                b.Attribute = GetTextBoxValue("Attribute") ?? b.Attribute;
                b.Amount = (int)GetNumericValue("Amount", b.Amount);
                b.DurationTurns = (int)GetNumericValue("DurationTurns", b.DurationTurns);
            }
        }

        private string GetTextBoxValue(string fieldTag)
        {
            foreach (Control c in _panel.Controls)
            {
                if (c is TextBox tb && tb.Tag as string == fieldTag) return tb.Text;
                if (c is TextBox tb2)
                {
                    var row = _panel.GetPositionFromControl(tb2).Row;
                    var lbl = _panel.GetControlFromPosition(0, row) as Label;
                    if (lbl != null && lbl.Text.TrimEnd(':').Equals(fieldTag, StringComparison.OrdinalIgnoreCase))
                        return tb2.Text;
                }
            }
            return null;
        }

        private decimal GetNumericValue(string labelText, decimal fallback)
        {
            foreach (Control c in _panel.Controls)
            {
                if (c is NumericUpDown num)
                {
                    var row = _panel.GetPositionFromControl(num).Row;
                    var lbl = _panel.GetControlFromPosition(0, row) as Label;
                    if (lbl != null && lbl.Text.TrimEnd(':').Equals(labelText, StringComparison.OrdinalIgnoreCase))
                        return num.Value;
                }
            }
            foreach (Control c in _panel.Controls)
            {
                if (c is NumericUpDown num2 && (num2.Tag as string) == labelText)
                    return num2.Value;
            }
            return fallback;
        }
    }
}
