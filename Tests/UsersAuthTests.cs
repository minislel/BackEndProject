// Tests/UsersAuthTests.cs
using System;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json.Nodes;
using FluentAssertions;

using WebApi.Dto;              // LoginDto

namespace Tests;

public class UsersAuthTests : IClassFixture<CustomWebApplicationFactory>
{
    private readonly HttpClient _client;

    public UsersAuthTests(CustomWebApplicationFactory factory)
        => _client = factory.CreateClient();

    [Fact(DisplayName = "POST /api/users/login zwraca token JWT")]
    public async Task Login_Returns_Jwt_Token()
    {
        var dto = new LoginDto { Login = "testuser", Password = "Pa$$w0rd!" };
        var res = await _client.PostAsJsonAsync("/api/users/login", dto);
        res.StatusCode.Should().Be(HttpStatusCode.OK);

        var json = await res.Content.ReadFromJsonAsync<JsonObject>();
        json!["token"]!.GetValue<string>().Should().NotBeNullOrWhiteSpace();
    }



   
}
