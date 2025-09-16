using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        // Заменяем CheckedListBox на DataGridView для поддержки Count
        private DataGridView gridNPCs;
        private DataGridView gridMonsters;
        private DataGridView gridGroundItems;

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
            this.Height = 800; // Увеличиваем высоту для новой секции
            this.StartPosition = FormStartPosition.CenterParent;

            var lblID = new Label { Text = "ID:", Left = 10, Top = 14, Width = 60 };
            numID = new NumericUpDown { Left = 80, Top = 10, Width = 100, Maximum = 99999, Minimum = 0 };

            var lblName = new Label { Text = "Имя:", Left = 200, Top = 14, Width = 60 };
            txtName = new TextBox { Left = 260, Top = 10, Width = 400 };

            var lblDesc = new Label { Text = "Описание:", Left = 10, Top = 46, Width = 80 };
            txtDescription = new TextBox { Left = 100, Top = 42, Width = 560, Height = 80, Multiline = true, ScrollBars = ScrollBars.Vertical };

            var lblNPCs = new Label { Text = "NPC:", Left = 10, Top = 140, Width = 80 };
            gridNPCs = new DataGridView
            {
                Left = 100,
                Top = 120,
                Width = 560,
                Height = 160,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var lblMonsters = new Label { Text = "Монстры:", Left = 10, Top = 300, Width = 80 };
            gridMonsters = new DataGridView
            {
                Left = 100,
                Top = 280,
                Width = 560,
                Height = 120,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var lblGroundItems = new Label { Text = "Предметы на земле:", Left = 10, Top = 420, Width = 120 };
            gridGroundItems = new DataGridView
            {
                Left = 100,
                Top = 400,
                Width = 560,
                Height = 120,
                AllowUserToAddRows = false,
                AllowUserToDeleteRows = false,
                RowHeadersVisible = false,
                SelectionMode = DataGridViewSelectionMode.FullRowSelect,
                AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill
            };

            var lblNorth = new Label { Text = "Север:", Left = 10, Top = 540, Width = 80 };
            comboNorth = new ComboBox { Left = 100, Top = 536, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblEast = new Label { Text = "Восток:", Left = 320, Top = 540, Width = 80 };
            comboEast = new ComboBox { Left = 400, Top = 536, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblSouth = new Label { Text = "Юг:", Left = 10, Top = 580, Width = 80 };
            comboSouth = new ComboBox { Left = 100, Top = 576, Width = 200, DropDownStyle = ComboBoxStyle.DropDownList };

            var lblWest = new Label { Text = "Запад:", Left = 320, Top = 580, Width = 80 };
            comboWest = new ComboBox { Left = 400, Top = 576, Width = 260, DropDownStyle = ComboBoxStyle.DropDownList };

            btnOk = new Button { Text = "OK", Left = 480, Top = 620, Width = 80 };
            btnCancel = new Button { Text = "Отмена", Left = 580, Top = 620, Width = 80 };

            btnOk.Click += BtnOk_Click;
            btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

            this.Controls.Add(lblID);
            this.Controls.Add(numID);
            this.Controls.Add(lblName);
            this.Controls.Add(txtName);
            this.Controls.Add(lblDesc);
            this.Controls.Add(txtDescription);
            this.Controls.Add(lblNPCs);
            this.Controls.Add(gridNPCs);
            this.Controls.Add(lblMonsters);
            this.Controls.Add(gridMonsters);
            this.Controls.Add(lblGroundItems);
            this.Controls.Add(gridGroundItems);
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

            InitializeGridColumns();
        }

        private void InitializeGridColumns()
        {
            // NPCs grid columns: Selected, ID, Name, Count
            gridNPCs.Columns.Clear();
            var colSelNpc = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Вкл.", Width = 40 };
            var colIdNpc = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", ReadOnly = true, FillWeight = 20 };
            var colNameNpc = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Имя", ReadOnly = true, FillWeight = 60 };
            var colCountNpc = new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Кол-во", FillWeight = 20 };
            gridNPCs.Columns.AddRange(new DataGridViewColumn[] { colSelNpc, colIdNpc, colNameNpc, colCountNpc });

            // Monsters grid columns: Selected, ID, Name, Count
            gridMonsters.Columns.Clear();
            var colSelMon = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Вкл.", Width = 40 };
            var colIdMon = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", ReadOnly = true, FillWeight = 20 };
            var colNameMon = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Имя", ReadOnly = true, FillWeight = 60 };
            var colCountMon = new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Кол-во", FillWeight = 20 };
            gridMonsters.Columns.AddRange(new DataGridViewColumn[] { colSelMon, colIdMon, colNameMon, colCountMon });

            // Ground Items grid columns: Selected, ID, Name, Count
            gridGroundItems.Columns.Clear();
            var colSelItem = new DataGridViewCheckBoxColumn { Name = "Selected", HeaderText = "Вкл.", Width = 40 };
            var colIdItem = new DataGridViewTextBoxColumn { Name = "ID", HeaderText = "ID", ReadOnly = true, FillWeight = 20 };
            var colNameItem = new DataGridViewTextBoxColumn { Name = "Name", HeaderText = "Имя", ReadOnly = true, FillWeight = 60 };
            var colCountItem = new DataGridViewTextBoxColumn { Name = "Count", HeaderText = "Кол-во", FillWeight = 20 };
            gridGroundItems.Columns.AddRange(new DataGridViewColumn[] { colSelItem, colIdItem, colNameItem, colCountItem });

            // Удобные настройки
            gridNPCs.AllowUserToResizeRows = false;
            gridMonsters.AllowUserToResizeRows = false;
            gridGroundItems.AllowUserToResizeRows = false;
        }

        private void LoadData()
        {
            numID.Value = _location.ID;
            txtName.Text = _location.Name;
            txtDescription.Text = _location.Description;

            // Получаем маппинги из возможных новых структур, используя reflection - безопасно если поле отсутствует
            var npcSpawnMap = ReadNpcSpawnsFromLocation(); // Dictionary<int, int>
            var monsterSpawnMap = ReadMonsterSpawnCountsFromLocation(); // Dictionary<int, int>
            var groundItemsMap = ReadGroundItemsFromLocation(); // Dictionary<int, int>

            // NPCs: fill grid rows
            gridNPCs.Rows.Clear();
            foreach (var npc in _gameData.NPCs)
            {
                bool selected = false;
                int count = 1;

                if (npcSpawnMap != null && npcSpawnMap.TryGetValue(npc.ID, out int c1))
                {
                    selected = true;
                    count = Math.Max(1, c1);
                }
                else if (_location.NPCsHere != null && _location.NPCsHere.Contains(npc.ID))
                {
                    selected = true;
                    count = 1;
                }

                gridNPCs.Rows.Add(selected, npc.ID, npc.Name, count);
            }

            // Monsters: fill grid rows
            gridMonsters.Rows.Clear();
            foreach (var mon in _gameData.Monsters)
            {
                bool selected = false;
                int count = 1;

                if (monsterSpawnMap != null && monsterSpawnMap.TryGetValue(mon.ID, out int c2))
                {
                    selected = true;
                    count = Math.Max(1, c2);
                }
                else if (_location.MonsterTemplates != null && _location.MonsterTemplates.Contains(mon.ID))
                {
                    selected = true;
                    count = 1;
                }

                gridMonsters.Rows.Add(selected, mon.ID, mon.Name, count);
            }

            // Ground Items: fill grid rows
            gridGroundItems.Rows.Clear();
            foreach (var item in _gameData.Items)
            {
                bool selected = false;
                int count = 1;

                if (groundItemsMap != null && groundItemsMap.TryGetValue(item.ID, out int c3))
                {
                    selected = true;
                    count = Math.Max(1, c3);
                }

                gridGroundItems.Rows.Add(selected, item.ID, item.Name, count);
            }

            // Локации для переходов
            BindCombo(comboNorth, _location.LocationToNorth);
            BindCombo(comboEast, _location.LocationToEast);
            BindCombo(comboSouth, _location.LocationToSouth);
            BindCombo(comboWest, _location.LocationToWest);
        }

        private Dictionary<int, int> ReadNpcSpawnsFromLocation()
        {
            // Ищем property NPCSpawns (игнорируем регистр)
            var prop = _location.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, "NPCSpawns", StringComparison.OrdinalIgnoreCase));
            if (prop == null) return null;
            var value = prop.GetValue(_location);
            if (value == null) return null;

            // value должен быть IEnumerable
            if (!(value is IEnumerable enumerable)) return null;

            var dict = new Dictionary<int, int>();
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                var t = item.GetType();
                var idProp = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "NPCID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
                var countProp = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase));
                if (idProp == null) continue;
                object idVal = idProp.GetValue(item);
                if (idVal == null) continue;
                if (!int.TryParse(idVal.ToString(), out int id)) continue;

                int cnt = 1;
                if (countProp != null)
                {
                    var cv = countProp.GetValue(item);
                    if (cv != null && int.TryParse(cv.ToString(), out int tmp)) cnt = Math.Max(1, tmp);
                }

                dict[id] = cnt;
            }
            return dict;
        }

        private Dictionary<int, int> ReadMonsterSpawnCountsFromLocation()
        {
            // Ищем property MonsterSpawns
            var prop = _location.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, "MonsterSpawns", StringComparison.OrdinalIgnoreCase));
            if (prop == null) return null;
            var value = prop.GetValue(_location);
            if (value == null) return null;
            if (!(value is IEnumerable enumerable)) return null;

            var dict = new Dictionary<int, int>();
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                var t = item.GetType();
                var idProp = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "MonsterTemplateID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "TemplateID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
                var countProp = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase));
                if (idProp == null) continue;
                object idVal = idProp.GetValue(item);
                if (idVal == null) continue;
                if (!int.TryParse(idVal.ToString(), out int id)) continue;

                int cnt = 1;
                if (countProp != null)
                {
                    var cv = countProp.GetValue(item);
                    if (cv != null && int.TryParse(cv.ToString(), out int tmp)) cnt = Math.Max(1, tmp);
                }

                dict[id] = cnt;
            }
            return dict;
        }

        private Dictionary<int, int> ReadGroundItemsFromLocation()
        {
            // Ищем property GroundItems
            var prop = _location.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, "GroundItems", StringComparison.OrdinalIgnoreCase));
            if (prop == null) return null;
            var value = prop.GetValue(_location);
            if (value == null) return null;
            if (!(value is IEnumerable enumerable)) return null;

            var dict = new Dictionary<int, int>();
            foreach (var item in enumerable)
            {
                if (item == null) continue;
                var t = item.GetType();
                var idProp = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "ItemID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
                var countProp = t.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Quantity", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase));
                if (idProp == null) continue;
                object idVal = idProp.GetValue(item);
                if (idVal == null) continue;
                if (!int.TryParse(idVal.ToString(), out int id)) continue;

                int cnt = 1;
                if (countProp != null)
                {
                    var cv = countProp.GetValue(item);
                    if (cv != null && int.TryParse(cv.ToString(), out int tmp)) cnt = Math.Max(1, tmp);
                }

                dict[id] = cnt;
            }
            return dict;
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
            // Сохраняем базовые поля
            _location.ID = (int)numID.Value;
            _location.Name = txtName.Text.Trim();
            _location.Description = txtDescription.Text.Trim();

            // Набор выбранных NPC и их количеств
            var selectedNpcMap = new Dictionary<int, int>();
            foreach (DataGridViewRow row in gridNPCs.Rows)
            {
                try
                {
                    bool sel = Convert.ToBoolean(row.Cells["Selected"].Value);
                    if (!sel) continue;
                }
                catch
                {
                    continue;
                }

                if (!int.TryParse(Convert.ToString(row.Cells["ID"].Value), out int id)) continue;
                int cnt = 1;
                int.TryParse(Convert.ToString(row.Cells["Count"].Value), out cnt);
                cnt = Math.Max(1, cnt);
                selectedNpcMap[id] = cnt;
            }

            // Набор выбранных монстров и их количеств
            var selectedMonMap = new Dictionary<int, int>();
            foreach (DataGridViewRow row in gridMonsters.Rows)
            {
                try
                {
                    bool sel = Convert.ToBoolean(row.Cells["Selected"].Value);
                    if (!sel) continue;
                }
                catch
                {
                    continue;
                }

                if (!int.TryParse(Convert.ToString(row.Cells["ID"].Value), out int id)) continue;
                int cnt = 1;
                int.TryParse(Convert.ToString(row.Cells["Count"].Value), out cnt);
                cnt = Math.Max(1, cnt);
                selectedMonMap[id] = cnt;
            }

            // Набор выбранных предметов на земле и их количеств
            var selectedItemsMap = new Dictionary<int, int>();
            foreach (DataGridViewRow row in gridGroundItems.Rows)
            {
                try
                {
                    bool sel = Convert.ToBoolean(row.Cells["Selected"].Value);
                    if (!sel) continue;
                }
                catch
                {
                    continue;
                }

                if (!int.TryParse(Convert.ToString(row.Cells["ID"].Value), out int id)) continue;
                int cnt = 1;
                int.TryParse(Convert.ToString(row.Cells["Count"].Value), out cnt);
                cnt = Math.Max(1, cnt);
                selectedItemsMap[id] = cnt;
            }

            // Сохраняем старую совместимую структуру (список ID)
            _location.NPCsHere = selectedNpcMap.Keys.ToList();
            _location.MonsterTemplates = selectedMonMap.Keys.ToList();

            // Попытка записать новые структуры, если они есть (через reflection)
            TrySetNpcSpawnsOnLocation(selectedNpcMap);
            TrySetMonsterSpawnsOnLocation(selectedMonMap);
            TrySetGroundItemsOnLocation(selectedItemsMap);

            // Сохраняем направления
            _location.LocationToNorth = comboNorth.SelectedValue as int?;
            _location.LocationToEast = comboEast.SelectedValue as int?;
            _location.LocationToSouth = comboSouth.SelectedValue as int?;
            _location.LocationToWest = comboWest.SelectedValue as int?;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void TrySetNpcSpawnsOnLocation(Dictionary<int, int> selectedNpcMap)
        {
            var prop = _location.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, "NPCSpawns", StringComparison.OrdinalIgnoreCase));
            if (prop == null) return; // нет нового поля — ничего не делаем

            var propType = prop.PropertyType;
            // ожидаем List<T>
            if (!propType.IsGenericType) return;
            var elemType = propType.GetGenericArguments()[0];

            // Создаём список T: List<T>
            var listType = typeof(List<>).MakeGenericType(elemType);
            var listInstance = Activator.CreateInstance(listType) as IList;
            if (listInstance == null) return;

            // Попробуем найти свойства элемента для записи
            var idProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "NPCID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
            var countProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase));

            if (idProp == null) return; // без ID бессмысленно

            foreach (var kv in selectedNpcMap)
            {
                var elem = Activator.CreateInstance(elemType);
                idProp.SetValue(elem, kv.Key);
                if (countProp != null)
                {
                    countProp.SetValue(elem, kv.Value);
                }
                listInstance.Add(elem);
            }

            // Устанавливаем свойство
            prop.SetValue(_location, listInstance);
        }

        private void TrySetMonsterSpawnsOnLocation(Dictionary<int, int> selectedMonMap)
        {
            var prop = _location.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, "MonsterSpawns", StringComparison.OrdinalIgnoreCase));
            if (prop == null) return; // нет поля — ничего не делаем

            var propType = prop.PropertyType;
            if (!propType.IsGenericType) return;
            var elemType = propType.GetGenericArguments()[0];

            var listType = typeof(List<>).MakeGenericType(elemType);
            var listInstance = Activator.CreateInstance(listType) as IList;
            if (listInstance == null) return;

            // Нахождение ключевых свойств элемента
            var idProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "MonsterTemplateID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "TemplateID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
            var countProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase));
            var levelProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Level", StringComparison.OrdinalIgnoreCase));
            var weightProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "SpawnWeight", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Weight", StringComparison.OrdinalIgnoreCase));

            if (idProp == null) return;

            foreach (var kv in selectedMonMap)
            {
                var elem = Activator.CreateInstance(elemType);
                idProp.SetValue(elem, kv.Key);
                if (countProp != null) countProp.SetValue(elem, kv.Value);
                // Если есть Level/Weight — оставляем дефолт (иначе можно расширить UI)
                listInstance.Add(elem);
            }

            prop.SetValue(_location, listInstance);
        }

        private void TrySetGroundItemsOnLocation(Dictionary<int, int> selectedItemsMap)
        {
            var prop = _location.GetType().GetProperties(BindingFlags.Instance | BindingFlags.Public)
                .FirstOrDefault(p => string.Equals(p.Name, "GroundItems", StringComparison.OrdinalIgnoreCase));
            if (prop == null) return; // нет поля — ничего не делаем

            var propType = prop.PropertyType;
            if (!propType.IsGenericType) return;
            var elemType = propType.GetGenericArguments()[0];

            var listType = typeof(List<>).MakeGenericType(elemType);
            var listInstance = Activator.CreateInstance(listType) as IList;
            if (listInstance == null) return;

            // Нахождение ключевых свойств элемента
            var idProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "ItemID", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Id", StringComparison.OrdinalIgnoreCase));
            var countProp = elemType.GetProperties().FirstOrDefault(p => string.Equals(p.Name, "Quantity", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Name, "Count", StringComparison.OrdinalIgnoreCase));

            if (idProp == null) return;

            foreach (var kv in selectedItemsMap)
            {
                var elem = Activator.CreateInstance(elemType);
                idProp.SetValue(elem, kv.Key);
                if (countProp != null) countProp.SetValue(elem, kv.Value);
                listInstance.Add(elem);
            }

            prop.SetValue(_location, listInstance);
        }

        public LocationData GetLocation() => _location;
    }
}
