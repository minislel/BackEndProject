using System.Net;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;
using Tests;
using WebApi.Dto;

namespace Tests;

public class UsersControllerTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersControllerTests(CustomWebApplicationFactory factory)
        => _client = factory.CreateClient();

    [Fact]
    public async Task Login_Returns_Jwt_Token()
    {
        var dto = new LoginDto { Login = "admin", Password = "Admin123!" };

        var res = await _client.PostAsJsonAsync("/api/users/login", dto);

        res.StatusCode.Should().Be(HttpStatusCode.OK);
        var json = await res.Content.ReadFromJsonAsync<JsonNode>();
        json!["token"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
    }
}
