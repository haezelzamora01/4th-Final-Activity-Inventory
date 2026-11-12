using System.Data.OleDb;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Helpers;

namespace InventoryManagementSystem.Forms;

public sealed class RegisterForm : Form
{
    private readonly Label titleLabel = new();
    private readonly Label fullNameLabel = new();
    private readonly Label usernameLabel = new();
    private readonly Label passwordLabel = new();
    private readonly Label confirmPasswordLabel = new();

    private readonly TextBox txtFullName = new() { Name = "txtFullName" };
    private readonly TextBox txtUsername = new() { Name = "txtUsername" };
    private readonly TextBox txtPassword = new() { Name = "txtPassword" };
    private readonly TextBox txtConfirmPassword = new() { Name = "txtConfirmPassword" };
    private readonly Button btnRegister = new() { Name = "btnRegister" };
    private readonly Button btnBackToLogin = new() { Name = "btnBackToLogin" };

    public RegisterForm()
    {
        InitializeLayout();
        btnRegister.Click += RegisterButton_Click;
        btnBackToLogin.Click += (_, _) => Close();
    }

    private void InitializeLayout()
    {
        Text = "Create Account - Inventory Management System";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(460, 420);
        ClientSize = new Size(520, 430);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Color.WhiteSmoke;

        titleLabel.Text = "Create an account";
        titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        titleLabel.AutoSize = true;

        ConfigureLabel(fullNameLabel, "Full name");
        ConfigureLabel(usernameLabel, "Username");
        ConfigureLabel(passwordLabel, "Password");
        ConfigureLabel(confirmPasswordLabel, "Confirm password");

        txtFullName.Dock = DockStyle.Fill;
        txtUsername.Dock = DockStyle.Fill;
        txtPassword.Dock = DockStyle.Fill;
        txtConfirmPassword.Dock = DockStyle.Fill;
        txtPassword.UseSystemPasswordChar = true;
        txtConfirmPassword.UseSystemPasswordChar = true;

        btnRegister.Text = "Register";
        btnRegister.AutoSize = true;
        btnRegister.DialogResult = DialogResult.None;

        btnBackToLogin.Text = "Back to sign in";
        btnBackToLogin.AutoSize = true;
        btnBackToLogin.DialogResult = DialogResult.Cancel;

        TableLayoutPanel fields = new()
        {
            Dock = DockStyle.Fill,
            ColumnCount = 2,
            RowCount = 5,
            Padding = new Padding(24, 16, 24, 8)
        };
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 145));
        fields.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        for (int i = 0; i < fields.RowCount; i++)
        {
            fields.RowStyles.Add(new RowStyle(SizeType.Absolute, 46));
        }

        fields.Controls.Add(fullNameLabel, 0, 0);
        fields.Controls.Add(txtFullName, 1, 0);
        fields.Controls.Add(usernameLabel, 0, 1);
        fields.Controls.Add(txtUsername, 1, 1);
        fields.Controls.Add(passwordLabel, 0, 2);
        fields.Controls.Add(txtPassword, 1, 2);
        fields.Controls.Add(confirmPasswordLabel, 0, 3);
        fields.Controls.Add(txtConfirmPassword, 1, 3);

        FlowLayoutPanel buttons = new()
        {
            Dock = DockStyle.Fill,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(24, 8, 24, 16),
            AutoSize = true
        };
        buttons.Controls.Add(btnRegister);
        buttons.Controls.Add(btnBackToLogin);

        TableLayoutPanel main = new()
        {
            Dock = DockStyle.Fill,
            RowCount = 3,
            ColumnCount = 1,
            Padding = new Padding(0, 20, 0, 0)
        };
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 55));
        main.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        main.RowStyles.Add(new RowStyle(SizeType.Absolute, 65));
        main.Controls.Add(titleLabel, 0, 0);
        main.Controls.Add(fields, 0, 1);
        main.Controls.Add(buttons, 0, 2);
        titleLabel.Margin = new Padding(24, 0, 0, 0);

        Controls.Add(main);
        AcceptButton = btnRegister;
        CancelButton = btnBackToLogin;
    }

    private static void ConfigureLabel(Label label, string text)
    {
        label.Text = text;
        label.AutoSize = true;
        label.Anchor = AnchorStyles.Left;
        label.TextAlign = ContentAlignment.MiddleLeft;
    }

    private void RegisterButton_Click(object? sender, EventArgs e)
    {
        string fullName = txtFullName.Text.Trim();
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;
        string confirmPassword = txtConfirmPassword.Text;

        if (string.IsNullOrWhiteSpace(fullName)
            || string.IsNullOrWhiteSpace(username)
            || string.IsNullOrWhiteSpace(password)
            || string.IsNullOrWhiteSpace(confirmPassword))
        {
            MessageBox.Show(this, "Please complete all required fields.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (password.Length < 8)
        {
            MessageBox.Show(this, "Password must contain at least 8 characters.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (!password.Equals(confirmPassword, StringComparison.Ordinal))
        {
            MessageBox.Show(this, "The passwords do not match.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            using OleDbConnection connection = Database.OpenConnection();

            using OleDbCommand duplicateCommand = connection.CreateCommand();
            duplicateCommand.CommandText = "SELECT COUNT(*) FROM Users WHERE Username = ?";
            duplicateCommand.Parameters.Add(new OleDbParameter("@username", OleDbType.VarWChar, 100) { Value = username });

            if (Convert.ToInt32(duplicateCommand.ExecuteScalar()) > 0)
            {
                MessageBox.Show(this, "That username is already registered.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtUsername.Focus();
                return;
            }

            string passwordHash = PasswordHelper.HashPassword(password);
            using OleDbCommand insertCommand = connection.CreateCommand();
            insertCommand.CommandText = "INSERT INTO Users (FullName, Username, PasswordHash, UserRole, IsActive) VALUES (?, ?, ?, ?, ?)";
            insertCommand.Parameters.Add(new OleDbParameter("@fullName", OleDbType.VarWChar, 150) { Value = fullName });
            insertCommand.Parameters.Add(new OleDbParameter("@username", OleDbType.VarWChar, 100) { Value = username });
            insertCommand.Parameters.Add(new OleDbParameter("@passwordHash", OleDbType.VarWChar, 255) { Value = passwordHash });
            insertCommand.Parameters.Add(new OleDbParameter("@userRole", OleDbType.VarWChar, 30) { Value = "User" });
            insertCommand.Parameters.Add(new OleDbParameter("@isActive", OleDbType.Boolean) { Value = true });
            insertCommand.ExecuteNonQuery();

            MessageBox.Show(this, "Registration successful. You can now sign in.", "Registration", MessageBoxButtons.OK, MessageBoxIcon.Information);
            ClearFields();
        }
        catch (OleDbException ex)
        {
            MessageBox.Show(this, $"The account could not be saved to Access.\n\n{ex.Message}", "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Registration could not be completed.\n\n{ex.Message}", "Registration error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ClearFields()
    {
        txtFullName.Clear();
        txtUsername.Clear();
        txtPassword.Clear();
        txtConfirmPassword.Clear();
        txtFullName.Focus();
    }
}
