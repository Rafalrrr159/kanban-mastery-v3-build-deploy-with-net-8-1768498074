using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace KanbanApi.Tests;

public class BoardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;
    private readonly HttpClient _client;

    public BoardTests(WebApplicationFactory<Program> factory)
    {
        var dbName = "BoardTestDb_" + Guid.NewGuid().ToString();

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
    public async Task CreateBoard_ValidRequest_CreatesBoardAndOwnerMembership()
    {
        var email = "board_owner@example.com";
        var password = "Password123!";

        var regResponse = await _client.PostAsJsonAsync("/register", new { email, password });
        var loginResponse = await _client.PostAsJsonAsync("/login", new { email, password });

        var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginContent.GetProperty("accessToken").GetString();

        _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var boardName = "Tablica Testowa";
        var response = await _client.PostAsJsonAsync("/api/boards", new Dtos(boardName));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var createdBoard = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(boardName, createdBoard.GetProperty("name").GetString());

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

        var boardId = createdBoard.GetProperty("id").GetInt32();
        var boardInDb = await db.Boards.Include(b => b.Members).FirstOrDefaultAsync(b => b.Id == boardId);
        Assert.NotNull(boardInDb);

        var membership = boardInDb.Members.FirstOrDefault();
        Assert.NotNull(membership);
        Assert.Equal(BoardRole.Owner, membership.Role);

        var user = await db.Users.FirstOrDefaultAsync(u => u.Email == email);
        Assert.Equal(user!.Id, membership.UserId);
    }
}