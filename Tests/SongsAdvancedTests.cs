// SongsAdvancedTests.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApplicationCore.Models;
using WebApi.Dto;
using FluentAssertions;
using Xunit;

namespace Tests
{
    public class SongsAdvancedTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public SongsAdvancedTests(SharedWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        private async Task AuthenticateAsync()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);
        }

        [Fact(DisplayName = "GET /songs/{URI} → HATEOAS links present")]
        public async Task GetSong_HasExpectedHateoasLinks()
        {
            await AuthenticateAsync();

            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = "HateoasTest",
                artistName = "Artist",
                albumName = "Album"
            });

            var res = await _client.GetAsync($"/api/SpotifyHistory/songs/{Uri.EscapeDataString(uri)}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var wrapper = await res.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            wrapper.Should().NotBeNull();

            var rels = wrapper.Links.Select(l => l.Rel).ToHashSet();
            // oczekujemy relacji zgodnych z implementacją
            rels.Should().Contain(new[]
            {
        "get_song",
        "put_song",
        "delete_song",
        "post_song"
    });
            wrapper.Links.Should().OnlyContain(l =>
                Uri.IsWellFormedUriString(l.Href, UriKind.Relative) &&
                (l.Method == "GET" || l.Method == "PUT" || l.Method == "DELETE" || l.Method == "POST"));
        }




        [Fact(DisplayName = "GET /songs/search → sorting by trackName works")]
        public async Task SearchSongs_SortByTrackName_Works()
        {
            await AuthenticateAsync();

            // Use a unique query to isolate seeded songs
            var q = Guid.NewGuid().ToString();
            var uriHigh = NewUri();
            var uriLow = NewUri();
            var titleHigh = q + "Z";
            var titleLow = q + "A";

            // Seed two songs
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri = uriHigh,
                trackName = titleHigh,
                artistName = "Artist",
                albumName = "Album"
            });
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri = uriLow,
                trackName = titleLow,
                artistName = "Artist",
                albumName = "Album"
            });

            // Perform search with sort by trackName
            var res = await _client.GetAsync(
                $"/api/SpotifyHistory/songs/search/{Uri.EscapeDataString(q)}?sort=trackName");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await res.Content.ReadFromJsonAsync<List<Song>>();
            list.Should().HaveCount(2);

            var names = list.Select(s => s.TrackName).ToList();
            // The API returns descending order by default, so Z comes before A
            names.Should().ContainInOrder(new[] { titleHigh, titleLow });
        }



        [Fact(DisplayName = "GET /songs/top10 → correct playCount order")]
        public async Task Top10_Returns_Songs_SortedByPlayCount()
        {
            await AuthenticateAsync();

            var res = await _client.GetAsync("/api/SpotifyHistory/songs/top10");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await res.Content.ReadFromJsonAsync<List<SongRankElementDto>>();
            list.Should().NotBeNull().And.HaveCountLessOrEqualTo(10);

            // Verify that the play counts are in descending order
            var counts = list.Select(e => e.PlayCount).ToList();
            counts.Should().BeInDescendingOrder();
        }

    }
}
