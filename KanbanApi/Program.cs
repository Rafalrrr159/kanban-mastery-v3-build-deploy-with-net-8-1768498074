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
builder.Services.AddScoped<IColumnService, ColumnService>();
builder.Services.AddScoped<ICardService, CardService>();
builder.Services.AddHttpContextAccessor();

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

app.MapPut("/api/boards/{boardId}", 
    async (
    int boardId,
    UpdateBoardDto dto,
    IBoardService boardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
    if (!authResult.Succeeded) return Results.Forbid();

    var boardToUpdate = new Board { Name = dto.Name };
    var updatedBoard = await boardService.UpdateAsync(boardId, boardToUpdate);

    return updatedBoard is not null ? TypedResults.Ok(updatedBoard) : Results.NotFound();
}).RequireAuthorization();

app.MapDelete("/api/boards/{boardId}", 
    async (
    int boardId,
    IBoardService boardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardOwner");
    if (!authResult.Succeeded) return Results.Forbid();

    var deleted = await boardService.DeleteAsync(boardId);
    return deleted ? Results.NoContent() : Results.NotFound();
}).RequireAuthorization();

app.MapPost("/api/boards", async (Dtos dto, IBoardService boardService, ClaimsPrincipal user) =>
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

var columns = app.MapGroup("/api/boards/{boardId}/columns")
    .RequireAuthorization();

columns.MapPost("/", async (
    int boardId,
    CreateColumnDto dto,
    IColumnService columnService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var column = await columnService.CreateAsync(boardId, dto.Name);
    return Results.Created($"/api/boards/{boardId}/columns/{column.Id}", column);
});

columns.MapPut("/{columnId}", async (
    int boardId,
    int columnId,
    UpdateColumnDto dto,
    IColumnService columnService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var updated = await columnService.UpdateAsync(boardId, columnId, dto.Name);
    return updated != null ? Results.Ok(updated) : Results.NotFound();
});

columns.MapDelete("/{columnId}", async (
    int boardId,
    int columnId,
    IColumnService columnService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var result = await columnService.DeleteAsync(boardId, columnId);

    return result switch
    {
        true => Results.NoContent(),
        false => Results.BadRequest("Cannot delete column with existing cards."),
        null => Results.NotFound()
    };
});

var cards = app.MapGroup("/api/boards/{boardId}/cards")
    .RequireAuthorization();

cards.MapPost("/", async (
    int boardId,
    CreateCardDto dto,
    ICardService cardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var card = await cardService.CreateAsync(boardId, dto);

    return card != null
        ? Results.Created($"/api/boards/{boardId}/cards/{card.Id}", card)
        : Results.BadRequest("Invalid ColumnId.");
});

cards.MapPut("/{cardId}", async (
    int boardId,
    int cardId,
    UpdateCardDto dto,
    ICardService cardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var updated = await cardService.UpdateAsync(boardId, cardId, dto);
    return updated != null ? Results.Ok(updated) : Results.NotFound("Card not found or invalid ColumnId.");
});

cards.MapDelete("/{cardId}", async (
    int boardId,
    int cardId,
    ICardService cardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var deleted = await cardService.DeleteAsync(boardId, cardId);
    return deleted ? Results.NoContent() : Results.NotFound();
});

cards.MapPut("/{cardId}/assign", async (
    int boardId,
    int cardId,
    AssignCardDto dto,
    ICardService cardService,
    IBoardService boardService,
    IAuthorizationService authService,
    ClaimsPrincipal user) =>
{
    var authResult = await authService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var isTargetUserMember = await boardService.IsMemberAsync(boardId, dto.UserId);
    if (!isTargetUserMember)
        return Results.BadRequest("User is not a board member");

    var card = await cardService.AssignAsync(boardId, cardId, dto.UserId);

    if (card is null) return Results.NotFound();

    return Results.Ok(new
    {
        card.Id,
        card.Title,
        card.Description,
        card.ColumnId,
        card.AssignedToUserId
    });
});

app.Run();

public partial class Program { }