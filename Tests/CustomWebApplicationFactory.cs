// SharedWebApplicationFactory.cs
using System;
using System.Linq;
using ApplicationCore.Models;
using Infrastructure.EF;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using WebApi;

namespace Tests
{
    public class SharedWebApplicationFactory : WebApplicationFactory<Program>, IDisposable
    {
        private IServiceScope? _scope;

        protected override void ConfigureWebHost(IWebHostBuilder builder)
        {
            builder.ConfigureServices(services =>
            {
                // 1. Usuń domyślne DbContextOptions
                var desc = services.SingleOrDefault(d =>
                    d.ServiceType == typeof(DbContextOptions<AppDbContext>));
                if (desc != null) services.Remove(desc);

                // 2. Dodaj prawdziwe połączenie do PostgreSQL
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseNpgsql("Host=localhost;Port=5432;Database=spotifydb;Username=postgres;Password=postgres"));

                // 3. Zbuduj provider i scope
                var sp = services.BuildServiceProvider();
                _scope = sp.CreateScope();
                var scoped = _scope.ServiceProvider;

                var ctx = scoped.GetRequiredService<AppDbContext>();
                var userManager = scoped.GetRequiredService<UserManager<UserEntity>>();

                // 4. Migracje i seed użytkownika
                ctx.Database.Migrate();
                if (userManager.FindByNameAsync("testuser").Result == null)
                {
                    var testUser = new UserEntity
                    {
                        UserName = "testuser",
                        Email = "test@local",
                        EmailConfirmed = true,
                        Details = new UserDetails { CreatedAt = DateTime.UtcNow }
                    };
                    userManager.CreateAsync(testUser, "Pa$$w0rd!").Wait();
                }

                // 5. Seed dla GET /playbacks/{URI}
                const string testUri = "spotify:track:testuri";
                if (!ctx.Songs.Any(s => s.URI == testUri))
                {
                    ctx.Songs.Add(new Song
                    {
                        URI = testUri,
                        TrackName = "Test Song",
                        ArtistName = "Test Artist",
                        AlbumName = "Test Album"
                    });
                    ctx.SaveChanges();
                }
                if (!ctx.SongPlays.Any(sp => sp.URI == testUri))
                {
                    ctx.SongPlays.Add(new SongPlay
                    {
                        URI = testUri,
                        PlayTime = DateTime.UtcNow,
                        Platform = "TestPlatform",
                        MsPlayed = 1234,
                        ReasonStart = "AutoSeed",
                        ReasonEnd = "AutoSeed",
                        Shuffle = false,
                        Skip = false
                    });
                    ctx.SaveChanges();
                }

                // 6. Seed dla testu wyszukiwania (query "Seed")
                const string seedUri = "spotify:track:seedsong";
                if (!ctx.Songs.Any(s => s.URI == seedUri))
                {
                    ctx.Songs.Add(new Song
                    {
                        URI = seedUri,
                        TrackName = "Seed Song Title",
                        ArtistName = "Some Artist",
                        AlbumName = "Seed Album"
                    });
                    ctx.SaveChanges();
                }
            });
        }

        public new void Dispose()
        {
            base.Dispose();
            _scope?.Dispose();
        }
    }
}
