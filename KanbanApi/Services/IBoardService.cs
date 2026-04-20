using KanbanApi.Models;

namespace KanbanApi.Services;

public interface IBoardService
{
    Task<IReadOnlyList<Board>> GetByUserIdAsync(string userId);
    Task<Board?> GetByIdAsync(int id);
    Task<Board> CreateAsync(Board board, string ownerUserId);
    Task<Board?> UpdateAsync(int id, Board board);
    Task<bool> DeleteAsync(int id);
    Task<bool> AddMemberAsync(int boardId, string userId);
    Task<bool> IsMemberAsync(int boardId, string userId);
}
