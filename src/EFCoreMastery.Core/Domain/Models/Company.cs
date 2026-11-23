namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Компанія (таблиця dbo.Companies).</summary>
public class Company
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int? CountryId { get; set; }
    public Country? Country { get; set; }
    public List<User> Users { get; set; } = new();
    public List<Product> Products { get; set; } = new();
}
