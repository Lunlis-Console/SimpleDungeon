using System;
using System.Windows.Forms;
using Engine.Data; // замените если у вас namespace другой

public class ItemEditorForm : Form
{
    private PropertyGrid propertyGrid;
    private Button btnOk;
    private Button btnCancel;
    public ItemData Item { get; private set; }

    public ItemEditorForm(ItemData item)
    {
        Item = item;
        Initialize();
        propertyGrid.SelectedObject = Item;
    }

    private void Initialize()
    {
        this.Width = 500;
        this.Height = 600;
        propertyGrid = new PropertyGrid { Dock = DockStyle.Top, Height = 480 };
        btnOk = new Button { Text = "OK", Left = 300, Width = 80, Top = 490 };
        btnCancel = new Button { Text = "Cancel", Left = 390, Width = 80, Top = 490 };

        btnOk.Click += (s, e) => { if (ValidateItem()) { this.DialogResult = DialogResult.OK; this.Close(); } };
        btnCancel.Click += (s, e) => { this.DialogResult = DialogResult.Cancel; this.Close(); };

        this.Controls.Add(propertyGrid);
        this.Controls.Add(btnOk);
        this.Controls.Add(btnCancel);
    }

    private bool ValidateItem()
    {
        if (string.IsNullOrWhiteSpace(Item.Name))
        {
            MessageBox.Show("Введите имя предмета");
            return false;
        }
        // другие проверки при желании
        return true;
    }
}
