using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using WebApi.Dto;

namespace Tests;

internal static class Helpers
{
    internal static async Task<string> GetJwtTokenAsync(HttpClient client)
    {
        var dto = new LoginDto { Login = "testuser", Password = "Pa$$w0rd!" };
        var res = await client.PostAsJsonAsync("/api/users/login", dto);
        res.EnsureSuccessStatusCode();

        var json = await res.Content.ReadFromJsonAsync<JsonObject>();
        return json!["token"]!.GetValue<string>();
    }

    internal static void SetBearer(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
