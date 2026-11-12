using System.Data.OleDb;
using InventoryManagementSystem.Data;
using InventoryManagementSystem.Helpers;
using InventoryManagementSystem.Models;

namespace InventoryManagementSystem.Forms;

public sealed class SignInForm : Form
{
    private readonly Label titleLabel = new();
    private readonly Label subtitleLabel = new();
    private readonly Label usernameLabel = new();
    private readonly Label passwordLabel = new();

    private readonly TextBox txtUsername = new() { Name = "txtUsername" };
    private readonly TextBox txtPassword = new() { Name = "txtPassword" };
    private readonly Button btnSignIn = new() { Name = "btnSignIn" };
    private readonly Button btnRegister = new() { Name = "btnRegister" };

    public SignInForm()
    {
        InitializeLayout();
        btnSignIn.Click += SignInButton_Click;
        btnRegister.Click += RegisterButton_Click;
    }

    private void InitializeLayout()
    {
        Text = "Sign In - Inventory Management System";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(520, 430);
        ClientSize = new Size(520, 430);
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;
        BackColor = Color.WhiteSmoke;

        titleLabel.Text = "Inventory Management System";
        titleLabel.Font = new Font("Segoe UI", 18F, FontStyle.Bold);
        titleLabel.AutoSize = true;
        titleLabel.Location = new Point(26, 28);

        subtitleLabel.Text = "Sign in to continue";
        subtitleLabel.AutoSize = true;
        subtitleLabel.ForeColor = Color.DimGray;
        subtitleLabel.Location = new Point(26, 72);

        ConfigureLabel(usernameLabel, "Username");
        ConfigureLabel(passwordLabel, "Password");
        usernameLabel.Location = new Point(32, 145);
        passwordLabel.Location = new Point(32, 205);
        txtUsername.Location = new Point(143, 136);
        txtPassword.Location = new Point(143, 196);
        txtUsername.Size = new Size(344, 27);
        txtPassword.Size = new Size(344, 27);
        txtPassword.UseSystemPasswordChar = true;

        btnSignIn.Text = "Sign in";
        btnSignIn.AutoSize = true;
        btnSignIn.Location = new Point(390, 350);
        btnRegister.Text = "Create account";
        btnRegister.AutoSize = true;
        btnRegister.Location = new Point(266, 350);

        Controls.Add(titleLabel);
        Controls.Add(subtitleLabel);
        Controls.Add(usernameLabel);
        Controls.Add(passwordLabel);
        Controls.Add(txtUsername);
        Controls.Add(txtPassword);
        Controls.Add(btnRegister);
        Controls.Add(btnSignIn);
        AcceptButton = btnSignIn;
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
        using RegisterForm registerForm = new();
        registerForm.ShowDialog(this);
    }

    private void SignInButton_Click(object? sender, EventArgs e)
    {
        string username = txtUsername.Text.Trim();
        string password = txtPassword.Text;

        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
        {
            MessageBox.Show(this, "Please enter both your username and password.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            User? user = FindUser(username, password);
            if (user is null)
            {
                MessageBox.Show(this, "The username or password is incorrect.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                txtPassword.Clear();
                txtPassword.Focus();
                return;
            }

            if (!user.IsActive)
            {
                MessageBox.Show(this, "This account is inactive. Please contact an administrator.", "Sign in", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            MessageBox.Show(this, $"Welcome, {user.FullName}!", "Sign in successful", MessageBoxButtons.OK, MessageBoxIcon.Information);
            Hide();
            using DashboardForm dashboardForm = new(user);
            dashboardForm.ShowDialog(this);
            Show();
            txtPassword.Clear();
        }
        catch (OleDbException ex)
        {
            MessageBox.Show(this, $"The sign-in query could not be completed.\n\n{ex.Message}", "Database error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch (Exception ex)
        {
            MessageBox.Show(this, $"Sign-in could not be completed.\n\n{ex.Message}", "Sign-in error", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private static User? FindUser(string username, string password)
    {
        using OleDbConnection connection = Database.OpenConnection();

        using OleDbCommand command = connection.CreateCommand();
        command.CommandText = "SELECT UserID, FullName, Username, PasswordHash, UserRole, IsActive FROM Users WHERE Username = ?";
        command.Parameters.Add(new OleDbParameter("@username", OleDbType.VarWChar, 100) { Value = username });

        using OleDbDataReader reader = command.ExecuteReader();
        if (reader is null || !reader.Read())
        {
            return null;
        }

        string storedHash = Convert.ToString(reader["PasswordHash"]) ?? string.Empty;
        if (!PasswordHelper.VerifyPassword(password, storedHash))
        {
            return null;
        }

        return new User
        {
            UserID = Convert.ToInt32(reader["UserID"]),
            FullName = Convert.ToString(reader["FullName"]) ?? string.Empty,
            Username = Convert.ToString(reader["Username"]) ?? string.Empty,
            UserRole = Convert.ToString(reader["UserRole"]) ?? "User",
            IsActive = Convert.ToBoolean(reader["IsActive"])
        };
    }
}
