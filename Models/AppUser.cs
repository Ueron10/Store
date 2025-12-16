using SQLite;

namespace StoreProgram.Models;

public class AppUser
{
    [PrimaryKey]
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty; // plain text for simplicity in this assignment
    public string Role { get; set; } = "Employee"; // Owner or Employee
    public string? Email { get; set; }
    public string? Phone { get; set; }
}
