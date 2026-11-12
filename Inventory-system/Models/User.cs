namespace InventoryManagementSystem.Models;

public sealed class User
{
    public int UserID { get; init; }
    public string FullName { get; init; } = string.Empty;
    public string Username { get; init; } = string.Empty;
    public string UserRole { get; init; } = "User";
    public bool IsActive { get; init; }
}
