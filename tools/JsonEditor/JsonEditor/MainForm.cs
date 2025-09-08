using Engine.Core;
using Engine.Data; // <- замени на твой namespace DTO
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows.Forms;

public class MainForm : Form
{
    private Button btnLoad;
    private Button btnSave;
    private Button btnAdd;
    private Button btnEdit;
    private Button btnDelete;
    private DataGridView dgvItems;
    private BindingList<ItemData> itemsBinding;
    private GameData currentData;
    private string currentPath;

    public MainForm()
    {
        InitializeComponents();
    }

    private void InitializeComponents()
    {
        this.Text = "JsonEditor";
        this.Width = 900;
        this.Height = 600;

        btnLoad = new Button { Text = "Load", Left = 10, Top = 10, Width = 80 };
        btnSave = new Button { Text = "Save", Left = 100, Top = 10, Width = 80 };
        btnAdd = new Button { Text = "Add", Left = 200, Top = 10, Width = 80 };
        btnEdit = new Button { Text = "Edit", Left = 290, Top = 10, Width = 80 };
        btnDelete = new Button { Text = "Delete", Left = 380, Top = 10, Width = 80 };

        dgvItems = new DataGridView { Left = 10, Top = 50, Width = 860, Height = 480, ReadOnly = true, AllowUserToAddRows = false, SelectionMode = DataGridViewSelectionMode.FullRowSelect };

        btnLoad.Click += BtnLoad_Click;
        btnSave.Click += BtnSave_Click;
        btnAdd.Click += BtnAdd_Click;
        btnEdit.Click += BtnEdit_Click;
        btnDelete.Click += BtnDelete_Click;

        this.Controls.AddRange(new Control[] { btnLoad, btnSave, btnAdd, btnEdit, btnDelete, dgvItems });

        // колонки
        dgvItems.AutoGenerateColumns = false;
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "ID", HeaderText = "ID", Width = 60 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Name", HeaderText = "Name", Width = 250 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "NamePlural", HeaderText = "NamePlural", Width = 200 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Type", HeaderText = "Type", Width = 120 });
        dgvItems.Columns.Add(new DataGridViewTextBoxColumn { DataPropertyName = "Price", HeaderText = "Price", Width = 80 });
    }

    private void BtnLoad_Click(object? sender, EventArgs e)
    {
        using var ofd = new OpenFileDialog { Filter = "JSON files|*.json|All files|*.*" };
        if (ofd.ShowDialog() != DialogResult.OK) return;
        currentPath = ofd.FileName;
        try
        {
            currentData = SerializerHelper.LoadGameData(currentPath);
            itemsBinding = new BindingList<ItemData>(currentData.Items ?? new List<ItemData>());
            dgvItems.DataSource = itemsBinding;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка загрузки: {ex.Message}");
        }
    }

    private void BtnSave_Click(object? sender, EventArgs e)
    {
        if (currentPath == null)
        {
            MessageBox.Show("Сначала загрузите файл.");
            return;
        }

        // синхронизируем
        currentData.Items = itemsBinding.ToList();
        var errors = ValidateGameData(currentData);
        if (errors.Any())
        {
            MessageBox.Show("Ошибки в данных:\n" + string.Join("\n", errors.Take(20)));
            return;
        }

        try
        {
            SerializerHelper.SaveGameData(currentData, currentPath);
            MessageBox.Show("Сохранено");
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Ошибка сохранения: {ex.Message}");
        }
    }

    private void BtnAdd_Click(object? sender, EventArgs e)
    {
        var newItem = new ItemData { ID = NextId(), Name = "NewItem", NamePlural = "NewItems", Type = ItemType.Stuff, Price = 0 };
        using var ed = new ItemEditorForm(newItem);
        if (ed.ShowDialog() == DialogResult.OK)
        {
            itemsBinding.Add(ed.Item);
        }
    }

    private void BtnEdit_Click(object? sender, EventArgs e)
    {
        if (dgvItems.CurrentRow == null) return;
        var selected = (ItemData)dgvItems.CurrentRow.DataBoundItem!;
        var clone = CloneItem(selected);
        using var ed = new ItemEditorForm(clone);
        if (ed.ShowDialog() == DialogResult.OK)
        {
            // копируем обратно
            CopyItem(ed.Item, selected);
            dgvItems.Refresh();
        }
    }

    private void BtnDelete_Click(object? sender, EventArgs e)
    {
        if (dgvItems.CurrentRow == null) return;
        var selected = (ItemData)dgvItems.CurrentRow.DataBoundItem!;
        var ok = MessageBox.Show($"Удалить {selected.Name}?", "Confirm", MessageBoxButtons.YesNo) == DialogResult.Yes;
        if (ok) itemsBinding.Remove(selected);
    }

    private int NextId()
    {
        if (itemsBinding == null || itemsBinding.Count == 0) return 1;
        return itemsBinding.Max(i => i.ID) + 1;
    }

    private ItemData CloneItem(ItemData src) => new ItemData { ID = src.ID, Name = src.Name, NamePlural = src.NamePlural, Type = src.Type, Price = src.Price, Description = src.Description };

    private void CopyItem(ItemData from, ItemData to)
    {
        to.ID = from.ID; to.Name = from.Name; to.NamePlural = from.NamePlural; to.Type = from.Type; to.Price = from.Price; to.Description = from.Description;
    }

    // базовый валидатор (позже расширишь)
    private List<string> ValidateGameData(GameData data)
    {
        var errors = new List<string>();
        if (data.Items != null)
        {
            var dup = data.Items.GroupBy(i => i.ID).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
            if (dup.Any()) errors.Add("Duplicate Item IDs: " + string.Join(", ", dup));
        }
        // TODO: добавить проверки ссылок для квестов/локаций и т.п.
        return errors;
    }
}
