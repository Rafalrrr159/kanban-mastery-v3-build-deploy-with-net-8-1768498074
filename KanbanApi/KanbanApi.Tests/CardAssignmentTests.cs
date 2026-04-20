using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KanbanApi.Tests
{
    public class CardAssignmentTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public CardAssignmentTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "CardAssignDb_" + Guid.NewGuid().ToString();
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
        public async Task AssignCard_ToMemberSucceeds_ToNonMemberFails()
        {
            var ownerEmail = "owner@assign.com";
            var memberEmail = "member@assign.com";
            var outsiderEmail = "outsider@assign.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = ownerEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = memberEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = outsiderEmail, password = pwd });

            string memberId, outsiderId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                memberId = (await db.Users.FirstAsync(u => u.Email == memberEmail)).Id;
                outsiderId = (await db.Users.FirstAsync(u => u.Email == outsiderEmail)).Id;
            }

            var login = await _client.PostAsJsonAsync("/login", new { email = ownerEmail, password = pwd });
            var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var boardCreate = await _client.PostAsJsonAsync("/api/boards", new CreateBoardDto("Projekt XYZ"));
            var boardId = (await boardCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            await _client.PostAsJsonAsync($"/api/boards/{boardId}/members", new AddBoardMemberDto(memberId));

            int cardId;
            using (var scope = _factory.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var col = new Column { Name = "To Do", BoardId = boardId };
                db.Columns.Add(col);
                await db.SaveChangesAsync();

                var card = new Card { Title = "Moja karta", ColumnId = col.Id };
                db.Cards.Add(card);
                await db.SaveChangesAsync();
                cardId = card.Id;
            }

            var badResponse = await _client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}/assign", new AssignCardDto(outsiderId));
            Assert.Equal(HttpStatusCode.BadRequest, badResponse.StatusCode);

            var goodResponse = await _client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}/assign", new AssignCardDto(memberId));
            Assert.Equal(HttpStatusCode.OK, goodResponse.StatusCode);
        }
    }
}