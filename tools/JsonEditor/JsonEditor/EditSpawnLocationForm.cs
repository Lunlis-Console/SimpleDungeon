using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using Engine.Data;
using Engine.Quests;

namespace JsonEditor
{
    public class EditSpawnLocationForm : Form
    {
        public QuestItemSpawnData SpawnData { get; private set; }
        private GameData _gameData;

        private ComboBox cbLocation;
        private NumericUpDown nudSpawnChance;
        private NumericUpDown nudQuantity;
        private NumericUpDown nudMaxItemsOnLocation;
        private NumericUpDown nudSpawnInterval;

        private Button btnOk;
        private Button btnCancel;

        public EditSpawnLocationForm(GameData gameData, QuestItemSpawnData existingSpawn = null)
        {
            _gameData = gameData ?? throw new ArgumentNullException(nameof(gameData));
            SpawnData = existingSpawn ?? new QuestItemSpawnData();

            InitializeComponent();
            LoadData();
        }

        private void InitializeComponent()
        {
            this.Text = "Редактирование локации спавна";
            this.Width = 400;
            this.Height = 350;
            this.StartPosition = FormStartPosition.CenterParent;
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;

            int leftLabel = 12;
            int leftControl = 150;
            int top = 12;
            int vertGap = 30;

            // Локация
            var lblLocation = new Label { Text = "Локация:", Left = leftLabel, Top = top + 4, Width = 130 };
            cbLocation = new ComboBox
            {
                Left = leftControl,
                Top = top,
                Width = 200,
                DropDownStyle = ComboBoxStyle.DropDownList
            };
            top += vertGap;

            // Шанс спавна
            var lblSpawnChance = new Label { Text = "Шанс спавна (%):", Left = leftLabel, Top = top + 4, Width = 130 };
            nudSpawnChance = new NumericUpDown 
            { 
                Left = leftControl, 
                Top = top, 
                Width = 100, 
                Minimum = 1, 
                Maximum = 100,
                DecimalPlaces = 0
            };
            top += vertGap;

            // Количество предметов
            var lblQuantity = new Label { Text = "Количество:", Left = leftLabel, Top = top + 4, Width = 130 };
            nudQuantity = new NumericUpDown 
            { 
                Left = leftControl, 
                Top = top, 
                Width = 100, 
                Minimum = 1, 
                Maximum = 100,
                DecimalPlaces = 0
            };
            top += vertGap;

            // Максимальное количество на локации
            var lblMaxItems = new Label { Text = "Макс. на локации:", Left = leftLabel, Top = top + 4, Width = 130 };
            nudMaxItemsOnLocation = new NumericUpDown 
            { 
                Left = leftControl, 
                Top = top, 
                Width = 100, 
                Minimum = 1, 
                Maximum = 1000,
                DecimalPlaces = 0
            };
            top += vertGap;

            // Интервал спавна
            var lblSpawnInterval = new Label { Text = "Интервал спавна:", Left = leftLabel, Top = top + 4, Width = 130 };
            nudSpawnInterval = new NumericUpDown 
            { 
                Left = leftControl, 
                Top = top, 
                Width = 100, 
                Minimum = 1, 
                Maximum = 100,
                DecimalPlaces = 0
            };
            top += vertGap + 20;

            // Кнопки
            btnOk = new Button { Text = "OK", Left = 150, Top = top, Width = 80, DialogResult = DialogResult.OK };
            btnCancel = new Button { Text = "Отмена", Left = 240, Top = top, Width = 80, DialogResult = DialogResult.Cancel };

            btnOk.Click += BtnOk_Click;

            this.Controls.AddRange(new Control[]
            {
                lblLocation, cbLocation,
                lblSpawnChance, nudSpawnChance,
                lblQuantity, nudQuantity,
                lblMaxItems, nudMaxItemsOnLocation,
                lblSpawnInterval, nudSpawnInterval,
                btnOk, btnCancel
            });

            LoadLocations();
        }

        private void LoadLocations()
        {
            if (_gameData.Locations != null)
            {
                foreach (var location in _gameData.Locations.OrderBy(l => l.Name))
                {
                    cbLocation.Items.Add(new LocationComboItem(location));
                }
            }
        }

        private void LoadData()
        {
            // Загружаем данные существующего спавна
            if (SpawnData.LocationID > 0)
            {
                var selectedLocation = cbLocation.Items.Cast<LocationComboItem>()
                    .FirstOrDefault(l => l.LocationData.ID == SpawnData.LocationID);
                if (selectedLocation != null)
                    cbLocation.SelectedItem = selectedLocation;
            }

            // Устанавливаем значения с проверкой на допустимые диапазоны
            nudSpawnChance.Value = Math.Max(nudSpawnChance.Minimum, Math.Min(nudSpawnChance.Maximum, SpawnData.SpawnChance));
            nudQuantity.Value = Math.Max(nudQuantity.Minimum, Math.Min(nudQuantity.Maximum, SpawnData.Quantity));
            nudMaxItemsOnLocation.Value = Math.Max(nudMaxItemsOnLocation.Minimum, Math.Min(nudMaxItemsOnLocation.Maximum, SpawnData.MaxItemsOnLocation));
            nudSpawnInterval.Value = Math.Max(nudSpawnInterval.Minimum, Math.Min(nudSpawnInterval.Maximum, SpawnData.SpawnInterval));
        }

        private void BtnOk_Click(object sender, EventArgs e)
        {
            // Валидация
            if (cbLocation.SelectedItem == null)
            {
                MessageBox.Show("Выберите локацию для спавна.", "Ошибка валидации", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!(cbLocation.SelectedItem is LocationComboItem selectedLocation))
            {
                MessageBox.Show("Ошибка: выбранный элемент не является локацией.", "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            // Сохранение данных
            SpawnData.LocationID = selectedLocation.LocationData.ID;
            SpawnData.SpawnChance = (int)nudSpawnChance.Value;
            SpawnData.Quantity = (int)nudQuantity.Value;
            SpawnData.MaxItemsOnLocation = (int)nudMaxItemsOnLocation.Value;
            SpawnData.SpawnInterval = (int)nudSpawnInterval.Value;

            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        // Вспомогательный класс для ComboBox
        private class LocationComboItem
        {
            public LocationData LocationData { get; }
            public LocationComboItem(LocationData locationData) { LocationData = locationData; }
            public override string ToString() => $"{LocationData.Name} (ID: {LocationData.ID})";
        }
    }
}
