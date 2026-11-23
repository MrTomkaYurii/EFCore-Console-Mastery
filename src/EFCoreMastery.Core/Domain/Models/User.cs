namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Користувач (таблиця dbo.Users).</summary>
public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Age { get; set; }
    public int? CompanyId { get; set; }
    public Company? Company { get; set; }
    public int? RoleId { get; set; }
    public Role? Role { get; set; }
    public UserProfile? Profile { get; set; }
}
