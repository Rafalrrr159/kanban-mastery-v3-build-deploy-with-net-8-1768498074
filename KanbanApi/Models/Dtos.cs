namespace KanbanApi.Models
{
    public record Dtos(string Name);
    public record AddBoardMemberDto(string UserId);
    public record CardDto(int Id, string Title, string? Description);
    public record ColumnDto(int Id, string Name, IEnumerable<CardDto> Cards);
    public record BoardDetailsDto(int Id, string Name, IEnumerable<ColumnDto> Columns);
    public record CreateColumnDto(string Name);
    public record UpdateColumnDto(string Name);
    public record CreateBoardDto(string Name);
    public record UpdateBoardDto(string Name);
    public record CreateCardDto(string Title, string? Description, int ColumnId);
    public record UpdateCardDto(string Title, string? Description, int ColumnId);
    public record AssignCardDto(string UserId);
}
