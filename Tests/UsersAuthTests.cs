// UsersAuthTests.cs
using System.Threading.Tasks;
using FluentAssertions;
using Xunit;

namespace Tests
{
    public class UsersAuthTests : IClassFixture<SharedWebApplicationFactory>
    {
        private readonly HttpClient _client;

        public UsersAuthTests(SharedWebApplicationFactory factory) =>
            _client = factory.CreateClient();

        [Fact(DisplayName = "POST /api/users/login zwraca token JWT")]
        public async Task Login_Returns_Jwt_Token()
        {
            var token = await Helpers.GetJwtTokenAsync(_client);
            token.Should().NotBeNullOrWhiteSpace();
        }
    }
}
