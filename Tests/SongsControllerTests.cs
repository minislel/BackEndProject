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
    public class SongsControllerTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SongsControllerTests(SharedWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        [Fact(DisplayName = "POST /songs → 200 + wrapper")]
        public async Task CreateSong_Returns_Ok_WithWrapper()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            var payload = new
            {
                uri,
                trackName = "Title",
                artistName = "Artist",
                albumName = "Album"
            };

            var res = await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", payload);
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var wrapper = await res.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            wrapper.Should().NotBeNull();
            wrapper!.Song.URI.Should().Be(uri);
        }

        [Fact(DisplayName = "GET /songs/{URI} → 200 + wrapper")]
        public async Task GetSongByUri_Returns_Ok_WithWrapper()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = "Title",
                artistName = "Artist",
                albumName = "Album"
            });

            var res = await _client.GetAsync($"/api/SpotifyHistory/songs/{Uri.EscapeDataString(uri)}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var wrapper = await res.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            wrapper.Should().NotBeNull();
            wrapper!.Song.URI.Should().Be(uri);
        }

        [Fact(DisplayName = "PUT /songs?URI=... → 200 + wrapper")]
        public async Task UpdateSong_Returns_Ok_WithWrapper()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = "OldTitle",
                artistName = "Artist",
                albumName = "Album"
            });

            var updatePayload = new
            {
                trackName = "NewTitle",
                artistName = "NewArtist",
                albumName = "NewAlbum"
            };

            var res = await _client.PutAsJsonAsync(
                $"/api/SpotifyHistory/songs?URI={Uri.EscapeDataString(uri)}",
                updatePayload);
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var wrapper = await res.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            wrapper.Should().NotBeNull();
            wrapper!.Song.URI.Should().Be(uri);
            wrapper.Song.TrackName.Should().Be("NewTitle");
        }

        [Fact(DisplayName = "DELETE /songs/{URI} → 200 + links")]
        public async Task DeleteSong_Returns_Ok_WithLinks()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = "ToDelete",
                artistName = "Artist",
                albumName = "Album"
            });

            var resDel = await _client.DeleteAsync($"/api/SpotifyHistory/songs/{Uri.EscapeDataString(uri)}");
            resDel.StatusCode.Should().Be(HttpStatusCode.OK);

            var json = await resDel.Content.ReadAsStringAsync();
            var links = JsonSerializer.Deserialize<List<HateoasLinkDto>>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
            links.Should().NotBeNull()
                 .And.OnlyContain(l =>
                     !string.IsNullOrWhiteSpace(l.Href) &&
                     !string.IsNullOrWhiteSpace(l.Method)
                 );
        }

        [Fact(DisplayName = "GET /songs/top10 → 200 + lista ≤10")]
        public async Task Top10_Returns_AtMost_10()
        {
            var res = await _client.GetAsync("/api/SpotifyHistory/songs/top10");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await res.Content.ReadFromJsonAsync<List<SongRankElementDto>>();
            list.Should().NotBeNull().And.HaveCountLessOrEqualTo(10);
        }

        [Theory(DisplayName = "GET /songs/search/{query} → 200 + lista wyników")]
        [InlineData("Title")]
        public async Task SearchSongs_Returns_Ok_WithResults(string q)
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = q + "Match",
                artistName = "Artist",
                albumName = "Album"
            });

            var res = await _client.GetAsync($"/api/SpotifyHistory/songs/search/{Uri.EscapeDataString(q)}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await res.Content.ReadFromJsonAsync<List<Song>>();
            list.Should().NotBeNull()
                .And.Contain(s => s.URI == uri);
        }

        [Theory(DisplayName = "GET /songs/search/{query} → 404 when empty")]
        [InlineData("NonExistentQuery123")]
        public async Task SearchSongs_Returns_404_WhenNoResults(string q)
        {
            var res = await _client.GetAsync($"/api/SpotifyHistory/songs/search/{Uri.EscapeDataString(q)}");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
