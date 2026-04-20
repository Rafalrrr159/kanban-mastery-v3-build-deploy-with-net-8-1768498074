using KanbanApi.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using System.Net;
using System.Net.Http.Headers;
using System.Text.Json;

namespace KanbanApi.Tests
{
    public class AuthTests : IClassFixture<WebApplicationFactory<Program>>

    {
        private readonly HttpClient _client;

        public AuthTests(WebApplicationFactory<Program> factory)
        {
            var dbName = "TestDb_" + Guid.NewGuid().ToString();

            _client = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureServices(services =>
                {
                    var descriptor = services.SingleOrDefault(
                        d => d.ServiceType == typeof(DbContextOptions<ApplicationDbContext>));
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
            }).CreateClient();
        }

        [Fact]
        public async Task Register_WithValidData_ReturnsOk()
        {
            var response = await _client.PostAsJsonAsync("/register", new
            {
                email = "test1@example.com",
                password = "Password123!"
            });

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Login_WithValidCredentials_ReturnsOkWithToken()
        {
            var credentials = new { email = "login_test@example.com", password = "Password123!" };

            await _client.PostAsJsonAsync("/register", credentials);

            var response = await _client.PostAsJsonAsync("/login", credentials);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var content = await response.Content.ReadAsStringAsync();

            Assert.Contains("accessToken", content);
        }

        [Fact]
        public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
        {
            var userData = new { email = "duplicate@example.com", password = "Password123!" };

            await _client.PostAsJsonAsync("/register", userData);

            var response = await _client.PostAsJsonAsync("/register", userData);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }



        [Fact]
        public async Task GetCurrentUser_Unauthenticated_ReturnsUnauthorized()
        {
            var response = await _client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [Fact]
        public async Task GetCurrentUser_Authenticated_ReturnsOkWithUserData()
        {
            var email = "smoke_test@example.com";
            var password = "TestPassword123!";
            await _client.PostAsJsonAsync("/register", new { email, password });

            var loginResponse = await _client.PostAsJsonAsync("/login", new { email, password });
            var loginContent = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
            var token = loginContent.GetProperty("accessToken").GetString();

            _client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.GetAsync("/api/users/me");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var userProfile = await response.Content.ReadFromJsonAsync<JsonElement>();
            Assert.Equal(email, userProfile.GetProperty("email").GetString());
            Assert.True(userProfile.TryGetProperty("id", out _), "Odpowiedź powinna zawierać pole 'id'");

            _client.DefaultRequestHeaders.Authorization = null;
        }
    }
}
