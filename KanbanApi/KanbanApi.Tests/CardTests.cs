using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KanbanApi.Tests
{
    public class CardTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CardTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "CardTestDb_" + Guid.NewGuid().ToString();
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
        public async Task Card_Lifecycle_VerifyOperationsAndSecurity()
        {
            var memberEmail = "member@cards.com";
            var hackerEmail = "hacker@cards.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = memberEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = hackerEmail, password = pwd });

            var login = await _client.PostAsJsonAsync("/login", new { email = memberEmail, password = pwd });
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var boardCreate = await _client.PostAsJsonAsync("/api/boards", new CreateBoardDto("Moja Tablica"));
            var boardId = (await boardCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            int colToDoid, colDoneId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var col1 = new Column { Name = "To Do", BoardId = boardId };
                var col2 = new Column { Name = "Done", BoardId = boardId };
                db.Columns.AddRange(col1, col2);
                await db.SaveChangesAsync();
                colToDoid = col1.Id;
                colDoneId = col2.Id;
            }

            var createResponse = await _client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
                new CreateCardDto("Zadanie 1", "Opis zadania", colToDoid));

            Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
            var cardId = (await createResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            var putResponse = await _client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
                new UpdateCardDto("Zadanie 1 - ZROBIONE", "Opis zadania", colDoneId));

            Assert.Equal(HttpStatusCode.OK, putResponse.StatusCode);

            var hackerLogin = await _client.PostAsJsonAsync("/login", new { email = hackerEmail, password = pwd });
            var hackerToken = (await hackerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hackerToken);

            var hackerDelete = await _client.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");
            Assert.Equal(HttpStatusCode.Forbidden, hackerDelete.StatusCode);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var deleteResponse = await _client.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");

            Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
        }
    }
}