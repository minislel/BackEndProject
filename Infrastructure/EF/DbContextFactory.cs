using Infrastructure.EF;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;

public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.EnableSensitiveDataLogging(true);

        //var config = new ConfigurationBuilder()
        //    .SetBasePath(Path.Combine(Directory.GetCurrentDirectory(), "../WebApi"))
        //    .AddJsonFile("appsettings.json")
        //    .Build();

        var config = new ConfigurationBuilder()
             .SetBasePath(Directory.GetCurrentDirectory())  
             .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
             .AddEnvironmentVariables()
             .Build();



        var defaultConn = config.GetConnectionString("DefaultConnection");
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION") ?? defaultConn;
        optionsBuilder.UseNpgsql(connectionString);

        return new AppDbContext(optionsBuilder.Options);

    }
}
