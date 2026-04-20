using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KanbanApi.Tests
{
    public class BoardManagementTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

        public BoardManagementTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "BoardManageDb_" + Guid.NewGuid().ToString();
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
        public async Task BoardManagement_OwnerCanModify_MemberGetsForbidden()
        {
            var ownerEmail = "owner@manage.com";
            var memberEmail = "member@manage.com";
            var pwd = "Password123!";

            await _client.PostAsJsonAsync("/register", new { email = ownerEmail, password = pwd });
            await _client.PostAsJsonAsync("/register", new { email = memberEmail, password = pwd });

            using var scope = _factory.Services.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var memberUser = await db.Users.FirstOrDefaultAsync(u => u.Email == memberEmail);
            var memberId = memberUser!.Id;

            var ownerLogin = await _client.PostAsJsonAsync("/login", new { email = ownerEmail, password = pwd });
            var ownerToken = (await ownerLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var boardCreate = await _client.PostAsJsonAsync("/api/boards", new CreateBoardDto("Stara Nazwa"));
            var boardId = (await boardCreate.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

            await _client.PostAsJsonAsync($"/api/boards/{boardId}/members", new AddBoardMemberDto(memberId));

            var memberLogin = await _client.PostAsJsonAsync("/login", new { email = memberEmail, password = pwd });
            var memberToken = (await memberLogin.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", memberToken);

            var memberPut = await _client.PutAsJsonAsync($"/api/boards/{boardId}", new UpdateBoardDto("Nowa Nazwa - HACK"));
            var memberDelete = await _client.DeleteAsync($"/api/boards/{boardId}");

            Assert.Equal(HttpStatusCode.Forbidden, memberPut.StatusCode);
            Assert.Equal(HttpStatusCode.Forbidden, memberDelete.StatusCode);

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", ownerToken);

            var ownerPut = await _client.PutAsJsonAsync($"/api/boards/{boardId}", new UpdateBoardDto("Poprawna Nowa Nazwa"));
            Assert.Equal(HttpStatusCode.OK, ownerPut.StatusCode);

            var ownerDelete = await _client.DeleteAsync($"/api/boards/{boardId}");
            Assert.Equal(HttpStatusCode.NoContent, ownerDelete.StatusCode);
        }
    }
}