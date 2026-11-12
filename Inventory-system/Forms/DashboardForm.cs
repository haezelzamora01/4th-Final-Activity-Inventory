using System.Data.OleDb;
using System.Text;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Forms;

public sealed class DashboardForm : Form
{
    private const int LowStockLimit = 10;
    private readonly User loggedInUser;
    private readonly DataGridView productsGrid = new() { Name = "dgvProducts" };
    private readonly TextBox searchTextBox = new() { Name = "txtSearch" };
    private readonly TextBox productNameTextBox = new() { Name = "txtProductName" };
    private readonly TextBox descriptionTextBox = new() { Name = "txtDescription" };
    private readonly ComboBox categoryComboBox = new() { Name = "cmbCategory" };
    private readonly TextBox supplierTextBox = new() { Name = "txtSupplier" };
    private readonly NumericUpDown quantityInput = new() { Name = "nudQuantity" };
    private readonly NumericUpDown priceInput = new() { Name = "nudPrice" };
    private readonly Label welcomeLabel = new() { Name = "lblWelcome" };
    private readonly Label roleLabel = new() { Name = "lblRole" };
    private readonly Label totalItemsLabel = new() { Name = "lblTotalProducts" };
    private readonly Label totalValueLabel = new() { Name = "lblTotalValue" };
    private readonly Label lowStockLabel = new() { Name = "lblLowStock" };
    private readonly Label supplierCountLabel = new() { Name = "lblOutOfStock" };
    private readonly Label inStockLabel = new() { Name = "lblInStock" };
    private readonly Label statusLabel = new() { Name = "lblStatus" };
    private readonly ProgressBar busyIndicator = new() { Name = "prgDatabaseActivity" };
    private readonly Button saveButton = new() { Name = "btnSave" };
    private int? editingProductId;
    private bool showingLowStock;
    private bool databaseOperationInProgress;

    public DashboardForm(User user)
    {
        loggedInUser = user;
        InitializeLayout();
        Load += (_, _) => LoadProducts();
    }

