using System.Security.Claims;
using KanbanApi.Data;
using KanbanApi.Models;
using KanbanApi.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddAuthorization();

// Required for DI. Without this, Minimal APIs will incorrectly infer IBoardService 
// as a Body parameter and throw an exception on GET requests.
builder.Services.AddScoped<IBoardService, BoardService>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapIdentityApi<ApplicationUser>();

app.MapGet("/api/users/me", async (ClaimsPrincipal user, ApplicationDbContext db) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var appUser = await db.Users.FindAsync(userId);

    if (appUser is null)
    {
        return Results.NotFound();
    }

    return TypedResults.Ok(new { appUser.Id, appUser.UserName, appUser.Email });
}).RequireAuthorization();

app.Run();


public partial class Program { }