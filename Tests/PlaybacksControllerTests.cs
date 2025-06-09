using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http.Json;
using System.Threading.Tasks;
using ApplicationCore.Models;
using WebApi.Dto;
using FluentAssertions;
using Xunit;

namespace Tests
{
    public class PlaybacksIntegrationTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public PlaybacksIntegrationTests(SharedWebApplicationFactory factory)
        {
            _client = factory.CreateClient();
        }

        private static string NewUri() => "test:track:" + Guid.NewGuid();

        [Fact(DisplayName = "End-to-end Playbacks Workflow")]
        public async Task Playbacks_EndToEnd_Workflow_Works()
        {
            // Authenticate
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            // 1) Create a new Song
            var uri = NewUri();
            var songPayload = new
            {
                uri,
                trackName = "Test Track",
                artistName = "Test Artist",
                albumName = "Test Album"
            };
            var songRes = await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", songPayload);
            songRes.EnsureSuccessStatusCode();
            var songWrapper = await songRes.Content.ReadFromJsonAsync<HateoasSongWrapperDto>();
            songWrapper!.Song.URI.Should().Be(uri);

            // 2) Create a new Playback
            var playbackPayload = new
            {
                uri,
                playTime = DateTime.UtcNow,
                platform = "TestPlatform",
                msPlayed = 1234,
                reasonStart = "AutoTestStart",
                reasonEnd = "AutoTestEnd",
                shuffle = false,
                skip = false
            };
            var playbackRes = await _client.PostAsJsonAsync("/api/SpotifyHistory/playbacks", playbackPayload);
            playbackRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var playbackWrapper = await playbackRes.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>();
            var playbackId = playbackWrapper!.SongPlay.Id;
            playbackId.Should().BeGreaterThan(0);
           

            // 3) Retrieve by ID
            var getByIdRes = await _client.GetAsync($"/api/SpotifyHistory/playbacks/{playbackId}");
            getByIdRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getByIdWrapper = await getByIdRes.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>();
            getByIdWrapper!.SongPlay.Id.Should().Be(playbackId);

            // 4) Retrieve all by URI
            var listRes = await _client.GetAsync($"/api/SpotifyHistory/playbacks/{Uri.EscapeDataString(uri)}");
            listRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var list = await listRes.Content.ReadFromJsonAsync<List<SongPlay>>();
            list.Should().ContainSingle(p => p.Id == playbackId);

            // 5) Update the Playback
            var updatePayload = new
            {
                id = playbackId,
                uri,
                playTime = DateTime.UtcNow.AddHours(1),
                platform = "UpdatedPlatform",
                msPlayed = 4321,
                reasonStart = "UpdatedStart",
                reasonEnd = "UpdatedEnd",
                shuffle = true,
                skip = true
            };
            var updateRes = await _client.PutAsJsonAsync($"/api/SpotifyHistory/playbacks/{playbackId}", updatePayload);
            updateRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var updatedWrapper = await updateRes.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>();
            updatedWrapper!.SongPlay.Platform.Should().Be("UpdatedPlatform");
            updatedWrapper.SongPlay.MsPlayed.Should().Be(4321);

            // 6) Delete the Playback (should return 204 No Content)
            var delRes = await _client.DeleteAsync($"/api/SpotifyHistory/playbacks/{playbackId}");
            delRes.StatusCode.Should().Be(HttpStatusCode.NoContent);
            delRes.Content.Headers.ContentLength.Should().Be(0);

            // 7) Confirm deletion
            var confirmRes = await _client.GetAsync($"/api/SpotifyHistory/playbacks/{playbackId}");
            confirmRes.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}
