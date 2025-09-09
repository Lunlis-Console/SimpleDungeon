using System;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;
using System.Collections.Generic;
using Engine.Entities;

namespace JsonEditor
{
    public class ComponentsEditorForm : Form
    {
        private ListBox _list;
        private Button _btnAdd, _btnEdit, _btnRemove, _btnUp, _btnDown, _btnOk, _btnCancel;
        private BindingList<IItemComponent> _binding;
        public List<IItemComponent> Result { get; private set; }

        public ComponentsEditorForm(IEnumerable<IItemComponent> initial)
        {
            Text = "Редактор компонентов";
            Width = 520;
            Height = 420;
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            _binding = new BindingList<IItemComponent>(initial?.ToList() ?? new List<IItemComponent>());

            _list = new ListBox { Left = 12, Top = 12, Width = 360, Height = 300, DataSource = _binding };
            _list.Format += (s, e) =>
            {
                if (e.ListItem is IItemComponent comp)
                {
                    switch (comp)
                    {
                        case EquipComponent eq:
                            e.Value = $"Equip: Slot={eq.Slot}, +Atk={eq.AttackBonus}, +Def={eq.DefenceBonus}";
                            break;
                        case HealComponent h:
                            e.Value = $"Heal: {h.HealAmount}";
                            break;
                        case DamageComponent d:
                            e.Value = $"Damage: {d.Damage} (Range {d.Range})";
                            break;
                        case BuffComponent b:
                            e.Value = $"Buff: {b.Attribute} +{b.Amount} ({b.DurationTurns}t)";
                            break;
                        default:
                            e.Value = comp.GetType().Name;
                            break;
                    }
                }
            };

            _btnAdd = new Button { Left = 384, Top = 12, Width = 110, Text = "Добавить..." };
            _btnEdit = new Button { Left = 384, Top = 52, Width = 110, Text = "Редактировать" };
            _btnRemove = new Button { Left = 384, Top = 92, Width = 110, Text = "Удалить" };
            _btnUp = new Button { Left = 384, Top = 132, Width = 110, Text = "Вверх" };
            _btnDown = new Button { Left = 384, Top = 172, Width = 110, Text = "Вниз" };

            _btnOk = new Button { Left = 260, Top = 330, Width = 110, Text = "OK", DialogResult = DialogResult.OK };
            _btnCancel = new Button { Left = 380, Top = 330, Width = 110, Text = "Отмена", DialogResult = DialogResult.Cancel };

            Controls.AddRange(new Control[] { _list, _btnAdd, _btnEdit, _btnRemove, _btnUp, _btnDown, _btnOk, _btnCancel });

            _btnAdd.Click += BtnAdd_Click;
            _btnEdit.Click += BtnEdit_Click;
            _btnRemove.Click += BtnRemove_Click;
            _btnUp.Click += BtnUp_Click;
            _btnDown.Click += BtnDown_Click;
            _btnOk.Click += (s, e) => { Result = _binding.ToList(); Close(); };
            _btnCancel.Click += (s, e) => { Result = null; Close(); };
        }

        private void BtnAdd_Click(object sender, EventArgs e)
        {
            using var pick = new ComponentTypePickForm();
            if (pick.ShowDialog(this) != DialogResult.OK) return;

            var type = pick.SelectedType;
            var comp = CreateDefaultComponent(type);
            using var editor = new EditComponentForm(comp);
            if (editor.ShowDialog(this) == DialogResult.OK)
            {
                _binding.Add(editor.Component);
            }
        }

        private void BtnEdit_Click(object sender, EventArgs e)
        {
            if (_list.SelectedItem is IItemComponent comp)
            {
                using var editor = new EditComponentForm(CloneComponent(comp));
                if (editor.ShowDialog(this) == DialogResult.OK)
                {
                    int idx = _list.SelectedIndex;
                    _binding[idx] = editor.Component;
                }
            }
        }

        private void BtnRemove_Click(object sender, EventArgs e)
        {
            if (_list.SelectedIndex >= 0) _binding.RemoveAt(_list.SelectedIndex);
        }

        private void BtnUp_Click(object sender, EventArgs e)
        {
            int i = _list.SelectedIndex;
            if (i > 0)
            {
                var item = _binding[i];
                _binding.RemoveAt(i);
                _binding.Insert(i - 1, item);
                _list.SelectedIndex = i - 1;
            }
        }

        private void BtnDown_Click(object sender, EventArgs e)
        {
            int i = _list.SelectedIndex;
            if (i >= 0 && i < _binding.Count - 1)
            {
                var item = _binding[i];
                _binding.RemoveAt(i);
                _binding.Insert(i + 1, item);
                _list.SelectedIndex = i + 1;
            }
        }

        private IItemComponent CreateDefaultComponent(string typeName)
        {
            return typeName switch
            {
                "Equip" => new EquipComponent { Slot = "None", AttackBonus = 0, DefenceBonus = 0, AgilityBonus = 0, HealthBonus = 0 },
                "Heal" => new HealComponent { HealAmount = 0 },
                "Damage" => new DamageComponent { Damage = 0, Range = 1 },
                "Buff" => new BuffComponent { Attribute = "None", Amount = 0, DurationTurns = 1 },
                _ => throw new ArgumentOutOfRangeException(nameof(typeName))
            };
        }

        private IItemComponent CloneComponent(IItemComponent src)
        {
            return src switch
            {
                EquipComponent e => new EquipComponent
                {
                    Slot = e.Slot,
                    AttackBonus = e.AttackBonus,
                    DefenceBonus = e.DefenceBonus,
                    AgilityBonus = e.AgilityBonus,
                    HealthBonus = e.HealthBonus
                },
                HealComponent h => new HealComponent { HealAmount = h.HealAmount },
                DamageComponent d => new DamageComponent { Damage = d.Damage, Range = d.Range },
                BuffComponent b => new BuffComponent { Attribute = b.Attribute, Amount = b.Amount, DurationTurns = b.DurationTurns },
                _ => src
            };
        }
    }

    internal class ComponentTypePickForm : Form
    {
        private ComboBox _cb;
        private Button _ok, _cancel;
        public string SelectedType { get; private set; }

        public ComponentTypePickForm()
        {
            Text = "Выберите тип компонента";
            Width = 320; Height = 130; StartPosition = FormStartPosition.CenterParent;
            _cb = new ComboBox { Left = 12, Top = 12, Width = 280, DropDownStyle = ComboBoxStyle.DropDownList };
            _cb.Items.AddRange(new[] { "Equip", "Heal", "Damage", "Buff" });
            _ok = new Button { Left = 60, Top = 50, Width = 80, Text = "OK", DialogResult = DialogResult.OK };
            _cancel = new Button { Left = 160, Top = 50, Width = 80, Text = "Отмена", DialogResult = DialogResult.Cancel };
            Controls.AddRange(new Control[] { _cb, _ok, _cancel });
            _ok.Click += (s, e) => { SelectedType = _cb.SelectedItem as string; };
            if (_cb.Items.Count > 0) _cb.SelectedIndex = 0;
        }
    }
}
