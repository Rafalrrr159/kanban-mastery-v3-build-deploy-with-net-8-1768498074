namespace KanbanApi.Models
{
    public record CreateBoardDto(string Name);
    public record AddBoardMemberDto(string UserId);
    public record CardDto(int Id, string Title, string? Description);
    public record ColumnDto(int Id, string Name, IEnumerable<CardDto> Cards);
    public record BoardDetailsDto(int Id, string Name, IEnumerable<ColumnDto> Columns);
}
