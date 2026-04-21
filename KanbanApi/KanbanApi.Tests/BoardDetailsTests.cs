using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Tests
{
    public class BoardDetailsTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BoardDetailsTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "BoardDetailsDb_" + Guid.NewGuid().ToString();
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
        public async Task GetBoardDetails_MemberGetsData_NonMemberGetsForbidden()
        {
            var memberEmail = "member@test.com";
            var hackerEmail = "hacker@test.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = memberEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = hackerEmail, password = pwd });

            var loginResponse = await _client.PostAsJsonAsync("/login", new { email = memberEmail, password = pwd });
            var token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var boardResponse = await _client.PostAsJsonAsync("/api/boards", new Dtos("Projekt Alpha"));
            var boardId = (await boardResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var column = new Column { Name = "To Do", BoardId = boardId };
                db.Columns.Add(column);
                await db.SaveChangesAsync();

                var card = new Card { Title = "Pierwsze Zadanie", ColumnId = column.Id };
                db.Cards.Add(card);
                await db.SaveChangesAsync();
            }

            var getBoardResponse = await _client.GetAsync($"/api/boards/{boardId}");

            Assert.Equal(HttpStatusCode.OK, getBoardResponse.StatusCode);
            var boardData = await getBoardResponse.Content.ReadFromJsonAsync<JsonElement>();

            Assert.Equal("Projekt Alpha", boardData.GetProperty("name").GetString());

            var columns = boardData.GetProperty("columns");
            Assert.True(columns.GetArrayLength() > 0, "Brak kolumn w JSONie!");

            var cards = columns[0].GetProperty("cards");
            Assert.True(cards.GetArrayLength() > 0, "Brak kart w JSONie!");
            Assert.Equal("Pierwsze Zadanie", cards[0].GetProperty("title").GetString());

            var hackerLogin = await _client.PostAsJsonAsync("/login", new { email = hackerEmail, password = pwd });
            var hackerToken = (await hackerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", hackerToken);

            var hackerResponse = await _client.GetAsync($"/api/boards/{boardId}");

            Assert.Equal(HttpStatusCode.Forbidden, hackerResponse.StatusCode);
        }

        [Fact]
        public async Task GetBoardDetails_NonExistentBoard_ReturnsForbidden()
        {
            var email = "notfound_test@example.com";
            var pwd = "Password123!";
            await _client.PostAsJsonAsync("/register", new { email, password = pwd });
            var loginResponse = await _client.PostAsJsonAsync("/login", new { email, password = pwd });
            var token = (await loginResponse.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/boards/9999");

            Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        }
    }
}
