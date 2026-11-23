namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Профіль користувача 1:1 (таблиця dbo.UserProfiles).</summary>
public class UserProfile
{
    public int Id { get; set; }
    public string Login { get; set; } = string.Empty;
    public string? Bio { get; set; }
    public int UserId { get; set; }
    public User? User { get; set; }
}
