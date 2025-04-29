using Infrastructure.EF;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using WebApi;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Text;
using System.Threading.Tasks;
using WebApi.Dto;
using Program = WebApi.Program;
using System.Text.Json.Nodes;

namespace Tests
{

    public class AppUsersControllerTests : IClassFixture<AppTestFactory<Program>>
    {
        private readonly HttpClient _client;
        private readonly AppTestFactory<Program> _app;
        private readonly AppDbContext _dbContext;

        public AppUsersControllerTests(AppTestFactory<Program> app)
        {
            _app = app;
            _client = app.CreateClient();
            using (var scope = app.Services.CreateScope())
            { 
                _dbContext = scope.ServiceProvider.GetService<AppDbContext>();
                _dbContext.Users.Add(
                    new UserEntity()
                    {
                                        Id = "7abf1057-5d1e-4efd-8166-27e4f6712ead",
                Email = "admin@wsei.edu.pl",
                NormalizedEmail = "ADMIN@WSEI.EDU.PL",
                UserName = "Admin",
                NormalizedUserName = "ADMIN",
                ConcurrencyStamp = "7abf1057-5d1e-4efd-8166-27e4f6712ead",
                SecurityStamp = "7abf1057-5d1e-4efd-8166-27e4f6712ead",
                PasswordHash = "AQAAAAIAAYagAAAAENrUGpVMb8wzhY3UuvwWcNf3lOjlXx/7expp/8dhpQOjv0cnxuQKvx+hFtP96D+ceA=="
                    }
                );
                _dbContext.SaveChanges();
            }
        }
        [Fact]
        public async void TestValidLogin()
        {
            var loginBody = new LoginDto()
            {
                Login = "admin",
                Password = "Admin123!"
            };
            var result = await _client.PostAsJsonAsync("/api/users/login", loginBody);
            Assert.NotNull( result );
            Assert.Equal(System.Net.HttpStatusCode.OK, result.StatusCode);
            JsonNode node  = JsonNode.Parse(await result.Content.ReadAsStringAsync());
            var token = node["token"].AsValue().ToString();
            Console.WriteLine(result.Content);
            Assert.NotNull(token);
        }
        [Fact]
        public async void TestBookController()
        {
            var result = await _client.GetAsync("/api/books");
            Assert.NotNull(result);
            Assert.Equal(System.Net.HttpStatusCode.NotFound, result.StatusCode);
            var content = await result.Content.ReadAsStringAsync();
            Console.WriteLine(content);
        }
    }


}
