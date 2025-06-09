// SongsControllerErrorTests.cs
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApplicationCore.Models;
using WebApi.Dto;
using FluentAssertions;
using Xunit;

namespace Tests
{
    public class SongsControllerErrorTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SongsControllerErrorTests(SharedWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        [Fact(DisplayName = "GET /songs/search/{empty} → 404 Not Found")]
        public async Task SearchSongs_EmptyQuery_Returns_NotFound()
        {
            // Wywołanie bez podania {query} w ścieżce
            var res = await _client.GetAsync("/api/SpotifyHistory/songs/search/");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }


        [Fact(DisplayName = "POST /songs missing required field → 400 Bad Request")]
        public async Task CreateSong_MissingField_Returns_BadRequest()
        {
            // missing 'uri'
            var payload = new { trackName = "T" };
            var res = await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", payload);
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "POST /playbacks unauthorized → 403 Forbidden")]
        public async Task CreatePlayback_Unauthorized_Returns_Forbidden()
        {
            var payload = new
            {
                uri = NewUri(),
                playTime = DateTime.UtcNow,
                platform = "X",
                msPlayed = 100,
                reasonStart = "S",
                reasonEnd = "E",
                shuffle = false,
                skip = false
            };
            // brak tokenu
            var res = await _client.PostAsJsonAsync("/api/SpotifyHistory/playbacks", payload);
            res.StatusCode.Should().Be(HttpStatusCode.Forbidden);
        }


        [Fact(DisplayName = "GET /songs/{invalid} → 404 Not Found")]
        public async Task GetSong_InvalidUri_Returns_NotFound()
        {
            // authorized
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var res = await _client.GetAsync("/api/SpotifyHistory/songs/nonexistent-uri");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "PUT /songs without URI query → 400 Bad Request")]
        public async Task UpdateSong_NoUriQuery_Returns_BadRequest()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var payload = new { trackName = "X" };
            var res = await _client.PutAsJsonAsync("/api/SpotifyHistory/songs", payload);
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }

        [Fact(DisplayName = "PUT /songs?URI=invalid → 400 Bad Request")]
        public async Task UpdateSong_InvalidUri_Returns_BadRequest()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var payload = new { trackName = "X" };
            var res = await _client.PutAsJsonAsync("/api/SpotifyHistory/songs?URI=invalid", payload);
            res.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        }


        [Fact(DisplayName = "DELETE /songs/{invalid} → 404 Not Found")]
        public async Task DeleteSong_InvalidUri_Returns_NotFound()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var res = await _client.DeleteAsync("/api/SpotifyHistory/songs/nonexistent-uri");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "GET /songs/search/{empty} → 404 Not Found")]
        public async Task SearchSongs_EmptyQuery_Returns_NotFound2()
        {
            // Wywołanie bez podania {query} w ścieżce
            var res = await _client.GetAsync("/api/SpotifyHistory/songs/search/");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

    }
}
