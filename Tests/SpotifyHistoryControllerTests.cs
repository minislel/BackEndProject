// SpotifyHistoryIntegrationTests.cs
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApplicationCore.Models;
using FluentAssertions;
using Xunit;

namespace Tests
{
    public class SpotifyHistoryIntegrationTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;
        private const string TestUri = "spotify:track:testuri";

        public SpotifyHistoryIntegrationTests(SharedWebApplicationFactory factory) =>
            _client = factory.CreateClient();

        [Fact(DisplayName = "GET /api/spotifyhistory/playbacks/{URI} zwraca dane")]
        public async Task Get_Playbacks_ByUri_Returns_Ok_And_Data()
        {
            var res = await _client.GetAsync($"/api/spotifyhistory/playbacks/{TestUri}");
            res.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await res.Content.ReadFromJsonAsync<List<SongPlay>>();
            list.Should().NotBeNull()
                .And.HaveCountGreaterThan(0);
            list!.All(p => p.URI == TestUri).Should().BeTrue();
        }

        [Theory(DisplayName = "GET /api/spotifyhistory/songs/search/{query}")]
        [InlineData("Seed", HttpStatusCode.OK)]
        [InlineData("NonExisting", HttpStatusCode.NotFound)]
        public async Task SearchSongs_Returns_CorrectStatus(string query, HttpStatusCode expected)
        {
            var res = await _client.GetAsync($"/api/spotifyhistory/songs/search/{query}");
            res.StatusCode.Should().Be(expected);

            if (expected == HttpStatusCode.OK)
            {
                var list = await res.Content.ReadFromJsonAsync<List<Song>>();
                list.Should().NotBeNull().And.HaveCountGreaterThan(0);
            }
        }
    }
}
