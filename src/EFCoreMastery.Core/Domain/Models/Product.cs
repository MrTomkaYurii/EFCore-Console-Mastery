namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Продукт компанії (таблиця dbo.Products).</summary>
public class Product
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public int CompanyId { get; set; }
    public Company? Company { get; set; }
}
