using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace EFCoreConsoleMastery.Core.Domain;

/// <summary>
/// Фабрика для CLI-інструментів EF Core (dotnet ef migrations add / database update).
/// </summary>
public class MasteryDbContextFactory : IDesignTimeDbContextFactory<MasteryDbContext>
{
    public MasteryDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<MasteryDbContext>();
        optionsBuilder.UseSqlServer(SqlServerDbHelper.ConnectionString);
        return new MasteryDbContext(optionsBuilder.Options);
    }
}
