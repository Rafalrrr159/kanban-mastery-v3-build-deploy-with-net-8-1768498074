using KanbanApi.Data;
using Microsoft.EntityFrameworkCore;
using KanbanApi.Models;

namespace KanbanApi.Services
{
    public class ColumnService : IColumnService
    {
        private readonly ApplicationDbContext _context;

        public ColumnService(ApplicationDbContext context) => _context = context;

        public async Task<Column> CreateAsync(int boardId, string name)
        {
            var column = new Column { Name = name, BoardId = boardId };
            _context.Columns.Add(column);
            await _context.SaveChangesAsync();
            return column;
        }

        public async Task<Column?> UpdateAsync(int boardId, int columnId, string name)
        {
            var column = await _context.Columns
                .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);

            if (column == null) return null;

            column.Name = name;
            await _context.SaveChangesAsync();
            return column;
        }

        public async Task<bool?> DeleteAsync(int boardId, int columnId)
        {
            var column = await _context.Columns
                .Include(c => c.Cards)
                .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);

            if (column == null) return null;

            if (column.Cards.Any()) return false;

            _context.Columns.Remove(column);
            await _context.SaveChangesAsync();
            return true;
        }
    }
}