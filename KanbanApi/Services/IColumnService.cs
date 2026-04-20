using KanbanApi.Models;

namespace KanbanApi.Services
{
    public interface IColumnService
    {
        Task<Column> CreateAsync(int boardId, string name);
        Task<Column?> UpdateAsync(int boardId, int columnId, string name);
        Task<bool?> DeleteAsync(int boardId, int columnId);
    }
}
