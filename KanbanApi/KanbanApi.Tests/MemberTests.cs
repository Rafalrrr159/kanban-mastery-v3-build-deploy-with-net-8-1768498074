using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KanbanApi.Tests
{
    public class MemberTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public MemberTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "MemberTestDb_" + Guid.NewGuid().ToString();
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);

                    services.AddDbContext<ApplicationDbContext>(options =>
                        options.UseInMemoryDatabase(dbName));

                    var sp = services.BuildServiceProvider();
                    using (var scope = sp.CreateScope())
                    {
                        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        db.Database.EnsureCreated();
                    }
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task AddMember_Flow_VerifyOwnerAndForbiddenAccess()
        {
            var ownerEmail = "owner@test.com";
            var hackerEmail = "hacker@test.com";
            var userToAddEmail = "newbie@test.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = ownerEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = hackerEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = userToAddEmail, password = pwd });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var userToAdd = await db.Users.FirstOrDefaultAsync(u => u.Email == userToAddEmail);
            Assert.NotNull(userToAdd);
            var userIdToAdd = userToAdd.Id;

            var loginResponse = await _client.PostAsJsonAsync("/login", new { email = ownerEmail, password = pwd });
            var token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var boardResponse = await _client.PostAsJsonAsync("/api/boards", new Dtos("Auth Test Board"));
            var boardId = (await boardResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var addMemberResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/members", new AddBoardMemberDto(userIdToAdd));
            Assert.Equal(HttpStatusCode.OK, addMemberResponse.StatusCode);

            var hackerLogin = await _client.PostAsJsonAsync("/login", new { email = hackerEmail, password = pwd });
            var hackerToken = (await hackerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hackerToken);

            var forbiddenResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/members", new AddBoardMemberDto(userIdToAdd));
            
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        }
    }
}
