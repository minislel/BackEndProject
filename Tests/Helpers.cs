// Helpers.cs
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;
using WebApi.Dto;

namespace Tests
{
    internal static class Helpers
    {
        internal static async Task<string> GetJwtTokenAsync(HttpClient client)
        {
            var loginDto = new LoginDto { Login = "testuser", Password = "Pa$$w0rd!" };
            var res = await client.PostAsJsonAsync("/api/users/login", loginDto);
            res.EnsureSuccessStatusCode();

            var json = await res.Content.ReadFromJsonAsync<JsonObject>();
            return json!["token"]!.GetValue<string>();
        }

        internal static void SetBearer(this HttpClient client, string token) =>
            client.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Bearer", token);
    }
}
