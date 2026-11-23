namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>Країна розташування компанії (таблиця dbo.Countries).</summary>
public class Country
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public List<Company> Companies { get; set; } = new();
}
