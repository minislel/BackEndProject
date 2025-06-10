// SongsExtraTests.cs
using System;
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
    public class SongsExtraTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;
        public SongsExtraTests(SharedWebApplicationFactory factory) => _client = factory.CreateClient();

        private static string NewUri() => "test:track:" + Guid.NewGuid();

      

        [Fact(DisplayName = "DELETE /songs/{URI} cascades to playbacks")]
        public async Task DeleteSong_CascadeDeletesPlaybacks()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            _client.SetBearer(token);

            // create song
            var uri = NewUri();
            await _client.PostAsJsonAsync("/api/SpotifyHistory/songs", new
            {
                uri,
                trackName = "CascadeT",
                artistName = "Artist",
                albumName = "Album"
            });

            // create one playback
            var post = await _client.PostAsJsonAsync("/api/SpotifyHistory/playbacks", new
            {
                uri,
                playTime = DateTime.UtcNow,
                platform = "X",
                msPlayed = 100,
                reasonStart = "S",
                reasonEnd = "E",
                shuffle = false,
                skip = false
            });
            var playbackId = (await post.Content.ReadFromJsonAsync<HateoasSongPlayWrapperDto>())!.SongPlay.Id;

            // delete song
            var delSong = await _client.DeleteAsync($"/api/SpotifyHistory/songs/{Uri.EscapeDataString(uri)}");
            delSong.StatusCode.Should().Be(HttpStatusCode.OK);

            // now playback should not be found by ID or by URI
            var getById = await _client.GetAsync($"/api/SpotifyHistory/playbacks/{playbackId}");
            getById.StatusCode.Should().Be(HttpStatusCode.NotFound);

            var getByUri = await _client.GetAsync($"/api/SpotifyHistory/playbacks/{Uri.EscapeDataString(uri)}");
            getByUri.StatusCode.Should().Be(HttpStatusCode.NotFound);
        }
    }
}


