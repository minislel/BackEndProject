using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Infrastructure.EF;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();

        // Tu wstaw connection string, np. do lokalnej bazy
        optionsBuilder.UseSqlite("Data Source=C:\\data\\app.db");

        return new AppDbContext(optionsBuilder.Options);
    }
}