    private void InitializeLayout()
    {
        Text = "Inventory Management System";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(1000, 700);
        ClientSize = new Size(1240, 820);
         BackColor = Color.FromArgb(242, 245, 249);
        Padding = new Padding(0);

        Panel accent = new()
        {
            Name = "pnlHeaderAccent",
            Dock = DockStyle.Top,
            Height = 5,
             BackColor = Color.FromArgb(79, 112, 154)
        };

        Panel header = new()
        {
            Name = "pnlHeader",
            Dock = DockStyle.Top,
            Height = 82,
            BackColor = Color.White,
            Padding = new Padding(28, 14, 28, 12)
        };

        Label appLabel = new()
        {
            Name = "lblAppName",
            Text = "INVENTORY HUB",
            AutoSize = true,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
             ForeColor = Color.FromArgb(79, 112, 154),
            Location = new Point(28, 12)
        };
        Label pageLabel = new()
        {
            Name = "lblPageTitle",
            Text = "Product Management",
            AutoSize = true,
            Font = new Font("Segoe UI", 18F, FontStyle.Bold),
             ForeColor = Color.FromArgb(39, 52, 67),
            Location = new Point(26, 30)
        };
        welcomeLabel.Text = $"Welcome, {loggedInUser.FullName}";
        welcomeLabel.Font = new Font("Segoe UI", 9F);
        welcomeLabel.ForeColor = Color.FromArgb(100, 116, 139);
        welcomeLabel.AutoSize = true;
        welcomeLabel.Location = new Point(0, 4);
        roleLabel.Text = loggedInUser.UserRole;
        roleLabel.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
         roleLabel.ForeColor = Color.FromArgb(79, 112, 154);
        roleLabel.AutoSize = true;
        roleLabel.Location = new Point(0, 27);

        Button logoutButton = new()
        {
            Name = "btnLogout",
            Text = "Log out",
            Width = 92,
            Height = 34,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            Anchor = AnchorStyles.Top | AnchorStyles.Right,
            Location = new Point(198, 10)
        };
        logoutButton.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        logoutButton.Click += LogoutButton_Click;

        Panel accountPanel = new()
        {
            Name = "pnlAccount",
            Dock = DockStyle.Right,
            Width = 310,
            Padding = new Padding(0, 10, 0, 0)
        };
        accountPanel.Controls.Add(welcomeLabel);
        accountPanel.Controls.Add(roleLabel);
        accountPanel.Controls.Add(logoutButton);
        header.Controls.Add(appLabel);
        header.Controls.Add(pageLabel);
        header.Controls.Add(accountPanel);

        TableLayoutPanel main = new()
        {
            Name = "tblDashboard",
            Dock = DockStyle.Fill,
            Padding = new Padding(28, 22, 28, 18),
            ColumnCount = 1,
            RowCount = 4,
             BackColor = Color.FromArgb(242, 245, 249)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 104));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 220));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));

        FlowLayoutPanel summaryFlow = new()
        {
            Name = "flpSummary",
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0)
        };
         summaryFlow.Controls.Add(CreateSummaryCard("Total Products", totalItemsLabel, Color.FromArgb(225, 235, 246)));
         summaryFlow.Controls.Add(CreateSummaryCard("Total Value", totalValueLabel, Color.FromArgb(231, 234, 244)));
         summaryFlow.Controls.Add(CreateSummaryCard("In Stock", inStockLabel, Color.FromArgb(225, 239, 231)));
         summaryFlow.Controls.Add(CreateSummaryCard("Low Stock", lowStockLabel, Color.FromArgb(246, 239, 218)));
         summaryFlow.Controls.Add(CreateSummaryCard("Out of Stock", supplierCountLabel, Color.FromArgb(246, 229, 229)));
        main.Controls.Add(summaryFlow, 0, 0);

        GroupBox editor = CreateEditor();
        main.Controls.Add(editor, 0, 1);

        GroupBox listCard = new()
        {
            Name = "grpProductList",
            Text = "Product List",
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            BackColor = Color.White,
             ForeColor = Color.FromArgb(39, 52, 67),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
        TableLayoutPanel listLayout = new() { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2 };
        listLayout.RowStyles.Add(new RowStyle(SizeType.Absolute, 42));
        listLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        FlowLayoutPanel listToolbar = new()
        {
            Name = "flpProductToolbar",
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            WrapContents = false,
            Padding = new Padding(0, 0, 0, 6)
        };
        Button refreshButton = CreateToolbarButton("btnRefresh", "Refresh", 82);
        refreshButton.Click += (_, _) => LoadProducts();
         Button lowButton = CreateToolbarButton("btnShowLowStock", "Low Stock", 88);
        lowButton.Click += (_, _) => { showingLowStock = true; LoadProducts(); };
         Button allButton = CreateToolbarButton("btnShowAll", "All Products", 96);
        allButton.Click += (_, _) => { showingLowStock = false; LoadProducts(); };
        Label searchLabel = new() { Name = "lblSearch", Text = "Search", AutoSize = true, ForeColor = Color.FromArgb(100, 116, 139), Margin = new Padding(12, 8, 6, 0) };
        searchTextBox.Width = 240;
        searchTextBox.Height = 32;
        searchTextBox.PlaceholderText = "Product name or ID";
        searchTextBox.Margin = new Padding(0, 2, 8, 0);
        searchTextBox.TextChanged += (_, _) => LoadProducts();
        listToolbar.Controls.Add(refreshButton);
        listToolbar.Controls.Add(lowButton);
        listToolbar.Controls.Add(allButton);
        listToolbar.Controls.Add(searchTextBox);
        listToolbar.Controls.Add(searchLabel);
        listLayout.Controls.Add(listToolbar, 0, 0);
        ConfigureGrid();
        productsGrid.Dock = DockStyle.Fill;
        productsGrid.BackgroundColor = Color.White;
        listLayout.Controls.Add(productsGrid, 0, 1);
        listCard.Controls.Add(listLayout);
        main.Controls.Add(listCard, 0, 2);

        FlowLayoutPanel statusBar = new()
        {
            Name = "flpStatusBar",
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.LeftToRight,
            WrapContents = false,
            Padding = new Padding(0, 3, 0, 0)
        };
        busyIndicator.Style = ProgressBarStyle.Marquee;
        busyIndicator.MarqueeAnimationSpeed = 25;
        busyIndicator.Width = 110;
        busyIndicator.Height = 18;
        busyIndicator.Visible = false;
        busyIndicator.Margin = new Padding(0, 2, 10, 0);
        statusLabel.Text = "Ready";
        statusLabel.AutoSize = true;
        statusLabel.ForeColor = Color.FromArgb(100, 116, 139);
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusBar.Controls.Add(busyIndicator);
        statusBar.Controls.Add(statusLabel);
        main.Controls.Add(statusBar, 0, 3);

        Controls.Add(main);
        Controls.Add(header);
        Controls.Add(accent);
    }

    private static Button CreateMenuButton(string text, EventHandler clickHandler)
    {
        Button button = new()
        {
            Text = text,
            Width = 66,
            Height = 28,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            Margin = new Padding(0),
            TextAlign = ContentAlignment.MiddleCenter
        };
        button.FlatAppearance.BorderSize = 0;
        button.Click += clickHandler;
        return button;
    }

    private static Button CreateSideButton(string text, EventHandler clickHandler)
    {
        Button button = new()
        {
            Text = text,
            Width = 155,
            Height = 36,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Color.FromArgb(223, 230, 238),
            BackColor = Color.FromArgb(32, 42, 54),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(12, 0, 0, 0),
            Margin = new Padding(0, 0, 0, 3)
        };
        button.FlatAppearance.BorderSize = 0;
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(53, 68, 84);
        button.Click += clickHandler;
        return button;
    }

    private static Panel CreateSummaryCard(string title, Label valueLabel, Color color)
    {
        Panel panel = new()
        {
            Name = "pnl" + title.Replace(" ", string.Empty),
            Width = 220,
            Height = 88,
            BorderStyle = BorderStyle.FixedSingle,
            BackColor = color,
            Margin = new Padding(0, 0, 12, 0),
            Padding = new Padding(16, 10, 16, 8)
        };
        Label titleLabel = new()
        {
            Name = "lbl" + title.Replace(" ", string.Empty),
            Text = title,
            AutoSize = false,
            Height = 24,
            Font = new Font("Segoe UI", 9F, FontStyle.Bold),
            ForeColor = Color.FromArgb(51, 65, 85),
            Dock = DockStyle.Top,
            TextAlign = ContentAlignment.MiddleLeft
        };
        valueLabel.Text = "0";
         valueLabel.Font = title.Equals("Total Value", StringComparison.OrdinalIgnoreCase)
             ? new Font("Segoe UI", 12F, FontStyle.Bold)
            : new Font("Segoe UI", 23F, FontStyle.Bold);
        valueLabel.AutoSize = false;
        valueLabel.Dock = DockStyle.Fill;
        valueLabel.TextAlign = ContentAlignment.MiddleLeft;
        valueLabel.AutoEllipsis = false;
        panel.Controls.Add(valueLabel);
        panel.Controls.Add(titleLabel);
        return panel;
    }

    private static Button CreateToolbarButton(string name, String text, int width)
    {
        Button button = new()
        {
            Name = name,
            Text = text,
            Width = width,
            Height = 30,
            FlatStyle = FlatStyle.Flat,
            BackColor = Color.White,
            ForeColor = Color.FromArgb(51, 65, 85),
            Margin = new Padding(6, 2, 0, 0)
        };
        button.FlatAppearance.BorderColor = Color.FromArgb(203, 213, 225);
        button.FlatAppearance.MouseOverBackColor = Color.FromArgb(239, 246, 255);
        return button;
    }

    private GroupBox CreateEditor()
    {
        GroupBox editor = new()
        {
            Name = "grpProductEditor",
            Text = "Add or Edit Product",
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            BackColor = Color.White,
            ForeColor = Color.FromArgb(15, 23, 42),
            Font = new Font("Segoe UI", 10F, FontStyle.Bold)
        };
         TableLayoutPanel layout = new() { Dock = DockStyle.Fill, ColumnCount = 6, RowCount = 3 };
         for (int i = 0; i < 6; i++) layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 16.6667F));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 25));
        layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
        layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        productNameTextBox.Dock = DockStyle.Fill;
        productNameTextBox.Font = new Font("Segoe UI", 10F);
        productNameTextBox.PlaceholderText = "Enter product name";
         descriptionTextBox.Dock = DockStyle.Fill;
         descriptionTextBox.Font = new Font("Segoe UI", 10F);
         descriptionTextBox.PlaceholderText = "Optional description";
        supplierTextBox.Dock = DockStyle.Fill;
        supplierTextBox.Font = new Font("Segoe UI", 10F);
        supplierTextBox.PlaceholderText = "Optional supplier";
        categoryComboBox.Dock = DockStyle.Fill;
        categoryComboBox.Font = new Font("Segoe UI", 10F);
        categoryComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        categoryComboBox.Items.AddRange(new object[] { "Supplies", "Office", "Electronics", "Furniture", "Other" });
         categoryComboBox.SelectedIndex = -1;
        quantityInput.Dock = DockStyle.Fill;
        quantityInput.Font = new Font("Segoe UI", 10F);
        quantityInput.Maximum = 1_000_000;
        priceInput.Dock = DockStyle.Fill;
        priceInput.Font = new Font("Segoe UI", 10F);
        priceInput.Maximum = 1_000_000_000;
        priceInput.DecimalPlaces = 2;
        priceInput.ThousandsSeparator = true;

        layout.Controls.Add(CreateEditorLabel("Product Name"), 0, 0);
         layout.Controls.Add(CreateEditorLabel("Description"), 1, 0);
         layout.Controls.Add(CreateEditorLabel("Category"), 2, 0);
         layout.Controls.Add(CreateEditorLabel("Supplier"), 3, 0);
         layout.Controls.Add(CreateEditorLabel("Quantity"), 4, 0);
         layout.Controls.Add(CreateEditorLabel("Unit Price"), 5, 0);
        layout.Controls.Add(productNameTextBox, 0, 1);
         layout.Controls.Add(descriptionTextBox, 1, 1);
         layout.Controls.Add(categoryComboBox, 2, 1);
         layout.Controls.Add(supplierTextBox, 3, 1);
         layout.Controls.Add(quantityInput, 4, 1);
         layout.Controls.Add(priceInput, 5, 1);

        FlowLayoutPanel buttons = new() { Dock = DockStyle.Fill, FlowDirection = FlowDirection.LeftToRight, WrapContents = false, Padding = new Padding(0, 8, 0, 0) };
         saveButton.Text = "Add Product";
         saveButton.Width = 112;
         saveButton.Height = 32;
         saveButton.FlatStyle = FlatStyle.Flat;
          saveButton.BackColor = Color.FromArgb(79, 112, 154);
         saveButton.ForeColor = Color.White;
         saveButton.FlatAppearance.BorderSize = 0;
        saveButton.Click += SaveButton_Click;
          Button clearButton = new() { Name = "btnClear", Text = "Clear", Width = 82, Height = 32 };
        clearButton.Click += (_, _) => ClearEditor();
          Button cancelButton = new() { Name = "btnCancelEdit", Text = "Cancel", Width = 82, Height = 32 };
        cancelButton.Click += (_, _) => ClearEditor();
          Button editButton = new() { Name = "btnUpdate", Text = "Edit Selected", Width = 112, Height = 32 };
        editButton.Click += (_, _) => BeginEdit();
          Button deleteButton = new() { Name = "btnDelete", Text = "Delete", Width = 82, Height = 32 };
         deleteButton.FlatStyle = FlatStyle.Flat;
         deleteButton.ForeColor = Color.FromArgb(185, 28, 28);
         deleteButton.FlatAppearance.BorderColor = Color.FromArgb(252, 165, 165);
        deleteButton.Click += (_, _) => DeleteSelected();
           Button stockInButton = new() { Name = "btnStockIn", Text = "Stock In", Width = 94, Height = 32 };
         stockInButton.FlatStyle = FlatStyle.Flat;
         stockInButton.BackColor = Color.FromArgb(232, 241, 235);
         stockInButton.ForeColor = Color.FromArgb(45, 93, 64);
         stockInButton.FlatAppearance.BorderColor = Color.FromArgb(160, 194, 171);
        stockInButton.Click += (_, _) => AdjustSelectedStock(1);
           Button stockOutButton = new() { Name = "btnStockOut", Text = "Stock Out", Width = 100, Height = 32 };
         stockOutButton.FlatStyle = FlatStyle.Flat;
         stockOutButton.BackColor = Color.FromArgb(247, 235, 232);
         stockOutButton.ForeColor = Color.FromArgb(128, 65, 57);
         stockOutButton.FlatAppearance.BorderColor = Color.FromArgb(211, 166, 157);
        stockOutButton.Click += (_, _) => AdjustSelectedStock(-1);
           Button reportButton = new() { Name = "btnReport", Text = "View Report", Width = 98, Height = 32 };
        reportButton.Click += (_, _) => ShowReport();
        buttons.Controls.Add(saveButton);
        buttons.Controls.Add(clearButton);
        buttons.Controls.Add(cancelButton);
        buttons.Controls.Add(editButton);
        buttons.Controls.Add(deleteButton);
        buttons.Controls.Add(stockInButton);
        buttons.Controls.Add(stockOutButton);
        buttons.Controls.Add(reportButton);
        layout.Controls.Add(buttons, 0, 2);
         layout.SetColumnSpan(buttons, 6);
        editor.Controls.Add(layout);
        return editor;
    }

    private static Label CreateEditorLabel(string text)
    {
        return new Label
        {
            Name = "lbl" + text.Replace(" ", string.Empty),
            Text = text,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(2, 0, 0, 0)
        };
    }

    private void ConfigureGrid()
    {
        productsGrid.AutoGenerateColumns = false;
        productsGrid.AllowUserToAddRows = false;
        productsGrid.ReadOnly = true;
        productsGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        productsGrid.MultiSelect = false;
        productsGrid.BackgroundColor = Color.White;
        productsGrid.RowHeadersVisible = false;
        productsGrid.ColumnHeadersHeight = 32;
        productsGrid.EnableHeadersVisualStyles = false;
         productsGrid.ColumnHeadersDefaultCellStyle.BackColor = Color.FromArgb(236, 240, 244);
         productsGrid.ColumnHeadersDefaultCellStyle.ForeColor = Color.FromArgb(55, 70, 87);
         productsGrid.ColumnHeadersDefaultCellStyle.SelectionBackColor = Color.FromArgb(236, 240, 244);
         productsGrid.ColumnHeadersDefaultCellStyle.SelectionForeColor = Color.FromArgb(55, 70, 87);
        productsGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
        productsGrid.ColumnHeadersDefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        productsGrid.DefaultCellStyle.BackColor = Color.White;
         productsGrid.DefaultCellStyle.ForeColor = Color.FromArgb(45, 58, 72);
          productsGrid.DefaultCellStyle.SelectionBackColor = Color.FromArgb(224, 233, 241);
          productsGrid.DefaultCellStyle.SelectionForeColor = Color.FromArgb(30, 45, 60);
         productsGrid.DefaultCellStyle.Padding = new Padding(6, 4, 6, 4);
         productsGrid.DefaultCellStyle.WrapMode = DataGridViewTriState.False;
         productsGrid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(247, 249, 251);
        productsGrid.RowTemplate.Height = 32;
        productsGrid.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;
        productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "RowNumber", HeaderText = "#", DataPropertyName = "DisplayNumber", Width = 48 });
         productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductID", HeaderText = "Product ID", DataPropertyName = "ProductID", Width = 82 });
        productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "ProductName", HeaderText = "Product Name", DataPropertyName = "ProductName", AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill });
         productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Description", HeaderText = "Description", DataPropertyName = "Description", Width = 180 });
        productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Category", HeaderText = "Category", DataPropertyName = "Category", Width = 120 });
        productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Supplier", HeaderText = "Supplier", DataPropertyName = "Supplier", Width = 170 });
        productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Quantity", HeaderText = "Quantity", DataPropertyName = "Quantity", Width = 90, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight } });
          productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "Price", HeaderText = "Unit Price", DataPropertyName = "Price", Width = 125, DefaultCellStyle = new DataGridViewCellStyle { Alignment = DataGridViewContentAlignment.MiddleRight, Format = "PHP #,##0.00" } });
         productsGrid.Columns.Add(new DataGridViewTextBoxColumn { Name = "CreatedAt", HeaderText = "Date Added", DataPropertyName = "CreatedAt", Width = 160, AutoSizeMode = DataGridViewAutoSizeColumnMode.None, DefaultCellStyle = new DataGridViewCellStyle { Format = "MMM dd, yyyy", WrapMode = DataGridViewTriState.False } });
        productsGrid.DataError += (_, e) =>
        {
            e.ThrowException = false;
            statusLabel.Text = "A product value could not be displayed";
        };
        productsGrid.CellDoubleClick += (_, e) => { if (e.RowIndex >= 0) BeginEdit(); };
    }

    private void LoadProducts()
    {
        try
        {
            Database.EnsureProductCreatedAtColumn();
            List<ProductRecord> products = new();
            using OleDbConnection connection = Database.OpenConnection();
            using OleDbCommand command = connection.CreateCommand();
             command.CommandText = "SELECT ProductID, ProductName, Description, Category, Supplier, Quantity, Price, CreatedAt FROM Products ORDER BY ProductID";
            using OleDbDataReader reader = command.ExecuteReader();
            while (reader.Read())
            {
                products.Add(new ProductRecord
                {
                    ProductID = Convert.ToInt32(reader["ProductID"]),
                    ProductName = Convert.ToString(reader["ProductName"]) ?? string.Empty,
                     Description = Convert.ToString(reader["Description"]) ?? string.Empty,
                    Category = Convert.ToString(reader["Category"]) ?? string.Empty,
                    Supplier = Convert.ToString(reader["Supplier"]) ?? string.Empty,
                    Quantity = Convert.ToInt32(reader["Quantity"]),
                    Price = Convert.ToDecimal(reader["Price"]),
                    CreatedAt = Convert.ToDateTime(reader["CreatedAt"])
                });
            }

            string search = searchTextBox.Text.Trim();
            IEnumerable<ProductRecord> visible = products;
            if (showingLowStock) visible = visible.Where(p => p.Quantity > 0 && p.Quantity <= LowStockLimit);
            if (!string.IsNullOrWhiteSpace(search)) visible = visible.Where(p => p.ProductName.Contains(search, StringComparison.OrdinalIgnoreCase) || p.ProductID.ToString().Contains(search, StringComparison.OrdinalIgnoreCase));
            List<ProductRecord> displayedProducts = visible.ToList();
            for (int index = 0; index < displayedProducts.Count; index++)
            {
                displayedProducts[index].DisplayNumber = index + 1;
            }

            productsGrid.DataSource = null;
            productsGrid.DataSource = displayedProducts;
            productsGrid.ClearSelection();
            productsGrid.Refresh();
            totalItemsLabel.Text = products.Count.ToString("N0");
             decimal totalValue = products.Sum(p => p.Quantity * p.Price);
             totalValueLabel.Text = $"PHP {totalValue:N2}";
            inStockLabel.Text = products.Count(p => p.Quantity > LowStockLimit).ToString("N0");
            lowStockLabel.Text = products.Count(p => p.Quantity > 0 && p.Quantity <= LowStockLimit).ToString("N0");
            supplierCountLabel.Text = products.Count(p => p.Quantity == 0).ToString("N0");
            statusLabel.Text = $"Ready | Logged as {loggedInUser.UserRole}";
        }
        catch (Exception ex)
        {
            statusLabel.Text = "Could not load products";
            MessageBox.Show(this, $"The products could not be loaded.\n\n{ex.Message}", "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void RefreshProductList(string statusMessage)
    {
        LoadProducts();
        productsGrid.ClearSelection();
        productsGrid.Refresh();
        statusLabel.Text = statusMessage;
    }

    private void SetBusy(bool isBusy, string message)
    {
        databaseOperationInProgress = isBusy;
        busyIndicator.Visible = isBusy;
        statusLabel.Text = message;
        saveButton.Enabled = !isBusy;
        productNameTextBox.Enabled = !isBusy;
         descriptionTextBox.Enabled = !isBusy;
        categoryComboBox.Enabled = !isBusy;
        supplierTextBox.Enabled = !isBusy;
        quantityInput.Enabled = !isBusy;
        priceInput.Enabled = !isBusy;
        productsGrid.Enabled = !isBusy;
        UseWaitCursor = isBusy;
    }

    private async void SaveButton_Click(object? sender, EventArgs e)
    {
        if (databaseOperationInProgress) return;

        if (string.IsNullOrWhiteSpace(productNameTextBox.Text))
        {
            MessageBox.Show(this, "Enter a product name.", "Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            productNameTextBox.Focus();
            return;
        }

        if (categoryComboBox.SelectedIndex < 0)
        {
            MessageBox.Show(this, "Select a product category.", "Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            categoryComboBox.Focus();
            return;
        }

        bool wasEditing = editingProductId.HasValue;
        int? productId = editingProductId;
        string productName = productNameTextBox.Text.Trim();
         string description = descriptionTextBox.Text.Trim();
        string category = categoryComboBox.Text;
        string supplier = supplierTextBox.Text.Trim();
        int quantity = (int)quantityInput.Value;
        decimal price = priceInput.Value;

        SetBusy(true, wasEditing ? "Updating product..." : "Adding product...");
        try
        {
             await Task.Run(() => SaveProductToDatabase(productId, productName, description, category, supplier, quantity, price));
            ClearEditor();
            RefreshProductList(wasEditing ? "Product updated successfully" : "Product added successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The product could not be saved.\n\n{ex.Message}", "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, statusLabel.Text);
        }
    }

    private static void SaveProductToDatabase(int? productId, string productName, string description, string category, string supplier, int quantity, decimal price)
    {
        using OleDbConnection connection = Database.OpenConnection();
        using OleDbCommand command = connection.CreateCommand();
        if (productId.HasValue)
        {
            command.CommandText = "UPDATE Products SET ProductName = ?, Description = ?, Category = ?, Supplier = ?, Quantity = ?, Price = ? WHERE ProductID = ?";
            command.Parameters.Add(new OleDbParameter("@name", OleDbType.VarWChar, 150) { Value = productName });
            command.Parameters.Add(new OleDbParameter("@description", OleDbType.VarWChar, 255) { Value = description });
            command.Parameters.Add(new OleDbParameter("@category", OleDbType.VarWChar, 50) { Value = category });
            command.Parameters.Add(new OleDbParameter("@supplier", OleDbType.VarWChar, 100) { Value = supplier });
            command.Parameters.Add(new OleDbParameter("@quantity", OleDbType.Integer) { Value = quantity });
            command.Parameters.Add(new OleDbParameter("@price", OleDbType.Currency) { Value = price });
            command.Parameters.Add(new OleDbParameter("@id", OleDbType.Integer) { Value = productId.Value });
        }
        else
        {
            command.CommandText = "INSERT INTO Products (ProductName, Description, Category, Supplier, Quantity, Price, CreatedAt) VALUES (?, ?, ?, ?, ?, ?, ?)";
            command.Parameters.Add(new OleDbParameter("@name", OleDbType.VarWChar, 150) { Value = productName });
            command.Parameters.Add(new OleDbParameter("@description", OleDbType.VarWChar, 255) { Value = description });
            command.Parameters.Add(new OleDbParameter("@category", OleDbType.VarWChar, 50) { Value = category });
            command.Parameters.Add(new OleDbParameter("@supplier", OleDbType.VarWChar, 100) { Value = supplier });
            command.Parameters.Add(new OleDbParameter("@quantity", OleDbType.Integer) { Value = quantity });
            command.Parameters.Add(new OleDbParameter("@price", OleDbType.Currency) { Value = price });
            command.Parameters.Add(new OleDbParameter("@createdAt", OleDbType.Date) { Value = DateTime.Now });
        }

        command.ExecuteNonQuery();
    }

    private void BeginEdit()
    {
        if (productsGrid.CurrentRow?.DataBoundItem is not ProductRecord product)
        {
            MessageBox.Show(this, "Select a product row to edit.", "Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        editingProductId = product.ProductID;
        productNameTextBox.Text = product.ProductName;
         descriptionTextBox.Text = product.Description;
        int categoryIndex = categoryComboBox.Items.IndexOf(product.Category);
        if (categoryIndex < 0 && !string.IsNullOrWhiteSpace(product.Category))
        {
            categoryComboBox.Items.Add(product.Category);
            categoryIndex = categoryComboBox.Items.Count - 1;
        }
        categoryComboBox.SelectedIndex = categoryIndex;
        supplierTextBox.Text = product.Supplier;
        quantityInput.Value = product.Quantity;
        priceInput.Value = product.Price;
        saveButton.Text = "Update Product";
        statusLabel.Text = $"Editing product {product.ProductID}";
    }

    private async void DeleteSelected()
    {
        if (databaseOperationInProgress) return;

        if (productsGrid.CurrentRow?.DataBoundItem is not ProductRecord product)
        {
            MessageBox.Show(this, "Select a product row to delete.", "Product", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }
        if (MessageBox.Show(this, $"Delete {product.ProductName}? This cannot be undone.", "Delete item", MessageBoxButtons.YesNo, MessageBoxIcon.Question) != DialogResult.Yes) return;

        SetBusy(true, "Deleting product...");
        try
        {
            await Task.Run(() => DeleteProductFromDatabase(product.ProductID));
            ClearEditor();
            RefreshProductList("Product deleted successfully");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The product could not be deleted.\n\n{ex.Message}", "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, statusLabel.Text);
        }
    }

    private static void DeleteProductFromDatabase(int productId)
    {
        using OleDbConnection connection = Database.OpenConnection();
        using OleDbCommand command = connection.CreateCommand();
        command.CommandText = "DELETE FROM Products WHERE ProductID = ?";
        command.Parameters.Add(new OleDbParameter("@id", OleDbType.Integer) { Value = productId });
        command.ExecuteNonQuery();
    }

    private void AdjustSelectedStock(int direction)
    {
        if (productsGrid.CurrentRow?.DataBoundItem is not ProductRecord product)
        {
            MessageBox.Show(this, "Select a product first.", "Stock adjustment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        using NumericInputDialog dialog = new(direction > 0 ? "Stock in" : "Stock out");
        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        int amount = dialog.Amount;
        if (direction < 0 && amount > product.Quantity)
        {
            MessageBox.Show(this, $"Only {product.Quantity} units are available.", "Stock adjustment", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using OleDbConnection connection = Database.OpenConnection();
            using OleDbTransaction transaction = connection.BeginTransaction();
            using OleDbCommand update = connection.CreateCommand();
            update.Transaction = transaction;
            update.CommandText = "UPDATE Products SET Quantity = Quantity + ? WHERE ProductID = ?";
            update.Parameters.Add(new OleDbParameter("@amount", OleDbType.Integer) { Value = direction * amount });
            update.Parameters.Add(new OleDbParameter("@id", OleDbType.Integer) { Value = product.ProductID });
            update.ExecuteNonQuery();
            using OleDbCommand log = connection.CreateCommand();
            log.Transaction = transaction;
            log.CommandText = "INSERT INTO StockTransactions (ProductID, TransactionType, Quantity, TransactionDate, UserID) VALUES (?, ?, ?, ?, ?)";
            log.Parameters.Add(new OleDbParameter("@productId", OleDbType.Integer) { Value = product.ProductID });
            log.Parameters.Add(new OleDbParameter("@type", OleDbType.VarWChar, 10) { Value = direction > 0 ? "IN" : "OUT" });
            log.Parameters.Add(new OleDbParameter("@quantity", OleDbType.Integer) { Value = amount });
            log.Parameters.Add(new OleDbParameter("@date", OleDbType.Date) { Value = DateTime.Now });
            log.Parameters.Add(new OleDbParameter("@userId", OleDbType.Integer) { Value = loggedInUser.UserID });
            log.ExecuteNonQuery();
            transaction.Commit();
            RefreshProductList($"{product.ProductName}: {(direction > 0 ? "+" : "-")}{amount} units");
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The stock adjustment could not be saved.\n\n{ex.Message}", "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ShowReport()
    {
        decimal totalValue = 0;
        int totalProducts = 0;
        List<string> lowStockNames = new();
        foreach (DataGridViewRow row in productsGrid.Rows)
        {
            if (row.DataBoundItem is not ProductRecord product) continue;
            totalProducts++;
            totalValue += product.Quantity * product.Price;
            if (product.Quantity <= LowStockLimit) lowStockNames.Add(product.ProductName);
        }
         MessageBox.Show(this, $"Products shown: {totalProducts}\nTotal value: PHP {totalValue:N2}\nLow-stock products: {(lowStockNames.Count == 0 ? "None" : string.Join(", ", lowStockNames))}", "Inventory Report", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void ExportCsv()
    {
        using SaveFileDialog dialog = new()
        {
            Filter = "CSV files (*.csv)|*.csv|All files (*.*)|*.*",
            FileName = "inventory.csv",
            Title = "Export inventory"
        };

        if (dialog.ShowDialog(this) != DialogResult.OK) return;

        try
        {
            using OleDbConnection connection = Database.OpenConnection();
            using OleDbCommand command = connection.CreateCommand();
             command.CommandText = "SELECT ProductID, ProductName, Description, Category, Supplier, Quantity, Price, CreatedAt FROM Products ORDER BY ProductID";
            using OleDbDataReader reader = command.ExecuteReader();
             StringBuilder csv = new("ID,Name,Description,Category,Supplier,Qty,UnitPrice,DateAdded\r\n");
            while (reader.Read())
            {
                csv.AppendLine(string.Join(",",
                    CsvValue(reader["ProductID"]),
                    CsvValue(reader["ProductName"]),
                     CsvValue(reader["Description"]),
                    CsvValue(reader["Category"]),
                    CsvValue(reader["Supplier"]),
                    CsvValue(reader["Quantity"]),
                    CsvValue(reader["Price"]),
                    CsvValue(reader["CreatedAt"])));
            }

            File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);
            statusLabel.Text = $"Exported {Path.GetFileName(dialog.FileName)}";
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"The inventory could not be exported.\n\n{ex.Message}", "Export error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static string CsvValue(object value)
    {
        string text = Convert.ToString(value, System.Globalization.CultureInfo.InvariantCulture) ?? string.Empty;
        return $"\"{text.Replace("\"", "\"\"")}\"";
    }

    private void LogoutButton_Click(object? sender, EventArgs e)
    {
        if (MessageBox.Show(this, "Are you sure you want to log out?", "Logout", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
        {
            Close();
        }
    }

    private void ClearEditor()
    {
        editingProductId = null;
        productNameTextBox.Clear();
         descriptionTextBox.Clear();
        supplierTextBox.Clear();
         categoryComboBox.SelectedIndex = -1;
        quantityInput.Value = 0;
        priceInput.Value = 0;
         saveButton.Text = "Add Product";
    }

    private sealed class ProductRecord
    {
        public int DisplayNumber { get; set; }
        public int ProductID { get; init; }
        public string ProductName { get; init; } = string.Empty;
         public string Description { get; init; } = string.Empty;
        public string Category { get; init; } = string.Empty;
        public string Supplier { get; init; } = string.Empty;
        public int Quantity { get; init; }
        public decimal Price { get; init; }
        public DateTime CreatedAt { get; init; }
    }

    private sealed class NumericInputDialog : Form
    {
        private readonly NumericUpDown amountInput = new() { Minimum = 1, Maximum = 1_000_000, Value = 1 };
        public int Amount => (int)amountInput.Value;

        public NumericInputDialog(string title)
        {
            Text = title;
            StartPosition = FormStartPosition.CenterParent;
            ClientSize = new Size(260, 120);
            FormBorderStyle = FormBorderStyle.FixedDialog;
            MaximizeBox = false;
            amountInput.Dock = DockStyle.Top;
            Button ok = new() { Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
            Button cancel = new() { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
            FlowLayoutPanel buttons = new() { Dock = DockStyle.Bottom, Height = 45, FlowDirection = FlowDirection.RightToLeft };
            buttons.Controls.Add(ok);
            buttons.Controls.Add(cancel);
            Controls.Add(amountInput);
            Controls.Add(buttons);
            AcceptButton = ok;
            CancelButton = cancel;
        }
    }
}
