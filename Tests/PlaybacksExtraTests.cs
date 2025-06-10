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
    public class PlaybacksExtraTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;
        public PlaybacksExtraTests(SharedWebApplicationFactory factory) => _client = factory.CreateClient();

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        [Fact(DisplayName = "GET /playbacks/{non-int} → 404 NotFound")]
        public async Task GetPlayback_InvalidIdFormat_Returns_NotFound()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            var res = await _client.GetAsync("/api/SpotifyHistory/playbacks/not-an-int");
            res.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }

        [Fact(DisplayName = "POST /playbacks concurrent → no lost records")]
        public async Task CreatePlaybacks_Concurrent_NoLoss()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            // create song first
            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = "ConcurrentT",
                artistName = "Artist",
                albumName = "Album"
            });

            // spawn 10 concurrent posts
            var tasks = Enumerable.Range(0, 10).Select(i => _client.PostAsJsonAsync("/api/SpotifyHistory/playbacks", new
            {
                uri,
                playTime = DateTime.UtcNow.AddSeconds(i),
                platform = "P",
                msPlayed = 100 + i,
                reasonStart = "S",
                reasonEnd = "E",
                shuffle = false,
                skip = false
            })).ToArray();

            await Task.WhenAll(tasks);

            // verify all succeeded
            foreach (var t in tasks)
                t.Result.StatusCode.Should().Be(HttpStatusCode.OK);

            // now list by URI
            var listRes = await _client.GetAsync($"/api/SpotifyHistory/playbacks/{Uri.EscapeDataString(uri)}");
            listRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var list = await listRes.Content.ReadFromJsonAsync<List<SongPlay>>();
            list.Should().HaveCount(10);
        }
    }
}