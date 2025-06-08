
using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using Infrastructure.EF;
using ApplicationCore.Models;               // Song, SongPlay
using Microsoft.Extensions.DependencyInjection;

namespace Tests;

public class SpotifyHistorySimpleTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;
    private const string TestUri = "spotify:track:testuri";

    public SpotifyHistorySimpleTests(CustomWebApplicationFactory factory)
    {
        _client = factory.CreateClient();

        // jednorazowy seed: Song + SongPlay
        using var scope = factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        if (!db.Songs.Any(s => s.URI == TestUri))           // unikamy duplikatów
        {
            db.Songs.Add(new Song
            {
                URI = TestUri,
                TrackName = "Stub Track",
                AlbumName = "Stub Album",
                ArtistName = "Stub Artist"
            });

            db.SongPlays.Add(new SongPlay
            {
                URI = TestUri,
                PlayTime = DateTime.UtcNow,
                Platform = "android",
                MsPlayed = 123_456,

                // ▼ wymagane pola (NOT NULL w modelu)
                ReasonStart = "trackdone",
                ReasonEnd = "trackdone",
                Shuffle = false,
                Skip = false
            });

            db.SaveChanges();
        }
    }

    [Fact(DisplayName = "GET /playbacks/{URI} zwraca min. 1 rekord")]
    public async Task Get_Playbacks_ByUri_Returns_Ok_And_Data()
    {
        var res = await _client.GetAsync($"/api/spotifyhistory/playbacks/{TestUri}");
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var list = await res.Content.ReadFromJsonAsync<List<SongPlay>>();
        list.Should().NotBeNull().And.HaveCountGreaterThan(0);
        list!.All(p => p.URI == TestUri).Should().BeTrue();
    }
}
