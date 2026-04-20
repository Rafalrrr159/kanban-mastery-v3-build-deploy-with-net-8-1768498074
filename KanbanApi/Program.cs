using KanbanApi.Authorization;
using KanbanApi.Data;
using KanbanApi.Models;
using KanbanApi.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddScoped<IAuthorizationHandler, IsBoardOwnerHandler>();
builder.Services.AddScoped<IAuthorizationHandler, IsBoardMemberHandler>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsBoardOwner", policy =>
        policy.Requirements.Add(new IsBoardOwnerRequirement()));

    options.AddPolicy("IsBoardMember", policy =>
        policy.Requirements.Add(new IsBoardMemberRequirement()));
});

// Required for DI. Without this, Minimal APIs will incorrectly infer IBoardService 
// as a Body parameter and throw an exception on GET requests.
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IUserService, UserService>();

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

app.MapGet("/api/users/me", async (ClaimsPrincipal user, IUserService userService) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();

    var profile = await userService.GetUserProfileAsync(userId);

    if (profile is null) return Results.NotFound();

    return TypedResults.Ok(profile);
}).RequireAuthorization();

app.MapPost("/api/boards", async (CreateBoardDto dto, IBoardService boardService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    if (userId is null) return Results.Unauthorized();

    var board = new Board { Name = dto.Name };

    var createdBoard = await boardService.CreateAsync(board, userId);

    return TypedResults.Created($"/api/boards/{createdBoard.Id}", new { id = createdBoard.Id, name = createdBoard.Name });
}).RequireAuthorization();

app.MapPost("/api/boards/{boardId}/members", 
    async (
    int boardId,
    AddBoardMemberDto dto,
    IBoardService boardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");

    if (!authResult.Succeeded)
    {
        return Results.Forbid();
    }

    var result = await boardService.AddMemberAsync(boardId, dto.UserId);

    if (!result) return Results.BadRequest("User is already a member or invalid request.");

    return Results.Ok();
}).RequireAuthorization();

app.MapGet("/api/boards/{boardId}", 
    async (
    int boardId,
    IBoardService boardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded)
    {
        return Results.Forbid();
    }

    var board = await boardService.GetByIdAsync(boardId);
    if (board == null) return Results.NotFound();

    var responseDto = new BoardDetailsDto(
        board.Id,
        board.Name,
        board.Columns.Select(col => new ColumnDto(
            col.Id,
            col.Name,
            col.Cards.Select(c => new CardDto(c.Id, c.Title, c.Description))
        ))
    );

    return TypedResults.Ok(responseDto);
}).RequireAuthorization();

app.Run();

public partial class Program { }