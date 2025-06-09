// SongsIdempotenceAndCleanupTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using ApplicationCore.Models;
using WebApi.Dto;
using FluentAssertions;
using Xunit;

namespace Tests
{
    public class SongsIdempotenceAndCleanupTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;
        public SongsIdempotenceAndCleanupTests(SharedWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        [Fact(DisplayName = "DELETE /songs/{URI} is idempotent")]
        public async Task DeleteSong_Twice_SecondReturnsNotFound()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            // create
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new { uri, trackName = "T", artistName = "A", albumName = "L" });

            // first delete
            var first = await _client.DeleteAsync($"/api/SpotifyHistory/songs/{Uri.EscapeDataString(uri)}");
            first.StatusCode.Should().Be(HttpStatusCode.OK);

            // second delete
            var second = await _client.DeleteAsync($"/api/SpotifyHistory/songs/{Uri.EscapeDataString(uri)}");
            second.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "PUT /songs is idempotent")]
        public async Task UpdateSong_RepeatedPut_NoSideEffects()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            // create
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new { uri, trackName = "Init", artistName = "A", albumName = "L" });

            var payload = new { trackName = "X", artistName = "B", albumName = "C" };
            var url = $"/api/SpotifyHistory/songs?URI={Uri.EscapeDataString(uri)}";

            // first update
            var first = await _client.PutAsJsonAsync(url, payload);
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            var wrapper1 = await first.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            wrapper1!.Song.TrackName.Should().Be("X");

            // second update, same payload
            var second = await _client.PutAsJsonAsync(url, payload);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var wrapper2 = await second.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            wrapper2!.Song.TrackName.Should().Be("X");
        }
    }
}