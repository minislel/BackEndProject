using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApplicationCore.Models; // Song, SongPlay
using FluentAssertions;
using Infrastructure.EF;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Tests
{
    public class SpotifyHistoryReadTests : IClassFixture<CustomWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private readonly int _playbackId;
        private readonly string _trackUri;

        public SpotifyHistoryReadTests(CustomWebApplicationFactory factory)
        {
            _client = factory.CreateClient();

            // seed data
            using var scope = factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

            // ensure at least one Song and SongPlay exist
            _trackUri = db.SongPlays.Select(sp => sp.URI).FirstOrDefault() ?? ($"spotify:track:{Guid.NewGuid()}");
            if (!db.Songs.Any(s => s.URI == _trackUri))
            {
                db.Songs.Add(new Song { URI = _trackUri, TrackName = "Seeded Track", ArtistName = "Seed Artist", AlbumName = "Seed Album" });
            }

            var existingPlay = db.SongPlays.FirstOrDefault(sp => sp.URI == _trackUri);
            if (existingPlay == null)
            {
                existingPlay = new SongPlay
                {
                    URI = _trackUri,
                    PlayTime = DateTime.UtcNow,
                    Platform = "test",
                    MsPlayed = 1000,
                    ReasonStart = "seed",
                    ReasonEnd = "seed",
                    Shuffle = false,
                    Skip = false
                };
                db.SongPlays.Add(existingPlay);
            }

            db.SaveChanges();
            _playbackId = existingPlay.Id;
        }

        [Fact]
        public async Task Get_PlaybackById_ReturnsOk()
        {
            var res = await _client.GetAsync($"/api/spotifyhistory/playbacks/{_playbackId}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var play = await res.Content.ReadFromJsonAsync<SongPlay>();
            play.Should().NotBeNull();
            play.Id.Should().Be(_playbackId);
        }

        [Fact]
        public async Task Get_PlaybacksByUri_ReturnsOk()
        {
            var res = await _client.GetAsync($"/api/spotifyhistory/playbacks/{_trackUri}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var plays = await res.Content.ReadFromJsonAsync<List<SongPlay>>();
            plays.Should().NotBeNull().And.HaveCountGreaterThan(0);
            plays.All(p => p.URI == _trackUri).Should().BeTrue();
        }

        [Fact]
        public async Task Get_Song_ReturnsOk()
        {
            var res = await _client.GetAsync($"/api/spotifyhistory/songs/{_trackUri}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var song = await res.Content.ReadFromJsonAsync<Song>();
            song.Should().NotBeNull();
            song.URI.Should().Be(_trackUri);
        }

      

        [Fact]
        public async Task SearchSongs_ByQuery_ReturnsOk()
        {
            // search by artist name
            var query = "Seed";
            var res = await _client.GetAsync($"/api/spotifyhistory/songs/search/{query}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await res.Content.ReadFromJsonAsync<List<Song>>();
            list.Should().NotBeNull().And.HaveCountGreaterThan(0);
        }

        [Fact]
        public async Task SearchSongs_NoMatch_ReturnsNotFound()
        {
            var query = Guid.NewGuid().ToString();
            var res = await _client.GetAsync($"/api/spotifyhistory/songs/search/{query}");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact]
        public async Task SearchSongs_InvalidFilter_ReturnsBadRequest()
        {
            var query = "Seed";
            var res = await _client.GetAsync($"/api/spotifyhistory/songs/search/{query}?filter=Unknown");
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }
    }
}
