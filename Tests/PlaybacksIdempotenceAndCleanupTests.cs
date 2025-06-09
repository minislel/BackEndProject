// PlaybacksIdempotenceAndCleanupTests.cs
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
    public class PlaybacksIdempotenceAndCleanupTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;
        public PlaybacksIdempotenceAndCleanupTests(SharedWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        [Fact(DisplayName = "DELETE /playbacks/{id} is idempotent")]
        public async Task DeletePlayback_Twice_SecondReturnsNotFound()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            // seed song
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new { uri, trackName = "T", artistName = "A", albumName = "L" });
            // seed playback
            var post = await _client.PostAsJsonAsync("/api/SpotifyHistory/playbacks", new
            {
                uri,
                playTime = DateTime.UtcNow,
                platform = "X",
                msPlayed = 10,
                reasonStart = "S",
                reasonEnd = "E",
                shuffle = false,
                skip = false
            });
            var id = (await post.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>())!.SongPlay.Id;

            // first delete
            var first = await _client.DeleteAsync($"/api/SpotifyHistory/playbacks/{id}");
            first.StatusCode.Should().Be(HttpStatusCode.NoContent);

            // second delete
            var second = await _client.DeleteAsync($"/api/SpotifyHistory/playbacks/{id}");
            second.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "PUT /playbacks/{id} is idempotent")]
        public async Task UpdatePlayback_RepeatedPut_NoSideEffects()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new { uri, trackName = "T", artistName = "A", albumName = "L" });
            var post = await _client.PostAsJsonAsync("/api/SpotifyHistory/playbacks", new
            {
                uri,
                playTime = DateTime.UtcNow,
                platform = "X",
                msPlayed = 10,
                reasonStart = "S",
                reasonEnd = "E",
                shuffle = false,
                skip = false
            });
            var id = (await post.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>())!.SongPlay.Id;

            var payload = new
            {
                id,
                uri,
                playTime = DateTime.UtcNow.AddMinutes(5),
                platform = "Y",
                msPlayed = 20,
                reasonStart = "NS",
                reasonEnd = "NE",
                shuffle = true,
                skip = true
            };
            var url = $"/api/SpotifyHistory/playbacks/{id}";

            // first update
            var first = await _client.PutAsJsonAsync(url, payload);
            first.StatusCode.Should().Be(HttpStatusCode.OK);
            var wrapper1 = await first.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>();
            wrapper1!.SongPlay.MsPlayed.Should().Be(20);

            // second update, same payload
            var second = await _client.PutAsJsonAsync(url, payload);
            second.StatusCode.Should().Be(HttpStatusCode.OK);
            var wrapper2 = await second.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>();
            wrapper2!.SongPlay.MsPlayed.Should().Be(20);
        }
    }
}