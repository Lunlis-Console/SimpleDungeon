using Engine.Core;
using Engine.Data;
using Engine.Entities;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace JsonEditor
{
    public class EditItemForm : Form
    {
        // Входные/выходные данные
        private ItemData _itemDataCopy;
        public ItemData EditedItemData => _itemDataCopy;

        // Контролы
        private Label lblName, lblNamePlural, lblType, lblPrice, lblDescription, lblComponents;
        private TextBox txtName, txtNamePlural, txtDescription;
        private ComboBox cbType;
        private NumericUpDown nudPrice;
        private Button btnOk, btnCancel, btnEditComponents;
        private Label lblComponentsPreview;


        // Внутри класса EditItemForm добавь этот перегруженный конструктор:
        public EditItemForm(Item sourceItem) : this(ConvertItemToItemData(sourceItem))
        {
            // Пусто — конструкция делегирует в уже существующий конструктор, который принимает ItemData.
        }

        // Внутри того же класса добавь статический метод-конвертер:
        private static ItemData ConvertItemToItemData(Item item)
        {
            if (item == null) return new ItemData();

            var data = new ItemData
            {
                ID = item.ID,
                Name = item.Name ?? string.Empty,
                NamePlural = item.NamePlural ?? string.Empty,
                Type = item.Type,
                Price = item.Price,
                Description = item.Description ?? string.Empty,
                // оставляем старые поля пустыми — используем Components для новых данных
                AttackBonus = null,
                DefenceBonus = null,
                AgilityBonus = null,
                HealthBonus = null,
                AmountToHeal = null,
                Components = new List<IItemComponent>()
            };

            // Если это CompositeItem, скопируем компоненты (shallow copy)
            if (item is CompositeItem comp)
            {
                if (comp.Components != null)
                {
                    foreach (var c in comp.Components)
                    {
                        data.Components.Add(CloneComponentForData(c));
                    }
                }
                return data;
            }

            // Если это Equipment — положим EquipComponent с бонусами
            if (item is Equipment equipment)
            {
                data.Components.Add(new EquipComponent
                {
                    Slot = equipment.Type.ToString(),
                    AttackBonus = equipment.AttackBonus,
                    DefenceBonus = equipment.DefenceBonus,
                    AgilityBonus = equipment.AgilityBonus,
                    HealthBonus = equipment.HealthBonus
                });

                return data;
            }

            // Если это HealingItem — положим HealComponent (и для совместимости можно установить AmountToHeal)
            if (item is HealingItem heal)
            {
                data.Components.Add(new HealComponent { HealAmount = heal.AmountToHeal });
                data.AmountToHeal = heal.AmountToHeal;
                return data;
            }

            // По умолчанию — оставляем пустые Components (Item без спец. поведения)
            return data;
        }

        // Вспомогательный метод - клонирует компонент для безопасного редактирования
        private static IItemComponent CloneComponentForData(IItemComponent src)
        {
            if (src == null) return null;
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


        // Конструктор: передаём исходный ItemData (может быть null для нового)
        public EditItemForm(ItemData source)
        {
            if (source == null)
            {
                // создаём новый пустой объект
                _itemDataCopy = new ItemData();
            }
            else
            {
                // копируем данные поверхностно — компоненты будем клонировать при открытии редактора
                _itemDataCopy = new ItemData
                {
                    ID = source.ID,
                    Name = source.Name,
                    NamePlural = source.NamePlural,
                    Type = source.Type,
                    Price = source.Price,
                    Description = source.Description,
                    AttackBonus = source.AttackBonus,
                    DefenceBonus = source.DefenceBonus,
                    AgilityBonus = source.AgilityBonus,
                    HealthBonus = source.HealthBonus,
                    AmountToHeal = source.AmountToHeal,
                    // копируем список компонентов (shallow copy)
                    Components = source.Components != null ? source.Components.ToList() : new List<IItemComponent>()
                };
            }

            InitializeComponent();
            LoadDataToControls();
            UpdateComponentsPreview();
        }

        private void InitializeComponent()
        {
            Text = "Редактирование предмета";
            Size = new Size(640, 360);
            StartPosition = FormStartPosition.CenterParent;
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            MinimizeBox = false;

            int leftLabel = 12;
            int leftControl = 140;
            int top = 12;
            int vertGap = 30;
            int labelWidth = 120;
            int controlWidth = 460;

            // Name
            lblName = new Label { Text = "Name:", Left = leftLabel, Top = top + 4, Width = labelWidth };
            txtName = new TextBox { Left = leftControl, Top = top, Width = controlWidth };
            top += vertGap;

            // NamePlural
            lblNamePlural = new Label { Text = "NamePlural:", Left = leftLabel, Top = top + 4, Width = labelWidth };
            txtNamePlural = new TextBox { Left = leftControl, Top = top, Width = controlWidth };
            top += vertGap;

            // Type (enum)
            lblType = new Label { Text = "Type:", Left = leftLabel, Top = top + 4, Width = labelWidth };
            cbType = new ComboBox { Left = leftControl, Top = top, Width = 220, DropDownStyle = ComboBoxStyle.DropDownList };
            // заполним из enum ItemType (если он присутствует)
            foreach (var val in Enum.GetValues(typeof(ItemType)))
                cbType.Items.Add(val);
            top += vertGap;

            // Price
            lblPrice = new Label { Text = "Price:", Left = leftLabel, Top = top + 4, Width = labelWidth };
            nudPrice = new NumericUpDown { Left = leftControl, Top = top, Width = 120, Minimum = 0, Maximum = 1000000, DecimalPlaces = 0 };
            top += vertGap;

            // Description
            lblDescription = new Label { Text = "Description:", Left = leftLabel, Top = top + 6, Width = labelWidth };
            txtDescription = new TextBox { Left = leftControl, Top = top, Width = controlWidth, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical };
            top += 90;

            // Components button & preview
            btnEditComponents = new Button { Left = leftLabel, Top = top, Width = 140, Height = 28, Text = "Компоненты..." };
            btnEditComponents.Click += BtnEditComponents_Click;

            lblComponents = new Label { Text = "Components:", Left = leftLabel + 150, Top = top + 6, Width = 80 };
            lblComponentsPreview = new Label { Left = leftLabel + 230, Top = top, Width = controlWidth - 100, Height = 40, AutoSize = false };

            top += 60;

            // Buttons OK/Cancel
            btnOk = new Button { Text = "OK", Left = leftControl - 120, Width = 100, Top = top, DialogResult = DialogResult.OK };
            btnOk.Click += BtnOk_Click;
            btnCancel = new Button { Text = "Отмена", Left = leftControl, Width = 100, Top = top, DialogResult = DialogResult.Cancel };
            btnCancel.Click += (s, e) => { DialogResult = DialogResult.Cancel; Close(); };

            Controls.AddRange(new Control[] {
                lblName, txtName,
                lblNamePlural, txtNamePlural,
                lblType, cbType,
                lblPrice, nudPrice,
                lblDescription, txtDescription,
                btnEditComponents, lblComponents, lblComponentsPreview,
                btnOk, btnCancel
            });
        }

        private void LoadDataToControls()
        {
            txtName.Text = _itemDataCopy.Name ?? string.Empty;
            txtNamePlural.Text = _itemDataCopy.NamePlural ?? string.Empty;
            cbType.SelectedItem = _itemDataCopy.Type;
            nudPrice.Value = _itemDataCopy.Price;
            txtDescription.Text = _itemDataCopy.Description ?? string.Empty;
        }

        private void BtnEditComponents_Click(object sender, EventArgs e)
        {
            // Создаём копию компонентов для редактирования
            var copy = (_itemDataCopy.Components != null) ? _itemDataCopy.Components.Select(CloneComponent).ToList() : new List<IItemComponent>();

            using (var dlg = new ComponentsEditorForm(copy))
            {
                var dr = dlg.ShowDialog(this);
                if (dr == DialogResult.OK && dlg.Result != null)
                {
                    // Присваиваем результат (копии) обратно — это изменит локальную копию itemData
                    _itemDataCopy.Components = dlg.Result;
                    UpdateComponentsPreview();
                }
            }
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Валидация минимальная
            if (string.IsNullOrWhiteSpace(txtName.Text))
            {
                MessageBox.Show(this, "Name обязателен.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            // Сохраняем поля обратно в _itemDataCopy
            _itemDataCopy.Name = txtName.Text.Trim();
            _itemDataCopy.NamePlural = txtNamePlural.Text.Trim();
            if (cbType.SelectedItem != null && cbType.SelectedItem is ItemType it)
                _itemDataCopy.Type = it;
            _itemDataCopy.Price = (int)nudPrice.Value;
            _itemDataCopy.Description = txtDescription.Text;

            // Если Components == null, оставляем как есть (параметры поведения JSON сохранятся)
            // Закрываем с результатом OK
            DialogResult = DialogResult.OK;
            Close();
        }

        // Клонирование компонентов (поверхностно) — чтобы не редактировать оригинальные объекты
        private IItemComponent CloneComponent(IItemComponent src)
        {
            if (src == null) return null;
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

        // Короткий предпросмотр списка компонентов в label
        private void UpdateComponentsPreview()
        {
            if (_itemDataCopy == null || _itemDataCopy.Components == null || !_itemDataCopy.Components.Any())
            {
                lblComponentsPreview.Text = "(компонентов нет)";
                return;
            }

            var parts = _itemDataCopy.Components.Select(c =>
            {
                switch (c)
                {
                    case EquipComponent eq:
                        return $"Equip[{eq.Slot}:+{eq.AttackBonus}/+{eq.DefenceBonus}]";
                    case HealComponent h:
                        return $"Heal[{h.HealAmount}]";
                    case DamageComponent d:
                        return $"Damage[{d.Damage}]";
                    case BuffComponent b:
                        return $"Buff[{b.Attribute}+{b.Amount}]";
                    default:
                        return c.GetType().Name;
                }
            });

            lblComponentsPreview.Text = string.Join(", ", parts);
        }
    }
}
