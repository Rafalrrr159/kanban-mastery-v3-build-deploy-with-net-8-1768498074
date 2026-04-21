using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KanbanApi.Tests
{
    public class ColumnTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public ColumnTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "ColumnTestDb_" + Guid.NewGuid().ToString();
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
                    if (descriptor != null) services.Remove(descriptor);
                    services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));

                    var sp = services.BuildServiceProvider();
                    using var scope = sp.CreateScope();
                    scope.ServiceProvider.GetRequiredService<ApplicationDbContext>().Database.EnsureCreated();
                });
            });
            _client = _factory.CreateClient();
        }

        [Fact]
        public async Task Column_Flow_VerifyOperationsAndSecurity()
        {
            var memberEmail = "member@test.com";
            var hackerEmail = "hacker@test.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = memberEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = hackerEmail, password = pwd });

            var login = await _client.PostAsJsonAsync("/login", new { email = memberEmail, password = pwd });
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var board = await _client.PostAsJsonAsync("/api/boards", new Dtos("Kanban"));
            var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var createResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new CreateColumnDto("Do zrobienia"));
            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var columnId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var putResponse = await _client.PutAsJsonAsync($"/api/boards/{boardId}/columns/{columnId}", new UpdateColumnDto("W toku"));
            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                db.Cards.Add(new Card { Title = "Test Card", ColumnId = columnId });
                await db.SaveChangesAsync();
            }
            var deleteResponse = await _client.DeleteAsync($"/api/boards/{boardId}/columns/{columnId}");
            Assert.Equal(HttpStatusCode.BadRequest, deleteResponse.StatusCode);

            var hackerLogin = await _client.PostAsJsonAsync("/login", new { email = hackerEmail, password = pwd });
            var hackerToken = (await hackerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hackerToken);

            var forbiddenResponse = await _client.DeleteAsync($"/api/boards/{boardId}/columns/{columnId}");
            Assert.Equal(HttpStatusCode.Forbidden, forbiddenResponse.StatusCode);
        }

        [Fact]
        public async Task CreateColumn_AsNonMember_ReturnsForbidden()
        {
            var ownerEmail = "owner_col@test.com";
            var hackerEmail = "hacker_col@test.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = ownerEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = hackerEmail, password = pwd });

            var ownerLogin = await _client.PostAsJsonAsync("/login", new { email = ownerEmail, password = pwd });
            var ownerToken = (await ownerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var boardResp = await _client.PostAsJsonAsync("/api/boards", new Dtos("Private Board"));
            var boardId = (await boardResp.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var hackerLogin = await _client.PostAsJsonAsync("/login", new { email = hackerEmail, password = pwd });
            var hackerToken = (await hackerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hackerToken);

            var response = await _client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new CreateColumnDto("Hacker List"));

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}