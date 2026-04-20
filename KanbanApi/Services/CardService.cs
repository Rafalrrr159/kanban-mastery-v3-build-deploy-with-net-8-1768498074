using KanbanApi.Data;
using KanbanApi.Models;
using Microsoft.EntityFrameworkCore;

namespace KanbanApi.Services
{
    public class CardService : ICardService
    {
        private readonly ApplicationDbContext _context;

        public CardService(ApplicationDbContext context) => _context = context;

        public async Task<Card?> CreateAsync(int boardId, CreateCardDto dto)
        {
            var columnExists = await _context.Columns.AnyAsync(c => c.Id == dto.ColumnId && c.BoardId == boardId);
            if (!columnExists) return null;

            var card = new Card
            {
                Title = dto.Title,
                Description = dto.Description,
                ColumnId = dto.ColumnId
            };

            _context.Cards.Add(card);
            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<Card?> UpdateAsync(int boardId, int cardId, UpdateCardDto dto)
        {
            var card = await _context.Cards
                .Include(c => c.Column)
                .FirstOrDefaultAsync(c => c.Id == cardId && c.Column!.BoardId == boardId);

            if (card == null) return null;

            if (card.ColumnId != dto.ColumnId)
            {
                var newColumnExists = await _context.Columns.AnyAsync(c => c.Id == dto.ColumnId && c.BoardId == boardId);
                if (!newColumnExists) return null;
            }

            card.Title = dto.Title;
            card.Description = dto.Description;
            card.ColumnId = dto.ColumnId;

            await _context.SaveChangesAsync();
            return card;
        }

        public async Task<bool> DeleteAsync(int boardId, int cardId)
        {
            var card = await _context.Cards
                .Include(c => c.Column)
                .FirstOrDefaultAsync(c => c.Id == cardId && c.Column!.BoardId == boardId);

            if (card == null) return false;

            _context.Cards.Remove(card);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<Card?> AssignAsync(int boardId, int cardId, string userId)
        {
            var card = await _context.Cards
                .Include(c => c.Column)
                .FirstOrDefaultAsync(c => c.Id == cardId && c.Column!.BoardId == boardId);

            if (card == null) return null;

            card.AssignedToUserId = userId;
            await _context.SaveChangesAsync();
            return card;
        }
    }
}