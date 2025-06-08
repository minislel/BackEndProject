// Tests/CustomWebApplicationFactory.cs
using Infrastructure.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using WebApi;

namespace Tests;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Test");

        // ─── 1. JWT z prostym sekretem ───────────────────────────────────────
        builder.ConfigureAppConfiguration((ctx, cfg) =>
        {
            cfg.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["JwtSettings:Secret"] = "TESTSECRET123456TESTSECRET123456",
                ["JwtSettings:ValidIssuer"] = "TestIssuer",
                ["JwtSettings:ValidAudience"] = "TestAudience"
            }!);
        });


        // ─── 2. DbContext → EF-InMemory (z internal provider) ────────────────
        builder.ConfigureServices(services =>
        {
            // usuń ewentualne wcześniejsze rejestracje AppDbContext
            services.RemoveAll<AppDbContext>();
            services.RemoveAll(typeof(DbContextOptions<AppDbContext>));
            services.RemoveAll(typeof(IDbContextFactory<AppDbContext>));

            // budujemy mini-container tylko z usługami InMemory
            var inMemoryProvider = new ServiceCollection()
                .AddEntityFrameworkInMemoryDatabase()
                .BuildServiceProvider();

            services.AddDbContext<AppDbContext>(opts =>
            {
                opts.UseInMemoryDatabase("BackEndProjectTest");
                opts.UseInternalServiceProvider(inMemoryProvider);
            });

            // ─── 3. Seed test-użytkownika ────────────────────────────────────
            using var scope = services.BuildServiceProvider().CreateScope();
            var ctx = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<UserEntity>>();

            ctx.Database.EnsureCreated();

            var testUser = new UserEntity
            {
                UserName = "testuser",
                Email = "test@local",
                EmailConfirmed = true
            };
            userManager.CreateAsync(testUser, "Pa$$w0rd!").Wait();
        });
    }
}
